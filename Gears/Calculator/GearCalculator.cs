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

        public void CalculateAll(Gear gear)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            var root = GetRoot(gear);
            var visited = new HashSet<Gear>(ReferenceEqualityComparer.Instance);
            CalculateSubtree(root, visited);
        }

        public double CalculateVariable(Gear gear, VariableNames name)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            var variable = gear.GearVariables.TryGetValue(name, out var v) ? v : null;
            if (variable == null) return 0;
            variable.Value = EvaluateVariableValue(gear, variable);
            return variable.Value;
        }

        private static Gear GetRoot(Gear gear)
        {
            var current = gear;
            var parentProp = typeof(Gear).GetProperty("ParentGear", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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
            if (visited == null) visited = new HashSet<Gear>(ReferenceEqualityComparer.Instance);
            if (!visited.Add(gear)) return;

            foreach (var variable in gear.GearVariables.Values)
            {
                CalculateVariable(gear, variable.Name);
            }

            if (gear.Gears == null) return;
            foreach (var child in gear.Gears)
            {
                CalculateSubtree(child, visited);
            }
        }

        private double EvaluateVariableValue(Gear gear, GearVariable variable)
        {
            if (variable.IsDesign) return variable.Value;

            switch (variable.CalcMethod)
            {
                case "NCalc":
                    EvaluateNCalcExpression(gear, variable);
                    break;
                case "PressureAngleByNewtonRaphson":
                    variable.Value = CustomCalculations.PressureAngleByNewtonRaphson(
                        CalculateVariable(gear, VariableNames.WorkingInvoluteFunction),
                        DefaultInitialGuess,
                        DefaultTolerance,
                        DefaultMaxIterations);
                    break;
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
                var ncalcMapProp = typeof(Gear).GetProperty("NCalcVariables", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var map = (Dictionary<string, VariableNames>)ncalcMapProp?.GetValue(gear);
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

        public double? ResolveByNCalcSymbol(string symbol, Gear currentGear)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return null;
            string formatted = $"[{symbol}]";

            var start = currentGear;
            var visited = new HashSet<Gear>(ReferenceEqualityComparer.Instance);
            var match = FindFirstVariableRecursive(start, formatted, visited);
            if (match != null) return match.Value;

            // search from the root as fallback
            var root = GetRoot(start);
            if (!ReferenceEquals(root, start))
            {
                var visitedFromRoot = new HashSet<Gear>(ReferenceEqualityComparer.Instance);
                var rootMatch = FindFirstVariableRecursive(root, formatted, visitedFromRoot);
                if (rootMatch != null) return rootMatch.Value;
            }

            return null;
        }

        private static GearVariable FindFirstVariableRecursive(Gear gear, string formattedSymbol, HashSet<Gear> visited)
        {
            if (gear == null) return null;
            if (visited != null && !visited.Add(gear)) return null;
            var current = SearchVariables(gear, formattedSymbol);
            if (current != null) return current;
            if (gear.Gears != null)
            {
                foreach (var child in gear.Gears)
                {
                    var found = FindFirstVariableRecursive(child, formattedSymbol, visited);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private static GearVariable SearchVariables(Gear gear, string symbol)
        {
            return gear?.GearVariables?.Values?.FirstOrDefault(v => v.NCalc == symbol);
        }

        // Reference equality comparer copy (internal to calculator) to avoid accessing Gear internals.
        private sealed class ReferenceEqualityComparer : IEqualityComparer<Gear>
        {
            public static readonly ReferenceEqualityComparer Instance = new ();
            public bool Equals(Gear x, Gear y) => ReferenceEquals(x, y);
            public int GetHashCode(Gear obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
