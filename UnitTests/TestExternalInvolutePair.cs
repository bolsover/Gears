using System;
using Bolsover.Gears.Calculator;

using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    public class TestExternalInvolutePair
    {
       
        private Gear _gear;


        [SetUp]
        public void SetUp()
        {
           
            _gear = new Gear(GearContext.ExternalInvolutePair, CalculationMode.Automatic);
            _gear.Calculate();
        }

        [Test]
        public void TestParentModue()
        {
            var module = _gear.CalculateVariable(VariableNames.Module);
            Assert.That(module, Is.EqualTo(3d).Within(0.001d));
        }

        [Test]
        public void TestParentPressureAngle()
        {
            var pressureAngle = _gear.CalculateVariable(VariableNames.PressureAngle);
            Assert.That(pressureAngle, Is.EqualTo(20.0d * Math.PI / 180).Within(0.001d));
        }

        [Test]
        public void TestParentCentreDistance()
        {
            var standardCentreDistance = _gear.CalculateVariable(VariableNames.StandardCentreDistance);
            Assert.That(standardCentreDistance , Is.EqualTo(54d).Within(0.001d));
        }

        [Test]
        public void TestInvoluteFunction()
        {
            var involuteFunction = _gear.CalculateVariable(VariableNames.InvoluteFunction);
            Assert.That(involuteFunction , Is.EqualTo(0.01490d).Within(0.0001d));
        }

        [Test]
        public void TestContactRatio()
        {
            var cantactRatio = _gear.CalculateVariable(VariableNames.ContactRatio);
            Assert.That(cantactRatio , Is.EqualTo(1.5111d).Within(0.0001d));
        }

        [Test]
        public void TestGear1Teeth()
        {
            var teeth = _gear.Gears[0].CalculateVariable(VariableNames.Teeth);
            Assert.That(teeth , Is.EqualTo(12d).Within(0.0001d));
        }

        [Test]
        public void TestGear2Teeth()
        {
            var teeth = _gear.Gears[1].CalculateVariable(VariableNames.Teeth);
            Assert.That(teeth , Is.EqualTo(24d).Within(0.0001d));
        }

        [Test]
        public void TestGear1PitchDiameter()
        {
            var pitchDiameter = _gear.Gears[0].CalculateVariable(VariableNames.PitchDiameter);
            Assert.That(pitchDiameter , Is.EqualTo(36d).Within(0.0001d));
        }

        [Test]
        public void TestGear2PitchDiameter()
        {
            var pitchDiameter = _gear.Gears[1].CalculateVariable(VariableNames.PitchDiameter);
            Assert.That(pitchDiameter , Is.EqualTo(72d).Within(0.0001d));
        }

        [Test]
        public void TestGear1BaseDiameter()
        {
            var pitchDiameter = _gear.Gears[0].CalculateVariable(VariableNames.BaseDiameter);
            Assert.That(pitchDiameter , Is.EqualTo(33.8289d).Within(0.0001d));
        }

        [Test]
        public void TestGear2BaseDiameter()
        {
            var pitchDiameter = _gear.Gears[1].CalculateVariable(VariableNames.BaseDiameter);
            Assert.That(pitchDiameter , Is.EqualTo(67.6578d).Within(0.0001d));
        }

        [Test]
        public void TestGear1Addendum()
        {
            var addendum = _gear.Gears[0].CalculateVariable(VariableNames.Addendum);
            Assert.That(addendum , Is.EqualTo(3.0d).Within(0.0001d));
        }

        [Test]
        public void TestGear2Addendum()
        {
            var addendum = _gear.Gears[1].CalculateVariable(VariableNames.Addendum);
            Assert.That(addendum , Is.EqualTo(3.0d).Within(0.0001d));
        }

        [Test]
        public void TestGear1Dedendum()
        {
            var dedendum = _gear.Gears[0].CalculateVariable(VariableNames.Dedendum);
            Assert.That(dedendum , Is.EqualTo(3.75d).Within(0.0001d));
        }

        [Test]
        public void TestGear2Dedendum()
        {
            var dedendum = _gear.Gears[1].CalculateVariable(VariableNames.Dedendum);
            Assert.That(dedendum , Is.EqualTo(3.75d).Within(0.0001d));
        }

        [Test]
        public void TestGear1OutsideDiameter()
        {
            var outsideDiameter = _gear.Gears[0].CalculateVariable(VariableNames.OutsideDiameter);
            Assert.That(outsideDiameter , Is.EqualTo(42.0d).Within(0.0001d));
        }

        [Test]
        public void TestGear2OutsideDiameter()
        {
            var outsideDiameter = _gear.Gears[1].CalculateVariable(VariableNames.OutsideDiameter);
            Assert.That(outsideDiameter , Is.EqualTo(78.0d).Within(0.0001d));
        }

        [Test]
        public void TestGear1RootDiameter()
        {
            var rootDiameter = _gear.Gears[0].CalculateVariable(VariableNames.RootDiameter);
            Assert.That(rootDiameter , Is.EqualTo(28.5d).Within(0.0001d));
        }

        [Test]
        public void TestGear2RootDiameter()
        {
            var rootDiameter = _gear.Gears[1].CalculateVariable(VariableNames.RootDiameter);
            Assert.That(rootDiameter , Is.EqualTo(64.5d).Within(0.0001d));
        }
    }
}