using System.Collections.Generic;
using Bolsover.Gears.Model.Variable;

namespace Bolsover.Gears.Model.GearBox;

/// <summary>
/// Represents a mesh between two gears.
/// The Dictionary<string, IVariable> GearVariables is a dictionary of variables that are shared between the two gears.
/// </summary>
public interface IMesh
{
    string Name { get; set; }
    Dictionary<string, IVariable> GearVariables { get; set; }
    IGear GearA { get; set; }
    IGear GearB { get; set; }
}