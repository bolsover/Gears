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
        private readonly ParameterTableFiller _tableFiller = new ParameterTableFiller();
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
        private readonly ToolStripMenuItem _btnExportSelected = new ToolStripMenuItem("Export &selected to Alibre…")         {             Enabled = false,             ToolTipText = "Create the selected parts in Alibre Design. One selected part (an involute gear, rack, worm, worm wheel, bevel gear, cycloidal gear, HTD pulley, timing belt, shaft or planetary carrier) is exported directly as a new Alibre part. With several parts selected you choose between separate new parts and one new assembly that holds only the selected gears, shafts, carriers and belts at their train positions. Click parts in the 3D or 2D view to select several. Available when the editor runs inside Alibre Design."         };         private readonly ToolStripMenuItem _btnExportAll = new ToolStripMenuItem("Export &all to Alibre…")         {             Enabled = false,             ToolTipText = "Create a new Alibre assembly from the whole gear train: every gear, every shaft (a shaft that only carries a rack is skipped), every planetary carrier and every timing belt. Each one is built as a new part and placed at its train position. The assembly needs at least two parts. Available when the editor runs inside Alibre Design."         };
        private readonly NumericUpDown _numAnimSpeed = new NumericUpDown { Minimum = -600, Maximum = 600, DecimalPlaces = 1, Value = 30, Width = 70 };
        private readonly Timer _animationTimer = new Timer { Interval = 60 };

        private bool _syncingAngle;
        private bool _updating;
        private AssemblyScene _scene;
        private object _selected;
        private IReadOnlyCollection<object> _selectedItems = new object[0];
        private ISet<GearComponent> _hidden;
        private AlibreGearExporter _alibreExporter;
        private readonly AlibreExportController _alibreExport;
        private PropertyLayoutSettings _layout = PropertyLayoutStore.Load(PropertyLayoutStore.DefaultPath);
        private object _gridSource;
        private ToolStripMenuItem[] _sortItems;

        /// <summary>
        /// Uses the gears icon that is built into GearsUI.exe as the window icon. This also works when the editor is hosted in Alibre Design,
        /// where the process executable is not ours. A missing icon leaves the default one.
        /// </summary>
        private void LoadApplicationIcon()
        {
            try
            {
                var icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
                if (icon != null) Icon = icon;
            }
            catch (Exception)
            {
                // The default window icon is used.
            }
        }

        public GearTrainForm()
        {
            Text = "Gears";
            Size = new Size(1400, 860);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f);
            LoadApplicationIcon();

            _host.Child = _viewport;
            _presenter = new GearTrainPresenter(this);
            _alibreExport = new AlibreExportController(this, _presenter, () => _alibreExporter, () => _selectedItems, ShowMessage);
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
                _btnExportSelected.Enabled = value != null;
                _btnExportAll.Enabled = value != null;
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
            var btnFit = new ToolStripMenuItem("&Fit view");

            // A ToolStripMenuItem can only have one parent, so each menu gets its own Remove item.
            Func<ToolStripMenuItem> createRemoveItem = () => new ToolStripMenuItem("&Remove selected", null, (s, e) => RunCommand(_presenter.RemoveSelected))
            {
                ToolTipText = "Remove the last selected part together with everything that hangs off it. When several parts are selected only the last one is removed."
            };

            var ddFile = CreateFileMenu(btnNew);
            var menuSpur = CreateSpurMenu(createRemoveItem);
            var menuBevel = CreateBevelMenu(createRemoveItem);
            var menuWorm = CreateWormMenu(createRemoveItem);
            var menuPlanetary = CreatePlanetaryMenu();
            var menuPulley = CreatePulleyMenu();
            var menuSprocket = CreateSprocketMenu();
            var menuCycloidal = CreateCycloidalMenu();
            var menuView = CreateViewMenu(btnFit);
            var menuSimulation = CreateSimulationMenu();

            WireMenuEvents(btnNew, btnFit);

            var menuHelp = CreateHelpMenu();

            menu.Items.AddRange(new ToolStripItem[] { ddFile, menuSpur, menuBevel, menuWorm, menuPlanetary, menuPulley, menuSprocket, menuCycloidal, menuView, menuSimulation, menuHelp });
            MainMenuStrip = menu;

            var statusBar = new StatusStrip();
            statusBar.Items.Add(_status);

            var leftSplit = CreateLeftSplit();

            var tabs = CreateReportTabs();

            var rightSplit = CreateRightSplit(tabs);

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

        private ToolStripMenuItem CreateFileMenu(ToolStripMenuItem btnNew)
        {
            var ddFile = new ToolStripMenuItem("&File");
            ddFile.DropDownItems.Add(btnNew);
            ddFile.DropDownItems.Add(new ToolStripSeparator());
            ddFile.DropDownItems.Add("Save gear train…", null, (s, e) => SaveTrain());
            ddFile.DropDownItems.Add("Load gear train…", null, (s, e) => LoadTrain());
            ddFile.DropDownItems.Add(new ToolStripSeparator());
            ddFile.DropDownItems.Add("Export STL…", null, (s, e) => ExportScene(false));
            ddFile.DropDownItems.Add("Export OBJ…", null, (s, e) => ExportScene(true));
            ddFile.DropDownItems.Add(new ToolStripSeparator());
            ddFile.DropDownItems.Add(_btnExportSelected);
            ddFile.DropDownItems.Add(_btnExportAll);
            ddFile.DropDownItems.Add(new ToolStripSeparator());
            ddFile.DropDownItems.Add("E&xit", null, (s, e) => Close());

            return ddFile;
        }

        private ToolStripMenuItem CreateSpurMenu(Func<ToolStripMenuItem> createRemoveItem)
        {
            var menuSpur = new ToolStripMenuItem("Sp&ur");
            menuSpur.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("&Straight", null, (s, e) => RunCommand(() => _presenter.NewTrain(GearFamily.Spur)))
                {
                    ToolTipText = "Start a new train with a straight (non-helical) spur gear."
                },
                new ToolStripMenuItem("&Helical", null, (s, e) => RunCommand(() => _presenter.NewTrain(GearFamily.Helical)))
                {
                    ToolTipText = "Start a new train with a helical spur gear (15 degree right-hand helix by default)."
                },
                new ToolStripSeparator(),
                new ToolStripMenuItem("Add &gear to shaft", null, (s, e) => RunCommand(AddGearToSelectedShaft)),
                new ToolStripMenuItem("Add &meshed gear", null, (s, e) => RunCommand(AddMeshedToSelectedGear)),
                new ToolStripMenuItem("Add rac&k for the selected pinion", null, (s, e) => RunCommand(() => AddMateToSelectedGear(MeshType.Rack))),
                new ToolStripMenuItem("Add helical gear on crosse&d shafts for the selected gear", null, (s, e) => RunCommand(() => AddMateToSelectedGear(MeshType.CrossedHelical)))
                {
                    ToolTipText = "Add a helical gear whose shaft is not parallel to the shaft of the selected helical gear. It gets the same hand and the helix angle that puts the shafts at 90 degrees; change the helix angles and the shaft angle in the properties."
                },
                new ToolStripSeparator(),
                new ToolStripMenuItem("Fit &profile shift", null, (s, e) => RunCommand(FitProfileShiftOfSelectedGear))
                {
                    ToolTipText = "Give the selected driven gear the profile shift that makes its mesh run at the current centre distance."
                },
                createRemoveItem()
            });

            return menuSpur;
        }

        private ToolStripMenuItem CreateBevelMenu(Func<ToolStripMenuItem> createRemoveItem)
        {
            var menuBevel = new ToolStripMenuItem("&Bevel");
            var ddBevelStraight = new ToolStripMenuItem("&Straight");
            ddBevelStraight.DropDownItems.Add("&Standard", null, (s, e) => RunCommand(() => _presenter.NewTrain(GearFamily.Bevel, BevelGearType.Standard)));
            ddBevelStraight.DropDownItems.Add("&Gleason", null, (s, e) => RunCommand(() => _presenter.NewTrain(GearFamily.Bevel, BevelGearType.Gleason)));
            menuBevel.DropDownItems.AddRange(new ToolStripItem[]
            {
                ddBevelStraight,
                new ToolStripMenuItem("Spir&al", null, (s, e) => RunCommand(() => _presenter.NewTrain(GearFamily.SpiralBevel))),
                new ToolStripMenuItem("&Zerol", null, (s, e) => RunCommand(() => _presenter.NewTrain(GearFamily.ZerolBevel))),
                new ToolStripMenuItem("&Hypoid", null, (s, e) => RunCommand(() => _presenter.NewTrain(GearFamily.Hypoid))),
                new ToolStripSeparator(),
                new ToolStripMenuItem("Add &meshed bevel gear", null, (s, e) => RunCommand(() => AddMateToSelectedGear(MeshType.Bevel)))
                {
                    ToolTipText = "Add the mate of the selected straight, spiral or zerol bevel gear. The mate of a spiral or zerol gear has the opposite hand and the same spiral angle and cutter radius."
                },
                new ToolStripMenuItem("Add hypoid gear or &pinion", null, (s, e) => RunCommand(() => AddMateToSelectedGear(MeshType.Hypoid)))
                {
                    ToolTipText = "Add the gear for a hypoid pinion, or the pinion for a hypoid gear, on a shaft offset from the selected one. Change the offset in the properties; its sign sets the hands."
                },
                new ToolStripSeparator(),
                createRemoveItem()
            });

            return menuBevel;
        }

        private ToolStripMenuItem CreateWormMenu(Func<ToolStripMenuItem> createRemoveItem)
        {
            var menuWorm = new ToolStripMenuItem("&Worm");
            menuWorm.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("&New worm", null, (s, e) => RunCommand(() => _presenter.NewTrain(GearFamily.Worm))),
                new ToolStripSeparator(),
                new ToolStripMenuItem("Add worm &wheel for the selected worm", null, (s, e) => RunCommand(() => AddMateToSelectedGear(MeshType.Worm))),
                createRemoveItem()
            });

            return menuWorm;
        }

        private ToolStripMenuItem CreatePlanetaryMenu()
        {
            var menuPlanetary = new ToolStripMenuItem("Pl&anetary");
            menuPlanetary.DropDownItems.Add(new ToolStripMenuItem("&New planetary stage", null, (s, e) => RunCommand(() => _presenter.NewPlanetary()))
            {
                ToolTipText = "Start a new train with a planetary stage (sun, planets and ring). A planetary stage cannot be edited gear by gear."
            });

            return menuPlanetary;
        }

        private ToolStripMenuItem CreatePulleyMenu()
        {
            var menuPulley = new ToolStripMenuItem("&Pulley");
            var ddPulleyStart = new ToolStripMenuItem("&New train with HTD pulley")
            {
                ToolTipText = "Start a new train with one HTD timing belt pulley of the chosen series. It is drawn in the 2D and 3D views."
            };
            var ddPulleyAdd = new ToolStripMenuItem("&Add HTD pulley to shaft")
            {
                ToolTipText = "Add an HTD pulley of the chosen series to the selected shaft (or the shaft of the selected gear)."
            };
            foreach (var profile in HtdProfile.Profiles)
            {
                var series = profile.Series;
                ddPulleyStart.DropDownItems.Add("HTD " + profile.Name + " (" + profile.Pitch.ToString("0", CultureInfo.InvariantCulture) + " mm pitch)", null,
                    (s, e) => RunCommand(() => _presenter.NewPulley(series)));
                ddPulleyAdd.DropDownItems.Add("HTD " + profile.Name + " (" + profile.Pitch.ToString("0", CultureInfo.InvariantCulture) + " mm pitch)", null,
                    (s, e) => RunCommand(() => AddPulleyToSelectedShaft(series)));
            }

            menuPulley.DropDownItems.AddRange(new ToolStripItem[]
            {
                ddPulleyStart, ddPulleyAdd, new ToolStripSeparator(),
                new ToolStripMenuItem("Toggle &flanges of selected pulley", null, (s, e) => RunCommand(ToggleFlangesOfSelectedPulley)),
                new ToolStripSeparator(),
                new ToolStripMenuItem("Add &belted pulley to selected pulley", null, (s, e) => RunCommand(AddBeltedPulleyToSelectedPulley))
                {
                    ToolTipText = "Adds a second pulley of the same series on a new shaft, joined to the selected pulley by a belt. Select the belt to set the centre distance."
                },
                new ToolStripMenuItem("&Remove selected belt", null, (s, e) => RunCommand(RemoveSelectedBelt)),
                new ToolStripSeparator(),
                new ToolStripMenuItem("HTD profile &data…", null, (s, e) => ShowHtdData())
            });

            return menuPulley;
        }

        private ToolStripMenuItem CreateSprocketMenu()
        {
            var menuSprocket = new ToolStripMenuItem("Spr&ocket");
            var ddSprocketStart = new ToolStripMenuItem("&New train with sprocket")
            {
                ToolTipText = "Start a new train with one ISO 606 (DIN 8187) B-series roller chain sprocket of the chosen chain. It is drawn in the 2D and 3D views."
            };
            var ddSprocketAdd = new ToolStripMenuItem("&Add sprocket to shaft")
            {
                ToolTipText = "Add a sprocket of the chosen chain to the selected shaft (or the shaft of the selected gear)."
            };
            foreach (var profile in ChainProfile.Profiles)
            {
                var series = profile.Series;
                var caption = "ISO 606 " + profile.Name + "-1 (" + profile.Pitch.ToString("0.###", CultureInfo.InvariantCulture) + " mm pitch)";
                ddSprocketStart.DropDownItems.Add(caption, null, (s, e) => RunCommand(() => _presenter.NewSprocket(series)));
                ddSprocketAdd.DropDownItems.Add(caption, null, (s, e) => RunCommand(() => AddSprocketToSelectedShaft(series)));
            }

            menuSprocket.DropDownItems.AddRange(new ToolStripItem[]
            {
                ddSprocketStart, ddSprocketAdd, new ToolStripSeparator(),
                new ToolStripMenuItem("Add &chained sprocket to selected sprocket", null, (s, e) => RunCommand(AddChainedSprocketToSelectedSprocket))
                {
                    ToolTipText = "Adds a second sprocket of the same chain on a new shaft, joined to the selected sprocket by a roller chain. Select the chain to set the centre distance."
                },
                new ToolStripMenuItem("&Remove selected chain", null, (s, e) => RunCommand(RemoveSelectedChain)),
                new ToolStripSeparator(),
                new ToolStripMenuItem("ISO 606 chain &data…", null, (s, e) => ShowChainData())
            });

            return menuSprocket;
        }

        private ToolStripMenuItem CreateCycloidalMenu()
        {
            var menuCycloidal = new ToolStripMenuItem("&Cycloidal");
            menuCycloidal.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("&New cycloidal pair (wheel and pinion)", null, (s, e) => RunCommand(() => _presenter.NewCycloidalPair()))
                {
                    ToolTipText = "Start a new train with a cycloidal wheel and the pinion that meshes with it. Cycloidal gears are always designed as a pair, so both are drawn together in the 2D and 3D views."
                },
                new ToolStripSeparator(),
                new ToolStripMenuItem("Add &meshed cycloidal gear to selected gear", null, (s, e) => RunCommand(AddCycloidalMateToSelectedGear))
                {
                    ToolTipText = "Add a gear that meshes with the selected cycloidal gear on a new shaft: a pinion for a wheel, a wheel for a pinion. Use it repeatedly to add further gears to the train."
                },
                new ToolStripMenuItem("Add cycloidal gear to &shaft", null, (s, e) => RunCommand(AddCycloidalGearToSelectedShaft))
                {
                    ToolTipText = "Add a compound cycloidal gear next to the gears of the selected shaft (or the shaft of the selected gear): the opposite kind to the last cycloidal gear on the shaft."
                },
                new ToolStripMenuItem("&Remove selected", null, (s, e) => RunCommand(_presenter.RemoveSelected))
            });

            return menuCycloidal;
        }

        private ToolStripMenuItem CreateViewMenu(ToolStripMenuItem btnFit)
        {
            var menuView = new ToolStripMenuItem("&View");
            menuView.DropDownItems.AddRange(new ToolStripItem[]
            {
                _btnWireframe, _btnShafts, _btnPitchCircles, _btnLabels, new ToolStripSeparator(), btnFit
            });

            return menuView;
        }

        private ToolStripMenuItem CreateSimulationMenu()
        {
            var menuSimulation = new ToolStripMenuItem("&Simulation");
            menuSimulation.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripLabel("Input speed (rpm)"), new ToolStripControlHost(_numSpeed),
                new ToolStripLabel("Input angle (deg)"), new ToolStripControlHost(_numAngle), new ToolStripSeparator(),
                _btnAnimate, new ToolStripLabel("Animation (rpm)"), new ToolStripControlHost(_numAnimSpeed)
            });

            return menuSimulation;
        }

        private void WireMenuEvents(ToolStripMenuItem btnNew, ToolStripMenuItem btnFit)
        {
            _btnExportSelected.Click += (s, e) => RunCommand(ExportSelectedToAlibre);
            _btnExportAll.Click += (s, e) => RunCommand(ExportAllToAlibre);
            btnNew.Click += (s, e) => RunCommand(() => _presenter.NewTrain());
            btnFit.Click += (s, e) =>
            {
                _viewport.FitView();
                _view2D.FitView();
            };
            _btnPitchCircles.Click += (s, e) => _view2D.ShowPitchCircles = _btnPitchCircles.Checked;
            _btnLabels.Click += (s, e) => _view2D.ShowLabels = _btnLabels.Checked;
            _btnWireframe.Click += (s, e) => RenderScene();
            _btnShafts.Click += (s, e) => RenderScene();
        }

        private ToolStripMenuItem CreateHelpMenu()
        {
            var menuHelp = new ToolStripMenuItem("&Help");
            menuHelp.DropDownItems.Add("&About", null, (s, e) => ShowAbout());
            menuHelp.DropDownItems.Add("&Help", null, (s, e) => ShowDocument("Help", "README.html"));
            menuHelp.DropDownItems.Add("&License", null, (s, e) => ShowDocument("License", "LICENSE.html"));

            return menuHelp;
        }

        private SplitContainer CreateLeftSplit()
        {
            var leftSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            leftSplit.Panel1.Controls.Add(_tree);
            leftSplit.Panel2.Controls.Add(_grid);
            leftSplit.Panel2.Controls.Add(CreatePropertyBar());

            return leftSplit;
        }

        private TabControl CreateReportTabs()
        {
            var tabs = new TabControl { Dock = DockStyle.Fill };
            AddTab(tabs, "Summary", _summary);
            AddTab(tabs, "Gears", _gearsGrid);
            AddTab(tabs, "Shafts", _shaftsGrid);
            AddTab(tabs, "Stages", _stagesGrid);
            AddTab(tabs, "Gear parameters", CreateParameterPanel(_gearParamsTitle, _gearParamsGrid));
            AddTab(tabs, "Mesh parameters", CreateParameterPanel(_meshParamsTitle, _meshParamsGrid));
            AddTab(tabs, "Imperial reference", CreateImperialPanel());

            return tabs;
        }

        private SplitContainer CreateRightSplit(TabControl tabs)
        {
            var rightSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            var viewTabs = new TabControl { Dock = DockStyle.Fill };
            AddTab(viewTabs, "3D view", _host);
            AddTab(viewTabs, "2D view", _view2D);
            rightSplit.Panel1.Controls.Add(viewTabs);
            rightSplit.Panel2.Controls.Add(tabs);

            return rightSplit;
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

        /// <summary>The bar above the property grid: sort mode, the property chooser and saving the layout as the default.</summary>
        private ToolStrip CreatePropertyBar()
        {
            var sort = new ToolStripDropDownButton("&Sort") { ToolTipText = "How the property list is ordered." };
            _sortItems = new[]
            {
                new ToolStripMenuItem("By category (A-Z)") { Tag = PropertySortMode.Category },
                new ToolStripMenuItem("Alphabetical (one list)") { Tag = PropertySortMode.Alphabetical },
                new ToolStripMenuItem("Custom order") { Tag = PropertySortMode.Custom, ToolTipText = "The order chosen in Choose properties." }
            };
            foreach (var item in _sortItems)
            {
                var mode = (PropertySortMode)item.Tag;
                item.Click += (s, e) =>
                {
                    _layout.SortMode = mode;
                    ApplyPropertyLayout();
                };
                sort.DropDownItems.Add(item);
            }

            var choose = new ToolStripButton("&Choose properties…")
            {
                ToolTipText = "Select which properties are shown, put them in order and add notes."
            };
            choose.Click += (s, e) => ChooseProperties();

            var save = new ToolStripButton("Save as &default")
            {
                ToolTipText = "Keep the current property layout as the default for the next start."
            };
            save.Click += (s, e) => SavePropertyLayout();

            var bar = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
            bar.Items.AddRange(new ToolStripItem[] { sort, choose, save });
            return bar;
        }

        /// <summary>Shows the selected item's properties in the grid, filtered and sorted as the layout says.</summary>
        private void ApplyPropertyLayout()
        {
            switch (_layout.SortMode)
            {
                case PropertySortMode.Alphabetical:
                    _grid.PropertySort = PropertySort.Alphabetical;
                    break;
                case PropertySortMode.Custom:
                    _grid.PropertySort = PropertySort.Categorized;
                    break;
                default:
                    _grid.PropertySort = PropertySort.CategorizedAlphabetical;
                    break;
            }

            if (_sortItems != null)
            {
                foreach (var item in _sortItems) item.Checked = (PropertySortMode)item.Tag == _layout.SortMode;
            }

            _grid.SelectedObject = _gridSource == null
                ? null
                : new FilteredProperties(_gridSource, _layout.Find(PropertyLayoutSettings.KeyOf(_gridSource)), _layout.SortMode);
        }

        private void ChooseProperties()
        {
            if (_gridSource == null)
            {
                ShowMessage("Select a shaft, gear or planetary stage first.", true);
                return;
            }

            using (var dialog = new PropertyLayoutDialog(_gridSource, _layout))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                _layout = dialog.Result;
                ApplyPropertyLayout();
                if (dialog.SaveAsDefault) SavePropertyLayout();
            }
        }

        private void SavePropertyLayout()
        {
            try
            {
                PropertyLayoutStore.Save(_layout, PropertyLayoutStore.DefaultPath);
                ShowMessage("Saved the property layout as the default: " + PropertyLayoutStore.DefaultPath, false);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                ShowMessage("The property layout could not be saved: " + ex.Message, true);
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
            _viewport.ItemPicked += tag => _presenter.ToggleSelection(tag);
            _view2D.ItemPicked += tag => _presenter.ToggleSelection(tag);

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
                _tableFiller.DisposeImages();
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

        /// <summary>
        /// Exports the selected parts: one part directly, several as separate Alibre parts or as one assembly, as the user chooses.
        /// </summary>
        private void ExportSelectedToAlibre() => _alibreExport.ExportSelectedToAlibre();

        /// <summary>Exports every gear, shaft, carrier and belt of the train to one new Alibre assembly.</summary>
        private void ExportAllToAlibre() => _alibreExport.ExportAllToAlibre();

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

        private void AddCycloidalMateToSelectedGear()
        {
            if (!(_selected is CycloidalGearComponent driver))
            {
                ShowMessage("Select the cycloidal gear that the new gear should mesh with.", true);
                return;
            }

            _presenter.AddCycloidalMate(driver);
        }

        private void AddCycloidalGearToSelectedShaft()
        {
            Shaft shaft = _selected as Shaft;
            if (shaft == null && _selected is GearComponent gear) _presenter.Assembly.FindMount(gear, out shaft);
            if (shaft == null) shaft = _presenter.InputShaft;
            if (shaft == null)
            {
                ShowMessage("Select a shaft first, or start a new cycloidal pair.", true);
                return;
            }

            _presenter.AddCycloidalGearToShaft(shaft);
        }

        private void AddPulleyToSelectedShaft(HtdSeries series)
        {
            Shaft shaft = _selected as Shaft;
            if (shaft == null && _selected is GearComponent gear) _presenter.Assembly.FindMount(gear, out shaft);
            if (shaft == null) shaft = _presenter.InputShaft;
            if (shaft == null)
            {
                ShowMessage("Select a shaft first, or start a new train with a pulley.", true);
                return;
            }

            _presenter.AddPulleyToShaft(shaft, series);
        }

        private void AddBeltedPulleyToSelectedPulley()
        {
            if (!(_selected is HtdPulleyComponent pulley))
            {
                ShowMessage("Select an HTD pulley first.", true);
                return;
            }

            _presenter.AddBeltedPulley(pulley);
        }

        private void RemoveSelectedBelt()
        {
            if (!(_selected is TimingBelt))
            {
                ShowMessage("Select a belt in the tree or in the views first.", true);
                return;
            }

            _presenter.RemoveSelected();
        }

        private void ToggleFlangesOfSelectedPulley()
        {
            if (!(_selected is HtdPulleyComponent pulley))
            {
                ShowMessage("Select an HTD pulley first.", true);
                return;
            }

            pulley.HasFlanges = !pulley.HasFlanges;
            _presenter.Refresh();
        }

        private void AddSprocketToSelectedShaft(ChainSeries series)
        {
            Shaft shaft = _selected as Shaft;
            if (shaft == null && _selected is GearComponent gear) _presenter.Assembly.FindMount(gear, out shaft);
            if (shaft == null) shaft = _presenter.InputShaft;
            if (shaft == null)
            {
                ShowMessage("Select a shaft first, or start a new train with a sprocket.", true);
                return;
            }

            _presenter.AddSprocketToShaft(shaft, series);
        }

        private void AddChainedSprocketToSelectedSprocket()
        {
            if (!(_selected is SprocketComponent sprocket))
            {
                ShowMessage("Select a sprocket first.", true);
                return;
            }

            _presenter.AddChainedSprocket(sprocket);
        }

        private void RemoveSelectedChain()
        {
            if (!(_selected is RollerChain))
            {
                ShowMessage("Select a chain in the tree or in the views first.", true);
                return;
            }

            _presenter.RemoveSelected();
        }

        /// <summary>Shows the ISO 606 chain dimensions and tooth form formulae that the sprockets are built from.</summary>
        private void ShowChainData()
        {
            var ci = CultureInfo.InvariantCulture;
            var text = new System.Text.StringBuilder();
            text.AppendLine("ISO 606 (DIN 8187) B-series simplex roller chains and sprockets, dimensions in mm");
            text.AppendLine();
            text.AppendLine("Chain    Pitch p   Roller d1   Inner width b1   Pin    Plate height   Tooth width");
            foreach (var p in ChainProfile.Profiles)
            {
                text.AppendLine(string.Format(ci, "{0,-8} {1,-9:0.###} {2,-11:0.##} {3,-16:0.##} {4,-6:0.##} {5,-14:0.##} {6:0.##}",
                    p.Name + "-1", p.Pitch, p.RollerDiameter, p.InnerWidth, p.PinDiameter, p.PlateHeight, p.ToothWidth));
            }

            text.AppendLine();
            text.AppendLine("Pitch diameter          d = p / sin(180 / z)");
            text.AppendLine("Root diameter           d_f = d - d1");
            text.AppendLine("Tip diameter            d_a = d + p (1 - 1.6 / z) - d1  (minimum)  to  d + 1.25 p - d1  (maximum)");
            text.AppendLine("Roller seating radius   r_i = 0.505 d1  (minimum)  to  0.505 d1 + 0.069 d1^(1/3)  (maximum)");
            text.AppendLine("Roller seating angle    alpha = 120 - 90 / z  (minimum)  to  140 - 90 / z  (maximum) degrees");
            text.AppendLine("Tooth flank radius      r_e = 0.12 d1 (z + 2)  (minimum)  to  0.008 d1 (z^2 + 180)  (maximum)");
            text.AppendLine("Tooth width             0.93 b1 for a pitch up to 12.7 mm, else 0.95 b1");
            text.AppendLine();
            text.AppendLine("The chain makers' pin, plate height and plate thicknesses are maximum values and only draw the chain.");
            MessageBox.Show(this, text.ToString(), "ISO 606 chain data", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>Shows the HTD dimensions that the pulleys are built from.</summary>
        private void ShowHtdData()
        {
            var ci = CultureInfo.InvariantCulture;
            var text = new System.Text.StringBuilder();
            text.AppendLine("HTD (High Torque Drive) timing belt pulleys, dimensions in mm");
            text.AppendLine();
            text.AppendLine("Series   Pitch   PLD      Groove depth   Head R   Foot R   Width   Min. grooves");
            foreach (var p in HtdProfile.Profiles)
            {
                text.AppendLine(string.Format(ci, "{0,-8} {1,-7:0.###} {2,-8:0.####} {3,-14:0.##} {4,-8:0.##} {5,-8:0.##} {6,-7:0.##} {7}",
                    p.Name, p.Pitch, p.PitchLineDifferential, p.ToothHeight, p.HeadRadius, p.FootRadius, p.ToothWidth, p.MinimumTeeth));
            }

            text.AppendLine();
            text.AppendLine("Pitch diameter     d = z p / pi");
            text.AppendLine("Outside diameter   d_o = d - 2 PLD   (PLD = pitch line differential)");
            text.AppendLine("Root diameter      d_r = d_o - 2 x groove depth");
            text.AppendLine();
            text.AppendLine("A groove is the negative of the belt tooth: a bottom arc of the head radius, flanks with a 14 degree included angle,");
            text.AppendLine("and an edge rounded with the foot radius. The 3M head radius, foot radius and width are approximate.");
            MessageBox.Show(this, text.ToString(), "HTD pulley data", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            _gridSource = properties;
            ApplyPropertyLayout();
        }

        public void ShowScene(AssemblyScene scene, IReadOnlyCollection<object> selectedItems, ISet<GearComponent> hiddenGears)
        {
            _scene = scene;
            _selectedItems = selectedItems ?? new object[0];
            _selected = _selectedItems.LastOrDefault();
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
                _tableFiller.FillParameterTable(_gearParamsGrid, _gearParamsTitle, GearParameterTable.Empty, "Select a gear to see its parameters, symbols and formulae.");
                _tableFiller.FillParameterTable(_meshParamsGrid, _meshParamsTitle, GearParameterTable.Empty, "Select a gear to see the parameters of its mesh.");
                ParameterTableFiller.FillImperialTable(_imperialGearGrid, _imperialGearTitle, ImperialTable.Empty, "Select a gear to see its module as diametral pitch and circular pitch, and its lengths in inches.");
                ParameterTableFiller.FillImperialTable(_imperialMeshGrid, _imperialMeshTitle, ImperialTable.Empty, "Select a gear to see the lengths of its mesh in inches.");
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

            _tableFiller.FillParameterTable(_gearParamsGrid, _gearParamsTitle, gearTable, _gearParamsTitle.Text);

            var relation = GearParameterTables.FindMeshFor(_presenter.Assembly, gear);
            var meshTable = GearParameterTables.ForMesh(relation);
            _tableFiller.FillParameterTable(_meshParamsGrid, _meshParamsTitle, meshTable,
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

            ParameterTableFiller.FillImperialTable(_imperialGearGrid, _imperialGearTitle, imperialGear, _imperialGearTitle.Text);
            ParameterTableFiller.FillImperialTable(_imperialMeshGrid, _imperialMeshTitle, ImperialReference.ForMesh(relation),
                relation == null ? $"{gear.Name} does not mesh with another gear." : "This mesh has no involute pair parameters.");
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
        /// Shows an HTML document that ships with the program (looked up next to the assembly, then in the parent folders)
        /// in a popup window.
        /// </summary>
        private void ShowDocument(string title, string fileName)
        {
            var path = FindDocument(fileName);
            if (path == null)
            {
                MessageBox.Show(this, $"The file {fileName} was not found.", "Gear Train", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var form = new HtmlDocumentForm(title, path))
                {
                    form.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not open {path}.\n{ex.Message}", "Gear Train", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            _viewport.SetScene(_scene, _selectedItems, _hidden, _btnWireframe.Checked, _btnShafts.Checked);
            _view2D.SetScene(_scene, _selectedItems, _hidden, _btnShafts.Checked);
        }
    }
}
