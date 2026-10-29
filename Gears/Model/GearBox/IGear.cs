using System.Collections.Generic;
using Bolsover.Gears.Model.Variable;

namespace Bolsover.Gears.Model.GearBox;

public interface IGear
{
        string Id { get; set; }
        GearTypes GearType { get; }
        CalculationMode Mode { get; set; }
        Dictionary<string, IVariable> GearVariables { get; set; }
        IMesh Mesh { get; set; } // The mesh to which this gear belongs if any
      
         void Calculate();
         void Validate();
}