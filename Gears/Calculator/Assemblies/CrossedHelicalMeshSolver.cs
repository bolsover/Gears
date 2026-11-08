using System;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Mesh solver for two involute helical gears on crossed shafts. The gears may have different helix angles; they share the normal
    /// module and the normal pressure angle, and each has its own transverse module and pitch diameter. The shaft angle must equal the sum
    /// of the helix angles for the same hand, or their difference for opposite hands.
    /// <para>
    /// In the driver's frame the line of centres runs along +X. The driven shaft is at the centre distance
    /// <c>(d1 + d2) / 2</c>, with its axis turned about the common normal (the X axis). The teeth touch at a point, so the
    /// speed ratio is z1 / z2 and does not depend on the pitch diameters.
    /// </para>
    /// <para>
    /// Sign conventions, from the tooth contact condition. Let s be +1 for a right hand and -1 for a left hand, so that the helix of a
    /// gear at the contact point (on the +X side of the driver) has the direction <c>(0, s sin(beta), cos(beta))</c> in the driver frame. The
    /// helix of the driven gear is the same line when its axis is <c>(0, sin S, cos S)</c> with <c>S = s1 beta1 + s2 beta2</c>; the
    /// angle between the shafts is then |S|, which is the expected shaft angle. The surface velocities along the tooth normal agree when the
    /// two gears turn in opposite senses about their own axes at z1 / z2, so the speed factor is -z1 / z2 for every combination of hands.
    /// </para>
    /// </summary>
    public sealed class CrossedHelicalMeshSolver : ICrossedAxisSolver
    {
        public MeshType MeshType => MeshType.CrossedHelical;

        /// <summary>Solves with the shaft angle that follows from the helix angles and hands.</summary>
        public MeshResult Solve(GearComponent driver, GearComponent driven, double driverPhaseRad)
        {
            var g1 = AsGear(driver);
            var g2 = AsGear(driven);
            var expected = HelicalPairGeometry.ExpectedShaftAngleDeg(g1.HelixAngleDeg, g1.HelicalDirection, g2.HelixAngleDeg, g2.HelicalDirection);
            return Solve(driver, driven, driverPhaseRad, expected);
        }

        public MeshResult Solve(GearComponent driver, GearComponent driven, double driverPhaseRad, double shaftAngleDeg)
        {
            var g1 = AsGear(driver);
            var g2 = AsGear(driven);

            g1.Validate();
            g2.Validate();

            if (g1.IsInternal || g2.IsInternal)
            {
                throw new InvalidOperationException($"Gears '{g1.Id}' and '{g2.Id}' cannot mesh on crossed shafts: internal gears are not supported.");
            }

            if (g1.IsHerringbone || g2.IsHerringbone)
            {
                throw new InvalidOperationException($"Gears '{g1.Id}' and '{g2.Id}' cannot mesh on crossed shafts: herringbone gears are not supported.");
            }

            if (!GearPrecision.AreEqual(g1.Module, g2.Module))
            {
                throw new InvalidOperationException(
                    $"Gears '{g1.Id}' and '{g2.Id}' cannot mesh: normal modules differ ({g1.Module} and {g2.Module}).");
            }

            if (!GearPrecision.AreEqual(g1.PressureAngleDeg, g2.PressureAngleDeg))
            {
                throw new InvalidOperationException(
                    $"Gears '{g1.Id}' and '{g2.Id}' cannot mesh: pressure angles differ ({g1.PressureAngleDeg} and {g2.PressureAngleDeg}).");
            }

            HelicalPairGeometry.CheckShaftAngle(g1.Id, g2.Id, shaftAngleDeg, g1.HelixAngleDeg, g1.HelicalDirection, g2.HelixAngleDeg, g2.HelicalDirection);

            var pair = GearParameterCalculator.CalculateCrossedPair(
                g1.Teeth, g1.Module, g1.PressureAngleDeg, g1.FaceWidth, g1.ProfileShift, g1.IsInternal,
                g2.Teeth, g2.FaceWidth, g2.ProfileShift, g2.IsInternal,
                shaftAngleDeg,
                g1.Clearance, g2.Clearance,
                g1.Backlash, g2.Backlash,
                g1.HelixAngleDeg, g2.HelixAngleDeg,
                g1.HelicalDirection, g2.HelicalDirection);

            var centreDistance = GearPrecision.Round(pair.WorkingCentreDistance);

            // Driven frame in the driver frame: origin on the line of centres, axis turned about X so that both helices are the same line.
            var hand1 = HelicalPairGeometry.SameHand(g1.HelicalDirection, "R") ? 1.0 : -1.0;
            var hand2 = HelicalPairGeometry.SameHand(g2.HelicalDirection, "R") ? 1.0 : -1.0;
            var signedAngle = (hand1 * g1.HelixAngleDeg + hand2 * g2.HelixAngleDeg) * Math.PI / 180.0;
            var axis2 = new Vector3D(0, Math.Sin(signedAngle), Math.Cos(signedAngle));
            var frame = new Placement3D(new Vector3D(centreDistance, 0, 0), axis2, new Vector3D(1, 0, 0));

            var basePhase = AlibreGearGenerator.CalculateMeshRotationAngle(false, false, g1.Teeth, g2.Teeth);
            var drivenPhase = basePhase - driverPhaseRad * g1.Teeth / g2.Teeth;
            var ratio = GearPrecision.Round((double)g2.Teeth / g1.Teeth);

            return new MeshResult(centreDistance, drivenPhase, ratio, pair, frame);
        }

        public double SpeedFactor(GearComponent driver, GearComponent driven)
        {
            var g1 = driver as InvoluteGearComponent;
            var g2 = driven as InvoluteGearComponent;
            if (g1 == null || g2 == null)
            {
                throw new NotSupportedException(
                    $"The speed ratio of '{driver?.Id}' and '{driven?.Id}' cannot be calculated: only involute gears mesh on crossed helical shafts.");
            }

            // The ratio follows from the teeth, not from the pitch diameters, which are equal for the 24T/26.6 and 12T/63.4 pair.
            return -(double)g1.Teeth / g2.Teeth;
        }

        private static InvoluteGearComponent AsGear(GearComponent gear)
        {
            return gear as InvoluteGearComponent
                   ?? throw new InvalidOperationException($"Gear '{gear?.Id}' is not an involute gear and cannot mesh on crossed helical shafts.");
        }
    }
}
