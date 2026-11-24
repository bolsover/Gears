using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Bolsover.Gears.Gui.Presenters;

namespace Bolsover.Gears.Gui.Views
{
    /// <summary>Edits a numeric property with a bounded NumericUpDown control.</summary>
    public sealed class NumericUpDownEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (!(provider?.GetService(typeof(IWindowsFormsEditorService)) is IWindowsFormsEditorService editorService) ||
                context?.PropertyDescriptor == null) return value;

            var limits = NumericInputLimits.For(context.PropertyDescriptor);
            using (var input = new NumericUpDown())
            {
                input.Minimum = limits.Minimum;
                input.Maximum = limits.Maximum;
                input.Increment = limits.Increment;
                input.DecimalPlaces = limits.DecimalPlaces;
                input.Value = limits.Clamp(ToDecimal(value, limits));
                input.Width = 120;
                input.KeyDown += (_, args) =>
                {
                    if (args.KeyCode != Keys.Enter) return;
                    args.Handled = true;
                    args.SuppressKeyPress = true;
                    editorService.CloseDropDown();
                };

                editorService.DropDownControl(input);
                return context.PropertyDescriptor.PropertyType == typeof(int)
                    ? (object)decimal.ToInt32(input.Value)
                    : decimal.ToDouble(input.Value);
            }
        }

        private static decimal ToDecimal(object value, NumericLimits limits)
        {
            if (value is double doubleValue)
            {
                if (double.IsNaN(doubleValue)) return limits.Clamp(0m);
                if (double.IsPositiveInfinity(doubleValue) || doubleValue >= (double)limits.Maximum) return limits.Maximum;
                if (double.IsNegativeInfinity(doubleValue) || doubleValue <= (double)limits.Minimum) return limits.Minimum;
            }

            try
            {
                return limits.Clamp(Convert.ToDecimal(value, CultureInfo.CurrentCulture));
            }
            catch (FormatException)
            {
                return limits.Clamp(0m);
            }
            catch (InvalidCastException)
            {
                return limits.Clamp(0m);
            }
            catch (OverflowException)
            {
                return limits.Clamp(0m);
            }
        }
    }
}
