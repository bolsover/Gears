using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Data;
using NCalc;

namespace Bolsover.Gears.Model;

/// <summary>
/// Represents a gear model, providing tools to manage and calculate gear variables
/// based on the given context and calculation mode.
/// </summary>
public class Gear
{
    private const string BracketsRegexPattern = @"\[(.*?)\]"; // Extracted constant for regex pattern

    /// <summary>
    /// Represents a Gear object with associated context, calculation mode, and variable management.
    /// </summary>
    public Gear(GearContext context, CalculationMode mode)
    {
        Mode = mode;
        Context = context;
        GearVariables = new Dictionary<VariableNames, GearVariable>();
        NCalcVariables = new Dictionary<string, VariableNames>();
        LoadVariables();
    }

    public CalculationMode Mode { get; set; }
    public GearContext Context { get; set; }

    public Dictionary<VariableNames, GearVariable> GearVariables { get; }
    public Dictionary<string, VariableNames> NCalcVariables { get; }

    private void LoadVariables()
    {
        var loader = new GearVariableLoader();
        loader.LoadGearVariables(Context).ToList().ForEach(AddVariable);
    }

    private void AddVariable(GearVariable variable)
    {
        if (variable.IsDesign)
        {
            variable.PropertyChanged += OnDesignVariableChanged;
        }
        GearVariables.Add(variable.Name, variable);
        var fieldInfo = typeof(VariableNames)
            .GetField(variable.Name.ToString(), BindingFlags.Public | BindingFlags.Static);

        if (fieldInfo == null) return;

        var symbolAttribute = fieldInfo.GetCustomAttribute<SymbolAttribute>();
        NCalcVariables.Add(symbolAttribute.Symbol, variable.Name);
    }

    /// <summary>
    /// Retrieves a gear variable by its name from the collection of gear variables.
    /// Returns null if the specified variable name does not exist.
    /// </summary>
    /// <param name="name">The name of the variable to retrieve.</param>
    /// <returns>The corresponding <see cref="GearVariable"/> if found, otherwise null.</returns>
    public GearVariable GetVariable(VariableNames name) =>
        GearVariables.TryGetValue(name, out var variable) ? variable : null;

    /// <summary>
    /// Calculates the value of a gear variable based on its name and expression.
    /// </summary>
    /// <param name="name">The name of the variable to calculate.</param>
    /// <returns>Returns the calculated value of the specified variable.</returns>
    public double CalculateGearVariable(VariableNames name)
    {
        var variable = GetVariable(name);
        if (variable.IsDesign) return variable.Value;

        var expression = new Expression(variable.NCalc);
        AssignExpressionParameters(expression, variable.NCalc);

        try
        {
            variable.Value = (double)expression.Evaluate()!;
        }
        catch (Exception ex)
        {
            LogEvaluationError(variable.NCalc, ex);
            throw;
        }

        return variable.Value;
    }

    private void LogEvaluationError(string formula, Exception ex)
    {
        Console.WriteLine($"Error evaluating formula: {formula}");
        Console.WriteLine($"Exception: {ex}");
    }

    /// <summary>
    /// Performs calculations for all gear variables within the current gear instance.
    /// Iterates through all stored gear variables and calculates each one based on the
    /// current gear context and calculation mode.
    /// </summary>
    public void Calculate()
    {
        foreach (var variableEntry in GearVariables.Values)
        {
            CalculateGearVariable(variableEntry.Name); // Inlined ProcessGearVariable logic
        }
    }

    private void AssignExpressionParameters(Expression expression, string nCalc)
    {
        foreach (var parameter in GetStringsWithinBrackets(nCalc))
        {
            expression.Parameters[parameter] = parameter == "pi"
                ? Math.PI
                : GetVariable(NCalcVariables[parameter])?.Value;
        }
    }

    private static string[] GetStringsWithinBrackets(string input)
    {
        if (string.IsNullOrEmpty(input)) return Array.Empty<string>();

        return Regex.Matches(input, BracketsRegexPattern)
            .Cast<Match>()
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .ToArray();
    }

    public void OnDesignVariableChanged(object sender, PropertyChangedEventArgs e)
    {
        Calculate();
    }
}