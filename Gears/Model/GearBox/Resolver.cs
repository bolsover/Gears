using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Model.Variable;

namespace Bolsover.Gears.Model.GearBox;

public class Resolver
{
  public double resolve(string symbol, IGear gear, IMesh mesh)
    {
        // Implement logic to resolve the NCalc symbol to a value based on the gear train configuration
        // This is a placeholder implementation and should be replaced with actual logic
        
        // 1 look for variable in the current gear
        // 2 look for variable in current mesh
        // 3 look for variable in meshed gear
        var parameter = ParamWithBrackets(symbol);
        List<IVariable> variables = gear.GearVariables.Values.ToList();
        
        var paramVariable = variables.Find(v => v.NCalc == parameter);
        if (paramVariable == null)
        {
            throw new Exception($"Variable '{parameter}' not found ");
        }
        
        return paramVariable.Value;
    }
    
    private string ParamWithBrackets(string param) => $"[{param}]";
}