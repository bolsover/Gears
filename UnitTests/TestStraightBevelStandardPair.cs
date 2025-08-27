using System;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class TestStraightBevelStandardPair
    {
        private Gear _gear;

        [SetUp]
        public void SetUp()
        {
            _gear = new Gear(GearContext.StraightBevelStandardPair, CalculationMode.Automatic);
           
        }
        [Test]
        public void Sigma()
        {
            var result = _gear.CalculateVariable(VariableNames.ShaftAngle);
            Assert.That(result * (180.0 / Math.PI), Is.EqualTo(90d).Within(0.001d));
        }

        [Test]
        public void M()
        {
            var result = _gear.CalculateVariable(VariableNames.Module);
            Assert.That(result, Is.EqualTo(3d).Within(0.001d));
        }
        
        [Test]
        public void Alpha()
        {
            var result = _gear.CalculateVariable(VariableNames.PressureAngle);
            Assert.That(result * (180.0 / Math.PI), Is.EqualTo(20d).Within(0.001d));
        }
        
        [Test]
        public void Z1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.Teeth);
            Assert.That(result , Is.EqualTo(20d).Within(0.001d));
        }
        
        
        [Test]
        public void Z2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.Teeth);
            Assert.That(result , Is.EqualTo(40d).Within(0.001d));
        }
        
        [Test]
        public void D1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.PitchDiameter);
            Assert.That(result , Is.EqualTo(60d).Within(0.001d));
        }
        [Test]
        public void D2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.PitchDiameter);
            Assert.That(result , Is.EqualTo(120d).Within(0.001d));
        }
        [Test]
        public void Sigma1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.PitchConeAngle);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(26.56505d).Within(0.001d));
        }
        [Test]
        public void Sigma2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.PitchConeAngle);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(63.43494d).Within(0.001d));
        }
        [Test]
        public void Re()
        {
            var result = _gear.CalculateVariable(VariableNames.ConeDistance);
            Assert.That(result , Is.EqualTo(67.08203d).Within(0.001d));
        }
        [Test]
        public void B()
        {
            var result = _gear.CalculateVariable(VariableNames.FaceWidth);
            Assert.That(result , Is.EqualTo(22d).Within(0.001d));
        }
        [Test]
        public void Ha1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.Addendum);
            Assert.That(result , Is.EqualTo(3.0d).Within(0.001d));
        }
        [Test]
        public void Ha2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.Addendum);
            Assert.That(result , Is.EqualTo(3.0d).Within(0.001d));
        }
        [Test]
        public void Hf1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.Dedendum);
            Assert.That(result , Is.EqualTo(3.75d).Within(0.001d));
        }
        [Test]
        public void Hf2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.Dedendum);
            Assert.That(result , Is.EqualTo(3.75d).Within(0.001d));
        }
        [Test]
        public void Thetaf1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.DedendumAngle);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(3.19960d).Within(0.001d));
        }
        [Test]
        public void Thetaf2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.DedendumAngle);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(3.19960d).Within(0.001d));
        }
        [Test]
        public void Thetaa1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.AddendumAngle);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(2.56063d).Within(0.001d));
        }
        [Test]
        public void Thetaa2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.AddendumAngle);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(2.56063d).Within(0.001d));
        }
        [Test]
        public void Deltaa1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.OuterConeAngle);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(29.12569d).Within(0.001d));
        }
        [Test]
        public void Deltaa2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.OuterConeAngle);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(65.99558d).Within(0.001d));
        }
        [Test]
        public void Deltaf1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.RootConeAngle);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(23.36544d).Within(0.001d));
        }
        [Test]
        public void Deltaf2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.RootConeAngle);
            Assert.That(result * (180.0 / Math.PI) , Is.EqualTo(60.23534d).Within(0.001d));
        }
        [Test]
        public void Da1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.OutsideDiameter);
            Assert.That(result , Is.EqualTo(65.36656d).Within(0.001d));
        }
        [Test]
        public void Da2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.OutsideDiameter);
            Assert.That(result , Is.EqualTo(122.68328d).Within(0.001d));
        }
        [Test]
        public void X1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.PitchApexToCrown);
            Assert.That(result , Is.EqualTo(58.65835d).Within(0.001d));
        }
        [Test]
        public void X2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.PitchApexToCrown);
            Assert.That(result , Is.EqualTo(27.31671d).Within(0.001d));
        }
        [Test]
        public void Xb1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.AxialFaceWidth);
            Assert.That(result , Is.EqualTo(19.23739d).Within(0.001d));
        }
        [Test]
        public void Xb2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.AxialFaceWidth);
            Assert.That(result , Is.EqualTo(8.95869d).Within(0.001d));
        }
        [Test]
        public void Di1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableNames.InnerOutsideDiameter);
            Assert.That(result , Is.EqualTo(43.92916d).Within(0.001d));
        }
        [Test]
        public void Di2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableNames.InnerOutsideDiameter);
            Assert.That(result , Is.EqualTo(82.44848d).Within(0.001d));
        }
        
    }
}