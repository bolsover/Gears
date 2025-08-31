using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;
using Newtonsoft.Json;

namespace Bolsover.Gears.Data
{
    /// <summary>
    /// Provides utilities to export <see cref="GearVariable"/> data from a <see cref="Gear"/> into a structured
    /// JSON format. The output groups variables into "design" and "calculated" collections and includes
    /// the variable name, symbol, formula and value.
    /// </summary>
    public static class GearVariableExporter
    {
        /// <summary>
        /// Serializes all variables of the provided <paramref name="gear"/> into a JSON string.
        /// The JSON structure is:
        /// {
        ///   "context": "...",
        ///   "mode": "...",
        ///   "variables": {
        ///     "design": [ { name, ordinal, symbol, formula, ncalc, value } ],
        ///     "calculated": [ { name, ordinal, symbol, formula, ncalc, value } ]
        ///   }
        /// }
        /// </summary>
        /// <param name="gear">The gear instance whose variables should be exported.</param>
        /// <returns>JSON string representing the gear variables.</returns>
        public static string ToJson(Gear gear)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));

            // Ensure calculated values are up-to-date if in automatic mode.
            if (gear.Mode == CalculationMode.Automatic)
            {
                gear.Calculate();
            }

            var variables = gear.GearVariables.Values.ToList();
            foreach (var v in gear.Gears)
            {
                variables.AddRange(v.GearVariables.Values.ToList());
            }
            
            return ToJson(variables, gearContext: GetContextString(gear), mode: gear.Mode.ToString());
        }

        /// <summary>
        /// Serializes the given collection of variables into a JSON string, grouped by design/calculated.
        /// </summary>
        /// <param name="variables">Collection of variables to serialize.</param>
        /// <param name="gearContext">Optional textual context for the output (e.g., Gear.Context.ToString()).</param>
        /// <param name="mode">Optional calculation mode text.</param>
        /// <returns>JSON string representing the variables.</returns>
        public static string ToJson(IEnumerable<GearVariable> variables, string gearContext = null, string mode = null)
        {
            if (variables == null) throw new ArgumentNullException(nameof(variables));

            var payload = new
            {
                context = gearContext,
                mode,
                variables = new
                {
                    design = variables.Where(v => v.IsDesign).Select(ToDto).ToList(),
                    calculated = variables.Where(v => !v.IsDesign).Select(ToDto).ToList()
                }
            };

            return JsonConvert.SerializeObject(payload, Formatting.Indented);
        }

        /// <summary>
        /// Writes the gear variables JSON to a file.
        /// </summary>
        /// <param name="gear">The gear instance to export.</param>
        /// <param name="filePath">Destination file path.</param>
        public static void ToJsonFile(Gear gear, string filePath)
        {
            var json = ToJson(gear);
            File.WriteAllText(filePath, json);
        }

        private static object ToDto(GearVariable v)
        {
            return new
            {
                name = v.Name.ToString(),
                ordinal = v.Ordinal,
                symbol = v.Symbol,
                formula = v.Formula,
                ncalc = v.NCalc,
                value = v.Value,
                isDesign = v.IsDesign,
                gearPart = v.GearPart
            };
        }

        private static string GetContextString(Gear gear)
        {
            // Gear.Context is private in current model; infer from the first variable's context as a reasonable proxy.
            // All variables loaded for a Gear are from the same context by design.
            var context = gear.GearVariables.Values.FirstOrDefault()?.Context;
            return context?.ToString();
        }
    }
}
