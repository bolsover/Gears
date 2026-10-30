namespace Bolsover.Gears.Model.Variable;

public class HelixAngle : IVariable
{
    public double Value { get; set; }
    public string Name { get; } = @"Helix Angle";
    public string NCalc { get; } = @"0.0";
    public string Symbol { get; } = @"\beta";
    public string Formula { get; } = @"\beta";
    public VariableRole Role { get; set; } = VariableRole.Design;
}
