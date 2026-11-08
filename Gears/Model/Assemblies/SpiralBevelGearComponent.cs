using System;
using Bolsover.Gears.Calculator;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>Hand of a spiral tooth trace.</summary>
    public enum SpiralHand
    {
        Left = 0,

        /// <summary>
        /// Looking from the heel towards the apex along the axis, the tooth line advances counter-clockwise from the heel to the toe, like the
        /// thread of a right hand screw. The mate of a right hand gear is a left hand gear.
        /// </summary>
        Right = 1
    }

    /// <summary>
    /// A spiral bevel gear: a bevel gear whose tooth trace is a circular arc of a face-mill cutter instead of a cone element.
    /// <para>
    /// The mean spiral angle is measured at the mean cone distance (the middle of the face). The tooth profile is the transverse (Tredgold)
    /// profile of the straight bevel gear, with the transverse pressure angle <c>tan(at) = tan(an) / cos(beta)</c>, swept through several
    /// sections along the arc (see <see cref="SpiralToothTrace"/>). The module is the transverse module at the large end, so the pitch
    /// cones, the cone angles and the cone distance are those of a straight bevel pair with the same teeth. The mate has the opposite hand
    /// and the same spiral angle, cutter radius, module and pressure angle.
    /// </para>
    /// </summary>
    public class SpiralBevelGearComponent : BevelGearComponent
    {
        /// <summary>Largest mean spiral angle (degrees).</summary>
        public const double MaxSpiralAngleDeg = 60.0;

        private double _meanSpiralAngleDeg = 35.0;

        public SpiralBevelGearComponent(string id) : base(id)
        {
        }

        /// <summary>Mean spiral angle in degrees, from 0 to <see cref="MaxSpiralAngleDeg"/> (typically 35).</summary>
        public virtual double MeanSpiralAngleDeg
        {
            get => _meanSpiralAngleDeg;
            set => _meanSpiralAngleDeg = value;
        }

        /// <summary>Radius of the face-mill cutter (mm); zero means automatic: the mean cone distance.</summary>
        public double CutterRadius { get; set; }

        public SpiralHand Hand { get; set; } = SpiralHand.Right;

        public override string TypeName => "Spiral bevel gear";

        /// <summary>The spiral angle (degrees) of this gear at the middle of the face. It is the mean spiral angle except for a hypoid gear.</summary>
        protected virtual double TraceSpiralAngleDeg => MeanSpiralAngleDeg;

        /// <summary>The spiral angle (degrees) of this gear at the middle of the face.</summary>
        public double EffectiveSpiralAngleDeg => TraceSpiralAngleDeg;

        /// <summary>Pressure angle in the plane perpendicular to the axis (degrees): <c>atan(tan(an) / cos(beta))</c>.</summary>
        public double TransversePressureAngleDeg =>
            Math.Atan(Math.Tan(PressureAngleDeg * Math.PI / 180.0) / Math.Cos(TraceSpiralAngleDeg * Math.PI / 180.0)) * 180.0 / Math.PI;

        protected override double TransverseProfilePressureAngleDeg => TransversePressureAngleDeg;

        /// <summary>Cutter radius used for the trace (mm): <see cref="CutterRadius"/> or the mean cone distance of the blank.</summary>
        public double EffectiveCutterRadiusFor(BevelBlankProfile blank)
        {
            if (blank == null) throw new ArgumentNullException(nameof(blank));
            return CutterRadius > 0.0 ? CutterRadius : blank.MeanConeDistance;
        }

        protected override SpiralToothTrace CreateTrace(BevelBlankProfile blank)
        {
            return new SpiralToothTrace(
                blank.MeanConeDistance, TraceSpiralAngleDeg * Math.PI / 180.0, EffectiveCutterRadiusFor(blank), Hand == SpiralHand.Right);
        }

        public override void Validate()
        {
            base.Validate();

            if (!(MeanSpiralAngleDeg >= 0.0 && MeanSpiralAngleDeg <= MaxSpiralAngleDeg))
            {
                throw new InvalidOperationException(
                    $"{TypeName} '{Id}': the mean spiral angle ({MeanSpiralAngleDeg}) must be from 0 to {MaxSpiralAngleDeg} degrees.");
            }

            if (Math.Abs(TraceSpiralAngleDeg) > MaxSpiralAngleDeg)
            {
                throw new InvalidOperationException(
                    $"{TypeName} '{Id}': the spiral angle at the middle of the face ({GearPrecision.Round(TraceSpiralAngleDeg)}) must not exceed {MaxSpiralAngleDeg} degrees.");
            }

            if (!(CutterRadius >= 0.0))
            {
                throw new InvalidOperationException($"{TypeName} '{Id}': the cutter radius cannot be negative.");
            }

            if (TransversePressureAngleDeg >= 45.0)
            {
                throw new InvalidOperationException(
                    $"{TypeName} '{Id}': the transverse pressure angle ({GearPrecision.Round(TransversePressureAngleDeg)}) must be below 45 degrees; use a smaller pressure angle or spiral angle.");
            }

            BevelBlankProfile blank;
            try
            {
                blank = CreateBlankProfile();
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException($"{TypeName} '{Id}': {ex.Message}", ex);
            }

            var trace = CreateTrace(blank);
            var heel = blank.ConeDistance;
            var toe = blank.ConeDistance * blank.ToeScale;
            if (!trace.Reaches(heel) || !trace.Reaches(toe))
            {
                throw new InvalidOperationException(
                    $"{TypeName} '{Id}': the cutter radius ({GearPrecision.Round(EffectiveCutterRadiusFor(blank))} mm) is too small for the face width: " +
                    $"the tooth trace must cover the cone distances {GearPrecision.Round(toe)} to {GearPrecision.Round(heel)} mm. Use a larger cutter radius or a narrower face.");
            }
        }
    }
}
