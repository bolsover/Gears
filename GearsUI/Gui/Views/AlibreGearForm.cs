using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Gui.Views
{
    /// <summary>
    /// Interactive front-end for designing and visualising external and internal involute gears.
    /// Provides editable parameters, calculation execution with a progress indicator,
    /// LaTeX-formatted parameters table, and interactive 2D profile and 3D solid mesh visualisations.
    /// </summary>
    public partial class AlibreGearForm : Form
    {
        private List<List<GearPoint>> _currentPolylines = new();
        private GearMesh3DBuilder.Mesh3D _currentMesh = new();
        private readonly Dictionary<string, Image> _latexImageCache = new();

        public AlibreGearForm()
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

            // Parameter change listeners
            numTeeth.ValueChanged += (s, e) => ExecuteCalculation();
            numModule.ValueChanged += (s, e) => ExecuteCalculation();
            numPressureAngle.ValueChanged += (s, e) => ExecuteCalculation();
            numFaceWidth.ValueChanged += (s, e) => ExecuteCalculation();
            numProfileShift.ValueChanged += (s, e) => ExecuteCalculation();
            numRimThickness.ValueChanged += (s, e) => ExecuteCalculation();
            chkOptimalShift.CheckedChanged += (s, e) => ExecuteCalculation();
            chkFullGear.CheckedChanged += (s, e) => ExecuteCalculation();

            chkInternal.CheckedChanged += OnInternalChanged;

            // View options listeners
            rb2D.CheckedChanged += (s, e) =>
            {
                if (rb2D.Checked)
                {
                    preview.ViewMode = PreviewViewMode.Mode2D;
                }
            };

            rb3D.CheckedChanged += (s, e) =>
            {
                if (rb3D.Checked)
                {
                    preview.ViewMode = PreviewViewMode.Mode3D;
                    preview.Mode3D = Render3DMode.Solid;
                }
            };

            rb3DWireframe.CheckedChanged += (s, e) =>
            {
                if (rb3DWireframe.Checked)
                {
                    preview.ViewMode = PreviewViewMode.Mode3D;
                    preview.Mode3D = Render3DMode.Wireframe;
                }
            };

            chkShowCircles.CheckedChanged += (s, e) =>
            {
                preview.ShowReferenceCircles = chkShowCircles.Checked;
                preview.Invalidate();
            };

            // Buttons
            btnCalculate.Click += (s, e) => ExecuteCalculation();
            btnResetView.Click += (s, e) => preview.ResetView();
            btnExportDxf.Click += (s, e) => Export(ExportFormat.Dxf);
            btnExportCsv.Click += (s, e) => Export(ExportFormat.Csv);

            UpdateInternalControls();
            ExecuteCalculation();
        }

        private void OnInternalChanged(object sender, EventArgs e)
        {
            UpdateInternalControls();
            ExecuteCalculation();
        }

        private void UpdateInternalControls()
        {
            var internalGear = chkInternal.Checked;
            numRimThickness.Enabled = internalGear;
            lblRimThickness.Enabled = internalGear;
        }

        /// <summary>
        /// Executes full gear geometry calculations, updates the progress indicator,
        /// generates the 2D profile and 3D extruded mesh, and populates the LaTeX parameter table.
        /// </summary>
        public void ExecuteCalculation()
        {
            var z = (int)numTeeth.Value;
            var m = (double)numModule.Value;
            var alphaDeg = (double)numPressureAngle.Value;
            var faceWidth = (double)numFaceWidth.Value;
            var profileShift = (double)numProfileShift.Value;
            var optimalShift = chkOptimalShift.Checked;
            var internalGear = chkInternal.Checked;
            var rimThickness = (double)numRimThickness.Value;

            try
            {
                lblError.Text = string.Empty;
                UpdateProgress(15, "Calculating geometric parameters...");

                // 1. Calculate metric gear parameters
                var calcData = GearParameterCalculator.Calculate(
                    z, m, alphaDeg, faceWidth, profileShift, internalGear, rimThickness);

                UpdateProgress(40, "Generating involute profile curves...");

                // 2. Generate 2D tooth and gear profile
                var profile = internalGear
                    ? AlibreGearGenerator.GenerateInternalToothProfile(z, m, alphaDeg, rimThickness, profileShift, optimalShift)
                    : AlibreGearGenerator.GenerateExternalToothProfile(z, m, alphaDeg, profileShift, optimalShift);

                var tooth = AlibreGearGenerator.BuildToothOutline(profile, internalGear);
                _currentPolylines = chkFullGear.Checked
                    ? AlibreGearGenerator.BuildFullGear(z, tooth)
                    : tooth;

                UpdateProgress(65, "Constructing 3D extruded solid mesh...");

                // 3. Generate 3D extruded mesh
                _currentMesh = GearMesh3DBuilder.BuildGearMesh(profile, z, faceWidth, internalGear, chkFullGear.Checked, rimThickness);

                UpdateProgress(85, "Rendering visual models and LaTeX tables...");

                // 4. Update preview control
                preview.Polylines = _currentPolylines;
                preview.Mesh = _currentMesh;
                preview.PitchRadius = calcData.PitchDiameter / 2.0;
                preview.BaseRadius = calcData.BaseDiameter / 2.0;
                preview.AddendumRadius = calcData.TipDiameter / 2.0;
                preview.DedendumRadius = calcData.RootDiameter / 2.0;
                preview.ShowReferenceCircles = chkShowCircles.Checked;
                if (rb3DWireframe.Checked)
                {
                    preview.ViewMode = PreviewViewMode.Mode3D;
                    preview.Mode3D = Render3DMode.Wireframe;
                }
                else if (rb3D.Checked)
                {
                    preview.ViewMode = PreviewViewMode.Mode3D;
                    preview.Mode3D = Render3DMode.Solid;
                }
                else
                {
                    preview.ViewMode = PreviewViewMode.Mode2D;
                }
                preview.Invalidate();

                // 5. Update summary radii labels
                UpdateResultLabels(calcData);

                // 6. Populate tabular calculated parameters with LaTeX formatting
                PopulateParameterTable(calcData);

                UpdateProgress(100, "Calculation completed successfully.");
            }
            catch (Exception ex)
            {
                _currentPolylines = new List<List<GearPoint>>();
                _currentMesh = new GearMesh3DBuilder.Mesh3D();
                preview.Polylines = _currentPolylines;
                preview.Mesh = _currentMesh;
                preview.Invalidate();

                ClearResultLabels();
                lblError.Text = ex.Message;
                UpdateProgress(0, "Error: " + ex.Message);
            }
        }

        private void UpdateProgress(int percentage, string message)
        {
            progressBar.Value = Math.Max(0, Math.Min(100, percentage));
            lblStatus.Text = message;
        }

        private void UpdateResultLabels(CalculatedGearData calcData)
        {
            lblPitch.Text = "Pitch (d): " + Fmt(calcData.PitchDiameter);
            lblBase.Text = "Base (db): " + Fmt(calcData.BaseDiameter);
            lblAddendum.Text = "Tip (da): " + Fmt(calcData.TipDiameter);
            lblDedendum.Text = "Root (df): " + Fmt(calcData.RootDiameter);
        }

        private void ClearResultLabels()
        {
            lblPitch.Text = "Pitch: –";
            lblBase.Text = "Base: –";
            lblAddendum.Text = "Base: –";
            lblDedendum.Text = "Dedendum: –";
        }

        private void PopulateParameterTable(CalculatedGearData data)
        {
            gridCalculations.Rows.Clear();

            foreach (var row in data.ParameterRows)
            {
                var symImage = GetOrRenderLatexImage(row.SymbolLatex);
                var formImage = GetOrRenderLatexImage(row.FormulaLatex);
                var valText = row.Value.ToString("0.####", CultureInfo.InvariantCulture);

                var rowIndex = gridCalculations.Rows.Add(
                    row.ParameterName,
                    symImage,
                    formImage,
                    valText,
                    row.Unit);

                // Set tooltip text with LaTeX source strings
                gridCalculations.Rows[rowIndex].Cells[1].ToolTipText = row.SymbolLatex;
                gridCalculations.Rows[rowIndex].Cells[2].ToolTipText = row.FormulaLatex;
            }
        }

        private Image GetOrRenderLatexImage(string latex)
        {
            if (string.IsNullOrWhiteSpace(latex)) return null;

            if (_latexImageCache.TryGetValue(latex, out var cached))
            {
                return cached;
            }

            try
            {
                if (LaTeXUtils.TryParse(latex, out _))
                {
                    var img = LaTeXUtils.CreateImageFromLatex(latex);
                    if (img != null)
                    {
                        _latexImageCache[latex] = img;
                        return img;
                    }
                }
            }
            catch
            {
                // Return null if LaTeX parser encounters an unhandled token
            }

            return null;
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
                sb.AppendLine("8").AppendLine("GEAR");
                sb.AppendLine("90").AppendLine(polyline.Count.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("70").AppendLine("0");
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
