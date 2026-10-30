using System;
using System.Drawing;
using System.Windows.Forms;

namespace Bolsover.Gears.Gui.Views
{
    /// <summary>
    /// Dialog for configuring a second meshed gear (Gear 2) in the gear pair design.
    /// </summary>
    public partial class AddMeshedGearDialog : Form
    {
        public int Teeth => (int)numTeeth.Value;
        public double HelixAngle => (double)numHelixAngle.Value;
        public string HelicalDirection => cboHelicalDirection.SelectedItem?.ToString().StartsWith("R", StringComparison.OrdinalIgnoreCase) == true ? "R" : "L";
        public double Clearance => (double)numClearance.Value;
        public double Backlash => (double)numBacklash.Value;
        public double ProfileShift => (double)numProfileShift.Value;
        public double FaceWidth => (double)numFaceWidth.Value;
        public bool IsInternal => chkInternal.Checked;
        public double RimThickness => (double)numRimThickness.Value;
        public bool OptimalShift => chkOptimalShift.Checked;

        public AddMeshedGearDialog()
        {
            InitializeComponent();
            try
            {
                Icon = Globals.Icon;
            }
            catch
            {
                // Resources may be unavailable outside an install; keep default icon.
            }

            if (cboHelicalDirection.SelectedIndex < 0)
            {
                cboHelicalDirection.SelectedIndex = 0; // Default "L (Left)"
            }

            chkInternal.CheckedChanged += (s, e) => UpdateInternalControls();
            UpdateInternalControls();
        }

        private void UpdateInternalControls()
        {
            var isInternal = chkInternal.Checked;
            numRimThickness.Enabled = isInternal;
            lblRimThickness.Enabled = isInternal;
        }

        public void SetValues(
            int teeth,
            double profileShift,
            double faceWidth,
            bool isInternal,
            double rimThickness,
            bool optimalShift,
            double sharedModule,
            double sharedPressureAngle,
            double clearance = 0.5,
            double backlash = 0.0,
            double helixAngle = 0.0,
            string helicalDirection = "L")
        {
            numTeeth.Value = Math.Max(numTeeth.Minimum, Math.Min(numTeeth.Maximum, teeth));
            numHelixAngle.Value = Math.Max(numHelixAngle.Minimum, Math.Min(numHelixAngle.Maximum, (decimal)helixAngle));
            cboHelicalDirection.SelectedIndex = string.Equals(helicalDirection, "R", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            numClearance.Value = Math.Max(numClearance.Minimum, Math.Min(numClearance.Maximum, (decimal)clearance));
            numBacklash.Value = Math.Max(numBacklash.Minimum, Math.Min(numBacklash.Maximum, (decimal)backlash));
            numProfileShift.Value = Math.Max(numProfileShift.Minimum, Math.Min(numProfileShift.Maximum, (decimal)profileShift));
            numFaceWidth.Value = Math.Max(numFaceWidth.Minimum, Math.Min(numFaceWidth.Maximum, (decimal)faceWidth));
            chkInternal.Checked = isInternal;
            numRimThickness.Value = Math.Max(numRimThickness.Minimum, Math.Min(numRimThickness.Maximum, (decimal)rimThickness));
            chkOptimalShift.Checked = optimalShift;

            lblModuleVal.Text = $"{sharedModule.ToString("0.###")} mm";
            lblPressureAngleVal.Text = $"{sharedPressureAngle.ToString("0.##")}°";

            UpdateInternalControls();
        }
    }
}
