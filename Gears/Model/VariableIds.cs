using System;
using System.ComponentModel;

namespace Bolsover.Gears.Model;

/// <summary>
/// Defines a set of enumerated values representing various gear-related variables.
/// </summary>
[Flags]
public enum VariableIds
{
    [Description("Addendum"), Symbol("ha")]
    Addendum,
    [Description("Addendum Angle"), Symbol("thetaa")]
    AddendumAngle,
    [Description("AssignedProfileShift"), Symbol("xagn")]
    AssignedProfileShift,
    [Description("Axial Contact Ratio"), Symbol("epsilonbeta")]
    AxialContactRatio,
    [Description("Axial Face Width"), Symbol("Xb")]
    AxialFaceWidth,
    [Description("Axial Module"), Symbol("mt")]
    AxialModule,
    [Description("Base Diameter"), Symbol("db")]
    BaseDiameter,
    [Description("Centre Distance Increment Factor"), Symbol("y")]
    CentreDistanceIncrementFactor,
    [Description("Circular Backlash"), Symbol("jt")]
    CircularBacklash,
    [Description("Coefficient Of Profile Shift"), Symbol("x")]
    CoefficientOfProfileShift,
    [Description("Cone Distance"), Symbol("Re")]
    ConeDistance,
    [Description("Contact Ratio"), Symbol("epsilongamma")]
    ContactRatio,
    [Description("Dedendum"), Symbol("hf")]
    Dedendum,
    [Description("Dedendum Angle"), Symbol("thetaf")]
    DedendumAngle,
    [Description("Difference Coefficient Of Profile Shift"), Symbol("Deltax")]
    DifferenceCoefficientOfProfileShift,
    [Description("Face Width"), Symbol("b")]
    FaceWidth,
    [Description("Hand"), Symbol("hand")] // 0 = L, 1 = R
    Hand,
    [Description("HeightOfGear"), Symbol("b")]
    HeightOfGear,
    [Description("Helix Angle"), Symbol("beta")]
    HelixAngle,
    [Description("Inner Outside Diameter"), Symbol("di")]
    InnerOutsideDiameter,
    [Description("Involute Function"), Symbol("inv")]
    InvoluteFunction,
    [Description("Kappa"), Symbol("kappa")]
    Kappa,
    [Description("Lead Angle"), Symbol("gamma")]
    LeadAngle,
    [Description("Module"), Symbol("m")]
    Module,
    [Description("Normal Coefficient Of Profile Shift"), Symbol("xn")]
    NormalCoefficientOfProfileShift,
    [Description("Normal Module"), Symbol("mn")]
    NormalModule,
    [Description("Normal Pressure Angle"), Symbol("alphan")]
    NormalPressureAngle,
    [Description("Outer Cone Angle"), Symbol("deltaa")]
    OuterConeAngle,
    [Description("Outside Diameter"), Symbol("da")]
    OutsideDiameter,
    [Description("Outside Radial Module"), Symbol("mtoe")]
    OutsideRadialModule,
    [Description("Phi"), Symbol("phi")]
    Phi,
    [Description("Pitch Apex To Crown"), Symbol("X")]
    PitchApexToCrown,
    [Description("Pitch Cone Angle"), Symbol("delta")]
    PitchConeAngle,
    [Description("Pitch Diameter"), Symbol("d")]
    PitchDiameter,
    [Description("Pressure Angle"), Symbol("alpha")]
    PressureAngle,
    [Description("ProfileShiftModificationForBacklash"), Symbol("xmod")]
    ProfileShiftModificationForBacklash,
    [Description("Radial Coefficient Of Profile Shift"), Symbol("xt")]
    RadialCoefficientOfProfileShift,
    [Description("Radial Contact Ratio"), Symbol("epsilonalpha")]
    RadialContactRatio,
    [Description("Radial Involute Function"), Symbol("invt")]
    RadialInvoluteFunction,
    [Description("Radial Module"), Symbol("mt")]
    RadialModule,
    [Description("Radial Pressure Angle"), Symbol("alphat")]
    RadialPressureAngle,
    [Description("Radial Working Involute Function"), Symbol("invtw")]
    RadialWorkingInvoluteFunction,
    [Description("Radial Working Pitch Diameter"), Symbol("dwt")]
    RadialWorkingPitchDiameter,
    [Description("Radial Working Pressure Angle"), Symbol("alphawt")]
    RadialWorkingPressureAngle,
    [Description("Root Cone Angle"), Symbol("deltaf")]
    RootConeAngle,
    [Description("Root Diameter"), Symbol("df")]
    RootDiameter,
    [Description("Root Fillet Factor"), Symbol("fr")]
    RootFilletFactor,
    [Description("RootFilletDiameter"), Symbol("rfd")]
    RootFilletDiameter,
    [Description("Shaft Angle"), Symbol("Sigma")]
    ShaftAngle,
    [Description("Speed Ratio"), Symbol("i")]
    SpeedRatio,
    [Description("Spiral Angle"), Symbol("beta")]
    SpiralAngle,
    [Description("Centre Distance"), Symbol("a")]
    StandardCentreDistance,
    [Description("Sum Coefficient Of Profile Shift"), Symbol("Sigmax")]
    SumCoefficientOfProfileShift,
    [Description("Sum Normal Coefficient Of Profile Shift"), Symbol("Sigmaxn")]
    SumNormalCoefficientOfProfileShift,
    [Description("Sum Radial Coefficient Of Profile Shift"), Symbol("Sigmaxt")]
    SumRadialCoefficientOfProfileShift,
    [Description("Teeth"), Symbol("z")]
    Teeth,
    [Description("Theta"), Symbol("theta")]
    Theta,
    [Description("Threads"), Symbol("zw")]
    Threads,
    [Description("Throat Diameter"), Symbol("dt")]
    ThroatDiameter,
    [Description("Throat Surface Radius"), Symbol("rt")]
    ThroatSurfaceRadius,
    [Description("Tip Relief Factor"), Symbol("ft")]
    TipReliefFactor,
    [Description("TipReliefRadius"), Symbol("trr")]
    TipReliefRadius,
    [Description("Whole Depth"), Symbol("h")]
    WholeDepth,
    [Description("Working Centre Distance"), Symbol("ax")]
    WorkingCentreDistanceax,
    [Description("Working Involute Function"), Symbol("invw")]
    WorkingInvoluteFunction,
    [Description("Working Pitch Diameter"), Symbol("dw")]
    WorkingPitchDiameter,
    [Description("Working Pressure Angle"), Symbol("alphaw")]
    WorkingPressureAngle,
    [Description("Working Pressure Angle"), Symbol("alphaw#")]
    WorkingPressureAngleNR
}

public class SymbolAttribute : Attribute
{
    public string Symbol { get; }

    public SymbolAttribute(string symbol)
    {
        Symbol = symbol;
    }
}