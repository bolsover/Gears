using System;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Mesh solver for involute spur and helical gears on parallel shafts. It reuses
    /// <see cref="GearParameterCalculator.CalculatePair"/> for the working centre distance and
    /// <see cref="AlibreGearGenerator.CalculateMeshRotationAngle(bool, bool, int, int)"/> for the tooth phase,
    /// so the result is identical to the existing gear pair preview.
    /// </summary>
    public sealed class ParallelAxisMeshSolver : IAdjustableCentreSolver
    {
        public MeshType MeshType => MeshType.Parallel;

        public MeshResult Solve(GearComponent driver, GearComponent driven, double driverPhaseRad)
        {
            return Solve(driver, driven, driverPhaseRad, null);
        }

        public MeshResult Solve(GearComponent driver, GearComponent driven, double driverPhaseRad, double? workingCentreDistance)
        {
            var g1 = driver as InvoluteGearComponent
                     ?? throw new InvalidOperationException($"Gear '{driver?.Id}' is not an involute gear and cannot mesh on parallel axes.");
            var g2 = driven as InvoluteGearComponent
                     ?? throw new InvalidOperationException($"Gear '{driven?.Id}' is not an involute gear and cannot mesh on parallel axes.");

            g1.Validate();
            g2.Validate();

            if (!GearPrecision.AreEqual(g1.Module, g2.Module))
            {
                throw new InvalidOperationException(
                    $"Gears '{g1.Id}' and '{g2.Id}' cannot mesh: modules differ ({g1.Module} and {g2.Module}).");
            }

            if (!GearPrecision.AreEqual(g1.PressureAngleDeg, g2.PressureAngleDeg))
            {
                throw new InvalidOperationException(
                    $"Gears '{g1.Id}' and '{g2.Id}' cannot mesh: pressure angles differ ({g1.PressureAngleDeg} and {g2.PressureAngleDeg}).");
            }

            var pair = GearParameterCalculator.CalculatePair(
                g1.Teeth, g1.Module, g1.PressureAngleDeg, g1.FaceWidth, g1.ProfileShift, g1.IsInternal, g1.RimThickness,
                g2.Teeth, g2.FaceWidth, g2.ProfileShift, g2.IsInternal, g2.RimThickness,
                g1.Clearance, g2.Clearance,
                g1.Backlash, g2.Backlash,
                g1.HelixAngleDeg, g2.HelixAngleDeg,
                g1.HelicalDirection, g2.HelicalDirection,
                workingCentreDistance);

            var centreDistance = GearPrecision.Round(pair.WorkingCentreDistance);

            // Base phase puts the driven gear's teeth into mesh when the driver is at phase zero.
            var basePhase = AlibreGearGenerator.CalculateMeshRotationAngle(g1.IsInternal, g2.IsInternal, g1.Teeth, g2.Teeth);

            // Turning the driver by phi turns the driven gear by phi * z1 / z2 in the opposite direction
            // for an external pair, and in the same direction when exactly one gear is internal.
            var direction = g1.IsInternal != g2.IsInternal ? 1.0 : -1.0;
            // The angle is deliberately not rounded: a 7 decimal place error in an angle moves points on a large
            // gear by far more than 1e-7 mm. Coordinates are rounded after the transform has been applied.
            var drivenPhase = basePhase + direction * driverPhaseRad * g1.Teeth / g2.Teeth;

            var ratio = GearPrecision.Round((double)g2.Teeth / g1.Teeth);
            return new MeshResult(centreDistance, drivenPhase, ratio, pair);
        }

        public double SpeedFactor(GearComponent driver, GearComponent driven)
        {
            var g1 = driver as InvoluteGearComponent;
            var g2 = driven as InvoluteGearComponent;
            if (g1 == null || g2 == null)
            {
                throw new NotSupportedException(
                    $"The speed ratio of '{driver?.Id}' and '{driven?.Id}' cannot be calculated: only involute gears mesh on parallel axes.");
            }

            var sign = g1.IsInternal != g2.IsInternal ? 1.0 : -1.0;
            return sign * g1.Teeth / g2.Teeth;
        }
    }
}
