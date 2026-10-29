using System.Collections.Generic;

namespace Bolsover.Gears.Model.GearBox


{
    public interface IGearTrain
    {
        List<IGear> Gears { get; set; }
        List<IShaft> Shafts { get; set; }
        List<IMesh> Meshes { get; set; }
        List<ISelector> Selectors { get; set; }
    }
}