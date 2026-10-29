using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Model;
using NCalc;

namespace Bolsover.Gears.Calculator
{
    /// <summary>
    /// Default implementation that performs calculations for Gear variables.
    /// Extracted from the Gear model to keep model focused on data/state.
    /// </summary>
    public class GearCalculator : IGearCalculator, IVariableResolver
    {
        private const string BracketsRegexPattern = @"\[(.*?)\]";
        private const double DefaultInitialGuess = 0.5;
        private const double DefaultTolerance = 1e-6;
        private const int DefaultMaxIterations = 100;

        // Variables currently being evaluated, used to detect circular references between NCalc expressions.
        private readonly HashSet<GearVariable> _evaluating = new();

        public void CalculateAll(Gear gear)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            var root = GetRoot(gear);
            var visited = new HashSet<Gear>(ReferenceEqualityComparer.Instance);
            CalculateSubtree(root, visited);
        }

        public double CalculateVariable(Gear gear, VariableIds id)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            var variable = gear.GearVariables.TryGetValue(id, out var v) ? v : null;
            if (variable == null) return 0;
            variable.Value = EvaluateVariableValue(gear, variable);
            return variable.Value;
        }


        public string CalculateDisplayImperialValue(Gear gear, VariableIds id)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            var variable = gear.GearVariables.TryGetValue(id, out var v) ? v : null;
            if (variable == null) return "";
            variable.DisplayImperialValue = EvaluateDisplayImperialValue(variable);
            return variable.DisplayImperialValue;
        }

        private string EvaluateDisplayImperialValue(GearVariable variable)
        {
            var returnValue = "";
            switch (variable.ConvertImperial)
            {
                case ConvertImperial.None:
                {
                    returnValue = variable.Value.ToString(variable.DisplayImperialFormat);
                    break;
                }
                case ConvertImperial.ToInches:
                {
                    var imperialValue = variable.Value / 25.4;
                    returnValue = imperialValue.ToString(variable.DisplayImperialFormat);
                    break;
                }
                case ConvertImperial.ToDms:
                {
                    var imperialValue = variable.Value * 180 / Math.PI;
                    var dms = ConversionUtils.DDtoDMS(imperialValue);
                    returnValue = dms;
                    break;
                }
                default:
                {
                    returnValue = variable.Value.ToString();
                    break;
                }
            }


            return returnValue;
        }


        public string CalculateDisplayMetricValue(Gear gear, VariableIds id)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            var variable = gear.GearVariables.TryGetValue(id, out var v) ? v : null;
            if (variable == null) return "";
            variable.DisplayMetricValue = EvaluateDisplayMetricValue(variable);
            return variable.DisplayMetricValue;
        }
        
     
        private string EvaluateDisplayMetricValue(GearVariable variable)
        {
            var returnValue = "";
            switch (variable.ConvertMetric)
            {
                case ConvertMetric.None:
                {
                    returnValue = variable.Value.ToString(variable.DisplayMetricFormat);
                    break;
                }
                case ConvertMetric.ToDegrees:
                {
                    var metricValue = variable.Value * (180.0 / Math.PI);
                    returnValue = metricValue.ToString(variable.DisplayMetricFormat);
                    break;
                }
               
                default:
                {
                    returnValue = variable.Value.ToString();
                    break;
                }
            }


            return returnValue;
        }

        private static Gear GetRoot(Gear gear)
        {
            var current = gear ?? throw new ArgumentNullException(nameof(gear));
            var parentProp = typeof(Gear).GetProperty("ParentGear",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            while (true)
            {
                var parent = (Gear)parentProp?.GetValue(current);
                if (parent == null) return current;
                current = parent;
            }
        }

        private void CalculateSubtree(Gear gear, HashSet<Gear> visited)
        {
            if (gear == null) return;
            visited ??= new HashSet<Gear>(ReferenceEqualityComparer.Instance);
            if (!visited.Add(gear)) return;

            foreach (var variable in gear.GearVariables.Values)
            {
                CalculateVariable(gear, variable.Id);
                CalculateDisplayImperialValue(gear, variable.Id);
                CalculateDisplayMetricValue(gear, variable.Id);
            }

            if (gear.Gears == null) return;
            foreach (var child in gear.Gears)
            {
                CalculateSubtree(child, visited);
            }
        }

        private double EvaluateVariableValue(Gear gear, GearVariable variable)
        {
            // A calculated variable whose expression is just its own [Key] behaves as an input.
            if (variable.IsDesign || variable.NCalc?.Trim() == $"[{variable.Key}]") return variable.Value;

            // NCalc expressions may reference other calculated variables, which are evaluated on demand.
            if (!_evaluating.Add(variable))
            {
                throw new InvalidOperationException(
                    $"Circular reference evaluating {variable.GearType}.{variable.Id} [{variable.Key}]");
            }

            try
            {
                switch (variable.CalcMethod)
                {
                    case "NCalc":
                        EvaluateNCalcExpression(gear, variable);
                        break;
                    case "PressureAngleByNewtonRaphson":
                        variable.Value = CustomCalculations.PressureAngleByNewtonRaphson(
                            CalculateVariable(gear, VariableIds.WorkingInvoluteFunction),
                            DefaultInitialGuess,
                            DefaultTolerance,
                            DefaultMaxIterations);
                        break;
                }
            }
            finally
            {
                _evaluating.Remove(variable);
            }

            return variable.Value;
        }


       

        private void EvaluateNCalcExpression(Gear gear, GearVariable variable)
        {
            var expression = new Expression(variable.NCalc);
            AssignExpressionParameters(gear, expression, variable.NCalc);
            try
            {
                variable.Value = (double)expression.Evaluate()!;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error evaluating formula: {variable.NCalc}");
                Console.WriteLine($"Exception: {ex}");
                throw;
            }
        }


        private void AssignExpressionParameters(Gear gear, Expression expression, string nCalc)
        {
            foreach (var parameter in GetStringsWithinBrackets(nCalc))
            {
                if (parameter == "pi")
                {
                    expression.Parameters[parameter] = Math.PI;
                    continue;
                }

                // local variables available on this gear via NCalcVariables map
                var ncalcMapProp = typeof(Gear).GetProperty("NCalcVariables",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var map = (Dictionary<string, VariableIds>)ncalcMapProp?.GetValue(gear);
                if (map != null && map.TryGetValue(parameter, out var varName))
                {
                    var localVar = gear.GetVariable(varName);
                    if (localVar != null)
                    {
                        expression.Parameters[parameter] = localVar.Value;
                        continue;
                    }
                }

                // fallback to resolver (this class implements IVariableResolver and will search the gear tree)
                var resolvedVal = ResolveByNCalcSymbol(parameter, gear);
                if (resolvedVal != null)
                {
                    expression.Parameters[parameter] = resolvedVal;
                    continue;
                }

                expression.Parameters[parameter] = null;
            }
        }

        private static string[] GetStringsWithinBrackets(string input)
        {
            if (string.IsNullOrEmpty(input)) return Array.Empty<string>();
            return System.Text.RegularExpressions.Regex.Matches(input, BracketsRegexPattern)
                .Cast<System.Text.RegularExpressions.Match>()
                .Where(m => m.Success)
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// Resolves an NCalc parameter to the value of the variable with that <see cref="GearVariable.Key"/>,
        /// searching the current gear's subtree and then the whole gear set. A calculated variable is evaluated first.
        /// </summary>
        public double? ResolveByNCalcSymbol(string symbol, Gear currentGear)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return null;

            var start = currentGear;
            var visited = new HashSet<Gear>(ReferenceEqualityComparer.Instance);
            var match = FindFirstVariableRecursive(start, symbol, visited);

            // search from the root as fallback
            var root = GetRoot(start);
            if (match == null && !ReferenceEquals(root, start))
            {
                var visitedFromRoot = new HashSet<Gear>(ReferenceEqualityComparer.Instance);
                match = FindFirstVariableRecursive(root, symbol, visitedFromRoot);
            }

            if (match == null) return null;
            var (owner, variable) = match.Value;
            return EvaluateVariableValue(owner, variable);
        }

        private static (Gear Owner, GearVariable Variable)? FindFirstVariableRecursive(Gear gear, string key,
            HashSet<Gear> visited)
        {
            if (gear == null) return null;
            if (visited != null && !visited.Add(gear)) return null;
            var current = SearchVariables(gear, key);
            if (current != null) return (gear, current);
            if (gear.Gears != null)
            {
                foreach (var child in gear.Gears)
                {
                    var found = FindFirstVariableRecursive(child, key, visited);
                    if (found != null) return found;
                }
            }

            return null;
        }

        private static GearVariable SearchVariables(Gear gear, string key)
        {
            var formatted = $"[{key}]";
            return gear?.GearVariables?.Values?.FirstOrDefault(v => v.Key == key || v.NCalc == formatted);
        }

        // Reference equality comparer copy (internal to calculator) to avoid accessing Gear internals.
        private sealed class ReferenceEqualityComparer : IEqualityComparer<Gear>
        {
            public static readonly ReferenceEqualityComparer Instance = new();
            public bool Equals(Gear x, Gear y) => ReferenceEquals(x, y);
            public int GetHashCode(Gear obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}