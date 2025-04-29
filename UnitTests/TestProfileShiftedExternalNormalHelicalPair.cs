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
           
            _gear = new Gear(GearContext.ProfileShiftedExternalNormalHelicalPair, CalculationMode.Automatic);
            _gear.Calculate();
        }

        [Test]
        public void TestParentNormalModue()
        {
            var normalModule = _gear.GetVariable(VariableNames.NormalModule);
            Assert.That(normalModule.Value, Is.EqualTo(3d).Within(0.001d));
        }

        [Test]
        public void TestParentPressureAngle()
        {
            var normalPressureAngle = _gear.GetVariable(VariableNames.NormalPressureAngle);
            Assert.That(normalPressureAngle.Value, Is.EqualTo(20.0d  * (Math.PI / 180.0)).Within(0.001d));
        }
        
        [Test]
        public void TestRadialPressureAngle()
        {
            var radialPressureAngle = _gear.GetVariable(VariableNames.RadialPressureAngle);
            Assert.That(radialPressureAngle.Value, Is.EqualTo(22.795877d  * (Math.PI / 180.0)).Within(0.0001d));
        }
        
        [Test]
        public void TestRadialWorkingPressureAngle()
        {
            var radialWorkingPressureAngle = _gear.GetVariable(VariableNames.RadialWorkingPressureAngle);
            Assert.That(radialWorkingPressureAngle.Value, Is.EqualTo(23.11263d * (Math.PI / 180.0)).Within(0.0001d));
        }

        [Test]
        public void TestStandardCentreDistance()
        {
            var standardCentreDistance = _gear.GetVariable(VariableNames.StandardCentreDistance);
            Assert.That(standardCentreDistance.Value, Is.EqualTo(124.7076d).Within(0.001d));
        }
        
        [Test]
        public void TestWorkingCentreDistance()
        {
            var standardCentreDistance = _gear.GetVariable(VariableNames.WorkingCentreDistanceax);
            Assert.That(standardCentreDistance.Value, Is.EqualTo(125d).Within(0.001d));
        }
        
        [Test]
        public void TestCentreDistanceIncrementFactor()
        {
            var centreDistanceIncrementFactor = _gear.GetVariable(VariableNames.CentreDistanceIncrementFactor);
            Assert.That(centreDistanceIncrementFactor.Value, Is.EqualTo(0.097447d).Within(0.001d));
        }
        
        [Test]
        public void TestInvoluteFunction()
        {
            var involuteFunction = _gear.GetVariable(VariableNames.InvoluteFunction);
            Assert.That(involuteFunction.Value, Is.EqualTo(0.01490d).Within(0.001d));
        }
        
       
        
        [Test]
        public void TestRadialWorkingInvoluteFunction()
        {
            var radialWorkingInvoluteFunction = _gear.GetVariable(VariableNames.RadialWorkingInvoluteFunction);
            Assert.That(radialWorkingInvoluteFunction.Value, Is.EqualTo(0.023405d).Within(0.001d));
        }
        
        [Test]
        public void SumNormalCoefficientOfProfileShift()
        {
            var sumNormalCoefficientOfProfileShift = _gear.GetVariable(VariableNames.SumNormalCoefficientOfProfileShift);
            Assert.That(sumNormalCoefficientOfProfileShift.Value, Is.EqualTo(0.098089d).Within(0.001d));
        }
        
        [Test]
        public void WholeDepth()
        {
            var wholeDepth = _gear.GetVariable(VariableNames.WholeDepth);
            Assert.That(wholeDepth.Value, Is.EqualTo(6.7480d).Within(0.001d));
        }
        
        [Test]
        public void TeethGear1()
        {
            var teeth = _gear.GetVariable(VariableNames.Teethz1);
            Assert.That(teeth.Value, Is.EqualTo(12.0d).Within(0.001d));
        }
        
        [Test]
        public void HandGear1()
        {
            var hand = _gear.GetVariable(VariableNames.Hand1);
            Assert.That(hand.Value, Is.EqualTo(0d).Within(0.001d));
        }
        
        [Test]
        public void NormalCoefficientOfProfileShiftGear1()
        {
            var normalCoefficientOfProfileShift = _gear.GetVariable(VariableNames.NormalCoefficientOfProfileShiftxn1);
            Assert.That(normalCoefficientOfProfileShift.Value, Is.EqualTo(0.09808d).Within(0.001d));
        }
        
        [Test]
        public void PitchDiameterGear1()
        {
            var pitchDiameter = _gear.GetVariable(VariableNames.PitchDiameterd1);
            Assert.That(pitchDiameter.Value, Is.EqualTo(41.569d).Within(0.001d));
        }
        
        [Test]
        public void WorkingPitchDiameterGear1()
        {
            var workingPitchDiameter = _gear.GetVariable(VariableNames.WorkingPitchDiameterdw1);
            Assert.That(workingPitchDiameter.Value, Is.EqualTo(41.6666d).Within(0.001d));
        }
        
        [Test]
        public void BaseDiameterGear1()
        {
            var baseDiameter = _gear.GetVariable(VariableNames.BaseDiameterdb1);
            Assert.That(baseDiameter.Value, Is.EqualTo(38.322d).Within(0.001d));
        }
        
        [Test]
        public void Addendum1()
        {
            var addendum = _gear.GetVariable(VariableNames.Addendumha1);
            Assert.That(addendum.Value, Is.EqualTo(3.2923d).Within(0.001d));
        }
        
        [Test]
        public void Dedendum1()
        {
            var dedendum = _gear.GetVariable(VariableNames.Dedendumhf1);
            Assert.That(dedendum.Value, Is.EqualTo(3.4557d).Within(0.001d));
        }
        
        [Test]
        public void OutsideDiameter1()
        {
            var outsideDiameter = _gear.GetVariable(VariableNames.OutsideDiameterda1);
            Assert.That(outsideDiameter.Value, Is.EqualTo(48.1539d).Within(0.001d));
        }
        
         
        [Test]
        public void RootDiameter1()
        {
            var rootDiameter = _gear.GetVariable(VariableNames.RootDiameterdf1);
            Assert.That(rootDiameter.Value, Is.EqualTo(34.6577d).Within(0.001d));
        }
        
        
        [Test]
        public void TeethGear2()
        {
            var teeth = _gear.GetVariable(VariableNames.Teethz2);
            Assert.That(teeth.Value, Is.EqualTo(60.0d).Within(0.001d));
        }
        
        [Test]
        public void HandGear2()
        {
            var hand = _gear.GetVariable(VariableNames.Hand2);
            Assert.That(hand.Value, Is.EqualTo(1d).Within(0.001d));
        }
        
        [Test]
        public void NormalCoefficientOfProfileShiftGear2()
        {
            var normalCoefficientOfProfileShift = _gear.GetVariable(VariableNames.NormalCoefficientOfProfileShiftxn2);
            Assert.That(normalCoefficientOfProfileShift.Value, Is.EqualTo(0d).Within(0.001d));
        }
        
        [Test]
        public void PitchDiameterGear2()
        {
            var pitchDiameter = _gear.GetVariable(VariableNames.PitchDiameterd2);
            Assert.That(pitchDiameter.Value, Is.EqualTo(207.846d).Within(0.001d));
        }
        
        [Test]
        public void WorkingPitchDiameterGear2()
        {
            var workingPitchDiameter = _gear.GetVariable(VariableNames.WorkingPitchDiameterdw2);
            Assert.That(workingPitchDiameter.Value, Is.EqualTo(208.333d).Within(0.001d));
        }
        
        [Test]
        public void BaseDiameterGear2()
        {
            var baseDiameter = _gear.GetVariable(VariableNames.BaseDiameterdb2);
            Assert.That(baseDiameter.Value, Is.EqualTo(191.611d).Within(0.001d));
        }
        
        [Test]
        public void Addendum2()
        {
            var addendum = _gear.GetVariable(VariableNames.Addendumha2);
            Assert.That(addendum.Value, Is.EqualTo(2.998d).Within(0.001d));
        }
        
        [Test]
        public void Dedendum2()
        {
            var dedendum = _gear.GetVariable(VariableNames.Dedendumhf2);
            Assert.That(dedendum.Value, Is.EqualTo(3.75d).Within(0.001d));
        }
        
        [Test]
        public void OutsideDiameter2()
        {
            var outsideDiameter = _gear.GetVariable(VariableNames.OutsideDiameterda2);
            Assert.That(outsideDiameter.Value, Is.EqualTo(213.8422d).Within(0.001d));
        }
        
        [Test]
        public void RootDiameter2()
        {
            var rootDiameter = _gear.GetVariable(VariableNames.RootDiameterdf2);
            Assert.That(rootDiameter.Value, Is.EqualTo(200.346d).Within(0.001d));
        }
        
        [Test]
        public void TestVariableChange()
        {
            _gear.GetVariable(VariableNames.NormalModule).Value = 30;
            var variable = _gear.GetVariable(VariableNames.PitchDiameterd1 );
            Assert.That(variable.Value, Is.EqualTo(415.6921d).Within(0.001d));
        }
    }
}