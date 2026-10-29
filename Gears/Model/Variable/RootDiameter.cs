using System.Collections.Generic;

namespace Bolsover.Gears.Model.Variable;

public class RootDiameter: IVariable
{
    public double Value { get; set; }
    public string Name { get; } = @"RootDiameter";
    public string NCalc { get; } = @"([z] * [m]) - 2.5 * [m]";
    public string Symbol { get; } = @"d_{f}";
    public string Formula { get; } = @"d - 2.5m";
    public VariableRole Role { get; set; } = VariableRole.Calculated;
}