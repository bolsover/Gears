using System.Drawing;

namespace Bolsover.Gears.Model;

public class DisplayData
{
    public DisplayData(string item, string metricValue, string imperialValue, string note, bool isError)
    {
        Item = item;
        MetricValue = metricValue;
        ImperialValue = imperialValue;
        Note = note;
        IsError = isError;
    }
    
    public DisplayData(string item, string metricValue, string imperialValue, string note, bool isError, Image symbol, Image formula)
    {
        Item = item;
        MetricValue = metricValue;
        ImperialValue = imperialValue;
        Note = note;
        IsError = isError;
        Symbol = symbol;
        Formula = formula;
    }
    
        
    public string Item { get; set; }
        
    public string MetricValue { get; set; }
        
    public string ImperialValue { get; set; }
        
    public string Note { get; set; }
        
    public bool IsError { get; set; }
    
    public Image Symbol { get; set; }
    
    public Image Formula { get; set; }
}