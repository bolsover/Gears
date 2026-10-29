using System;
using System.Collections.Generic;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Calculator
{
    /// <summary>
    /// Constructs 3D vertex buffers, triangle indices, and normals by extruding 2D gear profiles across a face width.
    /// </summary>
    public static class GearMesh3DBuilder
    {
        /// <summary>
        /// Represents a 3D triangle mesh composed of vertices, triangle indices, and normals.
        /// </summary>
        public class Mesh3D
        {
            public List<GearPoint3D> Vertices { get; } = new List<GearPoint3D>();
            public List<int> TriangleIndices { get; } = new List<int>();
            public List<GearPoint3D> Normals { get; } = new List<GearPoint3D>();

            /// <summary>
            /// Computes per-vertex normals based on face normals of adjacent triangles.
            /// </summary>
            public void CalculateNormals()
            {
                Normals.Clear();
                if (Vertices.Count == 0 || TriangleIndices.Count < 3) return;

                var tempNormals = new GearPoint3D[Vertices.Count];
                for (var i = 0; i < Vertices.Count; i++)
                {
                    tempNormals[i] = new GearPoint3D(0, 0, 0);
                }

                for (var i = 0; i + 2 < TriangleIndices.Count; i += 3)
                {
                    var i0 = TriangleIndices[i];
                    var i1 = TriangleIndices[i + 1];
                    var i2 = TriangleIndices[i + 2];

                    if (i0 < 0 || i0 >= Vertices.Count ||
                        i1 < 0 || i1 >= Vertices.Count ||
                        i2 < 0 || i2 >= Vertices.Count)
                    {
                        continue;
                    }

                    var v0 = Vertices[i0];
                    var v1 = Vertices[i1];
                    var v2 = Vertices[i2];

                    var ax = v1.X - v0.X;
                    var ay = v1.Y - v0.Y;
                    var az = v1.Z - v0.Z;

                    var bx = v2.X - v0.X;
                    var by = v2.Y - v0.Y;
                    var bz = v2.Z - v0.Z;

                    var nx = ay * bz - az * by;
                    var ny = az * bx - ax * bz;
                    var nz = ax * by - ay * bx;

                    tempNormals[i0] = new GearPoint3D(tempNormals[i0].X + nx, tempNormals[i0].Y + ny, tempNormals[i0].Z + nz);
                    tempNormals[i1] = new GearPoint3D(tempNormals[i1].X + nx, tempNormals[i1].Y + ny, tempNormals[i1].Z + nz);
                    tempNormals[i2] = new GearPoint3D(tempNormals[i2].X + nx, tempNormals[i2].Y + ny, tempNormals[i2].Z + nz);
                }

                for (var i = 0; i < Vertices.Count; i++)
                {
                    var n = tempNormals[i];
                    var mag = Math.Sqrt(n.X * n.X + n.Y * n.Y + n.Z * n.Z);
                    if (mag > 1e-9)
                    {
                        Normals.Add(new GearPoint3D(n.X / mag, n.Y / mag, n.Z / mag));
                    }
                    else
                    {
                        Normals.Add(new GearPoint3D(0, 0, 1));
                    }
                }
            }
        }

        /// <summary>
        /// Combines two 3D meshes into a single unified mesh.
        /// </summary>
        public static Mesh3D Combine(Mesh3D mesh1, Mesh3D mesh2)
        {
            var combined = new Mesh3D();
            if (mesh1 != null)
            {
                combined.Vertices.AddRange(mesh1.Vertices);
                combined.TriangleIndices.AddRange(mesh1.TriangleIndices);
                combined.Normals.AddRange(mesh1.Normals);
            }

            if (mesh2 != null)
            {
                var offset = combined.Vertices.Count;
                combined.Vertices.AddRange(mesh2.Vertices);
                foreach (var index in mesh2.TriangleIndices)
                {
                    combined.TriangleIndices.Add(index + offset);
                }
                combined.Normals.AddRange(mesh2.Normals);
            }

            return combined;
        }

        /// <summary>
        /// Transforms a 3D mesh by rotating around the Z axis and translating along (X, Y, Z).
        /// </summary>
        public static Mesh3D Transform(Mesh3D mesh, double offsetX, double offsetY, double offsetZ, double rotationAngleZRad = 0.0)
        {
            if (mesh == null) return new Mesh3D();

            var transformed = new Mesh3D();
            var cosZ = Math.Cos(rotationAngleZRad);
            var sinZ = Math.Sin(rotationAngleZRad);

            foreach (var v in mesh.Vertices)
            {
                var xRot = v.X * cosZ - v.Y * sinZ;
                var yRot = v.X * sinZ + v.Y * cosZ;
                transformed.Vertices.Add(new GearPoint3D(xRot + offsetX, yRot + offsetY, v.Z + offsetZ));
            }

            transformed.TriangleIndices.AddRange(mesh.TriangleIndices);

            foreach (var n in mesh.Normals)
            {
                var nxRot = n.X * cosZ - n.Y * sinZ;
                var nyRot = n.X * sinZ + n.Y * cosZ;
                transformed.Normals.Add(new GearPoint3D(nxRot, nyRot, n.Z));
            }

            return transformed;
        }

        /// <summary>
        /// Builds a 3D solid mesh for external or internal gears.
        /// For internal gears, the interior cavity is completely open/transparent (annular ring between inner teeth and outer rim).
        /// </summary>
        public static Mesh3D BuildGearMesh(
            AlibreGearGenerator.AlibreGearProfile profile,
            int z,
            double faceWidth,
            bool isInternal,
            bool isFullGear,
            double rimThickness = 0.0)
        {
            if (profile == null || z <= 0 || faceWidth <= 0) return new Mesh3D();

            if (isInternal)
            {
                var singleInner = new List<GearPoint>();
                for (var i = profile.LowerArc2.Count - 1; i >= 0; i--) singleInner.Add(profile.LowerArc2[i]);
                foreach (var pt in profile.Involute1) singleInner.Add(pt);
                foreach (var pt in profile.UpperArc) singleInner.Add(pt);
                for (var i = profile.Involute2.Count - 1; i >= 0; i--) singleInner.Add(profile.Involute2[i]);
                foreach (var pt in profile.LowerArc1) singleInner.Add(pt);

                singleInner = RemoveConsecutiveDuplicates(singleInner);
                if (singleInner.Count == 0) return new Mesh3D();

                var innerLoop = new List<GearPoint>();
                if (isFullGear)
                {
                    var pitch = 2.0 * Math.PI / z;
                    for (var k = 0; k < z; k++)
                    {
                        var angle = k * pitch;
                        innerLoop.AddRange(GearPoint.Rotated(singleInner, angle));
                    }
                }
                else
                {
                    innerLoop.AddRange(singleInner);
                }

                innerLoop = RemoveConsecutiveDuplicates(innerLoop);

                double rimRadius;
                if (profile.Parameters.TryGetValue("dedendumRadius", out var df))
                {
                    rimRadius = df + Math.Max(rimThickness, 1.0);
                }
                else
                {
                    var maxR = 0.0;
                    foreach (var p in innerLoop)
                    {
                        var r = Math.Sqrt(p.X * p.X + p.Y * p.Y);
                        if (r > maxR) maxR = r;
                    }
                    rimRadius = maxR + Math.Max(rimThickness, 1.0);
                }

                return BuildInternalGearMesh(innerLoop, rimRadius, faceWidth, isFullGear);
            }
            else
            {
                var singleOuter = new List<GearPoint>();
                for (var i = profile.Trochoid1.Count - 1; i >= 0; i--) singleOuter.Add(profile.Trochoid1[i]);
                foreach (var pt in profile.Involute1) singleOuter.Add(pt);
                foreach (var pt in profile.UpperArc) singleOuter.Add(pt);
                for (var i = profile.Involute2.Count - 1; i >= 0; i--) singleOuter.Add(profile.Involute2[i]);
                foreach (var pt in profile.Trochoid2) singleOuter.Add(pt);
                foreach (var pt in profile.LowerArc) singleOuter.Add(pt);

                singleOuter = RemoveConsecutiveDuplicates(singleOuter);
                if (singleOuter.Count == 0) return new Mesh3D();

                var outerLoop = new List<GearPoint>();
                if (isFullGear)
                {
                    var pitch = 2.0 * Math.PI / z;
                    for (var k = 0; k < z; k++)
                    {
                        var angle = k * pitch;
                        outerLoop.AddRange(GearPoint.Rotated(singleOuter, angle));
                    }
                }
                else
                {
                    outerLoop.AddRange(singleOuter);
                }

                outerLoop = RemoveConsecutiveDuplicates(outerLoop);

                return BuildExternalGearMesh(outerLoop, faceWidth, isFullGear);
            }
        }

        /// <summary>
        /// Builds an extruded 3D mesh for an internal gear (annular ring with internal teeth and outer rim).
        /// The central bore is completely empty/transparent.
        /// </summary>
        public static Mesh3D BuildInternalGearMesh(
            IReadOnlyList<GearPoint> innerProfile,
            double rimRadius,
            double faceWidth,
            bool isClosedLoop)
        {
            var mesh = new Mesh3D();
            if (innerProfile == null || innerProfile.Count < 3 || faceWidth <= 0) return mesh;

            var inner = new List<GearPoint>(innerProfile);
            EnsureCcw(inner, isClosedLoop);

            var n = inner.Count;
            var halfWidth = faceWidth / 2.0;

            // Compute outer rim points corresponding to each inner point
            var outer = new List<GearPoint>(n);
            for (var i = 0; i < n; i++)
            {
                var p = inner[i];
                var mag = Math.Sqrt(p.X * p.X + p.Y * p.Y);
                if (mag < 1e-9) mag = 1.0;
                var effectiveRim = Math.Max(rimRadius, mag + 0.1);
                outer.Add(new GearPoint(p.X / mag * effectiveRim, p.Y / mag * effectiveRim));
            }

            // Vertices layout:
            // 0 .. n-1:       Front Inner (z = +halfWidth)
            // n .. 2n-1:      Front Outer (z = +halfWidth)
            // 2n .. 3n-1:     Back Inner  (z = -halfWidth)
            // 3n .. 4n-1:     Back Outer  (z = -halfWidth)
            for (var i = 0; i < n; i++) mesh.Vertices.Add(new GearPoint3D(inner[i].X, inner[i].Y, halfWidth));
            for (var i = 0; i < n; i++) mesh.Vertices.Add(new GearPoint3D(outer[i].X, outer[i].Y, halfWidth));
            for (var i = 0; i < n; i++) mesh.Vertices.Add(new GearPoint3D(inner[i].X, inner[i].Y, -halfWidth));
            for (var i = 0; i < n; i++) mesh.Vertices.Add(new GearPoint3D(outer[i].X, outer[i].Y, -halfWidth));

            var segmentCount = isClosedLoop ? n : n - 1;

            for (var i = 0; i < segmentCount; i++)
            {
                var next = (i + 1) % n;

                var fi0 = i;
                var fi1 = next;
                var fo0 = n + i;
                var fo1 = n + next;

                var bi0 = 2 * n + i;
                var bi1 = 2 * n + next;
                var bo0 = 3 * n + i;
                var bo1 = 3 * n + next;

                // 1. Front Annular Cap (+Z normal)
                mesh.TriangleIndices.Add(fi0);
                mesh.TriangleIndices.Add(fo0);
                mesh.TriangleIndices.Add(fo1);

                mesh.TriangleIndices.Add(fi0);
                mesh.TriangleIndices.Add(fo1);
                mesh.TriangleIndices.Add(fi1);

                // 2. Back Annular Cap (-Z normal)
                mesh.TriangleIndices.Add(bi0);
                mesh.TriangleIndices.Add(bo1);
                mesh.TriangleIndices.Add(bo0);

                mesh.TriangleIndices.Add(bi0);
                mesh.TriangleIndices.Add(bi1);
                mesh.TriangleIndices.Add(bo1);

                // 3. Inner Side Walls (facing inward towards hollow center cavity)
                mesh.TriangleIndices.Add(fi0);
                mesh.TriangleIndices.Add(bi0);
                mesh.TriangleIndices.Add(bi1);

                mesh.TriangleIndices.Add(fi0);
                mesh.TriangleIndices.Add(bi1);
                mesh.TriangleIndices.Add(fi1);

                // 4. Outer Side Walls (facing outward into space)
                mesh.TriangleIndices.Add(fo0);
                mesh.TriangleIndices.Add(fo1);
                mesh.TriangleIndices.Add(bo1);

                mesh.TriangleIndices.Add(fo0);
                mesh.TriangleIndices.Add(bo1);
                mesh.TriangleIndices.Add(bo0);
            }

            // If not a closed loop (single tooth sector), cap the radial ends
            if (!isClosedLoop && n >= 2)
            {
                // Start radial wall (i = 0)
                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(n);
                mesh.TriangleIndices.Add(3 * n);

                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(3 * n);
                mesh.TriangleIndices.Add(2 * n);

                // End radial wall (i = n - 1)
                mesh.TriangleIndices.Add(n - 1);
                mesh.TriangleIndices.Add(3 * n - 1);
                mesh.TriangleIndices.Add(4 * n - 1);

                mesh.TriangleIndices.Add(n - 1);
                mesh.TriangleIndices.Add(4 * n - 1);
                mesh.TriangleIndices.Add(2 * n - 1);
            }

            mesh.CalculateNormals();
            return mesh;
        }

        /// <summary>
        /// Builds an extruded 3D mesh for an external gear (solid body with external teeth).
        /// </summary>
        public static Mesh3D BuildExternalGearMesh(
            IReadOnlyList<GearPoint> outerProfile,
            double faceWidth,
            bool isClosedLoop)
        {
            var mesh = new Mesh3D();
            if (outerProfile == null || outerProfile.Count < 3 || faceWidth <= 0) return mesh;

            var outer = new List<GearPoint>(outerProfile);
            EnsureCcw(outer, isClosedLoop);

            var n = outer.Count;
            var halfWidth = faceWidth / 2.0;

            // 0 .. n-1:       Front (z = +halfWidth)
            // n .. 2n-1:      Back  (z = -halfWidth)
            for (var i = 0; i < n; i++) mesh.Vertices.Add(new GearPoint3D(outer[i].X, outer[i].Y, halfWidth));
            for (var i = 0; i < n; i++) mesh.Vertices.Add(new GearPoint3D(outer[i].X, outer[i].Y, -halfWidth));

            var centerFrontIdx = mesh.Vertices.Count;
            mesh.Vertices.Add(new GearPoint3D(0, 0, halfWidth));
            var centerBackIdx = mesh.Vertices.Count;
            mesh.Vertices.Add(new GearPoint3D(0, 0, -halfWidth));

            var segmentCount = isClosedLoop ? n : n - 1;

            for (var i = 0; i < segmentCount; i++)
            {
                var next = (i + 1) % n;

                var f0 = i;
                var f1 = next;
                var b0 = n + i;
                var b1 = n + next;

                // Side Walls (facing outward)
                mesh.TriangleIndices.Add(f0);
                mesh.TriangleIndices.Add(f1);
                mesh.TriangleIndices.Add(b1);

                mesh.TriangleIndices.Add(f0);
                mesh.TriangleIndices.Add(b1);
                mesh.TriangleIndices.Add(b0);

                // Front Cap (+Z normal)
                mesh.TriangleIndices.Add(centerFrontIdx);
                mesh.TriangleIndices.Add(f0);
                mesh.TriangleIndices.Add(f1);

                // Back Cap (-Z normal)
                mesh.TriangleIndices.Add(centerBackIdx);
                mesh.TriangleIndices.Add(b1);
                mesh.TriangleIndices.Add(b0);
            }

            if (!isClosedLoop && n >= 2)
            {
                // Start radial wall (from (0,0) to outer[0])
                mesh.TriangleIndices.Add(centerFrontIdx);
                mesh.TriangleIndices.Add(centerBackIdx);
                mesh.TriangleIndices.Add(n);

                mesh.TriangleIndices.Add(centerFrontIdx);
                mesh.TriangleIndices.Add(n);
                mesh.TriangleIndices.Add(0);

                // End radial wall (from outer[n-1] to (0,0))
                mesh.TriangleIndices.Add(centerFrontIdx);
                mesh.TriangleIndices.Add(n - 1);
                mesh.TriangleIndices.Add(2 * n - 1);

                mesh.TriangleIndices.Add(centerFrontIdx);
                mesh.TriangleIndices.Add(2 * n - 1);
                mesh.TriangleIndices.Add(centerBackIdx);
            }

            mesh.CalculateNormals();
            return mesh;
        }

        private static void EnsureCcw(List<GearPoint> loop, bool isClosedLoop)
        {
            if (loop == null || loop.Count < 3) return;
            var area = 0.0;
            var n = loop.Count;
            if (isClosedLoop)
            {
                for (var i = 0; i < n; i++)
                {
                    var j = (i + 1) % n;
                    area += (loop[i].X * loop[j].Y - loop[j].X * loop[i].Y);
                }
            }
            else
            {
                for (var i = 0; i < n - 1; i++)
                {
                    area += (loop[i].X * loop[i + 1].Y - loop[i + 1].X * loop[i].Y);
                }
            }
            if (area < 0)
            {
                loop.Reverse();
            }
        }

        private static List<GearPoint> RemoveConsecutiveDuplicates(List<GearPoint> points)
        {
            if (points == null || points.Count == 0) return points;
            var cleaned = new List<GearPoint>(points.Count);
            cleaned.Add(points[0]);
            for (var i = 1; i < points.Count; i++)
            {
                var dx = points[i].X - points[i - 1].X;
                var dy = points[i].Y - points[i - 1].Y;
                if (dx * dx + dy * dy > 1e-10)
                {
                    cleaned.Add(points[i]);
                }
            }
            return cleaned;
        }

        /// <summary>
        /// Constructs an extruded 3D mesh from a closed 2D profile polyline across the specified face width.
        /// </summary>
        /// <param name="profile2D">The ordered list of 2D boundary points forming a closed loop.</param>
        /// <param name="faceWidth">The face width (thickness) along the Z-axis.</param>
        /// <returns>A <see cref="Mesh3D"/> containing extruded vertices and triangle faces.</returns>
        public static Mesh3D BuildExtrudedGearMesh(IReadOnlyList<GearPoint> profile2D, double faceWidth)
        {
            return BuildExternalGearMesh(profile2D, faceWidth, true);
        }

        /// <summary>
        /// Constructs an extruded 3D mesh by flattening a collection of polyline segments into a single profile.
        /// </summary>
        /// <param name="polylines">The collection of 2D polyline segments.</param>
        /// <param name="faceWidth">The face width (thickness) along the Z-axis.</param>
        /// <returns>A <see cref="Mesh3D"/> containing extruded vertices and triangle faces.</returns>
        public static Mesh3D BuildExtrudedGearMesh(IEnumerable<List<GearPoint>> polylines, double faceWidth)
        {
            var flatList = new List<GearPoint>();
            if (polylines != null)
            {
                foreach (var poly in polylines)
                {
                    if (poly != null)
                    {
                        flatList.AddRange(poly);
                    }
                }
            }

            return BuildExtrudedGearMesh(flatList, faceWidth);
        }
    }
}
