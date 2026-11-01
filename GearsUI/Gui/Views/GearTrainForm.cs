using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Bolsover.Gears.Gui.Presenters;
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
        private readonly DataGridView _gearsGrid = CreateGrid();
        private readonly DataGridView _shaftsGrid = CreateGrid();
        private readonly DataGridView _stagesGrid = CreateGrid();
        private readonly TextBox _summary = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font("Consolas", 9f)
        };

        private readonly ToolStripLabel _status = new ToolStripLabel("Ready");
        private readonly NumericUpDown _numSpeed = new NumericUpDown { Minimum = -100000, Maximum = 100000, DecimalPlaces = 1, Value = 1000, Width = 80 };
        private readonly NumericUpDown _numAngle = new NumericUpDown { Minimum = -360, Maximum = 360, DecimalPlaces = 1, Value = 0, Width = 70 };
        private readonly ToolStripButton _btnWireframe = new ToolStripButton("Wireframe") { CheckOnClick = true };
        private readonly ToolStripButton _btnShafts = new ToolStripButton("Shafts") { CheckOnClick = true, Checked = true };
        private readonly ToolStripButton _btnAnimate = new ToolStripButton("Animate") { CheckOnClick = true };
        private readonly NumericUpDown _numAnimSpeed = new NumericUpDown { Minimum = -600, Maximum = 600, DecimalPlaces = 1, Value = 30, Width = 70 };
        private readonly Timer _animationTimer = new Timer { Interval = 60 };

        private bool _syncingAngle;
        private bool _updating;
        private AssemblyScene _scene;
        private object _selected;
        private ISet<GearComponent> _hidden;

        public GearTrainForm()
        {
            Text = "Gear Train";
            Size = new Size(1400, 860);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9f);

            _host.Child = _viewport;
            _presenter = new GearTrainPresenter(this);
            BuildLayout();
            WireEvents();
        }

        public GearTrainPresenter Presenter => _presenter;

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

        private void BuildLayout()
        {
            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            var btnNew = new ToolStripButton("New train");
            var btnAddGear = new ToolStripButton("Add gear to shaft");
            var btnAddMeshed = new ToolStripButton("Add meshed gear");
            var btnRemove = new ToolStripButton("Remove");
            var btnFitShift = new ToolStripButton("Fit profile shift")
            {
                ToolTipText = "Give the selected driven gear the profile shift that makes its mesh run at the current centre distance."
            };
            var btnFit = new ToolStripButton("Fit view");

            var ddStart = new ToolStripDropDownButton("Start with");
            ddStart.DropDownItems.Add("Spur gear", null, (s, e) => RunCommand(() => _presenter.NewTrain(GearFamily.Spur)));
            ddStart.DropDownItems.Add("Bevel gear", null, (s, e) => RunCommand(() => _presenter.NewTrain(GearFamily.Bevel)));
            ddStart.DropDownItems.Add("Worm", null, (s, e) => RunCommand(() => _presenter.NewTrain(GearFamily.Worm)));
            ddStart.DropDownItems.Add("Planetary stage", null, (s, e) => RunCommand(() => _presenter.NewPlanetary()));

            var ddMate = new ToolStripDropDownButton("Add mate");
            ddMate.DropDownItems.Add("Rack for the selected pinion", null, (s, e) => RunCommand(() => AddMateToSelectedGear(MeshType.Rack)));
            ddMate.DropDownItems.Add("Bevel gear for the selected bevel gear", null, (s, e) => RunCommand(() => AddMateToSelectedGear(MeshType.Bevel)));
            ddMate.DropDownItems.Add("Worm wheel for the selected worm", null, (s, e) => RunCommand(() => AddMateToSelectedGear(MeshType.Worm)));

            var ddFile = new ToolStripDropDownButton("File");
            ddFile.DropDownItems.Add("Save gear train…", null, (s, e) => SaveTrain());
            ddFile.DropDownItems.Add("Load gear train…", null, (s, e) => LoadTrain());
            ddFile.DropDownItems.Add(new ToolStripSeparator());
            ddFile.DropDownItems.Add("Export STL…", null, (s, e) => ExportScene(false));
            ddFile.DropDownItems.Add("Export OBJ…", null, (s, e) => ExportScene(true));

            btnNew.Click += (s, e) => _presenter.NewTrain();
            btnAddGear.Click += (s, e) => RunCommand(AddGearToSelectedShaft);
            btnAddMeshed.Click += (s, e) => RunCommand(AddMeshedToSelectedGear);
            btnRemove.Click += (s, e) => _presenter.RemoveSelected();
            btnFitShift.Click += (s, e) => RunCommand(FitProfileShiftOfSelectedGear);
            btnFit.Click += (s, e) => _viewport.FitView();
            _btnWireframe.Click += (s, e) => RenderScene();
            _btnShafts.Click += (s, e) => RenderScene();

            toolbar.LayoutStyle = ToolStripLayoutStyle.Flow;
            toolbar.Items.AddRange(new ToolStripItem[]
            {
                btnNew, ddStart, btnAddGear, btnAddMeshed, ddMate, btnFitShift, btnRemove, ddFile, new ToolStripSeparator(),
                new ToolStripLabel("Input speed (rpm)"), new ToolStripControlHost(_numSpeed),
                new ToolStripLabel("Input angle (deg)"), new ToolStripControlHost(_numAngle), new ToolStripSeparator(),
                _btnAnimate, new ToolStripLabel("Animation (rpm)"), new ToolStripControlHost(_numAnimSpeed), new ToolStripSeparator(),
                _btnWireframe, _btnShafts, btnFit, new ToolStripSeparator(), _status
            });

            var leftSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            leftSplit.Panel1.Controls.Add(_tree);
            leftSplit.Panel2.Controls.Add(_grid);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            AddTab(tabs, "Summary", _summary);
            AddTab(tabs, "Gears", _gearsGrid);
            AddTab(tabs, "Shafts", _shaftsGrid);
            AddTab(tabs, "Stages", _stagesGrid);

            var rightSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            rightSplit.Panel1.Controls.Add(_host);
            rightSplit.Panel2.Controls.Add(tabs);

            var mainSplit = new SplitContainer { Dock = DockStyle.Fill };
            mainSplit.Panel1.Controls.Add(leftSplit);
            mainSplit.Panel2.Controls.Add(rightSplit);

            Controls.Add(mainSplit);
            Controls.Add(toolbar);

            Load += (s, e) =>
            {
                SetSplitter(mainSplit, 360);
                SetSplitter(leftSplit, leftSplit.Height / 2);
                SetSplitter(rightSplit, (int)(rightSplit.Height * 0.62));
                _presenter.NewTrain();
            };
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
                return;
            }

            _summary.Text = report.ToText();
            _gearsGrid.DataSource = report.Gears.ToList();
            _shaftsGrid.DataSource = report.Shafts.ToList();
            _stagesGrid.DataSource = report.Stages.ToList();
        }

        public void ShowMessage(string message, bool isError)
        {
            _status.Text = message;
            _status.ForeColor = isError ? Color.Firebrick : SystemColors.ControlText;
        }

        #endregion

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

        private void RenderScene()
        {
            _viewport.SetScene(_scene, _selected, _hidden, _btnWireframe.Checked, _btnShafts.Checked);
        }
    }
}
