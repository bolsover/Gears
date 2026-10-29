using System.Collections.Generic;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class TestGearMesh3DBuilder
    {
        [Test]
        public void TestBuildExtrudedGearMesh_EmptyProfile_ReturnsEmptyMesh()
        {
            var mesh = GearMesh3DBuilder.BuildExtrudedGearMesh(new List<GearPoint>(), 10.0);
            Assert.IsNotNull(mesh);
            Assert.AreEqual(0, mesh.Vertices.Count);
            Assert.AreEqual(0, mesh.TriangleIndices.Count);
            Assert.AreEqual(0, mesh.Normals.Count);
        }

        [Test]
        public void TestBuildExtrudedGearMesh_TriangleProfile_ConstructsValidVerticesAndTriangles()
        {
            var profile = new List<GearPoint>
            {
                new GearPoint(0, 0),
                new GearPoint(10, 0),
                new GearPoint(5, 10)
            };

            var faceWidth = 20.0;
            var mesh = GearMesh3DBuilder.BuildExtrudedGearMesh(profile, faceWidth);

            // 3 front + 3 back + 1 center front + 1 center back = 8 vertices
            Assert.AreEqual(8, mesh.Vertices.Count);

            // 3 side quads (6 triangles = 18 indices) + 3 front cap triangles (9 indices) + 3 back cap triangles (9 indices) = 36 indices
            Assert.AreEqual(36, mesh.TriangleIndices.Count);

            // Check Z coordinates
            Assert.AreEqual(10.0, mesh.Vertices[0].Z, 1e-6);
            Assert.AreEqual(-10.0, mesh.Vertices[3].Z, 1e-6);

            // Check normals count equals vertices count
            Assert.AreEqual(mesh.Vertices.Count, mesh.Normals.Count);
        }

        [Test]
        public void TestBuildExtrudedGearMesh_FromInvoluteProfile_GeneratesValidMesh()
        {
            var profile = AlibreGearGenerator.GenerateExternalToothProfile(20, 2.0, 20.0, 0.0, false);
            var toothOutline = AlibreGearGenerator.BuildToothOutline(profile, false);
            var fullGear = AlibreGearGenerator.BuildFullGear(20, toothOutline);

            var mesh = GearMesh3DBuilder.BuildExtrudedGearMesh(fullGear, 15.0);

            Assert.Greater(mesh.Vertices.Count, 0);
            Assert.Greater(mesh.TriangleIndices.Count, 0);
            Assert.AreEqual(0, mesh.TriangleIndices.Count % 3);
            Assert.AreEqual(mesh.Vertices.Count, mesh.Normals.Count);
        }

        [Test]
        public void TestBuildExtrudedGearMesh_FromInternalInvoluteProfile_GeneratesValidMesh()
        {
            var profile = AlibreGearGenerator.GenerateInternalToothProfile(30, 2.0, 20.0, 5.0, 0.0, false);
            var toothOutline = AlibreGearGenerator.BuildToothOutline(profile, true);
            var fullGear = AlibreGearGenerator.BuildFullGear(30, toothOutline);

            var mesh = GearMesh3DBuilder.BuildExtrudedGearMesh(fullGear, 12.0);

            Assert.Greater(mesh.Vertices.Count, 0);
            Assert.Greater(mesh.TriangleIndices.Count, 0);
            Assert.AreEqual(0, mesh.TriangleIndices.Count % 3);
            Assert.AreEqual(mesh.Vertices.Count, mesh.Normals.Count);
        }

        [Test]
        public void TestBuildGearMesh_InternalGear_HasNoCenterVerticesAndMaintainsHollowCavity()
        {
            var profile = AlibreGearGenerator.GenerateInternalToothProfile(30, 2.0, 20.0, 5.0, 0.0, false);
            var mesh = GearMesh3DBuilder.BuildGearMesh(profile, 30, 15.0, isInternal: true, isFullGear: true, rimThickness: 5.0);

            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.Vertices.Count, 0);
            Assert.Greater(mesh.TriangleIndices.Count, 0);
            Assert.AreEqual(0, mesh.TriangleIndices.Count % 3);
            Assert.AreEqual(mesh.Vertices.Count, mesh.Normals.Count);

            var minRadius = double.MaxValue;
            var maxRadius = 0.0;

            // Verify that no vertex is at the origin and vertices span between addendum and rim
            foreach (var vertex in mesh.Vertices)
            {
                var r = System.Math.Sqrt(vertex.X * vertex.X + vertex.Y * vertex.Y);
                if (r < minRadius) minRadius = r;
                if (r > maxRadius) maxRadius = r;
            }

            Assert.Greater(minRadius, 25.0, "Inner tooth tip radius must be greater than 25mm for m=2, z=30 internal gear.");
            Assert.Greater(maxRadius, 35.0, "Outer rim radius must be at least 35mm.");
        }

        [Test]
        public void TestBuildGearMesh_InternalSingleTooth_GeneratesValidMesh()
        {
            var profile = AlibreGearGenerator.GenerateInternalToothProfile(24, 2.5, 20.0, 6.0, 0.0, false);
            var mesh = GearMesh3DBuilder.BuildGearMesh(profile, 24, 10.0, isInternal: true, isFullGear: false, rimThickness: 6.0);

            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.Vertices.Count, 0);
            Assert.Greater(mesh.TriangleIndices.Count, 0);
            Assert.AreEqual(0, mesh.TriangleIndices.Count % 3);
            Assert.AreEqual(mesh.Vertices.Count, mesh.Normals.Count);

            foreach (var vertex in mesh.Vertices)
            {
                var r = System.Math.Sqrt(vertex.X * vertex.X + vertex.Y * vertex.Y);
                Assert.Greater(r, 20.0, "Internal single tooth sector must not contain origin vertices.");
            }
        }

        [Test]
        public void TestBuildGearMesh_ExternalGear_GeneratesValidSolidMesh()
        {
            var profile = AlibreGearGenerator.GenerateExternalToothProfile(20, 2.0, 20.0, 0.0, false);
            var mesh = GearMesh3DBuilder.BuildGearMesh(profile, 20, 15.0, isInternal: false, isFullGear: true);

            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.Vertices.Count, 0);
            Assert.Greater(mesh.TriangleIndices.Count, 0);
            Assert.AreEqual(0, mesh.TriangleIndices.Count % 3);
            Assert.AreEqual(mesh.Vertices.Count, mesh.Normals.Count);

            var maxRadius = 0.0;
            foreach (var vertex in mesh.Vertices)
            {
                var r = System.Math.Sqrt(vertex.X * vertex.X + vertex.Y * vertex.Y);
                if (r > maxRadius) maxRadius = r;
            }

            Assert.Greater(maxRadius, 20.0, "External gear tip radius must be ~22mm for m=2, z=20.");
        }

        [Test]
        public void TestBuildGearMesh_ExternalSingleTooth_GeneratesValidMesh()
        {
            var profile = AlibreGearGenerator.GenerateExternalToothProfile(20, 2.0, 20.0, 0.0, false);
            var mesh = GearMesh3DBuilder.BuildGearMesh(profile, 20, 15.0, isInternal: false, isFullGear: false);

            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.Vertices.Count, 0);
            Assert.Greater(mesh.TriangleIndices.Count, 0);
            Assert.AreEqual(0, mesh.TriangleIndices.Count % 3);
            Assert.AreEqual(mesh.Vertices.Count, mesh.Normals.Count);
        }
    }
}
