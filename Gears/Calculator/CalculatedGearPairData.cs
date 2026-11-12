using System.Collections.Generic;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Calculator
{
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

        /// <summary>Angle between the two shaft axes in degrees; 0 for a parallel pair.</summary>
        public double ShaftAngleDeg { get; set; }

        /// <summary>True for a crossed helical pair, whose teeth touch at a point: the contact ratio is not calculated and stays 0.</summary>
        public bool IsCrossedAxis { get; set; }

        public List<GearParameterRow> ParameterRows { get; } = new List<GearParameterRow>();
    }
}
