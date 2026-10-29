using System.Collections.Generic;

namespace Bolsover.Gears.Model.Variable;

public class Dedendum :IVariable
{
    public double Value { get; set; }
    public string Name { get; } = @"Dedendum";
    public string NCalc { get; } = @"1.25 * [m]";
    public string Symbol { get; } = @"h_{f}";
    public string Formula { get; } = @"1.25m";
    public VariableRole Role { get; set; } = VariableRole.Calculated;
}