using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Alibre;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Views
{
    /// <summary>
    /// Editor for multi-shaft gear trains: a tree of shafts and gears, a property grid, a 3D scene with one visual per
    /// part, and the gear ratio and speed report. Everything is driven by <see cref="GearTrainPresenter"/>.
    /// </summary>
    public sealed class GearTrainForm : Form, IGearTrainView
    {
        private readonly GearTrainPresenter _presenter;
        private readonly TreeView _tree = new TreeView { Dock = DockStyle.Fill, CheckBoxes = true, HideSelection = false };
        private readonly PropertyGrid _grid = new PropertyGrid { Dock = DockStyle.Fill, ToolbarVisible = false };
        private readonly HelixAssemblyViewport _viewport = new HelixAssemblyViewport();
        private readonly ElementHost _host = new ElementHost { Dock = DockStyle.Fill };
        private readonly GearScene2DControl _view2D = new GearScene2DControl { Dock = DockStyle.Fill };
        private readonly DataGridView _gearsGrid = CreateGrid();
        private readonly DataGridView _shaftsGrid = CreateGrid();
        private readonly DataGridView _stagesGrid = CreateGrid();
        private readonly DataGridView _gearParamsGrid = CreateParameterGrid();
        private readonly DataGridView _meshParamsGrid = CreateParameterGrid();
        private readonly Label _gearParamsTitle = CreateTitleLabel();
        private readonly Label _meshParamsTitle = CreateTitleLabel();
        private readonly DataGridView _imperialGearGrid = CreateImperialGrid();
        private readonly DataGridView _imperialMeshGrid = CreateImperialGrid();
        private readonly Label _imperialGearTitle = CreateTitleLabel();
        private readonly Label _imperialMeshTitle = CreateTitleLabel();
        private readonly Dictionary<string, Image> _latexImageCache = new Dictionary<string, Image>();
        private readonly TextBox _summary = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font("Consolas", 9f)
        };

        private readonly ToolStripStatusLabel _status = new ToolStripStatusLabel("Ready");
        private readonly NumericUpDown _numSpeed = new NumericUpDown { Minimum = -100000, Maximum = 100000, DecimalPlaces = 1, Value = 1000, Width = 80 };
        private readonly NumericUpDown _numAngle = new NumericUpDown { Minimum = -360, Maximum = 360, DecimalPlaces = 1, Value = 0, Width = 70 };
        private readonly ToolStripMenuItem _btnWireframe = new ToolStripMenuItem("Wireframe") { CheckOnClick = true };
        private readonly ToolStripMenuItem _btnShafts = new ToolStripMenuItem("Shafts") { CheckOnClick = true, Checked = true };
        private readonly ToolStripMenuItem _btnPitchCircles = new ToolStripMenuItem("Pitch circles") { CheckOnClick = true, Checked = true, ToolTipText = "2D view: show the pitch circle of each spur and helical gear." };
        private readonly ToolStripMenuItem _btnLabels = new ToolStripMenuItem("Labels") { CheckOnClick = true, Checked = true, ToolTipText = "2D view: show the name of each gear." };
        private readonly ToolStripMenuItem _btnAnimate = new ToolStripMenuItem("Animate") { CheckOnClick = true };
        private readonly ToolStripMenuItem _btnExportAlibre = new ToolStripMenuItem("Export gear to &Alibre…")
        {
            Enabled = false,
            ToolTipText = "Create the selected gear as a new Alibre part. Involute gears (external or internal, spur or helical) and worm wheels: one closed tooth sketch, extruded (swept along a helix for a helical gear) and circular-patterned. Racks (straight or helical): one tooth cell, extruded (swept along a slanted path for a helical rack) and linear-patterned. Worms: a core cylinder with a helical thread. Available when the editor runs inside Alibre Design."
        };
        private readonly ToolStripMenuItem _btnExportAlibreAssembly = new ToolStripMenuItem("Export Assembly to Alibre")
        {
            Enabled = false,
            ToolTipText = "Create a new Alibre assembly from the gear train (two or more gears). Each gear is added as a new part, built like the single gear export and placed at its shaft position. Available when the editor runs inside Alibre Design."
        };
        private readonly NumericUpDown _numAnimSpeed = new NumericUpDown { Minimum = -600, Maximum = 600, DecimalPlaces = 1, Value = 30, Width = 70 };
        private readonly Timer _animationTimer = new Timer { Interval = 60 };

        private bool _syncingAngle;
        private bool _updating;
        private AssemblyScene _scene;
        private object _selected;
        private ISet<GearComponent> _hidden;
        private AlibreGearExporter _alibreExporter;

        public GearTrainForm()
        {
            Text = "Gear Train";
            Size = new Size(1400, 860);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f);

            _host.Child = _viewport;
            _presenter = new GearTrainPresenter(this);
            BuildLayout();
            WireEvents();
        }

        public GearTrainPresenter Presenter => _presenter;

        /// <summary>
        /// Creates gears as Alibre parts. Set by the Alibre add-on; null when the editor runs as a stand-alone program,
        /// in which case the export menu item is disabled.
        /// </summary>
        public AlibreGearExporter AlibreExporter
        {
            get => _alibreExporter;
            set
            {
                _alibreExporter = value;
                _btnExportAlibre.Enabled = value != null;
                _btnExportAlibreAssembly.Enabled = value != null;
            }
        }

        public double InputSpeedRpm => (double)_numSpeed.Value;

        private static DataGridView CreateGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                BackgroundColor = SystemColors.Window
            };
        }

        private static Label CreateTitleLabel()
        {
            return new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
        }

        /// <summary>A grid for calculated parameters: name, rendered symbol, rendered formula, value and unit.</summary>
        private static DataGridView CreateParameterGrid()
        {
            var grid = CreateGrid();
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Parameter" });
            grid.Columns.Add(new DataGridViewImageColumn { Name = "colSymbol", HeaderText = "Symbol", ImageLayout = DataGridViewImageCellLayout.Normal });
            grid.Columns.Add(new DataGridViewImageColumn { Name = "colFormula", HeaderText = "Formula", ImageLayout = DataGridViewImageCellLayout.Normal });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colValue", HeaderText = "Value" });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit", HeaderText = "Unit" });
            grid.Columns["colValue"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return grid;
        }

        /// <summary>A grid for the imperial reference: name, symbol, formula, metric value and unit, imperial value and unit.</summary>
        private static DataGridView CreateImperialGrid()
        {
            var grid = CreateGrid();
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Parameter" });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSymbol", HeaderText = "Symbol" });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFormula", HeaderText = "Conversion" });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMetric", HeaderText = "Metric value" });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMetricUnit", HeaderText = "Metric unit" });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colImperial", HeaderText = "Imperial value" });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colImperialUnit", HeaderText = "Imperial unit" });
            grid.Columns["colMetric"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["colImperial"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return grid;
        }

        private void BuildLayout()
        {
            var menu = new MenuStrip();
            var btnNew = new ToolStripMenuItem("&New train");
            var btnAddGear = new ToolStripMenuItem("Add &gear to shaft");
            var btnAddMeshed = new ToolStripMenuItem("Add &meshed gear");
            var btnRemove = new ToolStripMenuItem("&Remove selected");
            var btnFitShift = new ToolStripMenuItem("Fit &profile shift")
            {
                ToolTipText = "Give the selected driven gear the profile shift that makes its mesh run at the current centre distance."
            };
            var btnFit = new ToolStripMenuItem("&Fit view");

            var ddStart = new ToolStripMenuItem("&Start with");
            ddStart.DropDownItems.Add("Spur gear", null, (s, e) => RunCommand(() => _presenter.NewTrain(GearFamily.Spur)));
            ddStart.DropDownItems.Add("Bevel gear (standard)", null, (s, e) => RunCommand(() => _presenter.NewTrain(GearFamily.Bevel, BevelGearType.Standard)));
            ddStart.DropDownItems.Add("Bevel gear (Gleason)", null, (s, e) => RunCommand(() => _presenter.NewTrain(GearFamily.Bevel, BevelGearType.Gleason)));
            ddStart.DropDownItems.Add("Worm", null, (s, e) => RunCommand(() => _presenter.NewTrain(GearFamily.Worm)));
            ddStart.DropDownItems.Add("Planetary stage", null, (s, e) => RunCommand(() => _presenter.NewPlanetary()));

            var ddMate = new ToolStripMenuItem("Add m&ate");
            ddMate.DropDownItems.Add("Rack for the selected pinion", null, (s, e) => RunCommand(() => AddMateToSelectedGear(MeshType.Rack)));
            ddMate.DropDownItems.Add("Bevel gear for the selected bevel gear", null, (s, e) => RunCommand(() => AddMateToSelectedGear(MeshType.Bevel)));
            ddMate.DropDownItems.Add("Worm wheel for the selected worm", null, (s, e) => RunCommand(() => AddMateToSelectedGear(MeshType.Worm)));

            var ddFile = new ToolStripMenuItem("&File");
            ddFile.DropDownItems.Add(btnNew);
            ddFile.DropDownItems.Add(new ToolStripSeparator());
            ddFile.DropDownItems.Add("Save gear train…", null, (s, e) => SaveTrain());
            ddFile.DropDownItems.Add("Load gear train…", null, (s, e) => LoadTrain());
            ddFile.DropDownItems.Add(new ToolStripSeparator());
            ddFile.DropDownItems.Add("Export STL…", null, (s, e) => ExportScene(false));
            ddFile.DropDownItems.Add("Export OBJ…", null, (s, e) => ExportScene(true));
            ddFile.DropDownItems.Add(new ToolStripSeparator());
            ddFile.DropDownItems.Add(_btnExportAlibre);
            ddFile.DropDownItems.Add(_btnExportAlibreAssembly);
            ddFile.DropDownItems.Add(new ToolStripSeparator());
            ddFile.DropDownItems.Add("E&xit", null, (s, e) => Close());

            var menuBuild = new ToolStripMenuItem("&Build");
            menuBuild.DropDownItems.AddRange(new ToolStripItem[]
            {
                ddStart, new ToolStripSeparator(), btnAddGear, btnAddMeshed, ddMate, new ToolStripSeparator(), btnFitShift, btnRemove
            });

            var menuView = new ToolStripMenuItem("&View");
            menuView.DropDownItems.AddRange(new ToolStripItem[]
            {
                _btnWireframe, _btnShafts, _btnPitchCircles, _btnLabels, new ToolStripSeparator(), btnFit
            });

            var menuSimulation = new ToolStripMenuItem("&Simulation");
            menuSimulation.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripLabel("Input speed (rpm)"), new ToolStripControlHost(_numSpeed),
                new ToolStripLabel("Input angle (deg)"), new ToolStripControlHost(_numAngle), new ToolStripSeparator(),
                _btnAnimate, new ToolStripLabel("Animation (rpm)"), new ToolStripControlHost(_numAnimSpeed)
            });

            _btnExportAlibre.Click += (s, e) => RunCommand(ExportSelectedGearToAlibre);
            _btnExportAlibreAssembly.Click += (s, e) => RunCommand(ExportAssemblyToAlibre);
            btnNew.Click += (s, e) => RunCommand(() => _presenter.NewTrain());
            btnAddGear.Click += (s, e) => RunCommand(AddGearToSelectedShaft);
            btnAddMeshed.Click += (s, e) => RunCommand(AddMeshedToSelectedGear);
            btnRemove.Click += (s, e) => _presenter.RemoveSelected();
            btnFitShift.Click += (s, e) => RunCommand(FitProfileShiftOfSelectedGear);
            btnFit.Click += (s, e) =>
            {
                _viewport.FitView();
                _view2D.FitView();
            };
            _btnPitchCircles.Click += (s, e) => _view2D.ShowPitchCircles = _btnPitchCircles.Checked;
            _btnLabels.Click += (s, e) => _view2D.ShowLabels = _btnLabels.Checked;
            _btnWireframe.Click += (s, e) => RenderScene();
            _btnShafts.Click += (s, e) => RenderScene();

            var menuHelp = new ToolStripMenuItem("&Help");
            menuHelp.DropDownItems.Add("&About", null, (s, e) => ShowAbout());
            menuHelp.DropDownItems.Add("&Help", null, (s, e) => OpenDocument("README.md", false));
            menuHelp.DropDownItems.Add("&License", null, (s, e) => OpenDocument("LICENSE", true));

            menu.Items.AddRange(new ToolStripItem[] { ddFile, menuBuild, menuView, menuSimulation, menuHelp });
            MainMenuStrip = menu;

            var statusBar = new StatusStrip();
            statusBar.Items.Add(_status);

            var leftSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            leftSplit.Panel1.Controls.Add(_tree);
            leftSplit.Panel2.Controls.Add(_grid);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            AddTab(tabs, "Summary", _summary);
            AddTab(tabs, "Gears", _gearsGrid);
            AddTab(tabs, "Shafts", _shaftsGrid);
            AddTab(tabs, "Stages", _stagesGrid);
            AddTab(tabs, "Gear parameters", CreateParameterPanel(_gearParamsTitle, _gearParamsGrid));
            AddTab(tabs, "Mesh parameters", CreateParameterPanel(_meshParamsTitle, _meshParamsGrid));
            AddTab(tabs, "Imperial reference", CreateImperialPanel());

            var rightSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            var viewTabs = new TabControl { Dock = DockStyle.Fill };
            AddTab(viewTabs, "3D view", _host);
            AddTab(viewTabs, "2D view", _view2D);
            rightSplit.Panel1.Controls.Add(viewTabs);
            rightSplit.Panel2.Controls.Add(tabs);

            var mainSplit = new SplitContainer { Dock = DockStyle.Fill };
            mainSplit.Panel1.Controls.Add(leftSplit);
            mainSplit.Panel2.Controls.Add(rightSplit);

            Controls.Add(mainSplit);
            Controls.Add(statusBar);
            Controls.Add(menu);

            Load += (s, e) =>
            {
                SetSplitter(mainSplit, 360);
                SetSplitter(leftSplit, leftSplit.Height / 2);
                SetSplitter(rightSplit, (int)(rightSplit.Height * 0.62));
                _presenter.NewTrain();
            };
        }

        private Control CreateImperialPanel()
        {
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            split.Panel1.Controls.Add(CreateParameterPanel(_imperialGearTitle, _imperialGearGrid));
            split.Panel2.Controls.Add(CreateParameterPanel(_imperialMeshTitle, _imperialMeshGrid));
            split.HandleCreated += (s, e) => SetSplitter(split, (int)(split.Height * 0.6));
            return split;
        }

        private static Control CreateParameterPanel(Label title, DataGridView grid)
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            panel.Controls.Add(grid);
            panel.Controls.Add(title);
            return panel;
        }

        private static void AddTab(TabControl tabs, string title, Control content)
        {
            var page = new TabPage(title);
            page.Controls.Add(content);
            tabs.TabPages.Add(page);
        }

        private static void SetSplitter(SplitContainer split, int distance)
        {
            try
            {
                split.SplitterDistance = Math.Max(split.Panel1MinSize, distance);
            }
            catch (ArgumentException)
            {
                // The container is smaller than the requested distance; keep the default.
            }
        }

        private void WireEvents()
        {
            _tree.AfterSelect += (s, e) =>
            {
                if (_updating) return;
                _presenter.Select((e.Node?.Tag as TrainTreeNode)?.Tag);
            };

            _tree.AfterCheck += (s, e) =>
            {
                if (_updating) return;
                var node = e.Node;
                _updating = true;
                try
                {
                    SetCheckedRecursive(node, node.Checked);
                }
                finally
                {
                    _updating = false;
                }

                _presenter.SetGearsVisible(CollectGears(node), node.Checked);
            };

            _grid.PropertyValueChanged += (s, e) => BeginInvoke(new Action(_presenter.Refresh));
            _viewport.ItemPicked += tag => _presenter.Select(tag);
            _view2D.ItemPicked += tag => _presenter.Select(tag);

            _numSpeed.ValueChanged += (s, e) => _presenter.Refresh();
            _numAngle.ValueChanged += (s, e) =>
            {
                if (_syncingAngle) return;
                _presenter.SetInputAngle((double)_numAngle.Value);
            };

            _btnAnimate.CheckedChanged += (s, e) => _animationTimer.Enabled = _btnAnimate.Checked;
            _animationTimer.Tick += (s, e) => AnimationTick();
            FormClosed += (s, e) =>
            {
                _animationTimer.Stop();
                _animationTimer.Dispose();
                foreach (var image in _latexImageCache.Values) image?.Dispose();
                _latexImageCache.Clear();
            };
        }

        private void AnimationTick()
        {
            // Stop on the first problem so that an error message does not scroll away behind new frames.
            _animationTimer.Stop();
            try
            {
                _presenter.AdvanceAnimation(_animationTimer.Interval / 1000.0, (double)_numAnimSpeed.Value);

                var degrees = _presenter.InputAngleRad * 180.0 / Math.PI;
                degrees -= 360.0 * Math.Floor(degrees / 360.0);
                _syncingAngle = true;
                try
                {
                    _numAngle.Value = (decimal)Math.Max(0.0, Math.Min(360.0, Math.Round(degrees, 1)));
                }
                finally
                {
                    _syncingAngle = false;
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                ShowMessage(ex.Message, true);
                _btnAnimate.Checked = false;
            }
            finally
            {
                if (_btnAnimate.Checked && !IsDisposed) _animationTimer.Start();
            }
        }

        private void AddMateToSelectedGear(MeshType meshType)
        {
            if (!(_selected is GearComponent driver))
            {
                ShowMessage("Select the gear that the new gear should mesh with.", true);
                return;
            }

            _presenter.AddMate(driver, meshType);
        }

        private void FitProfileShiftOfSelectedGear()
        {
            if (!(_selected is GearComponent driven))
            {
                ShowMessage("Select the driven gear whose profile shift should be fitted to the centre distance.", true);
                return;
            }

            _presenter.FitProfileShiftToCentreDistance(driven);
        }

        private void SaveTrain()
        {
            using (var dialog = new SaveFileDialog { Filter = "Gear train (*.json)|*.json", DefaultExt = "json", Title = "Save gear train" })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK) _presenter.Save(dialog.FileName);
            }
        }

        private void LoadTrain()
        {
            using (var dialog = new OpenFileDialog { Filter = "Gear train (*.json)|*.json", Title = "Load gear train" })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK) _presenter.Load(dialog.FileName);
            }
        }

        private void ExportScene(bool obj)
        {
            var filter = obj ? "Wavefront OBJ (*.obj)|*.obj" : "STL (*.stl)|*.stl";
            var extension = obj ? "obj" : "stl";
            using (var dialog = new SaveFileDialog { Filter = filter, DefaultExt = extension, Title = "Export 3D model" })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK) _presenter.Export(dialog.FileName, _btnShafts.Checked);
            }
        }

        private void ExportAssemblyToAlibre()
        {
            if (_alibreExporter == null)
            {
                ShowMessage("Export to Alibre is only available when the editor is opened from the Alibre Design add-on.", true);
                return;
            }

            var count = _presenter.Assembly.Components.Count();
            if (count < AlibreGearExporter.MinimumAssemblyParts)
            {
                ShowMessage($"Add at least {AlibreGearExporter.MinimumAssemblyParts} gears to the train to export an assembly to Alibre.", true);
                return;
            }

            ShowMessage($"Exporting {count} gears to a new Alibre assembly…", false);
            try
            {
                _alibreExporter.ExportAssembly(_presenter.Assembly);
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
                MessageBox.Show(this, ex.Message, "Export Assembly to Alibre", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ShowMessage($"Exported {count} gears to a new Alibre assembly, each as a new part. Log: {_alibreExporter.LastExportLog}", false);
        }

        private void ExportSelectedGearToAlibre()
        {
            if (_alibreExporter == null)
            {
                ShowMessage("Export to Alibre is only available when the editor is opened from the Alibre Design add-on.", true);
                return;
            }

            switch (_selected)
            {
                case InvoluteGearComponent gear:
                {
                    ShowMessage($"Exporting {gear.Id} to Alibre…", false);
                    _alibreExporter.Export(gear);
                    var kind = (gear.IsInternal ? "internal " : "external ") + (gear.IsHelical ? "helical" : "spur");
                    ShowMessage(string.Format(CultureInfo.InvariantCulture, "Exported {0} ({1} gear, {2} teeth, module {3:0.###}) to a new Alibre part.", gear.Id, kind, gear.Teeth, gear.Module), false);
                    break;
                }
                case RackComponent rack:
                {
                    ShowMessage($"Exporting {rack.Id} to Alibre…", false);
                    _alibreExporter.Export(rack);
                    var kind = rack.IsHelical ? string.Format(CultureInfo.InvariantCulture, "helical, {0:0.###} degrees {1}", rack.HelixAngleDeg, rack.HelicalDirection) : "straight";
                    ShowMessage(string.Format(CultureInfo.InvariantCulture, "Exported {0} (rack, {1}, {2} teeth, module {3:0.###}) to a new Alibre part.", rack.Id, kind, rack.Teeth, rack.Module), false);
                    break;
                }
                case WormComponent worm:
                {
                    ShowMessage($"Exporting {worm.Id} to Alibre…", false);
                    _alibreExporter.Export(worm);
                    ShowMessage(string.Format(CultureInfo.InvariantCulture, "Exported {0} (worm, {1} start(s), axial module {2:0.###}, {3} hand) to a new Alibre part.", worm.Id, worm.Starts, worm.Module, worm.HelicalDirection == "R" ? "right" : "left"), false);
                    break;
                }
                case WormWheelComponent wheel:
                {
                    ShowMessage($"Exporting {wheel.Id} to Alibre…", false);
                    _alibreExporter.Export(wheel);
                    ShowMessage(string.Format(CultureInfo.InvariantCulture, "Exported {0} (worm wheel, {1} teeth, axial module {2:0.###}, helix angle {3:0.###} degrees) to a new Alibre part.", wheel.Id, wheel.Teeth, wheel.Module, wheel.LeadAngleDeg), false);
                    break;
                }
                default:
                    ShowMessage("Select a gear, rack, worm or worm wheel to export to Alibre.", true);
                    break;
            }
        }

        private void RunCommand(Action command)
        {
            try
            {
                command();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                ShowMessage(ex.Message, true);
            }
        }

        private void AddGearToSelectedShaft()
        {
            Shaft shaft = _selected as Shaft;
            if (shaft == null && _selected is GearComponent gear) _presenter.Assembly.FindMount(gear, out shaft);
            if (shaft == null) shaft = _presenter.InputShaft;
            if (shaft == null)
            {
                ShowMessage("Select a shaft first.", true);
                return;
            }

            _presenter.AddGearToShaft(shaft);
        }

        private void AddMeshedToSelectedGear()
        {
            if (!(_selected is GearComponent driver))
            {
                ShowMessage("Select the gear that the new gear should mesh with.", true);
                return;
            }

            _presenter.AddMeshedGear(driver);
        }

        private static void SetCheckedRecursive(TreeNode node, bool value)
        {
            node.Checked = value;
            foreach (TreeNode child in node.Nodes) SetCheckedRecursive(child, value);
        }

        private static IEnumerable<GearComponent> CollectGears(TreeNode node)
        {
            if ((node.Tag as TrainTreeNode)?.Tag is GearComponent gear) yield return gear;
            foreach (TreeNode child in node.Nodes)
            {
                foreach (var g in CollectGears(child)) yield return g;
            }
        }

        #region IGearTrainView

        public void ShowTree(TrainTreeNode root, object selectedTag)
        {
            _updating = true;
            try
            {
                _tree.BeginUpdate();
                _tree.Nodes.Clear();
                if (root != null) _tree.Nodes.Add(CreateNode(root));
                _tree.ExpandAll();
                _tree.EndUpdate();
                _selected = selectedTag;
                SelectNode(selectedTag);
            }
            finally
            {
                _updating = false;
            }

            ShowParameters();
        }

        public void ShowSelection(object selectedTag)
        {
            _selected = selectedTag;
            _updating = true;
            try
            {
                SelectNode(selectedTag);
            }
            finally
            {
                _updating = false;
            }

            ShowParameters();
        }

        public void ShowProperties(object properties)
        {
            _grid.SelectedObject = properties;
        }

        public void ShowScene(AssemblyScene scene, object selectedTag, ISet<GearComponent> hiddenGears)
        {
            _scene = scene;
            _selected = selectedTag;
            _hidden = hiddenGears;
            RenderScene();
        }

        public void ShowReport(GearTrainReport report)
        {
            if (report == null)
            {
                _summary.Text = string.Empty;
                _gearsGrid.DataSource = null;
                _shaftsGrid.DataSource = null;
                _stagesGrid.DataSource = null;
                ShowParameters();
                return;
            }

            _summary.Text = report.ToText();
            _gearsGrid.DataSource = report.Gears.ToList();
            _shaftsGrid.DataSource = report.Shafts.ToList();
            _stagesGrid.DataSource = report.Stages.ToList();
            ShowParameters();
        }

        public void ShowMessage(string message, bool isError)
        {
            _status.Text = message;
            _status.ForeColor = isError ? Color.Firebrick : SystemColors.ControlText;
        }

        #endregion

        /// <summary>Fills the gear and mesh parameter tables for the selected gear (a shaft or nothing clears them).</summary>
        private void ShowParameters()
        {
            var gear = _selected as GearComponent;
            if (gear == null)
            {
                FillParameterTable(_gearParamsGrid, _gearParamsTitle, GearParameterTable.Empty, "Select a gear to see its parameters, symbols and formulae.");
                FillParameterTable(_meshParamsGrid, _meshParamsTitle, GearParameterTable.Empty, "Select a gear to see the parameters of its mesh.");
                FillImperialTable(_imperialGearGrid, _imperialGearTitle, ImperialTable.Empty, "Select a gear to see its module as diametral pitch and circular pitch, and its lengths in inches.");
                FillImperialTable(_imperialMeshGrid, _imperialMeshTitle, ImperialTable.Empty, "Select a gear to see the lengths of its mesh in inches.");
                return;
            }

            GearParameterTable gearTable;
            try
            {
                gearTable = GearParameterTables.ForGear(gear);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                gearTable = GearParameterTable.Empty;
                _gearParamsTitle.Text = ex.Message;
            }

            FillParameterTable(_gearParamsGrid, _gearParamsTitle, gearTable, _gearParamsTitle.Text);

            var relation = GearParameterTables.FindMeshFor(_presenter.Assembly, gear);
            var meshTable = GearParameterTables.ForMesh(relation);
            FillParameterTable(_meshParamsGrid, _meshParamsTitle, meshTable,
                relation == null ? $"{gear.Name} does not mesh with another gear." : "This mesh has no involute pair parameters.");

            ImperialTable imperialGear;
            try
            {
                imperialGear = ImperialReference.ForGear(gear);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                imperialGear = ImperialTable.Empty;
                _imperialGearTitle.Text = ex.Message;
            }

            FillImperialTable(_imperialGearGrid, _imperialGearTitle, imperialGear, _imperialGearTitle.Text);
            FillImperialTable(_imperialMeshGrid, _imperialMeshTitle, ImperialReference.ForMesh(relation),
                relation == null ? $"{gear.Name} does not mesh with another gear." : "This mesh has no involute pair parameters.");
        }

        /// <summary>
        /// Fills an imperial reference table. The design stays metric; the inch values, diametral pitch and circular pitch are for reference.
        /// </summary>
        private static void FillImperialTable(DataGridView grid, Label title, ImperialTable table, string emptyText)
        {
            grid.SuspendLayout();
            try
            {
                grid.Rows.Clear();
                title.Text = table.Rows.Count > 0 ? table.Title + " - imperial reference (metric values remain the design values)" : emptyText;
                foreach (var row in table.Rows)
                {
                    grid.Rows.Add(
                        row.Name,
                        row.Symbol,
                        row.Formula,
                        row.MetricValue.ToString("0.#######", CultureInfo.InvariantCulture),
                        row.MetricUnit,
                        row.ImperialValue.ToString("0.#######", CultureInfo.InvariantCulture),
                        row.ImperialUnit);
                }
            }
            finally
            {
                grid.ResumeLayout();
            }
        }

        private void FillParameterTable(DataGridView grid, Label title, GearParameterTable table, string emptyText)
        {
            grid.SuspendLayout();
            try
            {
                grid.Rows.Clear();
                title.Text = table.Rows.Count > 0 ? table.Title : emptyText;
                foreach (var row in table.Rows)
                {
                    var index = grid.Rows.Add(
                        row.ParameterName,
                        GetLatexImage(row.SymbolLatex),
                        GetLatexImage(row.FormulaLatex),
                        row.Value.ToString("0.#######", CultureInfo.InvariantCulture),
                        row.Unit);
                    grid.Rows[index].Cells["colSymbol"].ToolTipText = row.SymbolLatex;
                    grid.Rows[index].Cells["colFormula"].ToolTipText = row.FormulaLatex;
                }
            }
            finally
            {
                grid.ResumeLayout();
            }
        }

        private Image GetLatexImage(string latex)
        {
            if (string.IsNullOrWhiteSpace(latex)) return null;
            if (_latexImageCache.TryGetValue(latex, out var cached)) return cached;

            try
            {
                if (LaTeXUtils.TryParse(latex, out _))
                {
                    var image = LaTeXUtils.CreateImageFromLatex(latex);
                    if (image != null) _latexImageCache[latex] = image;
                    return image;
                }
            }
            catch
            {
                // A formula the renderer cannot handle is shown as a blank cell; the tooltip still has the source.
            }

            return null;
        }

        private TreeNode CreateNode(TrainTreeNode model)
        {
            var node = new TreeNode(model.Text) { Tag = model, Checked = model.Kind != TrainNodeKind.Gear || model.Visible };
            foreach (var child in model.Children) node.Nodes.Add(CreateNode(child));

            // A shaft or the assembly is shown as checked only while all of its gears are visible.
            if (model.Kind != TrainNodeKind.Gear) node.Checked = node.Nodes.Cast<TreeNode>().All(n => n.Checked);
            return node;
        }

        private void SelectNode(object tag)
        {
            if (tag == null)
            {
                _tree.SelectedNode = null;
                return;
            }

            var match = Flatten(_tree.Nodes).FirstOrDefault(n => ReferenceEquals((n.Tag as TrainTreeNode)?.Tag, tag));
            if (match != null) _tree.SelectedNode = match;
        }

        private static IEnumerable<TreeNode> Flatten(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                yield return node;
                foreach (var child in Flatten(node.Nodes)) yield return child;
            }
        }

        /// <summary>
        /// Shows a modal message with the copyright statement and version number from the assembly information.
        /// </summary>
        private void ShowAbout()
        {
            var assembly = typeof(GearTrainForm).Assembly;
            var title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "Gear Train";
            var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;
            var version = assembly.GetName().Version;
            MessageBox.Show(this, $"{title}\nVersion {version}\n\n{copyright}", "About Gear Train", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Opens a document that ships with the program (looked up next to the assembly, then in the parent folders).
        /// </summary>
        private void OpenDocument(string fileName, bool useNotepad)
        {
            var path = FindDocument(fileName);
            if (path == null)
            {
                MessageBox.Show(this, $"The file {fileName} was not found.", "Gear Train", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (useNotepad) Process.Start("notepad.exe", "\"" + path + "\"");
                else Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Win32Exception)
            {
                try
                {
                    Process.Start("notepad.exe", "\"" + path + "\"");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Could not open {path}.\n{ex.Message}", "Gear Train", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private static string FindDocument(string fileName)
        {
            var folder = Path.GetDirectoryName(typeof(GearTrainForm).Assembly.Location);
            while (!string.IsNullOrEmpty(folder))
            {
                var candidate = Path.Combine(folder, fileName);
                if (File.Exists(candidate)) return candidate;
                folder = Path.GetDirectoryName(folder);
            }

            return null;
        }

        private void RenderScene()
        {
            _viewport.SetScene(_scene, _selected, _hidden, _btnWireframe.Checked, _btnShafts.Checked);
            _view2D.SetScene(_scene, _selected, _hidden, _btnShafts.Checked);
        }
    }
}
