using System;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// The kind of meshing between two gears. Planetary sets are modelled by <see cref="PlanetaryGearSet"/>.
    /// </summary>
    public enum MeshType
    {
        /// <summary>Spur or helical gears on parallel shafts (external-external or external-internal).</summary>
        Parallel,

        /// <summary>An involute pinion driving a straight rack (the rack is the driven component).</summary>
        Rack,

        /// <summary>Straight bevel gears (Tredgold approximation) on intersecting shafts.</summary>
        Bevel,

        /// <summary>A worm driving a worm wheel on crossed shafts.</summary>
        Worm,

        /// <summary>A cycloidal wheel and pinion on parallel shafts, at the sum of their pitch radii.</summary>
        Cycloidal,

        /// <summary>Helical gears on crossed (non-parallel) shafts. The gears may have different helix angles; the shaft angle is stored on the relation.</summary>
        CrossedHelical,

        /// <summary>Hypoid gears (spiral teeth on shafts that do not intersect); the offset between the shafts is stored on the relation.</summary>
        Hypoid
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

        /// <summary>
        /// Working centre distance (mm) chosen by the user, or null to derive it from the gears' profile shifts. Only
        /// parallel-axis meshes support it. A value other than the derived distance changes the working pressure angle,
        /// the working backlash and the contact ratio of the pair (a larger distance adds backlash to an external pair
        /// and removes it from an internal pair).
        /// </summary>
        public double? CentreDistanceOverride { get; set; }

        /// <summary>
        /// Angle between the driver shaft and the driven shaft in degrees. Only a <see cref="Assemblies.MeshType.CrossedHelical"/> mesh uses it, and
        /// it must match the helix angles and hands of the two gears (the sum of the helix angles for the same hand, their difference for
        /// opposite hands). Zero for the other mesh types.
        /// </summary>
        public double ShaftAngleDeg { get; set; }

        /// <summary>
        /// Offset of the pinion shaft from the gear shaft in mm, along the common perpendicular of the two shafts; positive or negative for the two sides.
        /// Only a <see cref="Assemblies.MeshType.Hypoid"/> mesh uses it, and it must not be zero. Zero for the other mesh types.
        /// </summary>
        public double OffsetMm { get; set; }

        /// <summary>The solved centre distance, phase and pair data; null until the assembly has been solved.</summary>
        public MeshResult Result { get; internal set; }
    }
}
