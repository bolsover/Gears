using System;
using System.Collections.Generic;
using AlibreX;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>Draws the figures of a tooth, belt or bevel sketch plan into an Alibre sketch.</summary>
    internal static class AlibreSketchDrawer
    {
        /// <summary>
        /// Where the developed back cone (pointC as origin, x along the back cone line towards pointA, y across the tooth) lies in the
        /// sketch on the back cone plane. Alibre decides the origin and axes of a new plane's sketch, so they are found by mapping three
        /// world points that lie on the plane into the sketch.
        /// </summary>
        internal sealed class SketchMap
        {
            private double _originX;
            private double _originY;
            private double _ux;
            private double _uy;
            private double _vx;
            private double _vy;

            public bool Mirrored => _ux * _vy - _uy * _vx < 0.0;

            public double X(double u, double v) => _originX + u * _ux + v * _vx;

            public double Y(double u, double v) => _originY + u * _uy + v * _vy;

            public static SketchMap Create(IADPartSession session, IADSketch sketch, BevelGearSketch plan)
            {
                return CreateFromPoints(session, sketch, new[] { plan.BackConeApexX, 0.0, 0.0 }, new[] { 0.0, plan.PointAY, 0.0 },
                    new[] { plan.BackConeApexX, 0.0, 1.0 }, plan.BackConeDistance);
            }

            /// <summary>
            /// The map of a sketch from three world points on its plane: the origin, a point along the sketch x axis at <paramref name="alongLength"/>
            /// from the origin (in the units of the sketch figures) and a point along the sketch y axis.
            /// </summary>
            public static SketchMap CreateFromPoints(IADPartSession session, IADSketch sketch, double[] origin, double[] along, double[] across, double alongLength)
            {
                var factory = session.GeometryFactory;
                MapPoint(sketch, factory, origin[0], origin[1], origin[2], out var cx, out var cy);
                MapPoint(sketch, factory, along[0], along[1], along[2], out var ax, out var ay);
                MapPoint(sketch, factory, across[0], across[1], across[2], out var zx, out var zy);

                var uLength = Math.Sqrt((ax - cx) * (ax - cx) + (ay - cy) * (ay - cy));
                var vLength = Math.Sqrt((zx - cx) * (zx - cx) + (zy - cy) * (zy - cy));
                if (!(uLength > 1e-9) || !(vLength > 1e-9)) throw new InvalidOperationException("the back cone line has no length in the sketch");

                // The sketch is in the same units as the world, so the scale is 1; it is measured anyway.
                var scale = uLength / alongLength;
                var u = new[] { (ax - cx) / uLength, (ay - cy) / uLength };
                var v = new[] { (zx - cx) / vLength, (zy - cy) / vLength };
                if (Math.Abs(u[0] * v[0] + u[1] * v[1]) > 1e-6) throw new InvalidOperationException("the sketch axes of the back cone plane are not perpendicular");

                return new SketchMap { _originX = cx, _originY = cy, _ux = u[0] * scale, _uy = u[1] * scale, _vx = v[0] * scale, _vy = v[1] * scale };
            }

            private static void MapPoint(IADSketch sketch, IADGeometryFactory factory, double x, double y, double z, out double sx, out double sy)
            {
                sketch.MapFromWorldToSketch(factory.CreatePoint(x, y, z), out var a, out var b);
                sx = a;
                sy = b;
            }
        }

        /// <summary>Draws figures given in the developed back cone into a sketch through <paramref name="map"/>; returns whether the sketch is closed.</summary>
        internal static bool DrawMappedFigures(IADSketch sketch, IEnumerable<ToothSketchFigure> profile, SketchMap map)
        {
            sketch.BeginChange();
            try
            {
                var figures = sketch.Figures;
                foreach (var figure in profile)
                {
                    switch (figure.Kind)
                    {
                        case ToothSketchFigureKind.Line:
                            figures.AddLine(map.X(figure.StartX, figure.StartY), map.Y(figure.StartX, figure.StartY),
                                map.X(figure.EndX, figure.EndY), map.Y(figure.EndX, figure.EndY));
                            break;
                        case ToothSketchFigureKind.Arc:
                        {
                            // A mirrored sketch turns an anticlockwise arc into a clockwise one, so start and end change places.
                            var startX = map.Mirrored ? figure.EndX : figure.StartX;
                            var startY = map.Mirrored ? figure.EndY : figure.StartY;
                            var endX = map.Mirrored ? figure.StartX : figure.EndX;
                            var endY = map.Mirrored ? figure.StartY : figure.EndY;
                            figures.AddCircularArcByCenterStartEnd(map.X(figure.CentreX, figure.CentreY), map.Y(figure.CentreX, figure.CentreY),
                                map.X(startX, startY), map.Y(startX, startY), map.X(endX, endY), map.Y(endX, endY));
                            break;
                        }
                        default:
                            Array interpolation = new double[figure.Points.Count * 2];
                            var index = 0;
                            foreach (var point in figure.Points)
                            {
                                interpolation.SetValue(map.X(point.X, point.Y), index++);
                                interpolation.SetValue(map.Y(point.X, point.Y), index++);
                            }

                            figures.AddBsplineByInterpolation(ref interpolation);
                            break;
                    }
                }
            }
            finally
            {
                sketch.EndChange();
            }

            return sketch.IsClosed;
        }

        /// <summary>
        /// Draws the closed profile and the reference circles (when given); returns whether Alibre reports the sketch as closed.
        /// <paramref name="ySign"/> mirrors the sketch y coordinates (-1) for a plane whose sketch y axis points the other way.
        /// </summary>
        internal static bool DrawFigures(IADSketch sketch, IEnumerable<ToothSketchFigure> profile, IEnumerable<ToothSketchReferenceCircle> referenceCircles,
            double ySign = 1.0, double centreHoleRadius = 0.0)
        {
            sketch.BeginChange();
            try
            {
                var figures = sketch.Figures;
                foreach (var figure in profile)
                {
                    switch (figure.Kind)
                    {
                        case ToothSketchFigureKind.Line:
                            figures.AddLine(figure.StartX, ySign * figure.StartY, figure.EndX, ySign * figure.EndY);
                            break;
                        case ToothSketchFigureKind.Arc:
                            figures.AddCircularArcByCenterStartEnd(figure.CentreX, figure.CentreY, figure.StartX, figure.StartY, figure.EndX, figure.EndY);
                            break;
                        default:
                            Array interpolation = new double[figure.Points.Count * 2];
                            var index = 0;
                            foreach (var point in figure.Points)
                            {
                                interpolation.SetValue(point.X, index++);
                                interpolation.SetValue(point.Y, index++);
                            }

                            figures.AddBsplineByInterpolation(ref interpolation);
                            break;
                    }
                }

                if (referenceCircles != null)
                {
                    foreach (var circle in referenceCircles)
                    {
                        figures.AddCircle(0.0, 0.0, circle.Radius).IsReference = true;
                    }
                }

                // A hole through the centre: a closed circle inside the outline, which Alibre extrudes as a hole.
                if (centreHoleRadius > 0.0) figures.AddCircle(0.0, 0.0, centreHoleRadius);
            }
            finally
            {
                sketch.EndChange();
            }

            return sketch.IsClosed;
        }
    }
}
