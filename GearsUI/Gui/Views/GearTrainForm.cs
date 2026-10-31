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
            var btnFit = new ToolStripButton("Fit view");
            btnNew.Click += (s, e) => _presenter.NewTrain();
            btnAddGear.Click += (s, e) => RunCommand(AddGearToSelectedShaft);
            btnAddMeshed.Click += (s, e) => RunCommand(AddMeshedToSelectedGear);
            btnRemove.Click += (s, e) => _presenter.RemoveSelected();
            btnFit.Click += (s, e) => _viewport.FitView();
            _btnWireframe.Click += (s, e) => RenderScene();
            _btnShafts.Click += (s, e) => RenderScene();

            toolbar.Items.AddRange(new ToolStripItem[]
            {
                btnNew, btnAddGear, btnAddMeshed, btnRemove, new ToolStripSeparator(),
                new ToolStripLabel("Input speed (rpm)"), new ToolStripControlHost(_numSpeed),
                new ToolStripLabel("Input angle (deg)"), new ToolStripControlHost(_numAngle), new ToolStripSeparator(),
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
            _numAngle.ValueChanged += (s, e) => _presenter.SetInputAngle((double)_numAngle.Value);
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
