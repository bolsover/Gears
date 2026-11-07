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
    /// How the tooth of a helical gear is formed: the sketch is swept along a helix about the gear axis (an Alibre helical boss)
    /// instead of being extruded straight. The sketch turns through <see cref="TurnAngle"/> over the face width. Lengths are in
    /// Alibre units (centimetres).
    /// </summary>
    public sealed class SingleToothHelix
    {
        public SingleToothHelix(double angleDeg, bool rightHand, double height, double lead, double pitchRadius)
        {
            AngleDeg = angleDeg;
            RightHand = rightHand;
            Height = height;
            Lead = lead;
            PitchRadius = pitchRadius;
        }

        /// <summary>Helix angle at the pitch circle, in degrees.</summary>
        public double AngleDeg { get; }

        /// <summary>True for a right-hand helix, false for a left-hand helix.</summary>
        public bool RightHand { get; }

        /// <summary>Axial length of the helix: the face width, or half of it for a herringbone gear (see <see cref="SingleToothSketch.IsHerringbone"/>).</summary>
        public double Height { get; }

        /// <summary>Axial advance for one full turn: 2 pi pitch radius / tan(helix angle).</summary>
        public double Lead { get; }

        /// <summary>Pitch radius of the gear (including profile shift).</summary>
        public double PitchRadius { get; }

        /// <summary>Number of turns of the helix over <see cref="Height"/>.</summary>
        public double Revolutions => Height / Lead;

        /// <summary>Angle the tooth section turns through over the face width, in radians.</summary>
        public double TurnAngle => 2.0 * Math.PI * Revolutions;
    }

    /// <summary>
    /// The plan for exporting one involute gear to Alibre: the closed sketch of a single tooth, how the tooth is formed (extrusion,
    /// or a helical boss for a helical gear) and the circular pattern. The sketch is a wedge one pitch wide. For an external gear it
    /// is the tooth profile closed by two radial lines to the gear axis. For an internal gear it is the inner tooth profile closed by
    /// two radial lines out to the rim and a rim arc. Either way the patterned copies tile into a solid gear. All lengths are in
    /// Alibre units (centimetres).
    /// </summary>
    public sealed class SingleToothSketch
    {
        public SingleToothSketch(int teeth, double extrusionDepth, string partName, IReadOnlyList<ToothSketchFigure> figures,
            IReadOnlyList<ToothSketchReferenceCircle> referenceCircles, bool isInternal = false, SingleToothHelix helix = null, bool isHerringbone = false)
        {
            Teeth = teeth;
            ExtrusionDepth = extrusionDepth;
            PartName = partName;
            Figures = figures;
            ReferenceCircles = referenceCircles;
            IsInternal = isInternal;
            Helix = helix;
            IsHerringbone = isHerringbone && helix != null;
        }

        /// <summary>
        /// True for a herringbone gear: the tooth is swept along the helix over half the face width, starting at the sketch plane, and then
        /// mirrored across the sketch plane, so the part runs from -FaceWidth / 2 to +FaceWidth / 2. Only a helical gear can be herringbone.
        /// </summary>
        public bool IsHerringbone { get; }

        /// <summary>True for an internal (ring) gear.</summary>
        public bool IsInternal { get; }

        /// <summary>The helix the tooth is swept along; null for a spur gear, which is extruded straight.</summary>
        public SingleToothHelix Helix { get; }

        public bool IsHelical => Helix != null;

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
