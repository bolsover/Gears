using System.Collections.Generic;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model.GearBox;
using Bolsover.Gears.Model.GearModels;
using Bolsover.Gears.Model.Variable;
using NUnit.Framework;

namespace UnitTests.VariableTests
{
    [TestFixture]
    public class ExternalGearVariableTests
    {
            [Test]
            public void TestExternalGearVariable()
            {
                ExternalGear gear = new ExternalGear();
                Dictionary<string, IVariable> variables = gear.GearVariables;

                variables.TryGetValue(@"Teeth", out IVariable teeth);
                teeth.Value = 24;
                variables.TryGetValue(@"Module", out IVariable module);
                module.Value = 3;
                variables.TryGetValue(@"PressureAngle", out IVariable pressureAngle);
                pressureAngle.Value = ConversionUtils.Radians(20);
                gear.Calculate();

                // Assert calculated values for each variable
                Assert.That(teeth.Value, Is.EqualTo(24d).Within(0.001d), "Teeth value should be 24");
                Assert.That(module.Value, Is.EqualTo(3d).Within(0.001d), "Module value should be 3");
                Assert.That(pressureAngle.Value, Is.EqualTo(ConversionUtils.Radians(20)).Within(0.001d), "PressureAngle value should be 20 degrees in radians");

                variables.TryGetValue(@"PitchDiameter", out IVariable pitchDiameter);
                Assert.That(pitchDiameter.Value, Is.EqualTo(72d).Within(0.001d), "PitchDiameter should be 72");

                variables.TryGetValue(@"BaseDiameter", out IVariable baseDiameter);
                Assert.That(baseDiameter.Value, Is.EqualTo(67.6578d).Within(0.001d), "BaseDiameter should be 67.6578");

                variables.TryGetValue(@"Addendum", out IVariable addendum);
                Assert.That(addendum.Value, Is.EqualTo(3d).Within(0.001d), "Addendum should be 3");

                variables.TryGetValue(@"Dedendum", out IVariable dedendum);
                Assert.That(dedendum.Value, Is.EqualTo(3.75d).Within(0.001d), "Dedendum should be 3.75");

                variables.TryGetValue(@"OutsideDiameter", out IVariable outsideDiameter);
                Assert.That(outsideDiameter.Value, Is.EqualTo(78d).Within(0.001d), "OutsideDiameter should be 78");

                variables.TryGetValue(@"RootDiameter", out IVariable rootDiameter);
                Assert.That(rootDiameter.Value, Is.EqualTo(64.5d).Within(0.001d), "RootDiameter should be 64.5");

             
                foreach (var variable in variables.Values)
                {
                    ConsoleIO.WriteLine($"Name: {variable.Name}, Value: {variable.Value}, NCalc: {variable.NCalc}, Symbol: {variable.Symbol}, Formula: {variable.Formula}");
                }
              
                
            }
    }
}