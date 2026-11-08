using System;
using System.Collections.Generic;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>The kinds of piece that make up the toothed inner edge of a timing belt.</summary>
    public enum BeltPieceKind
    {
        Line,

        /// <summary>A circular arc about <see cref="BeltPiece.Centre"/>, anticlockwise from start to end.</summary>
        Arc,

        /// <summary>A smooth curve through <see cref="BeltPiece.Points"/>.</summary>
        Spline
    }

    /// <summary>One line, arc or spline of the inner edge of a belt (millimetres, in the driver shaft's XY plane).</summary>
    public sealed class BeltPiece
    {
        public BeltPiece(BeltPieceKind kind, string name, GearPoint start, GearPoint end, GearPoint centre, IReadOnlyList<GearPoint> points)
        {
            Kind = kind;
            Name = name;
            Start = start;
            End = end;
            Centre = centre;
            Points = points;
        }

        public BeltPieceKind Kind { get; }
        public string Name { get; }
        public GearPoint Start { get; }
        public GearPoint End { get; }

        /// <summary>Centre of an arc; not used for other kinds.</summary>
        public GearPoint Centre { get; }

        /// <summary>The points of a spline, the first being <see cref="Start"/> and the last <see cref="End"/>; null for other kinds.</summary>
        public IReadOnlyList<GearPoint> Points { get; }
    }

    /// <summary>
    /// The toothed inner edge of a belt, with the points of the back of the belt that go with it. Both rings have the same number of
    /// points, run anticlockwise (first point not repeated) and lie in the plane with the driver on the origin.
    /// </summary>
    public sealed class BeltToothedLoop
    {
        public BeltToothedLoop(IReadOnlyList<BeltPiece> innerPieces, IReadOnlyList<GearPoint> innerRing, IReadOnlyList<GearPoint> outerRing, int toothCount)
        {
            InnerPieces = innerPieces;
            InnerRing = innerRing;
            OuterRing = outerRing;
            ToothCount = toothCount;
        }

        /// <summary>The closed inner edge as lines, arcs and splines in drawing order: each tooth is two edge splines, two flat flanks and a bottom spline, joined by the lands between the teeth.</summary>
        public IReadOnlyList<BeltPiece> InnerPieces { get; }

        /// <summary>The points of the inner edge (teeth tips towards the pulleys).</summary>
        public IReadOnlyList<GearPoint> InnerRing { get; }

        /// <summary>For every inner point the point on the back of the belt that lies straight out from it.</summary>
        public IReadOnlyList<GearPoint> OuterRing { get; }

        /// <summary>Number of teeth on the belt.</summary>
        public int ToothCount { get; }
    }

    /// <summary>
    /// Lays the teeth of a <see cref="TimingBelt"/> along its path. The belt tooth is the negative of the pulley groove (the same
    /// <see cref="HtdPulleyComponent.HalfGroove"/> profile): a tooth stands on the land at the outside diameter of the pulleys and reaches
    /// to the root diameter. The teeth are spaced evenly along the pitch line, <see cref="TimingBelt.BeltTeeth"/> of them. Round a pulley
    /// a tooth is drawn like the groove it fills (its width follows the radius), and on a straight run it is the plain profile.
    /// </summary>
    public static class BeltToothPath
    {
        /// <summary>Angle of one segment of the land arcs between teeth (radians).</summary>
        private const double ArcStep = Math.PI / 36.0;

        private sealed class Segment
        {
            public bool IsArc;
            public double Length;
            public double CentreX, CentreY, Radius, StartAngle;
            public double StartX, StartY, TangentX, TangentY, NormalX, NormalY;
        }

        private sealed class Tooth
        {
            public int Segment;
            public List<GearPoint> Inner = new List<GearPoint>();
            public List<GearPoint> Outer = new List<GearPoint>();
            public int LeftCount;
            public int BottomCount;
        }

        /// <summary>Builds the toothed inner edge. Throws <see cref="InvalidOperationException"/> when the belt cannot be drawn.</summary>
        public static BeltToothedLoop Build(TimingBelt belt)
        {
            if (belt == null) throw new ArgumentNullException(nameof(belt));
            belt.Validate();

            var profile = belt.Profile;
            var radiusA = belt.Driver.PitchDiameter / 2.0;
            var radiusB = belt.Driven.PitchDiameter / 2.0;
            var distance = belt.CentreDistance;
            var theta = belt.CentreLineAngleDeg * Math.PI / 180.0;
            var cosTheta = Math.Cos(theta);
            var sinTheta = Math.Sin(theta);

            var nx = (radiusA - radiusB) / distance;
            var ny = Math.Sqrt(Math.Max(0.0, 1.0 - nx * nx));
            var upper = Math.Atan2(ny, nx);
            var lower = Math.Atan2(-ny, nx);
            var run = distance * ny;

            var landOffset = -profile.PitchLineDifferential;
            var backOffset = landOffset + belt.BackThickness;
            var depth = profile.ToothHeight;

            var segments = new[]
            {
                new Segment { IsArc = true, CentreX = distance, CentreY = 0.0, Radius = radiusB, StartAngle = lower, Length = radiusB * (upper - lower) },
                new Segment
                {
                    StartX = distance + radiusB * nx, StartY = radiusB * ny, TangentX = -ny, TangentY = nx, NormalX = nx, NormalY = ny, Length = run
                },
                new Segment { IsArc = true, CentreX = 0.0, CentreY = 0.0, Radius = radiusA, StartAngle = upper, Length = radiusA * (lower + 2.0 * Math.PI - upper) },
                new Segment
                {
                    StartX = radiusA * nx, StartY = -radiusA * ny, TangentX = ny, TangentY = nx, NormalX = nx, NormalY = -ny, Length = run
                }
            };

            var total = 0.0;
            foreach (var segment in segments) total += segment.Length;

            var count = belt.BeltTeeth;
            if (count < 2) throw new InvalidOperationException($"Belt '{belt.Id}' is too short to have teeth.");
            var spacing = total / count;

            GearPoint Rotate(double x, double y) => new GearPoint(x * cosTheta - y * sinTheta, x * sinTheta + y * cosTheta);

            // The point of a tooth at u across (along the belt) and v up from the tip, and the point of the belt back straight out from it.
            void Place(Segment segment, double along, double u, double v, out GearPoint inner, out GearPoint outer)
            {
                if (segment.IsArc)
                {
                    var radius = segment.Radius + landOffset - depth + v;
                    var angle = segment.StartAngle + along / segment.Radius + u / radius;
                    var backRadius = segment.Radius + backOffset;
                    inner = Rotate(segment.CentreX + radius * Math.Cos(angle), segment.CentreY + radius * Math.Sin(angle));
                    outer = Rotate(segment.CentreX + backRadius * Math.Cos(angle), segment.CentreY + backRadius * Math.Sin(angle));
                }
                else
                {
                    var baseX = segment.StartX + segment.TangentX * (along + u);
                    var baseY = segment.StartY + segment.TangentY * (along + u);
                    var d = landOffset - depth + v;
                    inner = Rotate(baseX + segment.NormalX * d, baseY + segment.NormalY * d);
                    outer = Rotate(baseX + segment.NormalX * backOffset, baseY + segment.NormalY * backOffset);
                }
            }

            var half = HtdPulleyComponent.HalfGroove(profile);
            var flankStart = HtdPulleyComponent.BottomArcSteps;
            var edgeStart = flankStart + 1;

            var teeth = new List<Tooth>(count);
            var segmentIndex = 0;
            var segmentStart = 0.0;
            for (var k = 0; k < count; k++)
            {
                var s = (k + 0.5) * spacing;
                while (segmentIndex < segments.Length - 1 && s >= segmentStart + segments[segmentIndex].Length)
                {
                    segmentStart += segments[segmentIndex].Length;
                    segmentIndex++;
                }

                var segment = segments[segmentIndex];
                var along = s - segmentStart;
                var tooth = new Tooth { Segment = segmentIndex };

                void Add(double u, double v)
                {
                    Place(segment, along, u, v, out var inner, out var outer);
                    tooth.Inner.Add(inner);
                    tooth.Outer.Add(outer);
                }

                for (var i = half.Count - 1; i >= edgeStart; i--) Add(-half[i].X, half[i].Y);
                tooth.LeftCount = tooth.Inner.Count;
                for (var i = flankStart; i >= 1; i--) Add(-half[i].X, half[i].Y);
                for (var i = 0; i <= flankStart; i++) Add(half[i].X, half[i].Y);
                tooth.BottomCount = tooth.Inner.Count - tooth.LeftCount;
                for (var i = edgeStart; i < half.Count; i++) Add(half[i].X, half[i].Y);
                teeth.Add(tooth);
            }

            var pieces = new List<BeltPiece>();
            var innerRing = new List<GearPoint>();
            var outerRing = new List<GearPoint>();

            for (var k = 0; k < count; k++)
            {
                var tooth = teeth[k];
                var points = tooth.Inner;
                var rightStart = tooth.LeftCount + tooth.BottomCount;
                innerRing.AddRange(points);
                outerRing.AddRange(tooth.Outer);

                var leftEdge = points.GetRange(0, tooth.LeftCount);
                var bottom = points.GetRange(tooth.LeftCount, tooth.BottomCount);
                var rightEdge = points.GetRange(rightStart, points.Count - rightStart);
                var name = "Tooth" + k;
                pieces.Add(Spline(name + "EdgeLeft", leftEdge));
                pieces.Add(Line(name + "FlankLeft", leftEdge[leftEdge.Count - 1], bottom[0]));
                pieces.Add(Spline(name + "Bottom", bottom));
                pieces.Add(Line(name + "FlankRight", bottom[bottom.Count - 1], rightEdge[0]));
                pieces.Add(Spline(name + "EdgeRight", rightEdge));

                // The land to the next tooth: an arc of the outside diameter when both teeth are on the same pulley, otherwise a line.
                var next = (k + 1) % count;
                var from = points[points.Count - 1];
                var to = teeth[next].Inner[0];
                var segment = segments[tooth.Segment];
                if (next > k && teeth[next].Segment == tooth.Segment && segment.IsArc)
                {
                    var centre = Rotate(segment.CentreX, segment.CentreY);
                    var angleFrom = Math.Atan2(from.Y - centre.Y, from.X - centre.X);
                    var angleTo = Math.Atan2(to.Y - centre.Y, to.X - centre.X);
                    while (angleTo <= angleFrom) angleTo += 2.0 * Math.PI;

                    var landRadius = segment.Radius + landOffset;
                    var backRadius = segment.Radius + backOffset;
                    var steps = Math.Max(1, (int)Math.Ceiling((angleTo - angleFrom) / ArcStep));
                    for (var i = 1; i < steps; i++)
                    {
                        var angle = angleFrom + (angleTo - angleFrom) * i / steps;
                        innerRing.Add(new GearPoint(centre.X + landRadius * Math.Cos(angle), centre.Y + landRadius * Math.Sin(angle)));
                        outerRing.Add(new GearPoint(centre.X + backRadius * Math.Cos(angle), centre.Y + backRadius * Math.Sin(angle)));
                    }

                    pieces.Add(new BeltPiece(BeltPieceKind.Arc, "Land" + k, from, to, centre, null));
                }
                else
                {
                    pieces.Add(Line("Land" + k, from, to));
                }
            }

            return new BeltToothedLoop(pieces, innerRing, outerRing, count);
        }

        private static BeltPiece Line(string name, GearPoint from, GearPoint to)
        {
            return new BeltPiece(BeltPieceKind.Line, name, from, to, from, null);
        }

        private static BeltPiece Spline(string name, List<GearPoint> points)
        {
            return new BeltPiece(BeltPieceKind.Spline, name, points[0], points[points.Count - 1], points[0], points);
        }
    }
}
