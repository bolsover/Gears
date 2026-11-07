using System;
using System.Linq;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>Which parts besides the gears an assembly export to Alibre adds.</summary>
    public sealed class AssemblyExportOptions
    {
        /// <summary>Add a part for every shaft of the train (a shaft that only carries a rack has none).</summary>
        public bool IncludeShafts { get; set; }

        /// <summary>Add a part for the carrier plate of every planetary stage.</summary>
        public bool IncludeCarriers { get; set; }

        /// <summary>Add a part for every timing belt of the train: a plain strip without teeth.</summary>
        public bool IncludeBelts { get; set; }
    }

    /// <summary>
    /// What an Alibre part for a shaft or a planetary carrier is made of: a circle of the shaft radius on the XY plane, extruded along Z
    /// to the length of the shaft. Needs no Alibre. Lengths are in centimetres, the unit of Alibre part sketches and features.
    /// </summary>
    public sealed class ShaftPartSketch
    {
        public ShaftPartSketch(string partName, double radius, double length, bool isCarrier)
        {
            PartName = partName;
            Radius = radius;
            Length = length;
            IsCarrier = isCarrier;
        }

        public string PartName { get; }

        /// <summary>Radius of the shaft or carrier plate in centimetres.</summary>
        public double Radius { get; }

        /// <summary>Length of the shaft, or thickness of the carrier plate, in centimetres.</summary>
        public double Length { get; }

        /// <summary>True for the plate of a planetary carrier, false for an ordinary shaft.</summary>
        public bool IsCarrier { get; }
    }

    /// <summary>Builds the <see cref="ShaftPartSketch"/> of a shaft or planetary carrier, and tells which shafts can be exported.</summary>
    public static class ShaftPartSketchBuilder
    {
        /// <summary>
        /// True when the shaft has a part of its own: it is a carrier plate, carries no gear, or carries at least one gear that is drawn on a
        /// shaft. A shaft that only carries a rack is not drawn in the 3D view and is not exported either.
        /// </summary>
        public static bool IsExportable(Shaft shaft)
        {
            if (shaft == null) return false;
            return shaft.Mounts.Count == 0 || shaft.Mounts.Any(m => m.Component.NeedsShaftMesh);
        }

        /// <summary>The sketch plan of a shaft. Throws <see cref="InvalidOperationException"/> when the shaft has no positive size.</summary>
        public static ShaftPartSketch Build(Shaft shaft)
        {
            if (shaft == null) throw new ArgumentNullException(nameof(shaft));
            if (shaft.Diameter <= 0) throw new InvalidOperationException($"Shaft '{shaft.Id}': the diameter must be positive.");

            shaft.GetAxialSpan(out var start, out var end);
            var length = end - start;
            if (length <= 0) throw new InvalidOperationException($"Shaft '{shaft.Id}': the length must be positive.");

            var isCarrier = PlanetaryGearSet.IsCarrier(shaft);
            return new ShaftPartSketch(
                (isCarrier ? "Carrier " : "Shaft ") + shaft.Id,
                Q(shaft.Diameter / 2.0),
                Q(length),
                isCarrier);
        }

        /// <summary>The start of the part along the shaft axis in millimetres: the part is extruded from there up to its length.</summary>
        public static double GetStart(Shaft shaft)
        {
            if (shaft == null) throw new ArgumentNullException(nameof(shaft));
            shaft.GetAxialSpan(out var start, out _);
            return start;
        }

        private static double Q(double millimetres)
        {
            return Math.Round(millimetres, 7) * SingleToothSketchBuilder.MmToAlibreUnits;
        }
    }
}
