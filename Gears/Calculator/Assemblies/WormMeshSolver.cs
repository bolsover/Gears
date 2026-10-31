using System;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Mesh solver for a worm (driver) and a worm wheel (driven) on crossed shafts at 90 degrees.
    /// <para>
    /// In the worm's frame the wheel axis points along +Y and the wheel centre lies on the +X axis at
    /// <c>(d_worm + d_wheel) / 2</c>. The wheel turns by <c>starts / teeth</c> of the worm's turn, clockwise or
    /// anticlockwise depending on the hand of the thread. At phase zero a thread centre crosses the contact point on the
    /// wheel's mid-plane, which meets the centre of a wheel tooth space.
    /// </para>
    /// </summary>
    public sealed class WormMeshSolver : IMeshSolver
    {
        public MeshType MeshType => MeshType.Worm;

        public MeshResult Solve(GearComponent driver, GearComponent driven, double driverPhaseRad)
        {
            var worm = driver as WormComponent
                       ?? throw new InvalidOperationException($"Gear '{driver?.Id}' is not a worm and cannot drive a worm wheel.");
            var wheel = driven as WormWheelComponent
                        ?? throw new InvalidOperationException($"Gear '{driven?.Id}' is not a worm wheel and cannot be driven by a worm.");

            if (!GearPrecision.AreEqual(worm.Module, wheel.Module))
            {
                throw new InvalidOperationException(
                    $"Worm '{worm.Id}' and wheel '{wheel.Id}' cannot mesh: modules differ ({worm.Module} and {wheel.Module}).");
            }

            if (!GearPrecision.AreEqual(worm.PressureAngleDeg, wheel.PressureAngleDeg))
            {
                throw new InvalidOperationException(
                    $"Worm '{worm.Id}' and wheel '{wheel.Id}' cannot mesh: pressure angles differ ({worm.PressureAngleDeg} and {wheel.PressureAngleDeg}).");
            }

            worm.Validate();
            wheel.LeadAngleDeg = worm.LeadAngleDeg;
            wheel.HelicalDirection = worm.HelicalDirection;
            wheel.Validate();

            var distance = GearPrecision.Round((worm.PitchDiameter + wheel.PitchDiameter) / 2.0);
            var hand = worm.IsRightHand ? 1.0 : -1.0;

            // Wheel frame in the worm frame: Z along the worm's +Y, X along the worm's +Z (so Y is the worm's +X).
            var frame = new Placement3D(new Vector3D(distance, 0, 0), Vector3D.UnitY, Vector3D.UnitZ);

            // The contact point lies at -Y of the wheel frame, i.e. at the wheel angle 3 pi / 2. The wheel then needs a tooth
            // space centred there at phase zero.
            var basePhase = 1.5 * Math.PI - Math.PI / wheel.Teeth;
            var drivenPhase = basePhase - hand * driverPhaseRad * worm.Starts / wheel.Teeth;
            var ratio = GearPrecision.Round(wheel.Teeth / (double)worm.Starts);

            return new MeshResult(distance, drivenPhase, ratio, null, frame);
        }

        public double SpeedFactor(GearComponent driver, GearComponent driven)
        {
            var worm = driver as WormComponent;
            var wheel = driven as WormWheelComponent;
            if (worm == null || wheel == null)
            {
                throw new NotSupportedException($"'{driver?.Id}' and '{driven?.Id}' are not a worm and a worm wheel.");
            }

            var hand = worm.IsRightHand ? 1.0 : -1.0;
            return -hand * worm.Starts / (double)wheel.Teeth;
        }
    }
}
