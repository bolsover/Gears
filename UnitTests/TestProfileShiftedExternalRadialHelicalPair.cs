using System;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    public class TestProfileShiftedExternalRadialHelicalPair
    {
        private Gear _gear;


        [SetUp]
        public void SetUp()
        {
            _gear = new Gear(GearContext.ProfileShiftedExternalRadialHelicalPair, CalculationMode.Automatic);
        }

        [Test]
        public void TestRadialModue()
        {
            var result = _gear.CalculateGearVariable(VariableNames.RadialModule);
            Assert.That(result, Is.EqualTo(3d).Within(0.001d));
        }

        [Test]
        public void TestRadialPressureAngle()
        {
            var result = _gear.CalculateGearVariable(VariableNames.RadialPressureAngle);
            Assert.That(result * (180.0 / Math.PI), Is.EqualTo(20.0d).Within(0.001d));
        }

        [Test]
        public void TestWorkingCentreDistance()
        {
            var result = _gear.CalculateGearVariable(VariableNames.WorkingCentreDistanceax);
            Assert.That(result, Is.EqualTo(109.0d).Within(0.001d));
        }

        [Test]
        public void TestNormalPressureAngle()
        {
            var result = _gear.CalculateGearVariable(VariableNames.NormalPressureAngle);
            Assert.That(result * (180.0 / Math.PI), Is.EqualTo(17.4952d).Within(0.0001d));
        }


        [Test]
        public void TestStandardCentreDistance()
        {
            var result = _gear.CalculateGearVariable(VariableNames.StandardCentreDistance);
            Assert.That(result, Is.EqualTo(108.0d).Within(0.001d));
        }

        [Test]
        public void TestCentreDistanceIncrementFactor()
        {
            var result = _gear.CalculateGearVariable(VariableNames.CentreDistanceIncrementFactor);
            Assert.That(result, Is.EqualTo(0.3333d).Within(0.001d));
        }

        [Test]
        public void TestInvoluteFunction()
        {
            var result = _gear.CalculateGearVariable(VariableNames.InvoluteFunction);
            Assert.That(result, Is.EqualTo(0.01490d).Within(0.001d));
        }

        [Test]
        public void TestRadialWorkingInvoluteFunction()
        {
            var result = _gear.CalculateGearVariable(VariableNames.RadialWorkingInvoluteFunction);
            Assert.That(result, Is.EqualTo(0.01838d).Within(0.001d));
        }

        [Test]
        public void TestRadialWorkingPressureAngle()
        {
            var result = _gear.CalculateGearVariable(VariableNames.RadialWorkingPressureAngle);
            Assert.That(result * (180.0 / Math.PI), Is.EqualTo(21.39752d).Within(0.001d));
        }

        [Test]
        public void SumRadialCoefficientOfProfileShift()
        {
            var result = _gear.CalculateGearVariable(VariableNames.SumRadialCoefficientOfProfileShift);
            Assert.That(result, Is.EqualTo(0.34462d).Within(0.001d));
        }

        [Test]
        public void WholeDepth()
        {
            var result = _gear.CalculateGearVariable(VariableNames.WholeDepth);
            Assert.That(result, Is.EqualTo(6.716d).Within(0.001d));
        }

        [Test]
        public void TeethGear1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Teethz1);
            Assert.That(result, Is.EqualTo(12.0d).Within(0.001d));
        }

        [Test]
        public void HandGear1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Hand1);
            Assert.That(result, Is.EqualTo(0d).Within(0.001d));
        }

        [Test]
        public void RadialCoefficientOfProfileShiftGear1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.RadialCoefficientOfProfileShiftxt1);
            Assert.That(result, Is.EqualTo(0.34461d).Within(0.001d));
        }

        [Test]
        public void PitchDiameterGear1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.PitchDiameterd1);
            Assert.That(result, Is.EqualTo(36.0d).Within(0.001d));
        }

        [Test]
        public void WorkingPitchDiameterGear1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.WorkingPitchDiameterdw1);
            Assert.That(result, Is.EqualTo(36.3333).Within(0.001d));
        }

        [Test]
        public void BaseDiameterGear1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.BaseDiameterdb1);
            Assert.That(result, Is.EqualTo(33.8289d).Within(0.001d));
        }

        [Test]
        public void Addendum1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Addendumha1);
            Assert.That(result, Is.EqualTo(4.0000d).Within(0.001d));
        }

        [Test]
        public void Dedendum1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Dedendumhf1);
            Assert.That(result, Is.EqualTo(2.7161d).Within(0.001d));
        }

        [Test]
        public void OutsideDiameter1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.OutsideDiameterda1);
            Assert.That(result, Is.EqualTo(44.000d).Within(0.001d));
        }


        [Test]
        public void RootDiameter1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.RootDiameterdf1);
            Assert.That(result, Is.EqualTo(30.5677d).Within(0.001d));
        }


        [Test]
        public void TeethGear2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Teethz2);
            Assert.That(result, Is.EqualTo(60.0d).Within(0.001d));
        }

        [Test]
        public void HandGear2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Hand2);
            Assert.That(result, Is.EqualTo(1d).Within(0.001d));
        }

        [Test]
        public void RadialCoefficientOfProfileShiftGear2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.RadialCoefficientOfProfileShiftxt2);
            Assert.That(result, Is.EqualTo(0d).Within(0.001d));
        }

        [Test]
        public void PitchDiameterGear2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.PitchDiameterd2);
            Assert.That(result, Is.EqualTo(180d).Within(0.001d));
        }

        [Test]
        public void WorkingPitchDiameterGear2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.WorkingPitchDiameterdw2);
            Assert.That(result, Is.EqualTo(181.6666d).Within(0.001d));
        }

        [Test]
        public void BaseDiameterGear2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.BaseDiameterdb2);
            Assert.That(result, Is.EqualTo(169.14467d).Within(0.001d));
        }

        [Test]
        public void Addendum2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Addendumha2);
            Assert.That(result, Is.EqualTo(2.9661d).Within(0.001d));
        }

        [Test]
        public void Dedendum2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Dedendumhf2);
            Assert.That(result, Is.EqualTo(3.75d).Within(0.001d));
        }

        [Test]
        public void OutsideDiameter2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.OutsideDiameterda2);
            Assert.That(result, Is.EqualTo(185.93228d).Within(0.001d));
        }

        [Test]
        public void RootDiameter2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.RootDiameterdf2);
            Assert.That(result, Is.EqualTo(172.5d).Within(0.001d));
        }
    }
}