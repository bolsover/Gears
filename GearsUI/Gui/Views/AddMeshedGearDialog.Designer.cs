using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Bolsover.Gears.Gui.Views
{
    partial class AddMeshedGearDialog
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
            this.grpSharedParams = new System.Windows.Forms.GroupBox();
            this.lblModule = new System.Windows.Forms.Label();
            this.lblModuleVal = new System.Windows.Forms.Label();
            this.lblPressureAngle = new System.Windows.Forms.Label();
            this.lblPressureAngleVal = new System.Windows.Forms.Label();
            this.grpGear2Params = new System.Windows.Forms.GroupBox();
            this.lblTeeth = new System.Windows.Forms.Label();
            this.numTeeth = new System.Windows.Forms.NumericUpDown();
            this.lblClearance = new System.Windows.Forms.Label();
            this.numClearance = new System.Windows.Forms.NumericUpDown();
            this.lblProfileShift = new System.Windows.Forms.Label();
            this.numProfileShift = new System.Windows.Forms.NumericUpDown();
            this.lblFaceWidth = new System.Windows.Forms.Label();
            this.numFaceWidth = new System.Windows.Forms.NumericUpDown();
            this.chkInternal = new System.Windows.Forms.CheckBox();
            this.lblRimThickness = new System.Windows.Forms.Label();
            this.numRimThickness = new System.Windows.Forms.NumericUpDown();
            this.chkOptimalShift = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.grpSharedParams.SuspendLayout();
            this.grpGear2Params.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTeeth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numClearance)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numProfileShift)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFaceWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRimThickness)).BeginInit();
            this.SuspendLayout();
            // 
            // grpSharedParams
            // 
            this.grpSharedParams.Controls.Add(this.lblModule);
            this.grpSharedParams.Controls.Add(this.lblModuleVal);
            this.grpSharedParams.Controls.Add(this.lblPressureAngle);
            this.grpSharedParams.Controls.Add(this.lblPressureAngleVal);
            this.grpSharedParams.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.grpSharedParams.Location = new System.Drawing.Point(12, 12);
            this.grpSharedParams.Name = "grpSharedParams";
            this.grpSharedParams.Size = new System.Drawing.Size(326, 75);
            this.grpSharedParams.TabIndex = 0;
            this.grpSharedParams.TabStop = false;
            this.grpSharedParams.Text = "Mating Parameters (from Gear 1)";
            // 
            // lblModule
            // 
            this.lblModule.AutoSize = true;
            this.lblModule.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblModule.Location = new System.Drawing.Point(12, 24);
            this.lblModule.Name = "lblModule";
            this.lblModule.Size = new System.Drawing.Size(86, 19);
            this.lblModule.TabIndex = 0;
            this.lblModule.Text = "Module (m):";
            // 
            // lblModuleVal
            // 
            this.lblModuleVal.AutoSize = true;
            this.lblModuleVal.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblModuleVal.Location = new System.Drawing.Point(175, 24);
            this.lblModuleVal.Name = "lblModuleVal";
            this.lblModuleVal.Size = new System.Drawing.Size(47, 19);
            this.lblModuleVal.TabIndex = 1;
            this.lblModuleVal.Text = "2 mm";
            // 
            // lblPressureAngle
            // 
            this.lblPressureAngle.AutoSize = true;
            this.lblPressureAngle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblPressureAngle.Location = new System.Drawing.Point(12, 48);
            this.lblPressureAngle.Name = "lblPressureAngle";
            this.lblPressureAngle.Size = new System.Drawing.Size(130, 19);
            this.lblPressureAngle.TabIndex = 2;
            this.lblPressureAngle.Text = "Pressure Angle (α):";
            // 
            // lblPressureAngleVal
            // 
            this.lblPressureAngleVal.AutoSize = true;
            this.lblPressureAngleVal.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblPressureAngleVal.Location = new System.Drawing.Point(175, 48);
            this.lblPressureAngleVal.Name = "lblPressureAngleVal";
            this.lblPressureAngleVal.Size = new System.Drawing.Size(32, 19);
            this.lblPressureAngleVal.TabIndex = 3;
            this.lblPressureAngleVal.Text = "20°";
            // 
            // grpGear2Params
            // 
            this.grpGear2Params.Controls.Add(this.lblTeeth);
            this.grpGear2Params.Controls.Add(this.numTeeth);
            this.grpGear2Params.Controls.Add(this.lblClearance);
            this.grpGear2Params.Controls.Add(this.numClearance);
            this.grpGear2Params.Controls.Add(this.lblProfileShift);
            this.grpGear2Params.Controls.Add(this.numProfileShift);
            this.grpGear2Params.Controls.Add(this.lblFaceWidth);
            this.grpGear2Params.Controls.Add(this.numFaceWidth);
            this.grpGear2Params.Controls.Add(this.chkInternal);
            this.grpGear2Params.Controls.Add(this.lblRimThickness);
            this.grpGear2Params.Controls.Add(this.numRimThickness);
            this.grpGear2Params.Controls.Add(this.chkOptimalShift);
            this.grpGear2Params.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.grpGear2Params.Location = new System.Drawing.Point(12, 95);
            this.grpGear2Params.Name = "grpGear2Params";
            this.grpGear2Params.Size = new System.Drawing.Size(326, 265);
            this.grpGear2Params.TabIndex = 1;
            this.grpGear2Params.TabStop = false;
            this.grpGear2Params.Text = "Second Meshed Gear Details";
            // 
            // lblTeeth
            // 
            this.lblTeeth.AutoSize = true;
            this.lblTeeth.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblTeeth.Location = new System.Drawing.Point(12, 26);
            this.lblTeeth.Name = "lblTeeth";
            this.lblTeeth.Size = new System.Drawing.Size(124, 19);
            this.lblTeeth.TabIndex = 0;
            this.lblTeeth.Text = "Number of teeth (z₂)";
            // 
            // numTeeth
            // 
            this.numTeeth.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.numTeeth.Location = new System.Drawing.Point(190, 24);
            this.numTeeth.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numTeeth.Minimum = new decimal(new int[] { 3, 0, 0, 0 });
            this.numTeeth.Name = "numTeeth";
            this.numTeeth.Size = new System.Drawing.Size(120, 26);
            this.numTeeth.TabIndex = 1;
            this.numTeeth.Value = new decimal(new int[] { 24, 0, 0, 0 });
            // 
            // lblClearance
            // 
            this.lblClearance.AutoSize = true;
            this.lblClearance.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblClearance.Location = new System.Drawing.Point(12, 58);
            this.lblClearance.Name = "lblClearance";
            this.lblClearance.Size = new System.Drawing.Size(147, 19);
            this.lblClearance.TabIndex = 2;
            this.lblClearance.Text = "Clearance c₂ (mm)";
            // 
            // numClearance
            // 
            this.numClearance.DecimalPlaces = 3;
            this.numClearance.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.numClearance.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numClearance.Location = new System.Drawing.Point(190, 56);
            this.numClearance.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numClearance.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.numClearance.Name = "numClearance";
            this.numClearance.Size = new System.Drawing.Size(120, 26);
            this.numClearance.TabIndex = 3;
            this.numClearance.Value = new decimal(new int[] { 5, 0, 0, 65536 });
            // 
            // lblProfileShift
            // 
            this.lblProfileShift.AutoSize = true;
            this.lblProfileShift.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblProfileShift.Location = new System.Drawing.Point(12, 90);
            this.lblProfileShift.Name = "lblProfileShift";
            this.lblProfileShift.Size = new System.Drawing.Size(157, 19);
            this.lblProfileShift.TabIndex = 4;
            this.lblProfileShift.Text = "Profile shift coeff. (x₂)";
            // 
            // numProfileShift
            // 
            this.numProfileShift.DecimalPlaces = 3;
            this.numProfileShift.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.numProfileShift.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            this.numProfileShift.Location = new System.Drawing.Point(190, 88);
            this.numProfileShift.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            this.numProfileShift.Minimum = new decimal(new int[] { 5, 0, 0, -2147483648 });
            this.numProfileShift.Name = "numProfileShift";
            this.numProfileShift.Size = new System.Drawing.Size(120, 26);
            this.numProfileShift.TabIndex = 5;
            // 
            // lblFaceWidth
            // 
            this.lblFaceWidth.AutoSize = true;
            this.lblFaceWidth.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblFaceWidth.Location = new System.Drawing.Point(12, 122);
            this.lblFaceWidth.Name = "lblFaceWidth";
            this.lblFaceWidth.Size = new System.Drawing.Size(147, 19);
            this.lblFaceWidth.TabIndex = 6;
            this.lblFaceWidth.Text = "Face width b₂ (mm)";
            // 
            // numFaceWidth
            // 
            this.numFaceWidth.DecimalPlaces = 2;
            this.numFaceWidth.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.numFaceWidth.Location = new System.Drawing.Point(190, 120);
            this.numFaceWidth.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numFaceWidth.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numFaceWidth.Name = "numFaceWidth";
            this.numFaceWidth.Size = new System.Drawing.Size(120, 26);
            this.numFaceWidth.TabIndex = 7;
            this.numFaceWidth.Value = new decimal(new int[] { 15, 0, 0, 0 });
            // 
            // chkInternal
            // 
            this.chkInternal.AutoSize = true;
            this.chkInternal.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.chkInternal.Location = new System.Drawing.Point(15, 154);
            this.chkInternal.Name = "chkInternal";
            this.chkInternal.Size = new System.Drawing.Size(111, 23);
            this.chkInternal.TabIndex = 8;
            this.chkInternal.Text = "Internal gear";
            this.chkInternal.UseVisualStyleBackColor = true;
            // 
            // lblRimThickness
            // 
            this.lblRimThickness.AutoSize = true;
            this.lblRimThickness.Enabled = false;
            this.lblRimThickness.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblRimThickness.Location = new System.Drawing.Point(12, 184);
            this.lblRimThickness.Name = "lblRimThickness";
            this.lblRimThickness.Size = new System.Drawing.Size(155, 19);
            this.lblRimThickness.TabIndex = 9;
            this.lblRimThickness.Text = "Rim thickness (mm)";
            // 
            // numRimThickness
            // 
            this.numRimThickness.DecimalPlaces = 2;
            this.numRimThickness.Enabled = false;
            this.numRimThickness.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.numRimThickness.Location = new System.Drawing.Point(190, 182);
            this.numRimThickness.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numRimThickness.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numRimThickness.Name = "numRimThickness";
            this.numRimThickness.Size = new System.Drawing.Size(120, 26);
            this.numRimThickness.TabIndex = 10;
            this.numRimThickness.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // chkOptimalShift
            // 
            this.chkOptimalShift.AutoSize = true;
            this.chkOptimalShift.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.chkOptimalShift.Location = new System.Drawing.Point(15, 219);
            this.chkOptimalShift.Name = "chkOptimalShift";
            this.chkOptimalShift.Size = new System.Drawing.Size(217, 23);
            this.chkOptimalShift.TabIndex = 11;
            this.chkOptimalShift.Text = "Optimal shift (anti-undercut)";
            this.chkOptimalShift.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.BackColor = System.Drawing.Color.SteelBlue;
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOK.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnOK.ForeColor = System.Drawing.Color.White;
            this.btnOK.Location = new System.Drawing.Point(128, 372);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(100, 32);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = false;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnCancel.Location = new System.Drawing.Point(238, 372);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 32);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // AddMeshedGearDialog
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(350, 416);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.grpGear2Params);
            this.Controls.Add(this.grpSharedParams);
            this.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddMeshedGearDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Configure Meshed Gear";
            this.grpSharedParams.ResumeLayout(false);
            this.grpSharedParams.PerformLayout();
            this.grpGear2Params.ResumeLayout(false);
            this.grpGear2Params.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTeeth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numClearance)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numProfileShift)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFaceWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRimThickness)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private GroupBox grpSharedParams;
        private Label lblModule;
        private Label lblModuleVal;
        private Label lblPressureAngle;
        private Label lblPressureAngleVal;
        private GroupBox grpGear2Params;
        private Label lblTeeth;
        private NumericUpDown numTeeth;
        private Label lblClearance;
        private NumericUpDown numClearance;
        private Label lblProfileShift;
        private NumericUpDown numProfileShift;
        private Label lblFaceWidth;
        private NumericUpDown numFaceWidth;
        private CheckBox chkInternal;
        private Label lblRimThickness;
        private NumericUpDown numRimThickness;
        private CheckBox chkOptimalShift;
        private Button btnOK;
        private Button btnCancel;
    }
}
