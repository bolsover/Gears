using System;
using System.Collections.Generic;
using Bolsover.Gears.Calculator;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// Geometry of a single gear component in its own local coordinate system
    /// (gear axis along Z, centre at the origin, face width centred on the Z = 0 mid-plane).
    /// </summary>
    public sealed class GearGeometry
    {
        public GearGeometry(
            AlibreGearGenerator.AlibreGearProfile profile,
            List<List<GearPoint>> polylines,
            GearMesh3DBuilder.Mesh3D mesh)
        {
            Profile = profile;
            Polylines = polylines ?? new List<List<GearPoint>>();
            Mesh = mesh ?? new GearMesh3DBuilder.Mesh3D();
        }

        /// <summary>The generated tooth profile segments and parameters.</summary>
        public AlibreGearGenerator.AlibreGearProfile Profile { get; }

        /// <summary>The 2D outline polylines in the gear plane.</summary>
        public List<List<GearPoint>> Polylines { get; }

        /// <summary>The 3D solid mesh.</summary>
        public GearMesh3DBuilder.Mesh3D Mesh { get; }
    }

    /// <summary>
    /// Base class for any gear-like part that can be mounted on a shaft.
    /// Future gear families (rack, bevel, worm, planetary stages) derive from this class.
    /// </summary>
    public abstract class GearComponent
    {
        protected GearComponent(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A gear component requires a non-empty identifier.", nameof(id));
            }

            Id = id;
            Name = id;
        }

        /// <summary>Unique identifier of the component within an assembly.</summary>
        public string Id { get; }

        /// <summary>Display name.</summary>
        public string Name { get; set; }

        /// <summary>Normal module (mm).</summary>
        public double Module { get; set; } = 2.0;

        /// <summary>Normal pressure angle (degrees).</summary>
        public double PressureAngleDeg { get; set; } = 20.0;

        /// <summary>Face width (mm), measured along the gear axis.</summary>
        public double FaceWidth { get; set; } = 15.0;

        /// <summary>Number of teeth (used for gear ratios).</summary>
        public abstract int ToothCount { get; }

        /// <summary>Pitch diameter (mm), used for pitch line velocity.</summary>
        public abstract double PitchDiameter { get; }

        /// <summary>Builds the component geometry in local coordinates.</summary>
        public abstract GearGeometry BuildGeometry();
    }
}
