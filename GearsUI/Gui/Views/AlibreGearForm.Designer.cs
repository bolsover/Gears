using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Bolsover.Gears.Gui.Views
{
    partial class AlibreGearForm
    {
        private IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.menuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.menuExportDxf = new System.Windows.Forms.ToolStripMenuItem();
            this.menuExportCsv = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparatorFile = new System.Windows.Forms.ToolStripSeparator();
            this.menuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuGear = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAddMeshedGear = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRemoveMeshedGear = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparatorGear = new System.Windows.Forms.ToolStripSeparator();
            this.menuCalculate = new System.Windows.Forms.ToolStripMenuItem();
            this.menuView = new System.Windows.Forms.ToolStripMenuItem();
            this.menuView2D = new System.Windows.Forms.ToolStripMenuItem();
            this.menuView3DSolid = new System.Windows.Forms.ToolStripMenuItem();
            this.menuView3DWireframe = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparatorView1 = new System.Windows.Forms.ToolStripSeparator();
            this.menuShowReferenceCircles = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparatorView2 = new System.Windows.Forms.ToolStripSeparator();
            this.menuResetView = new System.Windows.Forms.ToolStripMenuItem();
            this.panelInputs = new System.Windows.Forms.TableLayoutPanel();
            this.lblTeeth = new System.Windows.Forms.Label();
            this.numTeeth = new System.Windows.Forms.NumericUpDown();
            this.lblModule = new System.Windows.Forms.Label();
            this.numModule = new System.Windows.Forms.NumericUpDown();
            this.lblPressureAngle = new System.Windows.Forms.Label();
            this.numPressureAngle = new System.Windows.Forms.NumericUpDown();
            this.lblHelixAngle = new System.Windows.Forms.Label();
            this.numHelixAngle = new System.Windows.Forms.NumericUpDown();
            this.lblHelicalDirection = new System.Windows.Forms.Label();
            this.cboHelicalDirection = new System.Windows.Forms.ComboBox();
            this.lblClearance = new System.Windows.Forms.Label();
            this.numClearance = new System.Windows.Forms.NumericUpDown();
            this.lblBacklash = new System.Windows.Forms.Label();
            this.numBacklash = new System.Windows.Forms.NumericUpDown();
            this.lblFaceWidth = new System.Windows.Forms.Label();
            this.numFaceWidth = new System.Windows.Forms.NumericUpDown();
            this.lblProfileShift = new System.Windows.Forms.Label();
            this.numProfileShift = new System.Windows.Forms.NumericUpDown();
            this.lblRimThickness = new System.Windows.Forms.Label();
            this.numRimThickness = new System.Windows.Forms.NumericUpDown();
            this.chkInternal = new System.Windows.Forms.CheckBox();
            this.chkOptimalShift = new System.Windows.Forms.CheckBox();
            this.chkFullGear = new System.Windows.Forms.CheckBox();
            this.btnCalculate = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblResultsHeader = new System.Windows.Forms.Label();
            this.lblPitch = new System.Windows.Forms.Label();
            this.lblBase = new System.Windows.Forms.Label();
            this.lblAddendum = new System.Windows.Forms.Label();
            this.lblDedendum = new System.Windows.Forms.Label();
            this.lblClearanceVal = new System.Windows.Forms.Label();
            this.lblBacklashVal = new System.Windows.Forms.Label();
            this.lblHelixVal = new System.Windows.Forms.Label();
            this.lblError = new System.Windows.Forms.Label();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.preview = new Bolsover.Gears.Gui.AlibreGearPreviewControl();
            this.panelTableContainer = new System.Windows.Forms.Panel();
            this.gridCalculations = new System.Windows.Forms.DataGridView();
            this.colParamName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSymbol = new System.Windows.Forms.DataGridViewImageColumn();
            this.colFormula = new System.Windows.Forms.DataGridViewImageColumn();
            this.colValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblTableTitle = new System.Windows.Forms.Label();
            this.menuStrip.SuspendLayout();
            this.panelInputs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTeeth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numModule)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPressureAngle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHelixAngle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numClearance)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBacklash)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFaceWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numProfileShift)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRimThickness)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            this.panelTableContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridCalculations)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.menuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFile,
            this.menuGear,
            this.menuView});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(1412, 30);
            this.menuStrip.TabIndex = 0;
            this.menuStrip.Text = "menuStrip";
            // 
            // menuFile
            // 
            this.menuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuExportDxf,
            this.menuExportCsv,
            this.toolStripSeparatorFile,
            this.menuExit});
            this.menuFile.Name = "menuFile";
            this.menuFile.Size = new System.Drawing.Size(46, 26);
            this.menuFile.Text = "&File";
            // 
            // menuExportDxf
            // 
            this.menuExportDxf.Name = "menuExportDxf";
            this.menuExportDxf.Size = new System.Drawing.Size(177, 26);
            this.menuExportDxf.Text = "Export &DXF…";
            // 
            // menuExportCsv
            // 
            this.menuExportCsv.Name = "menuExportCsv";
            this.menuExportCsv.Size = new System.Drawing.Size(177, 26);
            this.menuExportCsv.Text = "Export &CSV…";
            // 
            // toolStripSeparatorFile
            // 
            this.toolStripSeparatorFile.Name = "toolStripSeparatorFile";
            this.toolStripSeparatorFile.Size = new System.Drawing.Size(174, 6);
            // 
            // menuExit
            // 
            this.menuExit.Name = "menuExit";
            this.menuExit.Size = new System.Drawing.Size(177, 26);
            this.menuExit.Text = "E&xit";
            // 
            // menuGear
            // 
            this.menuGear.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuAddMeshedGear,
            this.menuRemoveMeshedGear,
            this.toolStripSeparatorGear,
            this.menuCalculate});
            this.menuGear.Name = "menuGear";
            this.menuGear.Size = new System.Drawing.Size(54, 26);
            this.menuGear.Text = "&Gear";
            // 
            // menuAddMeshedGear
            // 
            this.menuAddMeshedGear.Name = "menuAddMeshedGear";
            this.menuAddMeshedGear.Size = new System.Drawing.Size(251, 26);
            this.menuAddMeshedGear.Text = "&Add Meshed Gear…";
            // 
            // menuRemoveMeshedGear
            // 
            this.menuRemoveMeshedGear.Enabled = false;
            this.menuRemoveMeshedGear.Name = "menuRemoveMeshedGear";
            this.menuRemoveMeshedGear.Size = new System.Drawing.Size(251, 26);
            this.menuRemoveMeshedGear.Text = "&Remove Meshed Gear";
            // 
            // toolStripSeparatorGear
            // 
            this.toolStripSeparatorGear.Name = "toolStripSeparatorGear";
            this.toolStripSeparatorGear.Size = new System.Drawing.Size(248, 6);
            // 
            // menuCalculate
            // 
            this.menuCalculate.Name = "menuCalculate";
            this.menuCalculate.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.menuCalculate.Size = new System.Drawing.Size(251, 26);
            this.menuCalculate.Text = "&Execute Calculations";
            // 
            // menuView
            // 
            this.menuView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuView2D,
            this.menuView3DSolid,
            this.menuView3DWireframe,
            this.toolStripSeparatorView1,
            this.menuShowReferenceCircles,
            this.toolStripSeparatorView2,
            this.menuResetView});
            this.menuView.Name = "menuView";
            this.menuView.Size = new System.Drawing.Size(55, 26);
            this.menuView.Text = "&View";
            // 
            // menuView2D
            // 
            this.menuView2D.Checked = true;
            this.menuView2D.CheckState = System.Windows.Forms.CheckState.Checked;
            this.menuView2D.Name = "menuView2D";
            this.menuView2D.Size = new System.Drawing.Size(245, 26);
            this.menuView2D.Text = "&2D Profile";
            // 
            // menuView3DSolid
            // 
            this.menuView3DSolid.Name = "menuView3DSolid";
            this.menuView3DSolid.Size = new System.Drawing.Size(245, 26);
            this.menuView3DSolid.Text = "3D &Solid";
            // 
            // menuView3DWireframe
            // 
            this.menuView3DWireframe.Name = "menuView3DWireframe";
            this.menuView3DWireframe.Size = new System.Drawing.Size(245, 26);
            this.menuView3DWireframe.Text = "3D &Wireframe";
            // 
            // toolStripSeparatorView1
            // 
            this.toolStripSeparatorView1.Name = "toolStripSeparatorView1";
            this.toolStripSeparatorView1.Size = new System.Drawing.Size(242, 6);
            // 
            // menuShowReferenceCircles
            // 
            this.menuShowReferenceCircles.Checked = true;
            this.menuShowReferenceCircles.CheckOnClick = true;
            this.menuShowReferenceCircles.CheckState = System.Windows.Forms.CheckState.Checked;
            this.menuShowReferenceCircles.Name = "menuShowReferenceCircles";
            this.menuShowReferenceCircles.Size = new System.Drawing.Size(245, 26);
            this.menuShowReferenceCircles.Text = "Show &Reference Circles";
            // 
            // toolStripSeparatorView2
            // 
            this.toolStripSeparatorView2.Name = "toolStripSeparatorView2";
            this.toolStripSeparatorView2.Size = new System.Drawing.Size(242, 6);
            // 
            // menuResetView
            // 
            this.menuResetView.Name = "menuResetView";
            this.menuResetView.Size = new System.Drawing.Size(245, 26);
            this.menuResetView.Text = "&Reset View / Camera";
            // 
            // panelInputs
            // 
            this.panelInputs.AutoScroll = true;
            this.panelInputs.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelInputs.ColumnCount = 2;
            this.panelInputs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 56F));
            this.panelInputs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 44F));
            this.panelInputs.Controls.Add(this.lblTeeth, 0, 0);
            this.panelInputs.Controls.Add(this.numTeeth, 1, 0);
            this.panelInputs.Controls.Add(this.lblModule, 0, 1);
            this.panelInputs.Controls.Add(this.numModule, 1, 1);
            this.panelInputs.Controls.Add(this.lblPressureAngle, 0, 2);
            this.panelInputs.Controls.Add(this.numPressureAngle, 1, 2);
            this.panelInputs.Controls.Add(this.lblHelixAngle, 0, 3);
            this.panelInputs.Controls.Add(this.numHelixAngle, 1, 3);
            this.panelInputs.Controls.Add(this.lblHelicalDirection, 0, 4);
            this.panelInputs.Controls.Add(this.cboHelicalDirection, 1, 4);
            this.panelInputs.Controls.Add(this.lblClearance, 0, 5);
            this.panelInputs.Controls.Add(this.numClearance, 1, 5);
            this.panelInputs.Controls.Add(this.lblBacklash, 0, 6);
            this.panelInputs.Controls.Add(this.numBacklash, 1, 6);
            this.panelInputs.Controls.Add(this.lblFaceWidth, 0, 7);
            this.panelInputs.Controls.Add(this.numFaceWidth, 1, 7);
            this.panelInputs.Controls.Add(this.lblProfileShift, 0, 8);
            this.panelInputs.Controls.Add(this.numProfileShift, 1, 8);
            this.panelInputs.Controls.Add(this.lblRimThickness, 0, 9);
            this.panelInputs.Controls.Add(this.numRimThickness, 1, 9);
            this.panelInputs.Controls.Add(this.chkInternal, 0, 10);
            this.panelInputs.Controls.Add(this.chkOptimalShift, 0, 11);
            this.panelInputs.Controls.Add(this.chkFullGear, 0, 12);
            this.panelInputs.Controls.Add(this.btnCalculate, 0, 13);
            this.panelInputs.Controls.Add(this.progressBar, 0, 14);
            this.panelInputs.Controls.Add(this.lblStatus, 0, 15);
            this.panelInputs.Controls.Add(this.lblResultsHeader, 0, 16);
            this.panelInputs.Controls.Add(this.lblPitch, 0, 17);
            this.panelInputs.Controls.Add(this.lblBase, 0, 18);
            this.panelInputs.Controls.Add(this.lblAddendum, 0, 19);
            this.panelInputs.Controls.Add(this.lblDedendum, 0, 20);
            this.panelInputs.Controls.Add(this.lblClearanceVal, 0, 21);
            this.panelInputs.Controls.Add(this.lblBacklashVal, 0, 22);
            this.panelInputs.Controls.Add(this.lblHelixVal, 0, 23);
            this.panelInputs.Controls.Add(this.lblError, 0, 24);
            this.panelInputs.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelInputs.Location = new System.Drawing.Point(0, 30);
            this.panelInputs.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.panelInputs.Name = "panelInputs";
            this.panelInputs.Padding = new System.Windows.Forms.Padding(12, 10, 12, 10);
            this.panelInputs.RowCount = 25;
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelInputs.SetColumnSpan(this.chkInternal, 2);
            this.panelInputs.SetColumnSpan(this.chkOptimalShift, 2);
            this.panelInputs.SetColumnSpan(this.chkFullGear, 2);
            this.panelInputs.SetColumnSpan(this.btnCalculate, 2);
            this.panelInputs.SetColumnSpan(this.progressBar, 2);
            this.panelInputs.SetColumnSpan(this.lblStatus, 2);
            this.panelInputs.SetColumnSpan(this.lblResultsHeader, 2);
            this.panelInputs.SetColumnSpan(this.lblPitch, 2);
            this.panelInputs.SetColumnSpan(this.lblBase, 2);
            this.panelInputs.SetColumnSpan(this.lblAddendum, 2);
            this.panelInputs.SetColumnSpan(this.lblDedendum, 2);
            this.panelInputs.SetColumnSpan(this.lblClearanceVal, 2);
            this.panelInputs.SetColumnSpan(this.lblBacklashVal, 2);
            this.panelInputs.SetColumnSpan(this.lblHelixVal, 2);
            this.panelInputs.SetColumnSpan(this.lblError, 2);
            this.panelInputs.Size = new System.Drawing.Size(400, 1036);
            this.panelInputs.TabIndex = 0;
            // 
            // lblTeeth
            // 
            this.lblTeeth.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblTeeth.AutoSize = true;
            this.lblTeeth.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblTeeth.Location = new System.Drawing.Point(15, 13);
            this.lblTeeth.Name = "lblTeeth";
            this.lblTeeth.Size = new System.Drawing.Size(140, 20);
            this.lblTeeth.TabIndex = 0;
            this.lblTeeth.Text = "Number of teeth (z)";
            // 
            // numTeeth
            // 
            this.numTeeth.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numTeeth.Location = new System.Drawing.Point(180, 12);
            this.numTeeth.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numTeeth.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numTeeth.Minimum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.numTeeth.Name = "numTeeth";
            this.numTeeth.Size = new System.Drawing.Size(132, 22);
            this.numTeeth.TabIndex = 1;
            this.numTeeth.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // lblModule
            // 
            this.lblModule.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblModule.AutoSize = true;
            this.lblModule.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblModule.Location = new System.Drawing.Point(15, 39);
            this.lblModule.Name = "lblModule";
            this.lblModule.Size = new System.Drawing.Size(117, 20);
            this.lblModule.TabIndex = 2;
            this.lblModule.Text = "Module m (mm)";
            // 
            // numModule
            // 
            this.numModule.DecimalPlaces = 3;
            this.numModule.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numModule.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numModule.Location = new System.Drawing.Point(180, 38);
            this.numModule.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numModule.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numModule.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.numModule.Name = "numModule";
            this.numModule.Size = new System.Drawing.Size(132, 22);
            this.numModule.TabIndex = 3;
            this.numModule.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // lblPressureAngle
            // 
            this.lblPressureAngle.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPressureAngle.AutoSize = true;
            this.lblPressureAngle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblPressureAngle.Location = new System.Drawing.Point(15, 65);
            this.lblPressureAngle.Name = "lblPressureAngle";
            this.lblPressureAngle.Size = new System.Drawing.Size(137, 20);
            this.lblPressureAngle.TabIndex = 4;
            this.lblPressureAngle.Text = "Pressure angle α (°)";
            // 
            // numPressureAngle
            // 
            this.numPressureAngle.DecimalPlaces = 2;
            this.numPressureAngle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numPressureAngle.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.numPressureAngle.Location = new System.Drawing.Point(180, 64);
            this.numPressureAngle.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numPressureAngle.Maximum = new decimal(new int[] {
            45,
            0,
            0,
            0});
            this.numPressureAngle.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numPressureAngle.Name = "numPressureAngle";
            this.numPressureAngle.Size = new System.Drawing.Size(132, 22);
            this.numPressureAngle.TabIndex = 5;
            this.numPressureAngle.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // lblHelixAngle
            // 
            this.lblHelixAngle.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblHelixAngle.AutoSize = true;
            this.lblHelixAngle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblHelixAngle.Location = new System.Drawing.Point(15, 91);
            this.lblHelixAngle.Name = "lblHelixAngle";
            this.lblHelixAngle.Size = new System.Drawing.Size(116, 20);
            this.lblHelixAngle.TabIndex = 6;
            this.lblHelixAngle.Text = "Helix angle β (°)";
            // 
            // numHelixAngle
            // 
            this.numHelixAngle.DecimalPlaces = 2;
            this.numHelixAngle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numHelixAngle.Location = new System.Drawing.Point(180, 90);
            this.numHelixAngle.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numHelixAngle.Maximum = new decimal(new int[] {
            75,
            0,
            0,
            0});
            this.numHelixAngle.Name = "numHelixAngle";
            this.numHelixAngle.Size = new System.Drawing.Size(132, 22);
            this.numHelixAngle.TabIndex = 7;
            // 
            // lblHelicalDirection
            // 
            this.lblHelicalDirection.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblHelicalDirection.AutoSize = true;
            this.lblHelicalDirection.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblHelicalDirection.Location = new System.Drawing.Point(15, 119);
            this.lblHelicalDirection.Name = "lblHelicalDirection";
            this.lblHelicalDirection.Size = new System.Drawing.Size(118, 20);
            this.lblHelicalDirection.TabIndex = 8;
            this.lblHelicalDirection.Text = "Helical direction";
            // 
            // cboHelicalDirection
            // 
            this.cboHelicalDirection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboHelicalDirection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboHelicalDirection.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.cboHelicalDirection.FormattingEnabled = true;
            this.cboHelicalDirection.Items.AddRange(new object[] {
            "R (Right)",
            "L (Left)"});
            this.cboHelicalDirection.Location = new System.Drawing.Point(180, 116);
            this.cboHelicalDirection.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cboHelicalDirection.Name = "cboHelicalDirection";
            this.cboHelicalDirection.Size = new System.Drawing.Size(132, 27);
            this.cboHelicalDirection.TabIndex = 9;
            // 
            // lblClearance
            // 
            this.lblClearance.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblClearance.AutoSize = true;
            this.lblClearance.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblClearance.Location = new System.Drawing.Point(15, 148);
            this.lblClearance.Name = "lblClearance";
            this.lblClearance.Size = new System.Drawing.Size(125, 20);
            this.lblClearance.TabIndex = 10;
            this.lblClearance.Text = "Clearance c (mm)";
            // 
            // numClearance
            // 
            this.numClearance.DecimalPlaces = 3;
            this.numClearance.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numClearance.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numClearance.Location = new System.Drawing.Point(180, 147);
            this.numClearance.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numClearance.Name = "numClearance";
            this.numClearance.Size = new System.Drawing.Size(132, 22);
            this.numClearance.TabIndex = 11;
            this.numClearance.Value = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            // 
            // lblBacklash
            // 
            this.lblBacklash.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblBacklash.AutoSize = true;
            this.lblBacklash.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBacklash.Location = new System.Drawing.Point(15, 174);
            this.lblBacklash.Name = "lblBacklash";
            this.lblBacklash.Size = new System.Drawing.Size(125, 20);
            this.lblBacklash.TabIndex = 12;
            this.lblBacklash.Text = "Backlash j_t (mm)";
            // 
            // numBacklash
            // 
            this.numBacklash.DecimalPlaces = 3;
            this.numBacklash.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numBacklash.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numBacklash.Location = new System.Drawing.Point(180, 173);
            this.numBacklash.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numBacklash.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numBacklash.Name = "numBacklash";
            this.numBacklash.Size = new System.Drawing.Size(132, 22);
            this.numBacklash.TabIndex = 13;
            // 
            // lblFaceWidth
            // 
            this.lblFaceWidth.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblFaceWidth.AutoSize = true;
            this.lblFaceWidth.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblFaceWidth.Location = new System.Drawing.Point(15, 200);
            this.lblFaceWidth.Name = "lblFaceWidth";
            this.lblFaceWidth.Size = new System.Drawing.Size(132, 20);
            this.lblFaceWidth.TabIndex = 14;
            this.lblFaceWidth.Text = "Face width b (mm)";
            // 
            // numFaceWidth
            // 
            this.numFaceWidth.DecimalPlaces = 2;
            this.numFaceWidth.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numFaceWidth.Location = new System.Drawing.Point(180, 199);
            this.numFaceWidth.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numFaceWidth.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numFaceWidth.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numFaceWidth.Name = "numFaceWidth";
            this.numFaceWidth.Size = new System.Drawing.Size(132, 22);
            this.numFaceWidth.TabIndex = 15;
            this.numFaceWidth.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
            // 
            // lblProfileShift
            // 
            this.lblProfileShift.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblProfileShift.AutoSize = true;
            this.lblProfileShift.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblProfileShift.Location = new System.Drawing.Point(15, 226);
            this.lblProfileShift.Name = "lblProfileShift";
            this.lblProfileShift.Size = new System.Drawing.Size(146, 20);
            this.lblProfileShift.TabIndex = 16;
            this.lblProfileShift.Text = "Profile shift coeff. (x)";
            // 
            // numProfileShift
            // 
            this.numProfileShift.DecimalPlaces = 3;
            this.numProfileShift.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numProfileShift.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numProfileShift.Location = new System.Drawing.Point(180, 225);
            this.numProfileShift.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numProfileShift.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numProfileShift.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            -2147483648});
            this.numProfileShift.Name = "numProfileShift";
            this.numProfileShift.Size = new System.Drawing.Size(132, 22);
            this.numProfileShift.TabIndex = 17;
            // 
            // lblRimThickness
            // 
            this.lblRimThickness.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblRimThickness.AutoSize = true;
            this.lblRimThickness.Enabled = false;
            this.lblRimThickness.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblRimThickness.Location = new System.Drawing.Point(15, 252);
            this.lblRimThickness.Name = "lblRimThickness";
            this.lblRimThickness.Size = new System.Drawing.Size(138, 20);
            this.lblRimThickness.TabIndex = 18;
            this.lblRimThickness.Text = "Rim thickness (mm)";
            // 
            // numRimThickness
            // 
            this.numRimThickness.DecimalPlaces = 2;
            this.numRimThickness.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numRimThickness.Enabled = false;
            this.numRimThickness.Location = new System.Drawing.Point(180, 251);
            this.numRimThickness.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numRimThickness.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numRimThickness.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numRimThickness.Name = "numRimThickness";
            this.numRimThickness.Size = new System.Drawing.Size(132, 22);
            this.numRimThickness.TabIndex = 19;
            this.numRimThickness.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // chkInternal
            // 
            this.chkInternal.AutoSize = true;
            this.chkInternal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkInternal.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkInternal.Location = new System.Drawing.Point(15, 277);
            this.chkInternal.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.chkInternal.Name = "chkInternal";
            this.chkInternal.Size = new System.Drawing.Size(297, 24);
            this.chkInternal.TabIndex = 20;
            this.chkInternal.Text = "Internal gear";
            this.chkInternal.UseVisualStyleBackColor = true;
            // 
            // chkOptimalShift
            // 
            this.chkOptimalShift.AutoSize = true;
            this.chkOptimalShift.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkOptimalShift.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkOptimalShift.Location = new System.Drawing.Point(15, 305);
            this.chkOptimalShift.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.chkOptimalShift.Name = "chkOptimalShift";
            this.chkOptimalShift.Size = new System.Drawing.Size(297, 24);
            this.chkOptimalShift.TabIndex = 21;
            this.chkOptimalShift.Text = "Optimal shift (anti-undercut)";
            this.chkOptimalShift.UseVisualStyleBackColor = true;
            // 
            // chkFullGear
            // 
            this.chkFullGear.AutoSize = true;
            this.chkFullGear.Checked = true;
            this.chkFullGear.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFullGear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkFullGear.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkFullGear.Location = new System.Drawing.Point(15, 333);
            this.chkFullGear.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.chkFullGear.Name = "chkFullGear";
            this.chkFullGear.Size = new System.Drawing.Size(297, 24);
            this.chkFullGear.TabIndex = 22;
            this.chkFullGear.Text = "Show full gear profile";
            this.chkFullGear.UseVisualStyleBackColor = true;
            // 
            // btnCalculate
            // 
            this.btnCalculate.BackColor = System.Drawing.Color.SteelBlue;
            this.btnCalculate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCalculate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCalculate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCalculate.ForeColor = System.Drawing.Color.White;
            this.btnCalculate.Location = new System.Drawing.Point(15, 361);
            this.btnCalculate.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnCalculate.Name = "btnCalculate";
            this.btnCalculate.Size = new System.Drawing.Size(297, 30);
            this.btnCalculate.TabIndex = 23;
            this.btnCalculate.Text = "Execute Calculations";
            this.btnCalculate.UseVisualStyleBackColor = false;
            // 
            // progressBar
            // 
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBar.Location = new System.Drawing.Point(15, 395);
            this.progressBar.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(297, 13);
            this.progressBar.TabIndex = 24;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this.lblStatus.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblStatus.Location = new System.Drawing.Point(15, 412);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(297, 19);
            this.lblStatus.TabIndex = 25;
            this.lblStatus.Text = "Ready";
            // 
            // lblResultsHeader
            // 
            this.lblResultsHeader.AutoSize = true;
            this.lblResultsHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblResultsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblResultsHeader.Location = new System.Drawing.Point(15, 437);
            this.lblResultsHeader.Margin = new System.Windows.Forms.Padding(3, 4, 3, 2);
            this.lblResultsHeader.Name = "lblResultsHeader";
            this.lblResultsHeader.Size = new System.Drawing.Size(297, 20);
            this.lblResultsHeader.TabIndex = 26;
            this.lblResultsHeader.Text = "Summary Radii (mm)";
            // 
            // lblPitch
            // 
            this.lblPitch.AutoSize = true;
            this.lblPitch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitch.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblPitch.Location = new System.Drawing.Point(15, 461);
            this.lblPitch.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.lblPitch.Name = "lblPitch";
            this.lblPitch.Size = new System.Drawing.Size(297, 20);
            this.lblPitch.TabIndex = 27;
            this.lblPitch.Text = "Pitch: –";
            // 
            // lblBase
            // 
            this.lblBase.AutoSize = true;
            this.lblBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBase.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBase.Location = new System.Drawing.Point(15, 485);
            this.lblBase.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.lblBase.Name = "lblBase";
            this.lblBase.Size = new System.Drawing.Size(297, 20);
            this.lblBase.TabIndex = 28;
            this.lblBase.Text = "Base: –";
            // 
            // lblAddendum
            // 
            this.lblAddendum.AutoSize = true;
            this.lblAddendum.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAddendum.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblAddendum.Location = new System.Drawing.Point(15, 509);
            this.lblAddendum.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.lblAddendum.Name = "lblAddendum";
            this.lblAddendum.Size = new System.Drawing.Size(297, 20);
            this.lblAddendum.TabIndex = 29;
            this.lblAddendum.Text = "Addendum: –";
            // 
            // lblDedendum
            // 
            this.lblDedendum.AutoSize = true;
            this.lblDedendum.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDedendum.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblDedendum.Location = new System.Drawing.Point(15, 533);
            this.lblDedendum.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.lblDedendum.Name = "lblDedendum";
            this.lblDedendum.Size = new System.Drawing.Size(297, 20);
            this.lblDedendum.TabIndex = 30;
            this.lblDedendum.Text = "Dedendum: –";
            // 
            // lblClearanceVal
            // 
            this.lblClearanceVal.AutoSize = true;
            this.lblClearanceVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblClearanceVal.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblClearanceVal.Location = new System.Drawing.Point(15, 557);
            this.lblClearanceVal.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.lblClearanceVal.Name = "lblClearanceVal";
            this.lblClearanceVal.Size = new System.Drawing.Size(297, 20);
            this.lblClearanceVal.TabIndex = 31;
            this.lblClearanceVal.Text = "Clearance: –";
            // 
            // lblBacklashVal
            // 
            this.lblBacklashVal.AutoSize = true;
            this.lblBacklashVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBacklashVal.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBacklashVal.Location = new System.Drawing.Point(15, 581);
            this.lblBacklashVal.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.lblBacklashVal.Name = "lblBacklashVal";
            this.lblBacklashVal.Size = new System.Drawing.Size(297, 20);
            this.lblBacklashVal.TabIndex = 32;
            this.lblBacklashVal.Text = "Backlash: –";
            // 
            // lblHelixVal
            // 
            this.lblHelixVal.AutoSize = true;
            this.lblHelixVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHelixVal.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblHelixVal.Location = new System.Drawing.Point(15, 605);
            this.lblHelixVal.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.lblHelixVal.Name = "lblHelixVal";
            this.lblHelixVal.Size = new System.Drawing.Size(297, 20);
            this.lblHelixVal.TabIndex = 33;
            this.lblHelixVal.Text = "Helix: –";
            // 
            // lblError
            // 
            this.lblError.AutoSize = true;
            this.lblError.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblError.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblError.ForeColor = System.Drawing.Color.Firebrick;
            this.lblError.Location = new System.Drawing.Point(15, 629);
            this.lblError.Margin = new System.Windows.Forms.Padding(3, 4, 3, 2);
            this.lblError.Name = "lblError";
            this.lblError.Size = new System.Drawing.Size(297, 20);
            this.lblError.TabIndex = 34;
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.Location = new System.Drawing.Point(327, 30);
            this.splitContainerMain.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.splitContainerMain.Name = "splitContainerMain";
            this.splitContainerMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.preview);
            this.splitContainerMain.Panel1MinSize = 250;
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.panelTableContainer);
            this.splitContainerMain.Panel2MinSize = 200;
            this.splitContainerMain.Size = new System.Drawing.Size(1085, 1036);
            this.splitContainerMain.SplitterDistance = 559;
            this.splitContainerMain.SplitterWidth = 3;
            this.splitContainerMain.TabIndex = 1;
            // 
            // preview
            // 
            this.preview.BackColor = System.Drawing.Color.White;
            this.preview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.preview.LineColor = System.Drawing.Color.MidnightBlue;
            this.preview.Location = new System.Drawing.Point(0, 0);
            this.preview.MeshColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(130)))), ((int)(((byte)(180)))));
            this.preview.Mode3D = Bolsover.Gears.Gui.Render3DMode.Solid;
            this.preview.Name = "preview";
            this.preview.ShowReferenceCircles = true;
            this.preview.Size = new System.Drawing.Size(1085, 559);
            this.preview.TabIndex = 0;
            this.preview.ViewMode = Bolsover.Gears.Gui.PreviewViewMode.Mode2D;
            // 
            // panelTableContainer
            // 
            this.panelTableContainer.Controls.Add(this.gridCalculations);
            this.panelTableContainer.Controls.Add(this.lblTableTitle);
            this.panelTableContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelTableContainer.Location = new System.Drawing.Point(0, 0);
            this.panelTableContainer.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.panelTableContainer.Name = "panelTableContainer";
            this.panelTableContainer.Padding = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.panelTableContainer.Size = new System.Drawing.Size(1085, 474);
            this.panelTableContainer.TabIndex = 0;
            // 
            // gridCalculations
            // 
            this.gridCalculations.AllowUserToAddRows = false;
            this.gridCalculations.AllowUserToDeleteRows = false;
            this.gridCalculations.AllowUserToResizeRows = false;
            this.gridCalculations.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridCalculations.BackgroundColor = System.Drawing.Color.White;
            this.gridCalculations.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.gridCalculations.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(244)))), ((int)(((byte)(248)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(50)))), ((int)(((byte)(60)))));
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridCalculations.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.gridCalculations.ColumnHeadersHeight = 30;
            this.gridCalculations.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colParamName,
            this.colSymbol,
            this.colFormula,
            this.colValue,
            this.colUnit});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(235)))), ((int)(((byte)(252)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gridCalculations.DefaultCellStyle = dataGridViewCellStyle2;
            this.gridCalculations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridCalculations.EnableHeadersVisualStyles = false;
            this.gridCalculations.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(230)))), ((int)(((byte)(235)))));
            this.gridCalculations.Location = new System.Drawing.Point(6, 27);
            this.gridCalculations.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.gridCalculations.Name = "gridCalculations";
            this.gridCalculations.ReadOnly = true;
            this.gridCalculations.RowHeadersVisible = false;
            this.gridCalculations.RowHeadersWidth = 51;
            this.gridCalculations.RowTemplate.Height = 30;
            this.gridCalculations.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridCalculations.Size = new System.Drawing.Size(1073, 442);
            this.gridCalculations.TabIndex = 1;
            // 
            // colParamName
            // 
            this.colParamName.FillWeight = 35.16133F;
            this.colParamName.HeaderText = "Parameter";
            this.colParamName.MinimumWidth = 6;
            this.colParamName.Name = "colParamName";
            this.colParamName.ReadOnly = true;
            // 
            // colSymbol
            // 
            this.colSymbol.FillWeight = 12.48969F;
            this.colSymbol.HeaderText = "Symbol";
            this.colSymbol.MinimumWidth = 6;
            this.colSymbol.Name = "colSymbol";
            this.colSymbol.ReadOnly = true;
            this.colSymbol.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            // 
            // colFormula
            // 
            this.colFormula.FillWeight = 19.17906F;
            this.colFormula.HeaderText = "Formula";
            this.colFormula.MinimumWidth = 6;
            this.colFormula.Name = "colFormula";
            this.colFormula.ReadOnly = true;
            this.colFormula.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            // 
            // colValue
            // 
            this.colValue.FillWeight = 17.58067F;
            this.colValue.HeaderText = "Value";
            this.colValue.MinimumWidth = 6;
            this.colValue.Name = "colValue";
            this.colValue.ReadOnly = true;
            // 
            // colUnit
            // 
            this.colUnit.FillWeight = 10.0461F;
            this.colUnit.HeaderText = "Unit";
            this.colUnit.MinimumWidth = 6;
            this.colUnit.Name = "colUnit";
            this.colUnit.ReadOnly = true;
            // 
            // lblTableTitle
            // 
            this.lblTableTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTableTitle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblTableTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(55)))), ((int)(((byte)(72)))));
            this.lblTableTitle.Location = new System.Drawing.Point(6, 5);
            this.lblTableTitle.Name = "lblTableTitle";
            this.lblTableTitle.Size = new System.Drawing.Size(1073, 22);
            this.lblTableTitle.TabIndex = 0;
            this.lblTableTitle.Text = "Calculated Gear Parameters (Elements of Metric Gear Technology)";
            this.lblTableTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // AlibreGearForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1412, 1066);
            this.Controls.Add(this.splitContainerMain);
            this.Controls.Add(this.panelInputs);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MinimumSize = new System.Drawing.Size(950, 529);
            this.Name = "AlibreGearForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Involute Gear Design & Visualisation (2D / 3D)";
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.panelInputs.ResumeLayout(false);
            this.panelInputs.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTeeth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numModule)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPressureAngle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHelixAngle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numClearance)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBacklash)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFaceWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numProfileShift)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRimThickness)).EndInit();
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            this.panelTableContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridCalculations)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem menuFile;
        private System.Windows.Forms.ToolStripMenuItem menuExportDxf;
        private System.Windows.Forms.ToolStripMenuItem menuExportCsv;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparatorFile;
        private System.Windows.Forms.ToolStripMenuItem menuExit;
        private System.Windows.Forms.ToolStripMenuItem menuGear;
        private System.Windows.Forms.ToolStripMenuItem menuAddMeshedGear;
        private System.Windows.Forms.ToolStripMenuItem menuRemoveMeshedGear;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparatorGear;
        private System.Windows.Forms.ToolStripMenuItem menuCalculate;
        private System.Windows.Forms.ToolStripMenuItem menuView;
        private System.Windows.Forms.ToolStripMenuItem menuView2D;
        private System.Windows.Forms.ToolStripMenuItem menuView3DSolid;
        private System.Windows.Forms.ToolStripMenuItem menuView3DWireframe;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparatorView1;
        private System.Windows.Forms.ToolStripMenuItem menuShowReferenceCircles;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparatorView2;
        private System.Windows.Forms.ToolStripMenuItem menuResetView;

        private System.Windows.Forms.TableLayoutPanel panelInputs;
        private System.Windows.Forms.Label lblTeeth;
        private System.Windows.Forms.NumericUpDown numTeeth;
        private System.Windows.Forms.Label lblModule;
        private System.Windows.Forms.NumericUpDown numModule;
        private System.Windows.Forms.Label lblPressureAngle;
        private System.Windows.Forms.NumericUpDown numPressureAngle;
        private System.Windows.Forms.Label lblHelixAngle;
        private System.Windows.Forms.NumericUpDown numHelixAngle;
        private System.Windows.Forms.Label lblHelicalDirection;
        private System.Windows.Forms.ComboBox cboHelicalDirection;
        private System.Windows.Forms.Label lblClearance;
        private System.Windows.Forms.NumericUpDown numClearance;
        private System.Windows.Forms.Label lblBacklash;
        private System.Windows.Forms.NumericUpDown numBacklash;
        private System.Windows.Forms.Label lblFaceWidth;
        private System.Windows.Forms.NumericUpDown numFaceWidth;
        private System.Windows.Forms.Label lblProfileShift;
        private System.Windows.Forms.NumericUpDown numProfileShift;
        private System.Windows.Forms.Label lblRimThickness;
        private System.Windows.Forms.NumericUpDown numRimThickness;
        private System.Windows.Forms.CheckBox chkOptimalShift;
        private System.Windows.Forms.CheckBox chkInternal;
        private System.Windows.Forms.CheckBox chkFullGear;
        private System.Windows.Forms.Button btnCalculate;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblResultsHeader;
        private System.Windows.Forms.Label lblPitch;
        private System.Windows.Forms.Label lblBase;
        private System.Windows.Forms.Label lblAddendum;
        private System.Windows.Forms.Label lblDedendum;
        private System.Windows.Forms.Label lblClearanceVal;
        private System.Windows.Forms.Label lblBacklashVal;
        private System.Windows.Forms.Label lblHelixVal;
        private System.Windows.Forms.Label lblError;
        private System.Windows.Forms.SplitContainer splitContainerMain;
        private Bolsover.Gears.Gui.AlibreGearPreviewControl preview;
        private System.Windows.Forms.Panel panelTableContainer;
        private System.Windows.Forms.Label lblTableTitle;
        private System.Windows.Forms.DataGridView gridCalculations;
        private System.Windows.Forms.DataGridViewTextBoxColumn colParamName;
        private System.Windows.Forms.DataGridViewImageColumn colSymbol;
        private System.Windows.Forms.DataGridViewImageColumn colFormula;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colUnit;
    }
}
