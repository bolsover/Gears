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

            if (gear is HtdPulleyComponent pulley)
            {
                var profile = pulley.Profile;
                var pulleyRows = new List<GearParameterRow>
                {
                    new GearParameterRow("Belt Pitch", "p", "p", profile.Pitch, "mm"),
                    new GearParameterRow("Number of Grooves", "z", "z", pulley.Teeth, "-"),
                    new GearParameterRow("Pitch Diameter", "d", "z p / \\pi", pulley.PitchDiameter, "mm"),
                    new GearParameterRow("Pitch Line Differential", "PLD", "PLD", profile.PitchLineDifferential, "mm"),
                    new GearParameterRow("Outside Diameter", "d_o", "d - 2 PLD", pulley.OutsideDiameter, "mm"),
                    new GearParameterRow("Groove Depth (Belt Tooth Height)", "h_t", "h_t", profile.ToothHeight, "mm"),
                    new GearParameterRow("Root Diameter", "d_r", "d_o - 2 h_t", pulley.RootDiameter, "mm"),
                    new GearParameterRow("Groove Bottom Radius (Belt Head Radius)", "R_1", "R_1", profile.HeadRadius, "mm"),
                    new GearParameterRow("Groove Edge Radius (Belt Foot Radius)", "R_2", "R_2", profile.FootRadius, "mm"),
                    new GearParameterRow("Groove Width at Outside Diameter", "w", "w", profile.ToothWidth, "mm"),
                    new GearParameterRow("Flank Included Angle", "\\theta", "\\theta", HtdProfile.FlankAngleDeg, "°"),
                    new GearParameterRow("Recommended Minimum Grooves", "z_min", "z_min", profile.MinimumTeeth, "-"),
                    new GearParameterRow("Pulley Width", "b", "b", pulley.FaceWidth, "mm")
                };
                if (pulley.HasFlanges)
                {
                    pulleyRows.Add(new GearParameterRow("Flange Diameter", "d_f", "d_o + 2 h_f", pulley.FlangeDiameter, "mm"));
                }

                return new GearParameterTable($"{pulley.Name} ({pulley.TypeName})", pulleyRows);
            }

            if (gear is SprocketComponent sprocket)
            {
                var chain = sprocket.Profile;
                var form = sprocket.ToothForm;
                var sprocketRows = new List<GearParameterRow>
                {
                    new GearParameterRow("Chain Pitch", "p", "p", chain.Pitch, "mm"),
                    new GearParameterRow("Roller Diameter", "d_1", "d_1", chain.RollerDiameter, "mm"),
                    new GearParameterRow("Chain Inner Width", "b_1", "b_1", chain.InnerWidth, "mm"),
                    new GearParameterRow("Number of Teeth", "z", "z", sprocket.Teeth, "-"),
                    new GearParameterRow("Pitch Diameter", "d", "p / \\sin(180° / z)", sprocket.PitchDiameter, "mm"),
                    new GearParameterRow("Root Diameter", "d_f", "d - d_1", sprocket.RootDiameter, "mm"),
                    new GearParameterRow("Tip Diameter (" + form + ")", "d_a", form == SprocketToothForm.Maximum ? "d + 1.25 p - d_1" : "d + p (1 - 1.6 / z) - d_1", sprocket.OutsideDiameter, "mm"),
                    new GearParameterRow("Roller Seating Radius (" + form + ")", "r_i", form == SprocketToothForm.Maximum ? "0.505 d_1 + 0.069 \\sqrt[3]{d_1}" : "0.505 d_1", chain.SeatRadius(form), "mm"),
                    new GearParameterRow("Roller Seating Angle (" + form + ")", "\\alpha", form == SprocketToothForm.Maximum ? "120° - 90° / z" : "140° - 90° / z", ChainProfile.SeatAngle(sprocket.Teeth, form) * 180.0 / Math.PI, "°"),
                    new GearParameterRow("Tooth Flank Radius (" + form + ")", "r_e", form == SprocketToothForm.Maximum ? "0.008 d_1 (z^2 + 180)" : "0.12 d_1 (z + 2)", chain.FlankRadius(sprocket.Teeth, form), "mm"),
                    new GearParameterRow("Tooth Width", "b_f", chain.Pitch <= 12.7 + 1e-9 ? "0.93 b_1" : "0.95 b_1", sprocket.ToothWidth, "mm"),
                    new GearParameterRow("Recommended Minimum Teeth", "z_min", "z_min", ChainProfile.RecommendedMinimumTeeth, "-")
                };
                if (sprocket.BoreDiameter > 0.0)
                {
                    sprocketRows.Add(new GearParameterRow("Bore Diameter", "d_b", "d_b", sprocket.BoreDiameter, "mm"));
                }

                if (sprocket.HasHub)
                {
                    sprocketRows.Add(new GearParameterRow("Hub Diameter", "d_h", "d_h", sprocket.HubDiameter, "mm"));
                    sprocketRows.Add(new GearParameterRow("Hub Length", "l_h", "l_h", sprocket.HubLength, "mm"));
                }

                return new GearParameterTable($"{sprocket.Name} ({sprocket.TypeName})", sprocketRows);
            }

            if (gear is CycloidalGearComponent cycloidal)
            {
                var cycloidalRows = new List<GearParameterRow>
                {
                    new GearParameterRow("Module", "m", "m", cycloidal.Module, "mm"),
                    new GearParameterRow("Number of Teeth", "z", "z", cycloidal.Teeth, "-"),
                    new GearParameterRow("Teeth of Mating Gear", "z_m", "z_m", cycloidal.EffectiveMateTeeth, "-"),
                    new GearParameterRow("Pair Ratio (Wheel : Pinion)", "i", "z_w / z_p", cycloidal.PairRatio, "-"),
                    new GearParameterRow("Pitch Diameter", "d", "m z", cycloidal.PitchDiameter, "mm"),
                    new GearParameterRow("Circular Pitch", "p", "\\pi m", cycloidal.CircularPitch, "mm"),
                    new GearParameterRow("Addendum", "h_a", "h_a", cycloidal.Addendum, "mm"),
                    new GearParameterRow("Dedendum", "h_f", "h_f", cycloidal.Dedendum, "mm"),
                    new GearParameterRow("Addendum Arc Radius", "r_a", "r_a", cycloidal.AddendumRadius, "mm"),
                    new GearParameterRow("Outside Diameter", "d_a", "d + 2 h_a", cycloidal.OutsideDiameter, "mm"),
                    new GearParameterRow("Root Diameter", "d_f", "d - 2 h_f", cycloidal.RootDiameter, "mm"),
                    new GearParameterRow("Face Width (Thickness)", "b", "b", cycloidal.FaceWidth, "mm")
                };
                if (cycloidal.CentreHole > 0.0)
                {
                    cycloidalRows.Add(new GearParameterRow("Centre Hole", "d_h", "d_h", cycloidal.CentreHole, "mm"));
                }

                return new GearParameterTable($"{cycloidal.Name} ({cycloidal.TypeName})", cycloidalRows);
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
