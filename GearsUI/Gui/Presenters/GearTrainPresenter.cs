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
    /// <summary>
    /// Presenter of the gear train editor. It owns a <see cref="GearAssembly"/> and builds the shaft/gear tree,
    /// the 3D scene and the ratio and speed report. Several gears can sit on one shaft (compound gears), and gears
    /// can mesh in chains (idlers) or branch in different directions.
    /// </summary>
    public class GearTrainPresenter
    {
        private const double AxialGap = 2.0;
        private const int DefaultHypoidPinionTeeth = 12;
        private const int DefaultHypoidGearTeeth = 37;

        /// <summary>Default offset of a hypoid pair: a quarter of the mean pitch radius of the gear (mm).</summary>
        private static double DefaultHypoidOffset(double module, int gearTeeth) => 0.25 * module * gearTeeth / 2.0;

        private readonly IGearTrainView _view;
        private readonly NodePropertiesFactory _propertiesFactory;
        private readonly HashSet<GearComponent> _hidden = new HashSet<GearComponent>();
        private readonly List<object> _selection = new List<object>();
        private int _gearCounter;
        private int _shaftCounter;
        private double _planetaryInputRad;
        private int _beltCounter;
        private int _chainCounter;

        public GearTrainPresenter(IGearTrainView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            Assembly = new GearAssembly();
            _propertiesFactory = new NodePropertiesFactory(this);
        }

        public GearAssembly Assembly { get; private set; }

        /// <summary>
        /// The last selected <see cref="Shaft"/>, <see cref="GearComponent"/> or <see cref="TimingBelt"/>; null when nothing is selected.
        /// Assigning an item replaces the whole selection with that one item.
        /// </summary>
        public object Selected
        {
            get => _selection.Count > 0 ? _selection[_selection.Count - 1] : null;
            private set
            {
                _selection.Clear();
                if (IsSelectable(value)) _selection.Add(value);
            }
        }

        /// <summary>All selected items in the order they were selected; the last one is <see cref="Selected"/>.</summary>
        public IReadOnlyList<object> SelectedItems => _selection;

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

        /// <summary>
        /// Starts a new train with one shaft carrying one gear of the given family (a 20 tooth spur or bevel gear, or a worm).
        /// <paramref name="bevelType"/> only applies to a bevel gear.
        /// </summary>
        public void NewTrain(GearFamily family, BevelGearType bevelType = BevelGearType.Standard)
        {
            ResetState();

            GearComponent first;
            switch (family)
            {
                case GearFamily.Bevel:
                    first = new BevelGearComponent("Bevel " + (++_gearCounter)) { Teeth = 20, FaceWidth = 10.0, BevelType = bevelType };
                    break;
                case GearFamily.Worm:
                    first = new WormComponent("Worm " + (++_gearCounter));
                    break;
                case GearFamily.SpiralBevel:
                    first = new SpiralBevelGearComponent("Spiral " + (++_gearCounter)) { Teeth = 20, FaceWidth = 10.0 };
                    break;
                case GearFamily.ZerolBevel:
                    first = new ZerolBevelGearComponent("Zerol " + (++_gearCounter)) { Teeth = 20, FaceWidth = 10.0 };
                    break;
                case GearFamily.Hypoid:
                {
                    var pinion = new HypoidGearComponent("Pinion " + (++_gearCounter))
                    {
                        Teeth = DefaultHypoidPinionTeeth,
                        MateTeeth = DefaultHypoidGearTeeth,
                        IsPinion = true,
                        FaceWidth = 10.0
                    };
                    pinion.OffsetMm = DefaultHypoidOffset(pinion.Module, pinion.MateTeeth);
                    pinion.Hand = SpiralHand.Left;
                    first = pinion;
                    break;
                }
                case GearFamily.Helical:
                {
                    var helical = CreateGear(null, 20);
                    helical.HelixAngleDeg = 15.0;
                    helical.HelicalDirection = "R";
                    first = helical;
                    break;
                }
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

        /// <summary>
        /// Starts a new train with one shaft carrying one HTD timing belt pulley of the given series. <paramref name="teeth"/> of zero or less
        /// chooses 20 grooves, or the recommended minimum of the series when that is larger.
        /// </summary>
        public HtdPulleyComponent NewPulley(HtdSeries series, int teeth = 0)
        {
            ResetState();

            var pulley = CreatePulley(series, teeth);
            var shaft = CreateShaft();
            shaft.Mount(pulley, 0.0);
            Assembly.AddShaft(shaft);

            Selected = pulley;
            Refresh();
            return pulley;
        }

        /// <summary>
        /// Adds an HTD timing belt pulley to a shaft next to the existing parts, with a small axial gap. A pulley has no meshing partner,
        /// so it is not connected to the other gears of the train.
        /// </summary>
        public HtdPulleyComponent AddPulleyToShaft(Shaft shaft, HtdSeries series, int teeth = 0)
        {
            EnsureNotPlanetary();
            if (shaft == null) throw new ArgumentNullException(nameof(shaft));
            if (!Assembly.Shafts.Contains(shaft)) throw new ArgumentException("The shaft is not part of the assembly.", nameof(shaft));

            var pulley = CreatePulley(series, teeth);
            pulley.GetBodyAxialExtent(out var start, out var end);

            var offset = 0.0;
            if (shaft.Mounts.Count > 0)
            {
                var last = double.MinValue;
                foreach (var mount in shaft.Mounts)
                {
                    mount.GetAxialExtent(out _, out var e);
                    if (e > last) last = e;
                }

                offset = last + AxialGap - start;
            }

            shaft.Mount(pulley, GearPrecision.Round(offset));
            Selected = pulley;
            Refresh();
            return pulley;
        }

        /// <summary>
        /// Adds a second pulley of the same belt series on a new shaft and joins it to <paramref name="driver"/> with a belt. The new shaft is
        /// placed by the belt, <paramref name="centreDistance"/> from the driver's shaft (zero or less chooses a distance that leaves a gap of
        /// at least 10 mm between the pulleys). <paramref name="teeth"/> of zero or less gives the new pulley as many grooves as the driver.
        /// </summary>
        public HtdPulleyComponent AddBeltedPulley(HtdPulleyComponent driver, int teeth = 0, double centreDistance = 0.0)
        {
            EnsureNotPlanetary();
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (Assembly.FindMount(driver, out var driverShaft) == null) throw new ArgumentException("The pulley is not part of the assembly.", nameof(driver));

            var driven = CreatePulley(driver.Series, teeth > 0 ? teeth : driver.Teeth);
            driven.FaceWidth = driver.FaceWidth;
            driven.HasFlanges = driver.HasFlanges;
            driven.FlangeHeight = driver.FlangeHeight;
            driven.FlangeThickness = driver.FlangeThickness;

            var shaft = CreateShaft();
            shaft.Diameter = driverShaft.Diameter;
            shaft.Mount(driven, 0.0);

            var belt = new TimingBelt("Belt " + (++_beltCounter), driver, driven, 0.0);
            belt.CentreDistance = centreDistance > 0.0
                ? centreDistance
                : GearPrecision.Round(belt.MinimumCentreDistance + Math.Max(10.0, 0.5 * Math.Max(driver.OutsideDiameter, driven.OutsideDiameter)));

            Assembly.AddShaft(shaft);
            try
            {
                Assembly.AddBelt(belt);
            }
            catch
            {
                Assembly.RemoveShaft(shaft);
                throw;
            }

            Selected = driven;
            Refresh();
            return driven;
        }

        /// <summary>Starts a new train with one shaft carrying one ISO 606 roller chain sprocket of the given chain series. <paramref name="teeth"/> of zero or less chooses 19 teeth.</summary>
        public SprocketComponent NewSprocket(ChainSeries series, int teeth = 0)
        {
            ResetState();

            var sprocket = CreateSprocket(series, teeth);
            var shaft = CreateShaft();
            shaft.Mount(sprocket, 0.0);
            Assembly.AddShaft(shaft);

            Selected = sprocket;
            Refresh();
            return sprocket;
        }

        /// <summary>
        /// Adds a roller chain sprocket to a shaft next to the existing parts, with a small axial gap. A sprocket has no meshing partner, so it is
        /// not connected to the other gears of the train.
        /// </summary>
        public SprocketComponent AddSprocketToShaft(Shaft shaft, ChainSeries series, int teeth = 0)
        {
            EnsureNotPlanetary();
            if (shaft == null) throw new ArgumentNullException(nameof(shaft));
            if (!Assembly.Shafts.Contains(shaft)) throw new ArgumentException("The shaft is not part of the assembly.", nameof(shaft));

            var sprocket = CreateSprocket(series, teeth);
            sprocket.GetBodyAxialExtent(out var start, out _);

            var offset = 0.0;
            if (shaft.Mounts.Count > 0)
            {
                var last = double.MinValue;
                foreach (var mount in shaft.Mounts)
                {
                    mount.GetAxialExtent(out _, out var e);
                    if (e > last) last = e;
                }

                offset = last + AxialGap - start;
            }

            shaft.Mount(sprocket, GearPrecision.Round(offset));
            Selected = sprocket;
            Refresh();
            return sprocket;
        }

        /// <summary>
        /// Adds a second sprocket of the same chain on a new shaft and joins it to <paramref name="driver"/> with a roller chain. The new shaft is
        /// placed by the chain, <paramref name="centreDistance"/> from the driver's shaft (zero or less chooses 30 pitches, or a distance that leaves a
        /// gap of at least 10 mm between the sprockets when that is larger). <paramref name="teeth"/> of zero or less gives the new sprocket as many
        /// teeth as the driver.
        /// </summary>
        public SprocketComponent AddChainedSprocket(SprocketComponent driver, int teeth = 0, double centreDistance = 0.0)
        {
            EnsureNotPlanetary();
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (Assembly.FindMount(driver, out var driverShaft) == null) throw new ArgumentException("The sprocket is not part of the assembly.", nameof(driver));

            var driven = CreateSprocket(driver.Series, teeth > 0 ? teeth : driver.Teeth);
            driven.ToothForm = driver.ToothForm;
            driven.FaceWidth = driver.FaceWidth;

            var shaft = CreateShaft();
            shaft.Diameter = driverShaft.Diameter;
            shaft.Mount(driven, 0.0);

            var chain = new RollerChain("Chain " + (++_chainCounter), driver, driven, 0.0);
            chain.CentreDistance = centreDistance > 0.0
                ? centreDistance
                : GearPrecision.Round(Math.Max(30.0 * driver.Profile.Pitch,
                    chain.MinimumCentreDistance + Math.Max(10.0, 0.5 * Math.Max(driver.OutsideDiameter, driven.OutsideDiameter))));

            Assembly.AddShaft(shaft);
            try
            {
                Assembly.AddChain(chain);
            }
            catch
            {
                Assembly.RemoveShaft(shaft);
                throw;
            }

            Selected = driven;
            Refresh();
            return driven;
        }

        private SprocketComponent CreateSprocket(ChainSeries series, int teeth)
        {
            return new SprocketComponent("Sprocket " + (++_gearCounter))
            {
                Series = series,
                Teeth = teeth > 0 ? teeth : 19
            };
        }

        private HtdPulleyComponent CreatePulley(HtdSeries series, int teeth)
        {
            var profile = HtdProfile.Get(series);
            return new HtdPulleyComponent("Pulley " + (++_gearCounter))
            {
                Series = series,
                Teeth = teeth > 0 ? teeth : Math.Max(20, profile.MinimumTeeth),
                FaceWidth = profile.StandardBeltWidth
            };
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
            _beltCounter = MaxSuffix(assembly.Belts.Select(b => b.Id));
            _chainCounter = MaxSuffix(assembly.Chains.Select(c => c.Id));
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
        /// pressure angle, face width and herringbone option, uses the opposite helix hand for an external pair, and sits in the same plane as the driver.
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
        /// Starts a new train with a cycloidal wheel on one shaft and a cycloidal pinion on a second shaft that meshes with it. Cycloidal gears are
        /// always designed as a pair, so both are drawn together; further gears can be added with <see cref="AddCycloidalMate"/> and
        /// <see cref="AddCycloidalGearToShaft"/>. <paramref name="wheelTeeth"/> or <paramref name="pinionTeeth"/> of zero or less choose 30 and 8.
        /// </summary>
        public CycloidalGearComponent NewCycloidalPair(int wheelTeeth = 0, int pinionTeeth = 0)
        {
            ResetState();

            var wheel = new CycloidalGearComponent("Wheel " + (++_gearCounter))
            {
                Role = CycloidalGearRole.Wheel,
                Teeth = wheelTeeth > 0 ? wheelTeeth : CycloidalGearComponent.DefaultWheelTeeth
            };
            var shaft = CreateShaft();
            shaft.Mount(wheel, 0.0);
            Assembly.AddShaft(shaft);

            Selected = wheel;
            AddCycloidalMate(wheel, pinionTeeth, 0.0);
            return wheel;
        }

        /// <summary>
        /// Adds a cycloidal gear on a new shaft that meshes with <paramref name="driver"/>: a pinion for a wheel and a wheel for a pinion.
        /// The new gear takes the module and face width of the driver and sits in the same plane. A gear that is both driven and a driver is
        /// an idler; several gears driven by one driver form a branching train.
        /// </summary>
        /// <param name="driver">The cycloidal gear to mesh with; it must be mounted in the assembly.</param>
        /// <param name="teeth">Number of teeth of the new gear; zero or less chooses 8 for a pinion, and 30 (at least three times the pinion) for a wheel.</param>
        /// <param name="centreLineAngleDeg">Direction of the new shaft from the driver shaft; null spaces branches 60 degrees apart.</param>
        public CycloidalGearComponent AddCycloidalMate(GearComponent driver, int teeth = 0, double? centreLineAngleDeg = null)
        {
            EnsureNotPlanetary();
            var driverGear = driver as CycloidalGearComponent
                             ?? throw new ArgumentException("Only a cycloidal gear can be meshed with a cycloidal gear.", nameof(driver));
            var driverMount = Assembly.FindMount(driverGear, out var driverShaft)
                              ?? throw new ArgumentException("The driver gear is not mounted in the assembly.", nameof(driver));

            var mate = CreateCycloidalGear(driverGear.IsPinion ? CycloidalGearRole.Wheel : CycloidalGearRole.Pinion, teeth, driverGear);
            mate.MateTeeth = driverGear.Teeth;

            var shaft = CreateShaft();
            shaft.Mount(mate, driverMount.AxialOffset);

            var branches = Assembly.Relations.Count(r => driverShaft.Mounts.Any(m => ReferenceEquals(m.Component, r.Driver)));
            var angle = centreLineAngleDeg ?? branches * 60.0;

            Assembly.AddShaft(shaft);
            try
            {
                Assembly.AddRelation(driverGear, mate, MeshType.Cycloidal, angle);
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
        /// Adds another cycloidal gear to a shaft next to the existing gears (a compound gear), with a small axial gap. Unless
        /// <paramref name="role"/> is given the new gear is the opposite kind to the last cycloidal gear on the shaft (a wheel and a pinion on
        /// one shaft are the usual compound pair), and a wheel when there is none. The gear takes the module and face width of that gear.
        /// </summary>
        public CycloidalGearComponent AddCycloidalGearToShaft(Shaft shaft, CycloidalGearRole? role = null, int teeth = 0)
        {
            EnsureNotPlanetary();
            if (shaft == null) throw new ArgumentNullException(nameof(shaft));
            if (!Assembly.Shafts.Contains(shaft)) throw new ArgumentException("The shaft is not part of the assembly.", nameof(shaft));

            var template = shaft.Mounts.Select(m => m.Component).OfType<CycloidalGearComponent>().LastOrDefault();
            var kind = role ?? (template != null && !template.IsPinion ? CycloidalGearRole.Pinion : CycloidalGearRole.Wheel);
            var gear = CreateCycloidalGear(kind, teeth, template);

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

        private CycloidalGearComponent CreateCycloidalGear(CycloidalGearRole role, int teeth, CycloidalGearComponent template)
        {
            int defaultTeeth;
            if (role == CycloidalGearRole.Pinion)
            {
                defaultTeeth = template != null && template.Teeth <= CycloidalGearComponent.DefaultPinionTeeth
                    ? CycloidalGearComponent.AbsoluteMinimumTeeth
                    : CycloidalGearComponent.DefaultPinionTeeth;
            }
            else
            {
                defaultTeeth = Math.Max(CycloidalGearComponent.DefaultWheelTeeth, template != null ? template.Teeth * 3 : 0);
            }

            var gear = new CycloidalGearComponent((role == CycloidalGearRole.Pinion ? "Pinion " : "Wheel ") + (++_gearCounter))
            {
                Role = role,
                Teeth = teeth > 0 ? teeth : defaultTeeth
            };
            if (template != null)
            {
                gear.Module = template.Module;
                gear.PressureAngleDeg = template.PressureAngleDeg;
                gear.FaceWidth = template.FaceWidth;
                gear.CustomSlopEnabled = template.CustomSlopEnabled;
                gear.CustomSlop = template.CustomSlop;
            }

            return gear;
        }

        /// <summary>
        /// Adds a gear that meshes with <paramref name="driver"/> on a new shaft, for any mesh type: an involute gear on parallel
        /// axes, a rack for a pinion, a bevel gear for a bevel gear (same shaft angle), a worm wheel for a worm or a helical gear on crossed
        /// shafts. The mate takes module, pressure angle and face width from the driver. A crossed helical mate has the hand of the driver and the
        /// helix angle that puts the shafts at 90 degrees (45 degrees when the driver is a spur gear); the shaft angle of the relation follows from
        /// the two helix angles and hands.
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
            if (meshType == MeshType.Cycloidal) return AddCycloidalMate(driver, teeth, centreLineAngleDeg);

            var driverMount = Assembly.FindMount(driver, out var driverShaft)
                              ?? throw new ArgumentException("The driver gear is not mounted in the assembly.", nameof(driver));

            GearComponent mate;
            var shaftAngle = 0.0;
            var relationOffset = 0.0;
            switch (meshType)
            {
                case MeshType.CrossedHelical:
                    mate = CreateCrossedHelicalMate(driver, teeth, out shaftAngle);
                    break;
                case MeshType.Rack:
                    mate = CreateRackMate(driver, teeth);
                    break;
                case MeshType.Bevel:
                    mate = CreateBevelMate(driver, teeth);
                    break;
                case MeshType.Hypoid:
                    mate = CreateHypoidMate(driver, teeth, out relationOffset);
                    break;
                case MeshType.Worm:
                    mate = CreateWormMate(driver, teeth);
                    break;
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
                Assembly.AddRelation(driver, mate, meshType, angle, shaftAngle, relationOffset);
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

        private GearComponent CreateCrossedHelicalMate(GearComponent driver, int teeth, out double shaftAngle)
        {
            var helical = driver as InvoluteGearComponent
                          ?? throw new ArgumentException("Only an involute gear can mesh on crossed shafts.", nameof(driver));
            if (helical.IsInternal || helical.IsHerringbone)
            {
                throw new ArgumentException("Internal and herringbone gears cannot mesh on crossed shafts.", nameof(driver));
            }

            var crossed = CreateGear(helical, teeth > 0 ? teeth : 12);
            crossed.IsInternal = false;
            crossed.IsHerringbone = false;
            crossed.HelicalDirection = helical.HelicalDirection;
            crossed.HelixAngleDeg = helical.HelixAngleDeg > 1e-6 && helical.HelixAngleDeg < 90.0 ? 90.0 - helical.HelixAngleDeg : 45.0;
            shaftAngle = Bolsover.Gears.Calculator.HelicalPairGeometry.ExpectedShaftAngleDeg(
                helical.HelixAngleDeg, helical.HelicalDirection, crossed.HelixAngleDeg, crossed.HelicalDirection);
            return crossed;
        }

        private GearComponent CreateRackMate(GearComponent driver, int teeth)
        {
            var pinion = driver as InvoluteGearComponent
                         ?? throw new ArgumentException("Only an involute pinion can drive a rack.", nameof(driver));
            return new RackComponent("Rack " + (++_gearCounter))
            {
                Teeth = teeth > 0 ? teeth : 12,
                Module = pinion.Module,
                PressureAngleDeg = pinion.PressureAngleDeg,
                FaceWidth = pinion.FaceWidth,
                HelixAngleDeg = pinion.IsHelical ? pinion.HelixAngleDeg : 0.0,
                HelicalDirection = pinion.HelicalDirection == "R" ? "L" : "R",
                IsHerringbone = pinion.IsHelical && pinion.IsHerringbone
            };
        }

        private GearComponent CreateBevelMate(GearComponent driver, int teeth)
        {
            var bevel = driver as BevelGearComponent
                        ?? throw new ArgumentException("Only a bevel gear can mesh with a bevel gear.", nameof(driver));
            if (bevel is HypoidGearComponent)
            {
                throw new ArgumentException("A hypoid gear meshes on a hypoid mesh, with a shaft offset.", nameof(driver));
            }

            // The mate of a spiral or zerol gear is of the same kind with the opposite hand and the same spiral angle and cutter radius.
            BevelGearComponent bevelMate;
            if (bevel is SpiralBevelGearComponent spiral)
            {
                SpiralBevelGearComponent spiralMate = spiral is ZerolBevelGearComponent
                    ? new ZerolBevelGearComponent("Zerol " + (++_gearCounter))
                    : new SpiralBevelGearComponent("Spiral " + (++_gearCounter));
                spiralMate.MeanSpiralAngleDeg = spiral.MeanSpiralAngleDeg;
                spiralMate.CutterRadius = spiral.CutterRadius;
                spiralMate.Hand = spiral.Hand == SpiralHand.Right ? SpiralHand.Left : SpiralHand.Right;
                bevelMate = spiralMate;
            }
            else
            {
                bevelMate = new BevelGearComponent("Bevel " + (++_gearCounter));
            }

            bevelMate.Teeth = teeth > 0 ? teeth : Math.Max(5, bevel.Teeth * 2);
            bevelMate.Module = bevel.Module;
            bevelMate.PressureAngleDeg = bevel.PressureAngleDeg;
            bevelMate.FaceWidth = bevel.FaceWidth;
            bevelMate.ShaftAngleDeg = bevel.ShaftAngleDeg;
            bevelMate.BevelType = bevel.BevelType;
            bevelMate.RackTipRadiusFactor = bevel.RackTipRadiusFactor;
            return bevelMate;
        }

        private GearComponent CreateHypoidMate(GearComponent driver, int teeth, out double relationOffset)
        {
            var hypoid = driver as HypoidGearComponent
                         ?? throw new ArgumentException("Only a hypoid gear can mesh with a hypoid gear.", nameof(driver));

            // The mate of the pinion is the gear and the other way round; both share the gear's mean module and the pinion's spiral angle.
            var mateIsPinion = !hypoid.IsPinion;
            var mateTeeth = teeth > 0
                ? teeth
                : mateIsPinion ? Math.Max(5, (int)Math.Round(hypoid.Teeth / 3.1)) : (int)Math.Round(hypoid.Teeth * 3.1);
            var gearTeeth = hypoid.IsPinion ? mateTeeth : hypoid.Teeth;
            relationOffset = Math.Abs(hypoid.OffsetMm) > 1e-9 ? hypoid.OffsetMm : DefaultHypoidOffset(hypoid.Module, gearTeeth);

            var hypoidMate = new HypoidGearComponent((mateIsPinion ? "Pinion " : "Gear ") + (++_gearCounter))
            {
                IsPinion = mateIsPinion,
                Teeth = mateTeeth,
                MateTeeth = hypoid.Teeth,
                Module = hypoid.Module,
                PressureAngleDeg = hypoid.PressureAngleDeg,
                FaceWidth = hypoid.FaceWidth,
                ShaftAngleDeg = hypoid.ShaftAngleDeg,
                MeanSpiralAngleDeg = hypoid.MeanSpiralAngleDeg,
                CutterRadius = hypoid.CutterRadius,
                RackTipRadiusFactor = hypoid.RackTipRadiusFactor
            };
            hypoid.MateTeeth = mateTeeth;
            ApplyHypoidOffset(hypoid, hypoidMate, relationOffset);
            return hypoidMate;
        }

        private GearComponent CreateWormMate(GearComponent driver, int teeth)
        {
            var worm = driver as WormComponent
                       ?? throw new ArgumentException("Only a worm can drive a worm wheel.", nameof(driver));
            return new WormWheelComponent("Wheel " + (++_gearCounter))
            {
                Teeth = teeth > 0 ? teeth : 30,
                Module = worm.Module,
                PressureAngleDeg = worm.PressureAngleDeg
            };
        }

        /// <summary>The gear that meshes with a bevel gear (spiral, zerol or hypoid included) on a bevel or hypoid mesh; null when there is none.</summary>
        public GearComponent FindBevelPartner(GearComponent gear)
        {
            foreach (var relation in Assembly.Relations)
            {
                if (relation.MeshType != MeshType.Bevel && relation.MeshType != MeshType.Hypoid) continue;
                if (ReferenceEquals(relation.Driver, gear)) return relation.Driven;
                if (ReferenceEquals(relation.Driven, gear)) return relation.Driver;
            }

            return null;
        }

        /// <summary>
        /// Sets the hand of a spiral or zerol gear and the opposite hand on the gear it meshes with. The hands of a hypoid pair follow from the
        /// side of the offset and cannot be set.
        /// </summary>
        public void SetSpiralHand(SpiralBevelGearComponent gear, SpiralHand hand)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            if (gear is HypoidGearComponent)
            {
                throw new InvalidOperationException("The hand of a hypoid gear follows from the side of the offset; change the offset instead.");
            }

            gear.Hand = hand;
            if (FindBevelPartner(gear) is SpiralBevelGearComponent partner)
            {
                partner.Hand = hand == SpiralHand.Right ? SpiralHand.Left : SpiralHand.Right;
            }
        }

        /// <summary>
        /// Sets the mean spiral angle (degrees; the pinion's for a hypoid pair) and the cutter radius (mm, zero is automatic) of a gear and of the gear
        /// it meshes with, which must agree. A null value is left as it is.
        /// </summary>
        public void SetSpiralParameters(SpiralBevelGearComponent gear, double? meanSpiralAngleDeg, double? cutterRadius)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            var partner = FindBevelPartner(gear) as SpiralBevelGearComponent;
            foreach (var g in new[] { gear, partner })
            {
                if (g == null) continue;
                if (meanSpiralAngleDeg.HasValue) g.MeanSpiralAngleDeg = meanSpiralAngleDeg.Value;
                if (cutterRadius.HasValue) g.CutterRadius = Math.Max(0.0, cutterRadius.Value);
            }
        }

        /// <summary>
        /// Sets the offset of a hypoid pair (mm, positive or negative for the two sides) on the relation and on both gears, and gives the gears the
        /// hands that the side of the offset needs (the gear right hand and the pinion left hand for a positive offset).
        /// </summary>
        public void SetHypoidOffset(HypoidGearComponent gear, double offsetMm)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            var partner = FindBevelPartner(gear) as HypoidGearComponent;
            ApplyHypoidOffset(gear, partner, offsetMm);
            foreach (var relation in Assembly.Relations)
            {
                if (relation.MeshType != MeshType.Hypoid) continue;
                if (ReferenceEquals(relation.Driver, gear) || ReferenceEquals(relation.Driven, gear)) relation.OffsetMm = offsetMm;
            }
        }

        private static void ApplyHypoidOffset(HypoidGearComponent first, HypoidGearComponent second, double offsetMm)
        {
            foreach (var g in new[] { first, second })
            {
                if (g == null) continue;
                g.OffsetMm = offsetMm;
                var gearHand = offsetMm >= 0.0 ? SpiralHand.Right : SpiralHand.Left;
                g.Hand = g.IsPinion ? (gearHand == SpiralHand.Right ? SpiralHand.Left : SpiralHand.Right) : gearHand;
            }
        }

        /// <summary>
        /// Removes the selected gear or shaft together with everything that hangs off it: a gear that drives other
        /// gears takes those branches with it, and a driven gear takes its whole shaft and the shaft's branches.
        /// </summary>
        public void RemoveSelected()
        {
            EnsureNotPlanetary();
            var removed = Selected;
            if (Selected is Shaft shaft)
            {
                RemoveShaftBranch(shaft, new HashSet<Shaft>());
            }
            else if (Selected is GearComponent gear)
            {
                RemoveGearBranch(gear);
            }
            else if (Selected is TimingBelt belt)
            {
                Assembly.RemoveBelt(belt);
            }
            else if (Selected is RollerChain chain)
            {
                Assembly.RemoveChain(chain);
            }
            else
            {
                return;
            }

            _selection.Remove(removed);
            Refresh();
        }

        /// <summary>Selects a shaft, gear or belt (or nothing), replacing the selection, and updates the highlight and the property editor.</summary>
        public void Select(object tag)
        {
            Selected = tag;
            PublishSelection();
        }

        /// <summary>
        /// Adds the shaft, gear or belt to the selection, or removes it when it is already selected. The last selected item
        /// feeds the property editor. Anything else is ignored.
        /// </summary>
        public void ToggleSelection(object tag)
        {
            if (!IsSelectable(tag)) return;
            if (!_selection.Remove(tag)) _selection.Add(tag);
            PublishSelection();
        }

        private static bool IsSelectable(object tag) => tag is Shaft || tag is GearComponent || tag is TimingBelt || tag is RollerChain;

        private void PublishSelection()
        {
            _view.ShowSelection(Selected);
            _view.ShowProperties(CreateProperties(Selected));
            if (Scene != null) _view.ShowScene(Scene, _selection, _hidden);
        }

        /// <summary>Drops selected items that are no longer part of the assembly.</summary>
        private void PruneSelection()
        {
            _selection.RemoveAll(item => !IsInAssembly(item));
        }

        private bool IsInAssembly(object item)
        {
            if (item is Shaft shaft) return Assembly.Shafts.Contains(shaft);
            if (item is TimingBelt belt) return Assembly.Belts.Contains(belt);
            if (item is RollerChain chain) return Assembly.Chains.Contains(chain);
            if (item is GearComponent gear) return Assembly.Components.Contains(gear);
            return false;
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

            if (Scene != null) _view.ShowScene(Scene, _selection, _hidden);
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

            PruneSelection();
            _view.ShowTree(BuildTree(), Selected);
            _view.ShowProperties(CreateProperties(Selected));
            if (error == null)
            {
                _view.ShowScene(Scene, _selection, _hidden);
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
            _beltCounter = 0;
            _chainCounter = 0;
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
            return PlanetaryReportBuilder.Build(Planetary, PlanetaryInput, _view.InputSpeedRpm, Assembly);
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
                gear.IsHerringbone = template.IsHerringbone;
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
            return _propertiesFactory.Create(selected);
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
                    var drivingBelt = Assembly.FindDrivingBelt(gear);
                    if (drivingBelt != null) text += $"  (belt from {drivingBelt.Driver.Name})";
                    var drivingChain = Assembly.FindDrivingChain(gear);
                    if (drivingChain != null) text += $"  (chain from {drivingChain.Driver.Name})";
                    shaftNode.Children.Add(new TrainTreeNode(text, TrainNodeKind.Gear, gear) { Visible = !_hidden.Contains(gear) });
                }

                root.Children.Add(shaftNode);
            }

            foreach (var belt in Assembly.Belts)
            {
                var beltText = $"{belt.Name}  HTD {belt.Profile.Name}  {belt.Driver.Name} -> {belt.Driven.Name}  {belt.BeltTeeth} teeth";
                root.Children.Add(new TrainTreeNode(beltText, TrainNodeKind.Belt, belt));
            }

            foreach (var chain in Assembly.Chains)
            {
                var chainText = $"{chain.Name}  ISO 606 {chain.Profile.Name}  {chain.Driver.Name} -> {chain.Driven.Name}  {chain.LinkCount} links";
                root.Children.Add(new TrainTreeNode(chainText, TrainNodeKind.Chain, chain));
            }

            return root;
        }
    }
}
