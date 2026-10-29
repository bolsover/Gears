using Bolsover.Gears.Model;

namespace Bolsover.Gears.Calculator
{
    /// <summary>
    /// Contract for performing calculations on a Gear model without mixing calculation logic into the model type.
    /// </summary>
    public interface IGearCalculator
    {
        /// <summary>
        /// Calculates all non-design variables for the provided gear (and its subtree if applicable).
        /// </summary>
        /// <param id="gear">The gear whose variables should be calculated.</param>
        void CalculateAll(Gear gear);

        /// <summary>
        /// Calculates a single variable by its id on the provided gear.
        /// </summary>
        double CalculateVariable(Gear gear, VariableIds id);
    }
}
