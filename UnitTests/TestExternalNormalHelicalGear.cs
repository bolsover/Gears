using System;
using Bolsover.Gears.Calculator;

using Bolsover.Gears.Model;
using NUnit.Framework;


namespace UnitTests
{
    public class TestExternalNormalHelicalGear
     {
         public static double Degrees(double radians)
         {
             return radians * (180.0 / Math.PI);
         }
       
        private Gear _gear;
      
        [SetUp]
        public void SetUp()
        {
            
            _gear = new Gear(GearContext.ExternalNormalHelical, CalculationMode.Automatic);
  
            _gear.Calculate();

        }
        

        
        [Test]
        public void TestNormalModule()
        {
            GearVariable module = _gear.GetVariable(VariableNames.NormalModule);
            Assert.That(module.Value, Is.EqualTo(3d).Within(0.001d));
        }
        
        [Test]
        public void TestTeeth()
        {
            GearVariable teeth = _gear.GetVariable(VariableNames.Teeth);
            Assert.That(teeth.Value, Is.EqualTo(60d).Within(0.001d));
        }
        
        [Test]
        public void TestNormalPressureAngle()
        {
            GearVariable pressureAngle =_gear.GetVariable(VariableNames.NormalPressureAngle);
            Assert.That(Degrees(pressureAngle.Value), Is.EqualTo(20d).Within(0.001d));
        }
        
        [Test]
        public void TestPitchDiameter()
        {
            GearVariable pitchDiameter = _gear.GetVariable(VariableNames.PitchDiameter);
            Assert.That(pitchDiameter.Value, Is.EqualTo(207.846d).Within(0.001d));
        }
        
        [Test]
        public void TestBaseDiameter()
        {
            var variable = _gear.GetVariable(VariableNames.BaseDiameter);
            Assert.That(variable.Value, Is.EqualTo(191.61145d).Within(0.001d));
        }
        
        [Test]
        public void TestAddendum()
        {
            var variable  = _gear.GetVariable(VariableNames.Addendum);
            Assert.That(variable.Value, Is.EqualTo(3d).Within(0.001d));
        }
        
        [Test]
        public void TestDedendum()
        {
            var variable  = _gear.GetVariable(VariableNames.Dedendum);
            Assert.That(variable.Value, Is.EqualTo(3.75d).Within(0.001d));
        }
        
        [Test]
        public void TestOutsideDiameter()
        {
            var variable  = _gear.GetVariable(VariableNames.OutsideDiameter);
            Assert.That(variable.Value, Is.EqualTo(213.846d).Within(0.001d));
        }
        
        [Test]
        public void TestRootDiameter()
        {
            var variable = _gear.GetVariable(VariableNames.RootDiameter);
            Assert.That(variable.Value, Is.EqualTo(200.346d).Within(0.001d));
        }
        
        [Test]
        public void TestRadialPressureAngle()
        {
            var variable =  _gear.GetVariable(VariableNames.RadialPressureAngle);
           Assert.That(variable.Value, Is.EqualTo(22.79587725895782d * (Math.PI / 180.0)).Within(0.001d));
        }
        
        [Test]
        public void TestWholeDepth()
        {
            var variable =  _gear.GetVariable(VariableNames.WholeDepth);
            Assert.That(variable.Value, Is.EqualTo(6.75d).Within(0.001d));
        }
        
        [Test]
        public void TestVariableChange()
        {
            _gear.GetVariable(VariableNames.NormalModule).Value = 2;
            _gear.Calculate();
            var variable  = _gear.GetVariable(VariableNames.PitchDiameter);
            Assert.That(variable.Value, Is.EqualTo(138.564d).Within(0.001d));
        }
        
        
    }
}