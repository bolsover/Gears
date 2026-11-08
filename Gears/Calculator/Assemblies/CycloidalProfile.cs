using System;
using System.Collections.Generic;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>The kinds of piece that make up the outline of a cycloidal gear.</summary>
    public enum CycloidalPieceKind
    {
        Line,

        /// <summary>A circular arc about <see cref="CycloidalPiece.Centre"/>, anticlockwise from start to end.</summary>
        Arc
    }

    /// <summary>One line or arc of the outline of a cycloidal gear (millimetres, in the gear plane).</summary>
    public sealed class CycloidalPiece
    {
        public CycloidalPiece(CycloidalPieceKind kind, string name, GearPoint start, GearPoint end, GearPoint centre)
        {
            Kind = kind;
            Name = name;
            Start = start;
            End = end;
            Centre = centre;
        }

        public CycloidalPieceKind Kind { get; }
        public string Name { get; }
        public GearPoint Start { get; }
        public GearPoint End { get; }

        /// <summary>Centre of an arc; not used for a line.</summary>
        public GearPoint Centre { get; }
    }

    /// <summary>The outline of a cycloidal gear as pieces for a sketch and as a closed ring of points for the 3D view.</summary>
    public sealed class CycloidalOutline
    {
        public CycloidalOutline(IReadOnlyList<CycloidalPiece> pieces, IReadOnlyList<GearPoint> ring)
        {
            Pieces = pieces;
            Ring = ring;
        }

        /// <summary>The lines and arcs of the closed outline. Each arc runs anticlockwise from its start to its end.</summary>
        public IReadOnlyList<CycloidalPiece> Pieces { get; }

        /// <summary>The points of the outline running anticlockwise, the first point not repeated.</summary>
        public IReadOnlyList<GearPoint> Ring { get; }
    }

    /// <summary>
    /// Builds the outline of a <see cref="CycloidalGearComponent"/>. For tooth <c>i</c> with its tip at the angle <c>a</c> and half tooth
    /// angle <c>h</c>, the points are the tip P1 (outside radius, at <c>a</c>), P2 and P5 (pitch radius, at <c>a + h</c> and <c>a - h</c>) and
    /// P3 and P4 (root radius, at <c>a + h</c> and <c>a - h</c>). The tooth is two circular arcs of the addendum radius from P5 to P1 and from
    /// P1 to P2, a radial flank P2-P3 and P4-P5, and a root arc about the gear axis from P3 to the P4 of the next tooth.
    /// </summary>
    public static class CycloidalProfile
    {
        /// <summary>Number of segments of one arc of a tooth tip.</summary>
        public const int TipArcSteps = 8;

        /// <summary>Angle of one segment of a root arc (radians).</summary>
        private const double RootArcStep = Math.PI / 36.0;

        public static CycloidalOutline Build(CycloidalGearComponent gear)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            gear.Validate();

            var teeth = gear.Teeth;
            var pitchRadius = gear.PitchDiameter / 2.0;
            var outerRadius = gear.OutsideDiameter / 2.0;
            var rootRadius = gear.RootDiameter / 2.0;
            var half = gear.HalfToothAngle;
            var step = 2.0 * Math.PI / teeth;
            var tipRadius = gear.AddendumRadius;
            var origin = new GearPoint(0.0, 0.0);

            var pieces = new List<CycloidalPiece>(5 * teeth);
            var ring = new List<GearPoint>();

            for (var i = 0; i < teeth; i++)
            {
                var angle = i * step;
                var p1 = Polar(outerRadius, angle);
                var p2 = Polar(pitchRadius, angle + half);
                var p3 = Polar(rootRadius, angle + half);
                var p4 = Polar(rootRadius, angle - half);
                var p5 = Polar(pitchRadius, angle - half);
                var nextP4 = Polar(rootRadius, angle + step - half);

                var centreLeft = ArcCentre(gear, tipRadius, p5, p1);
                var centreRight = ArcCentre(gear, tipRadius, p1, p2);

                var name = "Tooth" + i;
                pieces.Add(Line(name + "FlankLeft", p4, p5));

                // The tip arcs bulge away from the axis and their centres are on the axis side, so in the anticlockwise order of the
                // outline they also run anticlockwise about their own centres, as an arc is stored.
                pieces.Add(new CycloidalPiece(CycloidalPieceKind.Arc, name + "TipLeft", p5, p1, centreLeft));
                pieces.Add(new CycloidalPiece(CycloidalPieceKind.Arc, name + "TipRight", p1, p2, centreRight));
                pieces.Add(Line(name + "FlankRight", p2, p3));
                pieces.Add(new CycloidalPiece(CycloidalPieceKind.Arc, name + "Root", p3, nextP4, origin));

                ring.Add(p4);
                ring.Add(p5);
                AddArc(ring, centreLeft, p5, p1, TipArcSteps);
                AddArc(ring, centreRight, p1, p2, TipArcSteps);
                ring.Add(p3);
                var rootSteps = Math.Max(2, (int)Math.Ceiling((step - 2.0 * half) / RootArcStep));
                AddArc(ring, origin, p3, nextP4, rootSteps, includeEnd: false);
            }

            return new CycloidalOutline(pieces, ring);
        }

        private static GearPoint Polar(double radius, double angle)
        {
            return new GearPoint(radius * Math.Cos(angle), radius * Math.Sin(angle));
        }

        private static CycloidalPiece Line(string name, GearPoint from, GearPoint to)
        {
            return new CycloidalPiece(CycloidalPieceKind.Line, name, from, to, from);
        }

        /// <summary>
        /// The centre of the circle of the given radius through a and b that lies on the left of the way from a to b, which is towards the
        /// axis for an outline that runs anticlockwise round it. The arc between a and b then bulges away from the axis.
        /// </summary>
        private static GearPoint ArcCentre(CycloidalGearComponent gear, double radius, GearPoint a, GearPoint b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var q = Math.Sqrt(dx * dx + dy * dy);
            var rise = radius * radius - q * q / 4.0;
            if (rise < 0.0)
            {
                throw new InvalidOperationException(
                    $"Cycloidal gear '{gear.Id}': the tooth tip radius of {radius:0.###} mm is too small to join the tip to the pitch circle.");
            }

            var offset = Math.Sqrt(rise);
            return new GearPoint((a.X + b.X) / 2.0 - offset * dy / q, (a.Y + b.Y) / 2.0 + offset * dx / q);
        }

        /// <summary>
        /// Adds the points of the shorter arc about <paramref name="centre"/> from <paramref name="from"/> to <paramref name="to"/>, not
        /// including <paramref name="from"/>, and including <paramref name="to"/> unless <paramref name="includeEnd"/> is false.
        /// </summary>
        private static void AddArc(List<GearPoint> ring, GearPoint centre, GearPoint from, GearPoint to, int steps, bool includeEnd = true)
        {
            var radius = Math.Sqrt((from.X - centre.X) * (from.X - centre.X) + (from.Y - centre.Y) * (from.Y - centre.Y));
            var a0 = Math.Atan2(from.Y - centre.Y, from.X - centre.X);
            var a1 = Math.Atan2(to.Y - centre.Y, to.X - centre.X);
            var sweep = a1 - a0;
            while (sweep > Math.PI) sweep -= 2.0 * Math.PI;
            while (sweep <= -Math.PI) sweep += 2.0 * Math.PI;

            var last = includeEnd ? steps : steps - 1;
            for (var k = 1; k <= last; k++)
            {
                if (k == steps)
                {
                    ring.Add(to);
                    continue;
                }

                var a = a0 + sweep * k / steps;
                ring.Add(new GearPoint(centre.X + radius * Math.Cos(a), centre.Y + radius * Math.Sin(a)));
            }
        }
    }
}
