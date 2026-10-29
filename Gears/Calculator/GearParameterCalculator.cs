using System;
using System.Collections.Generic;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Calculator
{
    /// <summary>
    /// Calculated gear geometric parameters and metadata.
    /// </summary>
    public class CalculatedGearData
    {
        public int Teeth { get; set; }
        public double Module { get; set; }
        public double PressureAngleDeg { get; set; }
        public double PressureAngleRad { get; set; }
        public double FaceWidth { get; set; }
        public double ProfileShift { get; set; }
        public bool IsInternal { get; set; }
        public double RimThickness { get; set; }
        public double PitchDiameter { get; set; }
        public double BaseDiameter { get; set; }
        public double Addendum { get; set; }
        public double Dedendum { get; set; }
        public double WholeDepth { get; set; }
        public double TipDiameter { get; set; }
        public double RootDiameter { get; set; }
        public double CircularPitch { get; set; }
        public double ToothThickness { get; set; }
        public double BasePitch { get; set; }
        public double InvoluteFunction { get; set; }

        public List<GearParameterRow> ParameterRows { get; } = new List<GearParameterRow>();
    }

    /// <summary>
    /// Computes standard and profile-shifted metric gear parameters according to Elements of metric gear technology.
    /// </summary>
    public static class GearParameterCalculator
    {
        /// <summary>
        /// Calculates all metric gear parameters for external or internal spur/involute gears.
        /// </summary>
        public static CalculatedGearData Calculate(
            int z,
            double m,
            double alphaDeg,
            double faceWidth,
            double x,
            bool isInternal,
            double rimThickness = 10.0)
        {
            var data = new CalculatedGearData
            {
                Teeth = z,
                Module = m,
                PressureAngleDeg = alphaDeg,
                FaceWidth = faceWidth,
                ProfileShift = x,
                IsInternal = isInternal,
                RimThickness = rimThickness
            };

            var alphaRad = alphaDeg * Math.PI / 180.0;
            data.PressureAngleRad = alphaRad;
            data.InvoluteFunction = Math.Tan(alphaRad) - alphaRad;

            data.PitchDiameter = m * z;
            data.BaseDiameter = data.PitchDiameter * Math.Cos(alphaRad);

            if (isInternal)
            {
                data.Addendum = m * (1.0 - x);
                data.Dedendum = m * (1.25 + x);
                data.WholeDepth = data.Addendum + data.Dedendum;
                data.TipDiameter = data.PitchDiameter - 2.0 * data.Addendum;
                data.RootDiameter = data.PitchDiameter + 2.0 * data.Dedendum;
            }
            else
            {
                data.Addendum = m * (1.0 + x);
                data.Dedendum = m * (1.25 - x);
                data.WholeDepth = data.Addendum + data.Dedendum;
                data.TipDiameter = data.PitchDiameter + 2.0 * data.Addendum;
                data.RootDiameter = data.PitchDiameter - 2.0 * data.Dedendum;
            }

            data.CircularPitch = Math.PI * m;
            data.ToothThickness = m * (Math.PI / 2.0 + 2.0 * x * Math.Tan(alphaRad));
            data.BasePitch = Math.PI * m * Math.Cos(alphaRad);

            // Populate tabular rows with LaTeX formatted symbols and formulas
            data.ParameterRows.Add(new GearParameterRow("Module", "m", "m", m, "mm"));
            data.ParameterRows.Add(new GearParameterRow("Number of Teeth", "z", "z", z, "-"));
            data.ParameterRows.Add(new GearParameterRow("Standard Pressure Angle", "\\alpha", "\\alpha", alphaDeg, "°"));
            data.ParameterRows.Add(new GearParameterRow("Profile Shift Coefficient", "x", "x", x, "-"));
            data.ParameterRows.Add(new GearParameterRow("Face Width (Thickness)", "b", "b", faceWidth, "mm"));
            data.ParameterRows.Add(new GearParameterRow("Involute Function", "\\text{inv}(\\alpha)", "\\tan\\alpha - \\alpha", data.InvoluteFunction, "rad"));
            data.ParameterRows.Add(new GearParameterRow("Pitch Diameter", "d", "m \\cdot z", data.PitchDiameter, "mm"));
            data.ParameterRows.Add(new GearParameterRow("Base Diameter", "d_b", "d \\cdot \\cos\\alpha", data.BaseDiameter, "mm"));

            if (isInternal)
            {
                data.ParameterRows.Add(new GearParameterRow("Addendum", "h_a", "m(1 - x)", data.Addendum, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Dedendum", "h_f", "m(1.25 + x)", data.Dedendum, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Whole Depth", "h", "h_a + h_f", data.WholeDepth, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Inside (Tip) Diameter", "d_a", "d - 2 h_a", data.TipDiameter, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Root Diameter", "d_f", "d + 2 h_f", data.RootDiameter, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Rim Thickness", "s_{rim}", "s_{rim}", rimThickness, "mm"));
            }
            else
            {
                data.ParameterRows.Add(new GearParameterRow("Addendum", "h_a", "m(1 + x)", data.Addendum, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Dedendum", "h_f", "m(1.25 - x)", data.Dedendum, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Whole Depth", "h", "h_a + h_f", data.WholeDepth, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Outside (Tip) Diameter", "d_a", "d + 2 h_a", data.TipDiameter, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Root Diameter", "d_f", "d - 2 h_f", data.RootDiameter, "mm"));
            }

            data.ParameterRows.Add(new GearParameterRow("Circular Pitch", "p", "\\pi \\cdot m", data.CircularPitch, "mm"));
            data.ParameterRows.Add(new GearParameterRow("Circular Tooth Thickness", "s", "m(\\frac{\\pi}{2} + 2 x \\tan\\alpha)", data.ToothThickness, "mm"));
            data.ParameterRows.Add(new GearParameterRow("Base Pitch", "p_b", "\\pi m \\cos\\alpha", data.BasePitch, "mm"));

            return data;
        }
    }
}
