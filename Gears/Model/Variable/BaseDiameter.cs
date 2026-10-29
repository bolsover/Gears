using System.Collections.Generic;

namespace Bolsover.Gears.Model.Variable;

public class BaseDiameter : IVariable
{
    public double Value { get; set; }
    public string Name { get; } = @"BaseDiameter";
    public string NCalc { get; } = @"[z] * [m] * Cos([alpha])";
    public string Symbol { get; } = @"d_{b}";
    public string Formula { get; } = @"d\:cos\alpha";
   public VariableRole Role { get; set; } = VariableRole.Calculated;
}