using System;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Works out the profile shift that makes two meshing involute gears run at a chosen centre distance, using the same
    /// relations as <see cref="GearParameterCalculator.CalculatePair"/> run backwards.
    /// </summary>
    public static class ProfileShiftFitter
    {
        /// <summary>
        /// The standard (unshifted) centre distance of the pair in mm: <c>mt (z1 + z2) / 2</c> for an external pair and
        /// <c>mt (zInternal - zExternal) / 2</c> when exactly one gear is internal.
        /// </summary>
        public static double StandardCentreDistance(InvoluteGearComponent first, InvoluteGearComponent second)
        {
            Require(first, second);
            var cosBeta = CosBeta(first);
            return first.Module / cosBeta * SumTeeth(first, second) / 2.0;
        }

        /// <summary>
        /// The sum of profile shifts (x1 + x2 for an external pair, x of the internal gear minus x of the external gear for an
        /// internal pair) at which the pair runs at <paramref name="centreDistance"/> with the gears' own backlash.
        /// </summary>
        public static double RequiredProfileShiftSum(InvoluteGearComponent first, InvoluteGearComponent second, double centreDistance)
        {
            Require(first, second);
            var sumZ = SumTeeth(first, second);
            var aStd = StandardCentreDistance(first, second);
            if (aStd <= 0.0)
            {
                throw new InvalidOperationException($"'{first.Id}' and '{second.Id}' have no standard centre distance, so a profile shift cannot be fitted.");
            }

            if (double.IsNaN(centreDistance) || double.IsInfinity(centreDistance) || centreDistance <= 0.0)
            {
                throw new InvalidOperationException($"The centre distance must be a positive number (got {centreDistance}).");
            }

            var alpha = first.PressureAngleDeg * Math.PI / 180.0;
            var alphaT = Math.Atan(Math.Tan(alpha) / CosBeta(first));
            var cosAlphaW = aStd * Math.Cos(alphaT) / centreDistance;
            if (cosAlphaW > 1.0 + 1e-12)
            {
                throw new InvalidOperationException(
                    $"A centre distance of {centreDistance} mm is too small for '{first.Id}' and '{second.Id}': the base circles would overlap.");
            }

            var alphaW = Math.Acos(Math.Min(1.0, cosAlphaW));
            var invAlphaW = Math.Tan(alphaW) - alphaW;
            var invAlphaT = Math.Tan(alphaT) - alphaT;
            return (invAlphaW - invAlphaT) * sumZ / (2.0 * Math.Tan(alpha));
        }

        /// <summary>
        /// The profile shift that <paramref name="driven"/> needs, given the profile shift already set on
        /// <paramref name="driver"/>, for the pair to run at <paramref name="centreDistance"/>.
        /// </summary>
        public static double RequiredDrivenProfileShift(InvoluteGearComponent driver, InvoluteGearComponent driven, double centreDistance)
        {
            var sumX = RequiredProfileShiftSum(driver, driven, centreDistance);
            if (driver.IsInternal == driven.IsInternal) return sumX - driver.ProfileShift;
            return driven.IsInternal ? sumX + driver.ProfileShift : driver.ProfileShift - sumX;
        }

        private static double SumTeeth(InvoluteGearComponent first, InvoluteGearComponent second)
        {
            if (first.IsInternal == second.IsInternal) return first.Teeth + second.Teeth;
            var internalTeeth = first.IsInternal ? first.Teeth : second.Teeth;
            var externalTeeth = first.IsInternal ? second.Teeth : first.Teeth;
            return internalTeeth - externalTeeth;
        }

        private static double CosBeta(InvoluteGearComponent gear)
        {
            var cosBeta = Math.Cos(gear.HelixAngleDeg * Math.PI / 180.0);
            return cosBeta < 1e-6 ? 1e-6 : cosBeta;
        }

        private static void Require(InvoluteGearComponent first, InvoluteGearComponent second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            first.Validate();
            second.Validate();
        }
    }
}
