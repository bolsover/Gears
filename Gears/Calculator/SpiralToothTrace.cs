using System;

namespace Bolsover.Gears.Calculator
{
    /// <summary>
    /// The tooth trace of a spiral or zerol bevel gear: a circular arc of the face-mill cutter (radius <c>rc</c>) on the developed pitch
    /// cone, instead of a cone element. The mean spiral angle <c>betaM</c> is measured at the mean cone distance <c>Rm</c>.
    /// <para>
    /// With the cutter centre at <c>R0</c> from the apex, <c>R0^2 = Rm^2 + rc^2 - 2 Rm rc sin(betaM)</c>, the spiral angle at the cone distance R
    /// is <c>sin(beta) = (R^2 + rc^2 - R0^2) / (2 R rc)</c> and the polar angle of the trace seen from the apex is
    /// <c>cos(phi) = (R^2 + R0^2 - rc^2) / (2 R R0)</c>. The pitch cone develops into a sector of angle <c>2 pi sin(delta)</c>, so the polar angle
    /// phi on the developed cone is <c>sin(delta)</c> times the angle about the axis, and the twist of a section about the gear axis is
    /// <c>(phi(R) - phi(Rm)) / sin(delta)</c>, which is zero at the mean section. For the right hand this is the angle (counter-clockwise about
    /// the axis that points from the heel to the apex) that the trace has at the cone distance R relative to the mean point: going from the
    /// heel to the toe the trace advances counter-clockwise like a right hand helix. The left hand has the opposite sign.
    /// </para>
    /// </summary>
    public sealed class SpiralToothTrace
    {
        private readonly double _cutterCentreDistance;
        private readonly double _phiMean;

        public SpiralToothTrace(double meanConeDistance, double meanSpiralAngleRad, double cutterRadius, bool rightHand)
        {
            if (!(meanConeDistance > 0.0)) throw new ArgumentException("The mean cone distance must be positive.", nameof(meanConeDistance));
            if (!(cutterRadius > 0.0)) throw new ArgumentException("The cutter radius must be positive.", nameof(cutterRadius));
            if (double.IsNaN(meanSpiralAngleRad) || Math.Abs(meanSpiralAngleRad) >= Math.PI / 2.0)
            {
                throw new ArgumentException("The mean spiral angle must be between -90 and 90 degrees.", nameof(meanSpiralAngleRad));
            }

            MeanConeDistance = meanConeDistance;
            MeanSpiralAngleRad = meanSpiralAngleRad;
            CutterRadius = cutterRadius;
            RightHand = rightHand;
            var r0Squared = meanConeDistance * meanConeDistance + cutterRadius * cutterRadius
                            - 2.0 * meanConeDistance * cutterRadius * Math.Sin(meanSpiralAngleRad);
            _cutterCentreDistance = Math.Sqrt(r0Squared);
            _phiMean = PolarAngle(meanConeDistance);
        }

        private SpiralToothTrace()
        {
            IsStraight = true;
            RightHand = true;
        }

        /// <summary>The trace of a straight bevel gear: a cone element with no spiral angle and no twist.</summary>
        public static SpiralToothTrace Straight { get; } = new SpiralToothTrace();

        /// <summary>True for <see cref="Straight"/>.</summary>
        public bool IsStraight { get; }

        public double MeanConeDistance { get; }

        public double MeanSpiralAngleRad { get; }

        public double CutterRadius { get; }

        public bool RightHand { get; }

        /// <summary>Distance of the cutter centre from the cone apex.</summary>
        public double CutterCentreDistance => _cutterCentreDistance;

        /// <summary>The same trace for the other hand (the mate of a spiral gear).</summary>
        public SpiralToothTrace WithHand(bool rightHand)
        {
            return IsStraight ? this : new SpiralToothTrace(MeanConeDistance, MeanSpiralAngleRad, CutterRadius, rightHand);
        }

        /// <summary>True when the trace reaches the cone distance (the cutter circle covers it).</summary>
        public bool Reaches(double coneDistance)
        {
            if (IsStraight) return true;
            return coneDistance > 0.0
                   && coneDistance >= Math.Abs(_cutterCentreDistance - CutterRadius) * (1.0 + 1e-9)
                   && coneDistance <= (_cutterCentreDistance + CutterRadius) * (1.0 - 1e-9);
        }

        /// <summary>Spiral angle (rad) at the cone distance: <c>sin(beta) = (R^2 + rc^2 - R0^2) / (2 R rc)</c>. Zero for a straight trace.</summary>
        public double SpiralAngleAt(double coneDistance)
        {
            if (IsStraight) return 0.0;
            CheckReach(coneDistance);
            var s = (coneDistance * coneDistance + CutterRadius * CutterRadius - _cutterCentreDistance * _cutterCentreDistance)
                    / (2.0 * coneDistance * CutterRadius);
            return Math.Asin(Math.Max(-1.0, Math.Min(1.0, s)));
        }

        /// <summary>
        /// Twist (rad) of the section at the cone distance about the gear axis, <c>(phi(R) - phi(Rm)) / sin(delta)</c>, negated for the left
        /// hand. Zero at the mean cone distance, and always zero for a straight trace.
        /// </summary>
        public double TwistAt(double coneDistance, double pitchConeAngleRad)
        {
            if (IsStraight) return 0.0;
            CheckReach(coneDistance);
            var sinDelta = Math.Sin(pitchConeAngleRad);
            if (!(sinDelta > 1e-6)) throw new ArgumentOutOfRangeException(nameof(pitchConeAngleRad), "The pitch cone angle must be positive.");
            var twist = (PolarAngle(coneDistance) - _phiMean) / sinDelta;
            return RightHand ? twist : -twist;
        }

        private double PolarAngle(double coneDistance)
        {
            var c = (coneDistance * coneDistance + _cutterCentreDistance * _cutterCentreDistance - CutterRadius * CutterRadius)
                    / (2.0 * coneDistance * _cutterCentreDistance);
            return Math.Acos(Math.Max(-1.0, Math.Min(1.0, c)));
        }

        private void CheckReach(double coneDistance)
        {
            if (!Reaches(coneDistance))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(coneDistance),
                    $"The cutter of radius {CutterRadius:0.###} mm does not reach the cone distance {coneDistance:0.###} mm; " +
                    $"it reaches {Math.Abs(_cutterCentreDistance - CutterRadius):0.###} to {_cutterCentreDistance + CutterRadius:0.###} mm.");
            }
        }
    }
}
