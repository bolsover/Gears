using System;
using Bolsover.Gears.Calculator;

using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    public class TestProfileShiftedExternalInvolutePair
    {
      
        private Gear _gear;
      

        [SetUp]
        public void SetUp()
        {
         
            _gear = new Gear(GearTypes.ProfileShiftedExternalInvolutePair, CalculationMode.Automatic);
            // _gear.Calculate();
        }
        

        [Test]
        public void TestParentModue()
        {
            double result = _gear.CalculateVariable(VariableIds.Module);
            Assert.That(result, Is.EqualTo(3d).Within(0.001d));
        }
        
        [Test]
        public void TestPressureAngle()
        {
            double result = _gear.CalculateVariable(VariableIds.PressureAngle);
            Assert.That(result, Is.EqualTo(20.0d * (Math.PI / 180.0)).Within(0.001d));
        }
        
        [Test]
        public void TestCentreDistance()
        {
            double result = _gear.CalculateVariable(VariableIds.StandardCentreDistance);
            Assert.That(result, Is.EqualTo(54d).Within(0.001d));
        }
        
        [Test]
        public void TestGear1Teeth()
        {
            double result = _gear.Gears[0].CalculateVariable(VariableIds.Teeth);
            Assert.That(result, Is.EqualTo(12d).Within(0.001d));
          
        }
        
        [Test]
        public void TestGear2Teeth()
        {
            double result = _gear.Gears[1].CalculateVariable(VariableIds.Teeth);
            Assert.That(result, Is.EqualTo(24d).Within(0.001d));
        }
        
        [Test]
        public void ProfileShiftx1()
        {
            double result = _gear.Gears[0].CalculateVariable(VariableIds.CoefficientOfProfileShift);
            Assert.That(result, Is.EqualTo(0.60d).Within(0.001d));
        }
        
        [Test]
        public void ProfileShiftx2()
        {
            double result = _gear.Gears[1].CalculateVariable(VariableIds.CoefficientOfProfileShift);
            Assert.That(result, Is.EqualTo(0.36d).Within(0.001d));
        }
        
        [Test]
        public void WorkingCentreDistance()
        {
            double result = _gear.CalculateVariable(VariableIds.WorkingCentreDistanceax);
            Assert.That(result, Is.EqualTo(56.49989).Within(0.001d));
        }
        
        [Test]
        public void WorkingPressureAngle()
        {
            //Acos((([z1]+[z2])*Cos([alpha]))/(2*(([ax]/[m])-(([z1]+[z2])/2))+[z1]+[z2]))
            //Acos((([z1] + [z2]) * [m] * Cos([alpha])) / (2 * [ax]))
            double result = _gear.CalculateVariable(VariableIds.WorkingPressureAngle);
            Assert.That(result* (180.0 / Math.PI), Is.EqualTo(26.0886d).Within(0.001d));
        }
        
        [Test]
        public void TestCentreDistanceIncrementFactor()
        {
            double result = _gear.CalculateVariable(VariableIds.CentreDistanceIncrementFactor);
            Assert.That(result, Is.EqualTo(0.83329d).Within(0.001d));
        }
        
        [Test]
        public void TestWorkingInvoluteFunction()
        {
            double result = _gear.CalculateVariable(VariableIds.WorkingInvoluteFunction);
            Assert.That(result, Is.EqualTo(0.0343163d).Within(0.001d));
        }
        
        
        [Test]
        public void TestSumCoefficientOfProfileShift()
        {
            double result = _gear.CalculateVariable(VariableIds.SumCoefficientOfProfileShift);
            Assert.That(result, Is.EqualTo(0.9600).Within(0.001d));
        }
        
        
       
        
        [Test]
        public void TestWholeDepth()
        {
            double result = _gear.CalculateVariable(VariableIds.WholeDepth);
            Assert.That(result, Is.EqualTo(6.3698).Within(0.001d));
        }
        
        
        
        [Test]
        public void TestGear1PitchDiameter()
        {
            double result = _gear.Gears[0].CalculateVariable(VariableIds.PitchDiameter);
            Assert.That(result, Is.EqualTo(36d).Within(0.001d));
        }
        
        [Test]
        public void TestGear2PitchDiameter()
        {
            double result = _gear.Gears[1].CalculateVariable(VariableIds.PitchDiameter);
            Assert.That(result, Is.EqualTo(72.0d).Within(0.001d));
        }
        
        [Test]
        public void TestGear1BaseDiameter()
        {
            var result =  _gear.Gears[0].CalculateVariable(VariableIds.BaseDiameter);
            Assert.That(result, Is.EqualTo(33.8289d).Within(0.0001d));
        }
        
        [Test]
        public void TestGear2BaseDiameter()
        {
            var result =  _gear.Gears[1].CalculateVariable(VariableIds.BaseDiameter);
            Assert.That(result, Is.EqualTo(67.6578d).Within(0.0001d));
        }
        
        [Test]
        public void TestGear1WorkingPitchDiameter()
        {
            double result = _gear.Gears[0].CalculateVariable(VariableIds.WorkingPitchDiameter);
            Assert.That(result, Is.EqualTo(37.6665d).Within(0.001d));
        }
        
        [Test]
        public void TestGear2WorkingPitchDiameter()
        {
            double result = _gear.Gears[1].CalculateVariable(VariableIds.WorkingPitchDiameter);
            Assert.That(result, Is.EqualTo(75.333d).Within(0.001d));
        }
        
        [Test]
        public void TestGear1Addendum()
        {
            double result = _gear.Gears[0].CalculateVariable(VariableIds.Addendum);
            Assert.That(result, Is.EqualTo(4.4198d).Within(0.001d));
        }
        
        [Test]
        public void TestGear2Addendum()
        {
            double result = _gear.Gears[1].CalculateVariable(VariableIds.Addendum);
            Assert.That(result, Is.EqualTo(3.69989d).Within(0.001d));
        }
        
        [Test]
        public void TestGear1Dedendum()
        {
            double result = _gear.Gears[0].CalculateVariable(VariableIds.Dedendum);
            Assert.That(result, Is.EqualTo( 1.9500d).Within(0.001d));
        }
        
        [Test]
        public void TestGear2Dedendum()
        {
            double result = _gear.Gears[1].CalculateVariable(VariableIds.Dedendum);
            Assert.That(result, Is.EqualTo(2.6700d).Within(0.001d));
        }
        
        [Test]
        public void TestGear1OutsideDiameter()
        {
            double result = _gear.Gears[0].CalculateVariable(VariableIds.OutsideDiameter);
            Assert.That(result, Is.EqualTo(44.8397d).Within(0.001d));
        }
        
        [Test]
        public void TestGear2OutsideDiameter()
        {
            double result = _gear.Gears[1].CalculateVariable(VariableIds.OutsideDiameter);
            Assert.That(result, Is.EqualTo(79.3997d).Within(0.001d));
        }
        
        [Test]
        public void TestGear1RootDiameter()
        {
            double result = _gear.Gears[0].CalculateVariable(VariableIds.RootDiameter);
            Assert.That(result, Is.EqualTo(32.0999d).Within(0.001d));
        }
        
        [Test]
        public void TestGear2RootDiameter()
        {
            double result = _gear.Gears[1].CalculateVariable(VariableIds.RootDiameter);
            Assert.That(result, Is.EqualTo(66.6599d).Within(0.001d));
        }
        
        [Test]
        public void TestContactRatio()
        {
            double result = _gear.CalculateVariable(VariableIds.ContactRatio);
            Assert.That(result, Is.EqualTo(1.20210d).Within(0.001d));
        }
        
        
    }
}