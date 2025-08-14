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
        InitializeGearVariables();
    }

    public CalculationMode Mode { get; set; }
    private GearContext Context { get; set; }

    /// <summary>
    /// Represents a collection of gear-related variables, where each variable is
    /// identified by a unique name and represented as a key-value pair. The keys
    /// are of type <see cref="VariableNames"/>, and the values are instances of
    /// <see cref="GearVariable"/>. This property provides access to a dictionary
    /// that encapsulates the state and characteristics of a gear's behavior or
    /// configuration.
    /// </summary>
    public Dictionary<VariableNames, GearVariable> GearVariables { get; }

    /// <summary>
    /// Represents a collection of calculation-related variables used within the context of
    /// NCalc, where each variable is identified by a string key and its corresponding value
    /// is associated with <see cref="VariableNames"/>. This property provides a mechanism
    /// to manage and access variables essential for expression evaluation and dynamic
    /// computations.
    /// </summary>
    private Dictionary<string, VariableNames> NCalcVariables { get; }

    /// <summary>
    /// Initializes the gear variables by loading them from the specified gear context and adds
    /// them to the gear's internal variable dictionary for further calculations and management.
    /// </summary>
    private void InitializeGearVariables()
    {
      GearVariableLoader.LoadGearVariables(Context).ToList().ForEach(AddVariable);
    }

    /// <summary>
    /// Adds a GearVariable to the GearVariables dictionary and updates associated event handlers and NCalc variable mappings.
    /// </summary>
    /// <param name="variable">The GearVariable to be added. This variable contains properties including its name and whether it is a design variable.</param>
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
        variable.Value = GetVariableValue(variable);
        return variable.Value;
    }

    /// <summary>
    /// Returns the value of a gear variable based on its name and expression.
    /// </summary>
    /// <param name="variable"></param>
    /// <returns></returns>
    private double GetVariableValue(GearVariable variable)
    {
        if (variable.IsDesign)
            return variable.Value;

        switch (variable.CalcMethod)
        {
            case "NCalc":
                EvaluateNCalc(variable);
                break;
            case "PressureAngleByNewtonRaphson":
                variable.Value = CustomCalculations.PressureAngleByNewtonRaphson(
                    CalculateGearVariable(VariableNames.WorkingInvoluteFunction), initialGuess: 0.5, tolerance: 1e-6,
                    maxIterations: 100);
                break;
        }

        return variable.Value;
    }

    /// <summary>
    /// Evaluates a mathematical expression defined in the provided GearVariable
    /// using the NCalc library and updates its Value property with the result.
    /// </summary>
    /// <param name="variable">The GearVariable instance containing the expression to evaluate.</param>
    private void EvaluateNCalc(GearVariable variable)
    {
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
    }


    /// <summary>
    /// Logs an error that occurred during the evaluation of a formula.
    /// </summary>
    /// <param name="formula">The formula that caused the evaluation error.</param>
    /// <param name="ex">The exception detailing the error that occurred during evaluation.</param>
    private static void LogEvaluationError(string formula, Exception ex)
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
            CalculateGearVariable(variableEntry.Name); 
        }
    }

    /// <summary>
    /// Assigns parameters to the given NCalc expression based on the specified string formula,
    /// linking variable names within the expression to their corresponding values or constants.
    /// </summary>
    /// <param name="expression">The NCalc expression object to which parameters will be assigned.</param>
    /// <param name="nCalc">The string representation of the formula, containing variable placeholders in brackets.</param>
    private void AssignExpressionParameters(Expression expression, string nCalc)
    {
        foreach (var parameter in GetStringsWithinBrackets(nCalc))
        {
            expression.Parameters[parameter] = parameter == "pi"
                ? Math.PI
                : GetVariable(NCalcVariables[parameter])?.Value;
        }
    }

    /// <summary>
    /// Extracts unique strings enclosed within square brackets from the provided input string.
    /// </summary>
    /// <param name="input">The input string from which to extract strings within square brackets.</param>
    /// <returns>An array of unique strings found within square brackets in the input. Returns an empty array if the input is null or empty.</returns>
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

    /// <summary>
    /// Handles the PropertyChanged event of a design variable and triggers recalculation of all gear variables.
    /// </summary>
    /// <param name="sender">The source of the event, typically the design variable that was changed.</param>
    /// <param name="e">An object containing data about the property change event.</param>
    private void OnDesignVariableChanged(object sender, PropertyChangedEventArgs e)
    {
        Calculate();
    }
}