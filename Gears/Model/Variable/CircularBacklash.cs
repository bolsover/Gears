using System.Collections.Generic;

namespace Bolsover.Gears.Model.Variable;

public class CircularBacklash : IVariable
{
    public double Value { get; set; }
    public string Name { get; } = @"Circular Backlash";
    public string NCalc { get; } = @"0.0";
    public string Symbol { get; } = @"j_{t}";
    public string Formula { get; } = @"j_t";
    public VariableRole Role { get; set; } = VariableRole.Design;
}
