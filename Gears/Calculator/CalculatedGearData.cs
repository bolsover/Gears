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
}
