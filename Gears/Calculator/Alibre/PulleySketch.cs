using System;
using System.Collections.Generic;
using System.Globalization;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>
    /// The plan for exporting one HTD pulley to Alibre. <see cref="Tooth"/> is the closed sketch of one groove cell: a wedge one pitch
    /// wide, centred on the groove, bounded by two radial lines to the axis, two arcs of the outside diameter and the groove (a
    /// circular-arc bottom, two straight flanks and two rounded edges). It is extruded to the width and patterned about the Z axis, and
    /// the copies tile into the solid pulley. The flanges, when there are any, are two rectangles in the plane that holds the Z axis
    /// that are revolved about it. All lengths are in Alibre units (centimetres); the body runs from z = 0 to the width.
    /// </summary>
    public sealed class PulleySketch
    {
        public PulleySketch(SingleToothSketch tooth, IReadOnlyList<IReadOnlyList<ToothSketchFigure>> flanges)
        {
            Tooth = tooth ?? throw new ArgumentNullException(nameof(tooth));
            Flanges = flanges ?? new List<IReadOnlyList<ToothSketchFigure>>();
        }

        /// <summary>The wedge of one groove cell, with the number of grooves, the width and the part name.</summary>
        public SingleToothSketch Tooth { get; }

        /// <summary>
        /// The closed rectangle of each flange (none for a pulley without flanges), in the axial plane: sketch x is the radius and sketch y
        /// is the position along the axis. Each is revolved about the axis.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<ToothSketchFigure>> Flanges { get; }

        public bool HasFlanges => Flanges.Count > 0;

        /// <summary>True when the flange rectangles are closed loops.</summary>
        public bool IsFlangeClosed(double tolerance = 1e-6)
        {
            foreach (var flange in Flanges)
            {
                var closed = new SingleToothSketch(1, 1.0, "flange", flange, null);
                if (!closed.IsClosed(tolerance)) return false;
            }

            return true;
        }
    }

    /// <summary>Builds the <see cref="PulleySketch"/> of an <see cref="HtdPulleyComponent"/>.</summary>
    public static class PulleySketchBuilder
    {
        private const int Decimals = 7;

        /// <summary>The part name, for example <c>HtdPulley_5M_Z20_B15</c>.</summary>
        public static string PartName(HtdPulleyComponent pulley)
        {
            if (pulley == null) throw new ArgumentNullException(nameof(pulley));
            return string.Format(CultureInfo.InvariantCulture, "HtdPulley_{0}_Z{1}_B{2:0.##}", pulley.Profile.Name, pulley.Teeth, pulley.FaceWidth);
        }

        public static PulleySketch Build(HtdPulleyComponent pulley)
        {
            if (pulley == null) throw new ArgumentNullException(nameof(pulley));
            pulley.Validate();

            var profile = pulley.Profile;
            var outsideRadius = pulley.OutsideDiameter / 2.0;
            var rootRadius = outsideRadius - profile.ToothHeight;
            var half = HtdPulleyComponent.HalfGroove(profile);
            var step = 2.0 * Math.PI / pulley.Teeth;
            var halfAngle = half[half.Count - 1].X / outsideRadius;
            if (step - 2.0 * halfAngle <= 1e-4)
            {
                throw new InvalidOperationException(
                    $"HTD pulley '{pulley.Id}': {pulley.Teeth} grooves are too few for the {profile.Name} profile; the grooves would overlap.");
            }

            // half[0..BottomArcSteps] is the bottom arc, half[BottomArcSteps + 1..] the rounded edge; the flank joins the two.
            var flankStart = HtdPulleyComponent.BottomArcSteps;
            var edgeStart = flankStart + 1;

            GearPoint Map(double sign, GearPoint p)
            {
                var radius = rootRadius + p.Y;
                var angle = sign * p.X / radius;
                return new GearPoint(Q(radius * Math.Cos(angle)), Q(radius * Math.Sin(angle)));
            }

            GearPoint OnOutside(double angle) => new GearPoint(Q(outsideRadius * Math.Cos(angle)), Q(outsideRadius * Math.Sin(angle)));

            var origin = new GearPoint(0.0, 0.0);
            var leftRim = OnOutside(-step / 2.0);
            var rightRim = OnOutside(step / 2.0);
            var leftEnd = Map(-1.0, half[half.Count - 1]);
            var rightEnd = Map(1.0, half[half.Count - 1]);

            var leftEdge = new List<GearPoint>();
            for (var i = half.Count - 1; i >= edgeStart; i--) leftEdge.Add(Map(-1.0, half[i]));

            var bottom = new List<GearPoint>();
            for (var i = flankStart; i >= 1; i--) bottom.Add(Map(-1.0, half[i]));
            for (var i = 0; i <= flankStart; i++) bottom.Add(Map(1.0, half[i]));

            var rightEdge = new List<GearPoint>();
            for (var i = edgeStart; i < half.Count; i++) rightEdge.Add(Map(1.0, half[i]));

            var figures = new List<ToothSketchFigure>
            {
                Line("RadialLeft", origin, leftRim),
                new ToothSketchFigure(ToothSketchFigureKind.Arc, "RimLeft", leftRim.X, leftRim.Y, leftEnd.X, leftEnd.Y),
                Spline("EdgeLeft", leftEdge),
                Line("FlankLeft", leftEdge[leftEdge.Count - 1], bottom[0]),
                Spline("Bottom", bottom),
                Line("FlankRight", bottom[bottom.Count - 1], rightEdge[0]),
                Spline("EdgeRight", rightEdge),
                new ToothSketchFigure(ToothSketchFigureKind.Arc, "RimRight", rightEnd.X, rightEnd.Y, rightRim.X, rightRim.Y),
                Line("RadialRight", rightRim, origin)
            };

            var references = new List<ToothSketchReferenceCircle>
            {
                new ToothSketchReferenceCircle("PitchDiameter", Q(pulley.PitchDiameter / 2.0))
            };
            var tooth = new SingleToothSketch(pulley.Teeth, Q(pulley.FaceWidth), PartName(pulley), figures, references);

            var flanges = new List<IReadOnlyList<ToothSketchFigure>>();
            if (pulley.HasFlanges)
            {
                var flangeRadius = Q(pulley.FlangeDiameter / 2.0);
                flanges.Add(Rectangle(flangeRadius, Q(pulley.FaceWidth), Q(pulley.FaceWidth + pulley.FlangeThickness)));
                flanges.Add(Rectangle(flangeRadius, Q(-pulley.FlangeThickness), 0.0));
            }

            return new PulleySketch(tooth, flanges);
        }

        private static IReadOnlyList<ToothSketchFigure> Rectangle(double radius, double z0, double z1)
        {
            var a = new GearPoint(0.0, z0);
            var b = new GearPoint(radius, z0);
            var c = new GearPoint(radius, z1);
            var d = new GearPoint(0.0, z1);
            return new List<ToothSketchFigure> { Line("FlangeBottom", a, b), Line("FlangeOuter", b, c), Line("FlangeTop", c, d), Line("FlangeAxis", d, a) };
        }

        private static ToothSketchFigure Line(string name, GearPoint from, GearPoint to)
        {
            return new ToothSketchFigure(ToothSketchFigureKind.Line, name, from.X, from.Y, to.X, to.Y);
        }

        private static ToothSketchFigure Spline(string name, List<GearPoint> points)
        {
            var first = points[0];
            var last = points[points.Count - 1];
            return new ToothSketchFigure(ToothSketchFigureKind.Spline, name, first.X, first.Y, last.X, last.Y, 0.0, 0.0, points);
        }

        /// <summary>Rounds a length in millimetres to 7 decimal places and converts it to Alibre units.</summary>
        private static double Q(double millimetres)
        {
            return Math.Round(millimetres, Decimals) * SingleToothSketchBuilder.MmToAlibreUnits;
        }
    }
}
