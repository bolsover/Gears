using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Bolsover.Gears.Model;
//using Microsoft.Data.Sqlite;


using NUnit.Framework;
// using SQLitePCL;

//using SQLitePCL;


namespace UnitTests
{
    [TestFixture]
    public class SQLitetests
    {

       [Test]
        public void TestConnection()
        {
            ConsoleIO io = new ConsoleIO();
            var variables = new List<GearVariable>();
            string connectionString = "Data Source=D:\\02_Repository\\02_Bolsover\\Gears\\Gears\\Data\\Nexus.db";
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
            
                // Prepare the SQL query
                string query = "SELECT * FROM GearVariables where Context like 'ProfileShiftedExternalRadialHelicalPair%'";
                using (var command = new SQLiteCommand(query, connection))
                {
                    // Execute the query and read the results
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // For example, if the table contains a column "Name" of type TEXT
                            string context = reader.GetString(reader.GetOrdinal("Context"));
                            string variableName = reader.GetString(reader.GetOrdinal("VariableName"));
                            double value = reader.GetDouble(reader.GetOrdinal("Value"));
                            string symbol = reader.GetString(reader.GetOrdinal("Symbol"));
                            string formula = reader.GetString(reader.GetOrdinal("Formula"));
                            string valueType = reader.GetString(reader.GetOrdinal("Valuetype"));
                            int isDesign = reader.GetInt32(reader.GetOrdinal("IsDesign"));
                            var variable = new GearVariable( 
                                (GearContext)Enum.Parse(typeof(GearContext), context),
                                (VariableNames)Enum.Parse(typeof(VariableNames), variableName), 
                                value, 
                                symbol, 
                                formula,
                                isDesign== 1? true: false,
                                valueType);
                           
                            variables.Add(variable);
                            // io.WriteLine($"Context: {context}, Name: {variableName}, Value: {value}, Symbol: {symbol}, Formula: {formula}, IsDesign: {isDesign}");
                        }
                    }
                }
               
            }
            Form form = new TestFormulaVariablesForm2(variables);
            Application.Run(form);
        
        }
        
        
    }
     public class TestFormulaVariablesForm2 : Form
    {
     
        private List<GearVariable> _variables;
        
        public TestFormulaVariablesForm2(List<GearVariable> variables)
        {
            _variables = variables;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "JSON Data Renderer";
            this.Size = new Size(1000, 500);
           // List<GearVariable> variables = LoadVariablesByContext(GearContext.ExternalRadialHelical);
            TableLayoutPanel panel = new TableLayoutPanel()
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10),
                ColumnCount = 6,
                RowCount = _variables.Count + 1,
            };
            var row = 1;
            foreach (var gearVariable in _variables)
            {
                var labelVariableName = new Label()
                {
                    Text = gearVariable.Name.ToString(),
                    AutoSize = true,
                    Margin = new Padding(0, 0, 20, 0),
                };

                var labelValue = new Label()
                {
                    Text = gearVariable.Value.ToString(CultureInfo.CurrentCulture),
                    AutoSize = true,
                    Margin = new Padding(0, 0, 20, 0)
                };

                var symbolImage = LaTeXUtils.CreateImageFromLatex(gearVariable.Symbol);
                var formulaImage = LaTeXUtils.CreateImageFromLatex(gearVariable.Formula);
                panel.Controls.Add(labelVariableName, 0, row);
                panel.Controls.Add(labelValue, 1, row);
                ;
                // panel.Controls.Add(labelValue);

                if (symbolImage != null)
                {
                    var pictureBoxSymbol = new PictureBox()
                    {
                        Image = symbolImage,
                        SizeMode = PictureBoxSizeMode.AutoSize,
                        Margin = new Padding(0, 0, 20, 0)
                    };
                    panel.Controls.Add(pictureBoxSymbol, 2, row);
                }

                if (formulaImage != null)
                {
                    var pictureBoxFormula = new PictureBox()
                    {
                        Image = formulaImage,
                        SizeMode = PictureBoxSizeMode.AutoSize,
                        Margin = new Padding(0, 0, 20, 0)
                    };
                    panel.Controls.Add(pictureBoxFormula, 3, row);
                }

                row++;
            }

            Controls.Add(panel);
        }
    }
}