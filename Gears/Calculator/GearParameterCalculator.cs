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
        public double HelixAngleDeg { get; set; }
        public double HelixAngleRad { get; set; }
        public string HelicalDirection { get; set; } = "R";
        public bool IsHelical => HelixAngleDeg > 1e-6;
        public double NormalModule => Module;
        public double RadialModule { get; set; }
        public double NormalPressureAngleDeg => PressureAngleDeg;
        public double RadialPressureAngleDeg { get; set; }
        public double RadialPressureAngleRad { get; set; }
        public double AxialPitch { get; set; }
        public double Lead { get; set; }
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
        public double Backlash { get; set; }

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
        public double AxialContactRatio { get; set; }
        public double GearRatio { get; set; }
        public double Gear1Clearance => Gear1?.Clearance ?? 0.0;
        public double Gear2Clearance => Gear2?.Clearance ?? 0.0;
        public double BottomClearance1 { get; set; }
        public double BottomClearance2 { get; set; }
        public double Gear1Backlash => Gear1?.Backlash ?? 0.0;
        public double Gear2Backlash => Gear2?.Backlash ?? 0.0;
        public double TotalBacklash => Gear1Backlash + Gear2Backlash;
        public double NormalBacklash { get; set; }

        /// <summary>
        /// Circular backlash measured on the working pitch circles at the working centre distance (mm). Without a centre
        /// distance override and with zero profile shift it equals <see cref="TotalBacklash"/>. For an external pair, moving the
        /// shafts apart increases it and moving them closer reduces it; for an internal pair it is the other way round.
        /// A negative value means the teeth jam.
        /// </summary>
        public double WorkingCircularBacklash { get; set; }

        /// <summary>True when the working centre distance was set by the caller instead of being derived from the profile shifts.</summary>
        public bool CentreDistanceOverridden { get; set; }
        public double Gear1HelixAngle => Gear1?.HelixAngleDeg ?? 0.0;
        public double Gear2HelixAngle => Gear2?.HelixAngleDeg ?? 0.0;
        public string Gear1HelicalDirection => Gear1?.HelicalDirection ?? "R";
        public string Gear2HelicalDirection => Gear2?.HelicalDirection ?? "L";

        public List<GearParameterRow> ParameterRows { get; } = new List<GearParameterRow>();
    }

    /// <summary>
    /// Computes standard and profile-shifted metric gear parameters according to Elements of metric gear technology.
    /// </summary>
    public static class GearParameterCalculator
    {
        /// <summary>
        /// Calculates all metric gear parameters for external or internal spur and helical involute gears.
        /// </summary>
        public static CalculatedGearData Calculate(
            int z,
            double m,
            double alphaDeg,
            double faceWidth,
            double x,
            bool isInternal,
            double rimThickness = 10.0,
            double? clearance = null,
            double? backlash = null,
            double? helixAngleDeg = null,
            string helicalDirection = "R")
        {
            var c = clearance ?? (0.25 * m);
            var jt = backlash ?? 0.0;
            var betaDeg = helixAngleDeg ?? 0.0;
            var betaRad = betaDeg * Math.PI / 180.0;
            var cosBeta = Math.Cos(betaRad);
            if (cosBeta < 1e-6) cosBeta = 1e-6;

            var dir = string.IsNullOrWhiteSpace(helicalDirection) ||
                      helicalDirection.Trim().StartsWith("R", StringComparison.OrdinalIgnoreCase)
                      ? "R" : "L";

            var alphaRad = alphaDeg * Math.PI / 180.0;
            var tanAlphaT = Math.Tan(alphaRad) / cosBeta;
            var alphaTRad = Math.Atan(tanAlphaT);
            var alphaTDeg = alphaTRad * 180.0 / Math.PI;

            var mt = m / cosBeta;

            var data = new CalculatedGearData
            {
                Teeth = z,
                Module = m,
                PressureAngleDeg = alphaDeg,
                PressureAngleRad = alphaRad,
                HelixAngleDeg = betaDeg,
                HelixAngleRad = betaRad,
                HelicalDirection = dir,
                RadialModule = mt,
                RadialPressureAngleDeg = alphaTDeg,
                RadialPressureAngleRad = alphaTRad,
                FaceWidth = faceWidth,
                ProfileShift = x,
                IsInternal = isInternal,
                RimThickness = rimThickness,
                Clearance = c,
                Backlash = jt
            };

            data.InvoluteFunction = Math.Tan(alphaTRad) - alphaTRad;
            data.PitchDiameter = mt * z;
            data.BaseDiameter = data.PitchDiameter * Math.Cos(alphaTRad);

            if (isInternal)
            {
                data.Addendum = m * (1.0 - x);
                data.Dedendum = m * (1.0 + x) + c;
                data.TipDiameter = data.PitchDiameter - 2.0 * data.Addendum;
                data.RootDiameter = data.PitchDiameter + 2.0 * data.Dedendum;
            }
            else
            {
                data.Addendum = m * (1.0 + x);
                data.Dedendum = m * (1.0 - x) + c;
                data.TipDiameter = data.PitchDiameter + 2.0 * data.Addendum;
                data.RootDiameter = data.PitchDiameter - 2.0 * data.Dedendum;
            }

            data.WholeDepth = data.Addendum + data.Dedendum;
            data.CircularPitch = Math.PI * mt;
            data.ToothThickness = (m * (Math.PI / 2.0 + 2.0 * x * Math.Tan(alphaRad)) - jt) / cosBeta;
            data.BasePitch = Math.PI * mt * Math.Cos(alphaTRad);

            if (betaDeg > 1e-6)
            {
                var sinBeta = Math.Sin(betaRad);
                if (sinBeta > 1e-6)
                {
                    data.AxialPitch = Math.PI * m / sinBeta;
                    data.Lead = z * data.AxialPitch;
                }
            }

            // Populate tabular rows with LaTeX formatted symbols and formulas
            if (betaDeg > 1e-6)
            {
                data.ParameterRows.Add(new GearParameterRow("Normal Module", "m_n", "m_n", m, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Radial (Transverse) Module", "m_t", "\\frac{m_n}{\\cos\\beta}", mt, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Normal Pressure Angle", "\\alpha_n", "\\alpha_n", alphaDeg, "°"));
                data.ParameterRows.Add(new GearParameterRow("Radial (Transverse) Pressure Angle", "\\alpha_t", "\\arctan(\\frac{\\tan\\alpha_n}{\\cos\\beta})", alphaTDeg, "°"));
                data.ParameterRows.Add(new GearParameterRow("Helix Angle", "\\beta", "\\beta", betaDeg, "°"));
                data.ParameterRows.Add(new GearParameterRow("Helical Direction", "\\text{Hand}", "\\text{Direction}", dir == "R" ? 1.0 : 0.0, dir));
            }
            else
            {
                data.ParameterRows.Add(new GearParameterRow("Module", "m", "m", m, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Standard Pressure Angle", "\\alpha", "\\alpha", alphaDeg, "°"));
            }

            data.ParameterRows.Add(new GearParameterRow("Number of Teeth", "z", "z", z, "-"));
            data.ParameterRows.Add(new GearParameterRow("Profile Shift Coefficient", "x", "x", x, "-"));
            data.ParameterRows.Add(new GearParameterRow("Face Width (Thickness)", "b", "b", faceWidth, "mm"));
            data.ParameterRows.Add(new GearParameterRow("Bottom Clearance", "c", "c", data.Clearance, "mm"));
            data.ParameterRows.Add(new GearParameterRow("Backlash (Flank Clearance)", "j_t", "j_t", data.Backlash, "mm"));
            data.ParameterRows.Add(new GearParameterRow("Involute Function", betaDeg > 1e-6 ? "\\text{inv}(\\alpha_t)" : "\\text{inv}(\\alpha)", betaDeg > 1e-6 ? "\\tan\\alpha_t - \\alpha_t" : "\\tan\\alpha - \\alpha", data.InvoluteFunction, "rad"));
            data.ParameterRows.Add(new GearParameterRow("Pitch Diameter", "d", betaDeg > 1e-6 ? "m_t \\cdot z" : "m \\cdot z", data.PitchDiameter, "mm"));
            data.ParameterRows.Add(new GearParameterRow("Base Diameter", "d_b", betaDeg > 1e-6 ? "d \\cdot \\cos\\alpha_t" : "d \\cdot \\cos\\alpha", data.BaseDiameter, "mm"));

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

            data.ParameterRows.Add(new GearParameterRow("Circular Pitch", "p", betaDeg > 1e-6 ? "\\pi \\cdot m_t" : "\\pi \\cdot m", data.CircularPitch, "mm"));
            data.ParameterRows.Add(new GearParameterRow("Circular Tooth Thickness", "s", "m(\\frac{\\pi}{2} + 2 x \\tan\\alpha) - j_t", data.ToothThickness, "mm"));
            data.ParameterRows.Add(new GearParameterRow("Base Pitch", "p_b", betaDeg > 1e-6 ? "\\pi m_t \\cos\\alpha_t" : "\\pi m \\cos\\alpha", data.BasePitch, "mm"));

            if (betaDeg > 1e-6)
            {
                data.ParameterRows.Add(new GearParameterRow("Axial Pitch", "p_x", "\\frac{\\pi m_n}{\\sin\\beta}", data.AxialPitch, "mm"));
                data.ParameterRows.Add(new GearParameterRow("Lead", "p_z", "z \\cdot p_x", data.Lead, "mm"));
            }

            return data;
        }

        /// <summary>
        /// Calculates geometric parameters for a meshed gear pair according to Elements of metric gear technology.
        /// </summary>
        public static CalculatedGearPairData CalculatePair(
            int z1, double m, double alphaDeg, double faceWidth1, double x1, bool isInternal1, double rimThickness1,
            int z2, double faceWidth2, double x2, bool isInternal2, double rimThickness2,
            double? clearance1 = null, double? clearance2 = null,
            double? backlash1 = null, double? backlash2 = null,
            double? helixAngle1 = null, double? helixAngle2 = null,
            string helicalDirection1 = "R", string helicalDirection2 = "L",
            double? workingCentreDistance = null)
        {
            var beta1 = helixAngle1 ?? 0.0;
            var beta2 = helixAngle2 ?? beta1;
            var dir1 = string.IsNullOrWhiteSpace(helicalDirection1) ? "R" : helicalDirection1;
            var dir2 = string.IsNullOrWhiteSpace(helicalDirection2) ? (isInternal1 || isInternal2 ? dir1 : (dir1 == "R" ? "L" : "R")) : helicalDirection2;

            var gear1 = Calculate(z1, m, alphaDeg, faceWidth1, x1, isInternal1, rimThickness1, clearance1, backlash1, beta1, dir1);
            var gear2 = Calculate(z2, m, alphaDeg, faceWidth2, x2, isInternal2, rimThickness2, clearance2 ?? clearance1, backlash2 ?? backlash1, beta2, dir2);

            var pairData = new CalculatedGearPairData
            {
                Gear1 = gear1,
                Gear2 = gear2
            };

            var alphaRad = alphaDeg * Math.PI / 180.0;
            var betaRad = beta1 * Math.PI / 180.0;
            var cosBeta = Math.Cos(betaRad);
            if (cosBeta < 1e-6) cosBeta = 1e-6;

            var isInternalPair = isInternal1 || isInternal2;
            var alphaTRad = gear1.RadialPressureAngleRad;
            var alphaTDeg = gear1.RadialPressureAngleDeg;
            var mt = gear1.RadialModule;

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
                aStd = mt * sumZ / 2.0;
            }
            else
            {
                // External-external mesh
                sumZ = z1 + z2;
                sumX = x1 + x2;
                aStd = mt * sumZ / 2.0;
            }

            pairData.StandardCentreDistance = aStd;
            pairData.SumProfileShift = sumX;

            var invAlphaT = Math.Tan(alphaTRad) - alphaTRad;
            var invAlphaW = sumZ != 0
                ? 2.0 * Math.Tan(alphaRad) * (sumX / sumZ) + invAlphaT
                : invAlphaT;
            pairData.WorkingInvoluteFunction = invAlphaW;

            double alphaWRad;
            if (Math.Abs(sumX) < 1e-9 || Math.Abs(invAlphaW - invAlphaT) < 1e-9)
            {
                alphaWRad = alphaTRad;
            }
            else
            {
                alphaWRad = CustomCalculations.PressureAngleByNewtonRaphson(invAlphaW, alphaTRad, 1e-9, 100);
                if (alphaWRad <= 0 || double.IsNaN(alphaWRad) || double.IsInfinity(alphaWRad))
                {
                    alphaWRad = alphaTRad;
                }
            }

            var aWorking = aStd * (Math.Cos(alphaTRad) / Math.Cos(alphaWRad));

            if (workingCentreDistance.HasValue)
            {
                // The caller fixes the centre distance: the working pressure angle follows from a_w cos(alpha_wt) = a cos(alpha_t).
                aWorking = workingCentreDistance.Value;
                if (double.IsNaN(aWorking) || double.IsInfinity(aWorking) || aWorking <= 0.0 || aStd <= 0.0)
                {
                    throw new InvalidOperationException(
                        $"The centre distance must be a positive number (got {aWorking}); the standard centre distance is {aStd}.");
                }

                var cosAlphaW = aStd * Math.Cos(alphaTRad) / aWorking;
                if (cosAlphaW > 1.0 + 1e-12)
                {
                    throw new InvalidOperationException(
                        $"A centre distance of {aWorking} mm is too small for these gears: the base circles ({aStd * Math.Cos(alphaTRad):0.#######} mm apart) would overlap.");
                }

                alphaWRad = Math.Acos(Math.Min(1.0, cosAlphaW));
                invAlphaW = Math.Tan(alphaWRad) - alphaWRad;
                pairData.WorkingInvoluteFunction = invAlphaW;
                pairData.CentreDistanceOverridden = true;
            }

            pairData.WorkingPressureAngleRad = alphaWRad;
            pairData.WorkingPressureAngleDeg = alphaWRad * 180.0 / Math.PI;

            var y = (aWorking - aStd) / m;
            pairData.CentreDistanceIncrementFactor = y;
            pairData.WorkingCentreDistance = aWorking;

            var dw1 = gear1.PitchDiameter * (Math.Cos(alphaTRad) / Math.Cos(alphaWRad));
            var dw2 = gear2.PitchDiameter * (Math.Cos(alphaTRad) / Math.Cos(alphaWRad));
            pairData.Gear1WorkingPitchDiameter = dw1;
            pairData.Gear2WorkingPitchDiameter = dw2;

            // Contact Ratio
            var ra1 = gear1.TipDiameter / 2.0;
            var rb1 = gear1.BaseDiameter / 2.0;
            var ra2 = gear2.TipDiameter / 2.0;
            var rb2 = gear2.BaseDiameter / 2.0;

            var term1 = ra1 > rb1 ? Math.Sqrt(ra1 * ra1 - rb1 * rb1) : 0.0;
            var term2 = ra2 > rb2 ? Math.Sqrt(ra2 * ra2 - rb2 * rb2) : 0.0;
            var basePitch = Math.PI * mt * Math.Cos(alphaTRad);

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

            var axialContactRatio = 0.0;
            if (beta1 > 1e-6)
            {
                var minB = Math.Min(faceWidth1, faceWidth2);
                axialContactRatio = (minB * Math.Sin(betaRad)) / (Math.PI * m);
            }

            pairData.AxialContactRatio = axialContactRatio;
            pairData.ContactRatio = contactRatio + axialContactRatio;
            pairData.GearRatio = z1 > 0 ? (double)z2 / z1 : 0.0;
            pairData.WorkingCircularBacklash = CalculateWorkingBacklash(gear1, gear2, isInternal1, isInternal2, alphaTRad, alphaWRad, dw1, dw2, cosBeta);
            pairData.NormalBacklash = pairData.CentreDistanceOverridden
                ? pairData.WorkingCircularBacklash * Math.Cos(alphaWRad) * cosBeta
                : pairData.TotalBacklash * Math.Cos(alphaWRad) * cosBeta;

            // Populate pair tabular rows
            if (beta1 > 1e-6 || beta2 > 1e-6)
            {
                pairData.ParameterRows.Add(new GearParameterRow("Normal Module", "m_n", "m_n", m, "mm"));
                pairData.ParameterRows.Add(new GearParameterRow("Radial (Transverse) Module", "m_t", "\\frac{m_n}{\\cos\\beta}", mt, "mm"));
                pairData.ParameterRows.Add(new GearParameterRow("Normal Pressure Angle", "\\alpha_n", "\\alpha_n", alphaDeg, "°"));
                pairData.ParameterRows.Add(new GearParameterRow("Radial (Transverse) Pressure Angle", "\\alpha_t", "\\arctan(\\frac{\\tan\\alpha_n}{\\cos\\beta})", alphaTDeg, "°"));
                pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Helix Angle", "\\beta_1", "\\beta_1", beta1, "°"));
                pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Helix Angle", "\\beta_2", "\\beta_2", beta2, "°"));
                pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Direction", "\\text{Hand}_1", "\\text{Hand}_1", dir1 == "R" ? 1.0 : 0.0, dir1));
                pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Direction", "\\text{Hand}_2", "\\text{Hand}_2", dir2 == "R" ? 1.0 : 0.0, dir2));
            }
            else
            {
                pairData.ParameterRows.Add(new GearParameterRow("Module", "m", "m", m, "mm"));
                pairData.ParameterRows.Add(new GearParameterRow("Standard Pressure Angle", "\\alpha", "\\alpha", alphaDeg, "°"));
            }

            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Teeth", "z_1", "z_1", z1, "-"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Teeth", "z_2", "z_2", z2, "-"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Profile Shift", "x_1", "x_1", x1, "-"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Profile Shift", "x_2", "x_2", x2, "-"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Clearance", "c_1", "c_1", gear1.Clearance, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Clearance", "c_2", "c_2", gear2.Clearance, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Backlash", "j_{t1}", "j_{t1}", gear1.Backlash, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Backlash", "j_{t2}", "j_{t2}", gear2.Backlash, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Total Circular Backlash", "j_t", "j_{t1} + j_{t2}", pairData.TotalBacklash, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Normal Backlash", "j_n", (beta1 > 1e-6 || beta2 > 1e-6) ? "j_t \\cos\\alpha_{wt} \\cos\\beta" : "j_t \\cos\\alpha_w", pairData.NormalBacklash, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Face Width", "b_1", "b_1", faceWidth1, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Face Width", "b_2", "b_2", faceWidth2, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Pitch Diameter", "d_1", (beta1 > 1e-6 || beta2 > 1e-6) ? "m_t \\cdot z_1" : "m \\cdot z_1", gear1.PitchDiameter, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Pitch Diameter", "d_2", (beta1 > 1e-6 || beta2 > 1e-6) ? "m_t \\cdot z_2" : "m \\cdot z_2", gear2.PitchDiameter, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Base Diameter", "d_{b1}", (beta1 > 1e-6 || beta2 > 1e-6) ? "d_1 \\cdot \\cos\\alpha_t" : "d_1 \\cdot \\cos\\alpha", gear1.BaseDiameter, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Base Diameter", "d_{b2}", (beta1 > 1e-6 || beta2 > 1e-6) ? "d_2 \\cdot \\cos\\alpha_t" : "d_2 \\cdot \\cos\\alpha", gear2.BaseDiameter, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Working Pitch Diameter", "d_{w1}", (beta1 > 1e-6 || beta2 > 1e-6) ? "d_1 \\frac{\\cos\\alpha_t}{\\cos\\alpha_{wt}}" : "d_1 \\frac{\\cos\\alpha}{\\cos\\alpha_w}", dw1, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Working Pitch Diameter", "d_{w2}", (beta1 > 1e-6 || beta2 > 1e-6) ? "d_2 \\frac{\\cos\\alpha_t}{\\cos\\alpha_{wt}}" : "d_2 \\frac{\\cos\\alpha}{\\cos\\alpha_w}", dw2, "mm"));
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
            pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Tooth Thickness", "s_1", "m(\\frac{\\pi}{2} + 2 x_1 \\tan\\alpha) - j_{t1}", gear1.ToothThickness, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Tooth Thickness", "s_2", "m(\\frac{\\pi}{2} + 2 x_2 \\tan\\alpha) - j_{t2}", gear2.ToothThickness, "mm"));
            if (isInternal1) pairData.ParameterRows.Add(new GearParameterRow("Gear 1 Rim Thickness", "s_{rim1}", "s_{rim1}", rimThickness1, "mm"));
            if (isInternal2) pairData.ParameterRows.Add(new GearParameterRow("Gear 2 Rim Thickness", "s_{rim2}", "s_{rim2}", rimThickness2, "mm"));
            if (pairData.CentreDistanceOverridden)
            {
                pairData.ParameterRows.Add(new GearParameterRow("Working Circular Backlash", "j_{tw}", "j_{tw}", pairData.WorkingCircularBacklash, "mm"));
            }

            pairData.ParameterRows.Add(new GearParameterRow("Standard Centre Distance", "a", (beta1 > 1e-6 || beta2 > 1e-6) ? (isInternalPair ? "\\frac{m_t(z_2 - z_1)}{2}" : "\\frac{m_t(z_1 + z_2)}{2}") : (isInternalPair ? "\\frac{m(z_2 - z_1)}{2}" : "\\frac{m(z_1 + z_2)}{2}"), aStd, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Working Centre Distance", "a_w", (beta1 > 1e-6 || beta2 > 1e-6) ? "a \\frac{\\cos\\alpha_t}{\\cos\\alpha_{wt}}" : "a + y \\cdot m", aWorking, "mm"));
            pairData.ParameterRows.Add(new GearParameterRow("Working Pressure Angle", "\\alpha_w", (beta1 > 1e-6 || beta2 > 1e-6) ? "\\alpha_{wt}" : "\\alpha_w", pairData.WorkingPressureAngleDeg, "°"));
            pairData.ParameterRows.Add(new GearParameterRow("Centre Distance Increment Factor", "y", "\\frac{a_w - a}{m}", y, "-"));
            pairData.ParameterRows.Add(new GearParameterRow("Working Involute Function", "\\text{inv}(\\alpha_w)", (beta1 > 1e-6 || beta2 > 1e-6) ? "\\tan\\alpha_{wt} - \\alpha_{wt}" : "\\tan\\alpha_w - \\alpha_w", invAlphaW, "rad"));
            if (beta1 > 1e-6)
            {
                pairData.ParameterRows.Add(new GearParameterRow("Radial Contact Ratio", "\\epsilon_\\alpha", "\\epsilon_\\alpha", contactRatio, "-"));
                pairData.ParameterRows.Add(new GearParameterRow("Axial Contact Ratio", "\\epsilon_\\beta", "\\frac{b \\sin\\beta}{\\pi m_n}", axialContactRatio, "-"));
                pairData.ParameterRows.Add(new GearParameterRow("Total Contact Ratio", "\\epsilon_\\gamma", "\\epsilon_\\alpha + \\epsilon_\\beta", pairData.ContactRatio, "-"));
            }
            else
            {
                pairData.ParameterRows.Add(new GearParameterRow("Contact Ratio", "\\epsilon_\\alpha", "\\epsilon_\\alpha", contactRatio, "-"));
            }
            pairData.ParameterRows.Add(new GearParameterRow("Gear Ratio", "i", "\\frac{z_2}{z_1}", pairData.GearRatio, "-"));

            return pairData;
        }

        /// <summary>
        /// Circular backlash on the working pitch circles: the space width of one gear minus the tooth thickness of the
        /// other, both carried from the reference circle to the working circle with the involute function.
        /// For an external pair this is p_w - s_w1 - s_w2; for an internal pair it is the internal gear's space width
        /// minus the external gear's tooth thickness.
        /// </summary>
        private static double CalculateWorkingBacklash(
            CalculatedGearData gear1, CalculatedGearData gear2, bool isInternal1, bool isInternal2,
            double alphaTRad, double alphaWRad, double dw1, double dw2, double cosBeta)
        {
            var delta = (Math.Tan(alphaTRad) - alphaTRad) - (Math.Tan(alphaWRad) - alphaWRad);
            var alphaRad = gear1.PressureAngleRad;

            double WidthOnWorkingCircle(CalculatedGearData g, bool internalGear, double dw)
            {
                // Tooth thickness for an external gear; space width for an internal gear (it behaves like an external tooth).
                var width = internalGear
                    ? (g.Module * (Math.PI / 2.0 + 2.0 * g.ProfileShift * Math.Tan(alphaRad)) + g.Backlash) / cosBeta
                    : g.ToothThickness;
                return dw * (width / g.PitchDiameter + delta);
            }

            var w1 = WidthOnWorkingCircle(gear1, isInternal1, dw1);
            var w2 = WidthOnWorkingCircle(gear2, isInternal2, dw2);

            if (isInternal1 != isInternal2)
            {
                var space = isInternal1 ? w1 : w2;
                var tooth = isInternal1 ? w2 : w1;
                return space - tooth;
            }

            var workingPitch = Math.PI * dw1 / gear1.Teeth;
            return workingPitch - w1 - w2;
        }
    }
}
