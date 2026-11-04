using System;
using System.Collections.Generic;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Assemblies;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>Tooth proportion system of a straight bevel gear.</summary>
    public enum BevelGearType
    {
        /// <summary>Equal addendum m on both gears and a bottom clearance of 0.25 m.</summary>
        Standard = 0,

        /// <summary>
        /// Gleason straight bevel proportions: whole depth 2.188 m + 0.051 mm, clearance 0.188 m + 0.051 mm and unequal
        /// addenda (long pinion addendum, short gear addendum) that depend on the gear ratio.
        /// </summary>
        Gleason = 1
    }

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

        /// <summary>Gleason whole depth is 2.188 m + this constant (mm); the clearance is 0.188 m + the same constant.</summary>
        public const double GleasonDepthConstant = 0.051;

        public int Teeth { get; set; } = 20;

        /// <summary>Tooth proportion system; the mesh solver sets it on both gears of a pair, because the two gears must agree.</summary>
        public BevelGearType BevelType { get; set; } = BevelGearType.Standard;

        /// <summary>Tooth count of the mating gear; zero means "same as this gear".</summary>
        public int MateTeeth { get; set; }

        /// <summary>Angle between the two shaft axes in degrees (90 for a standard bevel pair).</summary>
        public double ShaftAngleDeg { get; set; } = 90.0;

        /// <summary>Circular backlash (mm).</summary>
        public double Backlash { get; set; }

        /// <summary>Bottom clearance (mm): 0.25 m (Standard) or 0.188 m + 0.051 mm (Gleason).</summary>
        public double Clearance => BevelType == BevelGearType.Gleason ? 0.188 * Module + GleasonDepthConstant : 0.25 * Module;

        /// <summary>Whole tooth depth at the large end (mm): 2 m + clearance.</summary>
        public double WholeDepth => 2.0 * Module + Clearance;

        /// <summary>
        /// Ratio of the virtual tooth numbers of this gear and its mate, <c>(z / cos delta) / (zMate / cos deltaMate)</c>.
        /// For a 90 degree pair this equals the square of the tooth ratio, (z / zMate)^2.
        /// </summary>
        public double VirtualToothRatio
        {
            get
            {
                var sigma = ShaftAngleDeg * Math.PI / 180.0;
                var mateCone = sigma - PitchConeAngleRad;
                var zv = Teeth / Math.Cos(PitchConeAngleRad);
                var zvMate = EffectiveMateTeeth / Math.Cos(mateCone);
                return zv / zvMate;
            }
        }

        /// <summary>
        /// Addendum at the large end (mm). Standard: m. Gleason: the gear (virtual ratio q &gt;= 1) has
        /// <c>0.54 m + 0.46 m / u^2</c>, where u = sqrt(q) is the equivalent 90 degree gear ratio (the tooth ratio z / zMate for a
        /// 90 degree pair), so <c>0.54 m + 0.46 m / q</c>. The pinion has the rest of the working depth, <c>2 m - a(gear)</c>.
        /// </summary>
        public double Addendum
        {
            get
            {
                if (BevelType != BevelGearType.Gleason) return Module;
                var q = VirtualToothRatio;
                if (q >= 1.0) return Module * (0.54 + 0.46 / q);
                return Module * (2.0 - 0.54 - 0.46 * q);
            }
        }

        /// <summary>Dedendum at the large end (mm): whole depth minus addendum.</summary>
        public double Dedendum => WholeDepth - Addendum;

        /// <summary>Profile shift of the virtual spur gear in mm: the addendum less the module (zero for Standard).</summary>
        public double ProfileShiftMm => Addendum - Module;

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

        /// <summary>
        /// Dedendum angle (rad) at the cone distance: <c>atan(hf / Re)</c>. The root cone angle is the pitch cone angle less this
        /// angle.
        /// </summary>
        public double DedendumAngleRad => Math.Atan(Dedendum / ConeDistance);

        /// <summary>
        /// Addendum angle (rad). Standard: <c>atan(ha / Re)</c>. Gleason: the dedendum angle of the mating gear, whose dedendum
        /// is the clearance plus this gear's addendum, so the tip cone runs parallel to the root cone of the mate and the
        /// clearance stays constant along the face (as in the HZPT/KHK tables).
        /// </summary>
        public double AddendumAngleRad =>
            BevelType == BevelGearType.Gleason
                ? Math.Atan((Clearance + Addendum) / ConeDistance)
                : Math.Atan(Addendum / ConeDistance);

        public double DedendumAngleDeg => DedendumAngleRad * 180.0 / Math.PI;

        public double AddendumAngleDeg => AddendumAngleRad * 180.0 / Math.PI;

        /// <summary>Tip (face) cone angle in degrees: pitch cone angle plus addendum angle.</summary>
        public double TipConeAngleDeg => PitchConeAngleDeg + AddendumAngleDeg;

        /// <summary>Root cone angle in degrees: pitch cone angle minus dedendum angle.</summary>
        public double RootConeAngleDeg => PitchConeAngleDeg - DedendumAngleDeg;

        /// <summary>Outside (tip) diameter at the large end (mm): <c>d + 2 ha cos(delta)</c>.</summary>
        public double TipDiameter => PitchDiameter + 2.0 * Addendum * Math.Cos(PitchConeAngleRad);

        /// <summary>Distance along the axis from the pitch cone apex to the crown (the tip at the large end) (mm): <c>Re cos(delta) - ha sin(delta)</c>.</summary>
        public double PitchApexToCrown =>
            ConeDistance * Math.Cos(PitchConeAngleRad) - Addendum * Math.Sin(PitchConeAngleRad);

        /// <summary>Usual upper limit of the face width (mm): one third of the cone distance, but not more than ten modules.</summary>
        public double RecommendedMaxFaceWidth => Math.Min(ConeDistance / 3.0, 10.0 * Module);

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
                virtualTeeth, Module, PressureAngleDeg, ProfileShiftMm, false, null, Clearance, Backlash, 0.0);

            // Radial distances from the pitch circle shrink by cos(delta) when the back cone profile is projected onto the large-end
            // plane, so the tip and root radii are exactly d/2 + ha cos(delta) and d/2 - hf cos(delta). The angle is stretched so
            // that the tooth pitch on the pitch circle is that of the real gear.
            var pitchRadius = PitchDiameter / 2.0;
            var virtualPitchRadius = Module * virtualTeeth / 2.0;
            var radialScale = Math.Cos(PitchConeAngleRad);
            var angleScale = virtualTeeth / (double)Teeth;
            var tooth = new List<List<GearPoint>>();
            foreach (var segment in AlibreGearGenerator.BuildToothOutline(profile, false))
            {
                var mapped = new List<GearPoint>(segment.Count);
                foreach (var point in segment)
                {
                    var radius = pitchRadius + (Math.Sqrt(point.X * point.X + point.Y * point.Y) - virtualPitchRadius) * radialScale;
                    var angle = Math.Atan2(point.Y, point.X) * angleScale;
                    mapped.Add(new GearPoint(radius * Math.Cos(angle), radius * Math.Sin(angle)));
                }

                tooth.Add(mapped);
            }

            var polylines = AlibreGearGenerator.BuildFullGear(Teeth, tooth);
            var outline = SolidMeshBuilder.Flatten(polylines);

            // The tooth ends are square to the pitch cone (back cone at the large end) and every tooth edge runs to the apex,
            // so the tip, pitch and root cones meet at one point as in the usual drawings.
            var cone = PitchConeAngleRad;
            var halfAxial = FaceWidth * Math.Cos(cone) / 2.0;
            var smallEndScale = (ConeDistance - FaceWidth) / ConeDistance;
            var tanCone = Math.Cos(cone) > 0.05 ? Math.Tan(cone) : 0.0;
            var mesh = SolidMeshBuilder.LoftToApex(outline, -halfAxial, PitchDiameter / 2.0, tanCone, ApexAxialPosition, smallEndScale);

            return new GearGeometry(profile, polylines, mesh);
        }
    }
}
