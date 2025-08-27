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
public class Gear : IVariableResolver
{
    // Constants
    private const string BracketsRegexPattern = @"\[(.*?)\]"; // Extracted constant for regex pattern
    private const double DefaultInitialGuess = 0.5;
    private const double DefaultTolerance = 1e-6;
    private const int DefaultMaxIterations = 100;

    // Properties
    public string Name { get; set; }
    public CalculationMode Mode { get; set; }
    public GearContext Context { get; private set; }
    public List<Gear> Gears { get; } = new();
    /// <summary>
    /// Represents a collection of gear-related variables, where each variable is
    /// identified by a unique name and represented as a key-value pair. The keys
    /// are of type <see cref="VariableNames"/>, and the values are instances of
    /// <see cref="GearVariable"/>. This property provides access to a dictionary
    /// that encapsulates the state and characteristics of a gear's behavior or
    /// configuration.
    /// </summary>
    public Dictionary<VariableNames, GearVariable> GearVariables { get; } = new();

    /// <summary>
    /// Represents a collection of calculation-related variables used within the context of
    /// NCalc, where each variable is identified by a string key and its corresponding value
    /// is associated with <see cref="VariableNames"/>. This property provides a mechanism
    /// to manage and access variables essential for expression evaluation and dynamic
    /// computations.
    /// </summary>
    protected Dictionary<string, VariableNames> NCalcVariables { get; } = new();

    private Gear ParentGear { get; set; }

    /// <summary>
    /// Adds a gear to this set and wires the resolver so it can use set-level and cross-gear variables.
    /// </summary>
    private void AddGear(Gear gear)
    {
        if (gear == null || Gears.Contains(gear)) return;
        gear.ParentGear = this;
        Gears.Add(gear);
        gear.VariableResolver = this;
    }


    /// <summary>
    /// Represents a Gear object with associated context, calculation mode, and variable management.
    /// </summary>
    public Gear(GearContext context, CalculationMode mode)
    {
        Mode = mode;
        Context = context;
        // Load grouped variables from DB
        var grouped = Data.GearVariableLoader.LoadGroupedGearVariables(context);

        // Add set-level variables
        foreach (var v in grouped.Common)
        {
            AddVariable(v);
        }

        // Create gears with deferred loading and add them to the set
        var gear1 = new Gear(context, mode, false);
        var gear2 = new Gear(context, mode, false);
        AddGear(gear1);
        AddGear(gear2);

        // Populate per-gear variables
        foreach (var v in grouped.Gear1)
        {
            gear1.AddVariable(v);
        }

        foreach (var v in grouped.Gear2)
        {
            gear2.AddVariable(v);
        }

        VariableResolver = this;
    }

    /// <summary>
    /// Constructor that allows deferring the automatic loading of variables. Useful when the gear is part of a GearSet
    /// and variables are supplied externally (e.g., from a grouped loader).
    /// </summary>
    public Gear(GearContext context, CalculationMode mode, bool autoLoad)
    {
        Mode = mode;
        Context = context;

        if (autoLoad)
        {
            LoadAndInitializeGearVariables();
        }
    }




    /// <summary>
    /// Initializes the gear variables by loading them from the specified gear context and adds
    /// them to the gear's internal variable dictionary for further calculations and management.
    /// </summary>
    private void LoadAndInitializeGearVariables()
    {
        GearVariableLoader.LoadGearVariables(Context).ToList().ForEach(AddVariable);
    }


    /// <summary>
    /// Adds a GearVariable to the GearVariables dictionary and updates associated event handlers and NCalc variable mappings.
    /// </summary>
    /// <param name="variable">The GearVariable to be added. This variable contains properties including its name and whether it is a design variable.</param>
    internal void AddVariable(GearVariable variable)
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
    public double CalculateVariable(VariableNames name)
    {
        var variable = FindVariable(name);
        if (variable == null) return 0;

        variable.Value = EvaluateVariableValue(variable);
        return variable.Value;
    }


    private GearVariable FindVariable(VariableNames name) =>
        GearVariables.TryGetValue(name, out var variable) ? variable : null;


