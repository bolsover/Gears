using System;
using System.Collections.Generic;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Registry of the mesh solvers available for each <see cref="MeshType"/>.
    /// </summary>
    public static class MeshSolvers
    {
        private static readonly Dictionary<MeshType, IMeshSolver> Solvers = new Dictionary<MeshType, IMeshSolver>
        {
            { MeshType.Parallel, new ParallelAxisMeshSolver() },
            { MeshType.Rack, new RackMeshSolver() },
            { MeshType.Bevel, new BevelMeshSolver() },
            { MeshType.Worm, new WormMeshSolver() }
        };

        public static IMeshSolver Get(MeshType meshType)
        {
            if (Solvers.TryGetValue(meshType, out var solver)) return solver;
            throw new NotSupportedException($"No mesh solver is available for mesh type '{meshType}'.");
        }
    }
}
