using System;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// Numerical precision standard for gear assembly geometry: all computed lengths and coordinates
    /// (placements, centre distances, vertices, polyline points) are reported to 7 decimal places.
    /// Angles are kept at full double precision and only the coordinates derived from them are rounded.
    /// </summary>
    public static class GearPrecision
    {
        /// <summary>Number of decimal places that geometry results are rounded to.</summary>
        public const int DecimalPlaces = 7;

        /// <summary>Largest difference at which two geometry values are considered equal.</summary>
        public const double Tolerance = 1e-7;

        /// <summary>
        /// Rounds a value to <see cref="DecimalPlaces"/> decimal places (midpoints away from zero).
        /// Negative zero is normalised to zero. NaN and infinities are returned unchanged.
        /// </summary>
        public static double Round(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return value;
            var rounded = Math.Round(value, DecimalPlaces, MidpointRounding.AwayFromZero);
            return rounded == 0.0 ? 0.0 : rounded;
        }

        /// <summary>Rounds each component of a vector to <see cref="DecimalPlaces"/> decimal places.</summary>
        public static Vector3D Round(Vector3D value)
        {
            return new Vector3D(Round(value.X), Round(value.Y), Round(value.Z));
        }

        /// <summary>Returns true when two values differ by no more than <see cref="Tolerance"/>.</summary>
        public static bool AreEqual(double a, double b)
        {
            return Math.Abs(a - b) <= Tolerance;
        }
    }
}
