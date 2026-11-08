using System;
using Bolsover.Gears.Calculator;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>The circular-arc tooth trace of spiral and zerol bevel gears.</summary>
    [TestFixture]
    public class TestSpiralToothTrace
    {
        private const double Rm = 60.0;
        private const double Rc = 63.5;
        private static readonly double Delta = 26.565 * Math.PI / 180.0;

        private static double Rad(double deg) => deg * Math.PI / 180.0;

        [Test]
        public void TestSpiralAngleAtTheMeanConeDistance_IsTheMeanSpiralAngle()
        {
            var trace = new SpiralToothTrace(Rm, Rad(35.0), Rc, true);

            Assert.AreEqual(Rad(35.0), trace.SpiralAngleAt(Rm), 1e-12);
        }

        [Test]
        public void TestTwistAtTheMeanConeDistance_IsZero()
        {
            foreach (var hand in new[] { true, false })
            {
                var trace = new SpiralToothTrace(Rm, Rad(35.0), Rc, hand);
                Assert.AreEqual(0.0, trace.TwistAt(Rm, Delta), 1e-12);
            }
        }

        [Test]
        public void TestSpiralAngle_FollowsTheCircleFormula()
        {
            var trace = new SpiralToothTrace(Rm, Rad(35.0), Rc, true);
            var r0 = Math.Sqrt(Rm * Rm + Rc * Rc - 2.0 * Rm * Rc * Math.Sin(Rad(35.0)));
            Assert.AreEqual(r0, trace.CutterCentreDistance, 1e-9);

            foreach (var r in new[] { 50.0, 55.0, 65.0, 70.0 })
            {
                var expected = Math.Asin((r * r + Rc * Rc - r0 * r0) / (2.0 * r * Rc));
                Assert.AreEqual(expected, trace.SpiralAngleAt(r), 1e-12, $"R = {r}");
            }
        }

        [Test]
        public void TestTwist_HasTheSlopeOfTheSpiralAngle()
        {
            // d(phi)/dR = -tan(beta) / R, so the twist changes by -tan(beta) / (R sin(delta)) per unit of cone distance, and the tooth line
            // makes the spiral angle beta with the cone element: r d(psi) = R sin(delta) d(psi) = tan(beta) dR.
            var trace = new SpiralToothTrace(Rm, Rad(35.0), Rc, true);
            foreach (var r in new[] { 52.0, 60.0, 68.0 })
            {
                const double h = 1e-5;
                var slope = (trace.TwistAt(r + h, Delta) - trace.TwistAt(r - h, Delta)) / (2.0 * h);
                var expected = -Math.Tan(trace.SpiralAngleAt(r)) / (Math.Sin(Delta) * r);
                Assert.AreEqual(expected, slope, 1e-7, $"R = {r}");
            }
        }

        [Test]
        public void TestTwist_OfOppositeHandsIsOpposite_AndHeelAndToeTwistInOppositeDirections()
        {
            var right = new SpiralToothTrace(Rm, Rad(35.0), Rc, true);
            var left = right.WithHand(false);

            foreach (var r in new[] { 50.0, 70.0 })
            {
                Assert.AreEqual(-right.TwistAt(r, Delta), left.TwistAt(r, Delta), 1e-12);
            }

            Assert.Greater(right.TwistAt(50.0, Delta) * right.TwistAt(70.0, Delta) * -1.0, 0.0, "Heel and toe lie on opposite sides of the mean section.");
        }

        [Test]
        public void TestZerolTrace_HasNoSpiralAngleAtTheMeanPoint_ButCurvesAtHeelAndToe()
        {
            var trace = new SpiralToothTrace(Rm, 0.0, Rc, true);

            Assert.AreEqual(0.0, trace.SpiralAngleAt(Rm), 1e-12);
            Assert.Greater(trace.SpiralAngleAt(Rm + 10.0), 0.0, "Heel.");
            Assert.Less(trace.SpiralAngleAt(Rm - 10.0), 0.0, "Toe.");
            Assert.Greater(Math.Abs(trace.TwistAt(Rm + 10.0, Delta)), 1e-6);
            Assert.Greater(Math.Abs(trace.TwistAt(Rm - 10.0, Delta)), 1e-6);
        }

        [Test]
        public void TestStraightTrace_HasNoSpiralAngleAndNoTwist()
        {
            var trace = SpiralToothTrace.Straight;

            Assert.IsTrue(trace.IsStraight);
            Assert.AreEqual(0.0, trace.SpiralAngleAt(40.0), 0.0);
            Assert.AreEqual(0.0, trace.TwistAt(40.0, Delta), 0.0);
            Assert.AreSame(trace, trace.WithHand(false));
        }

        [Test]
        public void TestCutterThatDoesNotReachTheConeDistance_IsRejected()
        {
            var trace = new SpiralToothTrace(Rm, Rad(35.0), 10.0, true);

            Assert.IsFalse(trace.Reaches(Rm + 30.0));
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => trace.TwistAt(Rm + 30.0, Delta));
            StringAssert.Contains("does not reach", ex.Message);
        }

        [Test]
        public void TestConstructor_RejectsInvalidValues()
        {
            Assert.Throws<ArgumentException>(() => new SpiralToothTrace(0.0, 0.1, 50.0, true));
            Assert.Throws<ArgumentException>(() => new SpiralToothTrace(60.0, 0.1, 0.0, true));
            Assert.Throws<ArgumentException>(() => new SpiralToothTrace(60.0, Math.PI / 2.0, 50.0, true));
        }
    }
}
