using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Windows.Forms;
using Bolsover.Gears.Builder;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class DataBuilder
    {
        [Test]
        public void Test()
        {
            GearContext ctx = GearContext.ProfileShiftedExternalInvolutePair;
            var variables = new List<GearVariable>();
            const string connectionString = @"Data Source=.\Data\Nexus.db";
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            // Prepare the SQL query
            var contextKey = ctx.ToString();
            var query =
                $"SELECT Context, VariableName, Value, Symbol, Formula, isDesign, Ordinal, NCalc, ValueType, CalcMethod, GroupId FROM GearVariables where Context = '{contextKey}' order by Ordinal";
            using var command = new SQLiteCommand(query, connection);
            // Execute the query and read the results
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                GearVariableBuilder builder = new();

                var context = (GearContext)Enum.Parse(typeof(GearContext),
                    GetReaderValue(reader, "Context", reader.GetString));
                var variableName = (VariableNames)Enum.Parse(typeof(VariableNames),
                    GetReaderValue(reader, "VariableName", reader.GetString));
                var value = GetReaderValue(reader, "Value", reader.GetDouble);
                var symbol = GetReaderValue(reader, "Symbol", reader.GetString);
                var formula = GetReaderValue(reader, "Formula", reader.GetString);
                var isDesign = GetReaderValue(reader, "IsDesign", reader.GetInt16) == 1;
                var ordinal = GetReaderValue(reader, "Ordinal", reader.GetInt32);
                var nCalc = GetReaderValue(reader, "NCalc", reader.GetString, null);
                var valueType = GetReaderValue(reader, "ValueType", reader.GetString, null);
                var calcMethod = GetReaderValue(reader, "CalcMethod", reader.GetString, "");
                var groupId = GetReaderValue(reader, "GroupId", reader.GetString, "");


                builder.SetGearContext(context)
                    .SetVariableName(variableName)
                    .SetValue(value)
                    .SetSymbol(symbol)
                    .SetFormula(formula)
                    .SetIsDesgn(isDesign)
                    .SetOrdinal(ordinal)
                    .SetNCalc(nCalc)
                    .SetValueType(valueType)
                    .SetCalcMethod(calcMethod)
                    .SetGroupId(groupId);

                variables.Add(builder.Build());
            }


// var row = 3;
//
//             foreach (var v in variables)
//             {
//                 var variableKey = v.Name.ToString();
//                 var elementKey = "N1";
//                 
//                 var rowspan = 1;
//                 var col = 1;
//                 var colspan = 1;
//                 var defaultValue = 0;
//                 var minValue = 0;
//                 var maxValue = 0;
//                 var stepSize = 0;
//                 var insert =
//                     @"insert into Display (Context, VariableName, DisplayElement, DisplayText, Row, RowSpan, Col, ColSpan, DefaultValue, MinValue, MaxValue, StepSize) VALUES" + 
//                     $"('{contextKey}', '{variableKey}', '{elementKey}', '{variableKey}', {row},{rowspan},{col},{colspan},{defaultValue},{minValue},{maxValue},{stepSize});";
//                 ConsoleIO.WriteLine(insert);
//                 elementKey = "S1";
//                col = 2;
//                 insert =
//                     @"insert into Display (Context, VariableName, DisplayElement, DisplayText, Row, RowSpan, Col, ColSpan, DefaultValue, MinValue, MaxValue, StepSize) VALUES" + 
//                     $"('{contextKey}', '{variableKey}', '{elementKey}', '{variableKey}', {row},{rowspan},{col},{colspan},{defaultValue},{minValue},{maxValue},{stepSize});";
//                 ConsoleIO.WriteLine(insert);
//                 elementKey = "S2";
//                 col = 3;
//                 insert =
//                     @"insert into Display (Context, VariableName, DisplayElement, DisplayText, Row, RowSpan, Col, ColSpan, DefaultValue, MinValue, MaxValue, StepSize) VALUES" + 
//                     $"('{contextKey}', '{variableKey}', '{elementKey}', '{variableKey}', {row},{rowspan},{col},{colspan},{defaultValue},{minValue},{maxValue},{stepSize});";
//                 ConsoleIO.WriteLine(insert);
//                 elementKey = "V1";
//                 col = 4;
//                 insert =
//                     @"insert into Display (Context, VariableName, DisplayElement, DisplayText, Row, RowSpan, Col, ColSpan, DefaultValue, MinValue, MaxValue, StepSize) VALUES" + 
//                     $"('{contextKey}', '{variableKey}', '{elementKey}', '{variableKey}', {row},{rowspan},{col},{colspan},{defaultValue},{minValue},{maxValue},{stepSize});";
//                 ConsoleIO.WriteLine(insert);
//                 elementKey = "V2";
//                 col = 5;
//                 insert =
//                     @"insert into Display (Context, VariableName, DisplayElement, DisplayText, Row, RowSpan, Col, ColSpan, DefaultValue, MinValue, MaxValue, StepSize) VALUES" + 
//                     $"('{contextKey}', '{variableKey}', '{elementKey}', '{variableKey}', {row},{rowspan},{col},{colspan},{defaultValue},{minValue},{maxValue},{stepSize});";
//                 ConsoleIO.WriteLine(insert);
//                 elementKey = "F2";
//                 col = 6;
//                 insert =
//                     @"insert into Display (Context, VariableName, DisplayElement, DisplayText, Row, RowSpan, Col, ColSpan, DefaultValue, MinValue, MaxValue, StepSize) VALUES" + 
//                     $"('{contextKey}', '{variableKey}', '{elementKey}', '{variableKey}', {row},{rowspan},{col},{colspan},{defaultValue},{minValue},{maxValue},{stepSize});";
//                 ConsoleIO.WriteLine(insert);
//                 elementKey = "F2";
//                 col = 7;
//                 insert =
//                     @"insert into Display (Context, VariableName, DisplayElement, DisplayText, Row, RowSpan, Col, ColSpan, DefaultValue, MinValue, MaxValue, StepSize) VALUES" + 
//                     $"('{contextKey}', '{variableKey}', '{elementKey}', '{variableKey}', {row},{rowspan},{col},{colspan},{defaultValue},{minValue},{maxValue},{stepSize});";
//                 ConsoleIO.WriteLine(insert);
//                 
//                 row++;
//             }
            

            // Group variables by GroupId (or Name if GroupId is null)
            var groupedVariables = variables
                .GroupBy(variable => variable.GroupId ?? variable.Name.ToString())
                .Select(g => new
                {
                    GroupId = g.Key, // Unique ID of the group (e.g., "Addendum")
                    Variables = g.ToList(), // List of variables in the group
                    GroupSize = g.Count() // Count members in the group
                })
                .ToList();

            //  var row = 2; // Start from the row after the header
            // var groupSize = groupedVariables.Count;

            foreach (var group in groupedVariables)
            {
                var row = 3;
                var rowspan = 1;
           
                var colspan = 1;
            
                var minValue = 0;
                var maxValue = 0;
                var stepSize = 0;
                
                var variable = group.Variables[0];
                var col = 1;
                var variableKey = variable.Name.ToString();
                var elementKey = "N1";
                var id = group.GroupId;
                var defaultValue = variable.Value;
                // Add a label for the group (e.g., "Addendum")
                var insert =
                    @"insert into Display (Context, VariableName, DisplayElement, DisplayText, Row, RowSpan, Col, ColSpan, DefaultValue, MinValue, MaxValue, StepSize) VALUES" +
                    $"('{contextKey}', '{variableKey}', '{elementKey}', '{id}', {row},{rowspan},{col},{colspan},{defaultValue},{minValue},{maxValue},{stepSize});";
                ConsoleIO.WriteLine(insert);
                
                if (group.GroupSize == 1)
                {
                    colspan = 2;
                }
                

                elementKey = "S1";
                col = 2;
                insert =
                    @"insert into Display (Context, VariableName, DisplayElement, DisplayText, Row, RowSpan, Col, ColSpan, DefaultValue, MinValue, MaxValue, StepSize) VALUES" +
                    $"('{contextKey}', '{variableKey}', '{elementKey}', '{id}', {row},{rowspan},{col},{colspan},{defaultValue},{minValue},{maxValue},{stepSize});";
                ConsoleIO.WriteLine(insert);
                elementKey = "V1";
                col = 4;
                insert =
                    @"insert into Display (Context, VariableName, DisplayElement, DisplayText, Row, RowSpan, Col, ColSpan, DefaultValue, MinValue, MaxValue, StepSize) VALUES" +
                    $"('{contextKey}', '{variableKey}', '{elementKey}', '{id}', {row},{rowspan},{col},{colspan},{defaultValue},{minValue},{maxValue},{stepSize});";
                ConsoleIO.WriteLine(insert);
                elementKey = "F1";
                col = 6;
                insert =
                    @"insert into Display (Context, VariableName, DisplayElement, DisplayText, Row, RowSpan, Col, ColSpan, DefaultValue, MinValue, MaxValue, StepSize) VALUES" +
                    $"('{contextKey}', '{variableKey}', '{elementKey}', '{id}', {row},{rowspan},{col},{colspan},{defaultValue},{minValue},{maxValue},{stepSize});";
                ConsoleIO.WriteLine(insert);
                if (group.GroupSize == 2)
                {
                    variable = group.Variables[1];
                    variableKey = variable.Name.ToString();
                    elementKey = "S2";
                    col = 3;
                    insert =
                        @"insert into Display (Context, VariableName, DisplayElement, DisplayText, Row, RowSpan, Col, ColSpan, DefaultValue, MinValue, MaxValue, StepSize) VALUES" +
                        $"('{contextKey}', '{variableKey}', '{elementKey}', '{id}', {row},{rowspan},{col},{colspan},{defaultValue},{minValue},{maxValue},{stepSize});";
                    ConsoleIO.WriteLine(insert);
                    elementKey = "V2";
                    col = 5;
                    insert =
                        @"insert into Display (Context, VariableName, DisplayElement, DisplayText, Row, RowSpan, Col, ColSpan, DefaultValue, MinValue, MaxValue, StepSize) VALUES" +
                        $"('{contextKey}', '{variableKey}', '{elementKey}', '{id}', {row},{rowspan},{col},{colspan},{defaultValue},{minValue},{maxValue},{stepSize});";
                    ConsoleIO.WriteLine(insert);
                    elementKey = "F2";
                    col = 7;
                    insert =
                        @"insert into Display (Context, VariableName, DisplayElement, DisplayText, Row, RowSpan, Col, ColSpan, DefaultValue, MinValue, MaxValue, StepSize) VALUES" +
                        $"('{contextKey}', '{variableKey}', '{elementKey}', '{id}', {row},{rowspan},{col},{colspan},{defaultValue},{minValue},{maxValue},{stepSize});";
                    ConsoleIO.WriteLine(insert);
                }

                // foreach (var variable in group.Variables)
                // {
                //     AddImageIfExists(panel, LaTeXUtils.CreateImageFromLatex(variable.Symbol), symbolColumn1++, row, colspan);
                //     AddImageIfExists(panel, LaTeXUtils.CreateImageFromLatex(variable.Formula), formulaColumn1++, row, colspan);
                //     AddVariableUiControl(panel, variable, row, uiColumn1++, colspan); // Add the variable's input control
                // }
                //
                // panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Adding style dynamically for the row
                row++; // Increment the row counter after all variables in the group are handled
            }
        }

        private static T GetReaderValue<T>(DbDataReader reader, string columnName, Func<int, T> valueGetter,
            T defaultValue = default)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) ? valueGetter(ordinal) : defaultValue;
        }
    }
}