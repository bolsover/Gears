using System;
using System.Collections.Generic;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Assemblies;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// A straight bevel gear built with the Tredgold (back-cone) approximation.
    /// <para>
    /// The tooth profile at the large end is the involute profile of a virtual spur gear with
    /// <c>Zv = z / cos(delta)</c> teeth, mapped onto the plane perpendicular to the axis (radius times z / Zv, angle times
    /// Zv / z, so the pitch radius stays <c>m * z / 2</c>). The small end is the same outline scaled towards the cone apex
    /// by <c>(Re - b) / Re</c>, where Re is the cone distance and b the face width along the cone.
    /// </para>
    /// <para>
    /// Local frame: the axis is Z, the large end lies at the lower Z, the apex of the pitch cone lies on the +Z axis at
    /// <see cref="ApexAxialPosition"/>, and the face is centred on the Z = 0 mid-plane. The module is the module at the
    /// large end. The pitch cone angle follows from the tooth counts of the gear and its mate and from the shaft angle;
    /// the mesh solver sets <see cref="MateTeeth"/> and <see cref="ShaftAngleDeg"/> on both gears of a pair, so a bevel
    /// gear can mesh with one mate only.
    /// </para>
    /// </summary>
    public class BevelGearComponent : GearComponent
    {
        public BevelGearComponent(string id) : base(id)
        {
        }

        public int Teeth { get; set; } = 20;

        /// <summary>Tooth count of the mating gear; zero means "same as this gear".</summary>
        public int MateTeeth { get; set; }

        /// <summary>Angle between the two shaft axes in degrees (90 for a standard bevel pair).</summary>
        public double ShaftAngleDeg { get; set; } = 90.0;

        /// <summary>Circular backlash (mm).</summary>
        public double Backlash { get; set; }

        /// <summary>Bottom clearance (mm).</summary>
        public double Clearance => 0.25 * Module;

        public override int ToothCount => Teeth;

        /// <summary>Pitch diameter at the large end (mm).</summary>
        public override double PitchDiameter => Module * Teeth;

        public override string TypeName => "Bevel gear";

        private int EffectiveMateTeeth => MateTeeth > 0 ? MateTeeth : Teeth;

        /// <summary>Pitch cone half angle in radians: tan(delta) = sin(sigma) / (z_mate / z + cos(sigma)).</summary>
        public double PitchConeAngleRad
        {
            get
            {
                var sigma = ShaftAngleDeg * Math.PI / 180.0;
                return Math.Atan2(Math.Sin(sigma), EffectiveMateTeeth / (double)Teeth + Math.Cos(sigma));
            }
        }

        public double PitchConeAngleDeg => PitchConeAngleRad * 180.0 / Math.PI;

        /// <summary>Distance from the cone apex to the pitch circle at the large end (mm).</summary>
        public double ConeDistance => PitchDiameter / 2.0 / Math.Sin(PitchConeAngleRad);

        /// <summary>Position of the cone apex along the gear axis, from the face mid-plane (mm).</summary>
        public double ApexAxialPosition => Math.Cos(PitchConeAngleRad) * (ConeDistance - FaceWidth / 2.0);

        /// <summary>Number of teeth of the virtual spur gear used for the tooth profile.</summary>
        public int VirtualTeeth => Math.Max(Teeth, (int)Math.Round(Teeth / Math.Cos(PitchConeAngleRad)));

        public void Validate()
        {
            if (Teeth < 5) throw new InvalidOperationException($"Bevel gear '{Id}': the number of teeth must be at least 5.");
            if (Module <= 0) throw new InvalidOperationException($"Bevel gear '{Id}': the module must be positive.");
            if (FaceWidth <= 0) throw new InvalidOperationException($"Bevel gear '{Id}': the face width must be positive.");
            if (PressureAngleDeg <= 0 || PressureAngleDeg >= 45)
            {
                throw new InvalidOperationException($"Bevel gear '{Id}': the pressure angle must be between 0 and 45 degrees.");
            }

            if (ShaftAngleDeg <= 0 || ShaftAngleDeg >= 180)
            {
                throw new InvalidOperationException($"Bevel gear '{Id}': the shaft angle must be between 0 and 180 degrees.");
            }

            if (FaceWidth >= ConeDistance)
            {
                throw new InvalidOperationException(
                    $"Bevel gear '{Id}': the face width ({FaceWidth}) must be smaller than the cone distance ({GearPrecision.Round(ConeDistance)}). " +
                    "A face width of at most one third of the cone distance is usual.");
            }
        }

        public override GearGeometry BuildGeometry()
        {
            Validate();

            var virtualTeeth = VirtualTeeth;
            var profile = AlibreGearGenerator.GenerateExternalToothProfile(
                virtualTeeth, Module, PressureAngleDeg, 0.0, false, null, Clearance, Backlash, 0.0);

            var radiusScale = Teeth / (double)virtualTeeth;
            var angleScale = virtualTeeth / (double)Teeth;
            var tooth = new List<List<GearPoint>>();
            foreach (var segment in AlibreGearGenerator.BuildToothOutline(profile, false))
            {
                var mapped = new List<GearPoint>(segment.Count);
                foreach (var point in segment)
                {
                    var radius = Math.Sqrt(point.X * point.X + point.Y * point.Y) * radiusScale;
                    var angle = Math.Atan2(point.Y, point.X) * angleScale;
                    mapped.Add(new GearPoint(radius * Math.Cos(angle), radius * Math.Sin(angle)));
                }

                tooth.Add(mapped);
            }

            var polylines = AlibreGearGenerator.BuildFullGear(Teeth, tooth);
            var outline = SolidMeshBuilder.Flatten(polylines);

            var cone = PitchConeAngleRad;
            var halfAxial = FaceWidth * Math.Cos(cone) / 2.0;
            var smallEndScale = (ConeDistance - FaceWidth) / ConeDistance;
            var mesh = SolidMeshBuilder.Loft(outline, halfAxial, smallEndScale, -halfAxial, 1.0, true);

            return new GearGeometry(profile, polylines, mesh);
        }
    }
}
