using System;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Builds a simple cylinder mesh for a shaft, in the shaft's local coordinates (axis along Z).
    /// </summary>
    public static class ShaftMeshBuilder
    {
        public const int DefaultSegments = 32;

        public static GearMesh3DBuilder.Mesh3D Build(Shaft shaft, int segments = DefaultSegments)
        {
            if (shaft == null) throw new ArgumentNullException(nameof(shaft));
            if (segments < 3) throw new ArgumentOutOfRangeException(nameof(segments), "A shaft needs at least 3 segments.");
            if (shaft.Diameter <= 0) throw new InvalidOperationException($"Shaft '{shaft.Id}': the diameter must be positive.");

            shaft.GetAxialSpan(out var z0, out var z1);
            return BuildCylinder(shaft.Diameter / 2.0, z0, z1, segments);
        }

        public static GearMesh3DBuilder.Mesh3D BuildCylinder(double radius, double z0, double z1, int segments)
        {
            var mesh = new GearMesh3DBuilder.Mesh3D();

            // Side wall: for every segment one bottom and one top vertex with a radial normal.
            for (var i = 0; i < segments; i++)
            {
                var a = 2.0 * Math.PI * i / segments;
                var c = Math.Cos(a);
                var s = Math.Sin(a);
                mesh.Vertices.Add(new GearPoint3D(radius * c, radius * s, z0));
                mesh.Normals.Add(new GearPoint3D(c, s, 0));
                mesh.Vertices.Add(new GearPoint3D(radius * c, radius * s, z1));
                mesh.Normals.Add(new GearPoint3D(c, s, 0));
            }

            for (var i = 0; i < segments; i++)
            {
                var next = (i + 1) % segments;
                var b0 = 2 * i;
                var t0 = 2 * i + 1;
                var b1 = 2 * next;
                var t1 = 2 * next + 1;
                mesh.TriangleIndices.AddRange(new[] { b0, b1, t0, b1, t1, t0 });
            }

            // Caps: a centre vertex and a ring of vertices with an axial normal.
            AddCap(mesh, radius, z0, -1.0, segments);
            AddCap(mesh, radius, z1, 1.0, segments);
            return mesh;
        }

        private static void AddCap(GearMesh3DBuilder.Mesh3D mesh, double radius, double z, double normalZ, int segments)
        {
            var centre = mesh.Vertices.Count;
            mesh.Vertices.Add(new GearPoint3D(0, 0, z));
            mesh.Normals.Add(new GearPoint3D(0, 0, normalZ));

            var ring = mesh.Vertices.Count;
            for (var i = 0; i < segments; i++)
            {
                var a = 2.0 * Math.PI * i / segments;
                mesh.Vertices.Add(new GearPoint3D(radius * Math.Cos(a), radius * Math.Sin(a), z));
                mesh.Normals.Add(new GearPoint3D(0, 0, normalZ));
            }

            for (var i = 0; i < segments; i++)
            {
                var v0 = ring + i;
                var v1 = ring + (i + 1) % segments;
                if (normalZ > 0) mesh.TriangleIndices.AddRange(new[] { centre, v0, v1 });
                else mesh.TriangleIndices.AddRange(new[] { centre, v1, v0 });
            }
        }
    }
}
