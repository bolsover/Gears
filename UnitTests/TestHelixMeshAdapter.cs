using System.Collections.Generic;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Gui;
using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class TestHelixMeshAdapter
    {
        [Test]
        public void ToMeshGeometry3D_NullOrEmptyMesh_ReturnsEmptyGeometry()
        {
            var nullResult = HelixMeshAdapter.ToMeshGeometry3D(null);
            Assert.IsNotNull(nullResult);
            Assert.AreEqual(0, nullResult.Positions.Count);
            Assert.AreEqual(0, nullResult.TriangleIndices.Count);

            var emptyMesh = new GearMesh3DBuilder.Mesh3D();
            var emptyResult = HelixMeshAdapter.ToMeshGeometry3D(emptyMesh);
            Assert.IsNotNull(emptyResult);
            Assert.AreEqual(0, emptyResult.Positions.Count);
            Assert.AreEqual(0, emptyResult.TriangleIndices.Count);
        }

        [Test]
        public void ToMeshGeometry3D_ValidMesh_CorrectlyMapsPositionsIndicesAndNormals()
        {
            var mesh = new GearMesh3DBuilder.Mesh3D();
            mesh.Vertices.Add(new GearPoint3D(0, 0, 0));
            mesh.Vertices.Add(new GearPoint3D(10, 0, 0));
            mesh.Vertices.Add(new GearPoint3D(0, 10, 0));

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);

            mesh.CalculateNormals();

            var wpfMesh = HelixMeshAdapter.ToMeshGeometry3D(mesh);

            Assert.IsNotNull(wpfMesh);
            Assert.AreEqual(3, wpfMesh.Positions.Count);
            Assert.AreEqual(3, wpfMesh.TriangleIndices.Count);
            Assert.AreEqual(3, wpfMesh.Normals.Count);

            Assert.AreEqual(0.0, wpfMesh.Positions[0].X, 1e-6);
            Assert.AreEqual(10.0, wpfMesh.Positions[1].X, 1e-6);
            Assert.AreEqual(10.0, wpfMesh.Positions[2].Y, 1e-6);

            Assert.AreEqual(0, wpfMesh.TriangleIndices[0]);
            Assert.AreEqual(1, wpfMesh.TriangleIndices[1]);
            Assert.AreEqual(2, wpfMesh.TriangleIndices[2]);

            Assert.AreEqual(0.0, wpfMesh.Normals[0].X, 1e-6);
            Assert.AreEqual(0.0, wpfMesh.Normals[0].Y, 1e-6);
            Assert.AreEqual(1.0, wpfMesh.Normals[0].Z, 1e-6);
        }

        [Test]
        public void ToWireframeLinePoints_NullOrEmptyMesh_ReturnsEmptyCollection()
        {
            var nullResult = HelixMeshAdapter.ToWireframeLinePoints(null);
            Assert.IsNotNull(nullResult);
            Assert.AreEqual(0, nullResult.Count);

            var emptyMesh = new GearMesh3DBuilder.Mesh3D();
            var emptyResult = HelixMeshAdapter.ToWireframeLinePoints(emptyMesh);
            Assert.IsNotNull(emptyResult);
            Assert.AreEqual(0, emptyResult.Count);
        }

        [Test]
        public void ToWireframeLinePoints_ValidMesh_CreatesSixPointsPerTriangle()
        {
            var mesh = new GearMesh3DBuilder.Mesh3D();
            mesh.Vertices.Add(new GearPoint3D(0, 0, 0));
            mesh.Vertices.Add(new GearPoint3D(10, 0, 0));
            mesh.Vertices.Add(new GearPoint3D(0, 10, 0));

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);

            var linePoints = HelixMeshAdapter.ToWireframeLinePoints(mesh);

            Assert.IsNotNull(linePoints);
            // 1 triangle = 3 edges = 6 points (v0->v1, v1->v2, v2->v0)
            Assert.AreEqual(6, linePoints.Count);

            // Segment 1: v0 -> v1
            Assert.AreEqual(0.0, linePoints[0].X, 1e-6);
            Assert.AreEqual(10.0, linePoints[1].X, 1e-6);

            // Segment 2: v1 -> v2
            Assert.AreEqual(10.0, linePoints[2].X, 1e-6);
            Assert.AreEqual(10.0, linePoints[3].Y, 1e-6);

            // Segment 3: v2 -> v0
            Assert.AreEqual(10.0, linePoints[4].Y, 1e-6);
            Assert.AreEqual(0.0, linePoints[5].X, 1e-6);
        }

        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public void HelixAssemblyViewport_CanBeConstructed()
        {
            var viewport = new HelixAssemblyViewport();
            Assert.IsNotNull(viewport);
            viewport.Measure(new System.Windows.Size(800, 600));
            viewport.Arrange(new System.Windows.Rect(0, 0, 800, 600));
            viewport.UpdateLayout();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
        }
    }
}
