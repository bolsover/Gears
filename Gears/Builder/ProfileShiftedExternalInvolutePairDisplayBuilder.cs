using System;
using System.Collections.Generic;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Builder;

public class ProfileShiftedExternalInvolutePairDisplayBuilder :IDisplayDataBuilder
{
    public IEnumerable<DisplayData> BuildDisplayData(Gear gear)
    {
        var gearData = new List<DisplayData>();
        var module = gear.Gears[0].GetVariable(VariableIds.NormalModule);
        gearData.Add(new DisplayData("Module" , module.Value.ToString("0.000"), (25.4 / module.Value).ToString("0.0000 in DP"),
                       (Math.PI / (25.4 / module.Value)).ToString("0.0000 in CP"), false));


        return gearData;
    }
}


 // var gearData = new List<GearData>();
 //            gearData.Add(new GearData("Pinion: " + pinionIn.Style, null, null, null, false));
 //            gearData.Add(new GearData("Module", pinionIn.Module.ToString("0.000"),
 //                (25.4 / pinionIn.Module).ToString("0.0000 in DP"),
 //                (Math.PI / (25.4 / pinionIn.Module)).ToString("0.0000 in CP"), IsError(pinionIn.Module)));
 //            gearData.Add(new GearData("Radial Module", pinionOut.RadialModule.ToString("0.000"),
 //                (25.4 / pinionOut.RadialModule).ToString("0.0000 in DP"),
 //                (Math.PI / (25.4 / pinionOut.RadialModule)).ToString("0.0000 in CP"), IsError(pinionOut.RadialModule)));
 //            gearData.Add(new GearData("Teeth: ", pinionIn.Teeth.ToString("0"), null, null, false));
 //            gearData.Add(new GearData("Pressure Angle: ", pinionIn.PressureAngle.ToString("0.000°"),
 //                pinionIn.PressureAngle.ToString("0.000°"), null, IsError(pinionIn.PressureAngle)));
 //            if (!gearIn.Style.HasFlag(GearStyle.Rack))
 //            {
 //                gearData.Add(new GearData("Coefficient of Profile Shift: ",
 //                    pinionIn.CoefficientOfProfileShift.ToString("0.0000"),
 //                    pinionIn.CoefficientOfProfileShift.ToString("0.0000"), null,
 //                    IsError(pinionIn.CoefficientOfProfileShift)));
 //            }
 //            
 //            gearData.Add(new GearData("Pitch Diameter: ", pinionOut.PitchCircleDiameter.ToString("0.000 mm"),
 //                (pinionOut.PitchCircleDiameter / 25.4).ToString("0.0000 in"), null, IsError(pinionOut.PitchCircleDiameter)));
 //            gearData.Add(new GearData("Base Diameter: ", pinionOut.BaseCircleDiameter.ToString("0.000 mm"),
 //                (pinionOut.BaseCircleDiameter / 25.4).ToString("0.0000 in"), null, IsError(pinionOut.BaseCircleDiameter)));
 //            gearData.Add(new GearData("Outside Diameter: ", pinionOut.OutsideDiameter.ToString("0.000 mm"),
 //                (pinionOut.OutsideDiameter / 25.4).ToString("0.0000 in"), null, IsError(pinionOut.OutsideDiameter)));
 //            gearData.Add(new GearData("Root Diameter: ", pinionOut.RootCircleDiameter.ToString("0.000 mm"),
 //                (pinionOut.RootCircleDiameter / 25.4).ToString("0.0000 in"), null, IsError(pinionOut.RootCircleDiameter)));
 //            gearData.Add(new GearData("Addendum: ", pinionOut.Addendum.ToString("0.000 mm"),
 //                (pinionOut.Addendum / 25.4).ToString("0.0000 in"), null, IsError(pinionOut.Addendum)));
 //            gearData.Add(new GearData("Dedendum: ", pinionOut.Dedendum.ToString("0.000 mm"),
 //                (pinionOut.Dedendum / 25.4).ToString("0.0000 in"), null, IsError(pinionOut.Dedendum)));
 //            gearData.Add(new GearData("Whole Depth: ", pinionOut.WholeDepth.ToString("0.000 mm"),
 //                (pinionOut.WholeDepth / 25.4).ToString("0.0000 in"), null, IsError(pinionOut.WholeDepth)));
 //            if (!gearIn.Style.HasFlag(GearStyle.Rack))
 //            {
 //                gearData.Add(new GearData("Phi: ", pinionOut.Phi.ToString("0.000°"), pinionOut.Phi.ToString("0.000°"), null,
 //                    IsError(pinionOut.Theta)));
 //                gearData.Add(new GearData("Theta: ", pinionOut.Theta.ToString("0.000°"), pinionOut.Theta.ToString("0.000°"),
 //                    null, IsError(pinionOut.Theta)));
 //                gearData.Add(new GearData("Kappa: ", pinionOut.Kappa.ToString("0.000"), pinionOut.Kappa.ToString("0.000"),
 //                    null, IsError(pinionOut.Kappa)));
 //                gearData.Add(new GearData(" ", null, null, null, false));
 //                gearData.Add(new GearData("Gear: " + gearIn.Style, null, null, null, false));
 //                gearData.Add(new GearData("Module", gearIn.Module.ToString("0.000"),
 //                    (25.4 / gearIn.Module).ToString("0.0000 in DP"),
 //                    (Math.PI / (25.4 / gearIn.Module)).ToString("0.0000 in CP"), false));
 //                gearData.Add(new GearData("Radial Module", gearOut.RadialModule.ToString("0.000"),
 //                    (25.4 / gearOut.RadialModule).ToString("0.0000 in DP"),
 //                    (Math.PI / (25.4 / gearOut.RadialModule)).ToString("0.0000 in CP"), IsError(gearOut.RadialModule)));
 //                gearData.Add(new GearData("Teeth: ", gearIn.Teeth.ToString("0"), null, null, false));
 //                gearData.Add(new GearData("Pressure Angle: ", gearIn.PressureAngle.ToString("0.000°"),
 //                    gearIn.PressureAngle.ToString("0.000°"), null, IsError(gearIn.PressureAngle)));
 //                gearData.Add(new GearData("Coefficient of Profile Shift: ",
 //                    gearIn.CoefficientOfProfileShift.ToString("0.0000"), gearIn.CoefficientOfProfileShift.ToString("0.0000"),
 //                    null, IsError(gearIn.CoefficientOfProfileShift)));
 //                gearData.Add(new GearData("Pitch Diameter: ", gearOut.PitchCircleDiameter.ToString("0.000 mm"),
 //                    (gearOut.PitchCircleDiameter / 25.4).ToString("0.0000 in"), null, IsError(gearOut.PitchCircleDiameter)));
 //                gearData.Add(new GearData("Base Diameter: ", gearOut.BaseCircleDiameter.ToString("0.000 mm"),
 //                    (gearOut.BaseCircleDiameter / 25.4).ToString("0.0000 in"), null, IsError(gearOut.BaseCircleDiameter)));
 //                gearData.Add(new GearData("Outside Diameter: ", gearOut.OutsideDiameter.ToString("0.000 mm"),
 //                    (gearOut.OutsideDiameter / 25.4).ToString("0.0000 in"), null, IsError(gearOut.OutsideDiameter)));
 //                gearData.Add(new GearData("Root Diameter: ", gearOut.RootCircleDiameter.ToString("0.000 mm"),
 //                    (gearOut.RootCircleDiameter / 25.4).ToString("0.0000 in"), null, IsError(gearOut.RootCircleDiameter)));
 //                gearData.Add(new GearData("Addendum: ", gearOut.Addendum.ToString("0.000 mm"),
 //                    (gearOut.Addendum / 25.4).ToString("0.0000 in"), null, IsError(gearOut.Addendum)));
 //                gearData.Add(new GearData("Dedendum: ", gearOut.Dedendum.ToString("0.000 mm"),
 //                    (gearOut.Dedendum / 25.4).ToString("0.0000 in"), null, IsError(gearOut.Dedendum)));
 //                gearData.Add(new GearData("Whole Depth: ", gearOut.WholeDepth.ToString("0.000 mm"),
 //                    (gearOut.WholeDepth / 25.4).ToString("0.0000 in"), null, IsError(gearOut.WholeDepth)));
 //                gearData.Add(new GearData("Phi: ", gearOut.Phi.ToString("0.000°"), gearOut.Phi.ToString("0.000°"), null,
 //                    IsError(gearOut.Phi)));
 //                gearData.Add(new GearData("Theta: ", gearOut.Theta.ToString("0.000°"), gearOut.Theta.ToString("0.000°"),
 //                    null, IsError(gearOut.Theta)));
 //                gearData.Add(new GearData("Kappa: ", gearOut.Kappa.ToString("0.000"), gearOut.Kappa.ToString("0.000"), null,
 //                    IsError(gearOut.Kappa)));
 //                gearData.Add(new GearData(" ", null, null, null, false));
 //                gearData.Add(new GearData("Gear Pair", null, null, null, false));
 //                gearData.Add(new GearData("Working Centre Distance: ",
 //                    pairDesignInputParams.WorkingCentreDistance.ToString("0.000 mm"),
 //                    (pairDesignInputParams.WorkingCentreDistance / 25.4).ToString("0.0000 in"), null,
 //                    IsError(pairDesignInputParams.WorkingCentreDistance)));
 //                gearData.Add(new GearData("Standard Centre Distance: ", gearOut.CentreDistance.ToString("0.000 mm"),
 //                    (gearOut.CentreDistance / 25.4).ToString("0.0000 in"), null, IsError(gearOut.CentreDistance)));
 //                gearData.Add(new GearData("Centre Distance Increment Factor: ",
 //                    gearOut.CentreDistanceIncrementFactor.ToString("0.0000"),
 //                    gearOut.CentreDistanceIncrementFactor.ToString("0.0000"), null,
 //                    IsError(gearOut.CentreDistanceIncrementFactor)));
 //                gearData.Add(new GearData("Diff Coefficient Of Profile Shift: ",
 //                    gearOut.DifferenceCoefficientOfProfileShift.ToString("0.0000"),
 //                    gearOut.DifferenceCoefficientOfProfileShift.ToString("0.0000"), null,
 //                    IsError(gearOut.DifferenceCoefficientOfProfileShift)));
 //                gearData.Add(new GearData("Circular Backlash Required: ", gearIn.CircularBacklash.ToString("0.0000"),
 //                    gearIn.CircularBacklash.ToString("0.0000"), null, IsError(gearIn.CircularBacklash)));
 //                gearData.Add(new GearData("Backlash Adjustment Factor: ",
 //                    gearOut.BacklashAdjustmentFactorXMod.ToString("0.0000"),
 //                    gearOut.BacklashAdjustmentFactorXMod.ToString("0.0000"), null,
 //                    IsError(gearOut.BacklashAdjustmentFactorXMod)));
 //                gearData.Add(new GearData("Radial Involute Function: ", gearOut.RadialInvoluteFunction.ToString("0.0000"),
 //                    gearOut.RadialInvoluteFunction.ToString("0.0000"), null, IsError(gearOut.RadialInvoluteFunction)));
 //                gearData.Add(new GearData("Radial Working Involute Function: ",
 //                    gearOut.RadialWorkingInvoluteFunction.ToString("0.0000"),
 //                    gearOut.RadialWorkingInvoluteFunction.ToString("0.0000"), null,
 //                    IsError(gearOut.RadialWorkingInvoluteFunction)));
 //                gearData.Add(new GearData("Radial Pressure Angle: ", gearOut.RadialPressureAngle.ToString("0.000°"),
 //                    gearOut.RadialPressureAngle.ToString("0.000°"), null, IsError(gearOut.RadialPressureAngle)));
 //                gearData.Add(new GearData("Radial Working Pressure Angle: ",
 //                    gearOut.RadialWorkingPressureAngle.ToString("0.000°"),
 //                    gearOut.RadialWorkingPressureAngle.ToString("0.000°"), null,
 //                    IsError(gearOut.RadialWorkingPressureAngle)));
 //                gearData.Add(new GearData("Axial Pitch: ", gearOut.AxialPitch.ToString("0.000 mm"),
 //                    (gearOut.AxialPitch / 25.4).ToString("0.0000 in"), null, IsError(gearOut.AxialPitch)));
 //                gearData.Add(new GearData("Working Pressure Angle: ", gearOut.WorkingPressureAngle.ToString("0.000°"),
 //                    gearOut.WorkingPressureAngle.ToString("0.000°"), null, IsError(gearOut.WorkingPressureAngle)));
 //                gearData.Add(new GearData("Working Involute Function: ", gearOut.WorkingInvoluteFunction.ToString("0.0000"),
 //                    gearOut.WorkingInvoluteFunction.ToString("0.0000"), null, IsError(gearOut.WorkingInvoluteFunction)));
 //                gearData.Add(new GearData("ContactRatio alpha: ", gearOut.ContactRatioAlpha.ToString("0.0000"),
 //                    gearOut.ContactRatioAlpha.ToString("0.0000"), null, IsError(gearOut.ContactRatioAlpha)));
 //                gearData.Add(new GearData("ContactRatio beta: ", gearOut.ContactRatioBeta.ToString("0.0000"),
 //                    gearOut.ContactRatioBeta.ToString("0.0000"), null, IsError(gearOut.ContactRatioBeta)));
 //                gearData.Add(new GearData("ContactRatio gamma: ", gearOut.ContactRatioGamma.ToString("0.0000"),
 //                    gearOut.ContactRatioGamma.ToString("0.0000"), null, IsError(gearOut.ContactRatioGamma)));
 //            }
 //            else
 //            {
 //                gearData.Add(new GearData(" ", null, null, null, false));
 //                gearData.Add(new GearData("Rack: " + gearIn.Style, null, null, null, false));
 //                gearData.Add(new GearData("Module", gearIn.Module.ToString("0.000"),
 //                    (25.4 / gearIn.Module).ToString("0.0000 in DP"),
 //                    (Math.PI / (25.4 / gearIn.Module)).ToString("0.0000 in CP"), false));
 //                gearData.Add(new GearData("Teeth: ", gearIn.Teeth.ToString("0"), null, null, false));
 //                gearData.Add(new GearData("Pressure Angle: ", gearIn.PressureAngle.ToString("0.000°"),
 //                    gearIn.PressureAngle.ToString("0.000°"), null, IsError(gearIn.PressureAngle)));
 //                gearData.Add(new GearData(" ", null, null, null, false));
 //                gearData.Add(new GearData("Gear Pair", null, null, null, false));
 //                var centreDistance = pinionOut.PitchCircleDiameter / 2;
 //                gearData.Add(new GearData("Working Centre Distance: ", centreDistance.ToString("0.000 mm"),
 //                    (centreDistance / 25.4).ToString("0.0000 in"), null,
 //                    IsError(pairDesignInputParams.WorkingCentreDistance)));
 //            }
            