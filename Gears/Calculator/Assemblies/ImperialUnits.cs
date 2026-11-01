using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Conversions between metric gear data (module, millimetres) and the imperial equivalents (diametral pitch, circular pitch
    /// in inches). All gear design and calculation stays metric; these conversions are a reference for the user only.
    /// <para>
    /// Relationships (Elements of Metric Gear Technology): diametral pitch <c>P = 25.4 / m</c> (teeth per inch),
    /// module <c>m = 25.4 / P</c>, circular pitch <c>p = pi * m</c> millimetres, or <c>pi / P</c> inches.
    /// </para>
    /// </summary>
    public static class ImperialUnits
    {
        /// <summary>Millimetres in one inch (exact).</summary>
        public const double MillimetresPerInch = 25.4;

        public static double ToInches(double millimetres) => millimetres / MillimetresPerInch;

        public static double ToMillimetres(double inches) => inches * MillimetresPerInch;

        /// <summary>Diametral pitch (teeth per inch of pitch diameter) from the module (mm): <c>P = 25.4 / m</c>.</summary>
        public static double DiametralPitchFromModule(double module)
        {
            if (module <= 0) throw new ArgumentOutOfRangeException(nameof(module), "The module must be positive.");
            return MillimetresPerInch / module;
        }

        /// <summary>Module (mm) from the diametral pitch: <c>m = 25.4 / P</c>.</summary>
        public static double ModuleFromDiametralPitch(double diametralPitch)
        {
            if (diametralPitch <= 0) throw new ArgumentOutOfRangeException(nameof(diametralPitch), "The diametral pitch must be positive.");
            return MillimetresPerInch / diametralPitch;
        }

        /// <summary>Circular pitch in millimetres from the module: <c>p = pi * m</c>.</summary>
        public static double CircularPitchMmFromModule(double module)
        {
            if (module <= 0) throw new ArgumentOutOfRangeException(nameof(module), "The module must be positive.");
            return Math.PI * module;
        }

        /// <summary>Circular pitch in inches from the module: <c>p = pi * m / 25.4</c>, which equals <c>pi / P</c>.</summary>
        public static double CircularPitchInchesFromModule(double module) => ToInches(CircularPitchMmFromModule(module));

        /// <summary>Circular pitch in inches from the diametral pitch: <c>p = pi / P</c>.</summary>
        public static double CircularPitchInchesFromDiametralPitch(double diametralPitch)
        {
            if (diametralPitch <= 0) throw new ArgumentOutOfRangeException(nameof(diametralPitch), "The diametral pitch must be positive.");
            return Math.PI / diametralPitch;
        }

        /// <summary>Diametral pitch from a circular pitch in inches: <c>P = pi / p</c>.</summary>
        public static double DiametralPitchFromCircularPitchInches(double circularPitchInches)
        {
            if (circularPitchInches <= 0) throw new ArgumentOutOfRangeException(nameof(circularPitchInches), "The circular pitch must be positive.");
            return Math.PI / circularPitchInches;
        }

        /// <summary>Transverse module of a helical gear: <c>m_t = m_n / cos(beta)</c>.</summary>
        public static double TransverseModule(double normalModule, double helixAngleDeg)
        {
            return normalModule / Math.Cos(helixAngleDeg * Math.PI / 180.0);
        }
    }

    /// <summary>One line of an imperial reference table: the metric value and its imperial equivalent.</summary>
    public sealed class ImperialRow
    {
        public ImperialRow(string name, string symbol, string formula, double metricValue, string metricUnit, double imperialValue, string imperialUnit)
        {
            Name = name;
            Symbol = symbol;
            Formula = formula;
            MetricValue = metricValue;
            MetricUnit = metricUnit;
            ImperialValue = imperialValue;
            ImperialUnit = imperialUnit;
        }

        public string Name { get; }

        /// <summary>Plain-text symbol, such as "P" or "p".</summary>
        public string Symbol { get; }

        /// <summary>Plain-text conversion formula.</summary>
        public string Formula { get; }

        public double MetricValue { get; }

        public string MetricUnit { get; }

        public double ImperialValue { get; }

        public string ImperialUnit { get; }
    }

    /// <summary>A titled list of <see cref="ImperialRow"/>.</summary>
    public sealed class ImperialTable
    {
        public ImperialTable(string title, IReadOnlyList<ImperialRow> rows)
        {
            Title = title ?? string.Empty;
            Rows = rows ?? new List<ImperialRow>();
        }

        public string Title { get; }

        public IReadOnlyList<ImperialRow> Rows { get; }

        public static ImperialTable Empty { get; } = new ImperialTable(string.Empty, new List<ImperialRow>());
    }

    /// <summary>
    /// Builds the imperial reference tables of gears and meshes: module with its diametral pitch (D.P.) and circular pitch
    /// (C.P.), followed by every calculated length converted to inches.
    /// </summary>
    public static class ImperialReference
    {
        /// <summary>
        /// The pitch conversions of a gear (module, D.P., C.P.; normal and transverse for a helical gear) followed by the
        /// lengths of its parameter table in inches.
        /// </summary>
        public static ImperialTable ForGear(GearComponent gear)
        {
            if (gear == null) return ImperialTable.Empty;

            var rows = new List<ImperialRow>();
            var helix = (gear as InvoluteGearComponent)?.HelixAngleDeg ?? 0.0;
            var helical = helix > 1e-6;

            if (gear.Module > 0)
            {
                if (helical)
                {
                    AddPitchRows(rows, "Normal ", "n", gear.Module);
                    AddPitchRows(rows, "Transverse ", "t", ImperialUnits.TransverseModule(gear.Module, helix));
                }
                else
                {
                    var prefix = gear is WormComponent ? "Axial " : string.Empty;
                    AddPitchRows(rows, prefix, string.Empty, gear.Module);
                }
            }

            var table = GearParameterTables.ForGear(gear);
            AddLengthRows(rows, table.Rows);
            return new ImperialTable(table.Title, rows);
        }

        /// <summary>The lengths of a mesh table (centre distances, diameters, backlash and so on) in inches.</summary>
        public static ImperialTable ForMesh(GearMeshRelation relation)
        {
            var table = GearParameterTables.ForMesh(relation);
            if (table.Rows.Count == 0) return ImperialTable.Empty;

            var rows = new List<ImperialRow>();
            AddLengthRows(rows, table.Rows);
            return new ImperialTable(table.Title, rows);
        }

        private static void AddPitchRows(List<ImperialRow> rows, string prefix, string subscript, double module)
        {
            string Sym(string letter) => subscript.Length == 0 ? letter : letter + "_" + subscript;

            var dp = ImperialUnits.DiametralPitchFromModule(module);
            rows.Add(new ImperialRow(prefix + "Diametral Pitch (D.P.)", Sym("P"), "P = 25.4 / " + Sym("m"),
                module, "mm (module)", dp, "teeth/in"));
            rows.Add(new ImperialRow(prefix + "Circular Pitch (C.P.)", Sym("p"), "p = \u03C0 \u00B7 " + Sym("m") + " = \u03C0 / " + Sym("P"),
                ImperialUnits.CircularPitchMmFromModule(module), "mm",
                ImperialUnits.CircularPitchInchesFromDiametralPitch(dp), "in"));
        }

        private static void AddLengthRows(List<ImperialRow> rows, IEnumerable<GearParameterRow> source)
        {
            foreach (var row in source.Where(r => r.Unit == "mm" && r.ParameterName.IndexOf("Module", StringComparison.OrdinalIgnoreCase) < 0))
            {
                rows.Add(new ImperialRow(row.ParameterName, row.SymbolLatex, "inches = mm / 25.4",
                    row.Value, "mm", ImperialUnits.ToInches(row.Value), "in"));
            }
        }
    }
}
