using System;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// Immutable rigid 3D transform (rotation matrix followed by a translation).
    /// </summary>
    public sealed class Transform3D
    {
        // Row-major 3x3 rotation matrix: element (row, col) is _r[row * 3 + col].
        private readonly double[] _r;

        private Transform3D(double[] rotation, Vector3D translation)
        {
            _r = rotation;
            Translation = translation;
        }

        public static Transform3D Identity { get; } = new Transform3D(new[] { 1.0, 0, 0, 0, 1.0, 0, 0, 0, 1.0 }, Vector3D.Zero);

        public Vector3D Translation { get; }

        /// <summary>Anticlockwise rotation about the Z axis (viewed from +Z), in radians.</summary>
        public static Transform3D RotationZ(double angleRad)
        {
            var c = Math.Cos(angleRad);
            var s = Math.Sin(angleRad);
            return new Transform3D(new[] { c, -s, 0, s, c, 0, 0, 0, 1.0 }, Vector3D.Zero);
        }

        public static Transform3D Translate(Vector3D offset)
        {
            return new Transform3D(new[] { 1.0, 0, 0, 0, 1.0, 0, 0, 0, 1.0 }, offset);
        }

        /// <summary>
        /// Builds the transform that maps local coordinates onto the frame with the given origin and axes.
        /// The axes must be orthonormal.
        /// </summary>
        public static Transform3D FromFrame(Vector3D origin, Vector3D xAxis, Vector3D yAxis, Vector3D zAxis)
        {
            return new Transform3D(new[]
            {
                xAxis.X, yAxis.X, zAxis.X,
                xAxis.Y, yAxis.Y, zAxis.Y,
                xAxis.Z, yAxis.Z, zAxis.Z
            }, origin);
        }

        public Vector3D TransformPoint(Vector3D p)
        {
            var d = TransformDirection(p);
            return new Vector3D(d.X + Translation.X, d.Y + Translation.Y, d.Z + Translation.Z);
        }

        public Vector3D TransformDirection(Vector3D d)
        {
            return new Vector3D(
                _r[0] * d.X + _r[1] * d.Y + _r[2] * d.Z,
                _r[3] * d.X + _r[4] * d.Y + _r[5] * d.Z,
                _r[6] * d.X + _r[7] * d.Y + _r[8] * d.Z);
        }

        /// <summary>
        /// Returns the transform that applies <paramref name="inner"/> first and then this transform.
        /// </summary>
        public Transform3D Compose(Transform3D inner)
        {
            if (inner == null) throw new ArgumentNullException(nameof(inner));

            var r = new double[9];
            for (var row = 0; row < 3; row++)
            {
                for (var col = 0; col < 3; col++)
                {
                    r[row * 3 + col] =
                        _r[row * 3] * inner._r[col] +
                        _r[row * 3 + 1] * inner._r[3 + col] +
                        _r[row * 3 + 2] * inner._r[6 + col];
                }
            }

            return new Transform3D(r, TransformPoint(inner.Translation));
        }
    }
}
