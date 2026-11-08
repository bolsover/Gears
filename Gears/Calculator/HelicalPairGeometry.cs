using System;

namespace Bolsover.Gears.Calculator
{
    /// <summary>
    /// Per-gear transverse geometry of a helical gear pair. Each gear has its own transverse module <c>mt = mn / cos(beta)</c> and pitch diameter
    /// <c>d = z * mt</c>, so that a pair with different helix angles (a crossed helical pair) gets the right centre distance. For equal helix
    /// angles the numbers are identical to the single transverse module used for parallel pairs.
    /// </summary>
    public static class HelicalPairGeometry
    {
        /// <summary>Largest difference in degrees between a shaft angle and the one the helix angles give that is still accepted.</summary>
        public const double ShaftAngleToleranceDeg = 0.01;

        /// <summary>Transverse module of a gear with the given normal module and helix angle (degrees).</summary>
        public static double TransverseModule(double normalModule, double helixAngleDeg)
        {
            var cosBeta = Math.Cos(helixAngleDeg * Math.PI / 180.0);
            if (cosBeta < 1e-6) cosBeta = 1e-6;
            return normalModule / cosBeta;
        }

        /// <summary>Pitch diameter of a gear with profile shift zero: <c>z * mn / cos(beta)</c>.</summary>
        public static double PitchDiameter(int teeth, double normalModule, double helixAngleDeg)
        {
            return teeth * TransverseModule(normalModule, helixAngleDeg);
        }

        /// <summary>
        /// Standard centre distance of the pair: half the sum of the pitch diameters of an external pair, or half the difference for an
        /// internal pair. The gears do not need the same helix angle.
        /// </summary>
        public static double StandardCentreDistance(
            int z1, double helixAngle1Deg, bool isInternal1,
            int z2, double helixAngle2Deg, bool isInternal2,
            double normalModule)
        {
            if (Math.Abs(helixAngle1Deg - helixAngle2Deg) < 1e-12)
            {
                // The same transverse module for both gears, exactly as it has always been calculated.
                var mt = TransverseModule(normalModule, helixAngle1Deg);
                var sumZ = isInternal1 || isInternal2
                    ? (isInternal2 ? z2 : z1) - (isInternal2 ? z1 : z2)
                    : z1 + z2;
                return mt * sumZ / 2.0;
            }

            var d1 = PitchDiameter(z1, normalModule, helixAngle1Deg);
            var d2 = PitchDiameter(z2, normalModule, helixAngle2Deg);
            if (isInternal1 || isInternal2)
            {
                return (isInternal2 ? d2 - d1 : d1 - d2) / 2.0;
            }

            return (d1 + d2) / 2.0;
        }

        /// <summary>
        /// Angle between the two shaft axes (degrees) at which two helical gears of the given helix angles and hands mesh:
        /// <c>beta1 + beta2</c> for the same hand and <c>|beta1 - beta2|</c> for opposite hands. Equal angles of opposite hands give 0,
        /// which is the parallel mesh.
        /// </summary>
        public static double ExpectedShaftAngleDeg(double helixAngle1Deg, string hand1, double helixAngle2Deg, string hand2)
        {
            return SameHand(hand1, hand2)
                ? helixAngle1Deg + helixAngle2Deg
                : Math.Abs(helixAngle1Deg - helixAngle2Deg);
        }

        /// <summary>True when both hands are the same; a missing hand counts as right.</summary>
        public static bool SameHand(string hand1, string hand2) => Normalise(hand1) == Normalise(hand2);

        /// <summary>
        /// Throws when the shaft angle differs from <see cref="ExpectedShaftAngleDeg"/> by more than <see cref="ShaftAngleToleranceDeg"/>.
        /// The message gives the expected angle.
        /// </summary>
        public static void CheckShaftAngle(string driverId, string drivenId, double shaftAngleDeg,
            double helixAngle1Deg, string hand1, double helixAngle2Deg, string hand2)
        {
            var expected = ExpectedShaftAngleDeg(helixAngle1Deg, hand1, helixAngle2Deg, hand2);
            if (double.IsNaN(shaftAngleDeg) || double.IsInfinity(shaftAngleDeg) || Math.Abs(shaftAngleDeg - expected) > ShaftAngleToleranceDeg)
            {
                throw new InvalidOperationException(
                    $"Gears '{driverId}' and '{drivenId}' cannot mesh on shafts at {shaftAngleDeg:0.####} degrees: helix angles of " +
                    $"{helixAngle1Deg:0.####} and {helixAngle2Deg:0.####} degrees ({Normalise(hand1)} and {Normalise(hand2)} hand) " +
                    $"need a shaft angle of {expected:0.####} degrees.");
            }
        }

        private static string Normalise(string hand) =>
            string.IsNullOrWhiteSpace(hand) || hand.Trim().StartsWith("R", StringComparison.OrdinalIgnoreCase) ? "R" : "L";
    }
}
