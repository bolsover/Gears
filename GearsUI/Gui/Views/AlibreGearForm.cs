using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Gui.Views
{
    /// <summary>
    /// Interactive front-end for <see cref="AlibreGearGenerator"/>. Lets the user adjust the gear
    /// parameters (teeth, module, pressure angle, thickness, profile shift) and see the involute
    /// profile update live, then export it to DXF or CSV.
    /// </summary>
    public partial class AlibreGearForm : Form
    {
        private List<List<GearPoint>> _currentPolylines = new();

        public AlibreGearForm()
        {
            InitializeComponent();
            try
            {
                Icon = Globals.Icon;
            }
            catch
            {
                // Alibre resources may be unavailable outside an install; keep the default icon.
            }

            // Live recompute on any parameter change.
            numTeeth.ValueChanged += OnParameterChanged;
            numModule.ValueChanged += OnParameterChanged;
            numPressureAngle.ValueChanged += OnParameterChanged;
            numThickness.ValueChanged += OnParameterChanged;
            numProfileShift.ValueChanged += OnParameterChanged;
            chkOptimalShift.CheckedChanged += OnParameterChanged;
            chkInternal.CheckedChanged += OnInternalChanged;
            chkFullGear.CheckedChanged += OnParameterChanged;

            btnExportDxf.Click += (s, e) => Export(ExportFormat.Dxf);
            btnExportCsv.Click += (s, e) => Export(ExportFormat.Csv);

            UpdateThicknessEnabled();
            Recompute();
        }

        private void OnParameterChanged(object sender, EventArgs e) => Recompute();

        private void OnInternalChanged(object sender, EventArgs e)
        {
            UpdateThicknessEnabled();
            Recompute();
        }

        private void UpdateThicknessEnabled()
        {
            var internalGear = chkInternal.Checked;
            numThickness.Enabled = internalGear;
            lblThickness.Enabled = internalGear;
        }

        /// <summary>
        /// Regenerates the profile from the current inputs and refreshes the preview / result labels.
        /// Invalid geometry is reported in the error label rather than thrown.
        /// </summary>
        private void Recompute()
        {
            var z = (int) numTeeth.Value;
            var m = (double) numModule.Value;
            var alphaDeg = (double) numPressureAngle.Value;
            var thickness = (double) numThickness.Value;
            var profileShift = (double) numProfileShift.Value;
            var optimalShift = chkOptimalShift.Checked;
            var internalGear = chkInternal.Checked;

            try
            {
                var profile = internalGear
                    ? AlibreGearGenerator.GenerateInternalToothProfile(z, m, alphaDeg, thickness, profileShift, optimalShift)
                    : AlibreGearGenerator.GenerateExternalToothProfile(z, m, alphaDeg, profileShift, optimalShift);

                var tooth = AlibreGearGenerator.BuildToothOutline(profile, internalGear);
                _currentPolylines = chkFullGear.Checked
                    ? AlibreGearGenerator.BuildFullGear(z, tooth)
                    : tooth;

                preview.Polylines = _currentPolylines;
                preview.ResetView();

                UpdateResultLabels(profile);
                lblError.Text = string.Empty;
            }
            catch (Exception ex)
            {
                _currentPolylines = new List<List<GearPoint>>();
                preview.Polylines = _currentPolylines;
                ClearResultLabels();
                lblError.Text = ex.Message;
            }
        }

        private void UpdateResultLabels(AlibreGearGenerator.AlibreGearProfile profile)
        {
            lblPitch.Text = "Pitch: " + Fmt(profile.Parameters["pitchRadius"]);
            lblBase.Text = "Base: " + Fmt(profile.Parameters["baseRadius"]);
            lblAddendum.Text = "Addendum: " + Fmt(profile.Parameters["addendumRadius"]);
            lblDedendum.Text = "Dedendum: " + Fmt(profile.Parameters["dedendumRadius"]);
        }

        private void ClearResultLabels()
        {
            lblPitch.Text = "Pitch: –";
            lblBase.Text = "Base: –";
            lblAddendum.Text = "Addendum: –";
            lblDedendum.Text = "Dedendum: –";
        }

        private static string Fmt(double value) => value.ToString("0.###", CultureInfo.InvariantCulture) + " mm";

        private enum ExportFormat
        {
            Dxf,
            Csv
        }

        private void Export(ExportFormat format)
        {
            if (_currentPolylines == null || _currentPolylines.Count == 0)
            {
                MessageBox.Show(this, "There is no valid geometry to export.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var isDxf = format == ExportFormat.Dxf;
            using var dialog = new SaveFileDialog();
            dialog.Filter = isDxf ? "DXF files (*.dxf)|*.dxf" : "CSV files (*.csv)|*.csv";
            dialog.FileName = isDxf ? "gear.dxf" : "gear.csv";
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var content = isDxf ? BuildDxf(_currentPolylines) : BuildCsv(_currentPolylines);
                File.WriteAllText(dialog.FileName, content);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to write file: " + ex.Message, "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Builds a minimal DXF document with each polyline emitted as an LWPOLYLINE entity.
        /// </summary>
        private static string BuildDxf(IEnumerable<List<GearPoint>> polylines)
        {
            var sb = new StringBuilder();
            sb.AppendLine("0").AppendLine("SECTION").AppendLine("2").AppendLine("ENTITIES");

            foreach (var polyline in polylines)
            {
                if (polyline == null || polyline.Count < 2) continue;
                sb.AppendLine("0").AppendLine("LWPOLYLINE");
                sb.AppendLine("8").AppendLine("GEAR"); // layer
                sb.AppendLine("90").AppendLine(polyline.Count.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("70").AppendLine("0"); // open polyline
                foreach (var p in polyline)
                {
                    sb.AppendLine("10").AppendLine(Coord(p.X));
                    sb.AppendLine("20").AppendLine(Coord(p.Y));
                }
            }

            sb.AppendLine("0").AppendLine("ENDSEC").AppendLine("0").AppendLine("EOF");
            return sb.ToString();
        }

        /// <summary>
        /// Builds a CSV of every point: segment index, point index, x, y.
        /// </summary>
        private static string BuildCsv(IReadOnlyList<List<GearPoint>> polylines)
        {
            var sb = new StringBuilder();
            sb.AppendLine("segment,point,x,y");
            for (var s = 0; s < polylines.Count; s++)
            {
                var polyline = polylines[s];
                if (polyline == null) continue;
                for (var i = 0; i < polyline.Count; i++)
                {
                    sb.Append(s.ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(i.ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(Coord(polyline[i].X)).Append(',')
                        .AppendLine(Coord(polyline[i].Y));
                }
            }

            return sb.ToString();
        }

        private static string Coord(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
    }
}
