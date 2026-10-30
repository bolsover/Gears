using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>
    /// File export formats supported by AlibreGearPresenter.
    /// </summary>
    public enum ExportFormat
    {
        Dxf,
        Csv
    }

    /// <summary>
    /// Configuration data parameters for the second (meshed) gear in a gear pair.
    /// </summary>
    public class MeshedGearParameters
    {
        public int Teeth { get; set; } = 24;
        public double HelixAngle { get; set; } = 0.0;
        public string HelicalDirection { get; set; } = "L";
        public double Clearance { get; set; } = 0.5;
        public double Backlash { get; set; } = 0.0;
        public double ProfileShift { get; set; } = 0.0;
        public double FaceWidth { get; set; } = 15.0;
        public bool IsInternal { get; set; } = false;
        public double RimThickness { get; set; } = 10.0;
        public bool OptimalShift { get; set; } = false;
    }

    /// <summary>
    /// Presenter for the Alibre Gear design, calculation, and visualisation workflow following MVP strategy.
    /// Handles user input events, coordinates mathematical calculations, generates 2D/3D geometry,
    /// and updates the view presentation elements.
    /// </summary>
    public class AlibreGearPresenter
    {
        private readonly IAlibreGearView _view;

        public bool HasMeshedGear { get; set; } = false;
        public MeshedGearParameters MeshedGear { get; set; } = new();

        public List<List<GearPoint>> CurrentPolylines { get; private set; } = new();
        public GearMesh3DBuilder.Mesh3D CurrentMesh { get; private set; } = new();
        public CalculatedGearData CurrentCalculatedGearData { get; private set; }
        public CalculatedGearPairData CurrentCalculatedGearPairData { get; private set; }

        public AlibreGearPresenter(IAlibreGearView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            SetupEventListeners();
            Initialise();
        }

        private void SetupEventListeners()
        {
            _view.CalculateRequested += (s, e) => ExecuteCalculation();
            _view.ModuleChanged += (s, e) => OnModuleChanged();
            _view.InternalChanged += (s, e) => OnInternalChanged();
            _view.AddOrEditMeshedGearRequested += (s, e) => OnAddOrEditMeshedGear();
            _view.RemoveMeshedGearRequested += (s, e) => OnRemoveMeshedGear();
            _view.ExportRequested += (s, format) => Export(format);
            _view.VisualisationModeChanged += (s, e) => SetVisualisationMode(e.ViewMode, e.Mode3D);
            _view.ReferenceCirclesChanged += (s, e) => OnReferenceCirclesChanged();
            _view.ResetViewRequested += (s, e) => _view.ResetView();
        }

        public void Initialise()
        {
            UpdateInternalControls();
            ExecuteCalculation();
        }

        public void OnModuleChanged()
        {
            var m = _view.Module;
            _view.Clearance = 0.25 * m;
            ExecuteCalculation();
        }

        public void OnInternalChanged()
        {
            UpdateInternalControls();
            ExecuteCalculation();
        }

        public void UpdateInternalControls()
        {
            _view.RimThicknessEnabled = _view.IsInternal;
        }

        public void OnAddOrEditMeshedGear()
        {
            var meshed = MeshedGear;
            if (_view.ShowAddMeshedGearDialog(ref meshed, _view.Module, _view.PressureAngle))
            {
                MeshedGear = meshed;
                HasMeshedGear = true;
                _view.AddMeshedGearMenuText = "Edit Meshed Gear…";
                _view.RemoveMeshedGearMenuEnabled = true;
                _view.TableTitle = "Calculated Gear Pair Parameters (Elements of Metric Gear Technology)";
                ExecuteCalculation();
            }
        }

        public void OnRemoveMeshedGear()
        {
            HasMeshedGear = false;
            _view.AddMeshedGearMenuText = "Add Meshed Gear…";
            _view.RemoveMeshedGearMenuEnabled = false;
            _view.TableTitle = "Calculated Gear Parameters (Elements of Metric Gear Technology)";
            ExecuteCalculation();
        }

        public void SetVisualisationMode(PreviewViewMode viewMode, Render3DMode mode3D = Render3DMode.Solid)
        {
            _view.SetVisualisationMode(viewMode, mode3D);
        }

        public void OnReferenceCirclesChanged()
        {
            // View handles toggling circle display on preview control
        }

        public void Export(ExportFormat format)
        {
            if (CurrentPolylines == null || CurrentPolylines.Count == 0)
            {
                _view.ShowMessage("There is no valid geometry to export.", "Export", MessageBoxIcon.Information);
                return;
            }

            var isDxf = format == ExportFormat.Dxf;
            var filter = isDxf ? "DXF files (*.dxf)|*.dxf" : "CSV files (*.csv)|*.csv";
            var defaultFileName = isDxf ? "gear.dxf" : "gear.csv";

            var filePath = _view.ShowSaveFileDialog(filter, defaultFileName);
            if (string.IsNullOrWhiteSpace(filePath)) return;

            try
            {
                var content = isDxf ? BuildDxf(CurrentPolylines) : BuildCsv(CurrentPolylines);
                File.WriteAllText(filePath, content);
            }
            catch (Exception ex)
            {
                _view.ShowMessage("Failed to write file: " + ex.Message, "Export", MessageBoxIcon.Error);
            }
        }

        public void ExecuteCalculation()
        {
            var z = _view.Teeth;
            var m = _view.Module;
            var alphaDeg = _view.PressureAngle;
            var helixAngle = _view.HelixAngle;
            var helicalDirection = _view.HelicalDirection?.StartsWith("L", StringComparison.OrdinalIgnoreCase) == true ? "L" : "R";
            var clearance = _view.Clearance;
            var backlash = _view.Backlash;
            var faceWidth = _view.FaceWidth;
            var profileShift = _view.ProfileShift;
            var optimalShift = _view.OptimalShift;
            var internalGear = _view.IsInternal;
            var rimThickness = _view.RimThickness;
            var fullGear = _view.ShowFullGear;

            try
            {
                _view.SetErrorMessage(string.Empty);
                _view.UpdateProgress(15, "Calculating geometric parameters...");

                if (HasMeshedGear)
                {
                    // 1. Calculate meshed gear pair parameters
                    var pairData = GearParameterCalculator.CalculatePair(
                        z, m, alphaDeg, faceWidth, profileShift, internalGear, rimThickness,
                        MeshedGear.Teeth, MeshedGear.FaceWidth, MeshedGear.ProfileShift, MeshedGear.IsInternal, MeshedGear.RimThickness,
                        clearance, MeshedGear.Clearance,
                        backlash, MeshedGear.Backlash,
                        helixAngle, MeshedGear.HelixAngle,
                        helicalDirection, MeshedGear.HelicalDirection);

                    CurrentCalculatedGearPairData = pairData;
                    CurrentCalculatedGearData = null;

                    _view.UpdateProgress(40, "Generating involute profile curves...");

                    // 2. Generate 2D tooth and gear profile for Gear 1
                    var profile1 = internalGear
                        ? AlibreGearGenerator.GenerateInternalToothProfile(z, m, alphaDeg, rimThickness, profileShift, optimalShift, null, clearance, backlash, helixAngle)
                        : AlibreGearGenerator.GenerateExternalToothProfile(z, m, alphaDeg, profileShift, optimalShift, null, clearance, backlash, helixAngle);

                    var tooth1 = AlibreGearGenerator.BuildToothOutline(profile1, internalGear);
                    var polylines1 = fullGear
                        ? AlibreGearGenerator.BuildFullGear(z, tooth1)
                        : tooth1;

                    // Generate 2D tooth and gear profile for Gear 2
                    var profile2 = MeshedGear.IsInternal
                        ? AlibreGearGenerator.GenerateInternalToothProfile(MeshedGear.Teeth, m, alphaDeg, MeshedGear.RimThickness, MeshedGear.ProfileShift, MeshedGear.OptimalShift, null, MeshedGear.Clearance, MeshedGear.Backlash, MeshedGear.HelixAngle)
                        : AlibreGearGenerator.GenerateExternalToothProfile(MeshedGear.Teeth, m, alphaDeg, MeshedGear.ProfileShift, MeshedGear.OptimalShift, null, MeshedGear.Clearance, MeshedGear.Backlash, MeshedGear.HelixAngle);

                    var tooth2 = AlibreGearGenerator.BuildToothOutline(profile2, MeshedGear.IsInternal);
                    var polylines2 = fullGear
                        ? AlibreGearGenerator.BuildFullGear(MeshedGear.Teeth, tooth2)
                        : tooth2;

                    // Mesh alignment:
                    // For external-external, rotate Gear 2 by (pi + pi / z2) so a tooth gap aligns with Gear 1's tooth at angle 0.
                    // For internal-external (e.g. adding a meshed external gear to an internal gear), Gear 1 has a tooth gap at angle 0
                    // and Gear 2 has a tooth at angle 0, so rotation is 0 without a 0.5 tooth offset.
                    var aw = pairData.WorkingCentreDistance;
                    var meshRotationAngle = AlibreGearGenerator.CalculateMeshRotationAngle(internalGear, MeshedGear.IsInternal, MeshedGear.Teeth);

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

                    CurrentPolylines = new List<List<GearPoint>>(polylines1.Count + transformedPolylines2.Count);
                    CurrentPolylines.AddRange(polylines1);
                    CurrentPolylines.AddRange(transformedPolylines2);

                    _view.UpdateProgress(65, "Constructing 3D extruded solid mesh...");

                    // 3. Generate 3D extruded meshes and combine
                    var mesh1 = GearMesh3DBuilder.BuildGearMesh(profile1, z, faceWidth, internalGear, fullGear, rimThickness, helixAngle, helicalDirection);
                    var mesh2 = GearMesh3DBuilder.BuildGearMesh(profile2, MeshedGear.Teeth, MeshedGear.FaceWidth, MeshedGear.IsInternal, fullGear, MeshedGear.RimThickness, MeshedGear.HelixAngle, MeshedGear.HelicalDirection);
                    var transformedMesh2 = GearMesh3DBuilder.Transform(mesh2, aw, 0.0, 0.0, meshRotationAngle);
                    CurrentMesh = GearMesh3DBuilder.Combine(mesh1, transformedMesh2);

                    _view.UpdateProgress(85, "Rendering visual models and LaTeX tables...");

                    // 4. Update preview control via view
                    _view.UpdatePreviewPair(
                        CurrentPolylines,
                        CurrentMesh,
                        pairData.Gear1.PitchDiameter / 2.0,
                        pairData.Gear1.BaseDiameter / 2.0,
                        pairData.Gear1.TipDiameter / 2.0,
                        pairData.Gear1.RootDiameter / 2.0,
                        aw,
                        pairData.Gear2.PitchDiameter / 2.0,
                        pairData.Gear2.BaseDiameter / 2.0,
                        pairData.Gear2.TipDiameter / 2.0,
                        pairData.Gear2.RootDiameter / 2.0);

                    // 5. Update summary radii labels
                    _view.UpdateResultLabelsForPair(pairData);

                    // 6. Populate tabular calculated parameters with LaTeX formatting
                    _view.PopulateParameterTable(pairData.ParameterRows);

                    _view.UpdateProgress(100, "Meshed gear pair calculation completed successfully.");
                }
                else
                {
                    // 1. Calculate metric gear parameters
                    var calcData = GearParameterCalculator.Calculate(
                        z, m, alphaDeg, faceWidth, profileShift, internalGear, rimThickness, clearance, backlash, helixAngle, helicalDirection);

                    CurrentCalculatedGearData = calcData;
                    CurrentCalculatedGearPairData = null;

                    _view.UpdateProgress(40, "Generating involute profile curves...");

                    // 2. Generate 2D tooth and gear profile
                    var profile = internalGear
                        ? AlibreGearGenerator.GenerateInternalToothProfile(z, m, alphaDeg, rimThickness, profileShift, optimalShift, null, clearance, backlash, helixAngle)
                        : AlibreGearGenerator.GenerateExternalToothProfile(z, m, alphaDeg, profileShift, optimalShift, null, clearance, backlash, helixAngle);

                    var tooth = AlibreGearGenerator.BuildToothOutline(profile, internalGear);
                    CurrentPolylines = fullGear
                        ? AlibreGearGenerator.BuildFullGear(z, tooth)
                        : tooth;

                    _view.UpdateProgress(65, "Constructing 3D extruded solid mesh...");

                    // 3. Generate 3D extruded mesh
                    CurrentMesh = GearMesh3DBuilder.BuildGearMesh(profile, z, faceWidth, internalGear, fullGear, rimThickness, helixAngle, helicalDirection);

                    _view.UpdateProgress(85, "Rendering visual models and LaTeX tables...");

                    // 4. Update preview control via view
                    _view.UpdatePreviewSingle(
                        CurrentPolylines,
                        CurrentMesh,
                        calcData.PitchDiameter / 2.0,
                        calcData.BaseDiameter / 2.0,
                        calcData.TipDiameter / 2.0,
                        calcData.RootDiameter / 2.0);

                    // 5. Update summary radii labels
                    _view.UpdateResultLabels(calcData);

                    // 6. Populate tabular calculated parameters with LaTeX formatting
                    _view.PopulateParameterTable(calcData.ParameterRows);

                    _view.UpdateProgress(100, "Calculation completed successfully.");
                }
            }
            catch (Exception ex)
            {
                CurrentPolylines = new List<List<GearPoint>>();
                CurrentMesh = new GearMesh3DBuilder.Mesh3D();
                _view.ClearPreview();
                _view.ClearResultLabels();
                _view.SetErrorMessage(ex.Message);
                _view.UpdateProgress(0, "Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Builds a minimal DXF document with each polyline emitted as an LWPOLYLINE entity.
        /// </summary>
        public static string BuildDxf(IEnumerable<List<GearPoint>> polylines)
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
        public static string BuildCsv(IReadOnlyList<List<GearPoint>> polylines)
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
