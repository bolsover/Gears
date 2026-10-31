using System;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// Immutable position and orientation of a shaft in the assembly: an origin, a rotation axis
    /// (the local Z axis, along which gears are extruded) and a reference direction (the local X axis).
    /// </summary>
    public sealed class Placement3D
    {
        /// <summary>Creates a placement. The reference direction is projected perpendicular to the axis.</summary>
        public Placement3D(Vector3D origin, Vector3D axis, Vector3D reference)
        {
            var z = axis.Normalized();
            var x = reference - z * reference.Dot(z);
            if (x.Length < 1e-9)
            {
                var candidate = Math.Abs(z.X) < 0.9 ? Vector3D.UnitX : Vector3D.UnitY;
                x = candidate - z * candidate.Dot(z);
            }

            x = x.Normalized();
            Origin = origin;
            ZAxis = z;
            XAxis = x;
            YAxis = z.Cross(x);
        }

        /// <summary>Creates a placement at <paramref name="origin"/> with the axis along world Z.</summary>
        public Placement3D(Vector3D origin) : this(origin, Vector3D.UnitZ, Vector3D.UnitX)
        {
        }

        /// <summary>Placement at the world origin with the axis along world Z.</summary>
        public static Placement3D Identity => new Placement3D(Vector3D.Zero);

        public Vector3D Origin { get; }

        /// <summary>The shaft rotation axis (local Z).</summary>
        public Vector3D ZAxis { get; }

        /// <summary>Reference direction (local X), perpendicular to the axis.</summary>
        public Vector3D XAxis { get; }

        /// <summary>Local Y, completing the right-handed frame.</summary>
        public Vector3D YAxis { get; }

        public Placement3D WithOrigin(Vector3D origin) => new Placement3D(origin, ZAxis, XAxis);

        /// <summary>Transform from the shaft's local coordinates to world coordinates.</summary>
        public Transform3D ToTransform() => Transform3D.FromFrame(Origin, XAxis, YAxis, ZAxis);
    }
}
