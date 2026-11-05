using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>The closed sketch of an 18 tooth bevel gear blank (mating with 45 teeth at 90 degrees) and its revolved solid.</summary>
    [TestFixture]
    public class TestBevelBlankProfile
    {
        private const double Module = 10.16;
        private const double FaceWidth = 63.5;

        private static BevelBlankProfile Sample() => new BevelBlankProfile(18, 45, Module, FaceWidth);

        [Test]
        public void TestConeAngles()
        {
            var profile = Sample();

            Assert.AreEqual(21.801409, profile.PitchConeAngleDeg, 1e-5);
            Assert.AreEqual(68.198591, profile.BackConeAngleDeg, 1e-5);
            Assert.AreEqual(90.0, profile.PitchConeAngleDeg + profile.BackConeAngleDeg, 1e-9);
            Assert.AreEqual(1.0 * Module, profile.Addendum, 1e-9);
            Assert.AreEqual(1.25 * Module, profile.Dedendum, 1e-9);
        }

        [Test]
        public void TestReferencePoints()
        {
            var profile = Sample();

            Assert.AreEqual(0.0, profile.PointA.X, 1e-9);
            Assert.AreEqual(91.44, profile.PointA.Y, 1e-9, "Half the pitch diameter.");
            Assert.AreEqual(-228.6, profile.PointB.X, 1e-6);
            Assert.AreEqual(0.0, profile.PointB.Y, 1e-9);
            Assert.AreEqual(36.576, profile.PointC.X, 1e-6);
            Assert.AreEqual(0.0, profile.PointC.Y, 1e-9);

            // refb meets the X axis at the pitch cone angle; refc is perpendicular to refb.
            var refb = profile.PointA - profile.PointB;
            var refc = profile.PointA - profile.PointC;
            Assert.AreEqual(profile.PitchConeAngleDeg, Math.Atan2(refb.Y, refb.X) * 180.0 / Math.PI, 1e-9);
            Assert.AreEqual(0.0, refb.X * refc.X + refb.Y * refc.Y, 1e-6, "refc is perpendicular to refb.");
        }

        [Test]
        public void TestDedendumAndAddendumPointsLieOnRefc()
        {
            var profile = Sample();

            Assert.AreEqual(12.7, (profile.PointD - profile.PointA).Length, 1e-9, "pointD is a dedendum from pointA.");
            Assert.AreEqual(22.86, (profile.PointE - profile.PointD).Length, 1e-9, "pointE is a whole depth from pointD.");
            Assert.AreEqual(10.16, (profile.PointE - profile.PointA).Length, 1e-9, "pointE is an addendum beyond pointA.");

            var refc = profile.PointC - profile.PointA;
            Assert.AreEqual(0.0, Cross(refc, profile.PointD - profile.PointA), 1e-6, "pointD is on refc.");
            Assert.AreEqual(0.0, Cross(refc, profile.PointE - profile.PointA), 1e-6, "pointE is on refc extended.");
            Assert.Greater(Dot(refc, profile.PointD - profile.PointA), 0.0, "pointD lies towards pointC.");
            Assert.Less(Dot(refc, profile.PointE - profile.PointA), 0.0, "pointE lies away from pointC.");

            Assert.AreEqual(0.0, Cross(profile.PointD - profile.PointB, profile.PointH - profile.PointB), 1e-6, "pointH is on the dedendum line refd.");
            Assert.AreEqual(0.0, Cross(profile.PointE - profile.PointB, profile.PointG - profile.PointB), 1e-6, "pointG is on the addendum line refe.");
        }

        [Test]
        public void TestSketchLines()
        {
            var profile = Sample();

            Assert.AreEqual(profile.PointD.X, profile.PointF.X, 1e-12, "lineF is vertical.");
            Assert.AreEqual(0.0, profile.PointF.Y, 1e-12);
            Assert.AreEqual(FaceWidth, (profile.PointG - profile.PointE).Length, 1e-9, "lineH is a face width long.");
            Assert.Less(profile.PointG.X, profile.PointE.X, "lineH runs from pointE towards pointB.");

            var refb = profile.PointA - profile.PointB;
            var lineI = profile.PointH - profile.PointG;
            Assert.AreEqual(0.0, Dot(refb, lineI), 1e-6, "lineI is perpendicular to refb.");
            Assert.Greater(lineI.Length, 0.0);

            Assert.AreEqual(profile.PointH.X, profile.PointI.X, 1e-12, "lineJ is vertical.");
            Assert.AreEqual(0.0, profile.PointI.Y, 1e-12);
            Assert.AreEqual(profile.PointF.Y, profile.PointI.Y, 1e-12, "lineK is horizontal.");
        }

        [Test]
        public void TestSketchIsClosedAndSimple()
        {
            var outline = Sample().Outline;
            Assert.AreEqual(6, outline.Count);
            Assert.AreEqual(6, Sample().Lines.Count());

            var area = 0.0;
            for (var i = 0; i < outline.Count; i++)
            {
                area += Cross(outline[i], outline[(i + 1) % outline.Count]);
            }

            Assert.Greater(area, 0.0, "The sketch runs counter-clockwise.");
            Assert.That(outline.All(p => p.Y >= 0.0), "The sketch lies on one side of the axis of revolution.");
        }

        [Test]
        public void TestSolid_IsClosedWithOutwardFacesAndTheVolumeOfTheRevolvedSketch()
        {
            var profile = Sample();
            var mesh = profile.BuildMesh(360);

            var volume = SignedVolume(mesh);
            Assert.Greater(volume, 0.0, "Faces must point outwards.");

            // Pappus: the volume of a revolved area is the area times the path of its centroid.
            var outline = profile.Outline;
            double area = 0.0, moment = 0.0;
            for (var i = 0; i < outline.Count; i++)
            {
                var p = outline[i];
                var q = outline[(i + 1) % outline.Count];
                var c = p.X * q.Y - q.X * p.Y;
                area += c;
                moment += (p.Y + q.Y) * c;
            }

            area /= 2.0;
            var expected = Math.PI * moment / 3.0; // 2 pi * (centroid y) * area = 2 pi * moment / 6
            Assert.AreEqual(expected, volume, expected * 1e-3);
            AssertMeshIsClosed(mesh);
        }

        [Test]
        public void TestSolid_VerticesStayWithinTheSketch()
        {
            var profile = Sample();
            var mesh = profile.BuildMesh(48);

            var maxRadius = profile.Outline.Max(p => p.Y);
            Assert.AreEqual(maxRadius, mesh.Vertices.Max(v => Math.Sqrt(v.Y * v.Y + v.Z * v.Z)), 1e-9);
            Assert.AreEqual(profile.PointG.X, mesh.Vertices.Min(v => v.X), 1e-9, "The toe end is at pointG, the tip of the toe face.");
            Assert.AreEqual(profile.PointD.X, mesh.Vertices.Max(v => v.X), 1e-9, "The back end is at pointD/pointF.");
            Assert.AreEqual(mesh.Vertices.Count, mesh.Normals.Count);
        }

        [TestCase(20, 20, 90.0)]
        [TestCase(40, 20, 90.0)]
        [TestCase(20, 36, 75.0)]
        public void TestOtherGears_AreClosedSolids(int teeth, int mateTeeth, double shaftAngleDeg)
        {
            var profile = new BevelBlankProfile(teeth, mateTeeth, 3.0, 15.0, shaftAngleDeg);
            var mesh = profile.BuildMesh(36);

            Assert.Greater(SignedVolume(mesh), 0.0);
            AssertMeshIsClosed(mesh);
        }

        [Test]
        public void TestEqualGears_HavePitchConeOf45Degrees()
        {
            Assert.AreEqual(45.0, new BevelBlankProfile(30, 30, 2.0, 10.0).PitchConeAngleDeg, 1e-9);
        }

        [Test]
        public void TestBadInputs_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new BevelBlankProfile(0, 45, Module, FaceWidth));
            Assert.Throws<ArgumentException>(() => new BevelBlankProfile(18, 0, Module, FaceWidth));
            Assert.Throws<ArgumentException>(() => new BevelBlankProfile(18, 45, 0.0, FaceWidth));
            Assert.Throws<ArgumentException>(() => new BevelBlankProfile(18, 45, Module, 0.0));
            Assert.Throws<ArgumentException>(() => new BevelBlankProfile(18, 45, Module, FaceWidth, 180.0));
            Assert.Throws<InvalidOperationException>(() => new BevelBlankProfile(18, 45, Module, 1000.0), "A face width longer than the cone leaves no toe.");
            Assert.Throws<ArgumentException>(() => Sample().BuildMesh(2));
        }

        private static double Cross(BevelBlankPoint a, BevelBlankPoint b) => a.X * b.Y - a.Y * b.X;

        private static double Dot(BevelBlankPoint a, BevelBlankPoint b) => a.X * b.X + a.Y * b.Y;

        private static double SignedVolume(GearMesh3DBuilder.Mesh3D mesh)
        {
            var volume = 0.0;
            for (var i = 0; i + 2 < mesh.TriangleIndices.Count; i += 3)
            {
                var a = mesh.Vertices[mesh.TriangleIndices[i]];
                var b = mesh.Vertices[mesh.TriangleIndices[i + 1]];
                var c = mesh.Vertices[mesh.TriangleIndices[i + 2]];
                volume += (a.X * (b.Y * c.Z - b.Z * c.Y) - a.Y * (b.X * c.Z - b.Z * c.X) + a.Z * (b.X * c.Y - b.Y * c.X)) / 6.0;
            }

            return volume;
        }

        /// <summary>Every triangle edge is shared by exactly two triangles (compared by position), so the solid has no holes.</summary>
        private static void AssertMeshIsClosed(GearMesh3DBuilder.Mesh3D mesh)
        {
            string Key(Bolsover.Gears.Model.GearPoint3D v) => Math.Round(v.X, 6) + "|" + Math.Round(v.Y, 6) + "|" + Math.Round(v.Z, 6);
            var edges = new Dictionary<string, int>();
            for (var i = 0; i + 2 < mesh.TriangleIndices.Count; i += 3)
            {
                var keys = new[]
                {
                    Key(mesh.Vertices[mesh.TriangleIndices[i]]),
                    Key(mesh.Vertices[mesh.TriangleIndices[i + 1]]),
                    Key(mesh.Vertices[mesh.TriangleIndices[i + 2]])
                };
                for (var e = 0; e < 3; e++)
                {
                    var a = keys[e];
                    var b = keys[(e + 1) % 3];
                    if (a == b) continue;
                    var edge = string.CompareOrdinal(a, b) < 0 ? a + ">" + b : b + ">" + a;
                    edges[edge] = edges.TryGetValue(edge, out var count) ? count + 1 : 1;
                }
            }

            Assert.IsEmpty(edges.Where(kv => kv.Value != 2).Take(5).ToList(), "Every edge must be shared by two triangles.");
        }
    }
}
