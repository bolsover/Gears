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

        private bool _hasMeshedGear = false;
        private int _meshedTeeth = 24;
        private double _meshedHelixAngle = 0.0;
        private string _meshedHelicalDirection = "L";
        private double _meshedClearance = 0.5;
        private double _meshedBacklash = 0.0;
        private double _meshedProfileShift = 0.0;
        private double _meshedFaceWidth = 15.0;
        private bool _meshedIsInternal = false;
        private double _meshedRimThickness = 10.0;
        private bool _meshedOptimalShift = false;

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

            if (cboHelicalDirection.SelectedIndex < 0)
            {
                cboHelicalDirection.SelectedIndex = 0; // Default "R (Right)"
            }

            // Parameter change listeners
            numTeeth.ValueChanged += (s, e) => ExecuteCalculation();
            numModule.ValueChanged += (s, e) =>
            {
                var m = (double)numModule.Value;
                numClearance.Value = Math.Max(numClearance.Minimum, Math.Min(numClearance.Maximum, (decimal)(0.25 * m)));
                ExecuteCalculation();
            };
            numPressureAngle.ValueChanged += (s, e) => ExecuteCalculation();
            numHelixAngle.ValueChanged += (s, e) => ExecuteCalculation();
            cboHelicalDirection.SelectedIndexChanged += (s, e) => ExecuteCalculation();
            numClearance.ValueChanged += (s, e) => ExecuteCalculation();
            numBacklash.ValueChanged += (s, e) => ExecuteCalculation();
            numFaceWidth.ValueChanged += (s, e) => ExecuteCalculation();
            numProfileShift.ValueChanged += (s, e) => ExecuteCalculation();
            numRimThickness.ValueChanged += (s, e) => ExecuteCalculation();
            chkOptimalShift.CheckedChanged += (s, e) => ExecuteCalculation();
            chkFullGear.CheckedChanged += (s, e) => ExecuteCalculation();

            chkInternal.CheckedChanged += OnInternalChanged;

            // Meshed gear buttons
            btnAddMeshedGear.Click += OnAddOrEditMeshedGear;
            btnRemoveMeshedGear.Click += OnRemoveMeshedGear;

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

        private void OnAddOrEditMeshedGear(object sender, EventArgs e)
        {
            using var dialog = new AddMeshedGearDialog();
            dialog.SetValues(
                _meshedTeeth,
                _meshedProfileShift,
                _meshedFaceWidth,
                _meshedIsInternal,
                _meshedRimThickness,
                _meshedOptimalShift,
                (double)numModule.Value,
                (double)numPressureAngle.Value,
                _meshedClearance,
                _meshedBacklash,
                _meshedHelixAngle,
                _meshedHelicalDirection);

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _hasMeshedGear = true;
                _meshedTeeth = dialog.Teeth;
                _meshedHelixAngle = dialog.HelixAngle;
                _meshedHelicalDirection = dialog.HelicalDirection;
                _meshedClearance = dialog.Clearance;
                _meshedBacklash = dialog.Backlash;
                _meshedProfileShift = dialog.ProfileShift;
                _meshedFaceWidth = dialog.FaceWidth;
                _meshedIsInternal = dialog.IsInternal;
                _meshedRimThickness = dialog.RimThickness;
                _meshedOptimalShift = dialog.OptimalShift;

                btnAddMeshedGear.Text = "Edit Meshed Gear…";
                btnRemoveMeshedGear.Visible = true;
                lblTableTitle.Text = "Calculated Gear Pair Parameters (Elements of Metric Gear Technology)";
                ExecuteCalculation();
            }
        }

        private void OnRemoveMeshedGear(object sender, EventArgs e)
        {
            _hasMeshedGear = false;
            btnAddMeshedGear.Text = "Add Meshed Gear…";
            btnRemoveMeshedGear.Visible = false;
            lblTableTitle.Text = "Calculated Gear Parameters (Elements of Metric Gear Technology)";
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
            var helixAngle = (double)numHelixAngle.Value;
            var helicalDirection = cboHelicalDirection.SelectedItem?.ToString().StartsWith("L", StringComparison.OrdinalIgnoreCase) == true ? "L" : "R";
            var clearance = (double)numClearance.Value;
            var backlash = (double)numBacklash.Value;
            var faceWidth = (double)numFaceWidth.Value;
            var profileShift = (double)numProfileShift.Value;
            var optimalShift = chkOptimalShift.Checked;
            var internalGear = chkInternal.Checked;
            var rimThickness = (double)numRimThickness.Value;

            try
            {
                lblError.Text = string.Empty;
                UpdateProgress(15, "Calculating geometric parameters...");

                if (_hasMeshedGear)
                {
                    // 1. Calculate meshed gear pair parameters
                    var pairData = GearParameterCalculator.CalculatePair(
                        z, m, alphaDeg, faceWidth, profileShift, internalGear, rimThickness,
                        _meshedTeeth, _meshedFaceWidth, _meshedProfileShift, _meshedIsInternal, _meshedRimThickness,
                        clearance, _meshedClearance,
                        backlash, _meshedBacklash,
                        helixAngle, _meshedHelixAngle,
                        helicalDirection, _meshedHelicalDirection);

                    UpdateProgress(40, "Generating involute profile curves...");

                    // 2. Generate 2D tooth and gear profile for Gear 1
                    var profile1 = internalGear
                        ? AlibreGearGenerator.GenerateInternalToothProfile(z, m, alphaDeg, rimThickness, profileShift, optimalShift, null, clearance, backlash, helixAngle)
                        : AlibreGearGenerator.GenerateExternalToothProfile(z, m, alphaDeg, profileShift, optimalShift, null, clearance, backlash, helixAngle);

                    var tooth1 = AlibreGearGenerator.BuildToothOutline(profile1, internalGear);
                    var polylines1 = chkFullGear.Checked
                        ? AlibreGearGenerator.BuildFullGear(z, tooth1)
                        : tooth1;

                    // Generate 2D tooth and gear profile for Gear 2
                    var profile2 = _meshedIsInternal
                        ? AlibreGearGenerator.GenerateInternalToothProfile(_meshedTeeth, m, alphaDeg, _meshedRimThickness, _meshedProfileShift, _meshedOptimalShift, null, _meshedClearance, _meshedBacklash, _meshedHelixAngle)
                        : AlibreGearGenerator.GenerateExternalToothProfile(_meshedTeeth, m, alphaDeg, _meshedProfileShift, _meshedOptimalShift, null, _meshedClearance, _meshedBacklash, _meshedHelixAngle);

                    var tooth2 = AlibreGearGenerator.BuildToothOutline(profile2, _meshedIsInternal);
                    var polylines2 = chkFullGear.Checked
                        ? AlibreGearGenerator.BuildFullGear(_meshedTeeth, tooth2)
                        : tooth2;

                    // Mesh alignment:
                    // For external-external, rotate Gear 2 by (pi + pi / z2) so a tooth gap aligns with Gear 1's tooth at angle 0.
                    // For internal-external (e.g. adding a meshed external gear to an internal gear), Gear 1 has a tooth gap at angle 0
                    // and Gear 2 has a tooth at angle 0, so rotation is 0 without a 0.5 tooth offset.
                    var aw = pairData.WorkingCentreDistance;
                    var meshRotationAngle = AlibreGearGenerator.CalculateMeshRotationAngle(internalGear, _meshedIsInternal, _meshedTeeth);

                    var cosM = Math.Cos(meshRotationAngle);
                    var sinM = Math.Sin(meshRotationAngle);

                    var transformedPolylines2 = new List<List<GearPoint>>(polylines2.Count);
                    foreach (var polyline in polylines2)
                    {
                        var transformed = new List<GearPoint>(polyline.Count);
                        foreach (var pt in polyline)
                        {
                            var xRot = pt.X * cosM - pt.Y * sinM;
                            var yRot = pt.X * sinM + pt.Y * cosM;
                            transformed.Add(new GearPoint(xRot + aw, yRot));
                        }
                        transformedPolylines2.Add(transformed);
                    }

                    _currentPolylines = new List<List<GearPoint>>(polylines1.Count + transformedPolylines2.Count);
                    _currentPolylines.AddRange(polylines1);
                    _currentPolylines.AddRange(transformedPolylines2);

                    UpdateProgress(65, "Constructing 3D extruded solid mesh...");

                    // 3. Generate 3D extruded meshes and combine
                    var mesh1 = GearMesh3DBuilder.BuildGearMesh(profile1, z, faceWidth, internalGear, chkFullGear.Checked, rimThickness, helixAngle, helicalDirection);
                    var mesh2 = GearMesh3DBuilder.BuildGearMesh(profile2, _meshedTeeth, _meshedFaceWidth, _meshedIsInternal, chkFullGear.Checked, _meshedRimThickness, _meshedHelixAngle, _meshedHelicalDirection);
                    var transformedMesh2 = GearMesh3DBuilder.Transform(mesh2, aw, 0.0, 0.0, meshRotationAngle);
                    _currentMesh = GearMesh3DBuilder.Combine(mesh1, transformedMesh2);

                    UpdateProgress(85, "Rendering visual models and LaTeX tables...");

                    // 4. Update preview control
                    preview.Polylines = _currentPolylines;
                    preview.Mesh = _currentMesh;
                    preview.PitchRadius = pairData.Gear1.PitchDiameter / 2.0;
                    preview.BaseRadius = pairData.Gear1.BaseDiameter / 2.0;
                    preview.AddendumRadius = pairData.Gear1.TipDiameter / 2.0;
                    preview.DedendumRadius = pairData.Gear1.RootDiameter / 2.0;

                    preview.HasSecondGear = true;
                    preview.Gear2CenterDistance = aw;
                    preview.Gear2PitchRadius = pairData.Gear2.PitchDiameter / 2.0;
                    preview.Gear2BaseRadius = pairData.Gear2.BaseDiameter / 2.0;
                    preview.Gear2AddendumRadius = pairData.Gear2.TipDiameter / 2.0;
                    preview.Gear2DedendumRadius = pairData.Gear2.RootDiameter / 2.0;

                    preview.ShowReferenceCircles = chkShowCircles.Checked;
                    UpdatePreviewViewMode();
                    preview.Invalidate();

                    // 5. Update summary radii labels
                    UpdateResultLabelsForPair(pairData);

                    // 6. Populate tabular calculated parameters with LaTeX formatting
                    PopulateParameterTable(pairData.ParameterRows);

                    UpdateProgress(100, "Meshed gear pair calculation completed successfully.");
                }
                else
                {
                    // 1. Calculate metric gear parameters
                    var calcData = GearParameterCalculator.Calculate(
                        z, m, alphaDeg, faceWidth, profileShift, internalGear, rimThickness, clearance, backlash, helixAngle, helicalDirection);

                    UpdateProgress(40, "Generating involute profile curves...");

                    // 2. Generate 2D tooth and gear profile
                    var profile = internalGear
                        ? AlibreGearGenerator.GenerateInternalToothProfile(z, m, alphaDeg, rimThickness, profileShift, optimalShift, null, clearance, backlash, helixAngle)
                        : AlibreGearGenerator.GenerateExternalToothProfile(z, m, alphaDeg, profileShift, optimalShift, null, clearance, backlash, helixAngle);

                    var tooth = AlibreGearGenerator.BuildToothOutline(profile, internalGear);
                    _currentPolylines = chkFullGear.Checked
                        ? AlibreGearGenerator.BuildFullGear(z, tooth)
                        : tooth;

                    UpdateProgress(65, "Constructing 3D extruded solid mesh...");

                    // 3. Generate 3D extruded mesh
                    _currentMesh = GearMesh3DBuilder.BuildGearMesh(profile, z, faceWidth, internalGear, chkFullGear.Checked, rimThickness, helixAngle, helicalDirection);

                    UpdateProgress(85, "Rendering visual models and LaTeX tables...");

                    // 4. Update preview control
                    preview.Polylines = _currentPolylines;
                    preview.Mesh = _currentMesh;
                    preview.PitchRadius = calcData.PitchDiameter / 2.0;
                    preview.BaseRadius = calcData.BaseDiameter / 2.0;
                    preview.AddendumRadius = calcData.TipDiameter / 2.0;
                    preview.DedendumRadius = calcData.RootDiameter / 2.0;

                    preview.HasSecondGear = false;
                    preview.ShowReferenceCircles = chkShowCircles.Checked;
                    UpdatePreviewViewMode();
                    preview.Invalidate();

                    // 5. Update summary radii labels
                    UpdateResultLabels(calcData);

                    // 6. Populate tabular calculated parameters with LaTeX formatting
                    PopulateParameterTable(calcData.ParameterRows);

                    UpdateProgress(100, "Calculation completed successfully.");
                }
            }
            catch (Exception ex)
            {
                _currentPolylines = new List<List<GearPoint>>();
                _currentMesh = new GearMesh3DBuilder.Mesh3D();
                preview.Polylines = _currentPolylines;
                preview.Mesh = _currentMesh;
                preview.HasSecondGear = false;
                preview.Invalidate();

                ClearResultLabels();
                lblError.Text = ex.Message;
                UpdateProgress(0, "Error: " + ex.Message);
            }
        }

        private void UpdatePreviewViewMode()
        {
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
            lblClearanceVal.Text = "Clearance (c): " + Fmt(calcData.Clearance);
            lblBacklashVal.Text = "Backlash (jt): " + Fmt(calcData.Backlash);
            lblHelixVal.Text = calcData.HelixAngleDeg > 1e-6
                ? $"Helix (β): {calcData.HelixAngleDeg:0.##}° ({calcData.HelicalDirection})"
                : "Helix (β): Spur (0°)";
        }

        private void UpdateResultLabelsForPair(CalculatedGearPairData pairData)
        {
            lblPitch.Text = $"Pitch: d₁={Fmt(pairData.Gear1.PitchDiameter)}, d₂={Fmt(pairData.Gear2.PitchDiameter)}";
            lblBase.Text = $"Base: db₁={Fmt(pairData.Gear1.BaseDiameter)}, db₂={Fmt(pairData.Gear2.BaseDiameter)}";
            lblAddendum.Text = $"Tip: da₁={Fmt(pairData.Gear1.TipDiameter)}, da₂={Fmt(pairData.Gear2.TipDiameter)}";
            lblDedendum.Text = $"Centre Dist (aw): {Fmt(pairData.WorkingCentreDistance)}";
            lblClearanceVal.Text = $"Clearance: c₁={Fmt(pairData.Gear1Clearance)}, c₂={Fmt(pairData.Gear2Clearance)}";
            lblBacklashVal.Text = $"Backlash: jt₁={Fmt(pairData.Gear1Backlash)}, jt₂={Fmt(pairData.Gear2Backlash)}, jn={Fmt(pairData.NormalBacklash)}";
            lblHelixVal.Text = $"Helix: β₁={pairData.Gear1HelixAngle:0.##}° ({pairData.Gear1HelicalDirection}), β₂={pairData.Gear2HelixAngle:0.##}° ({pairData.Gear2HelicalDirection})";
        }

        private void ClearResultLabels()
        {
            lblPitch.Text = "Pitch: –";
            lblBase.Text = "Base: –";
            lblAddendum.Text = "Tip: –";
            lblDedendum.Text = "Dedendum: –";
            lblClearanceVal.Text = "Clearance: –";
            lblBacklashVal.Text = "Backlash: –";
            lblHelixVal.Text = "Helix: –";
        }

        private void PopulateParameterTable(IEnumerable<GearParameterRow> parameterRows)
        {
            gridCalculations.Rows.Clear();

            foreach (var row in parameterRows)
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
