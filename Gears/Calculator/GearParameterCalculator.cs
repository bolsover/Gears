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
        public double Clearance { get; set; }

        public List<GearParameterRow> ParameterRows { get; } = new List<GearParameterRow>();
    }

    /// <summary>
    /// Calculated geometric parameters and metadata for a meshed pair of gears.
    /// </summary>
    public class CalculatedGearPairData
    {
        public CalculatedGearData Gear1 { get; set; }
        public CalculatedGearData Gear2 { get; set; }
        public double StandardCentreDistance { get; set; }
        public double WorkingCentreDistance { get; set; }
        public double WorkingPressureAngleDeg { get; set; }
        public double WorkingPressureAngleRad { get; set; }
        public double CentreDistanceIncrementFactor { get; set; }
        public double WorkingInvoluteFunction { get; set; }
        public double SumProfileShift { get; set; }
        public double Gear1WorkingPitchDiameter { get; set; }
        public double Gear2WorkingPitchDiameter { get; set; }
        public double ContactRatio { get; set; }
        public double GearRatio { get; set; }
        public double Gear1Clearance => Gear1?.Clearance ?? 0.0;
        public double Gear2Clearance => Gear2?.Clearance ?? 0.0;
        public double BottomClearance1 { get; set; }
        public double BottomClearance2 { get; set; }

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
            double rimThickness = 10.0,
            double? clearance = null)
        {
            var c = clearance ?? (0.25 * m);
            var data = new CalculatedGearData
            {
                Teeth = z,
                Module = m,
                PressureAngleDeg = alphaDeg,
                FaceWidth = faceWidth,
                ProfileShift = x,
                IsInternal = isInternal,
                RimThickness = rimThickness,
                Clearance = c
            };

            var alphaRad = alphaDeg * Math.PI / 180.0;
            data.PressureAngleRad = alphaRad;
            data.InvoluteFunction = Math.Tan(alphaRad) - alphaRad;

            data.PitchDiameter = m * z;
            data.BaseDiameter = data.PitchDiameter * Math.Cos(alphaRad);

            if (isInternal)
            {
                data.Addendum = m * (1.0 - x);
                data.Dedendum = m * (1.0 + x) + c;
                data.WholeDepth = data.Addendum + data.Dedendum;
                data.TipDiameter = data.PitchDiameter - 2.0 * data.Addendum;
                data.RootDiameter = data.PitchDiameter + 2.0 * data.Dedendum;
            }
            else
            {
                data.Addendum = m * (1.0 + x);
                data.Dedendum = m * (1.0 - x) + c;
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
            data.ParameterRows.Add(new GearParameterRow("Bottom Clearance", "c", "c", data.Clearance, "mm"));
            data.ParameterRows.Add(new GearParameterRow("Involute Function", "\\text{inv}(\\alpha)", "\\tan\\alpha - \\alpha", data.InvoluteFunction, "rad"));
            data.ParameterRows.Add(new GearParameterRow("Pitch Diameter", "d", "m \\cdot z", data.PitchDiameter, "mm"));
            data.ParameterRows.Add(new GearParameterRow("Base Diameter", "d_b", "d \\cdot \\cos\\alpha", data.BaseDiameter, "mm"));

            if (isInternal)
            {
                data.ParameterRows.Add(new GearParameterRow("Addendum", "h_a", "m(1 - x)", data.Addendum, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Dedendum", "h_f", "m(1 + x) + c", data.Dedendum, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Whole Depth", "h", "h_a + h_f", data.WholeDepth, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Inside (Tip) Diameter", "d_a", "d - 2 h_a", data.TipDiameter, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Root Diameter", "d_f", "d + 2 h_f", data.RootDiameter, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Rim Thickness", "s_{rim}", "s_{rim}", rimThickness, "mm"));
            }
            else
            {
                data.ParameterRows.Add(new GearParameterRow("Addendum", "h_a", "m(1 + x)", data.Addendum, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Dedendum", "h_f", "m(1 - x) + c", data.Dedendum, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Whole Depth", "h", "h_a + h_f", data.WholeDepth, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Outside (Tip) Diameter", "d_a", "d + 2 h_a", data.TipDiameter, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Root Diameter", "d_f", "d - 2 h_f", data.RootDiameter, "mm"));
            }

            data.ParameterRows.Add(new GearParameterRow("Circular Pitch", "p", "\\pi \\cdot m", data.CircularPitch, "mm"));
            data.ParameterRows.Add(new GearParameterRow("Circular Tooth Thickness", "s", "m(\\frac{\\pi}{2} + 2 x \\tan\\alpha)", data.ToothThickness, "mm"));
            data.ParameterRows.Add(new GearParameterRow("Base Pitch", "p_b", "\\pi m \\cos\\alpha", data.BasePitch, "mm"));

            return data;
        }

        /// <summary>
        /// Calculates geometric parameters for a meshed gear pair according to Elements of metric gear technology.
        /// </summary>
        public static CalculatedGearPairData CalculatePair(
            int z1, double m, double alphaDeg, double faceWidth1, double x1, bool isInternal1, double rimThickness1,
            int z2, double faceWidth2, double x2, bool isInternal2, double rimThickness2,
            double? clearance1 = null, double? clearance2 = null)
        {
            var gear1 = Calculate(z1, m, alphaDeg, faceWidth1, x1, isInternal1, rimThickness1, clearance1);
            var gear2 = Calculate(z2, m, alphaDeg, faceWidth2, x2, isInternal2, rimThickness2, clearance2 ?? clearance1);

            var pairData = new CalculatedGearPairData
            {
                Gear1 = gear1,
                Gear2 = gear2
            };

            var alphaRad = alphaDeg * Math.PI / 180.0;
            var isInternalPair = isInternal1 || isInternal2;

            double aStd;
            double sumX;
            double sumZ;

            if (isInternalPair)
            {
                // Internal-external mesh
                var zInternal = isInternal2 ? z2 : z1;
                var zExternal = isInternal2 ? z1 : z2;
                var xInternal = isInternal2 ? x2 : x1;
                var xExternal = isInternal2 ? x1 : x2;

                sumZ = zInternal - zExternal;
                sumX = xInternal - xExternal;
                aStd = m * sumZ / 2.0;
            }
            else
            {
                // External-external mesh
                sumZ = z1 + z2;
                sumX = x1 + x2;
                aStd = m * sumZ / 2.0;
            }

            pairData.StandardCentreDistance = aStd;
            pairData.SumProfileShift = sumX;

            var invAlpha = Math.Tan(alphaRad) - alphaRad;
            var invAlphaW = sumZ != 0
                ? 2.0 * Math.Tan(alphaRad) * (sumX / sumZ) + invAlpha
                : invAlpha;
            pairData.WorkingInvoluteFunction = invAlphaW;

            double alphaWRad;
            if (Math.Abs(sumX) < 1e-9 || Math.Abs(invAlphaW - invAlpha) < 1e-9)
            {
                alphaWRad = alphaRad;
            }
            else
            {
                alphaWRad = CustomCalculations.PressureAngleByNewtonRaphson(invAlphaW, alphaRad, 1e-9, 100);
                if (alphaWRad <= 0 || double.IsNaN(alphaWRad) || double.IsInfinity(alphaWRad))
                {
                    alphaWRad = alphaRad;
                }
            }

            pairData.WorkingPressureAngleRad = alphaWRad;
            pairData.WorkingPressureAngleDeg = alphaWRad * 180.0 / Math.PI;

            var y = (sumZ / 2.0) * ((Math.Cos(alphaRad) / Math.Cos(alphaWRad)) - 1.0);
            pairData.CentreDistanceIncrementFactor = y;

            var aWorking = aStd + y * m;
            pairData.WorkingCentreDistance = aWorking;

            var dw1 = gear1.PitchDiameter * (Math.Cos(alphaRad) / Math.Cos(alphaWRad));
            var dw2 = gear2.PitchDiameter * (Math.Cos(alphaRad) / Math.Cos(alphaWRad));
            pairData.Gear1WorkingPitchDiameter = dw1;
            pairData.Gear2WorkingPitchDiameter = dw2;

            // Contact Ratio
            var ra1 = gear1.TipDiameter / 2.0;
            var rb1 = gear1.BaseDiameter / 2.0;
            var ra2 = gear2.TipDiameter / 2.0;
            var rb2 = gear2.BaseDiameter / 2.0;

            var term1 = ra1 > rb1 ? Math.Sqrt(ra1 * ra1 - rb1 * rb1) : 0.0;
            var term2 = ra2 > rb2 ? Math.Sqrt(ra2 * ra2 - rb2 * rb2) : 0.0;
            var basePitch = Math.PI * m * Math.Cos(alphaRad);

            double contactRatio;
            if (isInternalPair)
            {
                contactRatio = basePitch > 1e-9
                    ? (term1 - term2 + aWorking * Math.Sin(alphaWRad)) / basePitch
                    : 0.0;
                contactRatio = Math.Abs(contactRatio);
            }
            else
            {
                contactRatio = basePitch > 1e-9
                    ? (term1 + term2 - aWorking * Math.Sin(alphaWRad)) / basePitch
                    : 0.0;
            }
            pairData.ContactRatio = contactRatio;
            pairData.GearRatio = z1 > 0 ? (double)z2 / z1 : 0.0;

            // Populate pair tabular rows
            pairData.ParameterRows.Add(new GearParameterRow("Module", "m", "m", m, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Standard Pressure Angle", "\\alpha", "\\alpha", alphaDeg, "°"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Teeth", "z_1", "z_1", z1, "-"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Teeth", "z_2", "z_2", z2, "-"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Profile Shift", "x_1", "x_1", x1, "-"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Profile Shift", "x_2", "x_2", x2, "-"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Clearance", "c_1", "c_1", gear1.Clearance, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Clearance", "c_2", "c_2", gear2.Clearance, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Face Width", "b_1", "b_1", faceWidth1, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Face Width", "b_2", "b_2", faceWidth2, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Pitch Diameter", "d_1", "m \\cdot z_1", gear1.PitchDiameter, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Pitch Diameter", "d_2", "m \\cdot z_2", gear2.PitchDiameter, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Base Diameter", "d_{b1}", "d_1 \\cdot \\cos\\alpha", gear1.BaseDiameter, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Base Diameter", "d_{b2}", "d_2 \\cdot \\cos\\alpha", gear2.BaseDiameter, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Working Pitch Diameter", "d_{w1}", "d_1 \\frac{\\cos\\alpha}{\\cos\\alpha_w}", dw1, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Working Pitch Diameter", "d_{w2}", "d_2 \\frac{\\cos\\alpha}{\\cos\\alpha_w}", dw2, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Addendum", "h_{a1}", isInternal1 ? "m(1 - x_1)" : "m(1 + x_1)", gear1.Addendum, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Addendum", "h_{a2}", isInternal2 ? "m(1 - x_2)" : "m(1 + x_2)", gear2.Addendum, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Dedendum", "h_{f1}", isInternal1 ? "m(1 + x_1) + c_1" : "m(1 - x_1) + c_1", gear1.Dedendum, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Dedendum", "h_{f2}", isInternal2 ? "m(1 + x_2) + c_2" : "m(1 - x_2) + c_2", gear2.Dedendum, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Whole Depth", "h_1", "h_{a1} + h_{f1}", gear1.WholeDepth, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Whole Depth", "h_2", "h_{a2} + h_{f2}", gear2.WholeDepth, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Tip Diameter", "d_{a1}", isInternal1 ? "d_1 - 2 h_{a1}" : "d_1 + 2 h_{a1}", gear1.TipDiameter, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Tip Diameter", "d_{a2}", isInternal2 ? "d_2 - 2 h_{a2}" : "d_2 + 2 h_{a2}", gear2.TipDiameter, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Root Diameter", "d_{f1}", isInternal1 ? "d_1 + 2 h_{f1}" : "d_1 - 2 h_{f1}", gear1.RootDiameter, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Root Diameter", "d_{f2}", isInternal2 ? "d_2 + 2 h_{f2}" : "d_2 - 2 h_{f2}", gear2.RootDiameter, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Tooth Thickness", "s_1", "m(\\frac{\\pi}{2} + 2 x_1 \\tan\\alpha)", gear1.ToothThickness, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Tooth Thickness", "s_2", "m(\\frac{\\pi}{2} + 2 x_2 \\tan\\alpha)", gear2.ToothThickness, "mm"));
            if (isInternal1) pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Rim Thickness", "s_{rim1}", "s_{rim1}", rimThickness1, "mm"));
            if (isInternal2) pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Rim Thickness", "s_{rim2}", "s_{rim2}", rimThickness2, "mm"));

            pairData.ParameterRows.Add(new GearParameterRow("Standard Centre Distance", "a", isInternalPair ? "\\frac{m(z_2 - z_1)}{2}" : "\\frac{m(z_1 + z_2)}{2}", aStd, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Working Centre Distance", "a_w", "a + y \\cdot m", aWorking, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Working Pressure Angle", "\\alpha_w", "\\alpha_w", pairData.WorkingPressureAngleDeg, "°"));
            pairData.ParameterRows.Add(new GearParameterRow("Centre Distance Increment Factor", "y", "\\frac{a_w - a}{m}", y, "-"));
            pairData.ParameterRows.Add(new GearParameterRow("Working Involute Function", "\\text{inv}(\\alpha_w)", "\\tan\\alpha_w - \\alpha_w", invAlphaW, "rad"));
            pairData.ParameterRows.Add(new GearParameterRow("Contact Ratio", "\\epsilon_\\alpha", "\\epsilon_\\alpha", contactRatio, "-"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear Ratio", "i", "\\frac{z_2}{z_1}", pairData.GearRatio, "-"));

            return pairData;
        }
    }
}
