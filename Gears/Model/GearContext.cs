using System;

namespace Bolsover.Gears.Model
{
    /// <summary>
    /// Represents the context or type of a gear in the system, using flags to allow combinations of gear attributes.
    /// </summary>
    /// <remarks>
    /// GearContext defines various types and characteristics of gears, including categories such as external, internal,
    /// involute, helical, or specific gear pair combinations. This enumeration can be combined using bitwise operators
    /// to describe complex gear configurations.
    /// </remarks>
    [Flags]
    public enum GearContext
    {
        Undefined = 0,
        External = 1 << 0,
        Internal = 1 << 1,
        Involute = 1 << 2,
        Helical = 1 << 3,
        Pair = 1 << 4,
        Bevel = 1 << 5,
        Worm = 1 << 6,
        WormGear = 1 << 7,
        Cycloidal = 1 << 8,
        ProfileShifted = 1 << 9,
        Normal = 1 << 10,
        Radial = 1 << 11,
        Rack = 1 << 12,
        Straight = 1 << 13,
        Gleason = 1 << 14,
        Standard = 1 << 15,

        ExternalInvolute = External | Involute,
        ExternalInvolutePair = External | Involute | Pair,
        ProfileShiftedExternalInvolutePair = ProfileShifted | External | Involute | Pair,
        ProfileShiftedInternalExternalInvolutePair = ProfileShifted | Internal | External | Involute | Pair,
        ProfileShiftedInternalExternalHelicalPair = ProfileShifted | Internal | External | Helical | Pair,
        ExternalNormalHelical = External | Normal | Helical,
        ExternalRadialHelical = External | Radial | Helical,
        ProfileShiftedExternalNormalHelicalPair = ProfileShifted | External | Normal | Helical | Pair,
        ProfileShiftedExternalRadialHelicalPair = ProfileShifted | External | Radial | Helical | Pair,
        StraightBevelGleasonPair = Straight | Bevel | Gleason | Pair,
        StraightBevelStandardPair = Straight | Bevel | Standard | Pair,

    }
}