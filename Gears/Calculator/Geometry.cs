using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolsover.Gears.Calculator;

public static class Geometry
{
    /// <summary>
    /// Converts spherical coordinates to Cartesian coordinates.
    /// </summary>
    /// <param name="r">Sphere radius</param>
    /// <param name="gamma">Polar angle (in radians)</param>
    /// <param name="theta">Azimuth angle (in radians)</param>
    /// <returns>Tuple containing Cartesian coordinates (x, y, z)</returns>
    public static (double x, double y, double z) SphereToCartesian(double r, double gamma, double theta)
    {
        double x = r * Math.Sin(gamma) * Math.Sin(theta);
        double y = r * Math.Sin(gamma) * Math.Cos(theta);
        double z = r * Math.Cos(gamma);

        return (x, y, z);
    }
    
     /// <summary>
        /// Gets arc points plotted on a sphere's surface.
        /// </summary>
        /// <param name="sr">Sphere radius.</param>
        /// <param name="cGamma">Polar angle of the arc's center (in radians).</param>
        /// <param name="cTheta">Azimuth angle of the arc's center (in radians).</param>
        /// <param name="rDelta">Angle between OC and OP, where O is the sphere's center,
        ///                      C is the arc's center, and P is any point on the arc.</param>
        /// <param name="start">Arc's starting angle (in radians).</param>
        /// <param name="end">Arc's ending angle (in radians).</param>
        /// <param name="n">Number of points on the arc.</param>
        /// <returns>List of Cartesian coordinates (x, y, z) representing the arc points.</returns>
        public static List<(double x, double y, double z)> SArc(
            double sr, double cGamma, double cTheta,
            double rDelta, double start, double end, int n = 32)
        {
            // Generate evenly spaced points between start and end (t values)
            double[] tValues = Enumerable.Range(0, n)
                                         .Select(i => start + i * (end - start) / (n - 1))
                                         .ToArray();

            // Unit vectors in Cartesian coordinates for specific gamma and theta
            var a = SphereToCartesian(1.0D, cGamma + rDelta, cTheta);
            var k = SphereToCartesian(1.0D, cGamma, cTheta);

            // Prepare the result list
            var arcPoints = new List<(double x, double y, double z)>();

            // Compute the arc points
            foreach (double t in tValues)
            {
                // Vector addition and scaling
                var cosT = Math.Cos(t);
                var sinT = Math.Sin(t);

                // Cross product
                var crossKA = CrossProduct(k, a);

                // Dot product
                var dotKA = DotProduct(k, a);

                // Combination in terms of the arc formula
                double x = cosT * a.x + sinT * crossKA.x + dotKA * (1.0 - cosT) * k.x;
                double y = cosT * a.y + sinT * crossKA.y + dotKA * (1.0 - cosT) * k.y;
                double z = cosT * a.z + sinT * crossKA.z + dotKA * (1.0 - cosT) * k.z;

                // Scale by sphere radius
                arcPoints.Add((x * sr, y * sr, z * sr));
            }

            return arcPoints;
        }

        /// <summary>
        /// Calculates the cross product of two 3D vectors.
        /// </summary>
        private static (double x, double y, double z) CrossProduct((double x, double y, double z) v1, (double x, double y, double z) v2)
        {
            return (
                v1.y * v2.z - v1.z * v2.y,
                v1.z * v2.x - v1.x * v2.z,
                v1.x * v2.y - v1.y * v2.x
            );
        }

        /// <summary>
        /// Calculates the dot product of two 3D vectors.
        /// </summary>
        private static double DotProduct((double x, double y, double z) v1, (double x, double y, double z) v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }

        /// <summary>
        /// Spherical involute geometry function.
        /// </summary>
        /// <param name="gamma0">Base polar angle that corresponds to the pitch cone.</param>
        /// <param name="gamma">Polar angle(s) of the point(s) lying on the geometry, for which the corresponding azimuth angle(s) are to be calculated.</param>
        /// <returns>Azimuth angle(s) of the point(s) lying on the geometry.</returns>
        public static double SphericalInvolute(double gamma0, double gamma)
        {
            // Calculate phi as the arccosine of the tangent ratio
            double phi = Math.Acos(Math.Tan(gamma0) / Math.Tan(gamma));

            // Calculate and return the azimuth angle
            return Math.Acos(Math.Cos(gamma) / Math.Cos(gamma0)) / Math.Sin(gamma0) - phi;
        }
        
