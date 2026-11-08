using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>The bevel blank mesh with curved teeth: the valley profile swept through several sections along a spiral tooth trace.</summary>
    [TestFixture]
    public class TestSpiralBlankMesh
    {
        private static BevelBlankProfile Blank() => new BevelBlankProfile(18, 45, 10.16, 63.5);

        private static SpiralToothTrace Trace(BevelBlankProfile blank, double betaDeg, bool rightHand, double cutterFactor = 1.0)
        {
            var rm = blank.MeanConeDistance;
            return new SpiralToothTrace(rm, betaDeg * Math.PI / 180.0, cutterFactor * rm, rightHand);
        }

        private static GearMesh3DBuilder.Mesh3D Build(BevelBlankProfile blank, SpiralToothTrace trace, int sections = 0, int valleys = 3)
        {
            var sketch = new BevelToothSketch(blank, 20.0);
            return blank.BuildToothedMesh(sketch, valleys, 0.0, 0.0, 0.0, trace, sections);
        }

        private static double AngleAbout(GearPoint3D v) => Math.Atan2(v.Z, v.Y);

        private static double Wrap(double angle)
        {
            while (angle > Math.PI) angle -= 2.0 * Math.PI;
            while (angle < -Math.PI) angle += 2.0 * Math.PI;
            return angle;
        }

        private static GearPoint3D FirstVertexAt(GearMesh3DBuilder.Mesh3D mesh, BevelBlankPoint p)
        {
            return mesh.Vertices.First(v => Math.Abs(v.X - p.X) < 1e-9 && Math.Abs(Math.Sqrt(v.Y * v.Y + v.Z * v.Z) - p.Y) < 1e-9);
        }

        [Test]
        public void TestStraightTrace_GivesExactlyTheHeelAndToeMesh()
        {
            var blank = Blank();
            var withoutTrace = Build(blank, null);
            var withStraight = Build(blank, SpiralToothTrace.Straight);

            Assert.AreEqual(withoutTrace.Vertices.Count, withStraight.Vertices.Count);
            Assert.AreEqual(withoutTrace.TriangleIndices.Count, withStraight.TriangleIndices.Count);
            for (var i = 0; i < withoutTrace.Vertices.Count; i++)
            {
                Assert.AreEqual(withoutTrace.Vertices[i].X, withStraight.Vertices[i].X, 0.0);
                Assert.AreEqual(withoutTrace.Vertices[i].Y, withStraight.Vertices[i].Y, 0.0);
                Assert.AreEqual(withoutTrace.Vertices[i].Z, withStraight.Vertices[i].Z, 0.0);
            }

            CollectionAssert.AreEqual(withoutTrace.TriangleIndices, withStraight.TriangleIndices);
        }

        [Test]
        public void TestSpiralTrace_UsesTheDefaultSectionCount()
        {
            var blank = Blank();
            var trace = Trace(blank, 35.0, true);

            Assert.AreEqual(1, blank.SectionCountFor(SpiralToothTrace.Straight));
            Assert.AreEqual(BevelBlankProfile.DefaultSections, blank.SectionCountFor(trace));

            var straight = Build(blank, null);
            var spiral = Build(blank, trace);
            var sketchPoints = new BevelToothSketch(blank, 20.0).BuildOutline(3).Count;
            Assert.AreEqual(straight.Vertices.Count + (BevelBlankProfile.DefaultSections - 1) * 2 * sketchPoints, spiral.Vertices.Count,
                "Every extra section adds one more ring to the two bands that touch it.");
        }

        [Test]
        public void TestTwist_IsZeroAtTheMeanSection_AndOppositeAtHeelAndToe()
        {
            var blank = Blank();
            var trace = Trace(blank, 35.0, true);
            var mid = (1.0 + blank.ToeScale) / 2.0;

            Assert.AreEqual(0.0, blank.TwistAtScale(trace, mid), 1e-12);
            var heel = blank.TwistAtScale(trace, 1.0);
            var toe = blank.TwistAtScale(trace, blank.ToeScale);
            Assert.Less(heel * toe, 0.0);
            Assert.AreEqual(-blank.TwistAtScale(trace.WithHand(false), 1.0), heel, 1e-12);
        }

        [Test]
        public void TestMesh_HeelAndToeRingsAreTurnedByTheTraceTwist()
        {
            var blank = Blank();
            var trace = Trace(blank, 35.0, true);
            var straight = Build(blank, null);
            var spiral = Build(blank, trace);

            var heelTwist = blank.TwistAtScale(trace, 1.0);
            var toeTwist = blank.TwistAtScale(trace, blank.ToeScale);
            Assert.AreNotEqual(heelTwist, toeTwist);

            var dStraight = FirstVertexAt(straight, blank.PointD);
            var dSpiral = FirstVertexAt(spiral, blank.PointD);
            Assert.AreEqual(heelTwist, Wrap(AngleAbout(dSpiral) - AngleAbout(dStraight)), 1e-9, "Heel end.");

            var hStraight = FirstVertexAt(straight, blank.PointH);
            var hSpiral = FirstVertexAt(spiral, blank.PointH);
            Assert.AreEqual(toeTwist, Wrap(AngleAbout(hSpiral) - AngleAbout(hStraight)), 1e-9, "Toe end.");
        }

        [Test]
        public void TestMesh_OfOppositeHandsIsMirrored()
        {
            var blank = Blank();
            var right = Build(blank, Trace(blank, 35.0, true), valleys: 1);
            var left = Build(blank, Trace(blank, 35.0, false), valleys: 1);
            Assert.AreEqual(right.Vertices.Count, left.Vertices.Count);

            var rightHeel = Wrap(AngleAbout(FirstVertexAt(right, blank.PointD)));
            var leftHeel = Wrap(AngleAbout(FirstVertexAt(left, blank.PointD)));
            Assert.AreEqual(0.0, Wrap(rightHeel + leftHeel - 2.0 * AngleAbout(FirstVertexAt(Build(blank, null, valleys: 1), blank.PointD))), 1e-9,
                "The twist of the left hand is the negative of the right hand.");
        }

        [TestCase(null)]
        [TestCase(35.0)]
        [TestCase(0.0)]
        public void TestMesh_IsWatertight(double? betaDeg)
        {
            var blank = Blank();
            var trace = betaDeg.HasValue ? Trace(blank, betaDeg.Value, true) : null;
            var mesh = Build(blank, trace, valleys: 0);

            var ids = new Dictionary<string, int>();
            int Id(GearPoint3D v)
            {
                var key = $"{Math.Round(v.X, 6)}|{Math.Round(v.Y, 6)}|{Math.Round(v.Z, 6)}";
                if (!ids.TryGetValue(key, out var id))
                {
                    id = ids.Count;
                    ids[key] = id;
                }

                return id;
            }

            var edges = new Dictionary<long, int>();
            void Edge(int a, int b)
            {
                var lo = Math.Min(a, b);
                var hi = Math.Max(a, b);
                var key = ((long)lo << 32) | (uint)hi;
                edges.TryGetValue(key, out var count);
                edges[key] = count + 1;
            }

            var t = mesh.TriangleIndices;
            for (var i = 0; i + 2 < t.Count; i += 3)
            {
                var a = Id(mesh.Vertices[t[i]]);
                var b = Id(mesh.Vertices[t[i + 1]]);
                var c = Id(mesh.Vertices[t[i + 2]]);
                Edge(a, b);
                Edge(b, c);
                Edge(c, a);
            }

            var bad = edges.Count(e => e.Value != 2);
            Assert.AreEqual(0, bad, "Every edge is shared by exactly two triangles.");
        }

        [Test]
        public void TestLargeTwist_IsRejectedBeforeTheFlanksFold()
        {
            var blank = new BevelBlankProfile(60, 61, 2.0, 12.0);
            var rm = blank.MeanConeDistance;
            // A small cutter makes the trace curl up quickly, and one section cannot follow it.
            var trace = new SpiralToothTrace(rm, 60.0 * Math.PI / 180.0, 0.5 * rm, true);

            var ex = Assert.Throws<InvalidOperationException>(() => Build(blank, trace, sections: 1, valleys: 1));
            StringAssert.Contains("fold over", ex.Message);
        }

        [Test]
        public void TestCutterThatDoesNotReachTheFace_IsRejected()
        {
            var blank = new BevelBlankProfile(60, 61, 2.0, 12.0);
            var rm = blank.MeanConeDistance;
            var trace = new SpiralToothTrace(rm, 60.0 * Math.PI / 180.0, 0.35 * rm, true);

            Assert.Throws<ArgumentOutOfRangeException>(() => Build(blank, trace, valleys: 1));
        }
    }
}
