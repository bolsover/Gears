using System;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Mesh solver for a hypoid pair: spiral teeth on shafts that do not intersect (see <see cref="HypoidGeometry"/>).
    /// <para>
    /// One gear of the pair is the pinion (<see cref="HypoidGearComponent.IsPinion"/>), the other the gear. The pitch cones touch at the
    /// mean point only and their apexes do not coincide. The solver places both gears so that their mean points coincide: the mean point
    /// lies on the +X axis of each gear at its face mid-plane, as the tooth phase logic of the other bevel meshes assumes, and the pinion
    /// shaft is offset from the gear shaft by the relation's offset along the common perpendicular.
    /// </para>
    /// <para>
    /// Hands. The tooth line through the mean point is the same line for both gears. For a positive offset (the side of the pair solution)
    /// the gear is right hand and the pinion left hand; the mirror image, a negative offset, swaps the hands. The tooth phase is that of
    /// the other bevel meshes: the driver has a tooth on its +X axis at the mean point and the driven gear has a gap there; the speed
    /// factor is -z1 / z2.
    /// </para>
    /// </summary>
    public sealed class HypoidMeshSolver : IOffsetAxisSolver
    {
        public MeshType MeshType => MeshType.Hypoid;

        /// <summary>Solves with the offset stored on the driver gear.</summary>
        public MeshResult Solve(GearComponent driver, GearComponent driven, double driverPhaseRad)
        {
            return Solve(driver, driven, driverPhaseRad, AsGear(driver).OffsetMm);
        }

        public MeshResult Solve(GearComponent driver, GearComponent driven, double driverPhaseRad, double offsetMm)
        {
            var g1 = AsGear(driver);
            var g2 = AsGear(driven);

            if (g1.IsPinion == g2.IsPinion)
            {
                throw new InvalidOperationException(
                    $"Hypoid gears '{g1.Id}' and '{g2.Id}' cannot mesh: one of them must be the pinion and the other the gear.");
            }

            var pinion = g1.IsPinion ? g1 : g2;
            var gear = g1.IsPinion ? g2 : g1;

            if (!(Math.Abs(offsetMm) > 1e-9))
            {
                throw new InvalidOperationException(
                    $"Hypoid gears '{g1.Id}' and '{g2.Id}' cannot mesh: the offset must not be zero. Use a spiral bevel mesh for intersecting shafts.");
            }

            if (!GearPrecision.AreEqual(g1.Module, g2.Module))
            {
                throw new InvalidOperationException(
                    $"Hypoid gears '{g1.Id}' and '{g2.Id}' cannot mesh: modules differ ({g1.Module} and {g2.Module}); the mean module of the gear is the module of both.");
            }

            if (!GearPrecision.AreEqual(g1.PressureAngleDeg, g2.PressureAngleDeg))
            {
                throw new InvalidOperationException(
                    $"Hypoid gears '{g1.Id}' and '{g2.Id}' cannot mesh: pressure angles differ ({g1.PressureAngleDeg} and {g2.PressureAngleDeg}).");
            }

            if (!GearPrecision.AreEqual(g1.CutterRadius, g2.CutterRadius))
            {
                throw new InvalidOperationException(
                    $"Hypoid gears '{g1.Id}' and '{g2.Id}' cannot mesh: cutter radii differ ({g1.CutterRadius} and {g2.CutterRadius}); expected {g1.CutterRadius}.");
            }

            // The pair is described by one set of values, so both gears are configured from the driver and the pinion.
            g1.MateTeeth = g2.Teeth;
            g2.MateTeeth = g1.Teeth;
            g2.ShaftAngleDeg = g1.ShaftAngleDeg;
            g2.BevelType = g1.BevelType;
            gear.MeanSpiralAngleDeg = pinion.MeanSpiralAngleDeg;
            g1.OffsetMm = offsetMm;
            g2.OffsetMm = offsetMm;

            var gearHand = offsetMm > 0.0 ? SpiralHand.Right : SpiralHand.Left;
            var pinionHand = offsetMm > 0.0 ? SpiralHand.Left : SpiralHand.Right;
            if (gear.Hand != gearHand || pinion.Hand != pinionHand)
            {
                throw new InvalidOperationException(
                    $"Hypoid gears '{g1.Id}' and '{g2.Id}' cannot mesh: for an offset of {offsetMm:0.###} mm the gear '{gear.Id}' must be {gearHand} hand " +
                    $"and the pinion '{pinion.Id}' {pinionHand} hand, not {gear.Hand} and {pinion.Hand}. The hand follows from the side of the offset.");
            }

            g1.Validate();
            g2.Validate();

            var pair = pinion.Pair ?? throw new InvalidOperationException($"Hypoid gears '{g1.Id}' and '{g2.Id}' cannot mesh: {pinion.PairError}");

            // The pair in the gear frame (origin at the gear apex, gear axis +Z, pinion axis offset along +Y); the other side is the mirror image.
            var side = offsetMm > 0.0 ? 1.0 : -1.0;
            var meanPoint = new Vector3D(pair.MeanPoint[0], side * pair.MeanPoint[1], pair.MeanPoint[2]);
            var pinionApex = new Vector3D(pair.PinionApex[0], side * pair.PinionApex[1], pair.PinionApex[2]);
            var pinionAxis = new Vector3D(pair.PinionAxis[0], side * pair.PinionAxis[1], pair.PinionAxis[2]);
            var gearApex = Vector3D.Zero;
            var gearAxis = Vector3D.UnitZ;

            var gearFrame = FrameOf(gearApex, gearAxis, meanPoint);
            var pinionFrame = FrameOf(pinionApex, pinionAxis, meanPoint);
            var driverFrame = ReferenceEquals(driver, gear) ? gearFrame : pinionFrame;
            var drivenFrame = ReferenceEquals(driver, gear) ? pinionFrame : gearFrame;
            var frame = Relative(driverFrame, drivenFrame);

            // Both frames have their X axis towards the mean point, whereas the bevel phase assumes the driven contact on its -X side.
            var basePhase = AlibreGearGenerator.CalculateMeshRotationAngle(false, false, g1.Teeth, g2.Teeth);
            var drivenPhase = basePhase - driverPhaseRad * g1.Teeth / g2.Teeth - Math.PI;
            var ratio = GearPrecision.Round((double)g2.Teeth / g1.Teeth);
            var distance = GearPrecision.Round(frame.Origin.Length);

            return new MeshResult(distance, drivenPhase, ratio, null, frame);
        }

        public double SpeedFactor(GearComponent driver, GearComponent driven)
        {
            var g1 = driver as HypoidGearComponent;
            var g2 = driven as HypoidGearComponent;
            if (g1 == null || g2 == null)
            {
                throw new NotSupportedException($"'{driver?.Id}' and '{driven?.Id}' are not both hypoid gears.");
            }

            return -(double)g1.Teeth / g2.Teeth;
        }

        /// <summary>
        /// The local frame of a gear: the origin on the axis at the axial position of the mean point (the face mid-plane), the Z axis from the
        /// heel towards the apex, and the X axis towards the mean point.
        /// </summary>
        private static Placement3D FrameOf(Vector3D apex, Vector3D axisFromApexToHeel, Vector3D meanPoint)
        {
            var along = (meanPoint - apex).Dot(axisFromApexToHeel);
            var origin = apex + axisFromApexToHeel * along;
            var radial = (meanPoint - origin).Normalized();
            return new Placement3D(origin, axisFromApexToHeel * -1.0, radial);
        }

        /// <summary>The frame of the driven gear expressed in the frame of the driver.</summary>
        private static Placement3D Relative(Placement3D driver, Placement3D driven)
        {
            Vector3D In(Vector3D v) => new Vector3D(v.Dot(driver.XAxis), v.Dot(driver.YAxis), v.Dot(driver.ZAxis));
            var origin = GearPrecision.Round(In(driven.Origin - driver.Origin));
            return new Placement3D(origin, In(driven.ZAxis), In(driven.XAxis));
        }

        private static HypoidGearComponent AsGear(GearComponent gear)
        {
            return gear as HypoidGearComponent
                   ?? throw new InvalidOperationException($"Gear '{gear?.Id}' is not a hypoid gear and cannot mesh on a hypoid mesh.");
        }
    }
}
