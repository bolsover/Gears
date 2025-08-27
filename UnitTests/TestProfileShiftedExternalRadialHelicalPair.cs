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
            var result = _gear.CalculateVariable(VariableNames.RadialModule);
            Assert.That(result, Is.EqualTo(3d).Within(0.001d));
        }

        [Test]
        public void TestRadialPressureAngle()
        {
            var result = _gear.CalculateVariable(VariableNames.RadialPressureAngle);
            Assert.That(result * (180.0 / Math.PI), Is.EqualTo(20.0d).Within(0.001d));
        }

        [Test]
        public void TestWorkingCentreDistance()
        {
            var result = _gear.CalculateVariable(VariableNames.WorkingCentreDistanceax);
            Assert.That(result, Is.EqualTo(109.0d).Within(0.001d));
        }

        [Test]
        public void TestNormalPressureAngle()
        {
            var result = _gear.CalculateVariable(VariableNames.NormalPressureAngle);
            Assert.That(result * (180.0 / Math.PI), Is.EqualTo(17.4952d).Within(0.0001d));
        }


        [Test]
        public void TestStandardCentreDistance()
        {
            var result = _gear.CalculateVariable(VariableNames.StandardCentreDistance);
            Assert.That(result, Is.EqualTo(108.0d).Within(0.001d));
        }

        [Test]
        public void TestCentreDistanceIncrementFactor()
        {
            var result = _gear.CalculateVariable(VariableNames.CentreDistanceIncrementFactor);
            Assert.That(result, Is.EqualTo(0.3333d).Within(0.001d));
        }

        [Test]
        public void TestInvoluteFunction()
        {
            var result = _gear.CalculateVariable(VariableNames.InvoluteFunction);
            Assert.That(result, Is.EqualTo(0.01490d).Within(0.001d));
        }

        [Test]
        public void TestRadialWorkingInvoluteFunction()
        {
            var result = _gear.CalculateVariable(VariableNames.RadialWorkingInvoluteFunction);
            Assert.That(result, Is.EqualTo(0.01838d).Within(0.001d));
        }

        [Test]
        public void TestRadialWorkingPressureAngle()
        {
            var result = _gear.CalculateVariable(VariableNames.RadialWorkingPressureAngle);
            Assert.That(result * (180.0 / Math.PI), Is.EqualTo(21.39752d).Within(0.001d));
        }

        [Test]
        public void SumRadialCoefficientOfProfileShift()
        {
            var result = _gear.CalculateVariable(VariableNames.SumRadialCoefficientOfProfileShift);
            Assert.That(result, Is.EqualTo(0.34462d).Within(0.001d));
        }

        [Test]
        public void WholeDepth()
        {
            var result = _gear.CalculateVariable(VariableNames.WholeDepth);
            Assert.That(result, Is.EqualTo(6.716d).Within(0.001d));
        }

        [Test]
        public void TeethGear1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.Teeth);
            Assert.That(result, Is.EqualTo(12.0d).Within(0.001d));
        }

        [Test]
        public void HandGear1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.Hand);
            Assert.That(result, Is.EqualTo(0d).Within(0.001d));
        }

        [Test]
        public void RadialCoefficientOfProfileShiftGear1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.RadialCoefficientOfProfileShift);
            Assert.That(result, Is.EqualTo(0.34461d).Within(0.001d));
        }

        [Test]
        public void PitchDiameterGear1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.PitchDiameter);
            Assert.That(result, Is.EqualTo(36.0d).Within(0.001d));
        }

        [Test]
        public void WorkingPitchDiameterGear1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.WorkingPitchDiameter);
            Assert.That(result, Is.EqualTo(36.3333).Within(0.001d));
        }

        [Test]
        public void BaseDiameterGear1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.BaseDiameter);
            Assert.That(result, Is.EqualTo(33.8289d).Within(0.001d));
        }

        [Test]
        public void Addendum1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.Addendum);
            Assert.That(result, Is.EqualTo(4.0000d).Within(0.001d));
        }

        [Test]
        public void Dedendum1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.Dedendum);
            Assert.That(result, Is.EqualTo(2.7161d).Within(0.001d));
        }

        [Test]
        public void OutsideDiameter1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.OutsideDiameter);
            Assert.That(result, Is.EqualTo(44.000d).Within(0.001d));
        }


        [Test]
        public void RootDiameter1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.RootDiameter);
            Assert.That(result, Is.EqualTo(30.5677d).Within(0.001d));
        }


        [Test]
        public void TeethGear2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.Teeth);
            Assert.That(result, Is.EqualTo(60.0d).Within(0.001d));
        }

        [Test]
        public void HandGear2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.Hand);
            Assert.That(result, Is.EqualTo(1d).Within(0.001d));
        }

        [Test]
        public void RadialCoefficientOfProfileShiftGear2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.RadialCoefficientOfProfileShift);
            Assert.That(result, Is.EqualTo(0d).Within(0.001d));
        }

        [Test]
        public void PitchDiameterGear2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.PitchDiameter);
            Assert.That(result, Is.EqualTo(180d).Within(0.001d));
        }

        [Test]
        public void WorkingPitchDiameterGear2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.WorkingPitchDiameter);
            Assert.That(result, Is.EqualTo(181.6666d).Within(0.001d));
        }

        [Test]
        public void BaseDiameterGear2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.BaseDiameter);
            Assert.That(result, Is.EqualTo(169.14467d).Within(0.001d));
        }

        [Test]
        public void Addendum2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.Addendum);
            Assert.That(result, Is.EqualTo(2.9661d).Within(0.001d));
        }

        [Test]
        public void Dedendum2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.Dedendum);
            Assert.That(result, Is.EqualTo(3.75d).Within(0.001d));
        }

        [Test]
        public void OutsideDiameter2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.OutsideDiameter);
            Assert.That(result, Is.EqualTo(185.93228d).Within(0.001d));
        }

        [Test]
        public void RootDiameter2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.RootDiameter);
            Assert.That(result, Is.EqualTo(172.5d).Within(0.001d));
        }
    }
}