using System;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Calculator
{
    /// <summary>
    /// Builds a 2D ToothProfile (feature dictionary) for a single gear tooth by
    /// delegating to a context- and mode-aware computation strategy.
    /// </summary>
    /// <remarks>
    /// The calculator selects a computation strategy based on Gear.Context and Gear.Mode.
    /// Default strategies produce a minimal, safe set of features (reference circles and
    /// simple flank placeholders) and can be extended to use different GearVariables
    /// per mode/context in the future.
    /// </remarks>
    public class ToothProfileCalculator
    {
        private readonly ToothProfileComputationProvider _provider;

        public ToothProfileCalculator() : this(new ToothProfileComputationProvider()) { }

        internal ToothProfileCalculator(ToothProfileComputationProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Creates a <see cref="ToothProfile"/> for the provided <paramref name="gear"/> by selecting
        /// an appropriate strategy for the gear's context and calculation mode.
        /// </summary>
        /// <param name="gear">The gear model containing variables, context and mode.</param>
        /// <returns>A populated <see cref="ToothProfile"/>.</returns>
        public ToothProfile Calculate(Gear gear)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            var strategy = _provider.Get(gear.Context, gear.Mode);
            return strategy.Calculate(gear);
        }
    }
}
