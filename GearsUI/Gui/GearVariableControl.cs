using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Gui;

public class GearVariableControl : UserControl
{
    private readonly Gear _gear;

    private const int NameColumn = 0;

    // private const int DesignColumn = 1;
    
    
    private const int ValueColumn1 = 4;
    private const int SymbolColumn1 = 2;
    private const int FormulaColumn1 = 6;
    private const int NCalcColumn = 12;

    public GearVariableControl(Gear gear)
    {
        _gear = gear;


        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // Set up the main container properties
        Dock = DockStyle.Fill;
        var panel = CreateTableLayoutPanel();
        AddColumnHeaders(panel);
        AddVariableRows(panel);
        Controls.Add(panel);
    }

    private TableLayoutPanel CreateTableLayoutPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
            Padding = new Padding(10),
            ColumnCount = 11, // Name, Design, Value, Symbol, Formula
            AutoScroll = true,
            RowCount = _gear.GearVariables.Count + 1, // Includes header row
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Header row style
        return panel;
    }

    private static void AddColumnHeaders(TableLayoutPanel panel)
    {
        string[] headers = { "Variable Name", "Symbol", "Value", "Formula" };
        // for (var col = 0; col < headers.Length; col++)
        // {
            AddLabel(panel, "Variable", true, 0, 0);
            panel.SetColumnSpan(panel.Controls[panel.Controls.Count - 1], 1);
            panel.Controls[panel.Controls.Count - 1].Dock = DockStyle.Fill;
            AddLabel(panel, "Symbol", true, 2, 0);
            panel.SetColumnSpan(panel.Controls[panel.Controls.Count - 1], 2);
            panel.Controls[panel.Controls.Count - 1].Dock = DockStyle.Fill;
            AddLabel(panel, "Pinion", true, 2, 1);
            panel.SetColumnSpan(panel.Controls[panel.Controls.Count - 1], 1);
            panel.Controls[panel.Controls.Count - 1].Dock = DockStyle.Fill;
            AddLabel(panel, "Gear", true, 3, 1);
            panel.SetColumnSpan(panel.Controls[panel.Controls.Count - 1], 1);
            panel.Controls[panel.Controls.Count - 1].Dock = DockStyle.Fill;
            AddLabel(panel, "Value", true, 4, 0);
            panel.SetColumnSpan(panel.Controls[panel.Controls.Count - 1], 2);
            panel.Controls[panel.Controls.Count - 1].Dock = DockStyle.Fill;
            AddLabel(panel, "Pinion", true, 4, 1);
            panel.SetColumnSpan(panel.Controls[panel.Controls.Count - 1], 1);
            panel.Controls[panel.Controls.Count - 1].Dock = DockStyle.Fill;
            AddLabel(panel, "Gear", true, 5, 1);
            panel.SetColumnSpan(panel.Controls[panel.Controls.Count - 1], 1);
            panel.Controls[panel.Controls.Count - 1].Dock = DockStyle.Fill;
            AddLabel(panel, "Formula", true, 6, 0);
            panel.SetColumnSpan(panel.Controls[panel.Controls.Count - 1], 2);
            panel.Controls[panel.Controls.Count - 1].Dock = DockStyle.Fill;
            AddLabel(panel, "Pinion", true, 6, 1);
            panel.SetColumnSpan(panel.Controls[panel.Controls.Count - 1], 1);
            panel.Controls[panel.Controls.Count - 1].Dock = DockStyle.Fill;
            AddLabel(panel, "Gear", true, 7, 1);
            panel.SetColumnSpan(panel.Controls[panel.Controls.Count - 1], 1);
            panel.Controls[panel.Controls.Count - 1].Dock = DockStyle.Fill;
        // }
    }

    private void AddVariableRows(TableLayoutPanel panel)
    {
        // Group variables by GroupId (or Name if GroupId is null)
        var groupedVariables = _gear.GearVariables.Values
            .GroupBy(variable => variable.GroupId ?? variable.Name.ToString())
            .Select(g => new
            {
                GroupId = g.Key, // Unique ID of the group (e.g., "Addendum")
                Variables = g.ToList(), // List of variables in the group
                GroupSize = g.Count() // Count members in the group
            })
            .ToList();

        var row = 2; // Start from the row after the header
       // var groupSize = groupedVariables.Count;
       
        foreach (var group in groupedVariables)
        {
            var colspan = 1;
            if (group.GroupSize == 1)
            {
                colspan = 2;
            }

            // Add a label for the group (e.g., "Addendum")
            AddLabel(panel, group.GroupId, true, NameColumn, row);
            var uiColumn1 = ValueColumn1;
            var symbolColumn1 = SymbolColumn1;
            var formulaColumn1 = FormulaColumn1;
            // Create layout for variables in the group, set size accordingly
            foreach (var variable in group.Variables)
            {
                AddImageIfExists(panel, LaTeXUtils.CreateImageFromLatex(variable.Symbol), symbolColumn1++, row, colspan);
                AddImageIfExists(panel, LaTeXUtils.CreateImageFromLatex(variable.Formula), formulaColumn1++, row, colspan);
                AddVariableUiControl(panel, variable, row, uiColumn1++, colspan); // Add the variable's input control
            }

            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Adding style dynamically for the row
            row++; // Increment the row counter after all variables in the group are handled
        }
    }



    private void AddVariableUiControl(TableLayoutPanel panel, GearVariable variable, int row, int col, int colspan)
    {
        if (variable.IsDesign)
        {
            AddNumericUpDown(panel, variable, row, col, colspan);
        }
        else
        {
            AddVariableValueLabel(panel, variable, row, col, colspan);
        }
    }


    private static void AddVariableValueLabel(TableLayoutPanel panel, GearVariable variable, int row, int col, int colspan)
    {
        Label valueLabel;
        switch (variable.ValueType)
        {
            case "Angle":
            {
                valueLabel = CreateLabel(
                    ConversionUtils.Degrees(variable.Value).ToString("F4", CultureInfo.CurrentCulture),
                    true, null);
                var bind = new Binding("Text", variable, "Value");
                bind.Format += bind_Format;
                bind.Parse += bind_Parse;
                valueLabel.DataBindings.Add(bind);
                break;
            }
            default:
                valueLabel = CreateLabel(variable.Value.ToString(CultureInfo.CurrentCulture), true, null);
              
                valueLabel.DataBindings.Add(new Binding("Text", variable, "Value", true, DataSourceUpdateMode.OnPropertyChanged)
                {
                    FormatString = "F4" // Automatically format as 4 decimal places
                });

                break;
        }
        
        panel.Controls.Add(valueLabel, ValueColumn1, row);
        panel.SetColumnSpan(valueLabel, colspan);
    }

    private static Label CreateLabel(string text, bool autoSize, Size? minSize = null)
    {
        return new Label
        {
            Text = text,
            AutoSize = autoSize,
            MinimumSize = minSize ?? Size.Empty,
            Font = new Font(FontFamily.GenericMonospace, 12),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 20, 0)
        };
    }

    private static void bind_Format(object sender, ConvertEventArgs e)
    {
        var radians = (double)e.Value;
        e.Value = ConversionUtils.Degrees(radians).ToString("F4", CultureInfo.CurrentCulture);

    }

    private static void bind_Parse(object sender, ConvertEventArgs e)
    {
        var degrees = (double)e.Value;
        e.Value = ConversionUtils.Radians(degrees).ToString("F4", CultureInfo.CurrentCulture);

    }

    private static void AddLabel(TableLayoutPanel panel, string text, bool autoSize, int col, int row,
        Size? minSize = null)
    {
        var label = CreateLabel(text, autoSize, minSize);
      //  panel.SetColumnSpan(label, colspan);
        panel.Controls.Add(label, col, row);
    }

    private static Label CreateLabel(string text, bool autoSize)
    {
        return new Label
        {
            Text = text,
            AutoSize = autoSize,
            Font = new Font(FontFamily.GenericMonospace, 12),
            Margin = new Padding(10),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private void AddNumericUpDown(TableLayoutPanel panel, GearVariable variable, int row, int col, int colspan)
    {
        var numericUpDown = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 1000000000,
            DecimalPlaces = 2,
            Margin = new Padding(0, 0, 20, 0),
            Font = new Font(FontFamily.GenericMonospace, 14),
            Size = new Size(100, 30),
            Dock = DockStyle.Fill
        };


        numericUpDown.Value = variable.ValueType.Equals("Angle")
            ? (decimal)ConversionUtils.Degrees(variable.Value)
            : (decimal)variable.Value;

        numericUpDown.ValueChanged += (sender, args) =>
        {
            variable.Value = variable.ValueType.Equals("Angle")
                ? ConversionUtils.Radians((double)numericUpDown.Value)
                : (double)numericUpDown.Value;

            _gear.Calculate();
        };
        panel.SetColumnSpan(numericUpDown, colspan);
        panel.Controls.Add(numericUpDown, col, row);
    }

    private void AddImageIfExists(TableLayoutPanel panel, Image image, int column, int row, int colspan)
    {
        if (image != null)
        {
            var pb = CreatePictureBox(image);
            pb.Dock = DockStyle.Fill;
            panel.SetColumnSpan(pb, colspan);
            panel.Controls.Add(pb, column, row);
        }
    }

    private static PictureBox CreatePictureBox(Image image)
    {
        return new PictureBox
        {
            Image = image,
            SizeMode = PictureBoxSizeMode.CenterImage,
            Margin = new Padding(10)
        };
    }
}