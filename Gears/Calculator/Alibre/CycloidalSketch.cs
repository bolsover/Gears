using System;
using System.Collections.Generic;
using System.Globalization;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>
    /// The plan for exporting one cycloidal gear to Alibre: a closed sketch of the whole gear on the XY plane (for every tooth two tip arcs,
    /// two radial flanks and a root arc, as in the Alibre cycloidal gear add-on), an optional circle for the centre hole, and a reference
    /// circle for the pitch diameter. It is extruded mid-plane to the face width, so the XY plane is the mid-plane of the gear. All lengths
    /// are in Alibre units (centimetres).
    /// </summary>
    public sealed class CycloidalGearSketch
    {
        public CycloidalGearSketch(string partName, int teeth, double width, IReadOnlyList<ToothSketchFigure> figures,
            IReadOnlyList<ToothSketchReferenceCircle> referenceCircles, double centreHoleRadius)
        {
            PartName = partName;
            Teeth = teeth;
            Width = width;
            Figures = figures;
            ReferenceCircles = referenceCircles;
            CentreHoleRadius = centreHoleRadius;
        }

        public string PartName { get; }

        public int Teeth { get; }

        /// <summary>Thickness of the extruded gear (the face width) in centimetres.</summary>
        public double Width { get; }

        /// <summary>The figures of the closed outline, in drawing order.</summary>
        public IReadOnlyList<ToothSketchFigure> Figures { get; }

        public IReadOnlyList<ToothSketchReferenceCircle> ReferenceCircles { get; }

        /// <summary>Radius of the hole through the centre in centimetres; zero for none.</summary>
        public double CentreHoleRadius { get; }

        /// <summary>True when every end point of the outline meets exactly one other end point.</summary>
        public bool IsClosed(double tolerance = 1e-6)
        {
            return new SingleToothSketch(Teeth, Width, PartName, Figures, ReferenceCircles).IsClosed(tolerance);
        }
    }

    /// <summary>Builds the <see cref="CycloidalGearSketch"/> of a <see cref="CycloidalGearComponent"/>.</summary>
    public static class CycloidalSketchBuilder
    {
        private const int Decimals = 7;

        /// <summary>The part name, for example <c>CycloidalWheel_Z30_M2_B10</c>.</summary>
        public static string PartName(CycloidalGearComponent gear)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            return string.Format(CultureInfo.InvariantCulture, "Cycloidal{0}_Z{1}_M{2:0.##}_B{3:0.##}", gear.Role, gear.Teeth, gear.Module, gear.FaceWidth);
        }

        public static CycloidalGearSketch Build(CycloidalGearComponent gear)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));

            var outline = CycloidalProfile.Build(gear);
            var figures = new List<ToothSketchFigure>(outline.Pieces.Count);
            foreach (var piece in outline.Pieces)
            {
                var start = Q(piece.Start);
                var end = Q(piece.End);
                if (piece.Kind == CycloidalPieceKind.Line)
                {
                    figures.Add(new ToothSketchFigure(ToothSketchFigureKind.Line, piece.Name, start.X, start.Y, end.X, end.Y));
                }
                else
                {
                    var centre = Q(piece.Centre);
                    figures.Add(new ToothSketchFigure(ToothSketchFigureKind.Arc, piece.Name, start.X, start.Y, end.X, end.Y, centre.X, centre.Y));
                }
            }

            var references = new List<ToothSketchReferenceCircle>
            {
                new ToothSketchReferenceCircle("PitchDiameter", Q(gear.PitchDiameter / 2.0))
            };
            return new CycloidalGearSketch(PartName(gear), gear.Teeth, Q(gear.FaceWidth), figures, references, Q(gear.CentreHole / 2.0));
        }

        private static GearPoint Q(GearPoint point)
        {
            return new GearPoint(Q(point.X), Q(point.Y));
        }

        /// <summary>Rounds a length in millimetres to 7 decimal places and converts it to Alibre units.</summary>
        private static double Q(double millimetres)
        {
            return Math.Round(millimetres, Decimals) * SingleToothSketchBuilder.MmToAlibreUnits;
        }
    }
}
