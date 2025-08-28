using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using Bolsover.Gears.Builder;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Data;

/// <summary>
/// GearVariableLoader is responsible for loading gear variables from a data source
/// based on a specified gear context.
/// </summary>
/// <remarks>
/// This class provides functionality to query a database and retrieve a list of
/// <see cref="GearVariable"/> objects associated with a specific gear context.
/// </remarks>
public static class GearVariableLoader
{
    /// <summary>
    /// Loads gear variables associated with the specified gear context.
    /// </summary>
    /// <param name="context">The gear context that determines which gear variables to load.</param>
    /// <returns>A list of gear variables corresponding to the specified context.</returns>
    public static List<GearVariable> LoadGearVariables(GearContext ctx)
    {
        var variables = new List<GearVariable>();
        const string connectionString = @"Data Source=.\Data\Nexus.db";
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        // Prepare the SQL query
        var contextKey = ctx.ToString();
        var query =
            $"SELECT Context, VariableName, Value, Symbol, Formula, isDesign, Ordinal, NCalc, ValueType, CalcMethod, GroupId, GearPart FROM GearVariables where Context = '{contextKey}' order by Ordinal";
        using var command = new SQLiteCommand(query, connection);
        // Execute the query and read the results
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            GearVariableBuilder builder = new();

            var context = (GearContext)Enum.Parse(typeof(GearContext), GetReaderValue(reader, "Context", reader.GetString));
            var variableName = (VariableNames)Enum.Parse(typeof(VariableNames), GetReaderValue(reader, "VariableName", reader.GetString));
            var value = GetReaderValue(reader, "Value", reader.GetDouble);
            var symbol = GetReaderValue(reader, "Symbol", reader.GetString);
            var formula = GetReaderValue(reader, "Formula", reader.GetString);
            var isDesign = GetReaderValue(reader, "IsDesign", reader.GetInt16) == 1;
            var ordinal = GetReaderValue(reader, "Ordinal", reader.GetInt32);
            var nCalc = GetReaderValue(reader, "NCalc", reader.GetString, null);
            var valueType = GetReaderValue(reader, "ValueType", reader.GetString, null);
            var calcMethod = GetReaderValue(reader, "CalcMethod", reader.GetString, "");
            var groupId = GetReaderValue(reader, "GroupId", reader.GetString, "");
            var gearPart = GetReaderValue(reader, "GearPart", reader.GetString, "");
            

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
                .SetGroupId(groupId)
                .SetGearPart(gearPart);

            variables.Add(builder.Build());
           // Console.Out.WriteLine(builder.Build());
        }
        connection.Close();

        return variables;
    }
    
    // Introducing a helper method to abstract the repetitive reader.Get calls
    private static T GetReaderValue<T>(DbDataReader reader, string columnName, Func<int, T> valueGetter, T defaultValue = default)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return !reader.IsDBNull(ordinal) ? valueGetter(ordinal) : defaultValue;
    }

    
    public class GroupedGearVariables
    {
        public List<GearVariable> Common { get; }
        // Map of gear name -> variables for that gear (supports 1..N named gears)
        public Dictionary<string, List<GearVariable>> PerGear { get; }

        public GroupedGearVariables(List<GearVariable> common, Dictionary<string, List<GearVariable>> perGear)
        {
            Common = common ?? new List<GearVariable>();
            PerGear = perGear ?? new Dictionary<string, List<GearVariable>>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Loads and groups variables for a GearContext that may represent a GearSet (e.g., Pair).
    /// Non-suffixed variable names are treated as set-level (common) variables.
    /// Variable names ending with '1' go to Gear1; ending with '2' go to Gear2.
    /// </summary>
    public static GroupedGearVariables LoadGroupedGearVariables(GearContext ctx)
    {
        var all = LoadGearVariables(ctx);
        var common = new List<GearVariable>();
        var perGear = new Dictionary<string, List<GearVariable>>(StringComparer.OrdinalIgnoreCase);

        foreach (var v in all)
        {
            var gearPart = v.GearPart;
            if (!string.IsNullOrWhiteSpace(gearPart) && IsIndexedGearName(gearPart))
            {
                if (!perGear.TryGetValue(gearPart, out var list))
                {
                    list = new List<GearVariable>();
                    perGear[gearPart] = list;
                }
                list.Add(v);
            }
            else
            {
                // Treat empty, null, or non-indexed names as common (backward compatible)
                common.Add(v);
            }
        }

        return new GroupedGearVariables(common, perGear);
    }

    private static bool IsIndexedGearName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (name.StartsWith("Gear", StringComparison.OrdinalIgnoreCase))
        {
            // must be Gear + digits
            var suffix = name.Substring(4);
            return suffix.Length > 0 && suffix.All(char.IsDigit);
        }
        return false;
    }
}