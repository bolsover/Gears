using System.ComponentModel;
using System.Linq;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Gui.Views;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class TestNumericInputs
    {
        private sealed class NumericProperties
        {
            public int Teeth { get; set; } = 20;
            public double PressureAngle { get; set; } = 20.0;
            public double EditableValue { get; set; } = 1.0;
            public double ReadOnlyValue => 2.0;
        }

        [Test]
        public void TestTeethLimits_AreIntegerAndBounded()
        {
            var descriptor = TypeDescriptor.GetProperties(typeof(NumericProperties))[nameof(NumericProperties.Teeth)];

            var limits = NumericInputLimits.For(descriptor);

            Assert.AreEqual(2m, limits.Minimum);
            Assert.AreEqual(10000m, limits.Maximum);
            Assert.AreEqual(1m, limits.Increment);
            Assert.AreEqual(0, limits.DecimalPlaces);
            Assert.IsTrue(limits.IsInteger);
        }

        [Test]
        public void TestPressureAngleLimits_UseTenthsWithThreeDecimalPlaces()
        {
            var descriptor = TypeDescriptor.GetProperties(typeof(NumericProperties))[nameof(NumericProperties.PressureAngle)];

            var limits = NumericInputLimits.For(descriptor);

            Assert.AreEqual(10m, limits.Minimum);
            Assert.AreEqual(45m, limits.Maximum);
            Assert.AreEqual(0.1m, limits.Increment);
            Assert.AreEqual(3, limits.DecimalPlaces);
            Assert.IsFalse(limits.IsInteger);
        }

        [Test]
        public void TestFilteredProperties_ClampsAndRoundsNumericValues()
        {
            var target = new NumericProperties();
            var filtered = new FilteredProperties(target, new TypePropertyLayout());
            var descriptors = filtered.GetProperties().Cast<PropertyDescriptor>().ToArray();
            var teeth = descriptors.Single(property => property.Name == nameof(NumericProperties.Teeth));
            var pressureAngle = descriptors.Single(property => property.Name == nameof(NumericProperties.PressureAngle));

            teeth.SetValue(filtered.GetPropertyOwner(teeth), 1);
            pressureAngle.SetValue(filtered.GetPropertyOwner(pressureAngle), 60.0);
            Assert.AreEqual(2, target.Teeth);
            Assert.AreEqual(45.0, target.PressureAngle);

            pressureAngle.SetValue(filtered.GetPropertyOwner(pressureAngle), 23.1236);
            Assert.AreEqual(23.124, target.PressureAngle, 1e-12);
        }

        [Test]
        public void TestFilteredProperties_OnlyWritableNumericPropertiesGetTheEditor()
        {
            var filtered = new FilteredProperties(new NumericProperties(), new TypePropertyLayout());
            var descriptors = filtered.GetProperties().Cast<PropertyDescriptor>().ToArray();
            var editable = descriptors.Single(property => property.Name == nameof(NumericProperties.EditableValue));
            var readOnly = descriptors.Single(property => property.Name == nameof(NumericProperties.ReadOnlyValue));

            var editor = editable.Attributes[typeof(EditorAttribute)] as EditorAttribute;
            Assert.IsNotNull(editor);
            StringAssert.Contains(typeof(NumericUpDownEditor).FullName, editor.EditorTypeName);
            Assert.IsTrue(readOnly.IsReadOnly);
            Assert.IsNull(readOnly.Attributes[typeof(EditorAttribute)]);
        }
    }
}
