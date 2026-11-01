using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// A titled table of calculated parameters: name, symbol, formula, value and unit, as shown by the single gear editor.
    /// </summary>
    public sealed class GearParameterTable
    {
        public GearParameterTable(string title, IReadOnlyList<GearParameterRow> rows)
        {
            Title = title ?? string.Empty;
            Rows = rows ?? new List<GearParameterRow>();
        }

        public string Title { get; }

        public IReadOnlyList<GearParameterRow> Rows { get; }

        public static GearParameterTable Empty { get; } = new GearParameterTable(string.Empty, new List<GearParameterRow>());
    }

    /// <summary>
    /// Builds the parameter tables (symbols, formulae and calculated values) of gears and meshes in an assembly, from the same
    /// calculator that fills the table of the single gear editor.
    /// </summary>
    public static class GearParameterTables
    {
        /// <summary>
        /// The parameters of one gear. Involute gears give the full table of the single gear editor; other gear families
        /// give their basic geometry.
        /// </summary>
        public static GearParameterTable ForGear(GearComponent gear)
        {
            if (gear == null) return GearParameterTable.Empty;

            if (gear is InvoluteGearComponent involute)
            {
                var data = GearParameterCalculator.Calculate(
                    involute.Teeth,
                    involute.Module,
                    involute.PressureAngleDeg,
                    involute.FaceWidth,
                    involute.ProfileShift,
                    involute.IsInternal,
                    involute.RimThickness,
                    involute.Clearance,
                    involute.Backlash,
                    involute.HelixAngleDeg,
                    involute.HelicalDirection);
                return new GearParameterTable($"{involute.Name} ({involute.TypeName})", data.ParameterRows.ToList());
            }

            var rows = new List<GearParameterRow>
            {
                new GearParameterRow("Module", "m", "m", gear.Module, "mm"),
                new GearParameterRow("Pressure Angle", "\\alpha", "\\alpha", gear.PressureAngleDeg, "°"),
                new GearParameterRow("Face Width (Thickness)", "b", "b", gear.FaceWidth, "mm")
            };
            if (gear.ToothCount > 0)
            {
                rows.Insert(2, new GearParameterRow("Number of Teeth", "z", "z", gear.ToothCount, "-"));
                rows.Add(new GearParameterRow("Pitch Diameter", "d", "m z", gear.PitchDiameter, "mm"));
            }

            return new GearParameterTable($"{gear.Name} ({gear.TypeName})", rows);
        }

        /// <summary>
        /// The parameters of the mesh between two gears: working pressure angle, centre distances, contact ratios, gear ratio and so on.
        /// Empty for meshes that have no involute pair data.
        /// </summary>
        public static GearParameterTable ForMesh(GearMeshRelation relation)
        {
            if (relation == null) return GearParameterTable.Empty;
            var rows = relation.Result?.PairData?.ParameterRows;
            if (rows == null || rows.Count == 0) return GearParameterTable.Empty;
            return new GearParameterTable($"{relation.Driver.Name} \u2192 {relation.Driven.Name} ({relation.MeshType})", rows.ToList());
        }

        /// <summary>
        /// The mesh to show for a gear: the mesh that drives it, otherwise the first mesh it drives; null when it has none.
        /// </summary>
        public static GearMeshRelation FindMeshFor(GearAssembly assembly, GearComponent gear)
        {
            if (assembly == null || gear == null) return null;
            return assembly.FindDrivingRelation(gear)
                   ?? assembly.Relations.FirstOrDefault(r => ReferenceEquals(r.Driver, gear));
        }
    }
}
