using System.Collections.Generic;

namespace Bolsover.Gears.Model.Variable;

public class OutsideDiameter : IVariable
{
    public double Value { get; set; }
    public string Name { get; } = @"OutsideDiameter";
    public string NCalc { get; } = @"([z] * [m]) + 2 * [m]";
    public string Symbol { get; } = @"d_{a}";
    public string Formula { get; } = @"d + 2m";
   public VariableRole Role { get; set; } = VariableRole.Calculated;
}