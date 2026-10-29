using System;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    public class TestGearLoading
    {
        [Test]
        public void GearPair_Loads_Common_And_PerGear_Variables()
        {
            // Arrange & Act
            var gear = new Gear(GearTypes.ProfileShiftedExternalInvolutePair, CalculationMode.Automatic);
            gear.Calculate();

            // Common (set-level) variable assertions
            var module = gear.GetVariable(VariableIds.Module);
            Assert.That(module, Is.Not.Null, "Set-level NormalModule should exist");
            Assert.That(module.Value, Is.EqualTo(3d).Within(0.001d));

            // Ensure we have exactly two gears
            Assert.That(gear.Gears.Count, Is.EqualTo(2), "GearPair should contain two gears");

            // Per-gear variables: Gear1
            var gear1 = gear.Gears[0];
            Assert.That(gear1.GetVariable(VariableIds.Teeth).Value, Is.EqualTo(12d).Within(0.001d));
            Assert.That(gear1.GetVariable(VariableIds.Addendum).Value, Is.EqualTo(4.41989d).Within(0.001d));

            // Per-gear variables: Gear2
            var gear2 = gear.Gears[1];
            Assert.That(gear2.GetVariable(VariableIds.Teeth).Value, Is.EqualTo(24d).Within(0.001d));
            Assert.That(gear2.GetVariable(VariableIds.Addendum).Value, Is.EqualTo(3.69989d).Within(0.001d));
        }
    }
}
