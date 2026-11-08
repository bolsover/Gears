using System;
using System.Collections.Generic;
using System.Globalization;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>
    /// The plan for exporting one timing belt to Alibre: a closed ring sketch on the XY plane, made of an outer and an inner loop. The
    /// outer loop (the back of the belt) is two straight runs and an arc round each pulley. The inner loop has the same path at the outside
    /// diameter of the pulleys with the HTD teeth on it (see <see cref="BeltToothPath"/>): each tooth is three splines and two lines, and
    /// the lands between the teeth are arcs or lines. It is extruded mid-plane to the belt width, so the
    /// XY plane is the mid-plane of the belt. The driver pulley's axis is the origin and the driven pulley lies at the centre distance
    /// along the line of centres, as in the 3D view. All lengths are in Alibre units (centimetres).
    /// </summary>
    public sealed class BeltSketch
    {
        public BeltSketch(string partName, double width, IReadOnlyList<ToothSketchFigure> outerLoop, IReadOnlyList<ToothSketchFigure> innerLoop)
        {
            PartName = partName;
            Width = width;
            OuterLoop = outerLoop ?? throw new ArgumentNullException(nameof(outerLoop));
            InnerLoop = innerLoop ?? throw new ArgumentNullException(nameof(innerLoop));
        }

        public string PartName { get; }

        /// <summary>Width of the belt, which is the extrusion depth, in centimetres.</summary>
        public double Width { get; }

        /// <summary>The loop on the back of the belt (the larger one), anticlockwise.</summary>
        public IReadOnlyList<ToothSketchFigure> OuterLoop { get; }

        /// <summary>The loop on the pulley side of the belt (the smaller one), anticlockwise.</summary>
        public IReadOnlyList<ToothSketchFigure> InnerLoop { get; }

        /// <summary>Both loops, outer first, in drawing order.</summary>
        public IReadOnlyList<ToothSketchFigure> Figures
        {
            get
            {
                var all = new List<ToothSketchFigure>(OuterLoop);
                all.AddRange(InnerLoop);
                return all;
            }
        }

        /// <summary>True when each loop is closed on its own.</summary>
        public bool IsClosed(double tolerance = 1e-6)
        {
            return new SingleToothSketch(1, Width, PartName, OuterLoop, null).IsClosed(tolerance)
                   && new SingleToothSketch(1, Width, PartName, InnerLoop, null).IsClosed(tolerance);
        }
    }

    /// <summary>Builds the <see cref="BeltSketch"/> of a <see cref="TimingBelt"/>.</summary>
    public static class BeltSketchBuilder
    {
        private const int Decimals = 7;

        /// <summary>The part name, for example <c>HtdBelt_5M_T98_W15</c> (series, number of belt teeth, width).</summary>
        public static string PartName(TimingBelt belt)
        {
            if (belt == null) throw new ArgumentNullException(nameof(belt));
            return string.Format(CultureInfo.InvariantCulture, "HtdBelt_{0}_T{1}_W{2:0.##}", belt.Profile.Name, belt.BeltTeeth, belt.Width);
        }

        /// <summary>
        /// The plan of a belt. Throws <see cref="InvalidOperationException"/> when the belt cannot be drawn (see
        /// <see cref="TimingBelt.Validate"/>). <paramref name="driverPhaseRad"/> is the phase of the driver pulley's mount: the teeth are
        /// placed in the grooves of the driver at that phase (see <see cref="BeltRegistration"/>), so the belt is not rotationally symmetric.
        /// </summary>
        public static BeltSketch Build(TimingBelt belt, double driverPhaseRad = 0.0)
        {
            if (belt == null) throw new ArgumentNullException(nameof(belt));
            belt.Validate();

            var inner = ToothedLoop(BeltToothPath.Build(belt, driverPhaseRad));
            var outer = Loop(belt, -belt.Profile.PitchLineDifferential + belt.BackThickness, "Outer");
            return new BeltSketch(PartName(belt), Q(belt.Width), outer, inner);
        }

        /// <summary>The inner loop with its teeth as sketch figures. Neighbouring figures share points, which are rounded alike so the loop closes.</summary>
        private static IReadOnlyList<ToothSketchFigure> ToothedLoop(BeltToothedLoop loop)
        {
            var figures = new List<ToothSketchFigure>(loop.InnerPieces.Count);
            foreach (var piece in loop.InnerPieces)
            {
                var start = Q(piece.Start);
                var end = Q(piece.End);
                switch (piece.Kind)
                {
                    case BeltPieceKind.Line:
                        figures.Add(new ToothSketchFigure(ToothSketchFigureKind.Line, piece.Name, start.X, start.Y, end.X, end.Y));
                        break;
                    case BeltPieceKind.Arc:
                    {
                        var centre = Q(piece.Centre);
                        figures.Add(new ToothSketchFigure(ToothSketchFigureKind.Arc, piece.Name, start.X, start.Y, end.X, end.Y, centre.X, centre.Y));
                        break;
                    }
                    default:
                    {
                        var points = new List<GearPoint>(piece.Points.Count);
                        foreach (var point in piece.Points) points.Add(Q(point));
                        figures.Add(new ToothSketchFigure(ToothSketchFigureKind.Spline, piece.Name, start.X, start.Y, end.X, end.Y, 0.0, 0.0, points));
                        break;
                    }
                }
            }

            return figures;
        }

        private static GearPoint Q(GearPoint point)
        {
            return new GearPoint(Q(point.X), Q(point.Y));
        }

        /// <summary>
        /// One loop at an offset from the pitch line (positive away from the pulleys), anticlockwise: the arc round the driven pulley from
        /// the lower to the upper tangent point, the upper run, the arc round the driver, and the lower run. The tangent points are those
        /// of <see cref="TimingBeltMeshBuilder.BuildLoop"/>.
        /// </summary>
        private static IReadOnlyList<ToothSketchFigure> Loop(TimingBelt belt, double offset, string prefix)
        {
            var radiusA = belt.Driver.PitchDiameter / 2.0;
            var radiusB = belt.Driven.PitchDiameter / 2.0;
            var distance = belt.CentreDistance;
            var theta = belt.CentreLineAngleDeg * Math.PI / 180.0;

            var nx = (radiusA - radiusB) / distance;
            var ny = Math.Sqrt(Math.Max(0.0, 1.0 - nx * nx));
            var upper = Math.Atan2(ny, nx) + theta;
            var lower = Math.Atan2(-ny, nx) + theta;

            var centreBx = distance * Math.Cos(theta);
            var centreBy = distance * Math.Sin(theta);

            GearPoint OnDriven(double angle) => new GearPoint(Q(centreBx + (radiusB + offset) * Math.Cos(angle)), Q(centreBy + (radiusB + offset) * Math.Sin(angle)));
            GearPoint OnDriver(double angle) => new GearPoint(Q((radiusA + offset) * Math.Cos(angle)), Q((radiusA + offset) * Math.Sin(angle)));

            var drivenLower = OnDriven(lower);
            var drivenUpper = OnDriven(upper);
            var driverUpper = OnDriver(upper);
            var driverLower = OnDriver(lower);
            var centreB = new GearPoint(Q(centreBx), Q(centreBy));

            return new List<ToothSketchFigure>
            {
                new ToothSketchFigure(ToothSketchFigureKind.Arc, prefix + "ArcDriven", drivenLower.X, drivenLower.Y, drivenUpper.X, drivenUpper.Y, centreB.X, centreB.Y),
                new ToothSketchFigure(ToothSketchFigureKind.Line, prefix + "RunUpper", drivenUpper.X, drivenUpper.Y, driverUpper.X, driverUpper.Y),
                new ToothSketchFigure(ToothSketchFigureKind.Arc, prefix + "ArcDriver", driverUpper.X, driverUpper.Y, driverLower.X, driverLower.Y, 0.0, 0.0),
                new ToothSketchFigure(ToothSketchFigureKind.Line, prefix + "RunLower", driverLower.X, driverLower.Y, drivenLower.X, drivenLower.Y)
            };
        }

        /// <summary>Rounds a length in millimetres to 7 decimal places and converts it to Alibre units.</summary>
        private static double Q(double millimetres)
        {
            return Math.Round(millimetres, Decimals) * SingleToothSketchBuilder.MmToAlibreUnits;
        }
    }
}
