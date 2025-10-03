using System.Collections.Generic;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Builder;

public interface IDisplayDataBuilder
{
    IEnumerable<DisplayData> BuildDisplayData(Gear gear);

}