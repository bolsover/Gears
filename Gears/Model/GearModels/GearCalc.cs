using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Model.GearBox;
using Bolsover.Gears.Model.Variable;
using NCalc;

namespace Bolsover.Gears.Model.GearModels;

public static class GearCalc
{
    public static  void Calculate(IGear gear)
    {
        var variables = gear.GearVariables.Values.ToList();
        foreach (var v in variables.Where(v => v.Role == VariableRole.Calculated))
        {
            CalculateVariable(v, variables);
        }
    }

    public static void Calculate(IMesh mesh)
    {
        var variables = mesh.GearVariables.Values.ToList();
        variables.AddRange(mesh.GearA.GearVariables.Values);
        variables.AddRange(mesh.GearB.GearVariables.Values);

        foreach (var v in variables.Where(v => v.Role == VariableRole.Calculated))
        {
            CalculateVariable(v, variables);
        }
    }

    private static void CalculateVariable(IVariable variable, List<IVariable> variables)
    {
        var expression = new Expression(variable.NCalc);
       
        foreach (var parameter in GetStringsWithinBrackets(variable.NCalc))
        {
            var val = variables.Find(v => v.NCalc == ParamWithBrackets(parameter))?.Value;

            expression.Parameters[parameter] = val;
        }

        variable.Value = Convert.ToDouble(expression.Evaluate());
    }
    
    
    private static string ParamWithBrackets(string param) => $"[{param}]";
    
    private const string BracketsRegexPattern = @"\[(.*?)\]";

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
}