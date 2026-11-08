using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// A mesh solver for shafts that do not intersect and need the offset between them as one more input (a hypoid pair).
    /// </summary>
    public interface IOffsetAxisSolver : IMeshSolver
    {
        /// <summary>
        /// Solves the mesh with the pinion shaft at <paramref name="offsetMm"/> from the gear shaft, along their common perpendicular.
        /// The sign selects the side of the offset and must match the hands of the gears.
        /// </summary>
        MeshResult Solve(GearComponent driver, GearComponent driven, double driverPhaseRad, double offsetMm);
    }
}
