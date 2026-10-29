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

        [Test]
        public void TestStandardExternalPairCalculation()
        {
            var pair = GearParameterCalculator.CalculatePair(
                z1: 20, m: 2.0, alphaDeg: 20.0, faceWidth1: 15.0, x1: 0.0, isInternal1: false, rimThickness1: 10.0,
                z2: 40, faceWidth2: 15.0, x2: 0.0, isInternal2: false, rimThickness2: 10.0);

            Assert.IsNotNull(pair);
            Assert.AreEqual(20, pair.Gear1.Teeth);
            Assert.AreEqual(40, pair.Gear2.Teeth);
            Assert.AreEqual(40.0, pair.Gear1.PitchDiameter, 1e-6);
            Assert.AreEqual(80.0, pair.Gear2.PitchDiameter, 1e-6);
            Assert.AreEqual(60.0, pair.StandardCentreDistance, 1e-6); // a = m * (z1 + z2) / 2 = 2 * 60 / 2 = 60
            Assert.AreEqual(60.0, pair.WorkingCentreDistance, 1e-6); // zero profile shift => aw = a
            Assert.AreEqual(20.0, pair.WorkingPressureAngleDeg, 1e-6);
            Assert.AreEqual(2.0, pair.GearRatio, 1e-6); // z2 / z1 = 40 / 20 = 2.0
            Assert.Greater(pair.ContactRatio, 1.0);
            Assert.Greater(pair.ParameterRows.Count, 20);
        }

        [Test]
        public void TestProfileShiftedExternalPairCalculation()
        {
            var pair = GearParameterCalculator.CalculatePair(
                z1: 16, m: 3.0, alphaDeg: 20.0, faceWidth1: 20.0, x1: 0.3, isInternal1: false, rimThickness1: 10.0,
                z2: 24, faceWidth2: 20.0, x2: 0.2, isInternal2: false, rimThickness2: 10.0);

            Assert.IsNotNull(pair);
            Assert.AreEqual(60.0, pair.StandardCentreDistance, 1e-6); // 3 * 40 / 2 = 60
            Assert.Greater(pair.WorkingCentreDistance, pair.StandardCentreDistance); // Positive sum shift increases aw
            Assert.Greater(pair.WorkingPressureAngleDeg, 20.0);
            Assert.Greater(pair.CentreDistanceIncrementFactor, 0.0);
            Assert.Greater(pair.ContactRatio, 1.0);

            var awRow = pair.ParameterRows.Find(r => r.ParameterName == "Working Centre Distance");
            Assert.IsNotNull(awRow);
            Assert.AreEqual("a_w", awRow.SymbolLatex);
            Assert.AreEqual(pair.WorkingCentreDistance, awRow.Value, 1e-6);
        }

        [Test]
        public void TestInternalExternalPairCalculation()
        {
            var pair = GearParameterCalculator.CalculatePair(
                z1: 20, m: 2.5, alphaDeg: 20.0, faceWidth1: 15.0, x1: 0.0, isInternal1: false, rimThickness1: 10.0,
                z2: 60, faceWidth2: 15.0, x2: 0.0, isInternal2: true, rimThickness2: 12.0);

            Assert.IsNotNull(pair);
            Assert.IsFalse(pair.Gear1.IsInternal);
            Assert.IsTrue(pair.Gear2.IsInternal);
            Assert.AreEqual(2.5 * (60 - 20) / 2.0, pair.StandardCentreDistance, 1e-6); // m*(z2 - z1)/2 = 50.0
            Assert.AreEqual(50.0, pair.WorkingCentreDistance, 1e-6);
            Assert.AreEqual(3.0, pair.GearRatio, 1e-6);
            Assert.Greater(pair.ContactRatio, 1.0);
        }

        [Test]
        public void TestClearanceCalculation_CustomClearance_ExternalGear()
        {
            var z = 20;
            var m = 2.0;
            var alphaDeg = 20.0;
            var faceWidth = 15.0;
            var x = 0.0;
            var customClearance = 0.6; // non-standard clearance (standard is 0.25 * 2 = 0.5)

            var result = GearParameterCalculator.Calculate(z, m, alphaDeg, faceWidth, x, false, 10.0, customClearance);

            Assert.IsNotNull(result);
            Assert.AreEqual(customClearance, result.Clearance, 1e-6);
            Assert.AreEqual(2.0, result.Addendum, 1e-6); // ha = m * (1 + x) = 2.0
            Assert.AreEqual(2.0 + customClearance, result.Dedendum, 1e-6); // hf = m * (1 - x) + c = 2.0 + 0.6 = 2.6
            Assert.AreEqual(4.6, result.WholeDepth, 1e-6); // h = ha + hf = 2.0 + 2.6 = 4.6
            Assert.AreEqual(44.0, result.TipDiameter, 1e-6); // da = d + 2 * ha = 44.0
            Assert.AreEqual(40.0 - 2.0 * 2.6, result.RootDiameter, 1e-6); // df = d - 2 * hf = 34.8

            var clearanceRow = result.ParameterRows.Find(r => r.ParameterName == "Bottom Clearance");
            Assert.IsNotNull(clearanceRow);
            Assert.AreEqual("c", clearanceRow.SymbolLatex);
            Assert.AreEqual("c", clearanceRow.FormulaLatex);
            Assert.AreEqual(customClearance, clearanceRow.Value, 1e-6);
            Assert.AreEqual("mm", clearanceRow.Unit);

            var dedendumRow = result.ParameterRows.Find(r => r.ParameterName == "Dedendum");
            Assert.IsNotNull(dedendumRow);
            Assert.AreEqual("m(1 - x) + c", dedendumRow.FormulaLatex);
            Assert.AreEqual(2.6, dedendumRow.Value, 1e-6);
        }

        [Test]
        public void TestClearanceCalculation_CustomClearance_InternalGear()
        {
            var z = 40;
            var m = 2.0;
            var alphaDeg = 20.0;
            var faceWidth = 15.0;
            var x = 0.1;
            var customClearance = 0.75;

            var result = GearParameterCalculator.Calculate(z, m, alphaDeg, faceWidth, x, true, 10.0, customClearance);

            Assert.IsNotNull(result);
            Assert.AreEqual(customClearance, result.Clearance, 1e-6);
            Assert.AreEqual(2.0 * (1.0 - 0.1), result.Addendum, 1e-6); // ha = 1.8
            Assert.AreEqual(2.0 * (1.0 + 0.1) + customClearance, result.Dedendum, 1e-6); // hf = 2.2 + 0.75 = 2.95
            Assert.AreEqual(1.8 + 2.95, result.WholeDepth, 1e-6); // h = 4.75
            Assert.AreEqual(80.0 - 2.0 * 1.8, result.TipDiameter, 1e-6); // da = 76.4
            Assert.AreEqual(80.0 + 2.0 * 2.95, result.RootDiameter, 1e-6); // df = 85.9

            var dedendumRow = result.ParameterRows.Find(r => r.ParameterName == "Dedendum");
            Assert.IsNotNull(dedendumRow);
            Assert.AreEqual("m(1 + x) + c", dedendumRow.FormulaLatex);
            Assert.AreEqual(2.95, dedendumRow.Value, 1e-6);
        }

        [Test]
        public void TestClearanceCalculation_GearPairWithClearance()
        {
            var pair = GearParameterCalculator.CalculatePair(
                z1: 20, m: 2.0, alphaDeg: 20.0, faceWidth1: 15.0, x1: 0.0, isInternal1: false, rimThickness1: 10.0,
                z2: 30, faceWidth2: 15.0, x2: 0.0, isInternal2: false, rimThickness2: 10.0,
                clearance1: 0.6, clearance2: 0.7);

            Assert.IsNotNull(pair);
            Assert.AreEqual(0.6, pair.Gear1.Clearance, 1e-6);
            Assert.AreEqual(0.7, pair.Gear2.Clearance, 1e-6);
            Assert.AreEqual(0.6, pair.Gear1Clearance, 1e-6);
            Assert.AreEqual(0.7, pair.Gear2Clearance, 1e-6);

            var c1Row = pair.ParameterRows.Find(r => r.ParameterName == "Gear 1 Clearance");
            Assert.IsNotNull(c1Row);
            Assert.AreEqual("c_1", c1Row.SymbolLatex);
            Assert.AreEqual(0.6, c1Row.Value, 1e-6);

            var c2Row = pair.ParameterRows.Find(r => r.ParameterName == "Gear 2 Clearance");
            Assert.IsNotNull(c2Row);
            Assert.AreEqual("c_2", c2Row.SymbolLatex);
            Assert.AreEqual(0.7, c2Row.Value, 1e-6);

            var hf1Row = pair.ParameterRows.Find(r => r.ParameterName == "Gear 1 Dedendum");
            Assert.IsNotNull(hf1Row);
            Assert.AreEqual("m(1 - x_1) + c_1", hf1Row.FormulaLatex);
            Assert.AreEqual(2.6, hf1Row.Value, 1e-6);

            var hf2Row = pair.ParameterRows.Find(r => r.ParameterName == "Gear 2 Dedendum");
            Assert.IsNotNull(hf2Row);
            Assert.AreEqual("m(1 - x_2) + c_2", hf2Row.FormulaLatex);
            Assert.AreEqual(2.7, hf2Row.Value, 1e-6);
        }

        [Test]
        public void TestBacklashCalculation_SingleGear()
        {
            var z = 20;
            var m = 2.0;
            var alphaDeg = 20.0;
            var faceWidth = 15.0;
            var x = 0.0;
            var backlash = 0.08;

            var result = GearParameterCalculator.Calculate(z, m, alphaDeg, faceWidth, x, false, 10.0, null, backlash);

            Assert.IsNotNull(result);
            Assert.AreEqual(backlash, result.Backlash, 1e-6);
            // s = m * (pi / 2 + 2 * x * tan(alpha)) - jt = 2 * (pi / 2) - 0.08 = pi - 0.08
            Assert.AreEqual(Math.PI - backlash, result.ToothThickness, 1e-6);

            var backlashRow = result.ParameterRows.Find(r => r.ParameterName == "Backlash (Flank Clearance)");
            Assert.IsNotNull(backlashRow);
            Assert.AreEqual("j_t", backlashRow.SymbolLatex);
            Assert.AreEqual("j_t", backlashRow.FormulaLatex);
            Assert.AreEqual(backlash, backlashRow.Value, 1e-6);
            Assert.AreEqual("mm", backlashRow.Unit);

            var thicknessRow = result.ParameterRows.Find(r => r.ParameterName == "Circular Tooth Thickness");
            Assert.IsNotNull(thicknessRow);
            Assert.AreEqual("m(\\frac{\\pi}{2} + 2 x \\tan\\alpha) - j_t", thicknessRow.FormulaLatex);
            Assert.AreEqual(Math.PI - backlash, thicknessRow.Value, 1e-6);
        }

        [Test]
        public void TestBacklashCalculation_GearPair()
        {
            var pair = GearParameterCalculator.CalculatePair(
                z1: 20, m: 2.0, alphaDeg: 20.0, faceWidth1: 15.0, x1: 0.0, isInternal1: false, rimThickness1: 10.0,
                z2: 30, faceWidth2: 15.0, x2: 0.0, isInternal2: false, rimThickness2: 10.0,
                clearance1: null, clearance2: null,
                backlash1: 0.05, backlash2: 0.07);

            Assert.IsNotNull(pair);
            Assert.AreEqual(0.05, pair.Gear1.Backlash, 1e-6);
            Assert.AreEqual(0.07, pair.Gear2.Backlash, 1e-6);
            Assert.AreEqual(0.05, pair.Gear1Backlash, 1e-6);
            Assert.AreEqual(0.07, pair.Gear2Backlash, 1e-6);
            Assert.AreEqual(0.12, pair.TotalBacklash, 1e-6);
            Assert.AreEqual(0.12 * Math.Cos(pair.WorkingPressureAngleRad), pair.NormalBacklash, 1e-6);

            var jt1Row = pair.ParameterRows.Find(r => r.ParameterName == "Gear 1 Backlash");
            Assert.IsNotNull(jt1Row);
            Assert.AreEqual("j_{t1}", jt1Row.SymbolLatex);
            Assert.AreEqual(0.05, jt1Row.Value, 1e-6);

            var jt2Row = pair.ParameterRows.Find(r => r.ParameterName == "Gear 2 Backlash");
            Assert.IsNotNull(jt2Row);
            Assert.AreEqual("j_{t2}", jt2Row.SymbolLatex);
            Assert.AreEqual(0.07, jt2Row.Value, 1e-6);

            var totalJtRow = pair.ParameterRows.Find(r => r.ParameterName == "Total Circular Backlash");
            Assert.IsNotNull(totalJtRow);
            Assert.AreEqual("j_t", totalJtRow.SymbolLatex);
            Assert.AreEqual("j_{t1} + j_{t2}", totalJtRow.FormulaLatex);
            Assert.AreEqual(0.12, totalJtRow.Value, 1e-6);

            var jnRow = pair.ParameterRows.Find(r => r.ParameterName == "Normal Backlash");
            Assert.IsNotNull(jnRow);
            Assert.AreEqual("j_n", jnRow.SymbolLatex);
            Assert.AreEqual("j_t \\cos\\alpha_w", jnRow.FormulaLatex);
            Assert.AreEqual(pair.NormalBacklash, jnRow.Value, 1e-6);

            var s1Row = pair.ParameterRows.Find(r => r.ParameterName == "Gear 1 Tooth Thickness");
            Assert.IsNotNull(s1Row);
            Assert.AreEqual("m(\\frac{\\pi}{2} + 2 x_1 \\tan\\alpha) - j_{t1}", s1Row.FormulaLatex);
            Assert.AreEqual(Math.PI - 0.05, s1Row.Value, 1e-6);

            var s2Row = pair.ParameterRows.Find(r => r.ParameterName == "Gear 2 Tooth Thickness");
            Assert.IsNotNull(s2Row);
            Assert.AreEqual("m(\\frac{\\pi}{2} + 2 x_2 \\tan\\alpha) - j_{t2}", s2Row.FormulaLatex);
            Assert.AreEqual(Math.PI - 0.07, s2Row.Value, 1e-6);
        }

        [Test]
        public void TestHelicalGearCalculation_SingleGear()
        {
            var z = 20;
            var m = 2.0;
            var alphaDeg = 20.0;
            var faceWidth = 20.0;
            var x = 0.0;
            var betaDeg = 25.0;
            var direction = "R";

            var result = GearParameterCalculator.Calculate(
                z, m, alphaDeg, faceWidth, x, false, 10.0, null, null, betaDeg, direction);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsHelical);
            Assert.AreEqual(betaDeg, result.HelixAngleDeg, 1e-6);
            Assert.AreEqual(betaDeg * Math.PI / 180.0, result.HelixAngleRad, 1e-6);
            Assert.AreEqual("R", result.HelicalDirection);

            var cosBeta = Math.Cos(betaDeg * Math.PI / 180.0);
            var expectedMt = m / cosBeta;
            var expectedAlphaT = Math.Atan(Math.Tan(alphaDeg * Math.PI / 180.0) / cosBeta) * 180.0 / Math.PI;

            Assert.AreEqual(expectedMt, result.RadialModule, 1e-6);
            Assert.AreEqual(expectedAlphaT, result.RadialPressureAngleDeg, 1e-6);
            Assert.AreEqual(expectedMt * z, result.PitchDiameter, 1e-6);
            Assert.AreEqual(result.PitchDiameter * Math.Cos(result.RadialPressureAngleRad), result.BaseDiameter, 1e-6);
            Assert.AreEqual(Math.PI * m / Math.Sin(result.HelixAngleRad), result.AxialPitch, 1e-6);
            Assert.AreEqual(z * result.AxialPitch, result.Lead, 1e-6);

            var helixRow = result.ParameterRows.Find(r => r.ParameterName == "Helix Angle");
            Assert.IsNotNull(helixRow);
            Assert.AreEqual("\\beta", helixRow.SymbolLatex);
            Assert.AreEqual(25.0, helixRow.Value, 1e-6);

            var dirRow = result.ParameterRows.Find(r => r.ParameterName == "Helical Direction");
            Assert.IsNotNull(dirRow);
            Assert.AreEqual("R", dirRow.Unit);
        }

        [Test]
        public void TestHelicalGearPairCalculation()
        {
            var pair = GearParameterCalculator.CalculatePair(
                z1: 20, m: 2.0, alphaDeg: 20.0, faceWidth1: 25.0, x1: 0.0, isInternal1: false, rimThickness1: 10.0,
                z2: 40, faceWidth2: 25.0, x2: 0.0, isInternal2: false, rimThickness2: 10.0,
                clearance1: null, clearance2: null,
                backlash1: null, backlash2: null,
                helixAngle1: 20.0, helixAngle2: 20.0,
                helicalDirection1: "R", helicalDirection2: "L");

            Assert.IsNotNull(pair);
            Assert.AreEqual(20.0, pair.Gear1HelixAngle, 1e-6);
            Assert.AreEqual(20.0, pair.Gear2HelixAngle, 1e-6);
            Assert.AreEqual("R", pair.Gear1HelicalDirection);
            Assert.AreEqual("L", pair.Gear2HelicalDirection);

            var cosBeta = Math.Cos(20.0 * Math.PI / 180.0);
            var expectedMt = 2.0 / cosBeta;
            Assert.AreEqual(expectedMt * (20 + 40) / 2.0, pair.StandardCentreDistance, 1e-6);
            Assert.AreEqual(pair.StandardCentreDistance, pair.WorkingCentreDistance, 1e-6);
            Assert.Greater(pair.AxialContactRatio, 1.0);
            Assert.Greater(pair.ContactRatio, 2.0);

            var beta1Row = pair.ParameterRows.Find(r => r.ParameterName == "Gear 1 Helix Angle");
            Assert.IsNotNull(beta1Row);
            Assert.AreEqual(20.0, beta1Row.Value, 1e-6);

            var totalCrRow = pair.ParameterRows.Find(r => r.ParameterName == "Total Contact Ratio");
            Assert.IsNotNull(totalCrRow);
            Assert.AreEqual(pair.ContactRatio, totalCrRow.Value, 1e-6);
        }
    }
}
