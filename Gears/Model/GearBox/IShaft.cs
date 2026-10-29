using System.Collections.Generic;

namespace Bolsover.Gears.Model.GearBox;

/// <summary>
/// Represents a gear shaft, which is a collection of gears.
/// </summary>
public interface IShaft
{
        string Name { get; set; }
        List<IGear> Gears { get; set; }
}