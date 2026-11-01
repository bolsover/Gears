using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>The kinds of 2D sketch figure that make up a single-tooth sketch.</summary>
    public enum ToothSketchFigureKind
    {
        Line,

        /// <summary>A circular arc about <see cref="ToothSketchFigure.CentreX"/>, <see cref="ToothSketchFigure.CentreY"/>, drawn anticlockwise from start to end.</summary>
        Arc,

        /// <summary>A B-spline through <see cref="ToothSketchFigure.Points"/>.</summary>
        Spline
    }

    /// <summary>One sketch figure. All coordinates are in Alibre sketch units (centimetres).</summary>
    public sealed class ToothSketchFigure
    {
        public ToothSketchFigure(ToothSketchFigureKind kind, string name, double startX, double startY, double endX, double endY,
            double centreX = 0.0, double centreY = 0.0, IReadOnlyList<GearPoint> points = null)
        {
            Kind = kind;
            Name = name;
            StartX = startX;
            StartY = startY;
            EndX = endX;
            EndY = endY;
            CentreX = centreX;
            CentreY = centreY;
            Points = points;
        }

        public ToothSketchFigureKind Kind { get; }
        public string Name { get; }
        public double StartX { get; }
        public double StartY { get; }
        public double EndX { get; }
        public double EndY { get; }
        public double CentreX { get; }
        public double CentreY { get; }

        /// <summary>The interpolation points of a spline (first and last are the start and end); null for other kinds.</summary>
        public IReadOnlyList<GearPoint> Points { get; }
    }

    /// <summary>A reference circle drawn in the sketch to document a diameter; it is not part of the profile.</summary>
    public sealed class ToothSketchReferenceCircle
    {
        public ToothSketchReferenceCircle(string name, double radius)
        {
            Name = name;
            Radius = radius;
        }

        public string Name { get; }

        /// <summary>Radius in Alibre sketch units (centimetres).</summary>
        public double Radius { get; }
    }

    /// <summary>
    /// The plan for exporting one involute spur gear to Alibre: the closed sketch of a single tooth, the extrusion depth and the
    /// circular pattern. The sketch is a wedge: the tooth profile (root to root, one pitch wide) closed by two radial lines to the
    /// gear axis, so that the patterned copies tile into a solid gear. All lengths are in Alibre units (centimetres).
    /// </summary>
    public sealed class SingleToothSketch
    {
        public SingleToothSketch(int teeth, double extrusionDepth, string partName, IReadOnlyList<ToothSketchFigure> figures,
            IReadOnlyList<ToothSketchReferenceCircle> referenceCircles)
        {
            Teeth = teeth;
            ExtrusionDepth = extrusionDepth;
            PartName = partName;
            Figures = figures;
            ReferenceCircles = referenceCircles;
        }

        public int Teeth { get; }

        /// <summary>Angle between pattern instances, in radians.</summary>
        public double PatternAngle => 2.0 * Math.PI / Teeth;

        /// <summary>Thickness of the extruded tooth (the face width) in centimetres.</summary>
        public double ExtrusionDepth { get; }

        public string PartName { get; }

        /// <summary>The figures of the closed profile, in drawing order.</summary>
        public IReadOnlyList<ToothSketchFigure> Figures { get; }

        public IReadOnlyList<ToothSketchReferenceCircle> ReferenceCircles { get; }

        /// <summary>
        /// True when the figures form closed loops: every figure end point meets exactly one other end point within
        /// <paramref name="tolerance"/> (centimetres).
        /// </summary>
        public bool IsClosed(double tolerance = 1e-6)
        {
            var ends = new List<double[]>();
            foreach (var figure in Figures)
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
    }
}
