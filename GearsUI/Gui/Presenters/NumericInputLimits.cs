using System;
using System.ComponentModel;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>The range and precision used by a numeric property editor.</summary>
    public sealed class NumericLimits
    {
        public NumericLimits(decimal minimum, decimal maximum, decimal increment, int decimalPlaces, bool isInteger)
        {
            if (minimum > maximum) throw new ArgumentOutOfRangeException(nameof(minimum));
            if (increment <= 0m) throw new ArgumentOutOfRangeException(nameof(increment));
            if (decimalPlaces < 0 || decimalPlaces > 28) throw new ArgumentOutOfRangeException(nameof(decimalPlaces));

            Minimum = minimum;
            Maximum = maximum;
            Increment = increment;
            DecimalPlaces = decimalPlaces;
            IsInteger = isInteger;
        }

        public decimal Minimum { get; }
        public decimal Maximum { get; }
        public decimal Increment { get; }
        public int DecimalPlaces { get; }
        public bool IsInteger { get; }

        public decimal Clamp(decimal value)
        {
            return Math.Min(Maximum, Math.Max(Minimum, value));
        }
    }

    /// <summary>Chooses practical bounds for the numeric properties shown by the gear-train editor.</summary>
    public static class NumericInputLimits
    {
        public static NumericLimits For(PropertyDescriptor property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            var name = property.Name ?? string.Empty;
            var displayName = property.DisplayName ?? string.Empty;

            if (name.EndsWith("Teeth", StringComparison.OrdinalIgnoreCase)) return Integer(2m, 10000m);
            if (Contains(name, "PressureAngle")) return Continuous(10m, 45m);
            if (Contains(name, "Module")) return Continuous(0.05m, 100m);
            if (Contains(name, "HelixAngle")) return Continuous(0m, 89.9m);
            if (Contains(name, "ProfileShift")) return Continuous(-2m, 2m);
            if (Contains(name, "Clearance")) return Continuous(0m, 1m);
            if (Contains(name, "Backlash")) return Continuous(0m, 10m);

            if (displayName.IndexOf("(deg)", StringComparison.OrdinalIgnoreCase) >= 0) return Continuous(-360m, 360m);
            if (displayName.IndexOf("(mm", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (Contains(name, "Offset") || Contains(name, "Origin") ||
                    Contains(displayName, "Offset") || Contains(displayName, "Origin"))
                {
                    return Continuous(-10000m, 10000m);
                }

                return Continuous(0m, 100000m);
            }

            return property.PropertyType == typeof(int)
                ? Integer(0m, 10000m)
                : Continuous(-10000m, 10000m);
        }

        private static bool Contains(string value, string part)
        {
            return value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static NumericLimits Integer(decimal minimum, decimal maximum)
        {
            return new NumericLimits(minimum, maximum, 1m, 0, true);
        }

        private static NumericLimits Continuous(decimal minimum, decimal maximum)
        {
            return new NumericLimits(minimum, maximum, 0.1m, 3, false);
        }
    }
}
