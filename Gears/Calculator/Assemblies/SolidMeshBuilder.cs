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

        /// <summary>
        /// Lofts the outline of a straight bevel gear towards the cone apex on the +Z axis. At the large end every outline point
        /// lies on the surface perpendicular to the pitch cone (the back cone): a point at radius <c>r</c> sits at
        /// <c>zLarge + (r - pitchRadius) tan(delta)</c>, so tooth tips are set back towards the apex relative to the pitch circle,
        /// as in the usual bevel gear drawings. The small end is the large end scaled about the apex by
        /// <paramref name="smallEndScale"/>, so all tooth edges, tip lines and root lines run through the apex.
        /// </summary>
        /// <param name="zLarge">Z of the pitch circle at the large end.</param>
        /// <param name="pitchRadius">Pitch radius at the large end.</param>
        /// <param name="tanCone">Tangent of the pitch cone half angle; zero gives flat ends perpendicular to the axis.</param>
        /// <param name="apexZ">Z of the cone apex.</param>
        /// <param name="smallEndScale">Scale about the apex of the small end relative to the large end.</param>
        /// <param name="boreRadius">
        /// Radius of the bore through the blank; zero for none. With a bore or a hub the large end is a ring on the back cone from the
        /// teeth in to the hub (or the bore), and the small end a ring on the front cone out to the bore.
        /// </param>
        /// <param name="hubRadius">Radius of the hub behind the back cone; zero or a value not above the bore radius for none.</param>
        /// <param name="hubLength">Length of the hub behind the back cone, along the axis (mm).</param>
        public static GearMesh3DBuilder.Mesh3D LoftToApex(
            IReadOnlyList<GearPoint> outline,
            double zLarge,
            double pitchRadius,
            double tanCone,
            double apexZ,
            double smallEndScale,
            double boreRadius = 0.0,
            double hubRadius = 0.0,
            double hubLength = 0.0)
        {
            var mesh = new GearMesh3DBuilder.Mesh3D();
            if (outline == null || outline.Count < 3) return mesh;

            var pts = new List<GearPoint>(outline);
            if (SignedArea(pts) < 0) pts.Reverse();
            var n = pts.Count;

            double LargeZ(double radius) => zLarge + (radius - pitchRadius) * tanCone;
            GearPoint3D Large(GearPoint p) => new GearPoint3D(p.X, p.Y, LargeZ(Math.Sqrt(p.X * p.X + p.Y * p.Y)));
            GearPoint3D Small(GearPoint3D p) =>
                new GearPoint3D(p.X * smallEndScale, p.Y * smallEndScale, apexZ + (p.Z - apexZ) * smallEndScale);

            var large = new GearPoint3D[n];
            var small = new GearPoint3D[n];
            var minRadius = double.MaxValue;
            for (var i = 0; i < n; i++)
            {
                large[i] = Large(pts[i]);
                small[i] = Small(large[i]);
                minRadius = Math.Min(minRadius, Math.Sqrt(pts[i].X * pts[i].X + pts[i].Y * pts[i].Y));
            }

            // Side faces: one quad per outline segment with its own four vertices (small end first, as in Loft).
            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                var baseIndex = mesh.Vertices.Count;
                mesh.Vertices.Add(small[i]);
                mesh.Vertices.Add(large[i]);
                mesh.Vertices.Add(large[j]);
                mesh.Vertices.Add(small[j]);
                mesh.TriangleIndices.AddRange(new[] { baseIndex, baseIndex + 1, baseIndex + 2, baseIndex, baseIndex + 2, baseIndex + 3 });
            }

            boreRadius = Math.Max(0.0, boreRadius);
            var hasHub = hubRadius > boreRadius && hubLength > 0.0;
            var innerRadius = hasHub ? hubRadius : boreRadius;
            if (innerRadius <= 0.0)
            {
                // Caps: a fan from the axis at the level of the root circle (the flat inside of the blank).
                var centreLarge = new GearPoint3D(0.0, 0.0, LargeZ(minRadius));
                AddConeCap(mesh, small, Small(centreLarge), true);
                AddConeCap(mesh, large, centreLarge, false);
                mesh.CalculateNormals();
                return mesh;
            }

            var dirX = new double[n];
            var dirY = new double[n];
            for (var i = 0; i < n; i++)
            {
                var radius = Math.Sqrt(pts[i].X * pts[i].X + pts[i].Y * pts[i].Y);
                dirX[i] = radius > 1e-12 ? pts[i].X / radius : 1.0;
                dirY[i] = radius > 1e-12 ? pts[i].Y / radius : 0.0;
            }

            GearPoint3D OnRing(int i, double radius, double z) => new GearPoint3D(dirX[i] * radius, dirY[i] * radius, z);

            // Large end: the back cone from the teeth in to the hub (or the bore), facing away from the apex.
            var zInner = LargeZ(innerRadius);
            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                AddQuad(mesh, OnRing(i, innerRadius, zInner), OnRing(j, innerRadius, zInner), large[j], large[i]);
            }

            // Small end: the front cone from the teeth in to the bore, or a fan to the cone apex of the blank when there is no bore.
            var zToeBore = 0.0;
            if (boreRadius > 0.0)
            {
                zToeBore = apexZ + (LargeZ(boreRadius / smallEndScale) - apexZ) * smallEndScale;
                for (var i = 0; i < n; i++)
                {
                    var j = (i + 1) % n;
                    AddQuad(mesh, OnRing(i, boreRadius, zToeBore), small[i], small[j], OnRing(j, boreRadius, zToeBore));
                }
            }
            else
            {
                AddConeCap(mesh, small, Small(new GearPoint3D(0.0, 0.0, LargeZ(0.0))), true);
            }

            var zBoreBottom = zInner;
            if (hasHub)
            {
                // Hub: a cylinder behind the back cone, closed by a flat end face (a ring when there is a bore).
                var zBack = zInner - hubLength;
                zBoreBottom = zBack;
                for (var i = 0; i < n; i++)
                {
                    var j = (i + 1) % n;
                    AddQuad(mesh, OnRing(i, hubRadius, zBack), OnRing(j, hubRadius, zBack), OnRing(j, hubRadius, zInner), OnRing(i, hubRadius, zInner));
                }

                if (boreRadius > 0.0)
                {
                    for (var i = 0; i < n; i++)
                    {
                        var j = (i + 1) % n;
                        AddQuad(mesh, OnRing(i, boreRadius, zBack), OnRing(j, boreRadius, zBack), OnRing(j, hubRadius, zBack), OnRing(i, hubRadius, zBack));
                    }
                }
                else
                {
                    var hubEnd = new GearPoint3D[n];
                    for (var i = 0; i < n; i++) hubEnd[i] = OnRing(i, hubRadius, zBack);
                    AddConeCap(mesh, hubEnd, new GearPoint3D(0.0, 0.0, zBack), false);
                }
            }

            // Bore: a cylinder whose faces point towards the axis.
            if (boreRadius > 0.0)
            {
                for (var i = 0; i < n; i++)
                {
                    var j = (i + 1) % n;
                    AddQuad(mesh, OnRing(i, boreRadius, zBoreBottom), OnRing(i, boreRadius, zToeBore), OnRing(j, boreRadius, zToeBore), OnRing(j, boreRadius, zBoreBottom));
                }
            }

            mesh.CalculateNormals();
            return mesh;
        }

        /// <summary>Adds a quad with its own four vertices as the triangles (a, b, c) and (a, c, d).</summary>
        private static void AddQuad(GearMesh3DBuilder.Mesh3D mesh, GearPoint3D a, GearPoint3D b, GearPoint3D c, GearPoint3D d)
        {
            var k = mesh.Vertices.Count;
            mesh.Vertices.Add(a);
            mesh.Vertices.Add(b);
            mesh.Vertices.Add(c);
            mesh.Vertices.Add(d);
            mesh.TriangleIndices.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
        }

        private static void AddConeCap(GearMesh3DBuilder.Mesh3D mesh, GearPoint3D[] ring, GearPoint3D centre, bool facesUp)
        {
            var n = ring.Length;
            var ringStart = mesh.Vertices.Count;
            mesh.Vertices.AddRange(ring);
            var centreIndex = mesh.Vertices.Count;
            mesh.Vertices.Add(centre);
            for (var i = 0; i < n; i++)
            {
                var a = ringStart + i;
                var b = ringStart + (i + 1) % n;
                if (facesUp) mesh.TriangleIndices.AddRange(new[] { centreIndex, a, b });
                else mesh.TriangleIndices.AddRange(new[] { centreIndex, b, a });
            }
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
