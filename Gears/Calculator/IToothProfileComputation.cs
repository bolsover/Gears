using Bolsover.Gears.Model;

namespace Bolsover.Gears.Calculator
{
    /// <summary>
    /// Strategy interface to compute a ToothProfile for a given Gear.
    /// Different implementations can vary by GearContext and CalculationMode
    /// and may rely on different sets of GearVariables.
    /// </summary>
    public interface IToothProfileComputation
    {
        ToothProfile Calculate(Gear gear);
    }
}
