using System;
using System.Collections.Generic;
using System.Globalization;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>
    /// The plan for exporting one straight bevel gear to Alibre, following the same steps as the 3D rendering in the editor
    /// (<see cref="BevelBlankProfile"/> and <see cref="BevelToothSketch"/>):
    /// <list type="number">
    /// <item>the closed gear blank profile, sketched on the XY plane with the gear axis along X (<see cref="BlankFigures"/>), and a sketch
    /// node at pointB, where the front (pitch) cone meets the X axis (<see cref="ApexNodeX"/>);</item>
    /// <item>a revolve of the blank about the X axis;</item>
    /// <item>a plane on the back cone: a plane offset from the YZ plane to pointC, where the back cone meets the X axis
    /// (<see cref="BackConeApexX"/>), an axis where that plane meets the XY plane, and a plane turned about the axis by
    /// <see cref="BackConePlaneAngleRad"/> so that it holds the back cone line from pointC to pointA;</item>
    /// <item>the sketch of one tooth valley on that plane (<see cref="ToothFigures"/>), drawn in the developed back cone
    /// (Tredgold) with pointC as its origin, its x axis along the back cone line towards pointA and its y axis across the tooth;</item>
    /// <item>a loft cut from the valley sketch to the sketch node, and a circular pattern of the cut about the X axis.</item>
    /// </list>
    /// All lengths are in Alibre units (centimetres).
    /// </summary>
    public sealed class BevelGearSketch
    {
        public BevelGearSketch(int teeth, string partName, double backConeApexX, double apexNodeX, double pointAY, double backConePlaneAngleRad,
            IReadOnlyList<ToothSketchFigure> blankFigures, IReadOnlyList<ToothSketchFigure> toothFigures)
        {
            Teeth = teeth;
            PartName = partName;
            BackConeApexX = backConeApexX;
            ApexNodeX = apexNodeX;
            PointAY = pointAY;
            BackConePlaneAngleRad = backConePlaneAngleRad;
            BlankFigures = blankFigures;
            ToothFigures = toothFigures;
        }

        public int Teeth { get; }

        public string PartName { get; }

        /// <summary>Angle between the teeth for the circular pattern, in radians.</summary>
        public double PatternAngle => 2.0 * Math.PI / Teeth;

        /// <summary>X of pointC, where the back cone meets the X axis: the offset of the first plane from the YZ plane.</summary>
        public double BackConeApexX { get; }

        /// <summary>X of the sketch node at pointB, where the front cone meets the X axis (its Y is zero).</summary>
        public double ApexNodeX { get; }

        /// <summary>Y of pointA, which is at X = 0 (half the pitch diameter).</summary>
        public double PointAY { get; }

        /// <summary>
        /// Angle through which the plane offset to pointC (parallel to the YZ plane) is turned about the Z direction axis so that it
        /// holds the back cone line: the pitch cone angle.
        /// </summary>
        public double BackConePlaneAngleRad { get; }

        /// <summary>Length of the back cone line from pointC to pointA.</summary>
        public double BackConeDistance => Math.Sqrt(BackConeApexX * BackConeApexX + PointAY * PointAY);

        /// <summary>
        /// Unit direction of the back cone line from pointC towards pointA in the XY plane. The plane that holds the back cone has the
        /// normal (<c>-dy, dx</c>) or its opposite.
        /// </summary>
        public void GetBackConeDirection(out double dx, out double dy)
        {
            dx = -BackConeApexX / BackConeDistance;
            dy = PointAY / BackConeDistance;
        }

        /// <summary>The closed gear blank profile (lines F-D, D-E, E-G, G-H, H-I, I-F), in sketch units on the XY plane.</summary>
        public IReadOnlyList<ToothSketchFigure> BlankFigures { get; }

        /// <summary>
        /// The closed sketch of one tooth valley in the developed back cone: x along the back cone line from pointC towards pointA,
        /// y across the tooth, origin at pointC. The valley is symmetric about the x axis. Arcs are about the origin.
        /// </summary>
        public IReadOnlyList<ToothSketchFigure> ToothFigures { get; }

        /// <summary>True when the figures form closed loops: every end point meets exactly one other end point within the tolerance.</summary>
        public static bool IsClosed(IReadOnlyList<ToothSketchFigure> figures, double tolerance = 1e-6)
        {
            var ends = new List<double[]>();
            foreach (var figure in figures)
            {
                ends.Add(new[] { figure.StartX, figure.StartY });
                ends.Add(new[] { figure.EndX, figure.EndY });
            }

            for (var i = 0; i < ends.Count; i++)
            {
                var matches = 0;
                for (var j = 0; j < ends.Count; j++)
                {
                    if (i == j) continue;
                    var dx = ends[i][0] - ends[j][0];
                    var dy = ends[i][1] - ends[j][1];
                    if (Math.Sqrt(dx * dx + dy * dy) <= tolerance) matches++;
                }

                if (matches != 1) return false;
            }

            return ends.Count > 0;
        }

        /// <summary>The name given to the Alibre part, for example <c>BevelGear_Z18_M10p16</c> or <c>BevelGearGleason_Z18_M10p16</c>.</summary>
        public static string PartNameOf(BevelGearComponent gear)
        {
            var module = gear.Module.ToString("0.###", CultureInfo.InvariantCulture).Replace('.', 'p');
            var kind = gear.BevelType == BevelGearType.Gleason ? "BevelGearGleason" : "BevelGear";
            return $"{kind}_Z{gear.Teeth}_M{module}";
        }

        /// <summary>
        /// Builds the plan for a bevel gear: the blank, the node and the valley sketch come from the same
        /// <see cref="BevelBlankProfile"/> and <see cref="BevelToothSketch"/> as the 3D view of the editor.
        /// Throws <see cref="InvalidOperationException"/> when the gear is not valid or has no tooth sketch.
        /// </summary>
        public static BevelGearSketch Build(BevelGearComponent gear, string partName = null)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            gear.Validate();

            BevelBlankProfile blank;
            BevelToothSketch sketch;
            try
            {
                blank = gear.CreateBlankProfile();
                sketch = new BevelToothSketch(blank, gear.PressureAngleDeg, rackTipRadiusFactor: gear.RackTipRadiusFactor, backlash: gear.Backlash);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException($"Bevel gear '{gear.Id}' cannot be exported to Alibre: {ex.Message}", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException($"Bevel gear '{gear.Id}' cannot be exported to Alibre: {ex.Message}", ex);
            }

            var blankFigures = new List<ToothSketchFigure>();
            foreach (var line in blank.Lines)
            {
                var from = line.Value[0];
                var to = line.Value[1];
                blankFigures.Add(new ToothSketchFigure(ToothSketchFigureKind.Line, line.Key, Q(from.X), Q(from.Y), Q(to.X), Q(to.Y)));
            }

            if (!IsClosed(blankFigures)) throw new InvalidOperationException($"The blank profile of bevel gear '{gear.Id}' is not closed, so it cannot be exported.");

            var toothFigures = BuildValleyFigures(gear, sketch);
            if (!IsClosed(toothFigures)) throw new InvalidOperationException($"The tooth sketch of bevel gear '{gear.Id}' is not closed, so it cannot be exported.");

            return new BevelGearSketch(gear.Teeth, string.IsNullOrWhiteSpace(partName) ? PartNameOf(gear) : partName,
                Q(blank.PointC.X), Q(blank.PointB.X), Q(blank.PointA.Y), blank.PitchConeAngleRad, blankFigures, toothFigures);
        }

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

        /// <summary>Largest gap, in sketch units, tolerated between neighbouring curves before the sketch is rejected.</summary>
        private const double JoinTolerance = 1e-4;

        private static List<ToothSketchFigure> BuildValleyFigures(BevelGearComponent gear, BevelToothSketch sketch)
        {
            var leftFlank = Reverse(sketch.LeftFlank);
            var leftFillet = Reverse(sketch.LeftFillet);
            var segments = new List<Segment>
            {
                new Segment("Root arc", ToothSketchFigureKind.Arc, Scale(sketch.RootArc)),
                new Segment("Right fillet", ToothSketchFigureKind.Spline, Scale(sketch.RightFillet)),
                new Segment("Right flank", ToothSketchFigureKind.Spline, Scale(sketch.RightFlank)),
                new Segment("Outer arc", ToothSketchFigureKind.Arc, Scale(sketch.OuterArc)),
                new Segment("Left flank", ToothSketchFigureKind.Spline, Scale(leftFlank)),
                new Segment("Left fillet", ToothSketchFigureKind.Spline, Scale(leftFillet))
            };

            var figures = new List<ToothSketchFigure>();
            GearPoint previousEnd = null;
            GearPoint chainStart = null;
            foreach (var segment in segments)
            {
                var points = segment.Points;
                if (points.Count < 2) continue;
                if (Distance(points[0], points[points.Count - 1]) < 1e-9 && segment.Kind == ToothSketchFigureKind.Arc) continue;

                if (previousEnd != null)
                {
                    var gap = Distance(previousEnd, points[0]);
                    if (gap > JoinTolerance)
                    {
                        throw new InvalidOperationException(
                            $"The tooth sketch of bevel gear '{gear.Id}' is not continuous at '{segment.Name}' (gap {gap:G3} cm), so it cannot be exported.");
                    }

                    // Share the exact end point so that the sketch figures meet exactly.
                    points[0] = previousEnd;
                }

                figures.Add(CreateFigure(segment, points));
                previousEnd = points[points.Count - 1];
                if (chainStart == null) chainStart = points[0];
            }

            if (chainStart == null || previousEnd == null || figures.Count == 0)
            {
                throw new InvalidOperationException($"The tooth sketch of bevel gear '{gear.Id}' is empty.");
            }

            var closing = Distance(previousEnd, chainStart);
            if (closing > JoinTolerance)
            {
                throw new InvalidOperationException($"The tooth sketch of bevel gear '{gear.Id}' does not close (gap {closing:G3} cm), so it cannot be exported.");
            }

            // Close the loop exactly: the last figure ends where the first begins.
            figures[figures.Count - 1] = WithEnd(figures[figures.Count - 1], chainStart);
            return figures;
        }

        private static ToothSketchFigure WithEnd(ToothSketchFigure figure, GearPoint end)
        {
            switch (figure.Kind)
            {
                case ToothSketchFigureKind.Arc:
                {
                    // The end of the loop is the end of the chain; for an arc drawn the other way round the chain end is its start.
                    var chainEndIsStart = Distance(new GearPoint(figure.StartX, figure.StartY), end) < Distance(new GearPoint(figure.EndX, figure.EndY), end);
                    return chainEndIsStart
                        ? new ToothSketchFigure(figure.Kind, figure.Name, end.X, end.Y, figure.EndX, figure.EndY)
                        : new ToothSketchFigure(figure.Kind, figure.Name, figure.StartX, figure.StartY, end.X, end.Y);
                }
                default:
                {
                    var points = new List<GearPoint>(figure.Points);
                    points[points.Count - 1] = end;
                    return new ToothSketchFigure(figure.Kind, figure.Name, figure.StartX, figure.StartY, end.X, end.Y, 0.0, 0.0, points);
                }
            }
        }

        private static ToothSketchFigure CreateFigure(Segment segment, List<GearPoint> points)
        {
            var first = points[0];
            var last = points[points.Count - 1];

            if (segment.Kind == ToothSketchFigureKind.Arc)
            {
                // Alibre draws an arc anticlockwise from its start point to its end point; the arc is about the origin (pointC).
                var sweep = NormaliseAngle(Math.Atan2(last.Y, last.X) - Math.Atan2(first.Y, first.X));
                return sweep >= 0
                    ? new ToothSketchFigure(ToothSketchFigureKind.Arc, segment.Name, first.X, first.Y, last.X, last.Y)
                    : new ToothSketchFigure(ToothSketchFigureKind.Arc, segment.Name, last.X, last.Y, first.X, first.Y);
            }

            return new ToothSketchFigure(ToothSketchFigureKind.Spline, segment.Name, first.X, first.Y, last.X, last.Y, 0.0, 0.0, points);
        }

        private static List<BevelToothPoint> Reverse(IReadOnlyList<BevelToothPoint> points)
        {
            var result = new List<BevelToothPoint>(points);
            result.Reverse();
            return result;
        }

        /// <summary>Converts points on the developed back cone (radius from pointC and angle) to plane coordinates in sketch units.</summary>
        private static List<GearPoint> Scale(IReadOnlyList<BevelToothPoint> points)
        {
            var result = new List<GearPoint>(points.Count);
            foreach (var point in points) result.Add(new GearPoint(Q(point.X), Q(point.Y)));
            return result;
        }

        /// <summary>Rounds a length in millimetres to 7 decimal places and converts it to Alibre units.</summary>
        private static double Q(double millimetres)
        {
            return Math.Round(millimetres, 7) * SingleToothSketchBuilder.MmToAlibreUnits;
        }

        private static double Distance(GearPoint a, GearPoint b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double NormaliseAngle(double angle)
        {
            while (angle > Math.PI) angle -= 2.0 * Math.PI;
            while (angle <= -Math.PI) angle += 2.0 * Math.PI;
            return angle;
        }
    }
}
