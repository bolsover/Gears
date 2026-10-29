using System;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;
using NCalc;
using NUnit.Framework;

namespace UnitTests
{
    public class TestProfileShiftedInternalExternalHelicalPair
    {
        private Gear _gear;
    

        [SetUp]
        public void SetUp()
        {
            
            _gear = new Gear(GearTypes.ProfileShiftedInternalExternalHelicalPair, CalculationMode.Automatic);

        }

        [Test]
        public void NormalModule()
        {
            var result = _gear.CalculateVariable(VariableIds.NormalModule);
            Assert.That(result, Is.EqualTo(3d).Within(0.001d));
        }
        
        [Test]
        public void RadialModule()
        {
            var result = _gear.CalculateVariable(VariableIds.RadialModule);
            Assert.That(result, Is.EqualTo(3.4641d).Within(0.001d));
        }
        
        [Test]
        public void Z1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableIds.Teeth);
            Assert.That(result, Is.EqualTo(16d).Within(0.001d));
        }
        
        [Test]
        public void Z2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableIds.Teeth);
            Assert.That(result, Is.EqualTo(24d).Within(0.001d));
        }
        
        [Test]
        public void Alpha()
        {
            var result = _gear.CalculateVariable(VariableIds.NormalPressureAngle);
            Assert.That(result * (180.0 / Math.PI), Is.EqualTo(20d).Within(0.001d));
        }
        
        [Test]
        public void Beta()
        {
            var result = _gear.CalculateVariable(VariableIds.HelixAngle);
            Assert.That(result * (180.0 / Math.PI), Is.EqualTo(30d).Within(0.001d));
        }
        
        [Test]
        public void X1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableIds.CoefficientOfProfileShift);
            Assert.That(result , Is.EqualTo(0d).Within(0.001d));
        }
        
        [Test]
        public void X2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableIds.CoefficientOfProfileShift);
            Assert.That(result, Is.EqualTo(0.04263d).Within(0.001d));
        }
        
        [Test]
        public void B()
        {
            var result = _gear.CalculateVariable(VariableIds.HeightOfGear);
            Assert.That(result, Is.EqualTo(20d).Within(0.001d));
        }
        
        [Test]
        public void Ax()
        {
            var result = _gear.CalculateVariable(VariableIds.WorkingCentreDistanceax);
            Assert.That(result, Is.EqualTo(14d).Within(0.001d));
        }
        
        [Test]
        public void Ha1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableIds.Addendum);
            Assert.That(result, Is.EqualTo(3d).Within(0.001d));
        }
        
        [Test]
        public void Ha2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableIds.Addendum);
            Assert.That(result, Is.EqualTo(2.872d).Within(0.001d));
        }
        
        [Test]
        public void EpsilonBeta()
        {
            var result = _gear.CalculateVariable(VariableIds.AxialContactRatio);
            Assert.That(result, Is.EqualTo(1.0610d).Within(0.001d));
        }
        
        [Test]
        public void EpsilonAlpha()
        {
            var result = _gear.CalculateVariable(VariableIds.RadialContactRatio);
            Assert.That(result, Is.EqualTo(1.7345d).Within(0.001d));
        }
        
        [Test]
        public void Db1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableIds.BaseDiameter);
            Assert.That(result, Is.EqualTo(51.0964d).Within(0.001d));
        }
        
        [Test]
        public void Db2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableIds.BaseDiameter);
            Assert.That(result, Is.EqualTo(76.6446d).Within(0.001d));
        }
        
        [Test]
        public void Da1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableIds.OutsideDiameter);
            Assert.That(result, Is.EqualTo(61.4256).Within(0.001d));
        }
        
        [Test]
        public void Da2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableIds.OutsideDiameter);
            Assert.That(result, Is.EqualTo(77.3942d).Within(0.001d));
        }
        
        [Test]
        public void D1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableIds.PitchDiameter);
            Assert.That(result, Is.EqualTo(55.4256d).Within(0.001d));
        }
        
        [Test]
        public void D2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableIds.PitchDiameter);
            Assert.That(result, Is.EqualTo(83.1384d).Within(0.001d));
        }
        
        [Test]
        public void Alphat()
        {
            var result = _gear.CalculateVariable(VariableIds.RadialPressureAngle);
            Assert.That(result * (180.0 / Math.PI), Is.EqualTo(22.7958d).Within(0.001d));
        }
        
        [Test]
        public void Y()
        {
            var result = _gear.CalculateVariable(VariableIds.CentreDistanceIncrementFactor);
            Assert.That(result, Is.EqualTo(0.04786d).Within(0.001d));
        }
        
        [Test]
        public void Hf1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableIds.Dedendum);
            Assert.That(result, Is.EqualTo(3.75d).Within(0.001d));
        }
        
        [Test]
        public void Hf2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableIds.Dedendum);
            Assert.That(result, Is.EqualTo(3.8778d).Within(0.001d));
        }
        
        [Test]
        public void AlphaWt()
        {
            //Acos( ( ([z2]-[z1]) * Cos((Atan(Tan([alphan])/Cos([beta]))))) / ((Cos([beta]) *  2* ([ax]/[mn] - ([z2]-[z1])/(2*Cos([beta])))) +[z2] -[z1] ))
            var result = _gear.CalculateVariable(VariableIds.RadialWorkingPressureAngle);
            Assert.That(result * (180.0 / Math.PI), Is.EqualTo(24.1558d).Within(0.001d));
        }
        
        [Test]
        public void FormulaTest()
        {
            // var alphawt =
            //     "Acos( ( ([z2]-[z1]) * Cos((Atan(Tan([alphan])/Cos([beta]))))) / ((Cos([beta]) *  2* ([ax]/[mn] - ([z2]-[z1])/(2*Cos([beta])))) +[z2] -[z1] ))";
            // var expression = new Expression(alphawt);

            var epsilonalpha =
                "(Sqrt(Pow(((([z1]*[mn])/Cos([beta])) + (2*((1+[x1])*[mn])))/2 ,2)-Pow(((([z1]*[mn])/Cos([beta]))*Cos(Atan(Tan([alphan])/Cos([beta]))))/2 ,2))-Sqrt(Pow(((([z2]*[mn])/Cos([beta])) - (2*((1-[x2])*[mn])))/2 ,2)-Pow(((([z2]*[mn])/Cos([beta]))*Cos(Atan(Tan([alphan])/Cos([beta]))))/2 ,2))+([ax] * (Sin(Acos((([z2]-[z1])*Cos((Atan(Tan([alphan])/Cos([beta])))))/((Cos([beta])*2*([ax]/[mn]-([z2]-[z1])/(2*Cos([beta]))))+[z2]-[z1]))))))/([pi] * ([mn]/Cos([beta])) * Cos(Atan(Tan([alphan])/Cos([beta]))))";
            var expression = new Expression(epsilonalpha);
            var ax = 14d;
            var z1 = 16;
            var z2 = 24;
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
            var alphan = 20 * (Math.PI / 180.0);
            var x1 = 0;
            var x2 = 0.04263;
            
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            expression.Parameters["x1"] = x1;
            expression.Parameters["x2"] = x2;
            expression.Parameters["pi"] = Math.PI;

            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(1.7345d).Within(0.001d));
        }
        
        [Test]
        public void InvAlphat()
        {
            var result = _gear.CalculateVariable(VariableIds.RadialInvoluteFunction);
            Assert.That(result, Is.EqualTo(0.022413d).Within(0.001d));
        }
        
        [Test]
        public void InvAlphaWt()
        {
            var result = _gear.CalculateVariable(VariableIds.RadialWorkingInvoluteFunction);
            Assert.That(result, Is.EqualTo(0.0268927d).Within(0.001d));
        }
      
        [Test]
        public void Df1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableIds.RootDiameter);
            Assert.That(result, Is.EqualTo(47.9256d).Within(0.001d));
        }
        
        [Test]
        public void Df2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableIds.RootDiameter);
            Assert.That(result, Is.EqualTo(90.8942d).Within(0.001d));
        }
        
        [Test]
        public void H()
        {
            var result = _gear.CalculateVariable(VariableIds.WholeDepth);
            Assert.That(result, Is.EqualTo(6.75d).Within(0.001d));
        }
        
        [Test]
        public void A()
        {
            var result = _gear.CalculateVariable(VariableIds.StandardCentreDistance);
            Assert.That(result, Is.EqualTo(13.8564d).Within(0.001d));
        }
        
        [Test]
        public void Dwt1()
        {
            var result = _gear.Gears[0].CalculateVariable(VariableIds.RadialWorkingPitchDiameter);
            Assert.That(result, Is.EqualTo(56.0d).Within(0.001d));
        }
        
        [Test]
        public void Dwt2()
        {
            var result = _gear.Gears[1].CalculateVariable(VariableIds.RadialWorkingPitchDiameter);
            Assert.That(result, Is.EqualTo(84.00d).Within(0.001d));
        }
        
        [Test]
        public void Deltax()
        {
            var result = _gear.CalculateVariable(VariableIds.DifferenceCoefficientOfProfileShift);
            Assert.That(result, Is.EqualTo(0.04263d).Within(0.001d));
        }
        
        [Test]
        public void InvAlpha()
        {
            var result = _gear.CalculateVariable(VariableIds.InvoluteFunction);
            Assert.That(result, Is.EqualTo(0.01490d).Within(0.001d));
        }
    }
}