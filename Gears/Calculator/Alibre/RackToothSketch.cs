using System;
using System.Collections.Generic;
using System.Globalization;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>
    /// The plan for exporting a rack to Alibre: the closed sketch of a single tooth cell (one pitch wide, with the tooth in the middle
    /// and the rack body below it), how the tooth is formed (an extrusion, or for a helical rack a rigid sweep along a slanted path
    /// that shears the section) and the linear pattern along the rack. The sketch is the end-face (transverse) section on the XY plane:
    /// the rack length runs along X and the tooth tips point towards +Y. The cell spans X = 0 to the pitch, so the patterned rack runs
    /// from X = 0 and starts and ends in the middle of a tooth space, like <see cref="RackComponent.BuildOutline"/>.
    /// All lengths are in Alibre units (centimetres).
    /// </summary>
    public sealed class RackToothSketch
    {
        public RackToothSketch(int teeth, double pitch, double extrusionDepth, string partName, IReadOnlyList<ToothSketchFigure> figures,
            double helixAngleDeg = 0.0, bool rightHand = true, double shearPerDepth = 0.0)
        {
            Teeth = teeth;
            Pitch = pitch;
            ExtrusionDepth = extrusionDepth;
            PartName = partName;
            Figures = figures;
            HelixAngleDeg = helixAngleDeg;
            RightHand = rightHand;
            ShearPerDepth = shearPerDepth;
        }

        /// <summary>Number of teeth, which is the number of instances of the linear pattern.</summary>
        public int Teeth { get; }

        /// <summary>Distance between pattern instances along X: the transverse pitch.</summary>
        public double Pitch { get; }

        /// <summary>Thickness of the rack (the face width) in centimetres.</summary>
        public double ExtrusionDepth { get; }

        public string PartName { get; }

        /// <summary>The figures of the closed tooth cell, in drawing order.</summary>
        public IReadOnlyList<ToothSketchFigure> Figures { get; }

        /// <summary>Helix angle in degrees; zero for a straight-toothed rack.</summary>
        public double HelixAngleDeg { get; }

        /// <summary>Hand of the rack teeth (a right-hand rack meshes with a left-hand pinion).</summary>
        public bool RightHand { get; }

        /// <summary>Sideways shift of the tooth section along X per unit of depth (Z); tan(beta) with the sign of the hand.</summary>
        public double ShearPerDepth { get; }

        public bool IsHelical => HelixAngleDeg > 1e-6;

        /// <summary>Sideways shift (X, centimetres) of the far end of the tooth relative to the sketch plane.</summary>
        public double ShearTotal => ShearPerDepth * ExtrusionDepth;

        /// <summary>True when the figures form one closed loop: every end point meets exactly one other end point.</summary>
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
    /// Builds the <see cref="RackToothSketch"/> of a straight or helical rack. Lengths are rounded to 7 decimal places in millimetres
    /// and then converted to Alibre units (centimetres), as for the gear sketches.
    /// </summary>
    public static class RackToothSketchBuilder
    {
        private const int Decimals = 7;

        /// <summary>Two sketch points closer than this (centimetres) are the same point.</summary>
        private const double SamePoint = 1e-9;

        /// <summary>Throws <see cref="InvalidOperationException"/> when the rack cannot be exported.</summary>
        public static RackToothSketch Build(RackComponent rack)
        {
            if (rack == null) throw new ArgumentNullException(nameof(rack));
            rack.Validate();

            var pitch = rack.Pitch;
            var tanAlpha = Math.Tan(rack.TransversePressureAngleRad);
            var addendum = rack.Module;
            var dedendum = rack.Module + rack.Clearance;
            var thickness = pitch / 2.0 - rack.Backlash / 2.0;

            var halfTip = thickness / 2.0 - addendum * tanAlpha;
            var halfRoot = Math.Min(thickness / 2.0 + dedendum * tanAlpha, pitch / 2.0);
            if (halfTip <= 0)
            {
                throw new InvalidOperationException($"Rack '{rack.Id}': the pressure angle is too large for the tooth tip to have a flat top.");
            }

            var centre = pitch / 2.0;
            var bottom = -dedendum - rack.BodyHeight;

            var millimetres = new[]
            {
                new[] { 0.0, bottom },
                new[] { pitch, bottom },
                new[] { pitch, -dedendum },
                new[] { centre + halfRoot, -dedendum },
                new[] { centre + halfTip, addendum },
                new[] { centre - halfTip, addendum },
                new[] { centre - halfRoot, -dedendum },
                new[] { 0.0, -dedendum }
            };

            var points = new List<double[]>();
            foreach (var point in millimetres)
            {
                var scaled = new[] { Q(point[0]), Q(point[1]) };
                if (points.Count > 0 && Same(points[points.Count - 1], scaled)) continue;
                points.Add(scaled);
            }

            if (points.Count > 1 && Same(points[0], points[points.Count - 1])) points.RemoveAt(points.Count - 1);

            var figures = new List<ToothSketchFigure>();
            for (var i = 0; i < points.Count; i++)
            {
                var from = points[i];
                var to = points[(i + 1) % points.Count];
                figures.Add(new ToothSketchFigure(ToothSketchFigureKind.Line, "Line " + (i + 1), from[0], from[1], to[0], to[1]));
            }

            var depth = Q(rack.FaceWidth);
            return new RackToothSketch(rack.Teeth, Q(pitch), depth, PartName(rack), figures,
                rack.IsHelical ? rack.HelixAngleDeg : 0.0, rack.HelicalDirection == "R", rack.ShearPerZ);
        }

        /// <summary>The name of the Alibre part, for example <c>Rack_Z12_M2</c> or <c>HelicalRack_Z12_M2_B15R</c>.</summary>
        public static string PartName(RackComponent rack)
        {
            var name = $"{(rack.IsHelical ? "HelicalRack" : "Rack")}_Z{rack.Teeth}_M{FormatNumber(rack.Module)}";
            if (rack.IsHelical) name += $"_B{FormatNumber(rack.HelixAngleDeg)}{rack.HelicalDirection}";
            return name;
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture).Replace('.', 'p');
        }

        private static double Q(double millimetres)
        {
            return Math.Round(millimetres, Decimals) * SingleToothSketchBuilder.MmToAlibreUnits;
        }

        private static bool Same(double[] a, double[] b)
        {
            return Math.Abs(a[0] - b[0]) < SamePoint && Math.Abs(a[1] - b[1]) < SamePoint;
        }
    }
}
