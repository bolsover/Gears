using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// A mesh solver whose centre distance can be fixed by the caller instead of being derived from the profile shifts.
    /// </summary>
    public interface IAdjustableCentreSolver : IMeshSolver
    {
        /// <summary>
        /// Solves the mesh with the working centre distance fixed at <paramref name="workingCentreDistance"/> (mm).
        /// A null distance gives the same result as <see cref="IMeshSolver.Solve"/>.
        /// </summary>
        MeshResult Solve(GearComponent driver, GearComponent driven, double driverPhaseRad, double? workingCentreDistance);
    }
}
