using System;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Mesh solver for an external spur pinion (driver) and a straight rack (driven).
    /// <para>
    /// The rack datum line is placed at the distance <c>m * z / 2 + x * m</c> from the pinion axis (x is the pinion's
    /// profile shift), parallel to the pinion axis, perpendicular to the line of centres and with the tooth tips pointing
    /// at the pinion. Turning the pinion by <c>phi</c> slides the rack by <c>m * z / 2 * phi</c> along its length.
    /// At phi = 0 the pinion tooth that points at the rack sits in the middle of a rack tooth space.
    /// </para>
    /// </summary>
    public sealed class RackMeshSolver : IMeshSolver
    {
        public MeshType MeshType => MeshType.Rack;

        public MeshResult Solve(GearComponent driver, GearComponent driven, double driverPhaseRad)
        {
            var pinion = Pinion(driver);
            var rack = driven as RackComponent
                       ?? throw new InvalidOperationException($"Gear '{driven?.Id}' is not a rack and cannot be driven by a pinion on a rack mesh.");

            pinion.Validate();
            rack.Validate();

            if (!GearPrecision.AreEqual(pinion.Module, rack.Module))
            {
                throw new InvalidOperationException(
                    $"'{pinion.Id}' and rack '{rack.Id}' cannot mesh: modules differ ({pinion.Module} and {rack.Module}).");
            }

            if (!GearPrecision.AreEqual(pinion.PressureAngleDeg, rack.PressureAngleDeg))
            {
                throw new InvalidOperationException(
                    $"'{pinion.Id}' and rack '{rack.Id}' cannot mesh: pressure angles differ ({pinion.PressureAngleDeg} and {rack.PressureAngleDeg}).");
            }

            var pitchRadius = PitchRadius(pinion);
            var distance = GearPrecision.Round(pitchRadius + pinion.ProfileShift * pinion.Module);
            var displacement = GearPrecision.Round(pitchRadius * driverPhaseRad);

            // Rack frame in the pinion frame: origin on the line of centres, axis parallel to the pinion axis,
            // rack length along the pinion's +Y direction and the tooth tips pointing back at the pinion (-X).
            var frame = new Placement3D(new Vector3D(distance, 0, 0), Vector3D.UnitZ, Vector3D.UnitY);
            return new MeshResult(distance, displacement, GearPrecision.Round(rack.Teeth / (double)pinion.Teeth), null, frame);
        }

        public double SpeedFactor(GearComponent driver, GearComponent driven)
        {
            var pinion = Pinion(driver);
            if (!(driven is RackComponent))
            {
                throw new NotSupportedException($"'{driven?.Id}' is not a rack.");
            }

            // Linear speed (mm/min) per pinion revolution per minute.
            return 2.0 * Math.PI * PitchRadius(pinion);
        }

        private static double PitchRadius(InvoluteGearComponent pinion) => pinion.Module * pinion.Teeth / 2.0;

        private static InvoluteGearComponent Pinion(GearComponent driver)
        {
            var pinion = driver as InvoluteGearComponent
                         ?? throw new InvalidOperationException($"Gear '{driver?.Id}' is not an involute pinion and cannot drive a rack.");
            if (pinion.IsInternal)
            {
                throw new InvalidOperationException($"Gear '{pinion.Id}' is an internal gear and cannot mesh with a rack.");
            }

            if (pinion.IsHelical)
            {
                throw new InvalidOperationException($"Gear '{pinion.Id}' is helical: helical racks are not supported, use a spur pinion.");
            }

            return pinion;
        }
    }
}
