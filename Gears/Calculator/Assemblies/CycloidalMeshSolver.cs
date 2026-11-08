using System;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Mesh solver for a cycloidal wheel and pinion on parallel shafts. The centre distance is the sum of the two pitch radii (cycloidal
    /// gears have a fixed centre distance), and the driven gear is turned so that a tooth of one gear meets a gap of the other on the line of
    /// centres. Because cycloidal gears are designed as a pair, the solver also tells each gear the number of teeth of the other (see
    /// <see cref="CycloidalGearComponent.MateTeeth"/>), so that the tooth profiles fit.
    /// </summary>
    public sealed class CycloidalMeshSolver : IMeshSolver
    {
        public MeshType MeshType => MeshType.Cycloidal;

        public MeshResult Solve(GearComponent driver, GearComponent driven, double driverPhaseRad)
        {
            var g1 = driver as CycloidalGearComponent
                     ?? throw new InvalidOperationException($"Gear '{driver?.Id}' is not a cycloidal gear and cannot mesh with a cycloidal gear.");
            var g2 = driven as CycloidalGearComponent
                     ?? throw new InvalidOperationException($"Gear '{driven?.Id}' is not a cycloidal gear and cannot mesh with a cycloidal gear.");

            if (g1.Role == g2.Role)
            {
                throw new InvalidOperationException(
                    $"Cycloidal gears '{g1.Id}' and '{g2.Id}' cannot mesh: cycloidal gears work as a wheel and a pinion, but both are a {g1.Role.ToString().ToLowerInvariant()}.");
            }

            if (!GearPrecision.AreEqual(g1.Module, g2.Module))
            {
                throw new InvalidOperationException(
                    $"Cycloidal gears '{g1.Id}' and '{g2.Id}' cannot mesh: modules differ ({g1.Module} and {g2.Module}).");
            }

            // The profiles of the pair depend on the teeth of both gears. The driven gear is made for its driver; the driver keeps the
            // profile for the gear that it drives last.
            g1.MateTeeth = g2.Teeth;
            g2.MateTeeth = g1.Teeth;
            g1.Validate();
            g2.Validate();

            var centreDistance = GearPrecision.Round((g1.PitchDiameter + g2.PitchDiameter) / 2.0);

            // Tooth 0 of every gear is centred on its local +X axis. With the driver's tooth towards the driven gear at phase zero, the
            // driven gear has a gap there when it is turned by pi plus half a tooth pitch; the gears then turn in opposite directions.
            var basePhase = Math.PI + Math.PI / g2.Teeth;
            var drivenPhase = basePhase - driverPhaseRad * g1.Teeth / g2.Teeth;

            var ratio = GearPrecision.Round((double)g2.Teeth / g1.Teeth);
            return new MeshResult(centreDistance, drivenPhase, ratio, null);
        }

        public double SpeedFactor(GearComponent driver, GearComponent driven)
        {
            var g1 = driver as CycloidalGearComponent;
            var g2 = driven as CycloidalGearComponent;
            if (g1 == null || g2 == null)
            {
                throw new NotSupportedException(
                    $"The speed ratio of '{driver?.Id}' and '{driven?.Id}' cannot be calculated: only cycloidal gears mesh with cycloidal gears.");
            }

            return -(double)g1.Teeth / g2.Teeth;
        }
    }
}
