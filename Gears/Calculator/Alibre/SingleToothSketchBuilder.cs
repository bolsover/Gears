using System;
using System.Collections.Generic;
using System.Globalization;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>
    /// Builds the <see cref="SingleToothSketch"/> of an external involute spur gear from the tooth profile made by
    /// <see cref="AlibreGearGenerator"/>. Gears are calculated in millimetres to 7 decimal places; Alibre sketches are in
    /// centimetres, so every length is rounded to 7 decimal places in millimetres and then divided by 10.
    /// </summary>
    public static class SingleToothSketchBuilder
    {
        /// <summary>Alibre part sketches and features are in centimetres: 1 mm is 0.1 sketch units.</summary>
        public const double MmToAlibreUnits = 0.1;

        /// <summary>Smallest tooth count that gives a sensible circular pattern.</summary>
        public const int MinimumTeeth = 3;

        private const int Decimals = 7;

        /// <summary>Points per curve used for the sketch: involute, trochoid, addendum arc, dedendum arc.</summary>
        private static readonly int[] PointsPerCurve = { 30, 15, 5, 5 };

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

        /// <summary>Throws <see cref="InvalidOperationException"/> when the gear is not a simple external involute spur gear.</summary>
        public static void EnsureSupported(InvoluteGearComponent gear)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            if (gear.IsInternal)
            {
                throw new InvalidOperationException($"Gear '{gear.Id}' is an internal gear. Only external involute spur gears can be exported to Alibre so far.");
            }

            if (gear.IsHelical)
            {
                throw new InvalidOperationException($"Gear '{gear.Id}' is helical. Only external involute spur gears can be exported to Alibre so far.");
            }

            if (gear.Teeth < MinimumTeeth)
            {
                throw new InvalidOperationException($"Gear '{gear.Id}' needs at least {MinimumTeeth} teeth to be exported to Alibre.");
            }

            gear.Validate();
        }

        public static SingleToothSketch Build(InvoluteGearComponent gear)
        {
            EnsureSupported(gear);

            var profile = AlibreGearGenerator.GenerateExternalToothProfile(
                gear.Teeth, gear.Module, gear.PressureAngleDeg, gear.ProfileShift, gear.OptimalShift,
                PointsPerCurve, gear.Clearance, gear.Backlash, 0.0);

            // The outline runs: root, trochoid, involute, tip, involute, trochoid, root, and the root arc to the next tooth.
            var segments = new List<Segment>
            {
                new Segment("Trochoid 1", ToothSketchFigureKind.Spline, Scale(profile.Trochoid1)),
                new Segment("Involute 1", ToothSketchFigureKind.Spline, Scale(profile.Involute1)),
                new Segment("Addendum arc", ToothSketchFigureKind.Arc, Scale(profile.UpperArc)),
                new Segment("Involute 2", ToothSketchFigureKind.Spline, Scale(profile.Involute2)),
                new Segment("Trochoid 2", ToothSketchFigureKind.Spline, Scale(profile.Trochoid2)),
                new Segment("Root arc", ToothSketchFigureKind.Arc, Scale(profile.LowerArc))
            };

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

            // The outline spans exactly one pitch; two radial lines to the axis close it into a wedge.
            var span = Math.Abs(NormaliseAngle(Math.Atan2(chainStart.Y, chainStart.X) - Math.Atan2(previousEnd.Y, previousEnd.X)));
            var pitch = 2.0 * Math.PI / gear.Teeth;
            if (Math.Abs(span - pitch) > 1e-5)
            {
                throw new InvalidOperationException(
                    $"The tooth outline of gear '{gear.Id}' spans {span * 180.0 / Math.PI:F4} degrees instead of one pitch ({pitch * 180.0 / Math.PI:F4} degrees).");
            }

            var figures = new List<ToothSketchFigure>
            {
                new ToothSketchFigure(ToothSketchFigureKind.Line, "Radial line 1", 0.0, 0.0, chainStart.X, chainStart.Y)
            };
            figures.AddRange(chain);
            figures.Add(new ToothSketchFigure(ToothSketchFigureKind.Line, "Radial line 2", previousEnd.X, previousEnd.Y, 0.0, 0.0));

            var circles = new List<ToothSketchReferenceCircle>
            {
                new ToothSketchReferenceCircle("Root circle", Q(profile.Parameters["dedendumRadius"])),
                new ToothSketchReferenceCircle("Base circle", Q(profile.Parameters["baseRadius"])),
                new ToothSketchReferenceCircle("Pitch circle", Q(profile.Parameters["pitchRadius"])),
                new ToothSketchReferenceCircle("Outside circle", Q(profile.Parameters["addendumRadius"]))
            };

            return new SingleToothSketch(gear.Teeth, Q(gear.FaceWidth), PartName(gear), figures, circles);
        }

        /// <summary>The name given to the Alibre part, for example <c>SpurGear_Z20_M2p5</c>.</summary>
        public static string PartName(InvoluteGearComponent gear)
        {
            var module = gear.Module.ToString("0.###", CultureInfo.InvariantCulture).Replace('.', 'p');
            return $"SpurGear_Z{gear.Teeth}_M{module}";
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
