using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Bolsover.Gears.Model;

/// <summary>
/// Represents a variable within a gear design or analysis context.
/// </summary>
/// <remarks>
/// The <c>GearVariable</c> class encapsulates various properties and metadata
/// associated with a specific gear attribute or design parameter. It provides
/// support for tracking its display characteristics, value ranges, symbolic representation,
/// calculation formulae, and design status.
/// This class also implements the <c>INotifyPropertyChanged</c> interface to
/// enable data-binding scenarios by notifying listeners about property changes.
/// </remarks>
public sealed class GearVariable : INotifyPropertyChanged
{
    public GearTypes GearType { get; set; }
    public VariableIds Id { get; set; }
    public bool IsDesign { get; set; }
    /// <summary>
    /// Machine identifier, unique per GearType. NCalc expressions reference this variable as <c>[Key]</c>,
    /// and <see cref="Symbol"/> is its LaTeX rendering.
    /// </summary>
    public string Key { get; set; }
    public string Symbol { get; set; }

    /// <summary>
    /// LaTeX for display. Generated from <see cref="NCalc"/> unless <see cref="FormulaOverride"/> is set.
    /// </summary>
    public string Formula { get; set; }

    /// <summary>
    /// Hand-written LaTeX shown in place of the generated formula, for rows that cannot or should not be generated.
    /// </summary>
    public string FormulaOverride { get; set; }
    public string NCalc { get; set; }
    public string CalcMethod { get; set; }
    public string GroupId { get; set; }
    public int Ordinal { get; set; }
    public string GearPart { get; set; }
    public string DisplayName { get; set; }
    public string DisplayMetricValue { get; set; }
    public string DisplayMetricCalc { get; set; }
    public string DisplayMetricFormat { get; set; }
    public string DisplayImperialValue { get; set; }
    public ConvertImperial ConvertImperial { get; set; }
    public ConvertMetric ConvertMetric { get; set; }
    public string DisplayImperialFormat { get; set; }
    public string DisplayNote1 { get; set; }
    public string DisplayNote2 { get; set; }
    public bool IsError { get; set; }

    private double _value;

    public string ValueType { get; set; }

    public double Value
    {
        get => _value;
        set => SetField(ref _value, value);
    }


    public GearVariable(GearTypes gearType, VariableIds id, double value, string symbol, string formula,
        bool isDesign, string valueType, string nCalc)
    {
        GearType = gearType;
        Id = id;
        Value = value;
        Symbol = symbol;
        Formula = formula;
        IsDesign = isDesign;
        ValueType = valueType;
        NCalc = nCalc;
    }

    public override string ToString()
    {
        return "GearVariable [Id=" + Id + ", Key=" + Key + ", Value=" + Value + ", Symbol=" + Symbol + ", Formula=" + Formula +
               ", IsDesign=" + IsDesign +
               ", NCalc=" + NCalc + "]";
    }

    public GearVariable()
    {
    }


    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}