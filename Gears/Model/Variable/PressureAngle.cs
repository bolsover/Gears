using System.Collections.Generic;

namespace Bolsover.Gears.Model.Variable;

public class PressureAngle :IVariable
{
    public double Value { get; set; }
    public string Name { get; } = @"PressureAngle";
    public string NCalc { get; } = @"[alpha]";
    public string Symbol { get; } = @"\alpha";
    public string Formula { get; } = @"\alpha";
   public VariableRole Role { get; set; } = VariableRole.Design;
}