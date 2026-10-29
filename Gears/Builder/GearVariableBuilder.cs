using System;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Builder;

public class GearVariableBuilder
{
    private readonly GearVariable _gearVariable = new GearVariable();

    public GearVariable Build()
    {
        return _gearVariable;
    }

    public GearVariableBuilder SetVariableId(VariableIds id)
    {
        _gearVariable.Id = id;
        return this;
    }

    public GearVariableBuilder SetGearType(GearTypes context)
    {
        _gearVariable.GearType = context;
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

    public GearVariableBuilder SetFormulaOverride(string formulaOverride)
    {
        _gearVariable.FormulaOverride = formulaOverride;
        return this;
    }

    public GearVariableBuilder SetKey(string key)
    {
        _gearVariable.Key = key;
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
    
    public GearVariableBuilder SetCalcMethod(string calcMethod)
    {
        _gearVariable.CalcMethod = calcMethod;
        return this;
    }

    public GearVariableBuilder SetGroupId(string groupId)
    {
        _gearVariable.GroupId = groupId;
        return this;
    }
    
    public GearVariableBuilder SetGearPart(string gearPart)
    {
        _gearVariable.GearPart = gearPart;
        return this;
    }
    
    public GearVariableBuilder SetDisplayName(string displayName){
        _gearVariable.DisplayName = displayName;
        return this;
    }
    public GearVariableBuilder SetDisplayMetricValue(string displayMetricValue){
        _gearVariable.DisplayMetricValue = displayMetricValue;
        return this;
    }
    public GearVariableBuilder SetDisplayImperialValue(string displayImperialValue){
        _gearVariable.DisplayImperialValue = displayImperialValue;
        return this;
    }
    // public GearVariableBuilder SetDisplayImperialCalc(string displayImperialCalc){
    //     _gearVariable.DisplayImperialCalc = displayImperialCalc;
    //     return this;
    // }
    
    public GearVariableBuilder SetDisplayImperialFormat(string displayImperialFormat){
        _gearVariable.DisplayImperialFormat = displayImperialFormat;
     return this;
    }
    
    public GearVariableBuilder SetDisplayMetricCalc(string displayMetricCalc){
        _gearVariable.DisplayMetricCalc = displayMetricCalc;
        return this;
    }
    
    public GearVariableBuilder SetDisplayMetricFormat(string displayMetricFormat){
        _gearVariable.DisplayMetricFormat = displayMetricFormat;
        return this;
    }
    public GearVariableBuilder SetDisplayNote1(string displayNote1){
        _gearVariable.DisplayNote1 = displayNote1;
        return this;
    }
    public GearVariableBuilder SetDisplayNote2(string displayNote2){
        _gearVariable.DisplayNote2 = displayNote2;
        return this;
    }
    public GearVariableBuilder SetIsError(bool isError){
        _gearVariable.IsError = isError;
        return this;
    }

    public GearVariableBuilder SetCovertImperial(ConvertImperial convertImperial)
    {
        _gearVariable.ConvertImperial = convertImperial;
        return this;
    }
    
    public GearVariableBuilder SetCovertMetric(ConvertMetric convertMetric)
    {
        _gearVariable.ConvertMetric = convertMetric;
        return this;
    }
}