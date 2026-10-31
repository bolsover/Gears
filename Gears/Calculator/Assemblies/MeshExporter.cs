using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Writes meshes and whole assembly scenes as ASCII STL or Wavefront OBJ. Coordinates are written with 7 decimal
    /// places (the geometry precision standard) using the invariant culture.
    /// </summary>
    public static class MeshExporter
    {
        private const string Format = "0.0000000";

        /// <summary>ASCII STL of one mesh; the facet normals are computed from the triangles.</summary>
        public static string ToStl(GearMesh3DBuilder.Mesh3D mesh, string name = "gear")
        {
            var sb = new StringBuilder();
            sb.AppendLine("solid " + Sanitise(name));
            AppendStlFacets(sb, mesh);
            sb.AppendLine("endsolid " + Sanitise(name));
            return sb.ToString();
        }

        /// <summary>ASCII STL of a scene: one solid per gear and, optionally, per shaft.</summary>
        public static string ToStl(AssemblyScene scene, bool includeShafts = false)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            var sb = new StringBuilder();
            foreach (var item in scene.Items)
            {
                sb.AppendLine("solid " + Sanitise(item.Component.Id));
                AppendStlFacets(sb, item.Mesh);
                sb.AppendLine("endsolid " + Sanitise(item.Component.Id));
            }

            if (includeShafts)
            {
                foreach (var item in scene.ShaftItems)
                {
                    if (item.Mesh.Vertices.Count == 0) continue;
                    sb.AppendLine("solid " + Sanitise(item.Shaft.Id));
                    AppendStlFacets(sb, item.Mesh);
                    sb.AppendLine("endsolid " + Sanitise(item.Shaft.Id));
                }
            }

            return sb.ToString();
        }

        /// <summary>Wavefront OBJ of a scene with one object per gear and, optionally, per shaft.</summary>
        public static string ToObj(AssemblyScene scene, bool includeShafts = false)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            var sb = new StringBuilder();
            sb.AppendLine("# Gears assembly export, 7 decimal places");

            var offset = 0;
            foreach (var item in scene.Items)
            {
                AppendObj(sb, item.Component.Id, item.Mesh, ref offset);
            }

            if (includeShafts)
            {
                foreach (var item in scene.ShaftItems)
                {
                    AppendObj(sb, item.Shaft.Id, item.Mesh, ref offset);
                }
            }

            return sb.ToString();
        }

        public static void WriteStl(AssemblyScene scene, string path, bool includeShafts = false)
        {
            File.WriteAllText(path, ToStl(scene, includeShafts), new UTF8Encoding(false));
        }

        public static void WriteObj(AssemblyScene scene, string path, bool includeShafts = false)
        {
            File.WriteAllText(path, ToObj(scene, includeShafts), new UTF8Encoding(false));
        }

        private static void AppendStlFacets(StringBuilder sb, GearMesh3DBuilder.Mesh3D mesh)
        {
            if (mesh == null) return;
            var ci = CultureInfo.InvariantCulture;
            for (var i = 0; i + 2 < mesh.TriangleIndices.Count; i += 3)
            {
                var a = mesh.Vertices[mesh.TriangleIndices[i]];
                var b = mesh.Vertices[mesh.TriangleIndices[i + 1]];
                var c = mesh.Vertices[mesh.TriangleIndices[i + 2]];

                var ux = b.X - a.X;
                var uy = b.Y - a.Y;
                var uz = b.Z - a.Z;
                var vx = c.X - a.X;
                var vy = c.Y - a.Y;
                var vz = c.Z - a.Z;
                var nx = uy * vz - uz * vy;
                var ny = uz * vx - ux * vz;
                var nz = ux * vy - uy * vx;
                var length = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                if (length < 1e-14) continue;
                nx /= length;
                ny /= length;
                nz /= length;

                sb.AppendLine($"  facet normal {nx.ToString(Format, ci)} {ny.ToString(Format, ci)} {nz.ToString(Format, ci)}");
                sb.AppendLine("    outer loop");
                foreach (var v in new[] { a, b, c })
                {
                    sb.AppendLine($"      vertex {v.X.ToString(Format, ci)} {v.Y.ToString(Format, ci)} {v.Z.ToString(Format, ci)}");
                }

                sb.AppendLine("    endloop");
                sb.AppendLine("  endfacet");
            }
        }

        private static void AppendObj(StringBuilder sb, string name, GearMesh3DBuilder.Mesh3D mesh, ref int offset)
        {
            if (mesh == null || mesh.Vertices.Count == 0) return;
            var ci = CultureInfo.InvariantCulture;

            sb.AppendLine("o " + Sanitise(name));
            foreach (var v in mesh.Vertices)
            {
                sb.AppendLine($"v {v.X.ToString(Format, ci)} {v.Y.ToString(Format, ci)} {v.Z.ToString(Format, ci)}");
            }

            var hasNormals = mesh.Normals.Count == mesh.Vertices.Count;
            if (hasNormals)
            {
                foreach (var n in mesh.Normals)
                {
                    sb.AppendLine($"vn {n.X.ToString(Format, ci)} {n.Y.ToString(Format, ci)} {n.Z.ToString(Format, ci)}");
                }
            }

            for (var i = 0; i + 2 < mesh.TriangleIndices.Count; i += 3)
            {
                var i0 = mesh.TriangleIndices[i] + 1 + offset;
                var i1 = mesh.TriangleIndices[i + 1] + 1 + offset;
                var i2 = mesh.TriangleIndices[i + 2] + 1 + offset;
                sb.AppendLine(hasNormals
                    ? $"f {i0}//{i0} {i1}//{i1} {i2}//{i2}"
                    : $"f {i0} {i1} {i2}");
            }

            offset += mesh.Vertices.Count;
        }

        private static string Sanitise(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "part";
            var chars = new List<char>(name.Length);
            foreach (var c in name)
            {
                chars.Add(char.IsWhiteSpace(c) ? '_' : c);
            }

            return new string(chars.ToArray());
        }
    }
}
