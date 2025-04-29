using System;

namespace Bolsover.Gears.Model;

/// <summary>
/// Defines a set of enumerated values representing various gear-related variables.
/// </summary>
[Flags]
public enum VariableNames
{
    [System.ComponentModel.Description("Module"), Symbol("m")]
    Module,

    [System.ComponentModel.Description("Pressure Angle"), Symbol("alpha")]
    PressureAngle,

    [System.ComponentModel.Description("Teeth"), Symbol("z")]
    Teeth,

    [System.ComponentModel.Description("Teeth"), Symbol("z1")]
    Teethz1,

    [System.ComponentModel.Description("Teeth"), Symbol("z2")]
    Teethz2,

    [System.ComponentModel.Description("Centre Distance"), Symbol("a")]
    StandardCentreDistance,

    [System.ComponentModel.Description("Pitch Diameter"), Symbol("d")]
    PitchDiameter,

    [System.ComponentModel.Description("Pitch Diameter"), Symbol("d1")]
    PitchDiameterd1,

    [System.ComponentModel.Description("Pitch Diameter"), Symbol("d2")]
    PitchDiameterd2,

    [System.ComponentModel.Description("Working Pitch Diameter"), Symbol("dw")]
    WorkingPitchDiameter,

    [System.ComponentModel.Description("Working Pitch Diameter"), Symbol("dw1")]
    WorkingPitchDiameterdw1,

    [System.ComponentModel.Description("Working Pitch Diameter"), Symbol("dw2")]
    WorkingPitchDiameterdw2,
    
    [System.ComponentModel.Description("Radial Working Pitch Diameter"), Symbol("dwt1")]
    RadialWorkingPitchDiameterdwt1,

    [System.ComponentModel.Description("Radial Working Pitch Diameter"), Symbol("dwt2")]
    RadialWorkingPitchDiameterdwt2,

    [System.ComponentModel.Description("Base Diameter"), Symbol("db")]
    BaseDiameter,

    [System.ComponentModel.Description("Base Diameter"), Symbol("db1")]
    BaseDiameterdb1,

    [System.ComponentModel.Description("Base Diameter"), Symbol("db2")]
    BaseDiameterdb2,

    [System.ComponentModel.Description("Addendum"), Symbol("ha")]
    Addendum,

    [System.ComponentModel.Description("Addendum"), Symbol("ha1")]
    Addendumha1,

    [System.ComponentModel.Description("Addendum"), Symbol("ha2")]
    Addendumha2,

    [System.ComponentModel.Description("Dedendum"), Symbol("hf")]
    Dedendum,

    [System.ComponentModel.Description("Dedendum"), Symbol("hf1")]
    Dedendumhf1,

    [System.ComponentModel.Description("Dedendum"), Symbol("hf2")]
    Dedendumhf2,

    [System.ComponentModel.Description("Outside Diameter"), Symbol("da")]
    OutsideDiameter,

    [System.ComponentModel.Description("Outside Diameter"), Symbol("da1")]
    OutsideDiameterda1,

    [System.ComponentModel.Description("Outside Diameter"), Symbol("da2")]
    OutsideDiameterda2,

    [System.ComponentModel.Description("Root Diameter"), Symbol("df")]
    RootDiameter,

    [System.ComponentModel.Description("Root Diameter"), Symbol("df1")]
    RootDiameterdf1,

    [System.ComponentModel.Description("Root Diameter"), Symbol("df2")]
    RootDiameterdf2,

    [System.ComponentModel.Description("Helix Angle"), Symbol("beta")]
    HelixAngle,

    [System.ComponentModel.Description("Speed Ratio"), Symbol("i")]
    SpeedRatio,

    [System.ComponentModel.Description("Radial Pressure Angle"), Symbol("alphat")]
    RadialPressureAngle,

    [System.ComponentModel.Description("Radial Working Pressure Angle"), Symbol("alphawt")]
    RadialWorkingPressureAngle,

    [System.ComponentModel.Description("Normal Module"), Symbol("mn")]
    NormalModule,

