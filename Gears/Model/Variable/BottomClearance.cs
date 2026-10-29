using System.Collections.Generic;

namespace Bolsover.Gears.Model.Variable;

public class BottomClearance : IVariable
{
    public double Value { get; set; }
    public string Name { get; } = @"Bottom Clearance";
    public string NCalc { get; } = @"0.25 * [m]";
    public string Symbol { get; } = @"c";
    public string Formula { get; } = @"0.25m";
    public VariableRole Role { get; set; } = VariableRole.Calculated;
}
