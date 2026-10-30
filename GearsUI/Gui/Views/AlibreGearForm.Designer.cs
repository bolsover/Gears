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
            this.panelInputs = new System.Windows.Forms.Panel();
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
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFile,
            this.menuGear,
            this.menuView});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(1200, 28);
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
            this.menuFile.Size = new System.Drawing.Size(46, 24);
            this.menuFile.Text = "&File";
            // 
            // menuExportDxf
            // 
            this.menuExportDxf.Name = "menuExportDxf";
            this.menuExportDxf.Size = new System.Drawing.Size(182, 26);
            this.menuExportDxf.Text = "Export &DXF…";
            // 
            // menuExportCsv
            // 
            this.menuExportCsv.Name = "menuExportCsv";
            this.menuExportCsv.Size = new System.Drawing.Size(182, 26);
            this.menuExportCsv.Text = "Export &CSV…";
            // 
            // toolStripSeparatorFile
            // 
            this.toolStripSeparatorFile.Name = "toolStripSeparatorFile";
            this.toolStripSeparatorFile.Size = new System.Drawing.Size(179, 6);
            // 
            // menuExit
            // 
            this.menuExit.Name = "menuExit";
            this.menuExit.Size = new System.Drawing.Size(182, 26);
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
            this.menuGear.Size = new System.Drawing.Size(55, 24);
            this.menuGear.Text = "&Gear";
            // 
            // menuAddMeshedGear
            // 
            this.menuAddMeshedGear.Name = "menuAddMeshedGear";
            this.menuAddMeshedGear.Size = new System.Drawing.Size(240, 26);
            this.menuAddMeshedGear.Text = "&Add Meshed Gear…";
            // 
            // menuRemoveMeshedGear
            // 
            this.menuRemoveMeshedGear.Enabled = false;
            this.menuRemoveMeshedGear.Name = "menuRemoveMeshedGear";
            this.menuRemoveMeshedGear.Size = new System.Drawing.Size(240, 26);
            this.menuRemoveMeshedGear.Text = "&Remove Meshed Gear";
            // 
            // toolStripSeparatorGear
            // 
            this.toolStripSeparatorGear.Name = "toolStripSeparatorGear";
            this.toolStripSeparatorGear.Size = new System.Drawing.Size(237, 6);
            // 
            // menuCalculate
            // 
            this.menuCalculate.Name = "menuCalculate";
            this.menuCalculate.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.menuCalculate.Size = new System.Drawing.Size(240, 26);
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
            this.menuView.Size = new System.Drawing.Size(55, 24);
            this.menuView.Text = "&View";
            // 
            // menuView2D
            // 
            this.menuView2D.Checked = true;
            this.menuView2D.CheckState = System.Windows.Forms.CheckState.Checked;
            this.menuView2D.Name = "menuView2D";
            this.menuView2D.Size = new System.Drawing.Size(244, 26);
            this.menuView2D.Text = "&2D Profile";
            // 
            // menuView3DSolid
            // 
            this.menuView3DSolid.Name = "menuView3DSolid";
            this.menuView3DSolid.Size = new System.Drawing.Size(244, 26);
            this.menuView3DSolid.Text = "3D &Solid";
            // 
            // menuView3DWireframe
            // 
            this.menuView3DWireframe.Name = "menuView3DWireframe";
            this.menuView3DWireframe.Size = new System.Drawing.Size(244, 26);
            this.menuView3DWireframe.Text = "3D &Wireframe";
            // 
            // toolStripSeparatorView1
            // 
            this.toolStripSeparatorView1.Name = "toolStripSeparatorView1";
            this.toolStripSeparatorView1.Size = new System.Drawing.Size(241, 6);
            // 
            // menuShowReferenceCircles
            // 
            this.menuShowReferenceCircles.Checked = true;
            this.menuShowReferenceCircles.CheckOnClick = true;
            this.menuShowReferenceCircles.CheckState = System.Windows.Forms.CheckState.Checked;
            this.menuShowReferenceCircles.Name = "menuShowReferenceCircles";
            this.menuShowReferenceCircles.Size = new System.Drawing.Size(244, 26);
            this.menuShowReferenceCircles.Text = "Show &Reference Circles";
            // 
            // toolStripSeparatorView2
            // 
            this.toolStripSeparatorView2.Name = "toolStripSeparatorView2";
            this.toolStripSeparatorView2.Size = new System.Drawing.Size(241, 6);
            // 
            // menuResetView
            // 
            this.menuResetView.Name = "menuResetView";
            this.menuResetView.Size = new System.Drawing.Size(244, 26);
            this.menuResetView.Text = "&Reset View / Camera";
            // 
            // panelInputs
            // 
            this.panelInputs.AutoScroll = true;
            this.panelInputs.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelInputs.Controls.Add(this.lblTeeth);
            this.panelInputs.Controls.Add(this.numTeeth);
            this.panelInputs.Controls.Add(this.lblModule);
            this.panelInputs.Controls.Add(this.numModule);
            this.panelInputs.Controls.Add(this.lblPressureAngle);
            this.panelInputs.Controls.Add(this.numPressureAngle);
            this.panelInputs.Controls.Add(this.lblHelixAngle);
            this.panelInputs.Controls.Add(this.numHelixAngle);
            this.panelInputs.Controls.Add(this.lblHelicalDirection);
            this.panelInputs.Controls.Add(this.cboHelicalDirection);
            this.panelInputs.Controls.Add(this.lblClearance);
            this.panelInputs.Controls.Add(this.numClearance);
            this.panelInputs.Controls.Add(this.lblBacklash);
            this.panelInputs.Controls.Add(this.numBacklash);
            this.panelInputs.Controls.Add(this.lblFaceWidth);
            this.panelInputs.Controls.Add(this.numFaceWidth);
            this.panelInputs.Controls.Add(this.lblProfileShift);
            this.panelInputs.Controls.Add(this.numProfileShift);
            this.panelInputs.Controls.Add(this.lblRimThickness);
            this.panelInputs.Controls.Add(this.numRimThickness);
            this.panelInputs.Controls.Add(this.chkInternal);
            this.panelInputs.Controls.Add(this.chkOptimalShift);
            this.panelInputs.Controls.Add(this.chkFullGear);
            this.panelInputs.Controls.Add(this.btnCalculate);
            this.panelInputs.Controls.Add(this.progressBar);
            this.panelInputs.Controls.Add(this.lblStatus);
            this.panelInputs.Controls.Add(this.lblResultsHeader);
            this.panelInputs.Controls.Add(this.lblPitch);
            this.panelInputs.Controls.Add(this.lblBase);
            this.panelInputs.Controls.Add(this.lblAddendum);
            this.panelInputs.Controls.Add(this.lblDedendum);
            this.panelInputs.Controls.Add(this.lblClearanceVal);
            this.panelInputs.Controls.Add(this.lblBacklashVal);
            this.panelInputs.Controls.Add(this.lblHelixVal);
            this.panelInputs.Controls.Add(this.lblError);
            this.panelInputs.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelInputs.Location = new System.Drawing.Point(0, 0);
            this.panelInputs.Name = "panelInputs";
            this.panelInputs.Padding = new System.Windows.Forms.Padding(12);
            this.panelInputs.Size = new System.Drawing.Size(340, 840);
            this.panelInputs.TabIndex = 0;
            // 
            // lblTeeth
            // 
            this.lblTeeth.AutoSize = true;
            this.lblTeeth.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblTeeth.Location = new System.Drawing.Point(12, 14);
            this.lblTeeth.Name = "lblTeeth";
            this.lblTeeth.Size = new System.Drawing.Size(117, 19);
            this.lblTeeth.TabIndex = 0;
            this.lblTeeth.Text = "Number of teeth (z)";
            // 
            // numTeeth
            // 
            this.numTeeth.Location = new System.Drawing.Point(195, 12);
            this.numTeeth.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numTeeth.Minimum = new decimal(new int[] { 3, 0, 0, 0 });
            this.numTeeth.Name = "numTeeth";
            this.numTeeth.Size = new System.Drawing.Size(115, 27);
            this.numTeeth.TabIndex = 1;
            this.numTeeth.Value = new decimal(new int[] { 20, 0, 0, 0 });
            // 
            // lblModule
            // 
            this.lblModule.AutoSize = true;
            this.lblModule.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblModule.Location = new System.Drawing.Point(12, 47);
            this.lblModule.Name = "lblModule";
            this.lblModule.Size = new System.Drawing.Size(127, 19);
            this.lblModule.TabIndex = 2;
            this.lblModule.Text = "Module m (mm)";
            // 
            // numModule
            // 
            this.numModule.DecimalPlaces = 3;
            this.numModule.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numModule.Location = new System.Drawing.Point(195, 45);
            this.numModule.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numModule.Minimum = new decimal(new int[] { 1, 0, 0, 196608 });
            this.numModule.Name = "numModule";
            this.numModule.Size = new System.Drawing.Size(115, 27);
            this.numModule.TabIndex = 3;
            this.numModule.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // lblPressureAngle
            // 
            this.lblPressureAngle.AutoSize = true;
            this.lblPressureAngle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblPressureAngle.Location = new System.Drawing.Point(12, 80);
            this.lblPressureAngle.Name = "lblPressureAngle";
            this.lblPressureAngle.Size = new System.Drawing.Size(149, 19);
            this.lblPressureAngle.TabIndex = 4;
            this.lblPressureAngle.Text = "Pressure angle α (°)";
            // 
            // numPressureAngle
            // 
            this.numPressureAngle.DecimalPlaces = 2;
            this.numPressureAngle.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            this.numPressureAngle.Location = new System.Drawing.Point(195, 78);
            this.numPressureAngle.Maximum = new decimal(new int[] { 45, 0, 0, 0 });
            this.numPressureAngle.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
            this.numPressureAngle.Name = "numPressureAngle";
            this.numPressureAngle.Size = new System.Drawing.Size(115, 27);
            this.numPressureAngle.TabIndex = 5;
            this.numPressureAngle.Value = new decimal(new int[] { 20, 0, 0, 0 });
            // 
            // lblHelixAngle
            // 
            this.lblHelixAngle.AutoSize = true;
            this.lblHelixAngle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblHelixAngle.Location = new System.Drawing.Point(12, 113);
            this.lblHelixAngle.Name = "lblHelixAngle";
            this.lblHelixAngle.Size = new System.Drawing.Size(125, 19);
            this.lblHelixAngle.TabIndex = 6;
            this.lblHelixAngle.Text = "Helix angle β (°)";
            // 
            // numHelixAngle
            // 
            this.numHelixAngle.DecimalPlaces = 2;
            this.numHelixAngle.Increment = new decimal(new int[] { 1, 0, 0, 0 });
            this.numHelixAngle.Location = new System.Drawing.Point(195, 111);
            this.numHelixAngle.Maximum = new decimal(new int[] { 75, 0, 0, 0 });
            this.numHelixAngle.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.numHelixAngle.Name = "numHelixAngle";
            this.numHelixAngle.Size = new System.Drawing.Size(115, 27);
            this.numHelixAngle.TabIndex = 7;
            this.numHelixAngle.Value = new decimal(new int[] { 0, 0, 0, 0 });
            // 
            // lblHelicalDirection
            // 
            this.lblHelicalDirection.AutoSize = true;
            this.lblHelicalDirection.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblHelicalDirection.Location = new System.Drawing.Point(12, 146);
            this.lblHelicalDirection.Name = "lblHelicalDirection";
            this.lblHelicalDirection.Size = new System.Drawing.Size(110, 19);
            this.lblHelicalDirection.TabIndex = 8;
            this.lblHelicalDirection.Text = "Helical direction";
            // 
            // cboHelicalDirection
            // 
            this.cboHelicalDirection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboHelicalDirection.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.cboHelicalDirection.FormattingEnabled = true;
            this.cboHelicalDirection.Items.AddRange(new object[] {
            "R (Right)",
            "L (Left)"});
            this.cboHelicalDirection.Location = new System.Drawing.Point(195, 144);
            this.cboHelicalDirection.Name = "cboHelicalDirection";
            this.cboHelicalDirection.Size = new System.Drawing.Size(115, 27);
            this.cboHelicalDirection.TabIndex = 9;
            // 
            // lblClearance
            // 
            this.lblClearance.AutoSize = true;
            this.lblClearance.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblClearance.Location = new System.Drawing.Point(12, 179);
            this.lblClearance.Name = "lblClearance";
            this.lblClearance.Size = new System.Drawing.Size(138, 19);
            this.lblClearance.TabIndex = 10;
            this.lblClearance.Text = "Clearance c (mm)";
            // 
            // numClearance
            // 
            this.numClearance.DecimalPlaces = 3;
            this.numClearance.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numClearance.Location = new System.Drawing.Point(195, 177);
            this.numClearance.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numClearance.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.numClearance.Name = "numClearance";
            this.numClearance.Size = new System.Drawing.Size(115, 27);
            this.numClearance.TabIndex = 11;
            this.numClearance.Value = new decimal(new int[] { 5, 0, 0, 65536 });
            // 
            // lblBacklash
            // 
            this.lblBacklash.AutoSize = true;
            this.lblBacklash.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBacklash.Location = new System.Drawing.Point(12, 212);
            this.lblBacklash.Name = "lblBacklash";
            this.lblBacklash.Size = new System.Drawing.Size(138, 19);
            this.lblBacklash.TabIndex = 12;
            this.lblBacklash.Text = "Backlash j_t (mm)";
            // 
            // numBacklash
            // 
            this.numBacklash.DecimalPlaces = 3;
            this.numBacklash.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            this.numBacklash.Location = new System.Drawing.Point(195, 210);
            this.numBacklash.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            this.numBacklash.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.numBacklash.Name = "numBacklash";
            this.numBacklash.Size = new System.Drawing.Size(115, 27);
            this.numBacklash.TabIndex = 13;
            this.numBacklash.Value = new decimal(new int[] { 0, 0, 0, 0 });
            // 
            // lblFaceWidth
            // 
            this.lblFaceWidth.AutoSize = true;
            this.lblFaceWidth.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblFaceWidth.Location = new System.Drawing.Point(12, 245);
            this.lblFaceWidth.Name = "lblFaceWidth";
            this.lblFaceWidth.Size = new System.Drawing.Size(140, 19);
            this.lblFaceWidth.TabIndex = 14;
            this.lblFaceWidth.Text = "Face width b (mm)";
            // 
            // numFaceWidth
            // 
            this.numFaceWidth.DecimalPlaces = 2;
            this.numFaceWidth.Location = new System.Drawing.Point(195, 243);
            this.numFaceWidth.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numFaceWidth.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numFaceWidth.Name = "numFaceWidth";
            this.numFaceWidth.Size = new System.Drawing.Size(115, 27);
            this.numFaceWidth.TabIndex = 15;
            this.numFaceWidth.Value = new decimal(new int[] { 15, 0, 0, 0 });
            // 
            // lblProfileShift
            // 
            this.lblProfileShift.AutoSize = true;
            this.lblProfileShift.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblProfileShift.Location = new System.Drawing.Point(12, 278);
            this.lblProfileShift.Name = "lblProfileShift";
            this.lblProfileShift.Size = new System.Drawing.Size(150, 19);
            this.lblProfileShift.TabIndex = 16;
            this.lblProfileShift.Text = "Profile shift coeff. (x)";
            // 
            // numProfileShift
            // 
            this.numProfileShift.DecimalPlaces = 3;
            this.numProfileShift.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numProfileShift.Location = new System.Drawing.Point(195, 276);
            this.numProfileShift.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            this.numProfileShift.Minimum = new decimal(new int[] { 5, 0, 0, -2147483648 });
            this.numProfileShift.Name = "numProfileShift";
            this.numProfileShift.Size = new System.Drawing.Size(115, 27);
            this.numProfileShift.TabIndex = 17;
            // 
            // lblRimThickness
            // 
            this.lblRimThickness.AutoSize = true;
            this.lblRimThickness.Enabled = false;
            this.lblRimThickness.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblRimThickness.Location = new System.Drawing.Point(12, 311);
            this.lblRimThickness.Name = "lblRimThickness";
            this.lblRimThickness.Size = new System.Drawing.Size(155, 19);
            this.lblRimThickness.TabIndex = 18;
            this.lblRimThickness.Text = "Rim thickness (mm)";
            // 
            // numRimThickness
            // 
            this.numRimThickness.DecimalPlaces = 2;
            this.numRimThickness.Enabled = false;
            this.numRimThickness.Location = new System.Drawing.Point(195, 309);
            this.numRimThickness.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numRimThickness.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numRimThickness.Name = "numRimThickness";
            this.numRimThickness.Size = new System.Drawing.Size(115, 27);
            this.numRimThickness.TabIndex = 19;
            this.numRimThickness.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // chkInternal
            // 
            this.chkInternal.AutoSize = true;
            this.chkInternal.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkInternal.Location = new System.Drawing.Point(15, 344);
            this.chkInternal.Name = "chkInternal";
            this.chkInternal.Size = new System.Drawing.Size(114, 24);
            this.chkInternal.TabIndex = 20;
            this.chkInternal.Text = "Internal gear";
            this.chkInternal.UseVisualStyleBackColor = true;
            // 
            // chkOptimalShift
            // 
            this.chkOptimalShift.AutoSize = true;
            this.chkOptimalShift.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkOptimalShift.Location = new System.Drawing.Point(15, 370);
            this.chkOptimalShift.Name = "chkOptimalShift";
            this.chkOptimalShift.Size = new System.Drawing.Size(225, 24);
            this.chkOptimalShift.TabIndex = 21;
            this.chkOptimalShift.Text = "Optimal shift (anti-undercut)";
            this.chkOptimalShift.UseVisualStyleBackColor = true;
            // 
            // chkFullGear
            // 
            this.chkFullGear.AutoSize = true;
            this.chkFullGear.Checked = true;
            this.chkFullGear.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFullGear.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkFullGear.Location = new System.Drawing.Point(15, 396);
            this.chkFullGear.Name = "chkFullGear";
            this.chkFullGear.Size = new System.Drawing.Size(149, 24);
            this.chkFullGear.TabIndex = 22;
            this.chkFullGear.Text = "Show full gear profile";
            this.chkFullGear.UseVisualStyleBackColor = true;
            // 
            // btnCalculate
            // 
            this.btnCalculate.BackColor = System.Drawing.Color.SteelBlue;
            this.btnCalculate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCalculate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCalculate.ForeColor = System.Drawing.Color.White;
            this.btnCalculate.Location = new System.Drawing.Point(12, 430);
            this.btnCalculate.Name = "btnCalculate";
            this.btnCalculate.Size = new System.Drawing.Size(298, 38);
            this.btnCalculate.TabIndex = 23;
            this.btnCalculate.Text = "Execute Calculations";
            this.btnCalculate.UseVisualStyleBackColor = false;
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 474);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(298, 16);
            this.progressBar.TabIndex = 24;
            // 
            // lblStatus
            // 
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this.lblStatus.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblStatus.Location = new System.Drawing.Point(12, 493);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(298, 22);
            this.lblStatus.TabIndex = 25;
            this.lblStatus.Text = "Ready";
            // 
            // lblResultsHeader
            // 
            this.lblResultsHeader.AutoSize = true;
            this.lblResultsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblResultsHeader.Location = new System.Drawing.Point(12, 522);
            this.lblResultsHeader.Name = "lblResultsHeader";
            this.lblResultsHeader.Size = new System.Drawing.Size(176, 20);
            this.lblResultsHeader.TabIndex = 26;
            this.lblResultsHeader.Text = "Summary Radii (mm)";
            // 
            // lblPitch
            // 
            this.lblPitch.AutoSize = true;
            this.lblPitch.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblPitch.Location = new System.Drawing.Point(12, 544);
            this.lblPitch.Name = "lblPitch";
            this.lblPitch.Size = new System.Drawing.Size(55, 19);
            this.lblPitch.TabIndex = 27;
            this.lblPitch.Text = "Pitch: –";
            // 
            // lblBase
            // 
            this.lblBase.AutoSize = true;
            this.lblBase.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBase.Location = new System.Drawing.Point(12, 564);
            this.lblBase.Name = "lblBase";
            this.lblBase.Size = new System.Drawing.Size(51, 19);
            this.lblBase.TabIndex = 28;
            this.lblBase.Text = "Base: –";
            // 
            // lblAddendum
            // 
            this.lblAddendum.AutoSize = true;
            this.lblAddendum.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblAddendum.Location = new System.Drawing.Point(12, 584);
            this.lblAddendum.Name = "lblAddendum";
            this.lblAddendum.Size = new System.Drawing.Size(92, 19);
            this.lblAddendum.TabIndex = 29;
            this.lblAddendum.Text = "Addendum: –";
            // 
            // lblDedendum
            // 
            this.lblDedendum.AutoSize = true;
            this.lblDedendum.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblDedendum.Location = new System.Drawing.Point(12, 604);
            this.lblDedendum.Name = "lblDedendum";
            this.lblDedendum.Size = new System.Drawing.Size(91, 19);
            this.lblDedendum.TabIndex = 30;
            this.lblDedendum.Text = "Dedendum: –";
            // 
            // lblClearanceVal
            // 
            this.lblClearanceVal.AutoSize = true;
            this.lblClearanceVal.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblClearanceVal.Location = new System.Drawing.Point(12, 624);
            this.lblClearanceVal.Name = "lblClearanceVal";
            this.lblClearanceVal.Size = new System.Drawing.Size(84, 19);
            this.lblClearanceVal.TabIndex = 31;
            this.lblClearanceVal.Text = "Clearance: –";
            // 
            // lblBacklashVal
            // 
            this.lblBacklashVal.AutoSize = true;
            this.lblBacklashVal.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBacklashVal.Location = new System.Drawing.Point(12, 644);
            this.lblBacklashVal.Name = "lblBacklashVal";
            this.lblBacklashVal.Size = new System.Drawing.Size(84, 19);
            this.lblBacklashVal.TabIndex = 32;
            this.lblBacklashVal.Text = "Backlash: –";
            // 
            // lblHelixVal
            // 
            this.lblHelixVal.AutoSize = true;
            this.lblHelixVal.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblHelixVal.Location = new System.Drawing.Point(12, 664);
            this.lblHelixVal.Name = "lblHelixVal";
            this.lblHelixVal.Size = new System.Drawing.Size(84, 19);
            this.lblHelixVal.TabIndex = 33;
            this.lblHelixVal.Text = "Helix: –";
            // 
            // lblError
            // 
            this.lblError.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblError.ForeColor = System.Drawing.Color.Firebrick;
            this.lblError.Location = new System.Drawing.Point(12, 687);
            this.lblError.Name = "lblError";
            this.lblError.Size = new System.Drawing.Size(298, 75);
            this.lblError.TabIndex = 34;
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.Location = new System.Drawing.Point(340, 28);
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
            this.splitContainerMain.Size = new System.Drawing.Size(860, 812);
            this.splitContainerMain.SplitterDistance = 440;
            this.splitContainerMain.TabIndex = 1;
            // 
            // preview
            // 
            this.preview.BackColor = System.Drawing.Color.White;
            this.preview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.preview.LineColor = System.Drawing.Color.MidnightBlue;
            this.preview.Location = new System.Drawing.Point(0, 0);
            this.preview.MeshColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(130)))), ((int)(((byte)(180)))));
            this.preview.Name = "preview";
            this.preview.ShowReferenceCircles = true;
            this.preview.Size = new System.Drawing.Size(860, 440);
            this.preview.TabIndex = 0;
            this.preview.ViewMode = Bolsover.Gears.Gui.PreviewViewMode.Mode2D;
            // 
            // panelTableContainer
            // 
            this.panelTableContainer.Controls.Add(this.gridCalculations);
            this.panelTableContainer.Controls.Add(this.lblTableTitle);
            this.panelTableContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelTableContainer.Location = new System.Drawing.Point(0, 0);
            this.panelTableContainer.Name = "panelTableContainer";
            this.panelTableContainer.Padding = new System.Windows.Forms.Padding(6);
            this.panelTableContainer.Size = new System.Drawing.Size(860, 368);
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
            this.gridCalculations.Location = new System.Drawing.Point(6, 34);
            this.gridCalculations.Name = "gridCalculations";
            this.gridCalculations.ReadOnly = true;
            this.gridCalculations.RowHeadersVisible = false;
            this.gridCalculations.RowTemplate.Height = 30;
            this.gridCalculations.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridCalculations.Size = new System.Drawing.Size(848, 328);
            this.gridCalculations.TabIndex = 1;
            // 
            // colParamName
            // 
            this.colParamName.FillWeight = 28F;
            this.colParamName.HeaderText = "Parameter";
            this.colParamName.Name = "colParamName";
            this.colParamName.ReadOnly = true;
            // 
            // colSymbol
            // 
            this.colSymbol.FillWeight = 16F;
            this.colSymbol.HeaderText = "Symbol (LaTeX)";
            this.colSymbol.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.colSymbol.Name = "colSymbol";
            this.colSymbol.ReadOnly = true;
            this.colSymbol.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            // 
            // colFormula
            // 
            this.colFormula.FillWeight = 34F;
            this.colFormula.HeaderText = "Source Formula (LaTeX)";
            this.colFormula.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.colFormula.Name = "colFormula";
            this.colFormula.ReadOnly = true;
            this.colFormula.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            // 
            // colValue
            // 
            this.colValue.FillWeight = 14F;
            this.colValue.HeaderText = "Value";
            this.colValue.Name = "colValue";
            this.colValue.ReadOnly = true;
            // 
            // colUnit
            // 
            this.colUnit.FillWeight = 8F;
            this.colUnit.HeaderText = "Unit";
            this.colUnit.Name = "colUnit";
            this.colUnit.ReadOnly = true;
            // 
            // lblTableTitle
            // 
            this.lblTableTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTableTitle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblTableTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(55)))), ((int)(((byte)(72)))));
            this.lblTableTitle.Location = new System.Drawing.Point(6, 6);
            this.lblTableTitle.Name = "lblTableTitle";
            this.lblTableTitle.Size = new System.Drawing.Size(848, 28);
            this.lblTableTitle.TabIndex = 0;
            this.lblTableTitle.Text = "Calculated Gear Parameters (Elements of Metric Gear Technology)";
            this.lblTableTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // AlibreGearForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 840);
            this.Controls.Add(this.splitContainerMain);
            this.Controls.Add(this.panelInputs);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.MinimumSize = new System.Drawing.Size(950, 650);
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

        private System.Windows.Forms.Panel panelInputs;
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
