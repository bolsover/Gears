using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolsover.Gears.Model
{
    /// <summary>
    /// A class to represent points in 3D space, extending GearPoint.
    /// </summary>
    public class GearPoint3D : GearPoint
    {
        /// <summary>
        /// Z-coordinate of the point in 3D space.
        /// </summary>
        public double Z { get; set; }

        /// <summary>
        /// Initializes a new instance of GearPoint3D.
        /// </summary>
        /// <param name="x">X-coordinate</param>
        /// <param name="y">Y-coordinate</param>
        /// <param name="z">Z-coordinate</param>
        public GearPoint3D(double x, double y, double z) : base(x, y)
        {
            Z = z;
        }

        /// <summary>
        /// Calculates the magnitude (distance from the origin) of the point in 3D space.
        /// </summary>
        public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);

        /// <summary>
        /// Offsets the point by the specified X, Y, and Z values.
        /// </summary>
        public GearPoint3D Offset(double x, double y, double z)
        {
            return new GearPoint3D(X + x, Y + y, Z + z);
        }

        /// <summary>
        /// Adds two GearPoint3D objects.
        /// </summary>
        public static GearPoint3D operator +(GearPoint3D a, GearPoint3D b)
        {
            return new GearPoint3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        /// <summary>
        /// Subtracts one GearPoint3D from another.
        /// </summary>
        public static GearPoint3D operator -(GearPoint3D a, GearPoint3D b)
        {
            return new GearPoint3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        /// <summary>
        /// Scales the point by a given magnitude.
        /// </summary>
        public GearPoint3D Scale(double magnitude)
        {
            return new GearPoint3D(X * magnitude, Y * magnitude, Z * magnitude);
        }

        /// <summary>
        /// Rotates the point about the Z-axis.
        /// </summary>
        public GearPoint3D RotateAboutZ(double angle)
        {
            var cosAngle = Math.Cos(angle);
            var sinAngle = Math.Sin(angle);
            return new GearPoint3D(
                X * cosAngle - Y * sinAngle,
                X * sinAngle + Y * cosAngle,
                Z
            );
        }

        /// <summary>
        /// Rotates the point about the X-axis.
        /// </summary>
        public GearPoint3D RotateAboutX(double angle)
        {
            var cosAngle = Math.Cos(angle);
            var sinAngle = Math.Sin(angle);
            return new GearPoint3D(
                X,
                Y * cosAngle - Z * sinAngle,
                Y * sinAngle + Z * cosAngle
            );
        }

        /// <summary>
        /// Rotates the point about the Y-axis.
        /// </summary>
        public GearPoint3D RotateAboutY(double angle)
        {
            var cosAngle = Math.Cos(angle);
            var sinAngle = Math.Sin(angle);
            return new GearPoint3D(
                X * cosAngle + Z * sinAngle,
                Y,
                -X * sinAngle + Z * cosAngle
            );
        }

        /// <summary>
        /// Converts the point to a string representation.
        /// </summary>
        public override string ToString()
        {
            return $"X: {X}, Y: {Y}, Z: {Z}";
        }
    }
}
