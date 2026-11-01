using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>The kind of item a <see cref="TrainTreeNode"/> stands for.</summary>
    public enum TrainNodeKind
    {
        Assembly,
        Shaft,
        Gear
    }

    /// <summary>The kind of gear that a new train starts with.</summary>
    public enum GearFamily
    {
        Spur,
        Bevel,
        Worm
    }

    /// <summary>One node of the assembly tree: assembly, then shafts, then the gears mounted on each shaft.</summary>
    public sealed class TrainTreeNode
    {
        public TrainTreeNode(string text, TrainNodeKind kind, object tag)
        {
            Text = text;
            Kind = kind;
            Tag = tag;
        }

        public string Text { get; }
        public TrainNodeKind Kind { get; }

        /// <summary>The <see cref="GearAssembly"/>, <see cref="Shaft"/> or <see cref="GearComponent"/> behind the node.</summary>
        public object Tag { get; }

        /// <summary>For gear nodes: false when the gear is hidden in the 3D scene.</summary>
        public bool Visible { get; set; } = true;

        public List<TrainTreeNode> Children { get; } = new List<TrainTreeNode>();
    }

    /// <summary>
    /// Presenter of the gear train editor. It owns a <see cref="GearAssembly"/> and builds the shaft/gear tree,
    /// the 3D scene and the ratio and speed report. Several gears can sit on one shaft (compound gears), and gears
    /// can mesh in chains (idlers) or branch in different directions.
    /// </summary>
    public class GearTrainPresenter
    {
        private const double AxialGap = 2.0;

        private readonly IGearTrainView _view;
        private readonly HashSet<GearComponent> _hidden = new HashSet<GearComponent>();
        private int _gearCounter;
        private int _shaftCounter;
        private double _planetaryInputRad;

        public GearTrainPresenter(IGearTrainView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            Assembly = new GearAssembly();
        }

        public GearAssembly Assembly { get; private set; }

        /// <summary>The selected <see cref="Shaft"/> or <see cref="GearComponent"/>; null when nothing is selected.</summary>
        public object Selected { get; private set; }

        /// <summary>The most recent successfully built scene; null before the first refresh.</summary>
        public AssemblyScene Scene { get; private set; }

        /// <summary>The most recent successfully built report; null before the first refresh.</summary>
        public GearTrainReport Report { get; private set; }

        public ISet<GearComponent> HiddenGears => _hidden;

        /// <summary>The planetary stage being edited; null while an ordinary gear train is shown.</summary>
        public PlanetaryGearSet Planetary { get; private set; }

        /// <summary>The planetary member that is turned from outside (the input angle and speed refer to it).</summary>
        public PlanetaryMember PlanetaryInput { get; set; } = PlanetaryMember.Sun;

        /// <summary>Current input angle in radians: that of the input shaft, or of the input member of a planetary stage.</summary>
        public double InputAngleRad => Planetary != null ? _planetaryInputRad : (InputShaft?.RotationRad ?? 0.0);

        /// <summary>The shaft that is turned from outside: the first shaft of the assembly.</summary>
        public Shaft InputShaft => Assembly.Shafts.Count > 0 ? Assembly.Shafts[0] : null;

        /// <summary>Starts a new train with one shaft carrying one 20 tooth gear (module 2 mm, 20 degree pressure angle).</summary>
        public void NewTrain() => NewTrain(GearFamily.Spur);

        /// <summary>Starts a new train with one shaft carrying one gear of the given family (a 20 tooth spur or bevel gear, or a worm).</summary>
        public void NewTrain(GearFamily family)
        {
            ResetState();

            GearComponent first;
            switch (family)
            {
                case GearFamily.Bevel:
                    first = new BevelGearComponent("Bevel " + (++_gearCounter)) { Teeth = 20, FaceWidth = 10.0 };
                    break;
                case GearFamily.Worm:
                    first = new WormComponent("Worm " + (++_gearCounter));
                    break;
                default:
                    first = CreateGear(null, 20);
                    break;
            }

            var shaft = CreateShaft();
            shaft.Mount(first, 0.0);
            Assembly.AddShaft(shaft);

            Selected = first;
            Refresh();
        }

        /// <summary>Replaces the train by a planetary stage (sun, planets, ring and carrier plate).</summary>
        public void NewPlanetary(PlanetaryGearSet set = null)
        {
            ResetState();
            Planetary = set ?? new PlanetaryGearSet("Planetary");
            PlanetaryInput = Planetary.Fixed == PlanetaryMember.Sun ? PlanetaryMember.Carrier : PlanetaryMember.Sun;
            _planetaryInputRad = 0.0;
            Refresh();
        }

        /// <summary>
        /// Replaces the train by a loaded assembly. Shafts and gears keep their identifiers; new ones are numbered after the highest existing number.
        /// </summary>
        public void LoadAssembly(GearAssembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            ResetState();
            Assembly = assembly;
            _gearCounter = MaxSuffix(assembly.Components.Select(c => c.Id));
            _shaftCounter = MaxSuffix(assembly.Shafts.Select(s => s.Id));
            Selected = assembly.Components.FirstOrDefault();
            Refresh();
        }

        /// <summary>Saves the train as JSON. Problems are shown as a message; returns false when saving failed.</summary>
        public bool Save(string path)
        {
            var ok = Guard(() => AssemblySerializer.Save(Assembly, path));
            if (ok) _view.ShowMessage("Saved " + path, false);
            return ok;
        }

        /// <summary>Loads a train from JSON. Problems are shown as a message and the current train stays; returns false when loading failed.</summary>
        public bool Load(string path)
        {
            GearAssembly loaded = null;
            if (!Guard(() => loaded = AssemblySerializer.Load(path))) return false;
            if (!Guard(() => LoadAssembly(loaded))) return false;
            return true;
        }

        /// <summary>
        /// Exports the current scene as STL (any extension but .obj) or Wavefront OBJ (.obj), with all gears and, optionally,
        /// the shafts. Returns false when there is nothing to export or writing failed.
        /// </summary>
        public bool Export(string path, bool includeShafts = false)
        {
            if (Scene == null || Scene.Items.Count == 0)
            {
                _view.ShowMessage("There is nothing to export: the train has not been solved.", true);
                return false;
            }

            var ok = Guard(() =>
            {
                if (string.Equals(Path.GetExtension(path), ".obj", StringComparison.OrdinalIgnoreCase))
                {
                    MeshExporter.WriteObj(Scene, path, includeShafts);
                }
                else
                {
                    MeshExporter.WriteStl(Scene, path, includeShafts);
                }
            });

            if (ok) _view.ShowMessage("Exported " + path, false);
            return ok;
        }

        /// <summary>Turns the input by <paramref name="seconds"/> at <paramref name="speedRpm"/> and rebuilds the scene (one animation frame).</summary>
        public void AdvanceAnimation(double seconds, double speedRpm)
        {
            if (Assembly.Shafts.Count == 0) return;

            if (Planetary != null)
            {
                var turn = 2.0 * Math.PI;
                var angle = _planetaryInputRad + speedRpm * turn / 60.0 * seconds;
                _planetaryInputRad = angle - turn * Math.Floor(angle / turn);
            }
            else
            {
                new GearTrainAnimator(Assembly) { SpeedRpm = speedRpm }.Advance(seconds);
            }

            Refresh();
        }

        /// <summary>
        /// Adds another gear to a shaft next to the existing gears (a compound gear). The new gear copies the
        /// parameters of the last gear on the shaft and sits beside it with a small axial gap.
        /// </summary>
        public InvoluteGearComponent AddGearToShaft(Shaft shaft, int teeth = 20)
        {
            EnsureNotPlanetary();
            if (shaft == null) throw new ArgumentNullException(nameof(shaft));
            if (!Assembly.Shafts.Contains(shaft)) throw new ArgumentException("The shaft is not part of the assembly.", nameof(shaft));

            var template = shaft.Mounts.Count > 0 ? shaft.Mounts[shaft.Mounts.Count - 1].Component as InvoluteGearComponent : null;
            var gear = CreateGear(template, teeth);

            var offset = 0.0;
            if (shaft.Mounts.Count > 0)
            {
                var end = double.MinValue;
                foreach (var mount in shaft.Mounts)
                {
                    mount.GetAxialExtent(out _, out var e);
                    if (e > end) end = e;
                }

                offset = end + AxialGap + gear.FaceWidth / 2.0;
            }

            shaft.Mount(gear, GearPrecision.Round(offset));
            Selected = gear;
            Refresh();
            return gear;
        }

        /// <summary>
        /// Adds a new shaft with a gear that meshes with <paramref name="driver"/>. The gear takes over module,
        /// pressure angle and face width, uses the opposite helix hand for an external pair, and sits in the same plane as the driver.
        /// A gear that is both driven and a driver is an idler; several gears driven by one driver form a branching train.
        /// </summary>
        /// <param name="driver">The gear to mesh with; it must be an involute gear mounted in the assembly.</param>
        /// <param name="teeth">Number of teeth; zero or less chooses the driver's teeth plus ten.</param>
        /// <param name="centreLineAngleDeg">Direction of the new shaft from the driver shaft; null spaces branches 60 degrees apart.</param>
        public InvoluteGearComponent AddMeshedGear(GearComponent driver, int teeth = 0, double? centreLineAngleDeg = null)
        {
            EnsureNotPlanetary();
            var driverGear = driver as InvoluteGearComponent
                             ?? throw new ArgumentException("Only involute gears can be meshed.", nameof(driver));
            var driverMount = Assembly.FindMount(driverGear, out var driverShaft)
                              ?? throw new ArgumentException("The driver gear is not mounted in the assembly.", nameof(driver));

            var gear = CreateGear(driverGear, teeth > 0 ? teeth : Math.Max(12, driverGear.Teeth + 10));
            gear.IsInternal = false;
            if (driverGear.IsHelical && !driverGear.IsInternal)
            {
                gear.HelicalDirection = driverGear.HelicalDirection == "R" ? "L" : "R";
            }

            var shaft = CreateShaft();
            shaft.Mount(gear, driverMount.AxialOffset);

            var branches = Assembly.Relations.Count(r => driverShaft.Mounts.Any(m => ReferenceEquals(m.Component, r.Driver)));
            var angle = centreLineAngleDeg ?? branches * 60.0;

            Assembly.AddShaft(shaft);
            try
            {
                Assembly.AddRelation(driverGear, gear, MeshType.Parallel, angle);
            }
            catch
            {
                Assembly.RemoveShaft(shaft);
                throw;
            }

            Selected = gear;
            Refresh();
            return gear;
        }

        /// <summary>
        /// Adds a gear that meshes with <paramref name="driver"/> on a new shaft, for any mesh type: an involute gear on parallel
        /// axes, a rack for a pinion, a bevel gear for a bevel gear (same shaft angle) or a worm wheel for a worm.
        /// The mate takes module, pressure angle and face width from the driver.
        /// </summary>
        /// <param name="driver">The gear to mesh with; it must be mounted in the assembly and suit the mesh type.</param>
        /// <param name="meshType">The kind of mesh.</param>
        /// <param name="teeth">Number of teeth of the mate (of the rack: number of rack teeth); zero or less chooses a default.</param>
        /// <param name="centreLineAngleDeg">Direction of the new shaft from the driver shaft; null spaces branches 60 degrees apart.</param>
        public GearComponent AddMate(GearComponent driver, MeshType meshType, int teeth = 0, double? centreLineAngleDeg = null)
        {
            EnsureNotPlanetary();
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (meshType == MeshType.Parallel) return AddMeshedGear(driver, teeth, centreLineAngleDeg);

            var driverMount = Assembly.FindMount(driver, out var driverShaft)
                              ?? throw new ArgumentException("The driver gear is not mounted in the assembly.", nameof(driver));

            GearComponent mate;
            switch (meshType)
            {
                case MeshType.Rack:
                {
                    var pinion = driver as InvoluteGearComponent
                                 ?? throw new ArgumentException("Only an involute pinion can drive a rack.", nameof(driver));
                    mate = new RackComponent("Rack " + (++_gearCounter))
                    {
                        Teeth = teeth > 0 ? teeth : 12,
                        Module = pinion.Module,
                        PressureAngleDeg = pinion.PressureAngleDeg,
                        FaceWidth = pinion.FaceWidth
                    };
                    break;
                }
                case MeshType.Bevel:
                {
                    var bevel = driver as BevelGearComponent
                                ?? throw new ArgumentException("Only a bevel gear can mesh with a bevel gear.", nameof(driver));
                    mate = new BevelGearComponent("Bevel " + (++_gearCounter))
                    {
                        Teeth = teeth > 0 ? teeth : Math.Max(5, bevel.Teeth * 2),
                        Module = bevel.Module,
                        PressureAngleDeg = bevel.PressureAngleDeg,
                        FaceWidth = bevel.FaceWidth,
                        ShaftAngleDeg = bevel.ShaftAngleDeg
                    };
                    break;
                }
                case MeshType.Worm:
                {
                    var worm = driver as WormComponent
                               ?? throw new ArgumentException("Only a worm can drive a worm wheel.", nameof(driver));
                    mate = new WormWheelComponent("Wheel " + (++_gearCounter))
                    {
                        Teeth = teeth > 0 ? teeth : 30,
                        Module = worm.Module,
                        PressureAngleDeg = worm.PressureAngleDeg
                    };
                    break;
                }
                default:
                    throw new ArgumentException("Unsupported mesh type " + meshType + ".", nameof(meshType));
            }

            var shaft = CreateShaft();
            shaft.Mount(mate, driverMount.AxialOffset);

            var branches = Assembly.Relations.Count(r => driverShaft.Mounts.Any(m => ReferenceEquals(m.Component, r.Driver)));
            var angle = centreLineAngleDeg ?? branches * 60.0;

            Assembly.AddShaft(shaft);
            try
            {
                Assembly.AddRelation(driver, mate, meshType, angle);
            }
            catch
            {
                Assembly.RemoveShaft(shaft);
                throw;
            }

            Selected = mate;
            Refresh();
            return mate;
        }

        /// <summary>
        /// Removes the selected gear or shaft together with everything that hangs off it: a gear that drives other
        /// gears takes those branches with it, and a driven gear takes its whole shaft and the shaft's branches.
        /// </summary>
        public void RemoveSelected()
        {
            EnsureNotPlanetary();
            if (Selected is Shaft shaft)
            {
                RemoveShaftBranch(shaft, new HashSet<Shaft>());
            }
            else if (Selected is GearComponent gear)
            {
                RemoveGearBranch(gear);
            }
            else
            {
                return;
            }

            Selected = null;
            Refresh();
        }

        /// <summary>Selects a shaft or gear (or nothing) and updates the selection highlight and the property editor.</summary>
        public void Select(object tag)
        {
            Selected = tag is Shaft || tag is GearComponent ? tag : null;
            _view.ShowSelection(Selected);
            _view.ShowProperties(CreateProperties(Selected));
            if (Scene != null) _view.ShowScene(Scene, Selected, _hidden);
        }

        public void SetGearVisible(GearComponent gear, bool visible)
        {
            SetGearsVisible(new[] { gear }, visible);
        }

        public void SetGearsVisible(IEnumerable<GearComponent> gears, bool visible)
        {
            foreach (var gear in gears)
            {
                if (visible) _hidden.Remove(gear);
                else _hidden.Add(gear);
            }

            if (Scene != null) _view.ShowScene(Scene, Selected, _hidden);
        }

        /// <summary>
        /// Sets the working centre distance between a driven gear and its driver (mm); zero or less goes back to the distance
        /// that follows from the profile shifts. Only parallel-axis meshes support it.
        /// </summary>
        public void SetCentreDistance(GearComponent driven, double centreDistance)
        {
            EnsureNotPlanetary();
            var relation = Assembly.FindDrivingRelation(driven)
                           ?? throw new ArgumentException("The gear is not driven by another gear.", nameof(driven));
            relation.CentreDistanceOverride = centreDistance > 0.0 ? centreDistance : (double?)null;
            Refresh();
        }

        /// <summary>
        /// Gives the driven gear the profile shift at which its mesh runs at the current centre distance (a fixed distance if one
        /// is set, otherwise the solved one) without a fixed distance, so that the distance follows from the profile shifts.
        /// The driver's profile shift is not changed.
        /// </summary>
        public void FitProfileShiftToCentreDistance(GearComponent driven)
        {
            EnsureNotPlanetary();
            var gear = driven as InvoluteGearComponent
                       ?? throw new ArgumentException("Only involute gears have a profile shift.", nameof(driven));
            var relation = Assembly.FindDrivingRelation(gear)
                           ?? throw new InvalidOperationException($"'{gear.Name}' is not driven by another gear, so there is no centre distance to fit.");
            var driver = relation.Driver as InvoluteGearComponent
                         ?? throw new InvalidOperationException($"'{relation.Driver.Name}' is not an involute gear.");
            var target = relation.CentreDistanceOverride ?? relation.Result?.CentreDistance
                         ?? throw new InvalidOperationException("The train has not been solved yet.");

            var shift = Bolsover.Gears.Calculator.Assemblies.ProfileShiftFitter.RequiredDrivenProfileShift(driver, gear, target);
            gear.ProfileShift = GearPrecision.Round(shift);
            relation.CentreDistanceOverride = null;
            Refresh();
        }

        /// <summary>Turns the input shaft to the given angle (degrees) and rebuilds the scene.</summary>
        public void SetInputAngle(double degrees)
        {
            var rad = degrees * Math.PI / 180.0;
            if (Planetary != null)
            {
                _planetaryInputRad = rad;
                Refresh();
                return;
            }

            var input = InputShaft;
            if (input == null) return;
            input.RotationRad = rad;
            Refresh();
        }

        /// <summary>
        /// Solves the train and updates the tree, property editor, 3D scene and report. A problem (for example differing
        /// modules or a locked-up train) is shown as a message and the previous scene and report stay on screen.
        /// </summary>
        public void Refresh()
        {
            string error = null;
            if (Planetary != null) error = RebuildPlanetary();

            if (error != null)
            {
                // Keep the previous scene and report; the message explains what is wrong.
            }
            else if (Assembly.Shafts.Count > 0)
            {
                try
                {
                    var scene = GearAssemblySceneBuilder.Build(Assembly);
                    var report = Planetary != null
                        ? CreatePlanetaryReport()
                        : GearTrainAnalyzer.Analyze(Assembly, null, _view.InputSpeedRpm);
                    Scene = scene;
                    Report = report;
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException || ex is NotSupportedException)
                {
                    error = ex.Message;
                }
            }
            else
            {
                Scene = new AssemblyScene();
                Report = null;
            }

            _view.ShowTree(BuildTree(), Selected);
            _view.ShowProperties(CreateProperties(Selected));
            if (error == null)
            {
                _view.ShowScene(Scene, Selected, _hidden);
                _view.ShowReport(Report);
                if (Report != null && Report.Warnings.Count > 0)
                {
                    _view.ShowMessage("Warning: " + string.Join(" ", Report.Warnings), false);
                }
                else if (Planetary != null)
                {
                    _view.ShowMessage(
                        $"Planetary stage: {Planetary.Fixed} fixed, {PlanetaryInput} is the input.", false);
                }
                else
                {
                    _view.ShowMessage("Gear train solved.", false);
                }
            }
            else
            {
                _view.ShowMessage(error, true);
            }
        }

        /// <summary>
        /// Gives <paramref name="source"/>'s module and pressure angle to every gear that meshes with it, directly or
        /// through a chain of meshes, because meshing gears must share both.
        /// </summary>
        internal void ApplyMeshCommon(GearComponent source)
        {
            var visited = new HashSet<GearComponent> { source };
            var queue = new Queue<GearComponent>();
            queue.Enqueue(source);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var relation in Assembly.Relations)
                {
                    GearComponent other = null;
                    if (ReferenceEquals(relation.Driver, current)) other = relation.Driven;
                    else if (ReferenceEquals(relation.Driven, current)) other = relation.Driver;
                    if (other == null || !visited.Add(other)) continue;

                    other.Module = source.Module;
                    other.PressureAngleDeg = source.PressureAngleDeg;
                    queue.Enqueue(other);
                }
            }
        }

        internal bool IsInputShaft(Shaft shaft) => ReferenceEquals(shaft, InputShaft);

        private void EnsureNotPlanetary()
        {
            if (Planetary != null)
            {
                throw new InvalidOperationException(
                    "A planetary stage is edited through its properties. Start a new train to add other gears.");
            }
        }

        private void ResetState()
        {
            Assembly = new GearAssembly();
            Planetary = null;
            _hidden.Clear();
            _gearCounter = 0;
            _shaftCounter = 0;
            Scene = null;
            Report = null;
            Selected = null;
        }

        private static int MaxSuffix(IEnumerable<string> ids)
        {
            var max = 0;
            foreach (var id in ids)
            {
                var index = id.LastIndexOf(' ');
                if (index >= 0 && int.TryParse(id.Substring(index + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number > max)
                {
                    max = number;
                }
            }

            return max;
        }

        private bool Guard(Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException
                                       || ex is InvalidOperationException || ex is ArgumentException || ex is NotSupportedException)
            {
                _view.ShowMessage(ex.Message, true);
                return false;
            }
        }

        /// <summary>Poses the planetary stage for the current input angle and replaces the assembly; returns an error message or null.</summary>
        private string RebuildPlanetary()
        {
            try
            {
                var set = Planetary;
                if (PlanetaryInput == set.Fixed)
                {
                    PlanetaryInput = set.Fixed == PlanetaryMember.Sun ? PlanetaryMember.Carrier : PlanetaryMember.Sun;
                }

                set.SolveAngles(PlanetaryInput, _planetaryInputRad);
                var assembly = set.BuildAssembly();

                var selectedGearId = (Selected as GearComponent)?.Id;
                var selectedShaftId = (Selected as Shaft)?.Id;
                var hiddenIds = new HashSet<string>(_hidden.Select(g => g.Id));

                Assembly = assembly;
                _hidden.Clear();
                foreach (var gear in assembly.Components.Where(c => hiddenIds.Contains(c.Id))) _hidden.Add(gear);

                Selected = selectedGearId != null
                    ? assembly.Components.FirstOrDefault(c => c.Id == selectedGearId)
                    : (object)assembly.Shafts.FirstOrDefault(s => s.Id == selectedShaftId);
                return null;
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException || ex is NotSupportedException)
            {
                return ex.Message;
            }
        }

        private GearTrainReport CreatePlanetaryReport()
        {
            var set = Planetary;
            var inputRpm = _view.InputSpeedRpm;
            var output = (PlanetaryMember)(3 - (int)PlanetaryInput - (int)set.Fixed);
            var speeds = set.Analyze(PlanetaryInput, inputRpm, output);

            double SpeedOf(Shaft shaft)
            {
                if (shaft.Id == set.Id + ".SunShaft") return speeds.SunRpm;
                if (shaft.Id == set.Id + ".RingShaft") return speeds.RingRpm;
                if (shaft.Id == set.Id + ".Carrier") return speeds.CarrierRpm;
                return speeds.PlanetRpm;
            }

            var shaftRows = new List<ShaftSpeedRow>();
            foreach (var shaft in Assembly.Shafts)
            {
                var speed = SpeedOf(shaft);
                double? ratio = Math.Abs(speed) > 1e-12 ? GearPrecision.Round(inputRpm / speed) : (double?)null;
                shaftRows.Add(new ShaftSpeedRow(shaft.Id, speed, ratio));
            }

            var gearRows = new List<GearSpeedRow>();
            foreach (var shaft in Assembly.Shafts)
            {
                foreach (var mount in shaft.Mounts)
                {
                    var gear = mount.Component;
                    var speed = SpeedOf(shaft);
                    var role = gear.Id == set.Id + ".Sun" ? "Sun" : gear.Id == set.Id + ".Ring" ? "Ring" : "Planet";
                    gearRows.Add(new GearSpeedRow(
                        gear.Id,
                        shaft.Id,
                        role,
                        gear.ToothCount,
                        GearPrecision.Round(gear.PitchDiameter),
                        speed,
                        GearPrecision.Round(Math.PI * gear.PitchDiameter * Math.Abs(speed) / 60000.0)));
                }
            }

            // Stage ratios are relative to the carrier, as for a gear train with a fixed carrier.
            var stages = new List<GearStageRow>
            {
                new GearStageRow(set.Id + ".Sun", set.Id + ".Planet1", GearPrecision.Round((double)set.PlanetTeeth / set.SunTeeth),
                    GearPrecision.Round(-(double)set.SunTeeth / set.PlanetTeeth), set.CentreDistance),
                new GearStageRow(set.Id + ".Planet1", set.Id + ".Ring", GearPrecision.Round((double)set.RingTeeth / set.PlanetTeeth),
                    GearPrecision.Round((double)set.PlanetTeeth / set.RingTeeth), set.CentreDistance)
            };

            var outputShaftId = set.Id + (output == PlanetaryMember.Sun ? ".SunShaft" : output == PlanetaryMember.Ring ? ".RingShaft" : ".Carrier");
            var inputShaftId = set.Id + (PlanetaryInput == PlanetaryMember.Sun ? ".SunShaft" : PlanetaryInput == PlanetaryMember.Ring ? ".RingShaft" : ".Carrier");
            return new GearTrainReport(inputShaftId, inputRpm, outputShaftId, speeds.OverallRatio, shaftRows, gearRows, stages, new List<string>());
        }

        private InvoluteGearComponent CreateGear(InvoluteGearComponent template, int teeth)
        {
            var gear = new InvoluteGearComponent("Gear " + (++_gearCounter)) { Teeth = Math.Max(1, teeth) };
            if (template != null)
            {
                gear.Module = template.Module;
                gear.PressureAngleDeg = template.PressureAngleDeg;
                gear.FaceWidth = template.FaceWidth;
                gear.HelixAngleDeg = template.HelixAngleDeg;
                gear.HelicalDirection = template.HelicalDirection;
                gear.Clearance = template.Clearance;
                gear.Backlash = template.Backlash;
                gear.ShowFullGear = template.ShowFullGear;
            }

            return gear;
        }

        private Shaft CreateShaft()
        {
            return new Shaft("Shaft " + (++_shaftCounter));
        }

        private void RemoveShaftBranch(Shaft shaft, ISet<Shaft> visited)
        {
            if (!visited.Add(shaft)) return;

            foreach (var mount in shaft.Mounts.ToList())
            {
                foreach (var relation in Assembly.Relations.Where(r => ReferenceEquals(r.Driver, mount.Component)).ToList())
                {
                    if (Assembly.FindMount(relation.Driven, out var drivenShaft) != null)
                    {
                        RemoveShaftBranch(drivenShaft, visited);
                    }
                }
            }

            foreach (var mount in shaft.Mounts.ToList())
            {
                _hidden.Remove(mount.Component);
            }

            Assembly.RemoveShaft(shaft);
        }

        private void RemoveGearBranch(GearComponent gear)
        {
            if (Assembly.FindMount(gear, out var shaft) == null) return;

            // A driven gear is what connects its shaft to the rest of the train, so its whole shaft goes.
            if (Assembly.FindDrivingRelation(gear) != null)
            {
                RemoveShaftBranch(shaft, new HashSet<Shaft>());
                return;
            }

            var visited = new HashSet<Shaft> { shaft };
            foreach (var relation in Assembly.Relations.Where(r => ReferenceEquals(r.Driver, gear)).ToList())
            {
                if (Assembly.FindMount(relation.Driven, out var drivenShaft) != null)
                {
                    RemoveShaftBranch(drivenShaft, visited);
                }
            }

            _hidden.Remove(gear);
            Assembly.RemoveGear(gear);
        }

        private object CreateProperties(object selected)
        {
            if (Planetary != null) return new PlanetaryNodeProperties(this, Planetary);

            if (selected is RackComponent || selected is BevelGearComponent || selected is WormComponent || selected is WormWheelComponent)
            {
                var component = (GearComponent)selected;
                var componentMount = Assembly.FindMount(component, out _);
                if (componentMount == null) return null;
                var relation = Assembly.FindDrivingRelation(component);
                if (component is RackComponent rack) return new RackNodeProperties(this, rack, componentMount, relation);
                if (component is BevelGearComponent bevel) return new BevelNodeProperties(this, bevel, componentMount, relation);
                if (component is WormComponent worm) return new WormNodeProperties(this, worm, componentMount, relation);
                return new WormWheelNodeProperties(this, (WormWheelComponent)component, componentMount, relation);
            }

            if (selected is InvoluteGearComponent gear)
            {
                var mount = Assembly.FindMount(gear, out _);
                if (mount == null) return null;
                return new GearNodeProperties(this, gear, mount, Assembly.FindDrivingRelation(gear));
            }

            if (selected is Shaft shaft && Assembly.Shafts.Contains(shaft))
            {
                return new ShaftNodeProperties(this, shaft);
            }

            return null;
        }

        private TrainTreeNode BuildTree()
        {
            var ci = CultureInfo.InvariantCulture;
            var root = new TrainTreeNode(
                $"Gear train ({Assembly.Shafts.Count} shafts, {Assembly.Components.Count()} gears)",
                TrainNodeKind.Assembly,
                Assembly);

            for (var i = 0; i < Assembly.Shafts.Count; i++)
            {
                var shaft = Assembly.Shafts[i];
                var shaftNode = new TrainTreeNode(i == 0 ? shaft.Id + " (input)" : shaft.Id, TrainNodeKind.Shaft, shaft);
                foreach (var mount in shaft.Mounts)
                {
                    var gear = mount.Component;
                    var relation = Assembly.FindDrivingRelation(gear);
                    var text = $"{gear.Name}  z={gear.ToothCount}  @ {mount.AxialOffset.ToString("0.###", ci)} mm";
                    if (relation != null) text += $"  (driven by {relation.Driver.Name})";
                    shaftNode.Children.Add(new TrainTreeNode(text, TrainNodeKind.Gear, gear) { Visible = !_hidden.Contains(gear) });
                }

                root.Children.Add(shaftNode);
            }

            return root;
        }
    }
}
