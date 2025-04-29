using System;
using Bolsover.Gears.Calculator;

using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    public class TestExternalRadialHelicalGear
   {

        private Gear _gear;


        [SetUp]
        public void SetUp()
        {

            _gear = new Gear(GearContext.ExternalRadialHelical, CalculationMode.Automatic);
          //  _gear.Calculate();

        }

        [Test]
        public void TestRadialModule()
        {
            var result = _gear.CalculateGearVariable(VariableNames.RadialModule);
            Assert.That(result, Is.EqualTo(3d).Within(0.001d));
        }
        
        [Test]
        public void TestTeeth()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Teeth);
            Assert.That(result, Is.EqualTo(60d).Within(0.001d));
        }
        
        [Test]
        public void TestRadialPressureAngle()
        {
            var result = _gear.CalculateGearVariable(VariableNames.RadialPressureAngle);
            Assert.That(result* (180.0 / Math.PI), Is.EqualTo(20d).Within(0.001d));
        }
        
        [Test]
        public void TestPitchDiameter()
        {
            var result = _gear.CalculateGearVariable(VariableNames.PitchDiameter);
            Assert.That(result, Is.EqualTo(180.0d).Within(0.001d));
        }
        
        [Test]
        public void TestBaseDiameter()
        {
            var result = _gear.CalculateGearVariable(VariableNames.BaseDiameter);
            Assert.That(result, Is.EqualTo(169.1446d).Within(0.001d));
        }
        
        [Test]
        public void TestAddendum()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Addendum);
            Assert.That(result, Is.EqualTo(3d).Within(0.001d));
        }
        
        [Test]
        public void TestDedendum()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Dedendum);
            Assert.That(result, Is.EqualTo(3.75d).Within(0.001d));
        }
        
        [Test]
        public void TestOutsideDiameter()
        {
            var result = _gear.CalculateGearVariable(VariableNames.OutsideDiameter);
            Assert.That(result, Is.EqualTo(186.0d).Within(0.001d));
        }
        
        [Test]
        public void TestRootDiameter()
        {
            var result = _gear.CalculateGearVariable(VariableNames.RootDiameter);
            Assert.That(result, Is.EqualTo(172.5d).Within(0.001d));
        }
        
        
        
        [Test]
        public void TestWholeDepth()
        {
            var result = _gear.CalculateGearVariable(VariableNames.WholeDepth);
            Assert.That(result, Is.EqualTo(6.75d).Within(0.001d));
        }
        
        [Test]
        public void TestVariableChange()
        {
            _gear.GetVariable(VariableNames.RadialModule).Value = 2;
           
            var result = _gear.CalculateGearVariable(VariableNames.PitchDiameter);
            Assert.That(result, Is.EqualTo(120.000d).Within(0.001d));
        }
        
        
    }
}