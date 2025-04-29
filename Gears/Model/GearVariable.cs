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
   public GearContext Context { get; set; }
    public VariableNames Name { get; set; }
    public bool IsDesign { get; set; }
    public string Symbol { get; set; }
    public string Formula { get; set; }
    public string NCalc { get; set; }

    private double _value;
    
    public string ValueType { get; set; }

    public double Value
    {
        get => _value;
        set => SetField(ref _value, value);
    }

    public int Ordinal { get; set; }

    public GearVariable(GearContext context, VariableNames name, double value, string symbol, string formula,
        bool isDesign, string valueType)
    {
        Context = context;
        Name = name;
        Value = value;
        Symbol = symbol;
        Formula = formula;
        IsDesign = isDesign;
        ValueType = valueType;
    }

    public override string ToString()
    {
        return "GearVariable [Name=" + Name + ", Value=" + Value + ", Symbol=" + Symbol + ", Formula=" + Formula +
               ", IsDesign=" + IsDesign + "]";
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