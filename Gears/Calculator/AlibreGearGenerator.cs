using System;
using System.Collections.Generic;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Calculator
{
    /// <summary>
    /// C# port of the involute gear profile logic from <c>alibre_gear_generator.py</c>.
    /// Generates external and internal spur gear tooth profiles as lists of <see cref="GearPoint"/>.
    /// The Alibre-specific sketching has been replaced by profile generation only; the calling
    /// GUI is responsible for previewing / exporting the resulting geometry.
    /// </summary>
    public static class AlibreGearGenerator
    {
        /// <summary>
        /// Result of a tooth profile generation. Segment lists are ordered from the point at which
        /// they are drawn. Which lists are populated depends on the gear type (external vs internal).
        /// </summary>
        public class AlibreGearProfile
        {
            // External profile segments
            public List<GearPoint> Trochoid1 { get; set; } = new List<GearPoint>();
            public List<GearPoint> Involute1 { get; set; } = new List<GearPoint>();
            public List<GearPoint> UpperArc { get; set; } = new List<GearPoint>();
            public List<GearPoint> Involute2 { get; set; } = new List<GearPoint>();
            public List<GearPoint> Trochoid2 { get; set; } = new List<GearPoint>();
            public List<GearPoint> LowerArc { get; set; } = new List<GearPoint>();

            // Additional internal profile segments
            public List<GearPoint> LowerArc1 { get; set; } = new List<GearPoint>();
            public List<GearPoint> LowerArc2 { get; set; } = new List<GearPoint>();
            public List<GearPoint> ExternalArc { get; set; } = new List<GearPoint>();

            /// <summary>
            /// Derived gear parameters (z, m, alphaDeg, profileShift, pitchRadius, baseRadius,
            /// addendumRadius, dedendumRadius).
            /// </summary>
            public Dictionary<string, double> Parameters { get; } = new Dictionary<string, double>();
        }

        /// <summary>
        /// Calculates the rotation angle in radians required to align Gear 2 with Gear 1 for meshing.
        /// For external-external pairs, Gear 2 is rotated by (pi + pi / z2) to align a tooth gap with Gear 1's tooth at angle 0.
        /// For internal-external pairs (e.g. adding a meshed external gear to an internal gear), Gear 1 has a tooth gap at angle 0
        /// and Gear 2 has a tooth at angle 0, so the required rotation angle is 0.
        /// </summary>
        public static double CalculateMeshRotationAngle(bool isInternal1, bool isInternal2, int z2)
        {
            if (isInternal1 || isInternal2)
            {
                return 0.0;
            }

            return z2 > 0 ? Math.PI + Math.PI / z2 : 0.0;
        }

        /// <summary>
        /// Involute function: tan(angle) - angle.
        /// </summary>
        public static double InvoluteFunction(double angle) => Math.Tan(angle) - angle;

        /// <summary>
        /// Generate one external tooth profile consisting of two trochoids, two involutes and
        /// upper (addendum) / lower (dedendum) arcs. Faithful port of
        /// <c>generate_external_tooth_profile</c>.
        /// </summary>
        /// <param name="z">Number of teeth.</param>
        /// <param name="m">Module (mm).</param>
        /// <param name="alphaDeg">Pressure angle in degrees.</param>
        /// <param name="profileShift">Profile shift value.</param>
        /// <param name="undercutAutoSuppress">Automatic undercut suppression.</param>
        /// <param name="numPoints">Points per segment: [involute, trochoid, addendum, dedendum]. Defaults to [10,10,5,5].</param>
        /// <param name="clearance">Bottom clearance (mm). Defaults to 0.25 * m if null.</param>
        /// <param name="backlash">Circular backlash / flank clearance (mm). Defaults to 0.0 if null.</param>
        public static AlibreGearProfile GenerateExternalToothProfile(
            int z, double m, double alphaDeg,
            double profileShift = 0.0,
            bool undercutAutoSuppress = false,
            int[] numPoints = null,
            double? clearance = null,
            double? backlash = null)
        {
            numPoints = numPoints ?? new[] { 10, 10, 5, 5 };
            var c = clearance ?? (0.25 * m);
            var jt = backlash ?? 0.0;

            var alpha = Radians(alphaDeg);

            // Standard radii calculations
            var pitchRadius = m * z / 2.0;
            var baseRadius = pitchRadius * Math.Cos(alpha);

            // Undercut auto suppress
            if (undercutAutoSuppress)
            {
                if (baseRadius > pitchRadius - m) // Check for undercut
                {
                    profileShift = baseRadius - (pitchRadius - m);
                }
            }

            // Apply profile shift
            pitchRadius += profileShift;
            var addendumRadius = pitchRadius + m;
            var dedendumRadius = Math.Max(pitchRadius - m - c, 0.01);

            if (baseRadius > pitchRadius)
            {
                throw new InvalidOperationException(
                    "Base radius is larger than pitch radius, resulting in invalid gear geometry.");
            }

            var offsetAngle = Math.Acos(baseRadius / pitchRadius);

            // Involute angle at pitch circle
            var phi = InvoluteFunction(offsetAngle);

            // Angular dimensions with circular backlash / flank clearance reduction
            var angularToothWidth = Math.Max(1e-6, (Math.PI * m / 2.0 - jt) / (m * z / 2.0));

            // Involute parameters
            var addendumInvoluteAngle = Math.Acos(baseRadius / addendumRadius);
            var maxInvoluteAngle = addendumInvoluteAngle + InvoluteFunction(addendumInvoluteAngle);

            // Dedendum involute angle only if base radius is smaller than dedendum radius
            double dedendumInvoluteAngle;
            if (baseRadius < dedendumRadius)
            {
                dedendumInvoluteAngle = Math.Acos(baseRadius / dedendumRadius);
            }
            else
            {
                dedendumInvoluteAngle = 0;
            }

            var minInvoluteAngle = dedendumInvoluteAngle + InvoluteFunction(dedendumInvoluteAngle);

            // Tooth positioning angle
            var toothAngle = -angularToothWidth - 2 * phi;

            var profile = new AlibreGearProfile();

            // === Involute curves ===
            for (var i = 0; i < numPoints[0]; i++)
            {
                var t = (double) i / (numPoints[0] - 1);
                var theta = t * (maxInvoluteAngle - minInvoluteAngle) + minInvoluteAngle;

                var xInv = baseRadius * (Math.Cos(theta) + theta * Math.Sin(theta));
                var yInv = baseRadius * (Math.Sin(theta) - theta * Math.Cos(theta));

                // First involute (rotated by tooth_angle) -> involute_2
                var cosTooth = Math.Cos(toothAngle);
                var sinTooth = Math.Sin(toothAngle);
                var x1 = xInv * cosTooth - yInv * sinTooth;
                var y1 = xInv * sinTooth + yInv * cosTooth;
                profile.Involute2.Add(new GearPoint(x1, y1));

                // Second involute (mirrored in y, no rotation) -> involute_1
                profile.Involute1.Add(new GearPoint(xInv, -yInv));
            }

            // === Trochoid curves ===
            double betaTrochoid = 0;
            if (baseRadius > dedendumRadius)
            {
                var tTrochoid = baseRadius - dedendumRadius;
                var bTrochoid = Math.Sqrt(
                    Math.Pow(baseRadius, 4) / Math.Pow(baseRadius - tTrochoid, 2) - baseRadius * baseRadius);
                var hTrochoid = bTrochoid * (1 - tTrochoid / baseRadius);

                var alphaTrochoid = Math.Atan(hTrochoid / baseRadius);
                var offsetTrochoidAngle = alphaTrochoid + InvoluteFunction(alphaTrochoid);
                betaTrochoid = Math.Atan(bTrochoid / baseRadius) - offsetTrochoidAngle;

                for (var i = 0; i < numPoints[1]; i++)
                {
                    var t = (double) i / (numPoints[1] - 1);
                    var thetaTro = t * offsetTrochoidAngle;

                    var xTro = baseRadius * (Math.Cos(thetaTro) + thetaTro * Math.Sin(thetaTro)) -
                               tTrochoid * Math.Cos(thetaTro);
                    var yTro = baseRadius * (Math.Sin(thetaTro) - thetaTro * Math.Cos(thetaTro)) -
                               tTrochoid * Math.Sin(thetaTro);

                    // Apply trochoid beta rotation
                    var cosBeta = Math.Cos(betaTrochoid);
                    var sinBeta = Math.Sin(betaTrochoid);
                    var xTroRot = xTro * cosBeta - yTro * sinBeta;
                    var yTroRot = xTro * sinBeta + yTro * cosBeta;

                    // First trochoid (no additional rotation)
                    profile.Trochoid1.Add(new GearPoint(xTroRot, yTroRot));

                    // Second trochoid (mirrored in y, then rotated by tooth_angle)
                    var cosTooth = Math.Cos(toothAngle);
                    var sinTooth = Math.Sin(toothAngle);
                    var x2 = xTroRot * cosTooth - -yTroRot * sinTooth;
                    var y2 = xTroRot * sinTooth + -yTroRot * cosTooth;
                    profile.Trochoid2.Add(new GearPoint(x2, y2));
                }

                profile.Trochoid1.Reverse();
                profile.Trochoid2.Reverse();
            }

            // === Arc segments ===

            // Upper arc (addendum)
            var addendumInvoluteAngleVal = Math.Acos(baseRadius / addendumRadius);
            var involuteAtAddendum = InvoluteFunction(addendumInvoluteAngleVal);
            var startAngleUpper = -involuteAtAddendum;
            var endAngleUpper = toothAngle + involuteAtAddendum;

            for (var i = 0; i < numPoints[2]; i++)
            {
                var t = (double) i / (numPoints[2] - 1);
                var thetaArc = startAngleUpper + t * (endAngleUpper - startAngleUpper);
                profile.UpperArc.Add(new GearPoint(addendumRadius * Math.Cos(thetaArc), addendumRadius * Math.Sin(thetaArc)));
            }

            // Lower arc (dedendum)
            double startAngleLower, endAngleLower;
            if (baseRadius > dedendumRadius)
            {
                startAngleLower = toothAngle - betaTrochoid;
                endAngleLower = -angularToothWidth * 2 + betaTrochoid;
            }
            else
            {
                startAngleLower = toothAngle + InvoluteFunction(dedendumInvoluteAngle);
                endAngleLower = -angularToothWidth * 2 - InvoluteFunction(dedendumInvoluteAngle);
            }

            for (var i = 0; i < numPoints[3]; i++)
            {
                var t = (double) i / (numPoints[3] - 1);
                var thetaArc = startAngleLower + t * (endAngleLower - startAngleLower);
                profile.LowerArc.Add(new GearPoint(dedendumRadius * Math.Cos(thetaArc), dedendumRadius * Math.Sin(thetaArc)));
            }

            // Rotate all profiles by half tooth angle to centre tooth on x-axis
            var rotationAngle = -toothAngle / 2;
            profile.Trochoid1 = GearPoint.Rotated(profile.Trochoid1, rotationAngle);
            profile.Involute1 = GearPoint.Rotated(profile.Involute1, rotationAngle);
            profile.UpperArc = GearPoint.Rotated(profile.UpperArc, rotationAngle);
            profile.Involute2 = GearPoint.Rotated(profile.Involute2, rotationAngle);
            profile.Trochoid2 = GearPoint.Rotated(profile.Trochoid2, rotationAngle);
            profile.LowerArc = GearPoint.Rotated(profile.LowerArc, rotationAngle);

            PopulateParameters(profile, z, m, alphaDeg, profileShift, pitchRadius, baseRadius, addendumRadius, dedendumRadius, c, jt);
            return profile;
        }

        /// <summary>
        /// Generate one internal tooth profile. Faithful port of
        /// <c>generate_internal_tooth_profile</c>.
        /// </summary>
        /// <param name="z">Number of teeth.</param>
        /// <param name="m">Module (mm).</param>
        /// <param name="alphaDeg">Pressure angle in degrees.</param>
        /// <param name="thickness">External thickness (mm).</param>
        /// <param name="profileShift">Profile shift value.</param>
        /// <param name="undercutAutoSuppress">Automatic undercut suppression.</param>
        /// <param name="numPoints">Points per segment: [involute, addendum, dedendum, external]. Defaults to [10,5,5,10].</param>
        /// <param name="clearance">Bottom clearance (mm). Defaults to 0.25 * m if null.</param>
        /// <param name="backlash">Circular backlash / flank clearance (mm). Defaults to 0.0 if null.</param>
        public static AlibreGearProfile GenerateInternalToothProfile(
            int z, double m, double alphaDeg, double thickness,
            double profileShift = 0.0,
            bool undercutAutoSuppress = false,
            int[] numPoints = null,
            double? clearance = null,
            double? backlash = null)
        {
            numPoints ??= new[] { 10, 5, 5, 10 };
            var c = clearance ?? (0.25 * m);
            var jt = backlash ?? 0.0;

            var alpha = Radians(alphaDeg);

            var pitchRadius = m * z / 2.0;
            var baseRadius = pitchRadius * Math.Cos(alpha);

            if (undercutAutoSuppress)
            {
                if (baseRadius > pitchRadius - m)
                {
                    profileShift = baseRadius - (pitchRadius - m);
                }
            }

            // Apply profile shift (note: addendum is inside, dedendum outside for internal gears)
            pitchRadius += profileShift;
            var addendumRadius = pitchRadius - m;
            var dedendumRadius = Math.Max(pitchRadius + m + c, 0.01);

            if (baseRadius > pitchRadius)
            {
                throw new InvalidOperationException(
                    "Base radius is larger than pitch radius, resulting in invalid gear geometry.");
            }

            var offsetAngle = Math.Acos(baseRadius / pitchRadius);
            var phi = InvoluteFunction(offsetAngle);
            var angularToothWidth = Math.Max(1e-6, (Math.PI * m / 2.0 - jt) / (m * z / 2.0));

            var dedendumInvoluteAngle = Math.Acos(baseRadius / dedendumRadius);

            double addendumInvoluteAngle;
            if (baseRadius < addendumRadius)
            {
                addendumInvoluteAngle = Math.Acos(baseRadius / addendumRadius);
            }
            else
            {
                addendumInvoluteAngle = 0;
            }

            var maxInvoluteAngle = dedendumInvoluteAngle + InvoluteFunction(dedendumInvoluteAngle);
            var minInvoluteAngle = addendumInvoluteAngle + InvoluteFunction(addendumInvoluteAngle);

            var toothAngle = -angularToothWidth - 2 * phi;

            var profile = new AlibreGearProfile();

            // === Involute curves ===
            for (var i = 0; i < numPoints[0]; i++)
            {
                var t = (double) i / (numPoints[0] - 1);
                var theta = t * (maxInvoluteAngle - minInvoluteAngle) + minInvoluteAngle;

                var xInv = baseRadius * (Math.Cos(theta) + theta * Math.Sin(theta));
                var yInv = baseRadius * (Math.Sin(theta) - theta * Math.Cos(theta));

                var cosTooth = Math.Cos(toothAngle);
                var sinTooth = Math.Sin(toothAngle);
                var x1 = xInv * cosTooth - yInv * sinTooth;
                var y1 = xInv * sinTooth + yInv * cosTooth;
                profile.Involute2.Add(new GearPoint(x1, y1));

                profile.Involute1.Add(new GearPoint(xInv, -yInv));
            }

            // === Arc segments ===

            // Upper arc (addendum)
            var involuteAtDedendum = InvoluteFunction(dedendumInvoluteAngle);
            var startAngleUpper = -involuteAtDedendum;
            var endAngleUpper = toothAngle + involuteAtDedendum;

            for (var i = 0; i < numPoints[1]; i++)
            {
                var t = (double) i / (numPoints[1] - 1);
                var thetaArc = startAngleUpper + t * (endAngleUpper - startAngleUpper);
                profile.UpperArc.Add(new GearPoint(dedendumRadius * Math.Cos(thetaArc), dedendumRadius * Math.Sin(thetaArc)));
            }

            // Lower arc (dedendum) - doubled on each side to avoid overly sharp corners
            var startAngleLower = toothAngle + InvoluteFunction(addendumInvoluteAngle);
            var endAngleLower = -angularToothWidth * 2 - InvoluteFunction(addendumInvoluteAngle);
            var angularWidthLower = endAngleLower - startAngleLower;

            var lowerRadius = Math.Max(addendumRadius, baseRadius);
            var halfNumPointsLower = numPoints[2] / 2;

            for (var i = 0; i < halfNumPointsLower; i++)
            {
                var t = (double) i / (halfNumPointsLower - 1);
                var thetaArc = startAngleLower + t * angularWidthLower / 2;
                profile.LowerArc1.Add(new GearPoint(lowerRadius * Math.Cos(thetaArc), lowerRadius * Math.Sin(thetaArc)));
            }

            for (var i = 0; i < halfNumPointsLower; i++)
            {
                var t = (double) i / (halfNumPointsLower - 1);
                var thetaArc = -InvoluteFunction(addendumInvoluteAngle) - t * angularWidthLower / 2;
                profile.LowerArc2.Add(new GearPoint(lowerRadius * Math.Cos(thetaArc), lowerRadius * Math.Sin(thetaArc)));
            }

            // External arc
            var startAngleExternal = -InvoluteFunction(addendumInvoluteAngle) - angularWidthLower / 2;
            var endAngleExternal = startAngleLower + angularWidthLower / 2;

            for (var i = 0; i < numPoints[3]; i++)
            {
                var t = (double) i / (numPoints[3] - 1);
                var thetaArc = startAngleExternal + t * (endAngleExternal - startAngleExternal);
                profile.ExternalArc.Add(new GearPoint((dedendumRadius + thickness) * Math.Cos(thetaArc), (dedendumRadius + thickness) * Math.Sin(thetaArc)));
            }

            // Rotate all profiles by half tooth angle to centre tooth on x-axis
            var rotationAngle = -toothAngle / 2;
            profile.Involute1 = GearPoint.Rotated(profile.Involute1, rotationAngle);
            profile.UpperArc = GearPoint.Rotated(profile.UpperArc, rotationAngle);
            profile.Involute2 = GearPoint.Rotated(profile.Involute2, rotationAngle);
            profile.LowerArc1 = GearPoint.Rotated(profile.LowerArc1, rotationAngle);
            profile.LowerArc2 = GearPoint.Rotated(profile.LowerArc2, rotationAngle);
            profile.ExternalArc = GearPoint.Rotated(profile.ExternalArc, rotationAngle);

            PopulateParameters(profile, z, m, alphaDeg, profileShift, pitchRadius, baseRadius, addendumRadius, dedendumRadius, c, jt);
            return profile;
        }

        /// <summary>
        /// Assemble the ordered outline segments of a single tooth, matching the draw order of the
        /// Python <c>create_external_gear_in_alibre</c> / <c>create_internal_gear_in_alibre</c>
        /// routines, including the connecting lines.
        /// </summary>
        public static List<List<GearPoint>> BuildToothOutline(AlibreGearProfile p, bool internalGear)
        {
            var segments = new List<List<GearPoint>>();
            if (p == null) return segments;

            if (internalGear)
            {
                // 1. Line from lower_arc_1 start to external arc start
                if (p.LowerArc1.Count > 0 && p.ExternalArc.Count > 0)
                {
             //       segments.Add(new List<GearPoint> { Last(p.LowerArc1), Last(p.ExternalArc) });
                }

                // 2. External arc
                AddIfAny(segments, p.ExternalArc);

                // 3. Line from external arc end to lower arc 2 end
                if (p.ExternalArc.Count > 0 && p.LowerArc2.Count > 0)
                {
              //      segments.Add(new List<GearPoint> { First(p.ExternalArc), Last(p.LowerArc2) });
                }

                // 4. Lower dedendum arcs
                AddIfAny(segments, p.LowerArc1);
                AddIfAny(segments, p.LowerArc2);

                // 5. Involute_2, 6. Upper arc, 7. Involute_1
                AddIfAny(segments, p.Involute2);
                AddIfAny(segments, p.UpperArc);
                AddIfAny(segments, p.Involute1);
            }
            else
            {
                // 1. Line from centre (0,0) to start of trochoid_1 (or involute_1 start if no trochoid)
                var trochoidStart = p.Trochoid1.Count == 0 ? First(p.Involute1) : Last(p.Trochoid1);
                if (trochoidStart != null)
                {
               //     segments.Add(new List<GearPoint> { new GearPoint(0, 0), trochoidStart });
                }

                // 2-7. Splines and arcs
                AddIfAny(segments, p.Trochoid1);
                AddIfAny(segments, p.Involute1);
                AddIfAny(segments, p.UpperArc);
                AddIfAny(segments, p.Involute2);
                AddIfAny(segments, p.Trochoid2);
                AddIfAny(segments, p.LowerArc);

                // 8. Line from end of lower arc back to centre
                if (p.LowerArc.Count > 0)
                {
               //     segments.Add(new List<GearPoint> { Last(p.LowerArc), new GearPoint(0, 0) });
                }
            }

            return segments;
        }

        /// <summary>
        /// Replicate a single tooth outline around the full gear by rotating each segment through
        /// multiples of the angular pitch (2*pi / z).
        /// </summary>
        public static List<List<GearPoint>> BuildFullGear(int z, List<List<GearPoint>> toothOutline)
        {
            var result = new List<List<GearPoint>>();
            if (toothOutline == null || z <= 0) return result;

            var pitch = 2 * Math.PI / z;
            for (var k = 0; k < z; k++)
            {
                var angle = k * pitch;
                foreach (var segment in toothOutline)
                {
                    result.Add(GearPoint.Rotated(segment, angle));
                }
            }

            return result;
        }

        private static void AddIfAny(List<List<GearPoint>> segments, List<GearPoint> points)
        {
            if (points is { Count: > 0 }) segments.Add(points);
        }

        private static GearPoint First(List<GearPoint> points) => points is { Count: > 0 } ? points[0] : null;
        private static GearPoint Last(List<GearPoint> points) => points is { Count: > 0 } ? points[points.Count - 1] : null;

        private static double Radians(double degrees) => degrees * Math.PI / 180.0;

        private static void PopulateParameters(AlibreGearProfile profile, int z, double m, double alphaDeg,
            double profileShift, double pitchRadius, double baseRadius, double addendumRadius, double dedendumRadius, double clearance = 0.0, double backlash = 0.0)
        {
            profile.Parameters["z"] = z;
            profile.Parameters["m"] = m;
            profile.Parameters["alphaDeg"] = alphaDeg;
            profile.Parameters["profileShift"] = profileShift;
            profile.Parameters["clearance"] = clearance;
            profile.Parameters["backlash"] = backlash;
            profile.Parameters["pitchRadius"] = pitchRadius;
            profile.Parameters["baseRadius"] = baseRadius;
            profile.Parameters["addendumRadius"] = addendumRadius;
            profile.Parameters["dedendumRadius"] = dedendumRadius;
        }
    }
}