    [System.ComponentModel.Description("Normal Pressure Angle"), Symbol("alphan")]
    NormalPressureAngle,

    [System.ComponentModel.Description("Whole Depth"), Symbol("h")]
    WholeDepth,

    [System.ComponentModel.Description("Hand"), Symbol("hand")] // 0 = L, 1 = R
    Hand,

    [System.ComponentModel.Description("Hand"), Symbol("hand1")] // 0 = L, 1 = R
    Hand1,

    [System.ComponentModel.Description("Hand"), Symbol("hand2")] // 0 = L, 1 = R
    Hand2,

    [System.ComponentModel.Description("Radial Module"), Symbol("mt")]
    RadialModule,

    [System.ComponentModel.Description("Coefficient Of Profile Shift"), Symbol("x")]
    CoefficientOfProfileShift,

    [System.ComponentModel.Description("Coefficient Of Profile Shift"), Symbol("x1")]
    CoefficientOfProfileShiftx1,

    [System.ComponentModel.Description("Coefficient Of Profile Shift"), Symbol("x2")]
    CoefficientOfProfileShiftx2,

    [System.ComponentModel.Description("Sum Coefficient Of Profile Shift"), Symbol("Sigmax")]
    SumCoefficientOfProfileShift,

    [System.ComponentModel.Description("Normal Coefficient Of Profile Shift"), Symbol("xn")]
    NormalCoefficientOfProfileShift,

    [System.ComponentModel.Description("Normal Coefficient Of Profile Shift"), Symbol("xn1")]
    NormalCoefficientOfProfileShiftxn1,

    [System.ComponentModel.Description("Normal Coefficient Of Profile Shift"), Symbol("xn2")]
    NormalCoefficientOfProfileShiftxn2,

    [System.ComponentModel.Description("Sum Normal Coefficient Of Profile Shift"), Symbol("Sigmaxn")]
    SumNormalCoefficientOfProfileShift,

    [System.ComponentModel.Description("Involute Function"), Symbol("inv")]
    InvoluteFunction,

    [System.ComponentModel.Description("Working Involute Function"), Symbol("invw")]
    WorkingInvoluteFunction,

    [System.ComponentModel.Description("Radial Working Involute Function"), Symbol("invtw")]
    RadialWorkingInvoluteFunction,

    [System.ComponentModel.Description("Centre Distance Increment Factor"), Symbol("y")]
    CentreDistanceIncrementFactor,

    [System.ComponentModel.Description("Working Pressure Angle"), Symbol("alphaw")]
    WorkingPressureAngle,

   [System.ComponentModel.Description("Working Centre Distance"), Symbol("ax")]
    WorkingCentreDistanceax,

    [System.ComponentModel.Description("Difference Coefficient Of Profile Shift"), Symbol("Deltax")]
    DifferenceCoefficientOfProfileShift,

    [System.ComponentModel.Description("Sum Radial Coefficient Of Profile Shift"), Symbol("Sigmaxt")]
    SumRadialCoefficientOfProfileShift,

    [System.ComponentModel.Description("Radial Coefficient Of Profile Shift"), Symbol("xt")]
    RadialCoefficientOfProfileShift,

    [System.ComponentModel.Description("Radial Coefficient Of Profile Shift"), Symbol("xt1")]
    RadialCoefficientOfProfileShiftxt1,

    [System.ComponentModel.Description("Radial Coefficient Of Profile Shift"), Symbol("xt2")]
    RadialCoefficientOfProfileShiftxt2,

    [System.ComponentModel.Description("Axial Module"), Symbol("mt")]
    AxialModule,

    [System.ComponentModel.Description("Lead Angle"), Symbol("gamma")]
    LeadAngle,

    [System.ComponentModel.Description("Throat Diameter")]
    ThroatDiameter,

    [System.ComponentModel.Description("Throat Surface Radius")]
    ThroatSurfaceRadius,

    [System.ComponentModel.Description("Threads")]
    Threads,

    [System.ComponentModel.Description("Shaft Angle"), Symbol("Sigma")]
    ShaftAngle,

    [System.ComponentModel.Description("Outside Radial Module")]
    OutsideRadialModule,

