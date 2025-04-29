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
            var module = _gear.GetVariable(VariableNames.Module);
            Assert.That(module.Value, Is.EqualTo(3d).Within(0.001d));
        }

        [Test]
        public void TestParentPressureAngle()
        {
            var pressureAngle = _gear.GetVariable(VariableNames.PressureAngle);
            Assert.That(pressureAngle.Value, Is.EqualTo(20.0d * Math.PI / 180).Within(0.001d));
        }

        [Test]
        public void TestParentCentreDistance()
        {
            var standardCentreDistance = _gear.GetVariable(VariableNames.StandardCentreDistance);
            Assert.That(standardCentreDistance.Value, Is.EqualTo(54d).Within(0.001d));
        }

        [Test]
        public void TestInvoluteFunction()
        {
            var involuteFunction = _gear.GetVariable(VariableNames.InvoluteFunction);
            Assert.That(involuteFunction.Value, Is.EqualTo(0.01490d).Within(0.0001d));
        }

        [Test]
        public void TestContactRatio()
        {
            var cantactRatio = _gear.GetVariable(VariableNames.ContactRatio);
            Assert.That(cantactRatio.Value, Is.EqualTo(1.5111d).Within(0.0001d));
        }

        [Test]
        public void TestGear1Teeth()
        {
            var teeth = _gear.GetVariable(VariableNames.Teethz1);
            Assert.That(teeth.Value, Is.EqualTo(12d).Within(0.0001d));
        }

        [Test]
        public void TestGear2Teeth()
        {
            var teeth = _gear.GetVariable(VariableNames.Teethz2);
            Assert.That(teeth.Value, Is.EqualTo(24d).Within(0.0001d));
        }

        [Test]
        public void TestGear1PitchDiameter()
        {
            var pitchDiameter = _gear.GetVariable(VariableNames.PitchDiameterd1);
            Assert.That(pitchDiameter.Value, Is.EqualTo(36d).Within(0.0001d));
        }

        [Test]
        public void TestGear2PitchDiameter()
        {
            var pitchDiameter = _gear.GetVariable(VariableNames.PitchDiameterd2);
            Assert.That(pitchDiameter.Value, Is.EqualTo(72d).Within(0.0001d));
        }

        [Test]
        public void TestGear1BaseDiameter()
        {
            var pitchDiameter = _gear.GetVariable(VariableNames.BaseDiameterdb1);
            Assert.That(pitchDiameter.Value, Is.EqualTo(33.8289d).Within(0.0001d));
        }

        [Test]
        public void TestGear2BaseDiameter()
        {
            var pitchDiameter = _gear.GetVariable(VariableNames.BaseDiameterdb2);
            Assert.That(pitchDiameter.Value, Is.EqualTo(67.6578d).Within(0.0001d));
        }

        [Test]
        public void TestGear1Addendum()
        {
            var addendum = _gear.GetVariable(VariableNames.Addendumha1);
            Assert.That(addendum.Value, Is.EqualTo(3.0d).Within(0.0001d));
        }

        [Test]
        public void TestGear2Addendum()
        {
            var addendum = _gear.GetVariable(VariableNames.Addendumha2);
            Assert.That(addendum.Value, Is.EqualTo(3.0d).Within(0.0001d));
        }

        [Test]
        public void TestGear1Dedendum()
        {
            var dedendum = _gear.GetVariable(VariableNames.Dedendumhf1);
            Assert.That(dedendum.Value, Is.EqualTo(3.75d).Within(0.0001d));
        }

        [Test]
        public void TestGear2Dedendum()
        {
            var dedendum = _gear.GetVariable(VariableNames.Dedendumhf2);
            Assert.That(dedendum.Value, Is.EqualTo(3.75d).Within(0.0001d));
        }

        [Test]
        public void TestGear1OutsideDiameter()
        {
            var outsideDiameter = _gear.GetVariable(VariableNames.OutsideDiameterda1);
            Assert.That(outsideDiameter.Value, Is.EqualTo(42.0d).Within(0.0001d));
        }

        [Test]
        public void TestGear2OutsideDiameter()
        {
            var outsideDiameter = _gear.GetVariable(VariableNames.OutsideDiameterda2);
            Assert.That(outsideDiameter.Value, Is.EqualTo(78.0d).Within(0.0001d));
        }

        [Test]
        public void TestGear1RootDiameter()
        {
            var rootDiameter = _gear.GetVariable(VariableNames.RootDiameterdf1);
            Assert.That(rootDiameter.Value, Is.EqualTo(28.5d).Within(0.0001d));
        }

        [Test]
        public void TestGear2RootDiameter()
        {
            var rootDiameter = _gear.GetVariable(VariableNames.RootDiameterdf2);
            Assert.That(rootDiameter.Value, Is.EqualTo(64.5d).Within(0.0001d));
        }
    }
}