    /// <summary>
    /// Returns the value of a gear variable based on its name and expression.
    /// </summary>
    /// <param name="variable"></param>
    /// <returns></returns>
    private double EvaluateVariableValue(GearVariable variable)
    {
        if (variable.IsDesign)
        {
            return variable.Value;
        }

        switch (variable.CalcMethod)
        {
            case "NCalc":
                EvaluateNCalcExpression(variable);
                break;
            case "PressureAngleByNewtonRaphson":
                variable.Value = CustomCalculations.PressureAngleByNewtonRaphson(
                    CalculateVariable(VariableNames.WorkingInvoluteFunction),
                    DefaultInitialGuess,
                    DefaultTolerance,
                    DefaultMaxIterations);
                break;
        }

        return variable.Value;
    }


    /// <summary>
    /// Evaluates a mathematical expression defined in the provided GearVariable
    /// using the NCalc library and updates its Value property with the result.
    /// </summary>
    /// <param name="variable">The GearVariable instance containing the expression to evaluate.</param>
    private void EvaluateNCalcExpression(GearVariable variable)
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
            CalculateVariable
                (variableEntry.Name);
        }

// calculate all the child gears
        foreach (var gear in Gears)
        {
            foreach (var variableEntry in gear.GearVariables.Values)
            {
                gear.CalculateVariable
                    (variableEntry.Name);
            }
        }
    }

    /// <summary>
    /// Assigns parameters to the given NCalc expression based on the specified string formula,
    /// linking variable names within the expression to their corresponding values or constants.
    /// </summary>
    /// <param name="expression">The NCalc expression object to which parameters will be assigned.</param>
    /// <param name="nCalc">The string representation of the formula, containing variable placeholders in brackets.</param>
    private IVariableResolver VariableResolver { get; set; }

    private void AssignExpressionParameters(Expression expression, string nCalc)
    {
        foreach (var parameter in GetStringsWithinBrackets(nCalc))
        {
            if (parameter == "pi")
            {
                expression.Parameters[parameter] = Math.PI;
                continue;
            }

            // Try resolve from this gear's known NCalcVariables mapping first
            if (NCalcVariables != null && NCalcVariables.TryGetValue(parameter, out var varName))
            {
                var localVar = GetVariable(varName);
                if (localVar != null)
                {
                    expression.Parameters[parameter] = localVar.Value;
                    continue;
                }
            }


            // Fallback to external resolver (e.g., GearSet) if available
            if (VariableResolver != null)
            {
                var resolvedVal = VariableResolver.ResolveByNCalcSymbol(parameter, this);
                if (resolvedVal != null)
                {
                    expression.Parameters[parameter] = resolvedVal;
                    continue;
                }
            }

            // If still unresolved, leave as null (NCalc may throw, which we log upstream)
            expression.Parameters[parameter] = null;
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

    /// <summary>
    /// Resolve a value by symbol from set-level variables or any gear in the set,
    /// prioritizing the current gear, then other gears.
    /// </summary>
    public double? ResolveByNCalcSymbol(string symbol, Gear current)
    {
        symbol = "[" + symbol + "]"; // NCalc requires brackets around symbols, e.g., "[z1]"
        // Try set-level variables via NCalcVariables mapping first
        if (NCalcVariables.TryGetValue(symbol, out var setVarName))
        {
            var setVar = GetVariable(setVarName);
            if (setVar != null) return setVar.Value;
        }

        // Prefer the current gear, if provided
        if (current != null)
        {
            var curMatch = current.GearVariables?.Values?.FirstOrDefault(v => v.NCalc == symbol);
            if (curMatch != null) return curMatch.Value;
        }

        // Search other gears
        foreach (var g in Gears)
        {
            if (g == null || ReferenceEquals(g, current)) continue;
            var match = g.GearVariables?.Values?.FirstOrDefault(v => v.NCalc == symbol);
            if (match != null) return match.Value;
        }

        // Search parent
        if (current != null && current.ParentGear != null)
        {
            var parentMatch = current.ParentGear.GearVariables?.Values?.FirstOrDefault(v => v.NCalc == symbol);
            if (parentMatch != null) return parentMatch.Value;
        }

        // Search parent children
        foreach (var g in current.ParentGear.Gears)
        {
            if (g == null || ReferenceEquals(g, current)) continue;
            var childMatch = g.GearVariables?.Values?.FirstOrDefault(v => v.NCalc == symbol);
            if (childMatch != null) return childMatch.Value;
        }

        return null;
    }
}