    [System.ComponentModel.Description("Spiral Angle")]
    SpiralAngle,

    [System.ComponentModel.Description("Pitch Cone Angle"), Symbol("delta1")]
    PitchConeAngledelta1,
    
    [System.ComponentModel.Description("Pitch Cone Angle"), Symbol("delta2")]
    PitchConeAngledelta2,

    [System.ComponentModel.Description("Cone Distance"), Symbol("Re")]
    ConeDistance,

    [System.ComponentModel.Description("Face Width"), Symbol("b")]
    FaceWidth,

    [System.ComponentModel.Description("Addendum Angle"), Symbol("thetaa")]
    AddendumAngle,
    [System.ComponentModel.Description("Addendum Angle"), Symbol("thetaa1")]
    AddendumAnglea1,
    [System.ComponentModel.Description("Addendum Angle"), Symbol("thetaa2")]
    AddendumAnglea2,

    [System.ComponentModel.Description("Dedendum Angle"), Symbol("thetaf")]
    DedendumAngle,
    [System.ComponentModel.Description("Dedendum Angle"), Symbol("thetaf1")]
    DedendumAnglef1,
    [System.ComponentModel.Description("Dedendum Angle"), Symbol("thetaf2")]
    DedendumAnglef2,

    [System.ComponentModel.Description("Outer Cone Angle"), Symbol("deltaa")]
    OuterConeAngle,
    [System.ComponentModel.Description("Outer Cone Angle"), Symbol("deltaa1")]
    OuterConeAngledeltaa1,
    [System.ComponentModel.Description("Outer Cone Angle"), Symbol("deltaa2")]
    OuterConeAngledeltaa2,

    [System.ComponentModel.Description("Root Cone Angle"), Symbol("deltaf")]
    RootConeAngle,
    [System.ComponentModel.Description("Root Cone Angle"), Symbol("deltaf1")]
    RootConeAngledeltaf1,
    [System.ComponentModel.Description("Root Cone Angle"), Symbol("deltaf2")]
    RootConeAngledeltaf2,

    [System.ComponentModel.Description("Pitch Apex To Crown"), Symbol("X")]
    PitchApexToCrown,
    [System.ComponentModel.Description("Pitch Apex To Crown"), Symbol("X1")]
    PitchApexToCrownX1,
    [System.ComponentModel.Description("Pitch Apex To Crown"), Symbol("X2")]
    PitchApexToCrownX2,

    [System.ComponentModel.Description("Axial Face Width"), Symbol("Xb")]
    AxialFaceWidth,
    [System.ComponentModel.Description("Axial Face Width"), Symbol("Xb1")]
    AxialFaceWidthXb1,
    [System.ComponentModel.Description("Axial Face Width"), Symbol("Xb2")]
    AxialFaceWidthXb2,

    [System.ComponentModel.Description("Inner Outside Diameter"), Symbol("di")]
    InnerOutsideDiameter,
    [System.ComponentModel.Description("Inner Outside Diameter"), Symbol("di1")]
    InnerOutsideDiameterdi1,
    [System.ComponentModel.Description("Inner Outside Diameter"), Symbol("di2")]
    InnerOutsideDiameterdi2,

    [System.ComponentModel.Description("Radial Contact Ratio"), Symbol("epsilonalpha")]
    RadialContactRatio,

    [System.ComponentModel.Description("Contact Ratio"), Symbol("epsilongamma")]
    ContactRatio,
    [System.ComponentModel.Description("Axial Contact Ratio"), Symbol("epsilonbeta")]
    AxialContactRatio,
    [System.ComponentModel.Description("HeightOfGear"), Symbol("b")]
    HeightOfGear,
    [System.ComponentModel.Description("RadialInvoluteFunction"), Symbol("invt")]
    RadialInvoluteFunction,
    // [System.ComponentModel.Description("FaceWidth"), Symbol("b")]
    // FaceWidth,
}

public class SymbolAttribute : Attribute
{
    public string Symbol { get; }

    public SymbolAttribute(string symbol)
    {
        Symbol = symbol;
    }
}