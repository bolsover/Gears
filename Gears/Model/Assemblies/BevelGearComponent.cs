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

        /// <summary>Circular backlash (mm). It is only reported; the solid is cut to the nominal tooth thickness.</summary>
        public double Backlash { get; set; }

        /// <summary>
        /// Corner radius of the generating rack tip as a multiple of the module, from 0 (a sharp corner and a pure trochoidal root fillet,
        /// the default) to below 1. A rounded corner gives a larger root fillet.
        /// </summary>
        public double RackTipRadiusFactor { get; set; }

        /// <summary>Corner radius of the generating rack tip (mm).</summary>
        public double RackTipRadius => RackTipRadiusFactor * Module;

        /// <summary>
        /// Bore diameter (mm). Zero means automatic: the diameter of the shaft carrying the gear (no bore when the gear is not mounted).
        /// The bore is limited to <see cref="MaxBoreDiameter"/>.
        /// </summary>
        public double BoreDiameter { get; set; }

        /// <summary>True when the gear has a hub projecting behind the back cone.</summary>
        public bool HasHub { get; set; } = true;

        /// <summary>
        /// Hub diameter (mm). Zero means automatic: 1.8 times the bore (0.4 times the root diameter without a bore), but not more than
        /// 0.85 times the root diameter.
        /// </summary>
        public double HubDiameter { get; set; }

        /// <summary>
        /// Length of the hub behind the back cone, measured along the axis (mm). Zero means automatic: 0.75 times the bore but not less
        /// than half the face width.
        /// </summary>
        public double HubLength { get; set; }

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

        /// <summary>Root diameter at the large end (mm): <c>d - 2 hf cos(delta)</c>.</summary>
        public double RootDiameter => PitchDiameter - 2.0 * Dedendum * Math.Cos(PitchConeAngleRad);

        /// <summary>Half angle of the back cone in degrees: the back cone is perpendicular to the pitch cone, so it is 90 degrees less the pitch cone angle.</summary>
        public double BackConeAngleDeg => 90.0 - PitchConeAngleDeg;

        /// <summary>Back cone distance (mm): the length of a back cone element, <c>d / (2 cos(delta))</c>; it gives the virtual tooth count z / cos(delta).</summary>
        public double BackConeDistance => PitchDiameter / 2.0 / Math.Cos(PitchConeAngleRad);

        /// <summary>Largest bore diameter (mm): 0.6 times the root diameter, which leaves a rim under the teeth.</summary>
        public double MaxBoreDiameter => 0.6 * RootDiameter;

        /// <summary>Largest hub diameter (mm): 0.85 times the root diameter, so the back cone keeps a face between the hub and the teeth.</summary>
        public double MaxHubDiameter => 0.85 * RootDiameter;

        /// <summary>Bore diameter used for the solid (mm): <see cref="BoreDiameter"/> or the shaft diameter, limited to <see cref="MaxBoreDiameter"/>.</summary>
        public double EffectiveBoreDiameter
        {
            get
            {
                var requested = BoreDiameter > 0.0 ? BoreDiameter : (CarrierShaft?.Diameter ?? 0.0);
                return Math.Min(requested, MaxBoreDiameter);
            }
        }

        /// <summary>Hub diameter used for the solid (mm); zero when the gear has no hub.</summary>
        public double EffectiveHubDiameter
        {
            get
            {
                if (!HasHub) return 0.0;
                if (HubDiameter > 0.0) return HubDiameter;
                var bore = EffectiveBoreDiameter;
                return Math.Min(bore > 0.0 ? 1.8 * bore : 0.4 * RootDiameter, MaxHubDiameter);
            }
        }

        /// <summary>Hub length used for the solid (mm); zero when the gear has no hub.</summary>
        public double EffectiveHubLength =>
            !HasHub ? 0.0 : HubLength > 0.0 ? HubLength : Math.Max(0.75 * EffectiveBoreDiameter, 0.5 * FaceWidth);

        private double LargeEndZ(double radius)
        {
            var cone = PitchConeAngleRad;
            var tanCone = Math.Cos(cone) > 0.05 ? Math.Tan(cone) : 0.0;
            return -FaceWidth * Math.Cos(cone) / 2.0 + (radius - PitchDiameter / 2.0) * tanCone;
        }

        /// <summary>Distance along the axis from the back of the hub (or back cone) to the front of the gear (mm).</summary>
        public double OverallAxialLength
        {
            get
            {
                GetBodyAxialExtent(out var start, out var end);
                return end - start;
            }
        }

        /// <summary>The body reaches from the back of the hub (or the back cone at its innermost radius) to the front cone at the tip.</summary>
        public override void GetBodyAxialExtent(out double start, out double end)
        {
            var cone = PitchConeAngleRad;
            var halfAxial = FaceWidth * Math.Cos(cone) / 2.0;
            var scale = (ConeDistance - FaceWidth) / ConeDistance;
            var bore = EffectiveBoreDiameter;
            var hub = EffectiveHubDiameter;
            var hasHub = hub > bore && EffectiveHubLength > 0.0;
            var inner = (hasHub ? hub : bore) / 2.0;

            start = -halfAxial;
            if (inner > 0.0)
            {
                var zInner = LargeEndZ(inner);
                start = Math.Min(start, hasHub ? zInner - EffectiveHubLength : zInner);
            }

            var tipRadius = TipDiameter / 2.0;
            end = Math.Max(halfAxial, ApexAxialPosition + (LargeEndZ(tipRadius) - ApexAxialPosition) * scale);

            // The solid made from the blank profile has a flat back face at pointF and ends at the toe corner pointG.
            try
            {
                var blank = CreateBlankProfile();
                var offset = BlankAxialOffset;
                var back = -blank.PointF.X + offset;
                var hubLength = EffectiveBoreDiameter < EffectiveHubDiameter ? EffectiveHubLength : 0.0;
                start = back - hubLength;
                end = -Math.Min(blank.PointG.X, blank.PointH.X) + offset;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                // An invalid combination is reported by Validate and BuildGeometry; the extent stays that of the simple cone body.
            }
        }

        /// <summary>Shift along the axis from the blank frame (X along the axis, apex towards negative X) to the local frame (Z axis, apex on +Z): z = offset - x.</summary>
        private double BlankAxialOffset => -FaceWidth * Math.Cos(PitchConeAngleRad) / 2.0;

        /// <summary>The closed profile sketch of the gear blank, with the proportions of this gear and its mate.</summary>
        public BevelBlankProfile CreateBlankProfile()
        {
            return new BevelBlankProfile(Teeth, EffectiveMateTeeth, Module, FaceWidth, ShaftAngleDeg, bevelType: BevelType);
        }

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

            if (!(RackTipRadiusFactor >= 0.0 && RackTipRadiusFactor < 1.0))
            {
                throw new InvalidOperationException($"Bevel gear '{Id}': the rack tip radius must be from 0 to below 1 module.");
            }

            if (FaceWidth >= ConeDistance)
            {
                throw new InvalidOperationException(
                    $"Bevel gear '{Id}': the face width ({FaceWidth}) must be smaller than the cone distance ({GearPrecision.Round(ConeDistance)}). " +
                    "A face width of at most one third of the cone distance is usual.");
            }

            if (BoreDiameter < 0 || HubDiameter < 0 || HubLength < 0)
            {
                throw new InvalidOperationException($"Bevel gear '{Id}': the bore diameter, hub diameter and hub length cannot be negative.");
            }

            if (BoreDiameter > MaxBoreDiameter + 1e-9)
            {
                throw new InvalidOperationException(
                    $"Bevel gear '{Id}': the bore diameter ({BoreDiameter}) must not exceed {GearPrecision.Round(MaxBoreDiameter)} mm (0.6 times the root diameter).");
            }

            if (HasHub && HubDiameter > 0.0)
            {
                if (HubDiameter <= EffectiveBoreDiameter)
                {
                    throw new InvalidOperationException(
                        $"Bevel gear '{Id}': the hub diameter ({HubDiameter}) must be larger than the bore diameter ({GearPrecision.Round(EffectiveBoreDiameter)}).");
                }

                if (HubDiameter > MaxHubDiameter + 1e-9)
                {
                    throw new InvalidOperationException(
                        $"Bevel gear '{Id}': the hub diameter ({HubDiameter}) must not exceed {GearPrecision.Round(MaxHubDiameter)} mm (0.85 times the root diameter).");
                }
            }
        }

        public override GearGeometry BuildGeometry()
        {
            Validate();

            var profile = AlibreGearGenerator.GenerateExternalToothProfile(
                VirtualTeeth, Module, PressureAngleDeg, ProfileShiftMm, false, null, Clearance, Backlash, 0.0);

            // The solid is the blank revolved about its axis with the Tredgold tooth valleys cut and patterned (see BevelBlankProfile and
            // BevelToothSketch). The blank frame has its axis along X and the cone apex towards negative X.
            var blank = CreateBlankProfile();
            var sketch = new BevelToothSketch(blank, PressureAngleDeg, rackTipRadiusFactor: RackTipRadiusFactor);

            // The bore and the hub are limited so that they stay inside the root of the teeth.
            var bore = Math.Min(EffectiveBoreDiameter / 2.0, 0.9 * blank.PointH.Y);
            var hubRadius = EffectiveHubDiameter / 2.0;
            var hubLength = EffectiveHubLength;
            if (hubRadius > 0.95 * blank.PointD.Y) hubRadius = 0.95 * blank.PointD.Y;
            var hasHub = hubRadius > bore && hubLength > 0.0;
            var blankMesh = blank.BuildToothedMesh(sketch, 0, bore, hasHub ? hubRadius : 0.0, hasHub ? hubLength : 0.0);

            // Into the local frame: the axis is Z with the apex on +Z, a proper rotation (x, y, z) -> (y, -z, -x) plus the axial shift. The
            // turn by half a pitch puts a tooth, not a valley, on the +X axis like the other gears, which the mesh phase assumes.
            var offset = BlankAxialOffset;
            var phase = Math.PI / Teeth;
            var cosPhase = Math.Cos(phase);
            var sinPhase = Math.Sin(phase);
            var mesh = new GearMesh3DBuilder.Mesh3D();
            foreach (var v in blankMesh.Vertices)
            {
                var x = v.Y;
                var y = -v.Z;
                mesh.Vertices.Add(new GearPoint3D(x * cosPhase - y * sinPhase, x * sinPhase + y * cosPhase, offset - v.X));
            }

            mesh.TriangleIndices.AddRange(blankMesh.TriangleIndices);
            mesh.CalculateNormals();

            // The 2D outline is the sketch outline projected onto the plane of the large end, with the same flip and turn.
            var cosDelta = Math.Cos(PitchConeAngleRad);
            var outline = new List<GearPoint>();
            foreach (var point in sketch.BuildOutline(Teeth))
            {
                var radius = blank.BackConePoint(point.Radius).Y;
                var angle = -point.Angle / cosDelta + phase;
                outline.Add(new GearPoint(radius * Math.Cos(angle), radius * Math.Sin(angle)));
            }

            if (outline.Count > 0) outline.Add(outline[0]);
            var polylines = new List<List<GearPoint>> { outline };

            // The hub and bore show in the 2D view as circles inside the tooth outline.
            if (hasHub) polylines.Add(Circle(hubRadius));
            if (bore > 0.0) polylines.Add(Circle(bore));

            return new GearGeometry(profile, polylines, mesh);
        }

        private static List<GearPoint> Circle(double radius)
        {
            const int segments = 72;
            var circle = new List<GearPoint>(segments + 1);
            for (var i = 0; i <= segments; i++)
            {
                var angle = 2.0 * Math.PI * (i % segments) / segments;
                circle.Add(new GearPoint(radius * Math.Cos(angle), radius * Math.Sin(angle)));
            }

            return circle;
        }
    }
}
