using System.Collections.Generic;

namespace Bolsover.Gears.Model.Variable;

public class Teeth :IVariable
{
    public double Value { get; set; }
    public string Name { get; } = @"Teeth";
    public string NCalc { get; } = @"[z]";
    public string Symbol { get; } = @"z";
    public string Formula { get; } = @"z";
   public VariableRole Role { get; set; } = VariableRole.Design;
}