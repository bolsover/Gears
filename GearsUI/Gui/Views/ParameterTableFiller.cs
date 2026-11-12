using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Gui.Views
{
    /// <summary>Fills the parameter and imperial reference tables of the gear train editor, and keeps the rendered formula images.</summary>
    internal sealed class ParameterTableFiller
    {
        private readonly Dictionary<string, Image> _latexImageCache = new Dictionary<string, Image>();

        /// <summary>
        /// Fills an imperial reference table. The design stays metric; the inch values, diametral pitch and circular pitch are for reference.
        /// </summary>
        internal static void FillImperialTable(DataGridView grid, Label title, ImperialTable table, string emptyText)
        {
            grid.SuspendLayout();
            try
            {
                grid.Rows.Clear();
                title.Text = table.Rows.Count > 0 ? table.Title + " - imperial reference (metric values remain the design values)" : emptyText;
                foreach (var row in table.Rows)
                {
                    grid.Rows.Add(
                        row.Name,
                        row.Symbol,
                        row.Formula,
                        row.MetricValue.ToString("0.#######", CultureInfo.InvariantCulture),
                        row.MetricUnit,
                        row.ImperialValue.ToString("0.#######", CultureInfo.InvariantCulture),
                        row.ImperialUnit);
                }
            }
            finally
            {
                grid.ResumeLayout();
            }
        }

        internal void FillParameterTable(DataGridView grid, Label title, GearParameterTable table, string emptyText)
        {
            grid.SuspendLayout();
            try
            {
                grid.Rows.Clear();
                title.Text = table.Rows.Count > 0 ? table.Title : emptyText;
                foreach (var row in table.Rows)
                {
                    var index = grid.Rows.Add(
                        row.ParameterName,
                        GetLatexImage(row.SymbolLatex),
                        GetLatexImage(row.FormulaLatex),
                        row.Value.ToString("0.#######", CultureInfo.InvariantCulture),
                        row.Unit);
                    grid.Rows[index].Cells["colSymbol"].ToolTipText = row.SymbolLatex;
                    grid.Rows[index].Cells["colFormula"].ToolTipText = row.FormulaLatex;
                }
            }
            finally
            {
                grid.ResumeLayout();
            }
        }

        private Image GetLatexImage(string latex)
        {
            if (string.IsNullOrWhiteSpace(latex)) return null;
            if (_latexImageCache.TryGetValue(latex, out var cached)) return cached;

            try
            {
                if (LaTeXUtils.TryParse(latex, out _))
                {
                    var image = LaTeXUtils.CreateImageFromLatex(latex);
                    if (image != null) _latexImageCache[latex] = image;
                    return image;
                }
            }
            catch
            {
                // A formula the renderer cannot handle is shown as a blank cell; the tooltip still has the source.
            }

            return null;
        }

        /// <summary>Releases the rendered formula images.</summary>
        public void DisposeImages()
        {
            foreach (var image in _latexImageCache.Values) image?.Dispose();
            _latexImageCache.Clear();
        }
    }
}
