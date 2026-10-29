using Bolsover.Gears.Calculator;
using NUnit.Framework;
using System;

namespace UnitTests
{
    [TestFixture]
    public class TestGearParameterCalculator
    {
        [Test]
        public void TestStandardExternalGearCalculation()
        {
            var z = 20;
            var m = 2.0;
            var alphaDeg = 20.0;
            var faceWidth = 15.0;
            var x = 0.0;

            var result = GearParameterCalculator.Calculate(z, m, alphaDeg, faceWidth, x, false);

            Assert.IsNotNull(result);
            Assert.AreEqual(20, result.Teeth);
            Assert.AreEqual(2.0, result.Module);
            Assert.AreEqual(40.0, result.PitchDiameter, 1e-6); // d = m * z = 40.0
            Assert.AreEqual(40.0 * Math.Cos(20.0 * Math.PI / 180.0), result.BaseDiameter, 1e-6); // db = d * cos(alpha)
            Assert.AreEqual(2.0, result.Addendum, 1e-6); // ha = m * (1 + 0) = 2.0
            Assert.AreEqual(2.5, result.Dedendum, 1e-6); // hf = m * (1.25 - 0) = 2.5
            Assert.AreEqual(4.5, result.WholeDepth, 1e-6); // h = ha + hf = 4.5
            Assert.AreEqual(44.0, result.TipDiameter, 1e-6); // da = d + 2 * ha = 44.0
            Assert.AreEqual(35.0, result.RootDiameter, 1e-6); // df = d - 2 * hf = 35.0
            Assert.AreEqual(2.0 * Math.PI, result.CircularPitch, 1e-6); // p = pi * m
            Assert.AreEqual(Math.PI, result.ToothThickness, 1e-6); // s = m * pi / 2
            Assert.AreEqual(2.0 * Math.PI * Math.Cos(20.0 * Math.PI / 180.0), result.BasePitch, 1e-6); // pb = pi * m * cos(alpha)
        }

        [Test]
        public void TestProfileShiftedExternalGearCalculation()
        {
            var z = 25;
            var m = 3.0;
            var alphaDeg = 20.0;
            var faceWidth = 20.0;
            var x = 0.35;

            var result = GearParameterCalculator.Calculate(z, m, alphaDeg, faceWidth, x, false);

            Assert.AreEqual(75.0, result.PitchDiameter, 1e-6); // d = 3 * 25 = 75
            Assert.AreEqual(3.0 * (1.0 + 0.35), result.Addendum, 1e-6); // ha = 4.05
            Assert.AreEqual(3.0 * (1.25 - 0.35), result.Dedendum, 1e-6); // hf = 2.7
            Assert.AreEqual(75.0 + 2.0 * 4.05, result.TipDiameter, 1e-6); // da = 83.1
            Assert.AreEqual(75.0 - 2.0 * 2.7, result.RootDiameter, 1e-6); // df = 69.6
            Assert.Greater(result.ToothThickness, 3.0 * Math.PI / 2.0); // Tooth thickness increases with positive shift
        }

        [Test]
        public void TestInternalGearCalculation()
        {
            var z = 40;
            var m = 2.5;
            var alphaDeg = 20.0;
            var faceWidth = 18.0;
            var x = 0.0;
            var rimThickness = 12.0;

            var result = GearParameterCalculator.Calculate(z, m, alphaDeg, faceWidth, x, true, rimThickness);

            Assert.AreEqual(100.0, result.PitchDiameter, 1e-6); // d = 2.5 * 40 = 100
            Assert.AreEqual(2.5, result.Addendum, 1e-6); // ha = m * (1 - 0) = 2.5
            Assert.AreEqual(3.125, result.Dedendum, 1e-6); // hf = m * (1.25 + 0) = 3.125
            Assert.AreEqual(100.0 - 2.0 * 2.5, result.TipDiameter, 1e-6); // Inside tip da = 95.0
            Assert.AreEqual(100.0 + 2.0 * 3.125, result.RootDiameter, 1e-6); // df = 106.25
            Assert.AreEqual(12.0, result.RimThickness, 1e-6);
        }

        [Test]
        public void TestParameterRowsContainExpectedFormulas()
        {
            var result = GearParameterCalculator.Calculate(20, 2.0, 20.0, 15.0, 0.0, false);

            Assert.IsNotNull(result.ParameterRows);
            Assert.Greater(result.ParameterRows.Count, 10);

            var pitchRow = result.ParameterRows.Find(r => r.ParameterName == "Pitch Diameter");
            Assert.IsNotNull(pitchRow);
            Assert.AreEqual("d", pitchRow.SymbolLatex);
            Assert.AreEqual("m \\cdot z", pitchRow.FormulaLatex);
            Assert.AreEqual(40.0, pitchRow.Value, 1e-6);
            Assert.AreEqual("mm", pitchRow.Unit);

            var baseRow = result.ParameterRows.Find(r => r.ParameterName == "Base Diameter");
            Assert.IsNotNull(baseRow);
            Assert.AreEqual("d_b", baseRow.SymbolLatex);

            var addendumRow = result.ParameterRows.Find(r => r.ParameterName == "Addendum");
            Assert.IsNotNull(addendumRow);
            Assert.AreEqual("h_a", addendumRow.SymbolLatex);
        }
    }
}
