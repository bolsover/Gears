using System;
using System.Collections.Generic;
using System.Globalization;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>
    /// Builds the <see cref="SingleToothSketch"/> of an involute gear (external or internal, spur or helical) from the tooth profile
    /// made by <see cref="AlibreGearGenerator"/>. Gears are calculated in millimetres to 7 decimal places; Alibre sketches are in
    /// centimetres, so every length is rounded to 7 decimal places in millimetres and then divided by 10.
    /// The sketch is always the transverse (end-face) section of one tooth. A helical gear is made by sweeping that sketch along a
    /// helix, described by <see cref="SingleToothSketch.Helix"/>.
    /// </summary>
    public static class SingleToothSketchBuilder
    {
        /// <summary>Alibre part sketches and features are in centimetres: 1 mm is 0.1 sketch units.</summary>
        public const double MmToAlibreUnits = 0.1;

        /// <summary>Smallest tooth count that gives a sensible circular pattern.</summary>
        public const int MinimumTeeth = 3;

        /// <summary>Largest helix angle accepted, in degrees. Beyond this the lead of the helix becomes unreasonably long.</summary>
        public const double MaximumHelixAngleDeg = 60.0;

        /// <summary>The rim of an internal gear is never thinner than this (mm), as in the 3D preview.</summary>
        public const double MinimumRimThickness = 1.0;

        private const int Decimals = 7;

        /// <summary>Points per curve for an external gear: involute, trochoid, addendum arc, dedendum arc.</summary>
        private static readonly int[] ExternalPointsPerCurve = { 30, 15, 5, 5 };

        /// <summary>Points per curve for an internal gear: involute, upper arc, lower arcs (halved, so at least 4), rim arc.</summary>
        private static readonly int[] InternalPointsPerCurve = { 30, 5, 6, 5 };

        /// <summary>Largest gap, in sketch units, tolerated between neighbouring curves before the profile is rejected.</summary>
        private const double JoinTolerance = 1e-4;

        private sealed class Segment
        {
            public Segment(string name, ToothSketchFigureKind kind, List<GearPoint> points)
            {
                Name = name;
                Kind = kind;
                Points = points;
            }

            public string Name { get; }
            public ToothSketchFigureKind Kind { get; }
            public List<GearPoint> Points { get; }
        }

        /// <summary>Throws <see cref="InvalidOperationException"/> when the gear is not a simple involute spur or helical gear.</summary>
        public static void EnsureSupported(InvoluteGearComponent gear)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));

            if (gear.Teeth < MinimumTeeth)
            {
                throw new InvalidOperationException($"Gear '{gear.Id}' needs at least {MinimumTeeth} teeth to be exported to Alibre.");
            }

            if (gear.HelixAngleDeg < 0.0 || gear.HelixAngleDeg > MaximumHelixAngleDeg)
            {
                throw new InvalidOperationException(
                    $"Gear '{gear.Id}' has a helix angle of {gear.HelixAngleDeg:G4} degrees. Use 0 for a spur gear, or an angle up to {MaximumHelixAngleDeg:0} degrees for a helical gear; set the hand with R or L.");
            }

            gear.Validate();
        }

        public static SingleToothSketch Build(InvoluteGearComponent gear)
        {
            return Build(gear, null);
        }

        /// <summary>Builds the sketch plan and names the Alibre part <paramref name="partName"/> (null for the default gear name).</summary>
        public static SingleToothSketch Build(InvoluteGearComponent gear, string partName)
        {
            EnsureSupported(gear);

            var isInternal = gear.IsInternal;
            var helixAngle = gear.IsHelical ? gear.HelixAngleDeg : 0.0;

            var profile = isInternal
                ? AlibreGearGenerator.GenerateInternalToothProfile(
                    gear.Teeth, gear.Module, gear.PressureAngleDeg, gear.RimThickness, gear.ProfileShift, gear.OptimalShift,
                    InternalPointsPerCurve, gear.Clearance, gear.Backlash, helixAngle)
                : AlibreGearGenerator.GenerateExternalToothProfile(
                    gear.Teeth, gear.Module, gear.PressureAngleDeg, gear.ProfileShift, gear.OptimalShift,
                    ExternalPointsPerCurve, gear.Clearance, gear.Backlash, helixAngle);

            var segments = isInternal ? InternalSegments(profile) : ExternalSegments(profile);

            var chain = new List<ToothSketchFigure>();
            GearPoint previousEnd = null;
            GearPoint chainStart = null;
            foreach (var segment in segments)
            {
                var points = segment.Points;
                if (points.Count < 2) continue;
                if (segment.Kind == ToothSketchFigureKind.Arc && Distance(points[0], points[points.Count - 1]) < 1e-9) continue;

                if (previousEnd != null)
                {
                    var gap = Distance(previousEnd, points[0]);
                    if (gap > JoinTolerance)
                    {
                        throw new InvalidOperationException(
                            $"The tooth outline of gear '{gear.Id}' is not continuous at '{segment.Name}' (gap {gap:G3} cm), so it cannot be exported.");
                    }

                    // Share the exact end point so that the sketch figures meet exactly.
                    points[0] = previousEnd;
                }

                chain.Add(CreateFigure(segment, points));
                previousEnd = points[points.Count - 1];
                if (chainStart == null) chainStart = points[0];
            }

            if (chainStart == null || previousEnd == null)
            {
                throw new InvalidOperationException($"The tooth outline of gear '{gear.Id}' is empty.");
            }

            // The outline spans exactly one pitch; radial lines close it into a wedge that tiles into a solid gear.
            var span = Math.Abs(NormaliseAngle(Math.Atan2(chainStart.Y, chainStart.X) - Math.Atan2(previousEnd.Y, previousEnd.X)));
            var pitch = 2.0 * Math.PI / gear.Teeth;
            if (Math.Abs(span - pitch) > 1e-5)
            {
                throw new InvalidOperationException(
                    $"The tooth outline of gear '{gear.Id}' spans {span * 180.0 / Math.PI:F4} degrees instead of one pitch ({pitch * 180.0 / Math.PI:F4} degrees).");
            }

            var figures = new List<ToothSketchFigure>();
            var circles = new List<ToothSketchReferenceCircle>();
            var parameters = profile.Parameters;

            if (isInternal)
            {
                // Out to the rim along the radial lines from both ends of the tooth contour, then the rim arc between them.
                var rimRadius = Math.Round(parameters["dedendumRadius"], Decimals) + Math.Max(gear.RimThickness, MinimumRimThickness);
                var rimStart = RimPoint(chainStart, rimRadius);
                var rimEnd = RimPoint(previousEnd, rimRadius);

                figures.AddRange(chain);
                figures.Add(new ToothSketchFigure(ToothSketchFigureKind.Line, "Radial line 1", previousEnd.X, previousEnd.Y, rimEnd.X, rimEnd.Y));
                figures.Add(CreateFigure(new Segment("Rim arc", ToothSketchFigureKind.Arc, new List<GearPoint> { rimEnd, rimStart }),
                    new List<GearPoint> { rimEnd, rimStart }));
                figures.Add(new ToothSketchFigure(ToothSketchFigureKind.Line, "Radial line 2", rimStart.X, rimStart.Y, chainStart.X, chainStart.Y));

                circles.Add(new ToothSketchReferenceCircle("Inside circle", Q(parameters["addendumRadius"])));
                circles.Add(new ToothSketchReferenceCircle("Pitch circle", Q(parameters["pitchRadius"])));
                circles.Add(new ToothSketchReferenceCircle("Base circle", Q(parameters["baseRadius"])));
                circles.Add(new ToothSketchReferenceCircle("Root circle", Q(parameters["dedendumRadius"])));
                circles.Add(new ToothSketchReferenceCircle("Rim circle", Q(rimRadius)));
            }
            else
            {
                figures.Add(new ToothSketchFigure(ToothSketchFigureKind.Line, "Radial line 1", 0.0, 0.0, chainStart.X, chainStart.Y));
                figures.AddRange(chain);
                figures.Add(new ToothSketchFigure(ToothSketchFigureKind.Line, "Radial line 2", previousEnd.X, previousEnd.Y, 0.0, 0.0));

                circles.Add(new ToothSketchReferenceCircle("Root circle", Q(parameters["dedendumRadius"])));
                circles.Add(new ToothSketchReferenceCircle("Base circle", Q(parameters["baseRadius"])));
                circles.Add(new ToothSketchReferenceCircle("Pitch circle", Q(parameters["pitchRadius"])));
                circles.Add(new ToothSketchReferenceCircle("Outside circle", Q(parameters["addendumRadius"])));
            }

            var depth = Q(gear.FaceWidth);
            var herringbone = gear.IsHerringbone && gear.IsHelical;

            // A herringbone gear is one helical half of half the face width, mirrored across the sketch plane.
            var helix = gear.IsHelical ? BuildHelix(gear, parameters["pitchRadius"], herringbone ? depth / 2.0 : depth) : null;

            return new SingleToothSketch(gear.Teeth, depth, string.IsNullOrWhiteSpace(partName) ? PartName(gear) : partName, figures, circles, isInternal, helix,
                herringbone);
        }

        /// <summary>
        /// The name given to the Alibre part, for example <c>SpurGear_Z20_M2p5</c>, <c>HelicalGear_Z20_M2p5_B15R</c>,
        /// <c>HerringboneGear_Z20_M2p5_B15R</c> or <c>InternalGear_Z40_M2</c>.
        /// </summary>
        public static string PartName(InvoluteGearComponent gear)
        {
            var module = FormatNumber(gear.Module);
            var helical = gear.IsHelical ? (gear.IsHerringbone ? "HerringboneGear" : "HelicalGear") : null;
            var kind = gear.IsInternal
                ? (gear.IsHelical ? "Internal" + helical : "InternalGear")
                : (helical ?? "SpurGear");
            var name = $"{kind}_Z{gear.Teeth}_M{module}";
            if (gear.IsHelical) name += $"_B{FormatNumber(gear.HelixAngleDeg)}{gear.HelicalDirection}";
            return name;
        }

        /// <summary>
        /// The helix of a helical tooth (for a herringbone gear, of one half). Over the height H the transverse section of the tooth turns through
        /// H tan(beta) / r (the same rule the 3D preview uses), so one full turn takes the lead 2 pi r / tan(beta), with r the pitch
        /// radius. Right and left hand follow the gear's hand.
        /// </summary>
        private static SingleToothHelix BuildHelix(InvoluteGearComponent gear, double pitchRadiusMm, double heightAlibre)
        {
            var radius = Math.Round(pitchRadiusMm, Decimals);
            var leadMm = 2.0 * Math.PI * radius / Math.Tan(gear.HelixAngleDeg * Math.PI / 180.0);
            return new SingleToothHelix(gear.HelixAngleDeg, gear.HelicalDirection == "R", heightAlibre, Q(leadMm), Q(radius));
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture).Replace('.', 'p');
        }

        /// <summary>Outline of an external tooth: root, trochoid, involute, tip, involute, trochoid, root, and the root arc to the next tooth.</summary>
        private static List<Segment> ExternalSegments(AlibreGearGenerator.AlibreGearProfile profile)
        {
            return new List<Segment>
            {
                new Segment("Trochoid 1", ToothSketchFigureKind.Spline, Scale(profile.Trochoid1)),
                new Segment("Involute 1", ToothSketchFigureKind.Spline, Scale(profile.Involute1)),
                new Segment("Addendum arc", ToothSketchFigureKind.Arc, Scale(profile.UpperArc)),
                new Segment("Involute 2", ToothSketchFigureKind.Spline, Scale(profile.Involute2)),
                new Segment("Trochoid 2", ToothSketchFigureKind.Spline, Scale(profile.Trochoid2)),
                new Segment("Root arc", ToothSketchFigureKind.Arc, Scale(profile.LowerArc))
            };
        }

        /// <summary>Inner outline of an internal tooth, in the order the 3D preview joins it.</summary>
        private static List<Segment> InternalSegments(AlibreGearGenerator.AlibreGearProfile profile)
        {
            return new List<Segment>
            {
                new Segment("Inside arc 1", ToothSketchFigureKind.Arc, Scale(profile.LowerArc2)),
                new Segment("Involute 1", ToothSketchFigureKind.Spline, Scale(profile.Involute1)),
                new Segment("Root arc", ToothSketchFigureKind.Arc, Scale(profile.UpperArc)),
                new Segment("Involute 2", ToothSketchFigureKind.Spline, Scale(profile.Involute2)),
                new Segment("Inside arc 2", ToothSketchFigureKind.Arc, Scale(profile.LowerArc1))
            };
        }

        /// <summary>The point on the rim circle (radius in millimetres) at the same angle as <paramref name="point"/> (Alibre units).</summary>
        private static GearPoint RimPoint(GearPoint point, double rimRadiusMm)
        {
            var angle = Math.Atan2(point.Y, point.X);
            return new GearPoint(Q(rimRadiusMm * Math.Cos(angle)), Q(rimRadiusMm * Math.Sin(angle)));
        }

        private static ToothSketchFigure CreateFigure(Segment segment, List<GearPoint> points)
        {
            var first = points[0];
            var last = points[points.Count - 1];

            switch (segment.Kind)
            {
                case ToothSketchFigureKind.Arc:
                {
                    // Alibre draws an arc anticlockwise from its start point to its end point.
                    var sweep = NormaliseAngle(Math.Atan2(last.Y, last.X) - Math.Atan2(first.Y, first.X));
                    return sweep >= 0
                        ? new ToothSketchFigure(ToothSketchFigureKind.Arc, segment.Name, first.X, first.Y, last.X, last.Y)
                        : new ToothSketchFigure(ToothSketchFigureKind.Arc, segment.Name, last.X, last.Y, first.X, first.Y);
                }
                default:
                    return new ToothSketchFigure(ToothSketchFigureKind.Spline, segment.Name, first.X, first.Y, last.X, last.Y, 0.0, 0.0, points);
            }
        }

        private static List<GearPoint> Scale(List<GearPoint> points)
        {
            var result = new List<GearPoint>(points?.Count ?? 0);
            if (points == null) return result;
            foreach (var point in points) result.Add(new GearPoint(Q(point.X), Q(point.Y)));
            return result;
        }

        /// <summary>Rounds a length in millimetres to 7 decimal places and converts it to Alibre units.</summary>
        private static double Q(double millimetres)
        {
            return Math.Round(millimetres, Decimals) * MmToAlibreUnits;
        }

        private static double Distance(GearPoint a, GearPoint b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>Wraps an angle to the range (-pi, pi].</summary>
        private static double NormaliseAngle(double angle)
        {
            while (angle > Math.PI) angle -= 2.0 * Math.PI;
            while (angle <= -Math.PI) angle += 2.0 * Math.PI;
            return angle;
        }
    }
}
