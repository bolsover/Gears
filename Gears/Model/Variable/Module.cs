using System.Collections.Generic;

namespace Bolsover.Gears.Model.Variable;

public class Module :IVariable
{
    public double Value { get; set; }
    public string Name { get; } = @"Module";
    public string NCalc { get; } = @"[m]";
    public string Symbol { get; } = @"m";
    public string Formula { get; } = @"m";
    public VariableRole Role { get; set; } = VariableRole.Design;
}