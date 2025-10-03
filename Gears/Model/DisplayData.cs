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
        
    public string Item { get; set; }
        
    public string MetricValue { get; set; }
        
    public string ImperialValue { get; set; }
        
    public string Note { get; set; }
        
    public bool IsError { get; set; }
}