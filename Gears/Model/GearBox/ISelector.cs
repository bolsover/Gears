using System.Collections.Generic;

namespace Bolsover.Gears.Model.GearBox;

public interface ISelector
{
    string Name { get; set; }
    List<IMesh> ControlledMeshes { get; set; }
    IMesh SelectedMesh { get; set; }
}