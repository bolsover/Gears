using System;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// Immutable 3D vector used for shaft origins, axes and direction maths.
    /// </summary>
    public readonly struct Vector3D
    {
        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public static Vector3D Zero => new Vector3D(0, 0, 0);
        public static Vector3D UnitX => new Vector3D(1, 0, 0);
        public static Vector3D UnitY => new Vector3D(0, 1, 0);
        public static Vector3D UnitZ => new Vector3D(0, 0, 1);

        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

        public double Dot(Vector3D other) => X * other.X + Y * other.Y + Z * other.Z;

        public Vector3D Cross(Vector3D other)
        {
            return new Vector3D(
                Y * other.Z - Z * other.Y,
                Z * other.X - X * other.Z,
                X * other.Y - Y * other.X);
        }

        /// <summary>Returns a unit-length copy. Throws when the vector has (near) zero length.</summary>
        public Vector3D Normalized()
        {
            var length = Length;
            if (length < 1e-12)
            {
                throw new InvalidOperationException("A zero-length vector cannot be normalised.");
            }

            return new Vector3D(X / length, Y / length, Z / length);
        }

        public static Vector3D operator +(Vector3D a, Vector3D b) => new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3D operator -(Vector3D a, Vector3D b) => new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3D operator -(Vector3D a) => new Vector3D(-a.X, -a.Y, -a.Z);
        public static Vector3D operator *(Vector3D a, double s) => new Vector3D(a.X * s, a.Y * s, a.Z * s);
        public static Vector3D operator *(double s, Vector3D a) => a * s;

        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
