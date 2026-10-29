using System;
using Bolsover.Gears.Model;
using Bolsover.Gears.Calculator.Strategies;

namespace Bolsover.Gears.Calculator
{
    /// <summary>
    /// Selects an appropriate tooth profile computation strategy based on
    /// GearContext and CalculationMode.
    /// </summary>
    public class ToothProfileComputationProvider
    {
        public IToothProfileComputation Get(GearTypes context, CalculationMode mode)
        {
            var isInternal = (context & GearTypes.Internal) == GearTypes.Internal;
            var isExternal = (context & GearTypes.External) == GearTypes.External || !isInternal;

            // Prefer explicit mapping; fall back to external automatic as a safe default.
            return (isInternal, mode) switch
            {
                (true, CalculationMode.Automatic) => new Strategies.InternalAutomaticToothProfileComputation(),
                (true, CalculationMode.Manual) => new Strategies.InternalManualToothProfileComputation(),
                (false, CalculationMode.Automatic) when isExternal => new Strategies.ExternalAutomaticToothProfileComputation(),
                (false, CalculationMode.Manual) when isExternal => new Strategies.ExternalManualToothProfileComputation(),
                _ => new Strategies.ExternalAutomaticToothProfileComputation()
            };
        }
    }
}
