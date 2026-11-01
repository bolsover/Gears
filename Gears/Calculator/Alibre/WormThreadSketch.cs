using System;
using System.Collections.Generic;
using System.Globalization;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>
    /// The plan for exporting a worm to Alibre. A worm is made the way a helical gear is made:
    /// 1. the end-face (transverse) section of ONE thread start is sketched on the XY plane as a closed wedge. The section of a worm
    ///    repeats every 360 / starts degrees, so the wedge is one start wide and is closed by two radial lines to the axis (a
    ///    single-start worm has a section that goes all the way round, which closes on itself without radial lines);
    /// 2. the wedge is swept along a helix about the Z axis with a helical boss (height = threaded length, pitch = lead), which turns
    ///    it through 2 pi per lead, so the single thread is formed;
    /// 3. for a worm with several starts the thread is circular-patterned about the Z axis, one instance per start.
    /// The wedge is built from the same radius function as the 3D model (<see cref="WormComponent.RadiusAt"/>), at the start of the
    /// part: the root arc, the spiral flank, the tip arc, the other flank and the root arc again. The part runs from Z = 0 to the
    /// threaded length. At Z = 0 the thread centre is at the angle it has in <see cref="WormComponent"/> at its end face
    /// (Z = -L/2), so the thread positions agree with the 3D model. All lengths are in Alibre units (centimetres).
    /// </summary>
    public sealed class WormThreadSketch
    {
        public WormThreadSketch(string partName, double length, IReadOnlyList<ToothSketchFigure> figures,
            IReadOnlyList<ToothSketchReferenceCircle> referenceCircles, int starts, SingleToothHelix helix)
        {
            PartName = partName;
            Length = length;
            Figures = figures;
            ReferenceCircles = referenceCircles;
            Starts = starts;
            Helix = helix;
        }

        public string PartName { get; }

        /// <summary>Length of the worm along Z, from Z = 0: the threaded length.</summary>
        public double Length { get; }

        /// <summary>The figures of the closed one-start section on the XY plane, in drawing order.</summary>
        public IReadOnlyList<ToothSketchFigure> Figures { get; }

        /// <summary>Reference circles (root, pitch and tip) drawn in the sketch to document the diameters.</summary>
        public IReadOnlyList<ToothSketchReferenceCircle> ReferenceCircles { get; }

        /// <summary>Number of thread starts; the thread is circular-patterned this many times.</summary>
        public int Starts { get; }

        /// <summary>The helix the section is swept along: height, lead (axial advance per turn), hand.</summary>
        public SingleToothHelix Helix { get; }

        /// <summary>Angle between the thread instances, in radians.</summary>
        public double PatternAngle => 2.0 * Math.PI / Starts;

        /// <summary>True when the figures form one closed loop.</summary>
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

    /// <summary>
    /// Builds the <see cref="WormThreadSketch"/> of a <see cref="WormComponent"/>, and names the Alibre part of a worm wheel.
    /// Lengths are rounded to 7 decimal places in millimetres and then converted to Alibre units (centimetres).
    /// </summary>
    public static class WormThreadSketchBuilder
    {
        private const int Decimals = 7;

        /// <summary>Points on each spiral flank.</summary>
        private const int FlankPoints = 16;

        /// <summary>Throws <see cref="InvalidOperationException"/> when the worm cannot be exported.</summary>
        public static WormThreadSketch Build(WormComponent worm)
        {
            if (worm == null) throw new ArgumentNullException(nameof(worm));
            worm.Validate();

            var pitch = worm.AxialPitch;
            var tanAlpha = Math.Tan(worm.AxialPressureAngleRad);

            // Half widths (along the axis) of the thread at the tip and at the root.
            var halfTip = pitch / 4.0 - worm.Module * tanAlpha;
            var halfRoot = Math.Min(pitch / 4.0 + (worm.Module + worm.Clearance) * tanAlpha, pitch / 2.0);
            if (halfTip <= 0)
            {
                throw new InvalidOperationException($"Worm '{worm.Id}': the pressure angle is too large for the thread tip to have a flat top.");
            }

            var starts = worm.Starts;
            var lead = worm.Lead;
            var hand = worm.IsRightHand ? 1.0 : -1.0;
            var turnPerMm = 2.0 * Math.PI / lead;

            // The section is taken at the end of the part (Z = 0 here, Z = -L/2 in the component). w is the axial distance from the
            // thread centre, and the angle grows with w: theta = thetaCentre + turnPerMm * w.
            var z0 = -worm.FaceWidth / 2.0;
            var thetaCentre = hand * turnPerMm * z0;

            GearPoint At(double w)
            {
                var theta = thetaCentre + turnPerMm * w;
                var r = worm.RadiusAt(theta, z0);
                return new GearPoint(Q(r * Math.Cos(theta)), Q(r * Math.Sin(theta)));
            }

            var half = pitch / 2.0;
            var b0 = At(-half);
            var b1 = At(-halfRoot);
            var b2 = At(-halfTip);
            var b3 = At(halfTip);
            var b4 = At(halfRoot);
            var b5 = At(half);

            var rootArcs = halfRoot < half - 1e-9;
            if (!rootArcs)
            {
                b1 = b0;
                b4 = b5;
            }

            // A single start goes all the way round: the end of the section is its start.
            if (starts == 1)
            {
                if (rootArcs) b5 = b0;
                else
                {
                    b4 = b0;
                    b5 = b0;
                }
            }

            var figures = new List<ToothSketchFigure>();

            if (starts > 1)
            {
                figures.Add(new ToothSketchFigure(ToothSketchFigureKind.Line, "Radial line 1", 0.0, 0.0, b0.X, b0.Y));
            }

            if (rootArcs) figures.Add(Arc("Root arc 1", b0, b1));
            figures.Add(Flank("Flank 1", b1, b2, At, -halfRoot, -halfTip));
            figures.Add(Arc("Tip arc", b2, b3));
            figures.Add(Flank("Flank 2", b3, b4, At, halfTip, halfRoot));
            if (rootArcs) figures.Add(Arc("Root arc 2", b4, b5));

            if (starts > 1)
            {
                figures.Add(new ToothSketchFigure(ToothSketchFigureKind.Line, "Radial line 2", b5.X, b5.Y, 0.0, 0.0));
            }

            var circles = new List<ToothSketchReferenceCircle>
            {
                new ToothSketchReferenceCircle("Root circle", Q(worm.RootRadius)),
                new ToothSketchReferenceCircle("Pitch circle", Q(worm.PitchRadius)),
                new ToothSketchReferenceCircle("Tip circle", Q(worm.TipRadius))
            };

            var length = Q(worm.FaceWidth);
            var helix = new SingleToothHelix(worm.LeadAngleDeg, worm.IsRightHand, length, Q(lead), Q(worm.PitchRadius));
            return new WormThreadSketch(PartName(worm), length, figures, circles, starts, helix);
        }

        /// <summary>The name of the Alibre part, for example <c>Worm_S2_M2_Q10_R</c>.</summary>
        public static string PartName(WormComponent worm)
        {
            return $"Worm_S{worm.Starts}_M{FormatNumber(worm.Module)}_Q{FormatNumber(worm.DiameterQuotient)}_{worm.HelicalDirection}";
        }

        /// <summary>The name of the Alibre part of a worm wheel, for example <c>WormWheel_Z30_M2_L5p71R</c> (L is the lead angle).</summary>
        public static string PartName(WormWheelComponent wheel)
        {
            return $"WormWheel_Z{wheel.Teeth}_M{FormatNumber(wheel.Module)}_L{FormatNumber(wheel.LeadAngleDeg)}{wheel.HelicalDirection}";
        }

        /// <summary>An arc about the Z axis, drawn anticlockwise from <paramref name="from"/> to <paramref name="to"/>.</summary>
        private static ToothSketchFigure Arc(string name, GearPoint from, GearPoint to)
        {
            return new ToothSketchFigure(ToothSketchFigureKind.Arc, name, from.X, from.Y, to.X, to.Y);
        }

        /// <summary>A spiral flank: a spline through points between w1 and w2 that starts and ends exactly on the given points.</summary>
        private static ToothSketchFigure Flank(string name, GearPoint from, GearPoint to, Func<double, GearPoint> at, double w1, double w2)
        {
            var points = new List<GearPoint>(FlankPoints) { from };
            for (var i = 1; i < FlankPoints - 1; i++)
            {
                points.Add(at(w1 + (w2 - w1) * i / (FlankPoints - 1)));
            }

            points.Add(to);
            return new ToothSketchFigure(ToothSketchFigureKind.Spline, name, from.X, from.Y, to.X, to.Y, 0.0, 0.0, points);
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture).Replace('.', 'p');
        }

        private static double Q(double millimetres)
        {
            return Math.Round(millimetres, Decimals) * SingleToothSketchBuilder.MmToAlibreUnits;
        }
    }
}
