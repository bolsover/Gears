using System.Collections.Generic;

namespace Bolsover.Gears.Model.Variable;

public class PitchDiameter : IVariable
{
    public double Value { get; set; }
    public string Name { get; } = @"PitchDiameter";
    public string NCalc { get; } = @"[z] * [m]";
    public string Symbol { get; } = @"d";
    public string Formula { get; } = @"zm";
    public VariableRole Role { get; set; } = VariableRole.Calculated;
}