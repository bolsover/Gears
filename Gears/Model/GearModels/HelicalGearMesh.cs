using System.Collections.Generic;
using Bolsover.Gears.Model.GearBox;
using Bolsover.Gears.Model.Variable;

namespace Bolsover.Gears.Model.GearModels;

public class HelicalGearMesh : IMesh
{
    public string Name { get; set; }
    public Dictionary<string, IVariable> GearVariables { get; set; }
    public IGear GearA { get; set; }
    public IGear GearB { get; set; }

    public HelicalGearMesh()
    {
        GearVariables = new();
        
    }
}