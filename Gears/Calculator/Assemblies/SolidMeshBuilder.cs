using System;
using System.Collections.Generic;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Builds flat-shaded solids from closed 2D outlines for the gear families that
    /// <see cref="GearMesh3DBuilder"/> does not cover: straight extrusions (racks) and tapered lofts (bevel gears).
    /// Every face has its own vertices, so the normals are exact face normals and all faces point outwards.
    /// </summary>
    public static class SolidMeshBuilder
    {
        /// <summary>
        /// Flattens outline segments into one closed loop. Consecutive points closer than <paramref name="minDistance"/>
        /// are merged, and a last point that repeats the first one is dropped.
        /// </summary>
        public static List<GearPoint> Flatten(IEnumerable<List<GearPoint>> polylines, double minDistance = 1e-6)
        {
            var result = new List<GearPoint>();
            if (polylines == null) return result;

            foreach (var polyline in polylines)
            {
                if (polyline == null) continue;
                foreach (var point in polyline)
                {
                    if (result.Count > 0 && Distance(result[result.Count - 1], point) < minDistance) continue;
                    result.Add(point);
                }
            }

            while (result.Count > 1 && Distance(result[0], result[result.Count - 1]) < minDistance)
            {
                result.RemoveAt(result.Count - 1);
            }

            return result;
        }

        /// <summary>Signed area of a closed loop (positive when it runs anticlockwise).</summary>
        public static double SignedArea(IReadOnlyList<GearPoint> loop)
        {
            var area = 0.0;
            for (var i = 0; i < loop.Count; i++)
            {
                var j = (i + 1) % loop.Count;
                area += loop[i].X * loop[j].Y - loop[j].X * loop[i].Y;
            }

            return area / 2.0;
        }

        /// <summary>
        /// Lofts a closed outline between two planes. The outline is scaled about the Z axis by
        /// <paramref name="scaleTop"/> in the plane z = <paramref name="zTop"/> and by <paramref name="scaleBottom"/> in the
        /// plane z = <paramref name="zBottom"/>. Equal scales give a straight extrusion.
        /// Every vertex is also moved along X by <paramref name="shearPerZ"/> times its z, which turns a straight extrusion into the
        /// sheared solid of a helical rack tooth (zero gives no shear).
        /// </summary>
        /// <param name="starShaped">True when every point of the outline can be seen from the axis (gears): the caps are
        /// fans from the axis. False uses ear clipping, which suits any simple polygon but is slower.</param>
        public static GearMesh3DBuilder.Mesh3D Loft(
            IReadOnlyList<GearPoint> outline,
            double zTop,
            double scaleTop,
            double zBottom,
            double scaleBottom,
            bool starShaped,
            double shearPerZ = 0.0)
        {
            var mesh = new GearMesh3DBuilder.Mesh3D();
            if (outline == null || outline.Count < 3) return mesh;

            var pts = new List<GearPoint>(outline);
            if (SignedArea(pts) < 0) pts.Reverse();
            var n = pts.Count;

            // Side faces: one quad per outline segment with its own four vertices.
            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                var baseIndex = mesh.Vertices.Count;
                mesh.Vertices.Add(new GearPoint3D(pts[i].X * scaleTop + shearPerZ * zTop, pts[i].Y * scaleTop, zTop));
                mesh.Vertices.Add(new GearPoint3D(pts[i].X * scaleBottom + shearPerZ * zBottom, pts[i].Y * scaleBottom, zBottom));
                mesh.Vertices.Add(new GearPoint3D(pts[j].X * scaleBottom + shearPerZ * zBottom, pts[j].Y * scaleBottom, zBottom));
                mesh.Vertices.Add(new GearPoint3D(pts[j].X * scaleTop + shearPerZ * zTop, pts[j].Y * scaleTop, zTop));
                mesh.TriangleIndices.AddRange(new[] { baseIndex, baseIndex + 1, baseIndex + 2, baseIndex, baseIndex + 2, baseIndex + 3 });
            }

            AddCap(mesh, pts, zTop, scaleTop, true, starShaped, shearPerZ * zTop);
            AddCap(mesh, pts, zBottom, scaleBottom, false, starShaped, shearPerZ * zBottom);
            mesh.CalculateNormals();
            return mesh;
        }

        private static void AddCap(GearMesh3DBuilder.Mesh3D mesh, List<GearPoint> pts, double z, double scale, bool facesUp, bool starShaped,
            double shearX = 0.0)
        {
            var n = pts.Count;
            var ring = mesh.Vertices.Count;
            foreach (var p in pts)
            {
                mesh.Vertices.Add(new GearPoint3D(p.X * scale + shearX, p.Y * scale, z));
            }

            var triangles = new List<int>();
            if (starShaped)
            {
                var centre = mesh.Vertices.Count;
                mesh.Vertices.Add(new GearPoint3D(shearX, 0, z));
                for (var i = 0; i < n; i++)
                {
                    triangles.Add(centre);
                    triangles.Add(ring + i);
                    triangles.Add(ring + (i + 1) % n);
                }
            }
            else
            {
                var scaled = new List<GearPoint>(n);
                foreach (var p in pts) scaled.Add(new GearPoint(p.X * scale, p.Y * scale));
                foreach (var local in Triangulate(scaled)) triangles.Add(ring + local);
            }

            for (var i = 0; i + 2 < triangles.Count; i += 3)
            {
                if (facesUp)
                {
                    mesh.TriangleIndices.AddRange(new[] { triangles[i], triangles[i + 1], triangles[i + 2] });
                }
                else
                {
                    mesh.TriangleIndices.AddRange(new[] { triangles[i], triangles[i + 2], triangles[i + 1] });
                }
            }
        }

        /// <summary>
        /// Ear-clipping triangulation of a simple anticlockwise polygon. Returns triples of indices into the polygon,
        /// each triple running anticlockwise.
        /// </summary>
        public static List<int> Triangulate(IReadOnlyList<GearPoint> polygon)
        {
            var result = new List<int>();
            var count = polygon.Count;
            if (count < 3) return result;

            var remaining = new List<int>(count);
            for (var i = 0; i < count; i++) remaining.Add(i);

            while (remaining.Count > 3)
            {
                var clipped = false;
                for (var i = 0; i < remaining.Count; i++)
                {
                    var prev = remaining[(i - 1 + remaining.Count) % remaining.Count];
                    var cur = remaining[i];
                    var next = remaining[(i + 1) % remaining.Count];
                    if (Cross(polygon[prev], polygon[cur], polygon[next]) <= 1e-12) continue;

                    var blocked = false;
                    foreach (var other in remaining)
                    {
                        if (other == prev || other == cur || other == next) continue;
                        if (InsideTriangle(polygon[other], polygon[prev], polygon[cur], polygon[next]))
                        {
                            blocked = true;
                            break;
                        }
                    }

                    if (blocked) continue;

                    result.Add(prev);
                    result.Add(cur);
                    result.Add(next);
                    remaining.RemoveAt(i);
                    clipped = true;
                    break;
                }

                if (!clipped)
                {
                    // Degenerate input (collinear points): drop the flattest vertex so that the loop always ends.
                    var flattest = 0;
                    var smallest = double.MaxValue;
                    for (var i = 0; i < remaining.Count; i++)
                    {
                        var prev = remaining[(i - 1 + remaining.Count) % remaining.Count];
                        var next = remaining[(i + 1) % remaining.Count];
                        var value = Math.Abs(Cross(polygon[prev], polygon[remaining[i]], polygon[next]));
                        if (value < smallest)
                        {
                            smallest = value;
                            flattest = i;
                        }
                    }

                    remaining.RemoveAt(flattest);
                }
            }

            if (remaining.Count == 3 && Cross(polygon[remaining[0]], polygon[remaining[1]], polygon[remaining[2]]) > 1e-12)
            {
                result.AddRange(remaining);
            }

            return result;
        }

        private static double Cross(GearPoint a, GearPoint b, GearPoint c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }

        private static bool InsideTriangle(GearPoint p, GearPoint a, GearPoint b, GearPoint c)
        {
            const double eps = 1e-12;
            return Cross(a, b, p) >= -eps && Cross(b, c, p) >= -eps && Cross(c, a, p) >= -eps;
        }

        private static double Distance(GearPoint a, GearPoint b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
