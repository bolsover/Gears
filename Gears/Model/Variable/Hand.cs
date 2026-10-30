namespace Bolsover.Gears.Model.Variable;

public class Hand : IVariable
{
    public double Value { get; set; } // 0 = L, 1 = R
    public string Name { get; } = @"Hand";
    public string NCalc { get; } = @"0.0";
    public string Symbol { get; } = @"\text{Hand}";
    public string Formula { get; } = @"\text{Hand}";
    public VariableRole Role { get; set; } = VariableRole.Design;
}
