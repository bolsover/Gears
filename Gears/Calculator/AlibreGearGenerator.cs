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
            public List<GearPoint> RootFillet1 { get; set; } = new List<GearPoint>();
            public List<GearPoint> FlankLine1 { get; set; } = new List<GearPoint>();
            public List<GearPoint> Involute1 { get; set; } = new List<GearPoint>();
            public List<GearPoint> UpperArc { get; set; } = new List<GearPoint>();
            public List<GearPoint> Involute2 { get; set; } = new List<GearPoint>();
            public List<GearPoint> FlankLine2 { get; set; } = new List<GearPoint>();
            public List<GearPoint> RootFillet2 { get; set; } = new List<GearPoint>();
            public List<GearPoint> Trochoid2 { get; set; } = new List<GearPoint>();
            public List<GearPoint> LowerArc { get; set; } = new List<GearPoint>();

            public GearPoint RootFilletCentre1 { get; set; }
            public GearPoint RootFilletCentre2 { get; set; }

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
        /// For internal-external pairs (Gear 1 internal, Gear 2 external), contact is at angle 0 where Gear 1 has a tooth gap
        /// and Gear 2 has a tooth, requiring 0 rotation.
        /// For external-internal pairs (Gear 1 external, Gear 2 internal), contact is at angle pi (-r1, 0).
        /// When (z1 - z2) is odd, an additional 0.5 tooth pitch rotation (pi / z2) is required to align tooth with tooth gap.
        /// </summary>
        public static double CalculateMeshRotationAngle(bool isInternal1, bool isInternal2, int z1, int z2)
        {
            if (isInternal1 && isInternal2)
            {
                return 0.0;
            }

            if (isInternal1 && !isInternal2)
            {
                // Internal Gear 1 meshed with External Gear 2: contact is at angle 0 (+r1).
                // Gear 1 has a tooth gap at angle 0 and Gear 2 has a tooth at angle 0, so rotation is 0.
                return 0.0;
            }

            if (!isInternal1 && isInternal2)
            {
                // External Gear 1 meshed with Internal Gear 2: contact is at angle pi (-r1).
                // If (z1 - z2) is odd, tooth/gap parity requires a 0.5 tooth offset (pi / z2) to mesh properly.
                return (Math.Abs(z1 - z2) % 2 != 0 && z2 > 0) ? Math.PI / z2 : 0.0;
            }

            // External-External pair:
            return z2 > 0 ? Math.PI + Math.PI / z2 : 0.0;
        }

        /// <summary>
        /// Overload for backwards compatibility.
        /// </summary>
        public static double CalculateMeshRotationAngle(bool isInternal1, bool isInternal2, int z2)
            => CalculateMeshRotationAngle(isInternal1, isInternal2, 0, z2);

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
        /// <param name="helixAngle">Helix angle in degrees. Defaults to 0.0 if null.</param>
        public static AlibreGearProfile GenerateExternalToothProfile(
            int z, double m, double alphaDeg,
            double profileShift = 0.0,
            bool undercutAutoSuppress = false,
            int[] numPoints = null,
            double? clearance = null,
            double? backlash = null,
            double? helixAngle = null,
            RootFilletType rootFillet = RootFilletType.Standard)
        {
            numPoints = numPoints ?? new[] { 10, 10, 5, 5 };
            var c = clearance ?? (0.25 * m);
            var jt = backlash ?? 0.0;
            var betaDeg = helixAngle ?? 0.0;

            var alpha = Radians(alphaDeg);
            var beta = Radians(betaDeg);
            var cosB = Math.Cos(beta);
            if (cosB < 1e-6) cosB = 1e-6;

            var mt = m / cosB;
            var alphaT = Math.Atan(Math.Tan(alpha) / cosB);

            // Standard radii calculations
            var pitchRadiusStd = mt * z / 2.0;
            var baseRadius = pitchRadiusStd * Math.Cos(alphaT);

            // Undercut auto suppress
            if (undercutAutoSuppress)
            {
                if (baseRadius > pitchRadiusStd - m) // Check for undercut
                {
                    profileShift = baseRadius - (pitchRadiusStd - m);
                }
            }

            // Apply profile shift
            var pitchRadius = pitchRadiusStd + profileShift;
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

            // Tooth positioning angle
            var toothAngle = -angularToothWidth - 2 * phi;

            var rootFilletRadius = rootFillet == RootFilletType.Radius ? 0.38 * m : 0.0;
            ExternalRootFilletGeometry rootFilletGeometry = null;
            if (rootFillet == RootFilletType.Radius)
            {
                dedendumRadius = AdjustExternalRootRadius(baseRadius, dedendumRadius, pitchRadius, rootFilletRadius, toothAngle, z);
                rootFilletGeometry = CreateExternalRootFillet(baseRadius, dedendumRadius, rootFilletRadius);
            }

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

            var minInvoluteAngle = rootFilletGeometry != null
                ? rootFilletGeometry.Theta
                : dedendumInvoluteAngle + InvoluteFunction(dedendumInvoluteAngle);

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

            profile.Involute2.Reverse();

            // === Trochoid curves ===
            double betaTrochoid = 0;
            if (rootFilletGeometry == null && baseRadius > dedendumRadius)
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
            var pitchAngle = 2.0 * Math.PI / z;
            double startAngleLower, endAngleLower;
            if (rootFilletGeometry != null)
            {
                var secondRootPoint = TransformSecondFlank(rootFilletGeometry.RootPoint, toothAngle, 0.0);
                startAngleLower = Math.Atan2(secondRootPoint.Y, secondRootPoint.X);
                endAngleLower = Math.Atan2(rootFilletGeometry.RootPoint.Y, rootFilletGeometry.RootPoint.X) - pitchAngle;
            }
            else if (baseRadius > dedendumRadius)
            {
                startAngleLower = toothAngle - betaTrochoid;
                endAngleLower = -pitchAngle + betaTrochoid;
            }
            else
            {
                startAngleLower = toothAngle + InvoluteFunction(dedendumInvoluteAngle);
                endAngleLower = -pitchAngle - InvoluteFunction(dedendumInvoluteAngle);
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

            if (rootFilletGeometry != null)
            {
                profile.RootFillet1 = Rotate(rootFilletGeometry.Points, rotationAngle);
                profile.RootFilletCentre1 = rootFilletGeometry.Centre.Rotate(rotationAngle);
                profile.RootFillet2 = TransformSecondFlank(rootFilletGeometry.Points, toothAngle, rotationAngle);
                profile.RootFillet2.Reverse();
                profile.RootFilletCentre2 = TransformSecondFlank(rootFilletGeometry.Centre, toothAngle, rotationAngle);

                if (rootFilletGeometry.HasFlankLine)
                {
                    var firstFlankLine = new List<GearPoint> { rootFilletGeometry.ContactPoint, new GearPoint(baseRadius, 0.0) };
                    profile.FlankLine1 = Rotate(firstFlankLine, rotationAngle);
                    profile.FlankLine2 = TransformSecondFlank(firstFlankLine, toothAngle, rotationAngle);
                    profile.FlankLine2.Reverse();
                    profile.Involute1[0] = profile.FlankLine1[profile.FlankLine1.Count - 1];
                    profile.Involute2[profile.Involute2.Count - 1] = profile.FlankLine2[0];
                }
                else
                {
                    profile.Involute1[0] = profile.RootFillet1[profile.RootFillet1.Count - 1];
                    profile.Involute2[profile.Involute2.Count - 1] = profile.RootFillet2[0];
                }

                profile.LowerArc[0] = profile.RootFillet2[profile.RootFillet2.Count - 1];
                profile.LowerArc[profile.LowerArc.Count - 1] = profile.RootFillet1[0].Rotate(-pitchAngle);
            }

            PopulateParameters(profile, z, m, alphaDeg, profileShift, pitchRadius, baseRadius, addendumRadius, dedendumRadius, c, jt, betaDeg, rootFilletRadius);
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
        /// <param name="helixAngle">Helix angle in degrees. Defaults to 0.0 if null.</param>
        public static AlibreGearProfile GenerateInternalToothProfile(
            int z, double m, double alphaDeg, double thickness,
            double profileShift = 0.0,
            bool undercutAutoSuppress = false,
            int[] numPoints = null,
            double? clearance = null,
            double? backlash = null,
            double? helixAngle = null,
            RootFilletType rootFillet = RootFilletType.Standard)
        {
            numPoints ??= new[] { 10, 5, 5, 10 };
            var c = clearance ?? (0.25 * m);
            var jt = backlash ?? 0.0;
            var betaDeg = helixAngle ?? 0.0;

            var alpha = Radians(alphaDeg);
            var beta = Radians(betaDeg);
            var cosB = Math.Cos(beta);
            if (cosB < 1e-6) cosB = 1e-6;

            var mt = m / cosB;
            var alphaT = Math.Atan(Math.Tan(alpha) / cosB);

            var pitchRadiusStd = mt * z / 2.0;
            var baseRadius = pitchRadiusStd * Math.Cos(alphaT);

            if (undercutAutoSuppress)
            {
                if (baseRadius > pitchRadiusStd - m)
                {
                    profileShift = baseRadius - (pitchRadiusStd - m);
                }
            }

            // Apply profile shift (note: addendum is inside, dedendum outside for internal gears)
            pitchRadiusStd += profileShift;
            var pitchRadius = pitchRadiusStd;
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

            var toothAngle = -angularToothWidth - 2 * phi;
            var rootFilletRadius = rootFillet == RootFilletType.Radius ? 0.38 * m : 0.0;
            InternalRootFilletGeometry rootFilletGeometry = null;
            if (rootFillet == RootFilletType.Radius)
            {
                dedendumRadius = AdjustInternalRootRadius(baseRadius, dedendumRadius, rootFilletRadius, toothAngle, z);
                rootFilletGeometry = CreateInternalRootFillet(baseRadius, dedendumRadius, rootFilletRadius, numPoints[1]);
            }

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

            var maxInvoluteAngle = rootFilletGeometry?.Theta ?? (dedendumInvoluteAngle + InvoluteFunction(dedendumInvoluteAngle));
            var minInvoluteAngle = addendumInvoluteAngle + InvoluteFunction(addendumInvoluteAngle);

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
            var startAngleUpper = rootFilletGeometry == null
                ? -involuteAtDedendum
                : Math.Atan2(rootFilletGeometry.RootPoint.Y, rootFilletGeometry.RootPoint.X);
            var secondRootPoint = rootFilletGeometry == null
                ? null
                : TransformSecondFlank(rootFilletGeometry.RootPoint, toothAngle, 0.0);
            var endAngleUpper = secondRootPoint == null
                ? toothAngle + involuteAtDedendum
                : Math.Atan2(secondRootPoint.Y, secondRootPoint.X);

            for (var i = 0; i < numPoints[1]; i++)
            {
                var t = (double) i / (numPoints[1] - 1);
                var thetaArc = startAngleUpper + t * (endAngleUpper - startAngleUpper);
                profile.UpperArc.Add(new GearPoint(dedendumRadius * Math.Cos(thetaArc), dedendumRadius * Math.Sin(thetaArc)));
            }

            // Lower arc (dedendum) - doubled on each side to avoid overly sharp corners
            var pitchAngle = 2.0 * Math.PI / z;
            var startAngleLower = toothAngle + InvoluteFunction(addendumInvoluteAngle);
            var endAngleLower = -pitchAngle - InvoluteFunction(addendumInvoluteAngle);
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

            profile.Involute2.Reverse();
            profile.LowerArc2.Reverse();

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

            if (rootFilletGeometry != null)
            {
                profile.RootFillet1 = Rotate(rootFilletGeometry.Points, rotationAngle);
                profile.RootFillet2 = TransformSecondFlank(rootFilletGeometry.Points, toothAngle, rotationAngle);
                profile.RootFillet2.Reverse();
                profile.RootFilletCentre1 = rootFilletGeometry.Centre.Rotate(rotationAngle);
                profile.RootFilletCentre2 = TransformSecondFlank(rootFilletGeometry.Centre, toothAngle, rotationAngle);

                profile.Involute1[profile.Involute1.Count - 1] = profile.RootFillet1[0];
                profile.Involute2[0] = profile.RootFillet2[profile.RootFillet2.Count - 1];
                profile.UpperArc[0] = profile.RootFillet1[profile.RootFillet1.Count - 1];
                profile.UpperArc[profile.UpperArc.Count - 1] = profile.RootFillet2[0];
            }

            PopulateParameters(profile, z, m, alphaDeg, profileShift, pitchRadius, baseRadius, addendumRadius, dedendumRadius, c, jt, betaDeg, rootFilletRadius);
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
                AddIfAny(segments, p.LowerArc2);
                AddIfAny(segments, p.Involute1);
                AddIfAny(segments, p.RootFillet1);
                AddIfAny(segments, p.UpperArc);
                AddIfAny(segments, p.RootFillet2);
                AddIfAny(segments, p.Involute2);
                AddIfAny(segments, p.LowerArc1);
                AddIfAny(segments, p.ExternalArc);
            }
            else
            {
                AddIfAny(segments, p.Trochoid1);
                AddIfAny(segments, p.RootFillet1);
                AddIfAny(segments, p.FlankLine1);
                AddIfAny(segments, p.Involute1);
                AddIfAny(segments, p.UpperArc);
                AddIfAny(segments, p.Involute2);
                AddIfAny(segments, p.FlankLine2);
                AddIfAny(segments, p.RootFillet2);
                AddIfAny(segments, p.Trochoid2);
                AddIfAny(segments, p.LowerArc);
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
                var angle = -k * pitch;
                foreach (var segment in toothOutline)
                {
                    result.Add(GearPoint.Rotated(segment, angle));
                }
            }

            return result;
        }

        private sealed class ExternalRootFilletGeometry
        {
            public double Theta { get; set; }
            public GearPoint ContactPoint { get; set; }
            public GearPoint Centre { get; set; }
            public GearPoint RootPoint { get; set; }
            public List<GearPoint> Points { get; set; }
            public bool HasFlankLine { get; set; }
        }

        private sealed class InternalRootFilletGeometry
        {
            public double Theta { get; set; }
            public GearPoint Centre { get; set; }
            public GearPoint RootPoint { get; set; }
            public List<GearPoint> Points { get; set; }
        }

        private static double AdjustExternalRootRadius(double baseRadius, double rootRadius, double maximumRadius,
            double filletRadius, double toothAngle, int teeth)
        {
            if (filletRadius <= 0.0) throw new InvalidOperationException("The root fillet radius must be positive.");
            var maximumAngle = Math.PI / teeth;
            if (ExternalRootAngle(baseRadius, rootRadius, filletRadius, toothAngle) <= maximumAngle) return rootRadius;
            if (ExternalRootAngle(baseRadius, maximumRadius, filletRadius, toothAngle) > maximumAngle)
            {
                throw new InvalidOperationException("The external root fillets overlap and the root radius cannot be adjusted to fit them.");
            }

            var low = rootRadius;
            var high = maximumRadius;
            for (var i = 0; i < 80; i++)
            {
                var middle = (low + high) / 2.0;
                if (ExternalRootAngle(baseRadius, middle, filletRadius, toothAngle) <= maximumAngle) high = middle;
                else low = middle;
            }

            return high;
        }

        private static double ExternalRootAngle(double baseRadius, double rootRadius, double filletRadius, double toothAngle)
        {
            var point = CreateExternalRootFillet(baseRadius, rootRadius, filletRadius).RootPoint;
            return Math.Atan2(point.Y, point.X) - toothAngle / 2.0;
        }

        private static ExternalRootFilletGeometry CreateExternalRootFillet(double baseRadius, double rootRadius, double filletRadius)
        {
            var discriminant = (rootRadius + filletRadius) * (rootRadius + filletRadius) - baseRadius * baseRadius;
            var theta = discriminant >= filletRadius * filletRadius
                ? (Math.Sqrt(discriminant) - filletRadius) / baseRadius
                : 0.0;
            GearPoint contactPoint;
            GearPoint centre;
            var hasFlankLine = theta <= 1e-12;
            if (hasFlankLine)
            {
                var x = Math.Sqrt(rootRadius * rootRadius + 2.0 * rootRadius * filletRadius);
                contactPoint = new GearPoint(x, 0.0);
                centre = new GearPoint(x, filletRadius);
                hasFlankLine = x < baseRadius - 1e-12;
            }
            else
            {
                var sinTheta = Math.Sin(theta);
                var cosTheta = Math.Cos(theta);
                contactPoint = new GearPoint(baseRadius * (cosTheta + theta * sinTheta),
                    -baseRadius * (sinTheta - theta * cosTheta));
                centre = new GearPoint(contactPoint.X + filletRadius * sinTheta,
                    contactPoint.Y + filletRadius * cosTheta);
            }

            var rootPoint = new GearPoint(centre.X * rootRadius / (rootRadius + filletRadius),
                centre.Y * rootRadius / (rootRadius + filletRadius));
            var points = SampleCircleArc(centre, filletRadius, rootPoint, contactPoint, 15, true);
            return new ExternalRootFilletGeometry
            {
                Theta = theta,
                ContactPoint = contactPoint,
                Centre = centre,
                RootPoint = rootPoint,
                Points = points,
                HasFlankLine = hasFlankLine
            };
        }

        private static double AdjustInternalRootRadius(double baseRadius, double rootRadius, double filletRadius,
            double toothAngle, int teeth)
        {
            if (filletRadius <= 0.0) throw new InvalidOperationException("The root fillet radius must be positive.");
            if (InternalRootAngle(baseRadius, rootRadius, filletRadius, toothAngle) > 1e-12) return rootRadius;

            var low = baseRadius + filletRadius + Math.Max(1e-9, baseRadius * 1e-12);
            if (low >= rootRadius || InternalRootAngle(baseRadius, low, filletRadius, toothAngle) <= 1e-12)
            {
                throw new InvalidOperationException("The internal root fillets overlap and the root radius cannot be adjusted to fit them.");
            }

            var high = rootRadius;
            for (var i = 0; i < 80; i++)
            {
                var middle = (low + high) / 2.0;
                if (InternalRootAngle(baseRadius, middle, filletRadius, toothAngle) > 1e-12) low = middle;
                else high = middle;
            }

            return low;
        }

        private static double InternalRootAngle(double baseRadius, double rootRadius, double filletRadius, double toothAngle)
        {
            var point = CreateInternalRootFillet(baseRadius, rootRadius, filletRadius, 2).RootPoint;
            return Math.Atan2(point.Y, point.X) - toothAngle / 2.0;
        }

        private static InternalRootFilletGeometry CreateInternalRootFillet(double baseRadius, double rootRadius,
            double filletRadius, int pointCount)
        {
            var centreRadius = rootRadius - filletRadius;
            var discriminant = centreRadius * centreRadius - baseRadius * baseRadius;
            if (discriminant < -1e-10)
            {
                throw new InvalidOperationException("The internal root radius is too small for a tangent root fillet.");
            }

            var theta = (filletRadius + Math.Sqrt(Math.Max(0.0, discriminant))) / baseRadius;
            var sinTheta = Math.Sin(theta);
            var cosTheta = Math.Cos(theta);
            var contactPoint = new GearPoint(baseRadius * (cosTheta + theta * sinTheta),
                -baseRadius * (sinTheta - theta * cosTheta));
            var centre = new GearPoint(contactPoint.X - filletRadius * sinTheta,
                contactPoint.Y - filletRadius * cosTheta);
            var rootPoint = new GearPoint(centre.X * rootRadius / centreRadius, centre.Y * rootRadius / centreRadius);
            var points = SampleCircleArc(centre, filletRadius, contactPoint, rootPoint, pointCount, false);
            return new InternalRootFilletGeometry { Theta = theta, Centre = centre, RootPoint = rootPoint, Points = points };
        }

        private static List<GearPoint> SampleCircleArc(GearPoint centre, double radius, GearPoint start, GearPoint end,
            int pointCount, bool counterClockwise)
        {
            var count = Math.Max(2, pointCount);
            var startAngle = Math.Atan2(start.Y - centre.Y, start.X - centre.X);
            var endAngle = Math.Atan2(end.Y - centre.Y, end.X - centre.X);
            var sweep = endAngle - startAngle;
            if (counterClockwise)
            {
                while (sweep < 0.0) sweep += 2.0 * Math.PI;
            }
            else
            {
                while (sweep > 0.0) sweep -= 2.0 * Math.PI;
            }

            var points = new List<GearPoint>(count);
            for (var i = 0; i < count; i++)
            {
                if (i == 0) points.Add(start);
                else if (i == count - 1) points.Add(end);
                else
                {
                    var angle = startAngle + sweep * i / (count - 1);
                    points.Add(new GearPoint(centre.X + radius * Math.Cos(angle), centre.Y + radius * Math.Sin(angle)));
                }
            }

            return points;
        }

        private static List<GearPoint> Rotate(List<GearPoint> points, double angle)
        {
            return points.ConvertAll(point => point.Rotate(angle));
        }

        private static List<GearPoint> TransformSecondFlank(List<GearPoint> points, double toothAngle, double rotationAngle)
        {
            return points.ConvertAll(point => TransformSecondFlank(point, toothAngle, rotationAngle));
        }

        private static GearPoint TransformSecondFlank(GearPoint point, double toothAngle, double rotationAngle)
        {
            return new GearPoint(point.X, -point.Y).Rotate(toothAngle).Rotate(rotationAngle);
        }

        private static void AddIfAny(List<List<GearPoint>> segments, List<GearPoint> points)
        {
            if (points is { Count: > 0 }) segments.Add(points);
        }

        private static double Radians(double degrees) => degrees * Math.PI / 180.0;

        private static void PopulateParameters(AlibreGearProfile profile, int z, double m, double alphaDeg,
            double profileShift, double pitchRadius, double baseRadius, double addendumRadius, double dedendumRadius,
            double clearance = 0.0, double backlash = 0.0, double helixAngle = 0.0, double rootFilletRadius = 0.0)
        {
            profile.Parameters["z"] = z;
            profile.Parameters["m"] = m;
            profile.Parameters["alphaDeg"] = alphaDeg;
            profile.Parameters["helixAngle"] = helixAngle;
            profile.Parameters["profileShift"] = profileShift;
            profile.Parameters["clearance"] = clearance;
            profile.Parameters["backlash"] = backlash;
            profile.Parameters["pitchRadius"] = pitchRadius;
            profile.Parameters["baseRadius"] = baseRadius;
            profile.Parameters["addendumRadius"] = addendumRadius;
            profile.Parameters["dedendumRadius"] = dedendumRadius;
            profile.Parameters["rootFilletRadius"] = rootFilletRadius;
        }
    }
}
