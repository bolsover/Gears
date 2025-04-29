using Bolsover.Gears.Model;

namespace Bolsover.Gears.Builder;

public class GearVariableBuilder
{
    private readonly GearVariable _gearVariable = new GearVariable();

    public GearVariable Build()
    {
        return _gearVariable;
    }

    public GearVariableBuilder SetVariableName(VariableNames name)
    {
        _gearVariable.Name = name;
        return this;
    }

    public GearVariableBuilder SetGearContext(GearContext context)
    {
        _gearVariable.Context = context;
        return this;
    }

    public GearVariableBuilder SetValue(double value)
    {
        _gearVariable.Value = value;
        return this;
    }

    public GearVariableBuilder SetSymbol(string symbol)
    {
        _gearVariable.Symbol = symbol;
        return this;
    }

    public GearVariableBuilder SetFormula(string formula)
    {
        _gearVariable.Formula = formula;
        return this;
    }

    public GearVariableBuilder SetIsDesgn(bool isDesign)
    {
        _gearVariable.IsDesign = isDesign;
        return this;
    }


    public GearVariableBuilder SetOrdinal(int ordinal)
    {
        _gearVariable.Ordinal = ordinal;
        return this;
    }

    public GearVariableBuilder SetNCalc(string ncalc)
    {
        _gearVariable.NCalc = ncalc;
        return this;
    }

    public GearVariableBuilder SetValueType(string valueType)
    {
        _gearVariable.ValueType = valueType;
        return this;
    }
}