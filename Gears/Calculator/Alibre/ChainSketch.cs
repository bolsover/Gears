using System;
using System.Collections.Generic;
using System.Globalization;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>A circle of the chain sketch (a roller or a pin), in Alibre units (centimetres).</summary>
    public sealed class ChainSketchCircle
    {
        public ChainSketchCircle(double x, double y, double radius)
        {
            X = x;
            Y = y;
            Radius = radius;
        }

        public double X { get; }
        public double Y { get; }
        public double Radius { get; }
    }

    /// <summary>
    /// The plan for exporting one roller chain to Alibre. A chain is made of plates at different heights along the axis, which a sketch plane
    /// cannot reach without an offset plane, so every step is a mid-plane extrusion about the XY plane and the order of the steps leaves the
    /// right material at every height:
    /// <list type="number">
    /// <item><description>a boss of all outer links (stadium outlines, <see cref="OuterLinks"/>) over the full <see cref="OverallWidth"/>,</description></item>
    /// <item><description>a cut of the same outlines over <see cref="InnerLinkWidth"/>, which leaves the two outer plates of every outer link,</description></item>
    /// <item><description>a boss of all inner links (<see cref="InnerLinks"/>) over <see cref="InnerLinkWidth"/>, which also fills what the cut took out where an
    /// inner link overlaps an outer one round a pin,</description></item>
    /// <item><description>a cut of the inner outlines over <see cref="RollerWidth"/>, which leaves the two inner plates of every inner link,</description></item>
    /// <item><description>a boss of the <see cref="Rollers"/> over <see cref="RollerWidth"/>,</description></item>
    /// <item><description>a boss of the <see cref="Pins"/> over <see cref="OverallWidth"/>.</description></item>
    /// </list>
    /// The driver sprocket's axis is the origin and the driven sprocket lies on the line of centres, as in the 3D view. All lengths are in Alibre units
    /// (centimetres).
    /// </summary>
    public sealed class ChainSketch
    {
        public ChainSketch(string partName, int linkCount, double overallWidth, double innerLinkWidth, double rollerWidth,
            IReadOnlyList<ToothSketchFigure> outerLinks, IReadOnlyList<ToothSketchFigure> innerLinks,
            IReadOnlyList<ChainSketchCircle> rollers, IReadOnlyList<ChainSketchCircle> pins)
        {
            PartName = partName;
            LinkCount = linkCount;
            OverallWidth = overallWidth;
            InnerLinkWidth = innerLinkWidth;
            RollerWidth = rollerWidth;
            OuterLinks = outerLinks ?? throw new ArgumentNullException(nameof(outerLinks));
            InnerLinks = innerLinks ?? throw new ArgumentNullException(nameof(innerLinks));
            Rollers = rollers ?? throw new ArgumentNullException(nameof(rollers));
            Pins = pins ?? throw new ArgumentNullException(nameof(pins));
        }

        public string PartName { get; }

        public int LinkCount { get; }

        /// <summary>Width over the outer plates (cm).</summary>
        public double OverallWidth { get; }

        /// <summary>Width over the inner plates (cm).</summary>
        public double InnerLinkWidth { get; }

        /// <summary>Width of the rollers, between the inner plates (cm).</summary>
        public double RollerWidth { get; }

        /// <summary>The stadium outlines of the outer links (odd links), four figures each.</summary>
        public IReadOnlyList<ToothSketchFigure> OuterLinks { get; }

        /// <summary>The stadium outlines of the inner links (even links), four figures each.</summary>
        public IReadOnlyList<ToothSketchFigure> InnerLinks { get; }

        public IReadOnlyList<ChainSketchCircle> Rollers { get; }

        public IReadOnlyList<ChainSketchCircle> Pins { get; }

        /// <summary>True when the outlines of the outer links and of the inner links are closed loops.</summary>
        public bool IsClosed(double tolerance = 1e-6)
        {
            return new SingleToothSketch(1, 1.0, PartName, OuterLinks, null).IsClosed(tolerance)
                   && new SingleToothSketch(1, 1.0, PartName, InnerLinks, null).IsClosed(tolerance);
        }
    }

    /// <summary>Builds the <see cref="ChainSketch"/> of a <see cref="RollerChain"/>.</summary>
    public static class ChainSketchBuilder
    {
        private const int Decimals = 7;

        /// <summary>The part name, for example <c>RollerChain_08B_L66</c> (chain, number of links).</summary>
        public static string PartName(RollerChain chain)
        {
            if (chain == null) throw new ArgumentNullException(nameof(chain));
            return string.Format(CultureInfo.InvariantCulture, "RollerChain_{0}_L{1}", chain.Profile.Name, chain.LinkCount);
        }

        /// <summary>
        /// The plan of a chain. Throws <see cref="InvalidOperationException"/> when the chain cannot be drawn (see <see cref="RollerChain.Validate"/>).
        /// <paramref name="driverPhaseRad"/> is the phase of the driver sprocket's mount: the rollers are placed in its tooth gaps at that phase
        /// (see <see cref="ChainRegistration"/>), so the chain is not rotationally symmetric.
        /// </summary>
        public static ChainSketch Build(RollerChain chain, double driverPhaseRad = 0.0)
        {
            if (chain == null) throw new ArgumentNullException(nameof(chain));
            chain.Validate();

            var layout = RollerChainLayout.Build(chain, driverPhaseRad);
            var profile = chain.Profile;
            var rollers = layout.Rollers;
            var count = rollers.Count;

            var outer = new List<ToothSketchFigure>();
            var inner = new List<ToothSketchFigure>();
            var rollerCircles = new List<ChainSketchCircle>(count);
            var pinCircles = new List<ChainSketchCircle>(count);

            for (var k = 0; k < count; k++)
            {
                rollerCircles.Add(new ChainSketchCircle(Q(rollers[k].X), Q(rollers[k].Y), Q(profile.RollerDiameter / 2.0)));
                pinCircles.Add(new ChainSketchCircle(Q(rollers[k].X), Q(rollers[k].Y), Q(profile.PinDiameter / 2.0)));
                AddStadium(k % 2 == 0 ? inner : outer, k, rollers[k], rollers[(k + 1) % count], profile.PlateHeight);
            }

            var innerLinkHalf = profile.InnerWidth / 2.0 + profile.InnerPlateThickness;
            return new ChainSketch(PartName(chain), count, Q(profile.OverallWidth), Q(2.0 * innerLinkHalf), Q(profile.InnerWidth), outer, inner, rollerCircles, pinCircles);
        }

        /// <summary>
        /// The four figures of the plate of link <paramref name="link"/>, anticlockwise: a line along the right side, an arc round the far pin, a
        /// line back along the left side and an arc round the near pin. The corner points are rounded once and shared, so the loop closes.
        /// </summary>
        private static void AddStadium(List<ToothSketchFigure> figures, int link, GearPoint from, GearPoint to, double height)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 1e-9) throw new InvalidOperationException("Two rollers of the chain are at the same place.");
            var nx = -dy / length;
            var ny = dx / length;
            var half = height / 2.0;

            var right0 = Q(from.X - nx * half, from.Y - ny * half);
            var right1 = Q(to.X - nx * half, to.Y - ny * half);
            var left1 = Q(to.X + nx * half, to.Y + ny * half);
            var left0 = Q(from.X + nx * half, from.Y + ny * half);
            var centreFrom = Q(from.X, from.Y);
            var centreTo = Q(to.X, to.Y);
            var name = "Link" + link;

            figures.Add(new ToothSketchFigure(ToothSketchFigureKind.Line, name + "Right", right0.X, right0.Y, right1.X, right1.Y));
            figures.Add(new ToothSketchFigure(ToothSketchFigureKind.Arc, name + "Far", right1.X, right1.Y, left1.X, left1.Y, centreTo.X, centreTo.Y));
            figures.Add(new ToothSketchFigure(ToothSketchFigureKind.Line, name + "Left", left1.X, left1.Y, left0.X, left0.Y));
            figures.Add(new ToothSketchFigure(ToothSketchFigureKind.Arc, name + "Near", left0.X, left0.Y, right0.X, right0.Y, centreFrom.X, centreFrom.Y));
        }

        private static GearPoint Q(double x, double y) => new GearPoint(Q(x), Q(y));

        /// <summary>Rounds a length in millimetres to 7 decimal places and converts it to Alibre units.</summary>
        private static double Q(double millimetres)
        {
            return Math.Round(millimetres, Decimals) * SingleToothSketchBuilder.MmToAlibreUnits;
        }
    }
}
