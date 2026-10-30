using System.Windows.Media;
using System.Windows.Media.Media3D;
using Bolsover.Gears.Calculator;

namespace Bolsover.Gears.Gui
{
    /// <summary>
    /// Converts domain Mesh3D geometry into hardware-accelerated WPF 3D mesh representations.
    /// </summary>
    public static class HelixMeshAdapter
    {
        /// <summary>
        /// Converts a <see cref="GearMesh3DBuilder.Mesh3D"/> to a WPF <see cref="MeshGeometry3D"/>.
        /// </summary>
        public static MeshGeometry3D ToMeshGeometry3D(GearMesh3DBuilder.Mesh3D mesh)
        {
            var geometry = new MeshGeometry3D();
            if (mesh == null || mesh.Vertices == null || mesh.Vertices.Count == 0 || mesh.TriangleIndices == null || mesh.TriangleIndices.Count < 3)
            {
                return geometry;
            }

            var positions = new Point3DCollection(mesh.Vertices.Count);
            for (var i = 0; i < mesh.Vertices.Count; i++)
            {
                var v = mesh.Vertices[i];
                positions.Add(new Point3D(v.X, v.Y, v.Z));
            }
            geometry.Positions = positions;

            var indices = new Int32Collection(mesh.TriangleIndices.Count);
            for (var i = 0; i < mesh.TriangleIndices.Count; i++)
            {
                indices.Add(mesh.TriangleIndices[i]);
            }
            geometry.TriangleIndices = indices;

            if (mesh.Normals != null && mesh.Normals.Count == mesh.Vertices.Count)
            {
                var normals = new Vector3DCollection(mesh.Normals.Count);
                for (var i = 0; i < mesh.Normals.Count; i++)
                {
                    var n = mesh.Normals[i];
                    normals.Add(new Vector3D(n.X, n.Y, n.Z));
                }
                geometry.Normals = normals;
            }

            return geometry;
        }

        /// <summary>
        /// Converts triangle edges of a <see cref="GearMesh3DBuilder.Mesh3D"/> into segment vertex pairs for wireframe line rendering.
        /// </summary>
        public static Point3DCollection ToWireframeLinePoints(GearMesh3DBuilder.Mesh3D mesh)
        {
            var linePoints = new Point3DCollection();
            if (mesh == null || mesh.Vertices == null || mesh.Vertices.Count == 0 || mesh.TriangleIndices == null || mesh.TriangleIndices.Count < 3)
            {
                return linePoints;
            }

            for (var i = 0; i + 2 < mesh.TriangleIndices.Count; i += 3)
            {
                var i0 = mesh.TriangleIndices[i];
                var i1 = mesh.TriangleIndices[i + 1];
                var i2 = mesh.TriangleIndices[i + 2];

                if (i0 < 0 || i0 >= mesh.Vertices.Count ||
                    i1 < 0 || i1 >= mesh.Vertices.Count ||
                    i2 < 0 || i2 >= mesh.Vertices.Count)
                {
                    continue;
                }

                var v0 = new Point3D(mesh.Vertices[i0].X, mesh.Vertices[i0].Y, mesh.Vertices[i0].Z);
                var v1 = new Point3D(mesh.Vertices[i1].X, mesh.Vertices[i1].Y, mesh.Vertices[i1].Z);
                var v2 = new Point3D(mesh.Vertices[i2].X, mesh.Vertices[i2].Y, mesh.Vertices[i2].Z);

                linePoints.Add(v0);
                linePoints.Add(v1);

                linePoints.Add(v1);
                linePoints.Add(v2);

                linePoints.Add(v2);
                linePoints.Add(v0);
            }

            return linePoints;
        }
    }
}
