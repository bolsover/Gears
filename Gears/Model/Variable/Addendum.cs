using System.Collections.Generic;

namespace Bolsover.Gears.Model.Variable;

public class Addendum :IVariable
{
    public double Value { get; set; }
    public string Name { get; } = @"Addendum";
    public string NCalc { get; } = @"1.00 * [m]";
    public string Symbol { get; } = @"h_{a}";
    public string Formula { get; } = @"1.00m";
   public VariableRole Role { get; set; } = VariableRole.Calculated;
}