namespace Bolsover.Gears.Model
{
    /// <summary>
    /// Represents a calculated gear parameter item with description, LaTeX symbols and source formulas.
    /// </summary>
    public class GearParameterRow
    {
        public string ParameterName { get; set; }
        public string SymbolLatex { get; set; }
        public string FormulaLatex { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }

        public GearParameterRow()
        {
        }

        public GearParameterRow(string parameterName, string symbolLatex, string formulaLatex, double value, string unit)
        {
            ParameterName = parameterName;
            SymbolLatex = symbolLatex;
            FormulaLatex = formulaLatex;
            Value = value;
            Unit = unit;
        }
    }
}
