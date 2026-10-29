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
            this.panelInputs = new System.Windows.Forms.Panel();
            this.lblTeeth = new System.Windows.Forms.Label();
            this.numTeeth = new System.Windows.Forms.NumericUpDown();
            this.lblModule = new System.Windows.Forms.Label();
            this.numModule = new System.Windows.Forms.NumericUpDown();
            this.lblPressureAngle = new System.Windows.Forms.Label();
            this.numPressureAngle = new System.Windows.Forms.NumericUpDown();
            this.lblClearance = new System.Windows.Forms.Label();
            this.numClearance = new System.Windows.Forms.NumericUpDown();
            this.lblFaceWidth = new System.Windows.Forms.Label();
            this.numFaceWidth = new System.Windows.Forms.NumericUpDown();
            this.lblProfileShift = new System.Windows.Forms.Label();
            this.numProfileShift = new System.Windows.Forms.NumericUpDown();
            this.lblRimThickness = new System.Windows.Forms.Label();
            this.numRimThickness = new System.Windows.Forms.NumericUpDown();
            this.chkInternal = new System.Windows.Forms.CheckBox();
            this.chkOptimalShift = new System.Windows.Forms.CheckBox();
            this.chkFullGear = new System.Windows.Forms.CheckBox();
            this.btnAddMeshedGear = new System.Windows.Forms.Button();
            this.btnRemoveMeshedGear = new System.Windows.Forms.Button();
            this.grpViewMode = new System.Windows.Forms.GroupBox();
            this.rb3DWireframe = new System.Windows.Forms.RadioButton();
            this.rb3D = new System.Windows.Forms.RadioButton();
            this.rb2D = new System.Windows.Forms.RadioButton();
            this.chkShowCircles = new System.Windows.Forms.CheckBox();
            this.btnCalculate = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnResetView = new System.Windows.Forms.Button();
            this.btnExportDxf = new System.Windows.Forms.Button();
            this.btnExportCsv = new System.Windows.Forms.Button();
            this.lblResultsHeader = new System.Windows.Forms.Label();
            this.lblPitch = new System.Windows.Forms.Label();
            this.lblBase = new System.Windows.Forms.Label();
            this.lblAddendum = new System.Windows.Forms.Label();
            this.lblDedendum = new System.Windows.Forms.Label();
            this.lblClearanceVal = new System.Windows.Forms.Label();
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
            this.panelInputs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTeeth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numModule)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPressureAngle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numClearance)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFaceWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numProfileShift)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRimThickness)).BeginInit();
            this.grpViewMode.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            this.panelTableContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridCalculations)).BeginInit();
            this.SuspendLayout();
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
            this.panelInputs.Controls.Add(this.lblClearance);
            this.panelInputs.Controls.Add(this.numClearance);
            this.panelInputs.Controls.Add(this.lblFaceWidth);
            this.panelInputs.Controls.Add(this.numFaceWidth);
            this.panelInputs.Controls.Add(this.lblProfileShift);
            this.panelInputs.Controls.Add(this.numProfileShift);
            this.panelInputs.Controls.Add(this.lblRimThickness);
            this.panelInputs.Controls.Add(this.numRimThickness);
            this.panelInputs.Controls.Add(this.chkInternal);
            this.panelInputs.Controls.Add(this.chkOptimalShift);
            this.panelInputs.Controls.Add(this.chkFullGear);
            this.panelInputs.Controls.Add(this.btnAddMeshedGear);
            this.panelInputs.Controls.Add(this.btnRemoveMeshedGear);
            this.panelInputs.Controls.Add(this.grpViewMode);
            this.panelInputs.Controls.Add(this.chkShowCircles);
            this.panelInputs.Controls.Add(this.btnCalculate);
            this.panelInputs.Controls.Add(this.progressBar);
            this.panelInputs.Controls.Add(this.lblStatus);
            this.panelInputs.Controls.Add(this.btnResetView);
            this.panelInputs.Controls.Add(this.btnExportDxf);
            this.panelInputs.Controls.Add(this.btnExportCsv);
            this.panelInputs.Controls.Add(this.lblResultsHeader);
            this.panelInputs.Controls.Add(this.lblPitch);
            this.panelInputs.Controls.Add(this.lblBase);
            this.panelInputs.Controls.Add(this.lblAddendum);
            this.panelInputs.Controls.Add(this.lblDedendum);
            this.panelInputs.Controls.Add(this.lblClearanceVal);
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
            // lblClearance
            // 
            this.lblClearance.AutoSize = true;
            this.lblClearance.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblClearance.Location = new System.Drawing.Point(12, 113);
            this.lblClearance.Name = "lblClearance";
            this.lblClearance.Size = new System.Drawing.Size(138, 19);
            this.lblClearance.TabIndex = 6;
            this.lblClearance.Text = "Clearance c (mm)";
            // 
            // numClearance
            // 
            this.numClearance.DecimalPlaces = 3;
            this.numClearance.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numClearance.Location = new System.Drawing.Point(195, 111);
            this.numClearance.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numClearance.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.numClearance.Name = "numClearance";
            this.numClearance.Size = new System.Drawing.Size(115, 27);
            this.numClearance.TabIndex = 7;
            this.numClearance.Value = new decimal(new int[] { 5, 0, 0, 65536 });
            // 
            // lblFaceWidth
            // 
            this.lblFaceWidth.AutoSize = true;
            this.lblFaceWidth.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblFaceWidth.Location = new System.Drawing.Point(12, 146);
            this.lblFaceWidth.Name = "lblFaceWidth";
            this.lblFaceWidth.Size = new System.Drawing.Size(140, 19);
            this.lblFaceWidth.TabIndex = 8;
            this.lblFaceWidth.Text = "Face width b (mm)";
            // 
            // numFaceWidth
            // 
            this.numFaceWidth.DecimalPlaces = 2;
            this.numFaceWidth.Location = new System.Drawing.Point(195, 144);
            this.numFaceWidth.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numFaceWidth.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numFaceWidth.Name = "numFaceWidth";
            this.numFaceWidth.Size = new System.Drawing.Size(115, 27);
            this.numFaceWidth.TabIndex = 9;
            this.numFaceWidth.Value = new decimal(new int[] { 15, 0, 0, 0 });
            // 
            // lblProfileShift
            // 
            this.lblProfileShift.AutoSize = true;
            this.lblProfileShift.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblProfileShift.Location = new System.Drawing.Point(12, 179);
            this.lblProfileShift.Name = "lblProfileShift";
            this.lblProfileShift.Size = new System.Drawing.Size(150, 19);
            this.lblProfileShift.TabIndex = 10;
            this.lblProfileShift.Text = "Profile shift coeff. (x)";
            // 
            // numProfileShift
            // 
            this.numProfileShift.DecimalPlaces = 3;
            this.numProfileShift.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numProfileShift.Location = new System.Drawing.Point(195, 177);
            this.numProfileShift.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            this.numProfileShift.Minimum = new decimal(new int[] { 5, 0, 0, -2147483648 });
            this.numProfileShift.Name = "numProfileShift";
            this.numProfileShift.Size = new System.Drawing.Size(115, 27);
            this.numProfileShift.TabIndex = 11;
            // 
            // lblRimThickness
            // 
            this.lblRimThickness.AutoSize = true;
            this.lblRimThickness.Enabled = false;
            this.lblRimThickness.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblRimThickness.Location = new System.Drawing.Point(12, 212);
            this.lblRimThickness.Name = "lblRimThickness";
            this.lblRimThickness.Size = new System.Drawing.Size(155, 19);
            this.lblRimThickness.TabIndex = 12;
            this.lblRimThickness.Text = "Rim thickness (mm)";
            // 
            // numRimThickness
            // 
            this.numRimThickness.DecimalPlaces = 2;
            this.numRimThickness.Enabled = false;
            this.numRimThickness.Location = new System.Drawing.Point(195, 210);
            this.numRimThickness.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numRimThickness.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numRimThickness.Name = "numRimThickness";
            this.numRimThickness.Size = new System.Drawing.Size(115, 27);
            this.numRimThickness.TabIndex = 13;
            this.numRimThickness.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // chkInternal
            // 
            this.chkInternal.AutoSize = true;
            this.chkInternal.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkInternal.Location = new System.Drawing.Point(15, 245);
            this.chkInternal.Name = "chkInternal";
            this.chkInternal.Size = new System.Drawing.Size(114, 24);
            this.chkInternal.TabIndex = 14;
            this.chkInternal.Text = "Internal gear";
            this.chkInternal.UseVisualStyleBackColor = true;
            // 
            // chkOptimalShift
            // 
            this.chkOptimalShift.AutoSize = true;
            this.chkOptimalShift.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkOptimalShift.Location = new System.Drawing.Point(15, 271);
            this.chkOptimalShift.Name = "chkOptimalShift";
            this.chkOptimalShift.Size = new System.Drawing.Size(225, 24);
            this.chkOptimalShift.TabIndex = 15;
            this.chkOptimalShift.Text = "Optimal shift (anti-undercut)";
            this.chkOptimalShift.UseVisualStyleBackColor = true;
            // 
            // chkFullGear
            // 
            this.chkFullGear.AutoSize = true;
            this.chkFullGear.Checked = true;
            this.chkFullGear.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFullGear.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkFullGear.Location = new System.Drawing.Point(15, 297);
            this.chkFullGear.Name = "chkFullGear";
            this.chkFullGear.Size = new System.Drawing.Size(149, 24);
            this.chkFullGear.TabIndex = 16;
            this.chkFullGear.Text = "Show full gear profile";
            this.chkFullGear.UseVisualStyleBackColor = true;
            // 
            // btnAddMeshedGear
            // 
            this.btnAddMeshedGear.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.btnAddMeshedGear.Location = new System.Drawing.Point(12, 327);
            this.btnAddMeshedGear.Name = "btnAddMeshedGear";
            this.btnAddMeshedGear.Size = new System.Drawing.Size(142, 32);
            this.btnAddMeshedGear.TabIndex = 17;
            this.btnAddMeshedGear.Text = "Add Meshed Gear…";
            this.btnAddMeshedGear.UseVisualStyleBackColor = true;
            // 
            // btnRemoveMeshedGear
            // 
            this.btnRemoveMeshedGear.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.btnRemoveMeshedGear.Location = new System.Drawing.Point(168, 327);
            this.btnRemoveMeshedGear.Name = "btnRemoveMeshedGear";
            this.btnRemoveMeshedGear.Size = new System.Drawing.Size(142, 32);
            this.btnRemoveMeshedGear.TabIndex = 18;
            this.btnRemoveMeshedGear.Text = "Remove Meshed";
            this.btnRemoveMeshedGear.UseVisualStyleBackColor = true;
            this.btnRemoveMeshedGear.Visible = false;
            // 
            // grpViewMode
            // 
            this.grpViewMode.Controls.Add(this.rb3DWireframe);
            this.grpViewMode.Controls.Add(this.rb3D);
            this.grpViewMode.Controls.Add(this.rb2D);
            this.grpViewMode.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.grpViewMode.Location = new System.Drawing.Point(12, 367);
            this.grpViewMode.Name = "grpViewMode";
            this.grpViewMode.Size = new System.Drawing.Size(298, 56);
            this.grpViewMode.TabIndex = 19;
            this.grpViewMode.TabStop = false;
            this.grpViewMode.Text = "Visualisation Mode";
            // 
            // rb2D
            // 
            this.rb2D.AutoSize = true;
            this.rb2D.Checked = true;
            this.rb2D.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.rb2D.Location = new System.Drawing.Point(8, 22);
            this.rb2D.Name = "rb2D";
            this.rb2D.Size = new System.Drawing.Size(84, 23);
            this.rb2D.TabIndex = 0;
            this.rb2D.TabStop = true;
            this.rb2D.Text = "2D Profile";
            this.rb2D.UseVisualStyleBackColor = true;
            // 
            // rb3D
            // 
            this.rb3D.AutoSize = true;
            this.rb3D.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.rb3D.Location = new System.Drawing.Point(96, 22);
            this.rb3D.Name = "rb3D";
            this.rb3D.Size = new System.Drawing.Size(79, 23);
            this.rb3D.TabIndex = 1;
            this.rb3D.Text = "3D Solid";
            this.rb3D.UseVisualStyleBackColor = true;
            // 
            // rb3DWireframe
            // 
            this.rb3DWireframe.AutoSize = true;
            this.rb3DWireframe.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.rb3DWireframe.Location = new System.Drawing.Point(180, 22);
            this.rb3DWireframe.Name = "rb3DWireframe";
            this.rb3DWireframe.Size = new System.Drawing.Size(112, 23);
            this.rb3DWireframe.TabIndex = 2;
            this.rb3DWireframe.Text = "3D Wireframe";
            this.rb3DWireframe.UseVisualStyleBackColor = true;
            // 
            // chkShowCircles
            // 
            this.chkShowCircles.AutoSize = true;
            this.chkShowCircles.Checked = true;
            this.chkShowCircles.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowCircles.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkShowCircles.Location = new System.Drawing.Point(15, 429);
            this.chkShowCircles.Name = "chkShowCircles";
            this.chkShowCircles.Size = new System.Drawing.Size(259, 24);
            this.chkShowCircles.TabIndex = 20;
            this.chkShowCircles.Text = "Show reference circles (pitch/base)";
            this.chkShowCircles.UseVisualStyleBackColor = true;
            // 
            // btnCalculate
            // 
            this.btnCalculate.BackColor = System.Drawing.Color.SteelBlue;
            this.btnCalculate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCalculate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCalculate.ForeColor = System.Drawing.Color.White;
            this.btnCalculate.Location = new System.Drawing.Point(12, 461);
            this.btnCalculate.Name = "btnCalculate";
            this.btnCalculate.Size = new System.Drawing.Size(298, 38);
            this.btnCalculate.TabIndex = 21;
            this.btnCalculate.Text = "Execute Calculations";
            this.btnCalculate.UseVisualStyleBackColor = false;
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 505);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(298, 16);
            this.progressBar.TabIndex = 22;
            // 
            // lblStatus
            // 
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this.lblStatus.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblStatus.Location = new System.Drawing.Point(12, 524);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(298, 22);
            this.lblStatus.TabIndex = 23;
            this.lblStatus.Text = "Ready";
            // 
            // btnResetView
            // 
            this.btnResetView.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.btnResetView.Location = new System.Drawing.Point(12, 551);
            this.btnResetView.Name = "btnResetView";
            this.btnResetView.Size = new System.Drawing.Size(298, 30);
            this.btnResetView.TabIndex = 24;
            this.btnResetView.Text = "Reset View / Camera";
            this.btnResetView.UseVisualStyleBackColor = true;
            // 
            // btnExportDxf
            // 
            this.btnExportDxf.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.btnExportDxf.Location = new System.Drawing.Point(12, 587);
            this.btnExportDxf.Name = "btnExportDxf";
            this.btnExportDxf.Size = new System.Drawing.Size(142, 32);
            this.btnExportDxf.TabIndex = 25;
            this.btnExportDxf.Text = "Export DXF…";
            this.btnExportDxf.UseVisualStyleBackColor = true;
            // 
            // btnExportCsv
            // 
            this.btnExportCsv.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.btnExportCsv.Location = new System.Drawing.Point(168, 587);
            this.btnExportCsv.Name = "btnExportCsv";
            this.btnExportCsv.Size = new System.Drawing.Size(142, 32);
            this.btnExportCsv.TabIndex = 26;
            this.btnExportCsv.Text = "Export CSV…";
            this.btnExportCsv.UseVisualStyleBackColor = true;
            // 
            // lblResultsHeader
            // 
            this.lblResultsHeader.AutoSize = true;
            this.lblResultsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblResultsHeader.Location = new System.Drawing.Point(12, 629);
            this.lblResultsHeader.Name = "lblResultsHeader";
            this.lblResultsHeader.Size = new System.Drawing.Size(176, 20);
            this.lblResultsHeader.TabIndex = 27;
            this.lblResultsHeader.Text = "Summary Radii (mm)";
            // 
            // lblPitch
            // 
            this.lblPitch.AutoSize = true;
            this.lblPitch.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblPitch.Location = new System.Drawing.Point(12, 651);
            this.lblPitch.Name = "lblPitch";
            this.lblPitch.Size = new System.Drawing.Size(55, 19);
            this.lblPitch.TabIndex = 28;
            this.lblPitch.Text = "Pitch: –";
            // 
            // lblBase
            // 
            this.lblBase.AutoSize = true;
            this.lblBase.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBase.Location = new System.Drawing.Point(12, 671);
            this.lblBase.Name = "lblBase";
            this.lblBase.Size = new System.Drawing.Size(51, 19);
            this.lblBase.TabIndex = 29;
            this.lblBase.Text = "Base: –";
            // 
            // lblAddendum
            // 
            this.lblAddendum.AutoSize = true;
            this.lblAddendum.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblAddendum.Location = new System.Drawing.Point(12, 691);
            this.lblAddendum.Name = "lblAddendum";
            this.lblAddendum.Size = new System.Drawing.Size(92, 19);
            this.lblAddendum.TabIndex = 30;
            this.lblAddendum.Text = "Addendum: –";
            // 
            // lblDedendum
            // 
            this.lblDedendum.AutoSize = true;
            this.lblDedendum.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblDedendum.Location = new System.Drawing.Point(12, 711);
            this.lblDedendum.Name = "lblDedendum";
            this.lblDedendum.Size = new System.Drawing.Size(91, 19);
            this.lblDedendum.TabIndex = 31;
            this.lblDedendum.Text = "Dedendum: –";
            // 
            // lblClearanceVal
            // 
            this.lblClearanceVal.AutoSize = true;
            this.lblClearanceVal.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblClearanceVal.Location = new System.Drawing.Point(12, 731);
            this.lblClearanceVal.Name = "lblClearanceVal";
            this.lblClearanceVal.Size = new System.Drawing.Size(84, 19);
            this.lblClearanceVal.TabIndex = 32;
            this.lblClearanceVal.Text = "Clearance: –";
            // 
            // lblError
            // 
            this.lblError.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblError.ForeColor = System.Drawing.Color.Firebrick;
            this.lblError.Location = new System.Drawing.Point(12, 755);
            this.lblError.Name = "lblError";
            this.lblError.Size = new System.Drawing.Size(298, 75);
            this.lblError.TabIndex = 33;
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.Location = new System.Drawing.Point(340, 0);
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
            this.splitContainerMain.Size = new System.Drawing.Size(860, 840);
            this.splitContainerMain.SplitterDistance = 460;
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
            this.preview.Size = new System.Drawing.Size(860, 460);
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
            this.panelTableContainer.Size = new System.Drawing.Size(860, 376);
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
            this.gridCalculations.Size = new System.Drawing.Size(848, 336);
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
            this.MinimumSize = new System.Drawing.Size(950, 650);
            this.Name = "AlibreGearForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Involute Gear Design & Visualisation (2D / 3D)";
            this.panelInputs.ResumeLayout(false);
            this.panelInputs.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTeeth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numModule)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPressureAngle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numClearance)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFaceWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numProfileShift)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRimThickness)).EndInit();
            this.grpViewMode.ResumeLayout(false);
            this.grpViewMode.PerformLayout();
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            this.panelTableContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridCalculations)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelInputs;
        private System.Windows.Forms.Label lblTeeth;
        private System.Windows.Forms.NumericUpDown numTeeth;
        private System.Windows.Forms.Label lblModule;
        private System.Windows.Forms.NumericUpDown numModule;
        private System.Windows.Forms.Label lblPressureAngle;
        private System.Windows.Forms.NumericUpDown numPressureAngle;
        private System.Windows.Forms.Label lblClearance;
        private System.Windows.Forms.NumericUpDown numClearance;
        private System.Windows.Forms.Label lblFaceWidth;
        private System.Windows.Forms.NumericUpDown numFaceWidth;
        private System.Windows.Forms.Label lblProfileShift;
        private System.Windows.Forms.NumericUpDown numProfileShift;
        private System.Windows.Forms.Label lblRimThickness;
        private System.Windows.Forms.NumericUpDown numRimThickness;
        private System.Windows.Forms.CheckBox chkOptimalShift;
        private System.Windows.Forms.CheckBox chkInternal;
        private System.Windows.Forms.CheckBox chkFullGear;
        private System.Windows.Forms.Button btnAddMeshedGear;
        private System.Windows.Forms.Button btnRemoveMeshedGear;
        private System.Windows.Forms.GroupBox grpViewMode;
        private System.Windows.Forms.RadioButton rb3DWireframe;
        private System.Windows.Forms.RadioButton rb3D;
        private System.Windows.Forms.RadioButton rb2D;
        private System.Windows.Forms.CheckBox chkShowCircles;
        private System.Windows.Forms.Button btnCalculate;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnResetView;
        private System.Windows.Forms.Button btnExportDxf;
        private System.Windows.Forms.Button btnExportCsv;
        private System.Windows.Forms.Label lblResultsHeader;
        private System.Windows.Forms.Label lblPitch;
        private System.Windows.Forms.Label lblBase;
        private System.Windows.Forms.Label lblAddendum;
        private System.Windows.Forms.Label lblDedendum;
        private System.Windows.Forms.Label lblClearanceVal;
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
