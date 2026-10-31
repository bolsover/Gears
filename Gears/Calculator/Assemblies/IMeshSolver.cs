using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Solves the centre distance and tooth phase for one kind of gear meshing.
    /// </summary>
    public interface IMeshSolver
    {
        MeshType MeshType { get; }

        /// <summary>
        /// Solves how <paramref name="driven"/> meshes with <paramref name="driver"/>.
        /// </summary>
        /// <param name="driver">The gear that is already positioned.</param>
        /// <param name="driven">The gear to be positioned.</param>
        /// <param name="driverPhaseRad">The current rotation of the driver gear about its axis (radians).</param>
        MeshResult Solve(GearComponent driver, GearComponent driven, double driverPhaseRad);

        /// <summary>
        /// Driven speed divided by driver speed (signed). For a rack the driven speed is the linear speed in
        /// mm/min, so the factor has the unit mm per revolution.
        /// </summary>
        double SpeedFactor(GearComponent driver, GearComponent driven);
    }
}
