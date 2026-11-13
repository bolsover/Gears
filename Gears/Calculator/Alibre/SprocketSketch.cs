using System;
using System.Collections.Generic;
using System.Globalization;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>
    /// The plan for exporting one ISO 606 sprocket to Alibre. <see cref="Tooth"/> is the closed sketch of one tooth cell: a wedge one
    /// pitch wide, centred on a tooth gap, bounded by two radial lines to the axis, two arcs of the tip circle (none for a pointed tooth),
    /// the two tooth flank arcs and the roller seating arc. It is extruded about the sketch plane (mid-plane) to the width and patterned
    /// about the Z axis, and the copies tile into the solid sprocket. When there is a hub, <see cref="Hub"/> is the rectangle of the hub on
    /// the positive side, in the plane that holds the Z axis; it is revolved about the axis. When there is a bore, <see cref="BoreRadius"/>
    /// is the radius of a circle that is cut through the whole body. <see cref="Relief"/> is the closed sketch of the tooth side relief
    /// on the positive side, in the same plane as the hub; it is revolved about the axis as a cut and mirrored about the XY plane. All
    /// lengths are in Alibre units (centimetres); the toothed width runs from z = -width / 2 to z = width / 2.
    /// </summary>
    public sealed class SprocketSketch
    {
        public SprocketSketch(SingleToothSketch tooth, IReadOnlyList<ToothSketchFigure> hub, double boreRadius, double boreDepth,
            IReadOnlyList<ToothSketchFigure> relief = null)
        {
            Tooth = tooth ?? throw new ArgumentNullException(nameof(tooth));
            Hub = hub;
            BoreRadius = boreRadius;
            BoreDepth = boreDepth;
            Relief = relief;
        }

        /// <summary>The wedge of one tooth cell, with the number of teeth, the width and the part name.</summary>
        public SingleToothSketch Tooth { get; }

        /// <summary>
        /// The closed rectangle of the hub on the positive side (null for a sprocket without a hub), in the axial plane: sketch x is the
        /// radius and sketch y is the position along the axis. It is revolved about the axis.
        /// </summary>
        public IReadOnlyList<ToothSketchFigure> Hub { get; }

        public bool HasHub => Hub != null;

        /// <summary>
        /// The closed profile of the material removed by the tooth side relief on the positive side (null for none), in the axial plane:
        /// sketch x is the radius and sketch y is the position along the axis. An arc of the tooth radius r3 is tangent to the tooth side
        /// face and cuts it back by the radius width C at the tip circle; three lines close the profile outside the tip circle. It is
        /// revolved about the axis as a cut and the cut is mirrored about the mid-plane.
        /// </summary>
        public IReadOnlyList<ToothSketchFigure> Relief { get; }

        public bool HasRelief => Relief != null;

        /// <summary>True when the relief profile is a closed loop (or there is no relief).</summary>
        public bool IsReliefClosed(double tolerance = 1e-6)
        {
            if (Relief == null) return true;
            return new SingleToothSketch(1, 1.0, "relief", Relief, null).IsClosed(tolerance);
        }

        /// <summary>Radius of the bore (zero for none).</summary>
        public double BoreRadius { get; }

        /// <summary>Total depth of the through cut of the bore, centred on the mid-plane: more than the length of the body.</summary>
        public double BoreDepth { get; }

        public bool HasBore => BoreRadius > 0.0;

        /// <summary>True when the hub rectangle is a closed loop (or there is no hub).</summary>
        public bool IsHubClosed(double tolerance = 1e-6)
        {
            if (Hub == null) return true;
            return new SingleToothSketch(1, 1.0, "hub", Hub, null).IsClosed(tolerance);
        }
    }

    /// <summary>Builds the <see cref="SprocketSketch"/> of a <see cref="SprocketComponent"/>.</summary>
    public static class SprocketSketchBuilder
    {
        private const int Decimals = 7;

        /// <summary>The part name, for example <c>Sprocket_08B_Z19_B7.21</c>.</summary>
        public static string PartName(SprocketComponent sprocket)
        {
            if (sprocket == null) throw new ArgumentNullException(nameof(sprocket));
            return string.Format(CultureInfo.InvariantCulture, "Sprocket_{0}_Z{1}_B{2:0.##}", sprocket.Profile.Name, sprocket.Teeth, sprocket.FaceWidth);
        }

        public static SprocketSketch Build(SprocketComponent sprocket)
        {
            if (sprocket == null) throw new ArgumentNullException(nameof(sprocket));
            sprocket.Validate();

            var profile = sprocket.Profile;
            var teeth = sprocket.Teeth;
            var form = sprocket.ToothForm;
            var half = SprocketComponent.HalfGap(profile, teeth, form, out var pointed);
            var step = 2.0 * Math.PI / teeth;

            var seatEnd = half[SprocketComponent.SeatArcSteps];
            var tip = half[half.Count - 1];
            var flankCentre = SprocketComponent.FlankCentre(profile, teeth, form);
            var seatCentreX = profile.PitchDiameter(teeth) / 2.0;
            var tipRadius = profile.TipDiameter(teeth, form) / 2.0;

            // Points of the cell in millimetres; the cell is centred on the +X axis, the left side is the mirror image of the right side.
            GearPoint P(double x, double y) => new GearPoint(Q(x), Q(y));
            GearPoint OnRadius(double radius, double angle) => P(radius * Math.Cos(angle), radius * Math.Sin(angle));

            var origin = new GearPoint(0.0, 0.0);
            var rightTip = P(tip.X, tip.Y);
            var leftTip = P(tip.X, -tip.Y);
            var rightSeatEnd = P(seatEnd.X, seatEnd.Y);
            var leftSeatEnd = P(seatEnd.X, -seatEnd.Y);
            var rightRim = pointed ? rightTip : OnRadius(tipRadius, step / 2.0);
            var leftRim = pointed ? leftTip : OnRadius(tipRadius, -step / 2.0);

            var figures = new List<ToothSketchFigure> { Line("RadialLeft", origin, leftRim) };
            if (!pointed)
            {
                figures.Add(Arc("TipLeft", 0.0, 0.0, leftRim, leftTip));
            }

            // The flank arcs are drawn anticlockwise: on the left from the seating arc out to the tip, on the right from the tip in to the seating arc.
            figures.Add(Arc("FlankLeft", flankCentre.X, -flankCentre.Y, leftSeatEnd, leftTip));
            figures.Add(Arc("Seat", seatCentreX, 0.0, rightSeatEnd, leftSeatEnd));
            figures.Add(Arc("FlankRight", flankCentre.X, flankCentre.Y, rightTip, rightSeatEnd));
            if (!pointed)
            {
                figures.Add(Arc("TipRight", 0.0, 0.0, rightTip, rightRim));
            }

            figures.Add(Line("RadialRight", rightRim, origin));

            var references = new List<ToothSketchReferenceCircle>
            {
                new ToothSketchReferenceCircle("PitchDiameter", Q(sprocket.PitchDiameter / 2.0))
            };
            var cell = new SingleToothSketch(teeth, Q(sprocket.FaceWidth), PartName(sprocket), figures, references);

            IReadOnlyList<ToothSketchFigure> hub = null;
            if (sprocket.HasHub)
            {
                var halfWidth = sprocket.FaceWidth / 2.0;
                hub = Rectangle(Q(sprocket.HubDiameter / 2.0), Q(halfWidth), Q(halfWidth + sprocket.HubLength));
            }

            var boreRadius = sprocket.BoreDiameter > 0.0 ? Q(sprocket.BoreDiameter / 2.0) : 0.0;
            var length = sprocket.FaceWidth + 2.0 * (sprocket.HasHub ? sprocket.HubLength : 0.0) + 2.0;
            return new SprocketSketch(cell, hub, boreRadius, Q(length), BuildRelief(sprocket, tipRadius));
        }

        /// <summary>
        /// The tooth side radius relief of the sprocket catalogue (tooth radius r3, radius width C), as the closed profile of the material
        /// to cut away on the positive side. The tooth side face is the plane z = width / 2. The relief is the arc of radius r3 that is tangent
        /// to that plane at radius R0 and has moved in by C where it reaches the tip radius Ra, so
        /// R0 = Ra - sqrt(2 r3 C - C^2). When R0 would be below the bottom of the roller seats (few teeth, short tooth form), the arc starts
        /// at the bottom radius instead and the cut-back at the tip circle is smaller than C. Null when the chain has no relief, or when it
        /// is wider than half the tooth.
        /// </summary>
        public static IReadOnlyList<ToothSketchFigure> BuildRelief(SprocketComponent sprocket, double tipRadius)
        {
            if (sprocket == null) throw new ArgumentNullException(nameof(sprocket));

            // The same relief as the 3D view of the sprocket draws.
            if (!sprocket.TrySideRelief(out var start)) return null;

            var profile = sprocket.Profile;
            var radius = profile.SideRadius;
            var halfWidth = sprocket.FaceWidth / 2.0;
            var tip = tipRadius;
            var width = sprocket.SideDrop(tip, start);

            var outer = tip + profile.Pitch / 10.0;
            GearPoint P(double r, double z) => new GearPoint(Q(r), Q(z));

            var tangent = P(start, halfWidth);
            var atTip = P(tip, halfWidth - width);
            var beyondTip = P(outer, halfWidth - width);
            var outerFace = P(outer, halfWidth);

            // The arc is anticlockwise about its centre (start, z - r3), from the tip end (the smaller angle) to the tangent point (90 degrees).
            return new List<ToothSketchFigure>
            {
                Arc("ReliefArc", start, halfWidth - radius, atTip, tangent),
                Line("ReliefTip", atTip, beyondTip),
                Line("ReliefOuter", beyondTip, outerFace),
                Line("ReliefFace", outerFace, tangent)
            };
        }

        private static IReadOnlyList<ToothSketchFigure> Rectangle(double radius, double z0, double z1)
        {
            var a = new GearPoint(0.0, z0);
            var b = new GearPoint(radius, z0);
            var c = new GearPoint(radius, z1);
            var d = new GearPoint(0.0, z1);
            return new List<ToothSketchFigure> { Line("HubBottom", a, b), Line("HubOuter", b, c), Line("HubTop", c, d), Line("HubAxis", d, a) };
        }

        private static ToothSketchFigure Line(string name, GearPoint from, GearPoint to)
        {
            return new ToothSketchFigure(ToothSketchFigureKind.Line, name, from.X, from.Y, to.X, to.Y);
        }

        /// <summary>An arc about a centre (in millimetres) from <paramref name="start"/> to <paramref name="end"/>, anticlockwise; the points are already in Alibre units.</summary>
        private static ToothSketchFigure Arc(string name, double centreX, double centreY, GearPoint start, GearPoint end)
        {
            return new ToothSketchFigure(ToothSketchFigureKind.Arc, name, start.X, start.Y, end.X, end.Y, Q(centreX), Q(centreY));
        }

        /// <summary>Rounds a length in millimetres to 7 decimal places and converts it to Alibre units.</summary>
        private static double Q(double millimetres)
        {
            return Math.Round(millimetres, Decimals) * SingleToothSketchBuilder.MmToAlibreUnits;
        }
    }
}
