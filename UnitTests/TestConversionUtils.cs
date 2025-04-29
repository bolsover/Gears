using Bolsover.Gears.Calculator;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class TestConversionUtils
    {
        [Test]
        public void TestRadiansConversion()
        {
            // Test conversion from degrees to radians
            Assert.That(ConversionUtils.Radians(180.0), Is.EqualTo(System.Math.PI).Within(0.000001));
            Assert.That(ConversionUtils.Radians(360.0), Is.EqualTo(2 * System.Math.PI).Within(0.000001));
            Assert.That(ConversionUtils.Radians(90.0), Is.EqualTo(System.Math.PI / 2).Within(0.000001));
        }

        [Test]
        public void TestDegreesConversion()
        {
            // Test conversion from radians to degrees
            Assert.That(ConversionUtils.Degrees(System.Math.PI), Is.EqualTo(180.0).Within(0.000001));
            Assert.That(ConversionUtils.Degrees(2 * System.Math.PI), Is.EqualTo(360.0).Within(0.000001));
            Assert.That(ConversionUtils.Degrees(System.Math.PI / 2), Is.EqualTo(90.0).Within(0.000001));
        }

        [Test]
        public void TestToDegreeFormat()
        {
            Assert.That(ConversionUtils.ToDegreeFormat(180.0), Is.EqualTo("180.000°"));
            Assert.That(ConversionUtils.ToDegreeFormat(90.0), Is.EqualTo("90.000°"));
            Assert.That(ConversionUtils.ToDegreeFormat(45.5), Is.EqualTo("45.500°"));
            Assert.That(ConversionUtils.ToDegreeFormat(0.0), Is.EqualTo("0.000°"));
            Assert.That(ConversionUtils.ToDegreeFormat(-90.123), Is.EqualTo("-90.123°"));
        }

        [Test]
        public void TestToFormat()
        {
            // Tests for a specific numeric format 
            Assert.That(ConversionUtils.ToFormat(1234.56789, "0.00"), Is.EqualTo("1234.57"));
            Assert.That(ConversionUtils.ToFormat(1234.56789, "0.000"), Is.EqualTo("1234.568"));
            Assert.That(ConversionUtils.ToFormat(-1234.56789, "0.00"), Is.EqualTo("-1234.57"));
            Assert.That(ConversionUtils.ToFormat(0.0, "0.000"), Is.EqualTo("0.000"));

            // Tests for unit appending (e.g., mm or in format)
            Assert.That(ConversionUtils.ToFormat(25.4, "0.00 mm"), Is.EqualTo("25.40 mm"));
            Assert.That(ConversionUtils.ToFormat(1.0, "0.000 in"), Is.EqualTo("1.000 in"));

            // Tests to ensure that the given format is respected
            Assert.That(ConversionUtils.ToFormat(3.14159, "0.0000"), Is.EqualTo("3.1416"));
            Assert.That(ConversionUtils.ToFormat(3.14159, "0.0"), Is.EqualTo("3.1"));
            Assert.That(ConversionUtils.ToFormat(3.14159, "0.000°"), Is.EqualTo("3.142°"));
            Assert.That(ConversionUtils.ToFormat(-3.14159, "0.000 mm"), Is.EqualTo("-3.142 mm"));
        }

        [Test]
        public void TestInchesToMillimeters()
        {
            // Test conversion from inches to millimeters with known values
            Assert.That(ConversionUtils.InchesToMillimeters(1.0), Is.EqualTo(25.4).Within(0.000001));
            Assert.That(ConversionUtils.InchesToMillimeters(0.0), Is.EqualTo(0.0).Within(0.000001));
            Assert.That(ConversionUtils.InchesToMillimeters(2.0), Is.EqualTo(50.8).Within(0.000001));
            Assert.That(ConversionUtils.InchesToMillimeters(0.5), Is.EqualTo(12.7).Within(0.000001));

            // Test conversion with negative values
            Assert.That(ConversionUtils.InchesToMillimeters(-1.0), Is.EqualTo(-25.4).Within(0.000001));
            Assert.That(ConversionUtils.InchesToMillimeters(-2.5), Is.EqualTo(-63.5).Within(0.000001));

            // Test precision with small values
            Assert.That(ConversionUtils.InchesToMillimeters(0.0001), Is.EqualTo(0.00254).Within(0.0000001));
            Assert.That(ConversionUtils.InchesToMillimeters(-0.0001), Is.EqualTo(-0.00254).Within(0.0000001));
        }

        [Test]
        public void TestMillimetersToInches()
        {
            // Test conversion from millimeters to inches with known values
            Assert.That(ConversionUtils.MillimetersToInches(25.4), Is.EqualTo(1.0).Within(0.000001));
            Assert.That(ConversionUtils.MillimetersToInches(50.8), Is.EqualTo(2.0).Within(0.000001));
            Assert.That(ConversionUtils.MillimetersToInches(12.7), Is.EqualTo(0.5).Within(0.000001));
            Assert.That(ConversionUtils.MillimetersToInches(0.0), Is.EqualTo(0.0).Within(0.000001));

            // Test conversion with negative values
            Assert.That(ConversionUtils.MillimetersToInches(-25.4), Is.EqualTo(-1.0).Within(0.000001));
            Assert.That(ConversionUtils.MillimetersToInches(-63.5), Is.EqualTo(-2.5).Within(0.000001));

            // Test precision with small values
            Assert.That(ConversionUtils.MillimetersToInches(0.00254), Is.EqualTo(0.0001).Within(0.0000001));
            Assert.That(ConversionUtils.MillimetersToInches(-0.00254), Is.EqualTo(-0.0001).Within(0.0000001));
        }
    }
}