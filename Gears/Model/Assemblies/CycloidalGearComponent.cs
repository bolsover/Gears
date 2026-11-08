using System;
using System.Collections.Generic;
using Bolsover.Gears.Calculator.Assemblies;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>The two kinds of gear of a cycloidal pair: cycloidal gears are always designed as a wheel and a pinion.</summary>
    public enum CycloidalGearRole
    {
        /// <summary>The larger gear: teeth with a rounded tip and radial flanks, as half a tooth pitch wide at the pitch circle.</summary>
        Wheel,

        /// <summary>The smaller gear (a leaved pinion): narrower teeth, with a profile that depends on the number of leaves.</summary>
        Pinion
    }

    /// <summary>
    /// A cycloidal gear, as used in clocks and instruments. Cycloidal gears are designed in pairs: the tooth height of the wheel follows
    /// from the number of teeth of the wheel and of the pinion it works with (<see cref="MateTeeth"/>), so a wheel and a pinion always
    /// mesh as a pair of <see cref="MeshType.Cycloidal"/>. The tooth sizes follow the tables for clock wheels and leaves from
    /// http://www.csparks.com/watchmaking/CycloidalGears/index.jhtml, as in the Alibre cycloidal gear add-on.
    /// <para>
    /// Local frame: the axis is Z, the centre is the origin and the face width is centred on the Z = 0 mid-plane. Tooth 0 is centred on
    /// the +X axis. A tooth has a rounded tip made of two circular arcs (the addendum radius), flanks that run radially from the pitch
    /// circle to the root circle, and a root arc between the teeth.
    /// </para>
    /// </summary>
    public class CycloidalGearComponent : GearComponent
    {
        /// <summary>The fewest teeth that are accepted (the leaf tables of the pinion start at six).</summary>
        public const int AbsoluteMinimumTeeth = 6;

        /// <summary>Teeth of the mate that is assumed for a wheel (a pinion) when <see cref="MateTeeth"/> is not set.</summary>
        public const int DefaultPinionTeeth = 8;

        /// <summary>Teeth of the mate that is assumed for a pinion (a wheel) when <see cref="MateTeeth"/> is not set.</summary>
        public const int DefaultWheelTeeth = 30;

        private const double ErrorLimit = 1e-6;
        private const int MaximumIterations = 1000;

        private GearGeometry _cachedGeometry;
        private string _cacheKey;

        public CycloidalGearComponent(string id) : base(id)
        {
            FaceWidth = 10.0;
        }

        /// <summary>Whether this is the wheel or the pinion of its pair.</summary>
        public CycloidalGearRole Role { get; set; } = CycloidalGearRole.Wheel;

        /// <summary>True for a pinion.</summary>
        public bool IsPinion => Role == CycloidalGearRole.Pinion;

        /// <summary>Number of teeth (leaves of a pinion).</summary>
        public int Teeth { get; set; } = DefaultWheelTeeth;

        /// <summary>
        /// Number of teeth of the gear that this one works with; zero for the default. The mesh solver sets it from the mating gear, so
        /// that the two profiles fit. A gear that works with two others takes the profile for the gear it drives.
        /// </summary>
        public int MateTeeth { get; set; }

        /// <summary>The number of teeth of the mate that the profile is calculated for.</summary>
        public int EffectiveMateTeeth => MateTeeth > 0 ? MateTeeth : (IsPinion ? DefaultWheelTeeth : DefaultPinionTeeth);

        /// <summary>True to give the pinion dedendum a clearance of <see cref="CustomSlop"/> over the wheel addendum.</summary>
        public bool CustomSlopEnabled { get; set; }

        /// <summary>Clearance (mm) between the wheel tip and the pinion root, used when <see cref="CustomSlopEnabled"/>.</summary>
        public double CustomSlop { get; set; }

        /// <summary>
        /// Diameter of a hole through the centre (mm); zero for none. The hole is part of the Alibre export; the 3D view shows the solid
        /// gear, as the shaft passes through it.
        /// </summary>
        public double CentreHole { get; set; }

        public override int ToothCount => Teeth;

        public override double PitchDiameter => Module * Teeth;

        public override string TypeName => IsPinion ? "Cycloidal pinion" : "Cycloidal wheel";

        /// <summary>Number of teeth of the wheel of the pair.</summary>
        public int WheelTeeth => IsPinion ? EffectiveMateTeeth : Teeth;

        /// <summary>Number of teeth (leaves) of the pinion of the pair.</summary>
        public int PinionTeeth => IsPinion ? Teeth : EffectiveMateTeeth;

        /// <summary>Gear ratio of the pair: wheel teeth over pinion teeth.</summary>
        public double PairRatio => (double)WheelTeeth / PinionTeeth;

        /// <summary>The factor that the wheel addendum follows from, which depends on the teeth of both gears of the pair.</summary>
        public double AddendumFactor => ComputeAddendumFactor(WheelTeeth, PinionTeeth);

        /// <summary>Addendum of the wheel (mm): 0.95 module times the addendum factor.</summary>
        public double WheelAddendum => Module * 0.95 * AddendumFactor;

        /// <summary>Radius of the arcs of the wheel tooth tip (mm).</summary>
        public double WheelAddendumRadius => Module * 1.4 * AddendumFactor;

        /// <summary>Tooth height above the pitch circle (mm).</summary>
        public double Addendum => IsPinion ? PinionAddendum(Teeth, Module) : WheelAddendum;

        /// <summary>Radius of the arcs of the tooth tip (mm).</summary>
        public double AddendumRadius => IsPinion ? PinionAddendumRadius(Teeth, Module) : WheelAddendumRadius;

        /// <summary>Depth below the pitch circle (mm): the wheel's is half a circular pitch, the pinion's clears the tip of the wheel.</summary>
        public double Dedendum
        {
            get
            {
                if (!IsPinion) return Module * Math.PI / 2.0;
                return CustomSlopEnabled ? WheelAddendum + CustomSlop : Module * (0.95 * AddendumFactor + 0.4);
            }
        }

        /// <summary>
        /// Half the angle of a tooth at the pitch circle (radians). For a wheel a quarter of the tooth angle, so the tooth is half a
        /// pitch wide; for a pinion 1.05 module wide (up to ten leaves) or 1.25 module (more leaves).
        /// </summary>
        public double HalfToothAngle => IsPinion
            ? (Teeth <= 10 ? 1.05 : 1.25) / Teeth
            : Math.PI / (2.0 * Teeth);

        /// <summary>Circular pitch (mm): pi times the module.</summary>
        public double CircularPitch => Math.PI * Module;

        /// <summary>Outside diameter (mm).</summary>
        public double OutsideDiameter => PitchDiameter + 2.0 * Addendum;

        /// <summary>Root diameter (mm).</summary>
        public double RootDiameter => PitchDiameter - 2.0 * Dedendum;

        /// <summary>Throws <see cref="InvalidOperationException"/> when the gear cannot be drawn.</summary>
        public void Validate()
        {
            if (Teeth < AbsoluteMinimumTeeth)
            {
                throw new InvalidOperationException($"Cycloidal gear '{Id}': the number of teeth must be at least {AbsoluteMinimumTeeth}.");
            }

            if (MateTeeth != 0 && MateTeeth < AbsoluteMinimumTeeth)
            {
                throw new InvalidOperationException($"Cycloidal gear '{Id}': the mating gear must have at least {AbsoluteMinimumTeeth} teeth.");
            }

            if (Module <= 0) throw new InvalidOperationException($"Cycloidal gear '{Id}': the module must be positive.");
            if (FaceWidth <= 0) throw new InvalidOperationException($"Cycloidal gear '{Id}': the face width must be positive.");
            if (CustomSlopEnabled && CustomSlop < 0) throw new InvalidOperationException($"Cycloidal gear '{Id}': the clearance must not be negative.");
            if (CentreHole < 0) throw new InvalidOperationException($"Cycloidal gear '{Id}': the centre hole must not be negative.");

            if (RootDiameter <= 0)
            {
                throw new InvalidOperationException($"Cycloidal gear '{Id}': {Teeth} teeth are too few for this module and mate; the root diameter would not be positive.");
            }

            if (CentreHole >= RootDiameter)
            {
                throw new InvalidOperationException(
                    $"Cycloidal gear '{Id}': the centre hole of {CentreHole:0.###} mm must be smaller than the root diameter of {RootDiameter:0.###} mm.");
            }

            if (2.0 * HalfToothAngle >= 2.0 * Math.PI / Teeth)
            {
                throw new InvalidOperationException($"Cycloidal gear '{Id}': the teeth would touch at the pitch circle.");
            }
        }

        /// <summary>
        /// Addendum factor of the wheel: how far a wheel tooth rises above the pitch circle so that its tip meets the flank of the
        /// pinion, in units of the module, from the numbers of teeth of the wheel and of the pinion (an iterative solution).
        /// </summary>
        public static double ComputeAddendumFactor(int wheelTeeth, int pinionTeeth)
        {
            if (wheelTeeth <= 0) throw new ArgumentOutOfRangeException(nameof(wheelTeeth));
            if (pinionTeeth <= 0) throw new ArgumentOutOfRangeException(nameof(pinionTeeth));

            var t0 = 1.0;
            var t1 = 0.0;
            var r2 = 2.0 * wheelTeeth / pinionTeeth;
            for (var i = 0; i < MaximumIterations && Math.Abs(t1 - t0) > ErrorLimit; i++)
            {
                t0 = t1;
                var b = Math.Atan2(Math.Sin(t0), 1.0 + r2 - Math.Cos(t0));
                t1 = Math.PI / pinionTeeth + r2 * b;
            }

            var k = 1.0 + r2;
            var d = Math.Sqrt(1.0 + k * k - 2.0 * k * Math.Cos(t1));
            return 0.25 * pinionTeeth * (1.0 - k + d);
        }

        /// <summary>The tooth height of a pinion above the pitch circle for its number of leaves (mm).</summary>
        public static double PinionAddendum(int leaves, double module)
        {
            if (leaves <= 7) return 0.855 * module; // high ogival
            if (leaves <= 9) return 0.67 * module; // medium ogival
            if (leaves == 10) return 0.525 * module; // round top for a small tooth
            return 0.625 * module; // round top for a wider tooth
        }

        /// <summary>The radius of the arcs of the tip of a pinion tooth for its number of leaves (mm).</summary>
        public static double PinionAddendumRadius(int leaves, double module)
        {
            if (leaves <= 7) return 1.05 * module;
            if (leaves <= 9) return 0.7 * module;
            if (leaves == 10) return 0.525 * module;
            return 0.625 * module;
        }

        /// <summary>
        /// The geometry is built once for a set of parameters and reused. The outline is the closed tooth profile of the whole gear; the
        /// solid is the outline extruded over the face width.
        /// </summary>
        public override GearGeometry BuildGeometry()
        {
            var key = string.Join("|", Role, Teeth, EffectiveMateTeeth, Module, FaceWidth, CustomSlopEnabled, CustomSlop);
            if (_cachedGeometry == null || _cacheKey != key)
            {
                _cachedGeometry = CreateGeometry();
                _cacheKey = key;
            }

            return _cachedGeometry;
        }

        private GearGeometry CreateGeometry()
        {
            var outline = CycloidalProfile.Build(this).Ring;
            var closed = new List<GearPoint>(outline) { outline[0] };
            var mesh = SolidMeshBuilder.Loft(outline, FaceWidth / 2.0, 1.0, -FaceWidth / 2.0, 1.0, true);
            return new GearGeometry(null, new List<List<GearPoint>> { closed }, mesh);
        }
    }
}
