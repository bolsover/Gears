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

        [Test]
        public void TestTransform_TranslatesAndRotatesVerticesAndNormals()
        {
            var mesh = new GearMesh3DBuilder.Mesh3D();
            mesh.Vertices.Add(new GearPoint3D(10, 0, 0));
            mesh.TriangleIndices.Add(0);
            mesh.Normals.Add(new GearPoint3D(1, 0, 0));

            var transformed = GearMesh3DBuilder.Transform(mesh, offsetX: 5.0, offsetY: 2.0, offsetZ: 3.0, rotationAngleZRad: System.Math.PI / 2.0);

            Assert.IsNotNull(transformed);
            Assert.AreEqual(1, transformed.Vertices.Count);
            Assert.AreEqual(0.0 + 5.0, transformed.Vertices[0].X, 1e-6); // 10 * cos(90) = 0 -> +5 = 5
            Assert.AreEqual(10.0 + 2.0, transformed.Vertices[0].Y, 1e-6); // 10 * sin(90) = 10 -> +2 = 12
            Assert.AreEqual(3.0, transformed.Vertices[0].Z, 1e-6);

            // Normal rotated by 90 deg around Z -> (0, 1, 0)
            Assert.AreEqual(0.0, transformed.Normals[0].X, 1e-6);
            Assert.AreEqual(1.0, transformed.Normals[0].Y, 1e-6);
            Assert.AreEqual(0.0, transformed.Normals[0].Z, 1e-6);
        }

        [Test]
        public void TestCombine_CombinesTwoMeshesWithOffsetIndices()
        {
            var mesh1 = new GearMesh3DBuilder.Mesh3D();
            mesh1.Vertices.Add(new GearPoint3D(0, 0, 0));
            mesh1.Vertices.Add(new GearPoint3D(1, 0, 0));
            mesh1.Vertices.Add(new GearPoint3D(0, 1, 0));
            mesh1.TriangleIndices.AddRange(new[] { 0, 1, 2 });
            mesh1.Normals.Add(new GearPoint3D(0, 0, 1));
            mesh1.Normals.Add(new GearPoint3D(0, 0, 1));
            mesh1.Normals.Add(new GearPoint3D(0, 0, 1));

            var mesh2 = new GearMesh3DBuilder.Mesh3D();
            mesh2.Vertices.Add(new GearPoint3D(5, 5, 0));
            mesh2.Vertices.Add(new GearPoint3D(6, 5, 0));
            mesh2.Vertices.Add(new GearPoint3D(5, 6, 0));
            mesh2.TriangleIndices.AddRange(new[] { 0, 1, 2 });
            mesh2.Normals.Add(new GearPoint3D(0, 0, 1));
            mesh2.Normals.Add(new GearPoint3D(0, 0, 1));
            mesh2.Normals.Add(new GearPoint3D(0, 0, 1));

            var combined = GearMesh3DBuilder.Combine(mesh1, mesh2);

            Assert.AreEqual(6, combined.Vertices.Count);
            Assert.AreEqual(6, combined.TriangleIndices.Count);
            Assert.AreEqual(6, combined.Normals.Count);

            // Verify triangle indices of second mesh are offset by 3
            Assert.AreEqual(0, combined.TriangleIndices[0]);
            Assert.AreEqual(1, combined.TriangleIndices[1]);
            Assert.AreEqual(2, combined.TriangleIndices[2]);
            Assert.AreEqual(3, combined.TriangleIndices[3]);
            Assert.AreEqual(4, combined.TriangleIndices[4]);
            Assert.AreEqual(5, combined.TriangleIndices[5]);
        }

        [Test]
        public void TestCalculateMeshRotationAngle_InternalExternal_ReturnsZero()
        {
            // Adding a meshed external gear to an internal gear: internal gear has gap at angle 0, external gear has tooth at angle 0
            var angle1 = AlibreGearGenerator.CalculateMeshRotationAngle(isInternal1: true, isInternal2: false, z2: 20);
            Assert.AreEqual(0.0, angle1, 1e-9, "External gear inside internal gear must have 0 rotation angle (no 0.5 tooth offset).");

            var angle2 = AlibreGearGenerator.CalculateMeshRotationAngle(isInternal1: false, isInternal2: true, z2: 60);
            Assert.AreEqual(0.0, angle2, 1e-9, "Internal gear meshed with external gear must have 0 rotation angle.");
        }

        [Test]
        public void TestCalculateMeshRotationAngle_ExternalExternal_ReturnsPiPlusHalfToothPitch()
        {
            var z2 = 24;
            var expected = System.Math.PI + System.Math.PI / z2;
            var angle = AlibreGearGenerator.CalculateMeshRotationAngle(isInternal1: false, isInternal2: false, z2: z2);
            Assert.AreEqual(expected, angle, 1e-9, "External-external pair must rotate Gear 2 by (pi + pi/z2).");
        }

        [Test]
        public void TestGenerateExternalToothProfile_WithCustomClearance()
        {
            var z = 20;
            var m = 2.0;
            var alphaDeg = 20.0;
            var clearance = 0.6; // custom clearance

            var profile = AlibreGearGenerator.GenerateExternalToothProfile(z, m, alphaDeg, 0.0, false, null, clearance);

            Assert.IsNotNull(profile);
            Assert.AreEqual(clearance, profile.Parameters["clearance"], 1e-6);
            // pitchRadius = 20, addendumRadius = 22, dedendumRadius = 20 - 2 - 0.6 = 17.4
            Assert.AreEqual(17.4, profile.Parameters["dedendumRadius"], 1e-6);

            var toothOutline = AlibreGearGenerator.BuildToothOutline(profile, false);
            var fullGear = AlibreGearGenerator.BuildFullGear(z, toothOutline);
            var mesh = GearMesh3DBuilder.BuildGearMesh(profile, z, 15.0, isInternal: false, isFullGear: true);

            Assert.Greater(mesh.Vertices.Count, 0);
            Assert.Greater(mesh.TriangleIndices.Count, 0);
        }

        [Test]
        public void TestGenerateInternalToothProfile_WithCustomClearance()
        {
            var z = 30;
            var m = 2.0;
            var alphaDeg = 20.0;
            var clearance = 0.7; // custom clearance
            var rimThickness = 5.0;

            var profile = AlibreGearGenerator.GenerateInternalToothProfile(z, m, alphaDeg, rimThickness, 0.0, false, null, clearance);

            Assert.IsNotNull(profile);
            Assert.AreEqual(clearance, profile.Parameters["clearance"], 1e-6);
            // pitchRadius = 30, addendumRadius = 28, dedendumRadius = 30 + 2 + 0.7 = 32.7
            Assert.AreEqual(32.7, profile.Parameters["dedendumRadius"], 1e-6);

            var toothOutline = AlibreGearGenerator.BuildToothOutline(profile, true);
            var fullGear = AlibreGearGenerator.BuildFullGear(z, toothOutline);
            var mesh = GearMesh3DBuilder.BuildGearMesh(profile, z, 15.0, isInternal: true, isFullGear: true, rimThickness: rimThickness);

            Assert.Greater(mesh.Vertices.Count, 0);
            Assert.Greater(mesh.TriangleIndices.Count, 0);
        }

        [Test]
        public void TestGenerateExternalToothProfile_WithBacklash()
        {
            var z = 20;
            var m = 2.0;
            var alphaDeg = 20.0;
            var backlash = 0.08;

            var profile = AlibreGearGenerator.GenerateExternalToothProfile(z, m, alphaDeg, 0.0, false, null, null, backlash);

            Assert.IsNotNull(profile);
            Assert.AreEqual(backlash, profile.Parameters["backlash"], 1e-6);

            var toothOutline = AlibreGearGenerator.BuildToothOutline(profile, false);
            var fullGear = AlibreGearGenerator.BuildFullGear(z, toothOutline);
            var mesh = GearMesh3DBuilder.BuildGearMesh(profile, z, 15.0, isInternal: false, isFullGear: true);

            Assert.Greater(mesh.Vertices.Count, 0);
            Assert.Greater(mesh.TriangleIndices.Count, 0);
        }

        [Test]
        public void TestGenerateInternalToothProfile_WithBacklash()
        {
            var z = 30;
            var m = 2.0;
            var alphaDeg = 20.0;
            var backlash = 0.06;
            var rimThickness = 5.0;

            var profile = AlibreGearGenerator.GenerateInternalToothProfile(z, m, alphaDeg, rimThickness, 0.0, false, null, null, backlash);

            Assert.IsNotNull(profile);
            Assert.AreEqual(backlash, profile.Parameters["backlash"], 1e-6);

            var toothOutline = AlibreGearGenerator.BuildToothOutline(profile, true);
            var fullGear = AlibreGearGenerator.BuildFullGear(z, toothOutline);
            var mesh = GearMesh3DBuilder.BuildGearMesh(profile, z, 15.0, isInternal: true, isFullGear: true, rimThickness: rimThickness);

            Assert.Greater(mesh.Vertices.Count, 0);
            Assert.Greater(mesh.TriangleIndices.Count, 0);
        }
    }
}
