using System;
using Bolsover.Gears.Calculator;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>Pitch-cone solution of a hypoid pair at the mean point.</summary>
    [TestFixture]
    public class TestHypoidGeometry
    {
        private static double Rad(double deg) => deg * Math.PI / 180.0;
        private static double Dot(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
        private static double Len(double[] a) => Math.Sqrt(Dot(a, a));
        private static double[] Sub(double[] a, double[] b) => new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
        private static double[] Mul(double[] a, double s) => new[] { a[0] * s, a[1] * s, a[2] * s };
        private static double[] Unit(double[] a) => Mul(a, 1.0 / Len(a));
        private static double[] Cross(double[] a, double[] b) =>
            new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };

        [Test]
        public void TestZeroOffset_ReducesToAnIntersectingSpiralPair()
        {
            var pair = HypoidGeometry.Solve(18, 36, 4.0, 0.0, 90.0, 35.0);

            Assert.AreEqual(Math.Atan2(36.0, 18.0), pair.GearConeAngleRad, 1e-9);
            Assert.AreEqual(Math.Atan2(18.0, 36.0), pair.PinionConeAngleRad, 1e-9);
            Assert.AreEqual(0.5, pair.PinionPitchRadius / pair.GearPitchRadius, 1e-12);
            Assert.AreEqual(35.0, pair.GearSpiralAngleDeg, 1e-12, "Equal spiral angles without an offset.");
            Assert.AreEqual(0.0, pair.OffsetAngleRad, 1e-12);
            Assert.AreEqual(pair.GearMeanConeDistance, pair.PinionMeanConeDistance, 1e-9, "The apexes coincide.");
            Assert.AreEqual(0.0, Len(pair.PinionApex), 1e-9);
        }

        [TestCase(10, 41, 5.0, 25.0, 90.0, 50.0)]
        [TestCase(12, 37, 4.0, 20.0, 90.0, 45.0)]
        [TestCase(8, 30, 3.0, 12.0, 90.0, 48.0)]
        [TestCase(14, 28, 4.0, 15.0, 80.0, 40.0)]
        public void TestPinionRadiusAndSpiralAngles_FollowTheEqualNormalPitchRelations(int z1, int z2, double module, double offset, double shaft, double beta1)
        {
            var pair = HypoidGeometry.Solve(z1, z2, module, offset, shaft, beta1);

            var b1 = Rad(pair.PinionSpiralAngleDeg);
            var b2 = Rad(pair.GearSpiralAngleDeg);
            Assert.AreEqual(module * z2 / 2.0, pair.GearPitchRadius, 1e-12);
            Assert.AreEqual(offset / pair.GearPitchRadius, Math.Sin(pair.OffsetAngleRad), 1e-12, "sin(eps) = E / r2.");
            Assert.AreEqual(pair.OffsetAngleRad, b1 - b2, 1e-12, "beta1 - beta2 = eps.");
            Assert.AreEqual(z1 * Math.Cos(b2) / (z2 * Math.Cos(b1)), pair.PinionPitchRadius / pair.GearPitchRadius, 1e-12);
            Assert.Greater(pair.PinionPitchRadius / pair.GearPitchRadius, (double)z1 / z2, "The pinion is larger than its tooth count suggests.");
            Assert.Greater(pair.PinionSpiralAngleDeg, pair.GearSpiralAngleDeg);
        }

        [TestCase(10, 41, 5.0, 25.0, 90.0, 50.0)]
        [TestCase(12, 37, 4.0, 20.0, 90.0, 45.0)]
        [TestCase(14, 28, 4.0, 15.0, 80.0, 40.0)]
        public void TestAxes_HaveTheShaftAngleAndTheOffset(int z1, int z2, double module, double offset, double shaft, double beta1)
        {
            var pair = HypoidGeometry.Solve(z1, z2, module, offset, shaft, beta1);

            var gearAxis = new[] { 0.0, 0.0, 1.0 };
            Assert.AreEqual(Math.Cos(Rad(shaft)), Dot(pair.PinionAxis, gearAxis), 1e-9, "Shaft angle.");

            var c = Cross(pair.PinionAxis, gearAxis);
            var distance = Math.Abs(Dot(pair.PinionApex, c)) / Len(c);
            Assert.AreEqual(offset, distance, 1e-7, "Offset between the axes.");
            Assert.AreEqual(offset, pair.PinionApex[1], 1e-7, "The pinion axis lies in the plane y = E.");
            Assert.AreEqual(0.0, pair.PinionAxis[1], 1e-9);
            Assert.Greater(Math.Abs(pair.PinionApex[2] - pair.MeanPoint[2]), 0.0);
        }

        [TestCase(10, 41, 5.0, 25.0, 90.0, 50.0)]
        [TestCase(12, 37, 4.0, 20.0, 90.0, 45.0)]
        [TestCase(14, 28, 4.0, 15.0, 80.0, 40.0)]
        public void TestPitchCones_AreTangentAtTheMeanPoint(int z1, int z2, double module, double offset, double shaft, double beta1)
        {
            var pair = HypoidGeometry.Solve(z1, z2, module, offset, shaft, beta1);
            var p = pair.MeanPoint;

            // Gear cone: apex at the origin, axis +Z.
            var radial2 = new[] { p[0], p[1], 0.0 };
            Assert.AreEqual(pair.GearPitchRadius, Len(radial2), 1e-7, "Mean point on the gear pitch radius.");
            var u2 = Unit(radial2);
            var n2 = new[] { Math.Cos(pair.GearConeAngleRad) * u2[0], Math.Cos(pair.GearConeAngleRad) * u2[1], -Math.Sin(pair.GearConeAngleRad) };
            Assert.AreEqual(pair.GearConeAngleRad, Math.Atan2(Len(radial2), p[2]), 1e-7, "The mean point lies on the gear cone.");

            // Pinion cone: apex at PinionApex, axis PinionAxis.
            var fromApex = Sub(p, pair.PinionApex);
            var along = Dot(fromApex, pair.PinionAxis);
            var radial1 = Sub(fromApex, Mul(pair.PinionAxis, along));
            Assert.AreEqual(pair.PinionPitchRadius, Len(radial1), 1e-7, "Mean point on the pinion pitch radius.");
            Assert.AreEqual(pair.PinionConeAngleRad, Math.Atan2(Len(radial1), along), 1e-7, "The mean point lies on the pinion cone.");
            var u1 = Unit(radial1);
            var n1 = new[]
            {
                Math.Cos(pair.PinionConeAngleRad) * u1[0] - Math.Sin(pair.PinionConeAngleRad) * pair.PinionAxis[0],
                Math.Cos(pair.PinionConeAngleRad) * u1[1] - Math.Sin(pair.PinionConeAngleRad) * pair.PinionAxis[1],
                Math.Cos(pair.PinionConeAngleRad) * u1[2] - Math.Sin(pair.PinionConeAngleRad) * pair.PinionAxis[2]
            };

            for (var i = 0; i < 3; i++) Assert.AreEqual(-n2[i], n1[i], 1e-7, $"The outward normals are opposite (component {i}).");

            // The cone elements through the mean point make the offset angle.
            var e2 = Unit(p);
            var e1 = Unit(fromApex);
            Assert.AreEqual(pair.OffsetAngleRad, Math.Acos(Math.Min(1.0, Dot(e1, e2))), 1e-7);
        }

        [TestCase(10, 41, 5.0, 25.0, 90.0, 50.0)]
        [TestCase(12, 37, 4.0, 20.0, 90.0, 45.0)]
        public void TestSurfaceSpeeds_AgreeAlongTheToothTangent(int z1, int z2, double module, double offset, double shaft, double beta1)
        {
            var pair = HypoidGeometry.Solve(z1, z2, module, offset, shaft, beta1);
            var p = pair.MeanPoint;
            var gearAxis = new[] { 0.0, 0.0, 1.0 };

            var v2 = Cross(gearAxis, p);
            var v1 = Cross(pair.PinionAxis, Sub(p, pair.PinionApex));
            // Angular speeds in the ratio z2 / z1, turning in the sense that gives equal directions at the mean point.
            var speedRatio = (double)z2 / z1;
            v1 = Mul(v1, speedRatio * (Dot(v1, v2) >= 0.0 ? 1.0 : -1.0));

            var e2 = Unit(p);
            var radial2 = Unit(new[] { p[0], p[1], 0.0 });
            var n2 = new[] { Math.Cos(pair.GearConeAngleRad) * radial2[0], Math.Cos(pair.GearConeAngleRad) * radial2[1], -Math.Sin(pair.GearConeAngleRad) };
            Assert.AreEqual(0.0, Dot(v1, n2), 1e-7, "The pinion surface moves in the pitch plane.");
            Assert.AreEqual(0.0, Dot(v2, n2), 1e-7, "The gear surface moves in the pitch plane.");

            // The tooth tangent leans away from the gear element by beta2; the sliding velocity lies along it.
            var y = Cross(n2, e2);
            var b2 = Rad(pair.GearSpiralAngleDeg);
            var tangent = new[]
            {
                Math.Cos(b2) * e2[0] - Math.Sin(b2) * y[0],
                Math.Cos(b2) * e2[1] - Math.Sin(b2) * y[1],
                Math.Cos(b2) * e2[2] - Math.Sin(b2) * y[2]
            };
            var across = Cross(n2, tangent);
            var slide = Sub(v1, v2);
            Assert.AreEqual(0.0, Dot(slide, across) / Len(v2), 1e-6, "The relative velocity has no component across the tooth.");
        }

        [Test]
        public void TestNegativeOffset_GivesTheSameSizes()
        {
            var positive = HypoidGeometry.Solve(12, 37, 4.0, 20.0, 90.0, 45.0);
            var negative = HypoidGeometry.Solve(12, 37, 4.0, -20.0, 90.0, 45.0);

            Assert.AreEqual(positive.PinionConeAngleRad, negative.PinionConeAngleRad, 1e-9);
            Assert.AreEqual(positive.GearConeAngleRad, negative.GearConeAngleRad, 1e-9);
            Assert.AreEqual(-20.0, negative.Offset);
        }

        [Test]
        public void TestOffsetLargerThanTheGearRadius_IsRejected()
        {
            var ex = Assert.Throws<ArgumentException>(() => HypoidGeometry.Solve(12, 37, 4.0, 80.0, 90.0, 45.0));
            StringAssert.Contains("smaller than the gear mean pitch radius", ex.Message);
        }

        [Test]
        public void TestInvalidInputs_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => HypoidGeometry.Solve(0, 37, 4.0, 10.0, 90.0, 45.0));
            Assert.Throws<ArgumentException>(() => HypoidGeometry.Solve(12, 37, 0.0, 10.0, 90.0, 45.0));
            Assert.Throws<ArgumentException>(() => HypoidGeometry.Solve(12, 37, 4.0, 10.0, 0.0, 45.0));
            Assert.Throws<ArgumentException>(() => HypoidGeometry.Solve(12, 37, 4.0, 10.0, 90.0, 90.0));
        }
    }
}
