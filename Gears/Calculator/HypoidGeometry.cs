using System;

namespace Bolsover.Gears.Calculator
{
    /// <summary>
    /// The pitch geometry of a hypoid gear pair at the mean point: pitch radii, pitch cone angles, spiral angles and the position of the
    /// apexes. The coordinates use the gear frame: the origin is the gear apex, the gear axis is +Z (from the apex towards the heel),
    /// the common perpendicular of the two axes is +Y (from the gear axis towards the pinion axis), and X completes a right handed frame.
    /// </summary>
    public sealed class HypoidPair
    {
        public int PinionTeeth { get; internal set; }
        public int GearTeeth { get; internal set; }

        /// <summary>Mean transverse module of the gear: <c>2 r2 / z2</c> (mm).</summary>
        public double MeanModule { get; internal set; }

        /// <summary>Offset of the pinion axis from the gear axis (mm), signed as given.</summary>
        public double Offset { get; internal set; }

        public double ShaftAngleDeg { get; internal set; }

        /// <summary>Pitch radius of the pinion at the mean point (mm).</summary>
        public double PinionPitchRadius { get; internal set; }

        /// <summary>Pitch radius of the gear at the mean point (mm).</summary>
        public double GearPitchRadius { get; internal set; }

        public double PinionConeAngleRad { get; internal set; }
        public double GearConeAngleRad { get; internal set; }

        public double PinionSpiralAngleDeg { get; internal set; }
        public double GearSpiralAngleDeg { get; internal set; }

        /// <summary>Offset angle in the pitch plane: the angle between the pinion and gear cone elements through the mean point (rad).</summary>
        public double OffsetAngleRad { get; internal set; }

        /// <summary>Distance from the pinion apex to the mean point (mm).</summary>
        public double PinionMeanConeDistance { get; internal set; }

        /// <summary>Distance from the gear apex to the mean point (mm).</summary>
        public double GearMeanConeDistance { get; internal set; }

        /// <summary>Mean point in the gear frame.</summary>
        public double[] MeanPoint { get; internal set; }

        /// <summary>Pinion apex in the gear frame.</summary>
        public double[] PinionApex { get; internal set; }

        /// <summary>Unit direction of the pinion axis in the gear frame, from the pinion apex towards the heel.</summary>
        public double[] PinionAxis { get; internal set; }

        public double PinionConeAngleDeg => PinionConeAngleRad * 180.0 / Math.PI;
        public double GearConeAngleDeg => GearConeAngleRad * 180.0 / Math.PI;
        public double OffsetAngleDeg => OffsetAngleRad * 180.0 / Math.PI;
    }