             /// <summary>
        /// Finds a circle in 3D space defined by three points.
        /// </summary>
        /// <param name="a">3D point A lying on the circle.</param>
        /// <param name="b">3D point B lying on the circle.</param>
        /// <param name="c">3D point C lying on the circle.</param>
        /// <returns>A tuple containing the radius and the center of the circle.</returns>
        public static (double radius, (double x, double y, double z) center) Circle3DBy3Points(
            (double x, double y, double z) a,
            (double x, double y, double z) b,
            (double x, double y, double z) c)
        {
            // Compute vectors u and w
            var u = SubtractVectors(b, a); // u = b - a
            var w = CrossProduct(SubtractVectors(c, a), u); // w = cross(c - a, u)

            // Normalize vectors u and w
            u = NormalizeVector(u);
            w = NormalizeVector(w);

            // Compute vector v as the cross product of w and u
            var v = CrossProduct(w, u);

            // Project b and c onto vectors u and v
            double bx = DotProduct(SubtractVectors(b, a), u);
            double cx = DotProduct(SubtractVectors(c, a), u);
            double cy = DotProduct(SubtractVectors(c, a), v);

            // Calculate h (height from the midpoint to the circle’s center)
            double h = ((cx - bx / 2.0) * (cx - bx / 2.0) + cy * cy - (bx / 2.0) * (bx / 2.0)) / (2.0 * cy);

            // Calculate circle center
            var cc = AddVectors(
                AddVectors(a, MultiplyVector(u, bx / 2.0)), // a + u * (bx / 2.0)
                MultiplyVector(v, h));                      // + v * h

            // Calculate circle radius
            double r = VectorMagnitude(SubtractVectors(a, cc));

            // Return radius and center
            return (r, cc);
        }

        /// <summary>
        /// Subtracts two 3D vectors.
        /// </summary>
        private static (double x, double y, double z) SubtractVectors(
            (double x, double y, double z) v1, (double x, double y, double z) v2)
        {
            return (v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }

        /// <summary>
        /// Adds two 3D vectors.
        /// </summary>
        private static (double x, double y, double z) AddVectors(
            (double x, double y, double z) v1, (double x, double y, double z) v2)
        {
            return (v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        /// <summary>
        /// Multiplies a 3D vector by a scalar.
        /// </summary>
        private static (double x, double y, double z) MultiplyVector(
            (double x, double y, double z) v, double scalar)
        {
            return (v.x * scalar, v.y * scalar, v.z * scalar);
        }

        /// <summary>
        /// Normalizes a 3D vector to a unit vector.
        /// </summary>
        private static (double x, double y, double z) NormalizeVector(
            (double x, double y, double z) v)
        {
            double magnitude = VectorMagnitude(v);
            return (v.x / magnitude, v.y / magnitude, v.z / magnitude);
        }

        /// <summary>
        /// Calculates the magnitude of a 3D vector.
        /// </summary>
        private static double VectorMagnitude((double x, double y, double z) v)
        {
            return Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        }
        /// <summary>
        /// Constructs a 3D rotation transform matrix.
        /// </summary>
        /// <param name="axis">The axis of rotation as a 3D vector (x, y, z).</param>
        /// <param name="alpha">The angle to rotate to (in radians).</param>
        /// <returns>A 3x3 rotation matrix represented as a 2D array of doubles.</returns>
        public static double[,] RotationMatrix((double x, double y, double z) axis, double alpha)
        {
            double ux = axis.x, uy = axis.y, uz = axis.z;
            double sina = Math.Sin(alpha);
            double cosa = Math.Cos(alpha);

            // Construct rotation matrix
            double[,] rMat = new double[3, 3]
            {
                {
                    cosa + (1.0 - cosa) * ux * ux,
                    ux * uy * (1.0 - cosa) - uz * sina,
                    ux * uz * (1.0 - cosa) + uy * sina
                },
                {
                    uy * ux * (1.0 - cosa) + uz * sina,
                    cosa + (1.0 - cosa) * uy * uy,
                    uy * uz * (1.0 - cosa) - ux * sina
                },
                {
                    uz * ux * (1.0 - cosa) - uy * sina,
                    uz * uy * (1.0 - cosa) + ux * sina,
                    cosa + (1.0 - cosa) * uz * uz
                }
            };

            return rMat;
        }
        
        /// <summary>
        /// Finds the angle between two vectors defined by points OA and OB.
        /// </summary>
        /// <param name="o">The origin point O of the vectors.</param>
        /// <param name="a">The endpoint A of the first vector.</param>
        /// <param name="b">The endpoint B of the second vector.</param>
        /// <returns>The angle between the two vectors in radians.</returns>
        public static double AngleBetween(
            (double x, double y, double z) o,
            (double x, double y, double z) a,
            (double x, double y, double z) b)
        {
            // Calculate vectors OA and OB
            var p = SubtractVectors(a, o);
            var q = SubtractVectors(b, o);

            // Compute the dot product and magnitudes of the vectors
            double dotProduct = DotProduct(p, q);
            double magnitudeP = VectorMagnitude(p);
            double magnitudeQ = VectorMagnitude(q);

            // Return the angle using the arccosine formula
            return Math.Acos(dotProduct / (magnitudeP * magnitudeQ));
        }
      
}