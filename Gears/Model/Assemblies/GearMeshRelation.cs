using System;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// The kind of meshing between two gears. Further kinds (rack, bevel, worm, planetary) are added in later phases.
    /// </summary>
    public enum MeshType
    {
        /// <summary>Spur or helical gears on parallel shafts (external-external or external-internal).</summary>
        Parallel
    }

    /// <summary>
    /// Describes that one gear (the driver) meshes with another (the driven gear).
    /// The solved result is populated by <see cref="GearAssembly.Solve"/>.
    /// </summary>
    public sealed class GearMeshRelation
    {
        public GearMeshRelation(GearComponent driver, GearComponent driven, MeshType meshType)
        {
            Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            Driven = driven ?? throw new ArgumentNullException(nameof(driven));
            MeshType = meshType;
        }

        public GearComponent Driver { get; }
        public GearComponent Driven { get; }
        public MeshType MeshType { get; }

        /// <summary>
        /// Direction of the line of centres from the driver shaft to the driven shaft, in degrees, measured anticlockwise
        /// from the driver shaft's local X axis within its XY plane. Zero keeps the driven shaft on the X axis.
        /// Use it to branch a gear train in different directions.
        /// </summary>
        public double CentreLineAngleDeg { get; set; }

        /// <summary>The solved centre distance, phase and pair data; null until the assembly has been solved.</summary>
        public MeshResult Result { get; internal set; }
    }
}