    /// <summary>
    /// Pitch-cone solution of a hypoid pair (spiral teeth on shafts that do not intersect).
    /// <list type="bullet">
    /// <item>The gear pitch radius at the mean point is <c>r2 = m z2 / 2</c>.</item>
    /// <item>The offset angle follows from the offset, <c>sin(eps) = E / r2</c>, and the spiral angles differ by it: <c>beta2 = beta1 - eps</c>.</item>
    /// <item>Equal normal pitch gives the pinion radius, <c>r1 / r2 = z1 cos(beta2) / (z2 cos(beta1))</c>.</item>
    /// <item>The pitch cones touch at the mean point only. Their half angles are solved numerically so that the axes make the shaft angle
    /// and are a distance E apart.</item>
    /// </list>
    /// </summary>
    public static class HypoidGeometry
    {
        /// <summary>Solves the pair; <paramref name="module"/> is the mean transverse module of the gear.</summary>
        public static HypoidPair Solve(int z1, int z2, double module, double offset, double shaftAngleDeg, double pinionSpiralDeg)
        {
            if (z1 < 1 || z2 < 1) throw new ArgumentException("The tooth counts must be positive.");
            if (!(module > 0.0)) throw new ArgumentException("The module must be positive.", nameof(module));
            if (!(shaftAngleDeg > 0.0 && shaftAngleDeg < 180.0)) throw new ArgumentException("The shaft angle must be between 0 and 180 degrees.", nameof(shaftAngleDeg));
            if (!(pinionSpiralDeg >= 0.0 && pinionSpiralDeg < 85.0)) throw new ArgumentException("The pinion spiral angle must be from 0 to below 85 degrees.", nameof(pinionSpiralDeg));

            var sigma = shaftAngleDeg * Math.PI / 180.0;
            var r2 = module * z2 / 2.0;
            var e = Math.Abs(offset);
            if (e >= r2 * 0.9999)
            {
                throw new ArgumentException(
                    $"The offset ({offset:0.###} mm) must be smaller than the gear mean pitch radius ({r2:0.###} mm).", nameof(offset));
            }

            var eps = Math.Asin(e / r2);
            var beta1 = pinionSpiralDeg * Math.PI / 180.0;
            var beta2 = beta1 - eps;
            if (Math.Abs(beta2) >= Math.PI / 2.0 - 1e-6)
            {
                throw new ArgumentException(
                    $"The offset angle {eps * 180.0 / Math.PI:0.###} degrees is too large for a pinion spiral angle of {pinionSpiralDeg:0.###} degrees.");
            }

            var r1 = r2 * z1 * Math.Cos(beta2) / (z2 * Math.Cos(beta1));

            // Initial values: the intersecting-axes pair of the same radii.
            var d2Start = Math.Atan2(Math.Sin(sigma), r1 / r2 + Math.Cos(sigma));
            double d1, d2;
            if (e < 1e-9)
            {
                d2 = d2Start;
                d1 = sigma - d2;
                if (d1 <= 0.0) throw new ArgumentException("The tooth counts and shaft angle give no pitch cone for the pinion.");
            }
            else if (!SolveCones(r1, r2, eps, sigma, e, d2Start, out d1, out d2))
            {
                throw new ArgumentException(
                    $"No hypoid pitch cones exist for {z1}/{z2} teeth, offset {offset:0.###} mm, shaft angle {shaftAngleDeg:0.###} degrees " +
                    $"and pinion spiral angle {pinionSpiralDeg:0.###} degrees.");
            }

            BuildAxes(r1, r2, eps, d1, d2, out var gearApex, out var pinionApex, out var a1, out var a2);

            // Gear frame: origin at the gear apex, z along the gear axis, y along the common perpendicular towards the pinion axis.
            var c = Cross(a1, a2);
            var cLen = Length(c);
            var diff = Sub(pinionApex, gearApex);
            var dist = Dot(diff, c) / cLen;
            var yw = Scale(c, (dist >= 0.0 ? 1.0 : -1.0) / cLen);
            var zw = a2;
            var xw = Cross(yw, zw);

            double[] ToFrame(double[] p, bool isPoint)
            {
                var v = isPoint ? Sub(p, gearApex) : p;
                return new[] { Dot(v, xw), Dot(v, yw), Dot(v, zw) };
            }

            return new HypoidPair
            {
                PinionTeeth = z1,
                GearTeeth = z2,
                MeanModule = module,
                Offset = offset,
                ShaftAngleDeg = shaftAngleDeg,
                PinionPitchRadius = r1,
                GearPitchRadius = r2,
                PinionConeAngleRad = d1,
                GearConeAngleRad = d2,
                PinionSpiralAngleDeg = pinionSpiralDeg,
                GearSpiralAngleDeg = beta2 * 180.0 / Math.PI,
                OffsetAngleRad = eps,
                PinionMeanConeDistance = r1 / Math.Sin(d1),
                GearMeanConeDistance = r2 / Math.Sin(d2),
                MeanPoint = ToFrame(new[] { 0.0, 0.0, 0.0 }, true),
                PinionApex = ToFrame(pinionApex, true),
                PinionAxis = ToFrame(a1, false)
            };
        }

        /// <summary>
        /// Axes of the two pitch cones in a frame at the mean point: X along the gear cone element (towards the heel), Z the outward normal
        /// of the gear cone, Y completing the frame. The pinion element is turned by the offset angle about Z.
        /// </summary>
        private static void BuildAxes(double r1, double r2, double eps, double d1, double d2,
            out double[] gearApex, out double[] pinionApex, out double[] a1, out double[] a2)
        {
            var e2 = new[] { 1.0, 0.0, 0.0 };
            var n2 = new[] { 0.0, 0.0, 1.0 };
            var e1 = new[] { Math.Cos(eps), Math.Sin(eps), 0.0 };
            gearApex = Scale(e2, -r2 / Math.Sin(d2));
            pinionApex = Scale(e1, -r1 / Math.Sin(d1));
            a2 = Add(Scale(e2, Math.Cos(d2)), Scale(n2, -Math.Sin(d2)));
            a1 = Add(Scale(e1, Math.Cos(d1)), Scale(n2, Math.Sin(d1)));
        }

