using System.Collections.Generic;

namespace Bolsover.Gears.Model.Variable;

public interface IVariable
{
    double Value { get; set; }
    string Name { get; }
    string NCalc { get; }
    string Symbol { get; }
    string Formula { get; }
    VariableRole Role { get; set; }
   
}

public enum VariableRole
{
    Design,
    Calculated
}