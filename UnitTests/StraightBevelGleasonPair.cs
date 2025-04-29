using System;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class StraightBevelGleasonPair
    {
        private Gear _gear;

        [SetUp]
        public void SetUp()
        {
            _gear = new Gear(GearContext.StraightBevelGleasonPair, CalculationMode.Automatic);
           
        }
        [Test]
        public void Sigma()
        {
            var result = _gear.CalculateGearVariable(VariableNames.ShaftAngle);
            Assert.That(result * (180.0 / Math.PI), Is.EqualTo(90d).Within(0.001d));
        }

        [Test]
        public void M()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Module);
            Assert.That(result, Is.EqualTo(3d).Within(0.001d));
        }
        
        [Test]
        public void Alpha()
        {
            var result = _gear.CalculateGearVariable(VariableNames.PressureAngle);
            Assert.That(result * (180.0 / Math.PI), Is.EqualTo(20d).Within(0.001d));
        }
        
        [Test]
        public void Z1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Teethz1);
            Assert.That(result , Is.EqualTo(20d).Within(0.001d));
        }
        
        
        [Test]
        public void Z2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Teethz2);
            Assert.That(result , Is.EqualTo(40d).Within(0.001d));
        }
        
        [Test]
        public void D1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.PitchDiameterd1);
            Assert.That(result , Is.EqualTo(60d).Within(0.001d));
        }
        [Test]
        public void D2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.PitchDiameterd2);
            Assert.That(result , Is.EqualTo(120d).Within(0.001d));
        }
        [Test]
        public void Sigma1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.PitchConeAngledelta1);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(26.56505d).Within(0.001d));
        }
        [Test]
        public void Sigma2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.PitchConeAngledelta2);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(63.43494d).Within(0.001d));
        }
        [Test]
        public void Re()
        {
            var result = _gear.CalculateGearVariable(VariableNames.ConeDistance);
            Assert.That(result , Is.EqualTo(67.08203d).Within(0.001d));
        }
        [Test]
        public void B()
        {
            var result = _gear.CalculateGearVariable(VariableNames.FaceWidth);
            Assert.That(result , Is.EqualTo(22d).Within(0.001d));
        }
        [Test]
        public void Ha1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Addendumha1);
            Assert.That(result , Is.EqualTo(4.0350d).Within(0.001d));
        }
        [Test]
        public void Ha2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Addendumha2);
            Assert.That(result , Is.EqualTo(1.9649d).Within(0.001d));
        }
        [Test]
        public void Hf1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Dedendumhf1);
            Assert.That(result , Is.EqualTo(2.52899d).Within(0.001d));
        }
        [Test]
        public void Hf2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.Dedendumhf2);
            Assert.That(result , Is.EqualTo(4.5990d).Within(0.001d));
        }
        [Test]
        public void Thetaf1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.DedendumAnglef1);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(2.1590d).Within(0.001d));
        }
        [Test]
        public void Thetaf2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.DedendumAnglef2);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(3.92193d).Within(0.001d));
        }
        [Test]
        public void Thetaa1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.AddendumAnglea1);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(3.92193d).Within(0.001d));
        }
        [Test]
        public void Thetaa2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.AddendumAnglea2);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(2.1590d).Within(0.001d));
        }
        [Test]
        public void Deltaa1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.OuterConeAngledeltaa1);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(30.48698d).Within(0.001d));
        }
        [Test]
        public void Deltaa2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.OuterConeAngledeltaa2);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(65.5939d).Within(0.001d));
        }
        [Test]
        public void Deltaf1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.RootConeAngledeltaf1);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(24.40601d).Within(0.001d));
        }
        [Test]
        public void Deltaf2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.RootConeAngledeltaf2);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(59.51301d).Within(0.001d));
        }
        [Test]
        public void Da1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.OutsideDiameterda1);
            Assert.That(result , Is.EqualTo(67.21802d).Within(0.001d));
        }
        [Test]
        public void Da2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.OutsideDiameterda2);
            Assert.That(result , Is.EqualTo(121.75754d).Within(0.001d));
        }
        [Test]
        public void X1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.PitchApexToCrownX1);
            Assert.That(result , Is.EqualTo(58.19549d).Within(0.001d));
        }
        [Test]
        public void X2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.PitchApexToCrownX2);
            Assert.That(result , Is.EqualTo(28.24245d).Within(0.001d));
        }
        [Test]
        public void Xb1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.AxialFaceWidthXb1);
            Assert.That(result , Is.EqualTo(19.00287d).Within(0.001d));
        }
        [Test]
        public void Xb2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.AxialFaceWidthXb2);
            Assert.That(result , Is.EqualTo(9.096859d).Within(0.001d));
        }
        [Test]
        public void Di1()
        {
            var result = _gear.CalculateGearVariable(VariableNames.InnerOutsideDiameterdi1);
            Assert.That(result , Is.EqualTo(44.84254d).Within(0.001d));
        }
        [Test]
        public void Di2()
        {
            var result = _gear.CalculateGearVariable(VariableNames.InnerOutsideDiameterdi2);
            Assert.That(result , Is.EqualTo(81.66091d).Within(0.001d));
        }
        
    }
}