        /// <summary>Shortest distance between the two axes for the given cone angles (the shaft angle is satisfied by construction).</summary>
        private static double AxisDistance(double r1, double r2, double eps, double d1, double d2)
        {
            BuildAxes(r1, r2, eps, d1, d2, out var gearApex, out var pinionApex, out var a1, out var a2);
            var c = Cross(a1, a2);
            var cLen = Length(c);
            return cLen < 1e-12 ? double.NaN : Math.Abs(Dot(Sub(pinionApex, gearApex), c)) / cLen;
        }

        /// <summary>
        /// Pinion cone angle for a gear cone angle so that the axes make the shaft angle: with a1 = (cos d1 cos eps, cos d1 sin eps, sin d1) and
        /// a2 = (cos d2, 0, -sin d2) in the frame at the mean point, <c>a1 . a2 = cos d1 cos d2 cos eps - sin d1 sin d2 = cos sigma</c>.
        /// </summary>
        private static bool PinionConeAngle(double d2, double eps, double sigma, bool upperBranch, out double d1)
        {
            d1 = 0.0;
            var a = Math.Cos(d2) * Math.Cos(eps);
            var b = Math.Sin(d2);
            var rho = Math.Sqrt(a * a + b * b);
            var ratio = Math.Cos(sigma) / rho;
            if (Math.Abs(ratio) > 1.0) return false;
            d1 = (upperBranch ? 1.0 : -1.0) * Math.Acos(ratio) - Math.Atan2(b, a);
            return d1 > 1e-6 && d1 < Math.PI - 1e-6;
        }

        private static bool SolveCones(double r1, double r2, double eps, double sigma, double e, double d2Start, out double d1, out double d2)
        {
            // For each gear cone angle the shaft angle fixes the pinion cone angle; the offset is then a function of the gear cone angle alone.
            // Scan it for sign changes of (distance - E), refine by bisection and keep the root closest to the intersecting-axes pair.
            d1 = 0.0;
            d2 = 0.0;
            var found = false;
            var bestDistance = double.MaxValue;
            const int steps = 1440;
            foreach (var upper in new[] { true, false })
            {
                var havePrevious = false;
                double previousD2 = 0.0, previousF = 0.0;
                for (var i = 1; i < steps; i++)
                {
                    var t2 = Math.PI * i / steps;
                    double f;
                    if (!PinionConeAngle(t2, eps, sigma, upper, out var t1) || double.IsNaN(f = AxisDistance(r1, r2, eps, t1, t2) - e))
                    {
                        havePrevious = false;
                        continue;
                    }

                    if (havePrevious && previousF * f <= 0.0)
                    {
                        var lo = previousD2;
                        var hi = t2;
                        var fLo = previousF;
                        for (var k = 0; k < 80; k++)
                        {
                            var mid = 0.5 * (lo + hi);
                            if (!PinionConeAngle(mid, eps, sigma, upper, out var m1)) break;
                            var fMid = AxisDistance(r1, r2, eps, m1, mid) - e;
                            if (fLo * fMid <= 0.0) hi = mid;
                            else
                            {
                                lo = mid;
                                fLo = fMid;
                            }
                        }

                        var root2 = 0.5 * (lo + hi);
                        if (PinionConeAngle(root2, eps, sigma, upper, out var root1))
                        {
                            var distance = Math.Abs(root2 - d2Start) + Math.Abs(root1 - (sigma - d2Start));
                            if (distance < bestDistance)
                            {
                                bestDistance = distance;
                                d1 = root1;
                                d2 = root2;
                                found = true;
                            }
                        }
                    }

                    havePrevious = true;
                    previousD2 = t2;
                    previousF = f;
                }
            }

            return found;
        }

        private static double Dot(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
        private static double Length(double[] a) => Math.Sqrt(Dot(a, a));
        private static double[] Add(double[] a, double[] b) => new[] { a[0] + b[0], a[1] + b[1], a[2] + b[2] };
        private static double[] Sub(double[] a, double[] b) => new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
        private static double[] Scale(double[] a, double s) => new[] { a[0] * s, a[1] * s, a[2] * s };

        private static double[] Cross(double[] a, double[] b) =>
            new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
    }
}
