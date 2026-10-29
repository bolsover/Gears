using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Data;

namespace Bolsover.Gears.Model;

/// <summary>
/// Represents a gear model, providing tools to manage and calculate gear variables
/// based on the given context and calculation mode.
/// </summary>
public class Gear
{
    // Properties
    public string Id { get; set; }
    public CalculationMode Mode { get; set; }
    public GearTypes Context { get; private set; }
    public List<Gear> Gears { get; } = new();

    private readonly GearCalculator _calculator = new();

    /// <summary>
    /// Represents a collection of gear-related variables, where each variable is
    /// identified by a unique id and represented as a key-value pair. The keys
    /// are of type <see cref="VariableIds"/>, and the values are instances of
    /// <see cref="GearVariable"/>. This property provides access to a dictionary
    /// that encapsulates the state and characteristics of a gear's behavior or
    /// configuration.
    /// </summary>
    public Dictionary<VariableIds, GearVariable> GearVariables { get; } = new();

    private Gear ParentGear { get; set; }

    /// <summary>
    /// Adds a gear to this set and wires the resolver so it can use set-level and cross-gear variables.
    /// </summary>
    private void AddGear(Gear gear)
    {
        if (gear == null || Gears.Contains(gear)) return;
        gear.ParentGear = this;
        Gears.Add(gear);
        gear.VariableResolver = _calculator;
    }


    /// <summary>
    /// Represents a Gear object with associated context, calculation mode, and variable management.
    /// </summary>
    public Gear(GearTypes context, CalculationMode mode)
    {
        Mode = mode;
        Context = context;
        // Load grouped variables from DB (now supports N named gears)
        var grouped = GearVariableLoader.LoadGroupedGearVariables(context);

        // Add set-level variables
        foreach (var v in grouped.Common)
        {
            AddVariable(v);
        }

        // Populate per-gear variables
        if (grouped.PerGear != null && grouped.PerGear.Count > 0)
        {
            if (grouped.PerGear.Count == 1)
            {
                // Backward-compatible: if there is only one named gear group, flatten into root
                var only = grouped.PerGear.First();
                foreach (var v in only.Value)
                {
                    AddVariable(v);
                }
            }
            else
            {
                // Create named child gears and populate their variables
                // Ensure deterministic order: Gear1, Gear2, Gear3... then by id
                IEnumerable<KeyValuePair<string, List<GearVariable>>> ordered = grouped.PerGear
                    .OrderBy(kvp => TryParseGearIndex(kvp.Key) ?? int.MaxValue)
                    .ThenBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in ordered)
                {
                    var child = new Gear(context, mode, false) { Id = kvp.Key };
                    AddGear(child);
                    foreach (var v in kvp.Value)
                    {
                        child.AddVariable(v);
                    }
                }
            }
        }

        VariableResolver = _calculator;
    }

    /// <summary>
    /// Constructor that allows deferring the automatic loading of variables. Useful when the gear is part of a GearSet
    /// and variables are supplied externally (e.g., from a grouped loader).
    /// </summary>
    private Gear(GearTypes context, CalculationMode mode, bool autoLoad)
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
    /// <param id="variable">The GearVariable to be added. This variable contains properties including its id and whether it is a design variable.</param>
    private void AddVariable(GearVariable variable)
    {
        if (variable.IsDesign)
        {
            variable.PropertyChanged += OnDesignVariableChanged;
        }

        GearVariables.Add(variable.Id, variable);
    }

    /// <summary>
    /// Retrieves a gear variable by its id from the collection of gear variables.
    /// Returns null if the specified variable id does not exist.
    /// </summary>
    /// <param id="id">The id of the variable to retrieve.</param>
    /// <returns>The corresponding <see cref="GearVariable"/> if found, otherwise null.</returns>
    public GearVariable GetVariable(VariableIds id) =>
        GearVariables.TryGetValue(id, out var variable) ? variable : null;

    /// <summary>
    /// Calculates the value of a gear variable based on its id and expression.
    /// Delegates to external calculator to avoid mixing calculations into model
    /// </summary>
    /// <param id="id">The id of the variable to calculate.</param>
    /// <returns>Returns the calculated value of the specified variable.</returns>
    public double CalculateVariable(VariableIds id)
    {
        return _calculator.CalculateVariable(this, id);
    }
    
    public string CalculateDisplayImperialValue(VariableIds id)
    {
        return _calculator.CalculateDisplayImperialValue(this, id);
    }
    
    public string CalculateDisplayMetricValue(VariableIds id)
    {
        return _calculator.CalculateDisplayMetricValue(this, id);
    }


    /// <summary>
    /// Performs calculations for all gear variables within the current gear instance.
    /// Iterates through all stored gear variables and calculates each one based on the
    /// current gear context and calculation mode.
    /// </summary>
    public void Calculate()
    {
        _calculator.CalculateAll(this);
    }


    /// <summary>
    /// Assigns parameters to the given NCalc expression based on the specified string formula,
    /// linking variable names within the expression to their corresponding values or constants.
    /// </summary>
    private IVariableResolver VariableResolver { get; set; }


    /// <summary>
    /// Handles the PropertyChanged event of a design variable and triggers recalculation of all gear variables.
    /// </summary>
    /// <param id="sender">The source of the event, typically the design variable that was changed.</param>
    /// <param id="e">An object containing data about the property change event.</param>
    private void OnDesignVariableChanged(object sender, PropertyChangedEventArgs e)
    {
        Calculate();
    }


    private static int? TryParseGearIndex(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        // Expect names like "Gear1", "Gear2", or plain numbers
        int idx;
        if (name.StartsWith("Gear", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(name.Substring(4), out idx)) return idx;
        }
        else if (int.TryParse(name, out idx))
        {
            return idx;
        }

        return null;
    }
}