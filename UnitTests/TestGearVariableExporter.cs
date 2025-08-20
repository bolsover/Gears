using Bolsover.Gears.Calculator;
using Bolsover.Gears.Data;
using Bolsover.Gears.Model;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace UnitTests
{
    public class TestGearVariableExporter
    {
        [Test]
        public void ToJson_ProducesDesignAndCalculatedCollections_WithRequiredFields()
        {
            // Arrange
            var gear = new Gear(GearContext.ExternalNormalHelical, CalculationMode.Automatic);

            // Act
            var json = GearVariableExporter.ToJson(gear);
            var obj = JObject.Parse(json);

            // Assert
            Assert.IsNotNull(obj["variables"], "variables object should be present");
            var design = obj["variables"]?["design"] as JArray;
            var calculated = obj["variables"]?["calculated"] as JArray;

            Assert.IsNotNull(design, "design array should be present");
            Assert.IsNotNull(calculated, "calculated array should be present");

            Assert.Greater(design!.Count, 0, "design array should not be empty for ExternalInvolute context");
            Assert.Greater(calculated!.Count, 0, "calculated array should not be empty for ExternalInvolute context");

            // Check shape of first items
            var firstDesign = (JObject)design[0]!;
            Assert.IsNotNull(firstDesign["name"], "name should be present");
            Assert.IsNotNull(firstDesign["symbol"], "symbol should be present");
            Assert.IsNotNull(firstDesign["formula"], "formula should be present (may be empty) ");
            Assert.IsNotNull(firstDesign["value"], "value should be present");
        }
    }
}
