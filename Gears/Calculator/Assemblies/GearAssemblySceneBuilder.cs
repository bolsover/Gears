using System;
using System.Collections.Generic;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Solves a <see cref="GearAssembly"/> and builds the world-space geometry of every gear.
    /// Each gear is built in its own local frame and moved with the shaft placement, the gear's axial
    /// offset and its tooth phase. All world coordinates are rounded to 7 decimal places.
    /// The existing single-gear and pair builders are not modified.
    /// </summary>
    public static class GearAssemblySceneBuilder
    {
        public static AssemblyScene Build(GearAssembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            assembly.Solve();

            var scene = new AssemblyScene();
            foreach (var shaft in assembly.Shafts)
            {
                scene.Add(new SceneShaftItem(
                    shaft,
                    TransformMesh(ShaftMeshBuilder.Build(shaft), shaft.Placement.ToTransform())));

                foreach (var mount in shaft.Mounts)
                {
                    var geometry = mount.Component.BuildGeometry();

                    var localToWorld = shaft.Placement.ToTransform()
                        .Compose(Transform3D.Translate(new Vector3D(0, 0, mount.AxialOffset)))
                        .Compose(Transform3D.RotationZ(mount.PhaseRad));

                    scene.Add(new SceneItem(
                        mount.Component,
                        shaft,
                        localToWorld,
                        TransformPolylines(geometry.Polylines, localToWorld),
                        TransformMesh(geometry.Mesh, localToWorld)));
                }
            }

            return scene;
        }

        /// <summary>
        /// Transforms outline polylines (which lie in the local XY plane) and keeps their world X/Y coordinates.
        /// </summary>
        public static List<List<GearPoint>> TransformPolylines(IEnumerable<List<GearPoint>> polylines, Transform3D transform)
        {
            var result = new List<List<GearPoint>>();
            foreach (var polyline in polylines)
            {
                var transformed = new List<GearPoint>(polyline.Count);
                foreach (var point in polyline)
                {
                    var world = transform.TransformPoint(new Vector3D(point.X, point.Y, 0.0));
                    transformed.Add(new GearPoint(GearPrecision.Round(world.X), GearPrecision.Round(world.Y)));
                }

                result.Add(transformed);
            }

            return result;
        }

        /// <summary>
        /// Transforms a mesh: vertices as points and normals as directions (re-normalised).
        /// The input mesh is not modified.
        /// </summary>
        public static GearMesh3DBuilder.Mesh3D TransformMesh(GearMesh3DBuilder.Mesh3D mesh, Transform3D transform)
        {
            var result = new GearMesh3DBuilder.Mesh3D();
            if (mesh == null) return result;

            foreach (var vertex in mesh.Vertices)
            {
                var world = GearPrecision.Round(transform.TransformPoint(new Vector3D(vertex.X, vertex.Y, vertex.Z)));
                result.Vertices.Add(new GearPoint3D(world.X, world.Y, world.Z));
            }

            result.TriangleIndices.AddRange(mesh.TriangleIndices);

            foreach (var normal in mesh.Normals)
            {
                var direction = transform.TransformDirection(new Vector3D(normal.X, normal.Y, normal.Z));
                var length = direction.Length;
                if (length > 1e-12) direction = direction * (1.0 / length);
                direction = GearPrecision.Round(direction);
                result.Normals.Add(new GearPoint3D(direction.X, direction.Y, direction.Z));
            }

            return result;
        }
    }
}
