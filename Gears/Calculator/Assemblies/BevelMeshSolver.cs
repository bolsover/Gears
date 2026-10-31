using System;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Mesh solver for a pair of straight bevel gears on intersecting shafts (Tredgold approximation).
    /// <para>
    /// The pitch cones touch along a line and share one apex. In the driver's frame the apex lies on its +Z axis; the
    /// driven shaft axis is turned by the shaft angle sigma about the driver's Y axis, so the pair lies in the driver's
    /// XZ plane with the contact line on the +X side. The driven gear is placed so that its own apex coincides with the
    /// driver's apex. The tooth phase is the same as for an external spur pair.
    /// </para>
    /// </summary>
    public sealed class BevelMeshSolver : IMeshSolver
    {
        public MeshType MeshType => MeshType.Bevel;

        public MeshResult Solve(GearComponent driver, GearComponent driven, double driverPhaseRad)
        {
            var g1 = driver as BevelGearComponent
                     ?? throw new InvalidOperationException($"Gear '{driver?.Id}' is not a bevel gear and cannot mesh on a bevel mesh.");
            var g2 = driven as BevelGearComponent
                     ?? throw new InvalidOperationException($"Gear '{driven?.Id}' is not a bevel gear and cannot mesh on a bevel mesh.");

            if (!GearPrecision.AreEqual(g1.Module, g2.Module))
            {
                throw new InvalidOperationException(
                    $"Bevel gears '{g1.Id}' and '{g2.Id}' cannot mesh: modules differ ({g1.Module} and {g2.Module}).");
            }

            if (!GearPrecision.AreEqual(g1.PressureAngleDeg, g2.PressureAngleDeg))
            {
                throw new InvalidOperationException(
                    $"Bevel gears '{g1.Id}' and '{g2.Id}' cannot mesh: pressure angles differ ({g1.PressureAngleDeg} and {g2.PressureAngleDeg}).");
            }

            // The cone angles depend on the pair, so both gears are configured from it.
            g1.MateTeeth = g2.Teeth;
            g2.MateTeeth = g1.Teeth;
            g2.ShaftAngleDeg = g1.ShaftAngleDeg;

            g1.Validate();
            g2.Validate();

            var sigma = g1.ShaftAngleDeg * Math.PI / 180.0;
            var apex1 = g1.ApexAxialPosition;
            var apex2 = g2.ApexAxialPosition;

            var axis2 = new Vector3D(-Math.Sin(sigma), 0, Math.Cos(sigma));
            var reference2 = new Vector3D(Math.Cos(sigma), 0, Math.Sin(sigma));
            var origin2 = GearPrecision.Round(new Vector3D(apex2 * Math.Sin(sigma), 0, apex1 - apex2 * Math.Cos(sigma)));
            var frame = new Placement3D(origin2, axis2, reference2);

            var basePhase = AlibreGearGenerator.CalculateMeshRotationAngle(false, false, g1.Teeth, g2.Teeth);
            var drivenPhase = basePhase - driverPhaseRad * g1.Teeth / g2.Teeth;
            var ratio = GearPrecision.Round((double)g2.Teeth / g1.Teeth);

            return new MeshResult(GearPrecision.Round(origin2.Length), drivenPhase, ratio, null, frame);
        }

        public double SpeedFactor(GearComponent driver, GearComponent driven)
        {
            var g1 = driver as BevelGearComponent;
            var g2 = driven as BevelGearComponent;
            if (g1 == null || g2 == null)
            {
                throw new NotSupportedException($"'{driver?.Id}' and '{driven?.Id}' are not both bevel gears.");
            }

            return -(double)g1.Teeth / g2.Teeth;
        }
    }
}
