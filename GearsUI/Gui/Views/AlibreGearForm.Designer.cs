using System.ComponentModel;
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
            this.panelInputs = new System.Windows.Forms.Panel();
            this.lblTeeth = new System.Windows.Forms.Label();
            this.numTeeth = new System.Windows.Forms.NumericUpDown();
            this.lblModule = new System.Windows.Forms.Label();
            this.numModule = new System.Windows.Forms.NumericUpDown();
            this.lblPressureAngle = new System.Windows.Forms.Label();
            this.numPressureAngle = new System.Windows.Forms.NumericUpDown();
            this.lblThickness = new System.Windows.Forms.Label();
            this.numThickness = new System.Windows.Forms.NumericUpDown();
            this.lblProfileShift = new System.Windows.Forms.Label();
            this.numProfileShift = new System.Windows.Forms.NumericUpDown();
            this.chkOptimalShift = new System.Windows.Forms.CheckBox();
            this.chkInternal = new System.Windows.Forms.CheckBox();
            this.chkFullGear = new System.Windows.Forms.CheckBox();
            this.btnExportDxf = new System.Windows.Forms.Button();
            this.btnExportCsv = new System.Windows.Forms.Button();
            this.lblResultsHeader = new System.Windows.Forms.Label();
            this.lblPitch = new System.Windows.Forms.Label();
            this.lblBase = new System.Windows.Forms.Label();
            this.lblAddendum = new System.Windows.Forms.Label();
            this.lblDedendum = new System.Windows.Forms.Label();
            this.lblError = new System.Windows.Forms.Label();
            this.preview = new Bolsover.Gears.Gui.AlibreGearPreviewControl();
            this.panelInputs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTeeth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numModule)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPressureAngle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numThickness)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numProfileShift)).BeginInit();
            this.SuspendLayout();
            //
            // panelInputs
            //
            this.panelInputs.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelInputs.Controls.Add(this.lblTeeth);
            this.panelInputs.Controls.Add(this.numTeeth);
            this.panelInputs.Controls.Add(this.lblModule);
            this.panelInputs.Controls.Add(this.numModule);
            this.panelInputs.Controls.Add(this.lblPressureAngle);
            this.panelInputs.Controls.Add(this.numPressureAngle);
            this.panelInputs.Controls.Add(this.lblThickness);
            this.panelInputs.Controls.Add(this.numThickness);
            this.panelInputs.Controls.Add(this.lblProfileShift);
            this.panelInputs.Controls.Add(this.numProfileShift);
            this.panelInputs.Controls.Add(this.chkOptimalShift);
            this.panelInputs.Controls.Add(this.chkInternal);
            this.panelInputs.Controls.Add(this.chkFullGear);
            this.panelInputs.Controls.Add(this.btnExportDxf);
            this.panelInputs.Controls.Add(this.btnExportCsv);
            this.panelInputs.Controls.Add(this.lblResultsHeader);
            this.panelInputs.Controls.Add(this.lblPitch);
            this.panelInputs.Controls.Add(this.lblBase);
            this.panelInputs.Controls.Add(this.lblAddendum);
            this.panelInputs.Controls.Add(this.lblDedendum);
            this.panelInputs.Controls.Add(this.lblError);
            this.panelInputs.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelInputs.Location = new System.Drawing.Point(0, 0);
            this.panelInputs.Name = "panelInputs";
            this.panelInputs.Padding = new System.Windows.Forms.Padding(12);
            this.panelInputs.Size = new System.Drawing.Size(300, 720);
            this.panelInputs.TabIndex = 0;
            //
            // lblTeeth
            //
            this.lblTeeth.AutoSize = true;
            this.lblTeeth.Location = new System.Drawing.Point(15, 18);
            this.lblTeeth.Name = "lblTeeth";
            this.lblTeeth.Size = new System.Drawing.Size(120, 20);
            this.lblTeeth.Text = "Number of teeth";
            //
            // numTeeth
            //
            this.numTeeth.Location = new System.Drawing.Point(160, 15);
            this.numTeeth.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numTeeth.Minimum = new decimal(new int[] { 3, 0, 0, 0 });
            this.numTeeth.Name = "numTeeth";
            this.numTeeth.Size = new System.Drawing.Size(110, 26);
            this.numTeeth.TabIndex = 0;
            this.numTeeth.Value = new decimal(new int[] { 20, 0, 0, 0 });
            //
            // lblModule
            //
            this.lblModule.AutoSize = true;
            this.lblModule.Location = new System.Drawing.Point(15, 52);
            this.lblModule.Name = "lblModule";
            this.lblModule.Size = new System.Drawing.Size(90, 20);
            this.lblModule.Text = "Module (mm)";
            //
            // numModule
            //
            this.numModule.DecimalPlaces = 3;
            this.numModule.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numModule.Location = new System.Drawing.Point(160, 49);
            this.numModule.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numModule.Minimum = new decimal(new int[] { 1, 0, 0, 196608 });
            this.numModule.Name = "numModule";
            this.numModule.Size = new System.Drawing.Size(110, 26);
            this.numModule.TabIndex = 1;
            this.numModule.Value = new decimal(new int[] { 2, 0, 0, 0 });
            //
            // lblPressureAngle
            //
            this.lblPressureAngle.AutoSize = true;
            this.lblPressureAngle.Location = new System.Drawing.Point(15, 86);
            this.lblPressureAngle.Name = "lblPressureAngle";
            this.lblPressureAngle.Size = new System.Drawing.Size(130, 20);
            this.lblPressureAngle.Text = "Pressure angle (°)";
            //
            // numPressureAngle
            //
            this.numPressureAngle.DecimalPlaces = 2;
            this.numPressureAngle.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            this.numPressureAngle.Location = new System.Drawing.Point(160, 83);
            this.numPressureAngle.Maximum = new decimal(new int[] { 45, 0, 0, 0 });
            this.numPressureAngle.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
            this.numPressureAngle.Name = "numPressureAngle";
            this.numPressureAngle.Size = new System.Drawing.Size(110, 26);
            this.numPressureAngle.TabIndex = 2;
            this.numPressureAngle.Value = new decimal(new int[] { 20, 0, 0, 0 });
            //
            // lblThickness
            //
            this.lblThickness.AutoSize = true;
            this.lblThickness.Location = new System.Drawing.Point(15, 120);
            this.lblThickness.Name = "lblThickness";
            this.lblThickness.Size = new System.Drawing.Size(110, 20);
            this.lblThickness.Text = "Thickness (mm)";
            //
            // numThickness
            //
            this.numThickness.DecimalPlaces = 2;
            this.numThickness.Location = new System.Drawing.Point(160, 117);
            this.numThickness.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numThickness.Name = "numThickness";
            this.numThickness.Size = new System.Drawing.Size(110, 26);
            this.numThickness.TabIndex = 3;
            this.numThickness.Value = new decimal(new int[] { 10, 0, 0, 0 });
            //
            // lblProfileShift
            //
            this.lblProfileShift.AutoSize = true;
            this.lblProfileShift.Location = new System.Drawing.Point(15, 154);
            this.lblProfileShift.Name = "lblProfileShift";
            this.lblProfileShift.Size = new System.Drawing.Size(120, 20);
            this.lblProfileShift.Text = "Profile shift (mm)";
            //
            // numProfileShift
            //
            this.numProfileShift.DecimalPlaces = 3;
            this.numProfileShift.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numProfileShift.Location = new System.Drawing.Point(160, 151);
            this.numProfileShift.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numProfileShift.Minimum = new decimal(new int[] { 100, 0, 0, -2147483648 });
            this.numProfileShift.Name = "numProfileShift";
            this.numProfileShift.Size = new System.Drawing.Size(110, 26);
            this.numProfileShift.TabIndex = 4;
            //
            // chkOptimalShift
            //
            this.chkOptimalShift.AutoSize = true;
            this.chkOptimalShift.Location = new System.Drawing.Point(15, 190);
            this.chkOptimalShift.Name = "chkOptimalShift";
            this.chkOptimalShift.Size = new System.Drawing.Size(200, 24);
            this.chkOptimalShift.TabIndex = 5;
            this.chkOptimalShift.Text = "Optimal profile shift (undercut)";
            //
            // chkInternal
            //
            this.chkInternal.AutoSize = true;
            this.chkInternal.Location = new System.Drawing.Point(15, 220);
            this.chkInternal.Name = "chkInternal";
            this.chkInternal.Size = new System.Drawing.Size(110, 24);
            this.chkInternal.TabIndex = 6;
            this.chkInternal.Text = "Internal gear";
            //
            // chkFullGear
            //
            this.chkFullGear.AutoSize = true;
            this.chkFullGear.Checked = true;
            this.chkFullGear.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFullGear.Location = new System.Drawing.Point(15, 250);
            this.chkFullGear.Name = "chkFullGear";
            this.chkFullGear.Size = new System.Drawing.Size(130, 24);
            this.chkFullGear.TabIndex = 7;
            this.chkFullGear.Text = "Show full gear";
            //
            // btnExportDxf
            //
            this.btnExportDxf.Location = new System.Drawing.Point(15, 290);
            this.btnExportDxf.Name = "btnExportDxf";
            this.btnExportDxf.Size = new System.Drawing.Size(125, 32);
            this.btnExportDxf.TabIndex = 8;
            this.btnExportDxf.Text = "Export DXF…";
            this.btnExportDxf.UseVisualStyleBackColor = true;
            //
            // btnExportCsv
            //
            this.btnExportCsv.Location = new System.Drawing.Point(147, 290);
            this.btnExportCsv.Name = "btnExportCsv";
            this.btnExportCsv.Size = new System.Drawing.Size(123, 32);
            this.btnExportCsv.TabIndex = 9;
            this.btnExportCsv.Text = "Export CSV…";
            this.btnExportCsv.UseVisualStyleBackColor = true;
            //
            // lblResultsHeader
            //
            this.lblResultsHeader.AutoSize = true;
            this.lblResultsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblResultsHeader.Location = new System.Drawing.Point(15, 340);
            this.lblResultsHeader.Name = "lblResultsHeader";
            this.lblResultsHeader.Size = new System.Drawing.Size(80, 20);
            this.lblResultsHeader.Text = "Radii (mm)";
            //
            // lblPitch
            //
            this.lblPitch.AutoSize = true;
            this.lblPitch.Location = new System.Drawing.Point(15, 366);
            this.lblPitch.Name = "lblPitch";
            this.lblPitch.Size = new System.Drawing.Size(80, 20);
            this.lblPitch.Text = "Pitch: –";
            //
            // lblBase
            //
            this.lblBase.AutoSize = true;
            this.lblBase.Location = new System.Drawing.Point(15, 390);
            this.lblBase.Name = "lblBase";
            this.lblBase.Size = new System.Drawing.Size(80, 20);
            this.lblBase.Text = "Base: –";
            //
            // lblAddendum
            //
            this.lblAddendum.AutoSize = true;
            this.lblAddendum.Location = new System.Drawing.Point(15, 414);
            this.lblAddendum.Name = "lblAddendum";
            this.lblAddendum.Size = new System.Drawing.Size(80, 20);
            this.lblAddendum.Text = "Addendum: –";
            //
            // lblDedendum
            //
            this.lblDedendum.AutoSize = true;
            this.lblDedendum.Location = new System.Drawing.Point(15, 438);
            this.lblDedendum.Name = "lblDedendum";
            this.lblDedendum.Size = new System.Drawing.Size(80, 20);
            this.lblDedendum.Text = "Dedendum: –";
            //
            // lblError
            //
            this.lblError.ForeColor = System.Drawing.Color.Firebrick;
            this.lblError.Location = new System.Drawing.Point(15, 480);
            this.lblError.Name = "lblError";
            this.lblError.Size = new System.Drawing.Size(270, 100);
            this.lblError.TabIndex = 10;
            //
            // preview
            //
            this.preview.BackColor = System.Drawing.Color.White;
            this.preview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.preview.Location = new System.Drawing.Point(300, 0);
            this.preview.Name = "preview";
            this.preview.Size = new System.Drawing.Size(700, 720);
            this.preview.TabIndex = 1;
            //
            // AlibreGearForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 720);
            this.Controls.Add(this.preview);
            this.Controls.Add(this.panelInputs);
            this.Name = "AlibreGearForm";
            this.Text = "Alibre Gear Generator";
            this.panelInputs.ResumeLayout(false);
            this.panelInputs.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTeeth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numModule)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPressureAngle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numThickness)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numProfileShift)).EndInit();
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
        private System.Windows.Forms.Label lblThickness;
        private System.Windows.Forms.NumericUpDown numThickness;
        private System.Windows.Forms.Label lblProfileShift;
        private System.Windows.Forms.NumericUpDown numProfileShift;
        private System.Windows.Forms.CheckBox chkOptimalShift;
        private System.Windows.Forms.CheckBox chkInternal;
        private System.Windows.Forms.CheckBox chkFullGear;
        private System.Windows.Forms.Button btnExportDxf;
        private System.Windows.Forms.Button btnExportCsv;
        private System.Windows.Forms.Label lblResultsHeader;
        private System.Windows.Forms.Label lblPitch;
        private System.Windows.Forms.Label lblBase;
        private System.Windows.Forms.Label lblAddendum;
        private System.Windows.Forms.Label lblDedendum;
        private System.Windows.Forms.Label lblError;
        private Bolsover.Gears.Gui.AlibreGearPreviewControl preview;
    }
}
