using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using Bolsover.Gears.Builder;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Data;

/// <summary>
/// GearVariableLoader is responsible for loading gear variables from a data source
/// based on a specified gear type.
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
    /// <param id="type">The gear context that determines which gear variables to load.</param>
    /// <returns>A list of gear variables corresponding to the specified context.</returns>
    public static List<GearVariable> LoadGearVariables(GearTypes type)
    {
        var variables = new List<GearVariable>();
        const string connectionString = @"Data Source=.\Data\Nexus.db";
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        // Prepare the SQL query
        var typeKey = type.ToString();
        // SELECT * so that databases both before and after Migrations\001_SingleSourceFormula.sql can be read
        var query =
            $"SELECT * FROM GearVariables where GearType = '{typeKey}' order by Ordinal";
        using var command = new SQLiteCommand(query, connection);
        // Execute the query and read the results
        using var reader = command.ExecuteReader();
     //   var overrideColumn = HasColumn(reader, "FormulaOverride") ? "FormulaOverride" : "Formula";
        var hasKeyColumn = HasColumn(reader, "Key");
        while (reader.Read())
        {
            GearVariableBuilder builder = new();

            var gearType =
                (GearTypes)Enum.Parse(typeof(GearTypes), GetReaderValue(reader, "GearType", reader.GetString));
            var variableId = (VariableIds)Enum.Parse(typeof(VariableIds),
                GetReaderValue(reader, "VariableId", reader.GetString));
            var value = GetReaderValue(reader, "Value", reader.GetDouble);
            var symbol = GetReaderValue(reader, "Symbol", reader.GetString);
            var formulaOverride = GetReaderValue(reader, "FormulaOverride", reader.GetString);
            var isDesign = GetReaderValue(reader, "IsDesign", reader.GetInt16) == 1;
            var ordinal = GetReaderValue(reader, "Ordinal", reader.GetInt32);
            var nCalc = GetReaderValue(reader, "NCalc", reader.GetString);
            var valueType = GetReaderValue(reader, "ValueType", reader.GetString);
            var calcMethod = GetReaderValue(reader, "CalcMethod", reader.GetString, "");
            var gearPart = GetReaderValue(reader, "GearPart", reader.GetString, "");
            var displayName = GetReaderValue(reader, "DisplayName", reader.GetString, "");
            var displayMetricFormat = GetReaderValue(reader, "DisplayMetricFormat", reader.GetString, "");
            var displayImperialFormat = GetReaderValue(reader, "DisplayImperialFormat", reader.GetString, "");
            var convertImperial = (ConvertImperial)Enum.Parse(typeof(ConvertImperial),
                GetReaderValue(reader, "ConvertImperial", reader.GetString, "None"));
            var convertMetric = (ConvertMetric)Enum.Parse(typeof(ConvertMetric),
                GetReaderValue(reader, "ConvertMetric", reader.GetString, "None"));
            var displayNote1 = GetReaderValue(reader, "DisplayNote1", reader.GetString, "");
            var displayNote2 = GetReaderValue(reader, "DisplayNote2", reader.GetString, "");
            var isError = GetReaderValue(reader, "IsError", reader.GetInt16) == 0;
            var key = hasKeyColumn ? GetReaderValue(reader, "Key", reader.GetString) : null;
            if (string.IsNullOrWhiteSpace(key)) key = DeriveKey(variableId, symbol, nCalc, gearPart);


            builder.SetGearType(gearType)
                .SetVariableId(variableId)
                .SetKey(key)
                .SetValue(value)
                .SetSymbol(symbol)
                .SetFormulaOverride(formulaOverride)
                .SetIsDesgn(isDesign)
                .SetOrdinal(ordinal)
                .SetNCalc(nCalc)
                .SetValueType(valueType)
                .SetCalcMethod(calcMethod)
                .SetGearPart(gearPart)
                .SetDisplayName(displayName)
                .SetDisplayMetricFormat(displayMetricFormat)
                .SetCovertMetric(convertMetric)
                .SetDisplayImperialFormat(displayImperialFormat)
                .SetCovertImperial(convertImperial)
                .SetDisplayNote1(displayNote1)
                .SetDisplayNote2(displayNote2)
                .SetIsError(isError);


            variables.Add(builder.Build());
        }

        connection.Close();

        ApplyFormulas(variables);
        return variables;
    }

    /// <summary>
    /// Sets the display <see cref="GearVariable.Formula"/> of each variable: its FormulaOverride when present,
    /// otherwise LaTeX generated from its NCalc expression, with each [Key] rendered as that variable's Symbol.
    /// </summary>
    public static void ApplyFormulas(IReadOnlyCollection<GearVariable> variables)
    {
        var symbols = variables
            .Where(v => !string.IsNullOrWhiteSpace(v.Key))
            .GroupBy(v => v.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Symbol, StringComparer.Ordinal);

        foreach (var variable in variables)
        {
            if (!string.IsNullOrWhiteSpace(variable.FormulaOverride))
            {
                variable.Formula = variable.FormulaOverride;
                continue;
            }

            try
            {
                variable.Formula = NCalcLatexRenderer.Render(variable.NCalc,
                    k => symbols.TryGetValue(k, out var s) ? s : null);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Cannot render {variable.GearType}.{variable.Id} NCalc '{variable.NCalc}': {ex.Message}");
                variable.Formula = variable.Symbol;
            }
        }
    }

    /// <summary>
    /// Derives a key for a database that predates the Key column, using the same rules as
    /// Migrations\001_SingleSourceFormula.sql.
    /// </summary>
    private static string DeriveKey(VariableIds id, string symbol, string nCalc, string gearPart)
    {
        if (id == VariableIds.Hand) return "hand" + (gearPart?.Length > 4 ? gearPart.Substring(4) : "");

        var trimmed = nCalc?.Trim() ?? "";
        var key = trimmed.Length > 2 && trimmed[0] == '[' && trimmed.IndexOf(']') == trimmed.Length - 1
            ? trimmed.Substring(1, trimmed.Length - 2)
            : new string((symbol ?? "").Where(c => !@"\{}_ ^':+-".Contains(c)).ToArray());
        return id == VariableIds.WorkingPressureAngleNR ? key + "NR" : key;
    }

    private static bool HasColumn(DbDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    public static List<string> GetGearContexts()
    {
        var contexts = new List<string>();
        const string connectionString = @"Data Source=.\Data\Nexus.db";
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();


        var query =
            $"SELECT distinct GearType FROM GearVariables ";
        using var command = new SQLiteCommand(query, connection);
        // Execute the query and read the results
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var ctx = GetReaderValue(reader, "GearType", reader.GetString);
            contexts.Add(ctx);
        }

        return contexts;
    }

    // Introducing a helper method to abstract the repetitive reader.Get calls
    private static T GetReaderValue<T>(DbDataReader reader, string columnName, Func<int, T> valueGetter,
        T defaultValue = default)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return !reader.IsDBNull(ordinal) ? valueGetter(ordinal) : defaultValue;
    }


    public class GroupedGearVariables
    {
        public List<GearVariable> Common { get; }

        // Map of gear id -> variables for that gear (supports 1..N named gears)
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
    public static GroupedGearVariables LoadGroupedGearVariables(GearTypes ctx)
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