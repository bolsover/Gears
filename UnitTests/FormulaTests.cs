using System.Collections.Generic;
using NUnit.Framework;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;
using Binding = System.Windows.Forms.Binding;

namespace UnitTests
{
    [TestFixture]
    public class FormulaTests
    {
        [Test]
        public void TestGearVariables()
        {
            var context = GearTypes.ProfileShiftedExternalNormalHelicalPair;
            var gear = new Gear(context, CalculationMode.Automatic);
            gear.Calculate();
            var variables0 = gear.Gears[0].GearVariables;
          //  var variables1 = gear.Gears[1].GearVariables;
          // var variables = gear.GearVariables;
         
            using Form form = new TestFormulaVariablesForm(variables0, context, gear);
            Application.Run(form);
        }
    }

    public class TestFormulaVariablesForm : Form
    {
        private readonly Dictionary<VariableIds, GearVariable> _variables;
        private readonly GearTypes _context;
        private readonly Gear _gear;
        private const int NameColumn = 0;
        private const int DesignColumn = 1;
        private const int ValueColumn = 2;
        private const int SymbolColumn = 3;
        private const int FormulaColumn = 4;
        private const int FormulaOverrideColumn = 5;
        private const int NCalcColumn = 6;


        public TestFormulaVariablesForm(Dictionary<VariableIds, GearVariable> variables, GearTypes context,
            Gear gear)
        {
            _variables = variables;
            _context = context;
            _gear = gear;
            InitializeComponents();
        }


        private void InitializeComponents()
        {
            SetFormProperties();
            var panel = CreateTableLayoutPanel();
            AddColumnHeaders(panel);
            AddVariableRows(panel);
            Controls.Add(panel);
        }

        private void SetFormProperties()
        {
            Text = $@"Test Data {_context} {_variables.Count} variables";
            Size = new Size(1920, 1020);
        }

        private TableLayoutPanel CreateTableLayoutPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                ColumnCount =7,
                AutoScroll = true,
                RowCount = _variables.Count + 1,
                
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            return panel;
        }

        private static void AddColumnHeaders(TableLayoutPanel panel)
        {
            string[] headers = { "VariableName", "IsDesign", "Value", "Symbol", "Formula", "FormulaOverride","Ncalc" };
            for (var col = 0; col < headers.Length; col++)
            {
                AddLabel(panel, headers[col], true, col, 0);
            }
        }

        

        private void AddVariableUiControl(TableLayoutPanel panel, GearVariable variable, int row)
        {
            if (variable.IsDesign)
            {
                AddNumericUpDown(panel, variable, row);
            }
            else
            {
                AddVariableValueLabel(panel, variable, row);
            }
        }

        private void AddVariableRows(TableLayoutPanel panel)
        {
            var row = 1;
            foreach (var variable in _variables.Select(gearVariable => gearVariable.Value))
            {
                AddLabel(panel, variable.Id.ToString(), true, NameColumn, row);
                AddLabel(panel, variable.IsDesign.ToString(), true, DesignColumn, row);
                AddVariableUiControl(panel, variable, row);
                AddImageIfExists(panel, LaTeXUtils.CreateImageFromLatex(variable.Symbol), SymbolColumn, row);
                AddImageIfExists(panel, LaTeXUtils.CreateImageFromLatex(variable.Formula), FormulaColumn, row);
             //   AddImageIfExists(panel, LaTeXUtils.CreateImageFromLatex(variable.FormulaOverride), FormulaOverrideColumn, row);
                AddLabel(panel, variable.NCalc.ToString(CultureInfo.CurrentCulture), false, NCalcColumn, row,
                    new Size(1000, 70));

                row++;
            }
           
        }

        private void AddNumericUpDown(TableLayoutPanel panel, GearVariable variable, int row)
        {
            var numericUpDown = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 1000000000,
                DecimalPlaces = 2,
                Margin = new Padding(0, 0, 20, 0),
                Font = new Font(FontFamily.GenericMonospace, 14),
                Size =  new Size(100, 30),
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

            panel.Controls.Add(numericUpDown, ValueColumn, row);
        }

        private static void AddVariableValueLabel(TableLayoutPanel panel, GearVariable variable, int row)
        {
            Label valueLabel;
            switch (variable.ValueType)
            {
                case "Angle":
                {
                    valueLabel = CreateLabel(
                        ConversionUtils.Degrees(variable.Value).ToString(CultureInfo.CurrentCulture),
                        true, null);
                    var bind = new Binding("Text", variable, "Value");
                    bind.Format += bind_Format;
                    bind.Parse += bind_Parse;
                    valueLabel.DataBindings.Add(bind);
                    break;
                }
                default:
                    valueLabel = CreateLabel(variable.Value.ToString(CultureInfo.CurrentCulture), true, null);
                    valueLabel.DataBindings.Add(new Binding("Text", variable, "Value"));
                    break;
            }

            panel.Controls.Add(valueLabel, ValueColumn, row);
        }
        private static void bind_Format(object sender, ConvertEventArgs e)
        {
            var radians = (double)e.Value;
            e.Value = ConversionUtils.Degrees(radians);
        }

        private static void bind_Parse(object sender, ConvertEventArgs e)
        {
            var degrees = (double)e.Value;
            e.Value = ConversionUtils.Radians(degrees);
        }

        private static void AddLabel(TableLayoutPanel panel, string text, bool autoSize, int column, int row,
            Size? minSize = null)
        {
            panel.Controls.Add(CreateLabel(text, autoSize, minSize), column, row);
        }

        private void AddImageIfExists(TableLayoutPanel panel, Image image, int column, int row)
        {
            if (image != null)
            {
                panel.Controls.Add(CreatePictureBox(image), column, row);
            }
        }

        private static Label CreateLabel(string text, bool autoSize, Size? minSize = null)
        {
            return new Label
            {
                Text = text,
                AutoSize = autoSize,
                MinimumSize = minSize ?? Size.Empty,
                Font = new Font(FontFamily.GenericMonospace, 12),
                Margin = new Padding(0, 0, 20, 0)
            };
        }

        private PictureBox CreatePictureBox(Image image)
        {
            return new PictureBox
            {
                Image = image,
                SizeMode = PictureBoxSizeMode.AutoSize,
                Margin = new Padding(0, 0, 20, 0)
            };
        }
    }
}