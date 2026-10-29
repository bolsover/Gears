using System;
using Bolsover.Gears.Calculator;

using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    public class TestProfileShiftedExternalNormalHelicalPair
    {
        
       
        private Gear _gear;
       

        [SetUp]
        public void SetUp()
        {
           
            _gear = new Gear(GearTypes.ProfileShiftedExternalNormalHelicalPair, CalculationMode.Automatic);
          //  _gear.Calculate();
        }

        [Test]
        public void TestParentNormalModue()
        {
            var normalModule = _gear. CalculateVariable(VariableIds.NormalModule);
            Assert.That(normalModule , Is.EqualTo(3d).Within(0.001d));
        }

        [Test]
        public void TestParentPressureAngle()
        {
            var normalPressureAngle = _gear. CalculateVariable(VariableIds.NormalPressureAngle);
            Assert.That(normalPressureAngle , Is.EqualTo(20.0d  * (Math.PI / 180.0)).Within(0.001d));
        }
        
        [Test]
        public void TestRadialPressureAngle()
        {
            var radialPressureAngle = _gear. CalculateVariable(VariableIds.RadialPressureAngle);
            Assert.That(radialPressureAngle , Is.EqualTo(22.795877d  * (Math.PI / 180.0)).Within(0.0001d));
        }
        
        [Test]
        public void TestRadialWorkingPressureAngle()
        {
            var radialWorkingPressureAngle = _gear. CalculateVariable(VariableIds.RadialWorkingPressureAngle);
            Assert.That(radialWorkingPressureAngle , Is.EqualTo(23.11263d * (Math.PI / 180.0)).Within(0.0001d));
        }

        [Test]
        public void TestStandardCentreDistance()
        {
            var standardCentreDistance = _gear. CalculateVariable(VariableIds.StandardCentreDistance);
            Assert.That(standardCentreDistance , Is.EqualTo(124.7076d).Within(0.001d));
        }
        
        [Test]
        public void TestWorkingCentreDistance()
        {
            var standardCentreDistance = _gear. CalculateVariable(VariableIds.WorkingCentreDistanceax);
            Assert.That(standardCentreDistance , Is.EqualTo(125d).Within(0.001d));
        }
        
        [Test]
        public void TestCentreDistanceIncrementFactor()
        {
            var centreDistanceIncrementFactor = _gear. CalculateVariable(VariableIds.CentreDistanceIncrementFactor);
            Assert.That(centreDistanceIncrementFactor , Is.EqualTo(0.097447d).Within(0.001d));
        }
        
        [Test]
        public void TestInvoluteFunction()
        {
            var involuteFunction = _gear. CalculateVariable(VariableIds.InvoluteFunction);
            Assert.That(involuteFunction , Is.EqualTo(0.01490d).Within(0.001d));
        }
        
       
        
        [Test]
        public void TestRadialWorkingInvoluteFunction()
        {
            var radialWorkingInvoluteFunction = _gear. CalculateVariable(VariableIds.RadialWorkingInvoluteFunction);
            Assert.That(radialWorkingInvoluteFunction , Is.EqualTo(0.023405d).Within(0.001d));
        }
        
        [Test]
        public void SumNormalCoefficientOfProfileShift()
        {
            var sumNormalCoefficientOfProfileShift = _gear. CalculateVariable(VariableIds.SumNormalCoefficientOfProfileShift);
            Assert.That(sumNormalCoefficientOfProfileShift , Is.EqualTo(0.098089d).Within(0.001d));
        }
        
        [Test]
        public void WholeDepth()
        {
            var wholeDepth = _gear. CalculateVariable(VariableIds.WholeDepth);
            Assert.That(wholeDepth , Is.EqualTo(6.7480d).Within(0.001d));
        }
        
        [Test]
        public void TeethGear1()
        {
            var teeth = _gear.Gears[0].CalculateVariable(VariableIds.Teeth);
            Assert.That(teeth , Is.EqualTo(12.0d).Within(0.001d));
        }
        
        [Test]
        public void HandGear1()
        {
            var hand = _gear.Gears[0].CalculateVariable(VariableIds.Hand);
            Assert.That(hand , Is.EqualTo(0d).Within(0.001d));
        }
        
        [Test]
        public void NormalCoefficientOfProfileShiftGear1()
        {
            var normalCoefficientOfProfileShift = _gear.Gears[0].CalculateVariable(VariableIds.NormalCoefficientOfProfileShift);
            Assert.That(normalCoefficientOfProfileShift , Is.EqualTo(0.09808d).Within(0.001d));
        }
        
        [Test]
        public void PitchDiameterGear1()
        {
            var pitchDiameter = _gear.Gears[0].CalculateVariable(VariableIds.PitchDiameter);
            Assert.That(pitchDiameter , Is.EqualTo(41.569d).Within(0.001d));
        }
        
        [Test]
        public void WorkingPitchDiameterGear1()
        {
            var workingPitchDiameter = _gear.Gears[0].CalculateVariable(VariableIds.WorkingPitchDiameter);
            Assert.That(workingPitchDiameter , Is.EqualTo(41.6666d).Within(0.001d));
        }
        
        [Test]
        public void BaseDiameterGear1()
        {
            var baseDiameter = _gear.Gears[0].CalculateVariable(VariableIds.BaseDiameter);
            Assert.That(baseDiameter , Is.EqualTo(38.322d).Within(0.001d));
        }
        
        [Test]
        public void Addendum1()
        {
            var addendum = _gear.Gears[0].CalculateVariable(VariableIds.Addendum);
            Assert.That(addendum , Is.EqualTo(3.2923d).Within(0.001d));
        }
        
        [Test]
        public void Dedendum1()
        {
            var dedendum = _gear.Gears[0].CalculateVariable(VariableIds.Dedendum);
            Assert.That(dedendum , Is.EqualTo(3.4557d).Within(0.001d));
        }
        
        [Test]
        public void OutsideDiameter1()
        {
            var outsideDiameter = _gear.Gears[0].CalculateVariable(VariableIds.OutsideDiameter);
            Assert.That(outsideDiameter , Is.EqualTo(48.1539d).Within(0.001d));
        }
        
         
        [Test]
        public void RootDiameter1()
        {
            var rootDiameter = _gear.Gears[0].CalculateVariable(VariableIds.RootDiameter);
            Assert.That(rootDiameter , Is.EqualTo(34.6577d).Within(0.001d));
        }
        
        
        [Test]
        public void TeethGear2()
        {
            var teeth = _gear.Gears[1].CalculateVariable(VariableIds.Teeth);
            Assert.That(teeth , Is.EqualTo(60.0d).Within(0.001d));
        }
        
        [Test]
        public void HandGear2()
        {
            var hand = _gear.Gears[1].CalculateVariable(VariableIds.Hand);
            Assert.That(hand , Is.EqualTo(1d).Within(0.001d));
        }
        
        [Test]
        public void NormalCoefficientOfProfileShiftGear2()
        {
            var normalCoefficientOfProfileShift = _gear.Gears[1].CalculateVariable(VariableIds.NormalCoefficientOfProfileShift);
            Assert.That(normalCoefficientOfProfileShift , Is.EqualTo(0d).Within(0.001d));
        }
        
        [Test]
        public void PitchDiameterGear2()
        {
            var pitchDiameter = _gear.Gears[1].CalculateVariable(VariableIds.PitchDiameter);
            Assert.That(pitchDiameter , Is.EqualTo(207.846d).Within(0.001d));
        }
        
        [Test]
        public void WorkingPitchDiameterGear2()
        {
            var workingPitchDiameter = _gear.Gears[1].CalculateVariable(VariableIds.WorkingPitchDiameter);
            Assert.That(workingPitchDiameter , Is.EqualTo(208.333d).Within(0.001d));
        }
        
        [Test]
        public void BaseDiameterGear2()
        {
            var baseDiameter = _gear.Gears[1].CalculateVariable(VariableIds.BaseDiameter);
            Assert.That(baseDiameter , Is.EqualTo(191.611d).Within(0.001d));
        }
        
        [Test]
        public void Addendum2()
        {
            var addendum = _gear.Gears[1].CalculateVariable(VariableIds.Addendum);
            Assert.That(addendum , Is.EqualTo(2.998d).Within(0.001d));
        }
        
        [Test]
        public void Dedendum2()
        {
            var dedendum = _gear.Gears[1].CalculateVariable(VariableIds.Dedendum);
            Assert.That(dedendum , Is.EqualTo(3.75d).Within(0.001d));
        }
        
        [Test]
        public void OutsideDiameter2()
        {
            var outsideDiameter = _gear.Gears[1].CalculateVariable(VariableIds.OutsideDiameter);
            Assert.That(outsideDiameter , Is.EqualTo(213.8422d).Within(0.001d));
        }
        
        [Test]
        public void RootDiameter2()
        {
            var rootDiameter = _gear.Gears[1].CalculateVariable(VariableIds.RootDiameter);
            Assert.That(rootDiameter , Is.EqualTo(200.346d).Within(0.001d));
        }
        
        // [Test]
        // public void TestVariableChange()
        // {
        //     var variable = _gear. CalculateVariable(VariableIds.NormalModule)  = 30;
        //     var variable = _gear. CalculateVariable(VariableIds.PitchDiameter );
        //     Assert.That(variable , Is.EqualTo(415.6921d).Within(0.001d));
        // }
    }
}