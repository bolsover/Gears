using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// A mesh solver for shafts that are not parallel and need the angle between them as one more input.
    /// </summary>
    public interface ICrossedAxisSolver : IMeshSolver
    {
        /// <summary>
        /// Solves the mesh with the driven shaft at <paramref name="shaftAngleDeg"/> degrees to the driver shaft. The angle must match
        /// the helix angles and hands of the gears.
        /// </summary>
        MeshResult Solve(GearComponent driver, GearComponent driven, double driverPhaseRad, double shaftAngleDeg);
    }
}
