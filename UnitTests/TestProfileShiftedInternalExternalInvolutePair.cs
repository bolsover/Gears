using System;
using Bolsover.Gears.Calculator;

using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    public class TestProfileShiftedInternalExternalInvolutePair{
    
        
        private Gear _gear;
    

        [SetUp]
        public void SetUp()
        {
            
            _gear = new Gear(GearContext.ProfileShiftedInternalExternalInvolutePair, CalculationMode.Automatic);

        }

        [Test]
        public void TestModule()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Module);
            Assert.That(result, Is.EqualTo(3d).Within(0.001d));
        }

        [Test]
        public void TestPressureAngle()
        {
            var result = _gear.CalculateGearVariable(VariableNames.PressureAngle);
            Assert.That(result * (180.0 / Math.PI), Is.EqualTo(20.0d).Within(0.001d));
        }

        [Test]
        public void TestCentreDistance()
        {
            var result = _gear.CalculateGearVariable(VariableNames.StandardCentreDistance);
            Assert.That(result, Is.EqualTo(12d).Within(0.001d));
        }

        [Test]
        public void TestDifferenceCoefficientOfProfileShift()
        {
            var result = _gear.CalculateGearVariable(VariableNames.DifferenceCoefficientOfProfileShift);
            Assert.That(result, Is.EqualTo(0.38943).Within(0.001d));
        }
        
        [Test]
        public void TestCentreDistanceIncrementFactor()
        {
            var result = _gear.CalculateGearVariable(VariableNames.CentreDistanceIncrementFactor);
            Assert.That(result, Is.EqualTo(0.38943).Within(0.001d));
        }

        [Test]
        public void TestInvoluteFunction()
        {
            var result = _gear.CalculateGearVariable(VariableNames.InvoluteFunction);
            Assert.That(result, Is.EqualTo(0.01490d).Within(0.0001d));
        }
        
        [Test]
        public void TestWorkingPressureAngle()
        {
            var result = _gear.CalculateGearVariable(VariableNames.WorkingPressureAngle);
            Assert.That(result * (180.0 / Math.PI), Is.EqualTo(31.0938d).Within(0.0001d));
        }
        
        [Test]
        public void TestWorkingInvoluteFunction()
        {
            var result = _gear.CalculateGearVariable(VariableNames.WorkingInvoluteFunction);
            Assert.That(result, Is.EqualTo( 0.06040d).Within(0.00001d));
        }
        
        [Test]
        public void TestWholeDepth()
        {
            var result = _gear.CalculateGearVariable(VariableNames.WholeDepth);
            Assert.That(result, Is.EqualTo(6.75).Within(0.00001d));
        }

        [Test]
        public void TestGear1Teeth()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Teethz1);
            Assert.That(result, Is.EqualTo(16d).Within(0.0001d));
        }
        
        [Test]
        public void TestGear2Teeth()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Teethz2);
            Assert.That(result, Is.EqualTo(24d).Within(0.0001d));
        }
        
        [Test]
        public void TestGear1PitchDiameter()
        {
            var result = _gear.CalculateGearVariable(VariableNames.PitchDiameterd1);
            Assert.That(result, Is.EqualTo(48.0d).Within(0.0001d));
        }
        
        [Test]
        public void TestGear2PitchDiameter()
        {
            var result = _gear.CalculateGearVariable(VariableNames.PitchDiameterd2);
            Assert.That(result, Is.EqualTo(72d).Within(0.0001d));
        }
        
        [Test]
        public void TestGear1BaseDiameter()
        {
            var result = _gear.CalculateGearVariable(VariableNames.BaseDiameterdb1);
            Assert.That(result, Is.EqualTo(45.1052d).Within(0.0001d));
        }
        
        [Test]
        public void TestGear2BaseDiameter()
        {
            var result = _gear.CalculateGearVariable(VariableNames.BaseDiameterdb2);
            Assert.That(result, Is.EqualTo(67.6578d).Within(0.0001d));
        }
        
        [Test]
        public void TestGear1Addendum()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Addendumha1);
            Assert.That(result, Is.EqualTo(3.0d).Within(0.0001d));
        }
        
        [Test]
        public void TestGear2Addendum()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Addendumha2);
            Assert.That(result, Is.EqualTo(1.50d).Within(0.0001d));
        }
        
        [Test]
        public void TestGear1Dedendum()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Dedendumhf1);
            Assert.That(result, Is.EqualTo(3.7500d).Within(0.0001d));
        }
        
        [Test]
        public void TestGear2Dedendum()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Dedendumhf2);
            Assert.That(result, Is.EqualTo(5.2500d).Within(0.0001d));
        }
        
        [Test]
        public void TestGear1OutsideDiameter()
        {
            var result = _gear.CalculateGearVariable(VariableNames.OutsideDiameterda1);
            Assert.That(result, Is.EqualTo(54.0d).Within(0.0001d));
        }
        
        [Test]
        public void TestWorkingPitchDiameterGear1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.WorkingPitchDiameterdw1);
            Assert.That(result, Is.EqualTo(52.6732d).Within(0.001d));
        }
        
        [Test]
        public void TestGear2OutsideDiameter()
        {
            var result = _gear.CalculateGearVariable(VariableNames.OutsideDiameterda2);
            Assert.That(result, Is.EqualTo(69.0d).Within(0.0001d));
        }
        
        [Test]
        public void TestGear1RootDiameter()
        {
            var result = _gear.CalculateGearVariable(VariableNames.RootDiameterdf1);
            Assert.That(result, Is.EqualTo(40.5d).Within(0.0001d));
        }
        
        [Test]
        public void TestGear2RootDiameter()
        {
            var result = _gear.CalculateGearVariable(VariableNames.RootDiameterdf2);
            Assert.That(result, Is.EqualTo(82.5d).Within(0.0001d));
        }
        
        [Test]
        public void TestWorkingPitchDiameterGear2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.WorkingPitchDiameterdw2);
            Assert.That(result, Is.EqualTo(79.0098d).Within(0.001d));
        }
        
        [Test]
        public void ContactRatio()
        {
            var result = _gear.CalculateGearVariable(VariableNames.ContactRatio);
            Assert.That(result, Is.EqualTo(1.67950d).Within(0.001d));
        }
    }
}