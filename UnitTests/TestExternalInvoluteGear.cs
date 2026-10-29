using System;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class TestExternalInvoluteGear
    {
        private Gear _gear;

        [SetUp]
        public void SetUp()
        {
            _gear = new Gear(GearTypes.ExternalInvolute, CalculationMode.Automatic);
            _gear.Calculate();
        }


        [Test]
        public void TestModule()
        {
            GearVariable module = _gear.GetVariable(VariableIds.Module);
            Assert.That(module.Value, Is.EqualTo(3d).Within(0.001d));
        }

        [Test]
        public void TestTeeth()
        {
            GearVariable teeth = _gear.GetVariable(VariableIds.Teeth);
            Assert.That(teeth.Value, Is.EqualTo(24d).Within(0.001d));
        }

        [Test]
        public void TestPressureAngle()
        {
            GearVariable pressureAngle = _gear.GetVariable(VariableIds.PressureAngle);
            Assert.That(pressureAngle.Value * (180.0 / Math.PI), Is.EqualTo(20d).Within(0.001d));
        }

        [Test]
        public void TestPitchDiameter()
        {
            GearVariable pitchDiameter = _gear.GetVariable(VariableIds.PitchDiameter);
            Assert.That(pitchDiameter.Value, Is.EqualTo(72d).Within(0.001d));
        }

        [Test]
        public void TestBaseDiameter()
        {
            var variable = _gear.GetVariable(VariableIds.BaseDiameter);
            Assert.That(variable.Value, Is.EqualTo(67.6578d).Within(0.001d));
        }

        [Test]
        public void TestAddendum()
        {
            var variable = _gear.GetVariable(VariableIds.Addendum);
            Assert.That(variable.Value, Is.EqualTo(3d).Within(0.001d));
        }

        [Test]
        public void TestDedendum()
        {
            var variable = _gear.GetVariable(VariableIds.Dedendum);
            Assert.That(variable.Value, Is.EqualTo(3.75d).Within(0.001d));
        }

        [Test]
        public void TestOutsideDiameter()
        {
            var variable = _gear.GetVariable(VariableIds.OutsideDiameter);
            Assert.That(variable.Value, Is.EqualTo(78.0d).Within(0.001d));
        }

        [Test]
        public void TestRootDiameter()
        {
            var variable = _gear.GetVariable(VariableIds.RootDiameter);
            Assert.That(variable.Value, Is.EqualTo(64.50d).Within(0.001d));
        }

        [Test]
        public void TestVariableChange()
        {
            _gear.GetVariable(VariableIds.Module).Value = 2;
            var variable = _gear.GetVariable(VariableIds.PitchDiameter);
            Assert.That(variable.Value, Is.EqualTo(48d).Within(0.001d));
        }
    }
}