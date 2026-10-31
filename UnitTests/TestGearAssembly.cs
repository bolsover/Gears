using System;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class TestGearAssembly
    {
        private const double Tol = 1e-7;

        private static InvoluteGearComponent Gear(string id, int teeth, bool isInternal = false, double helix = 0.0, string direction = "R")
        {
            return new InvoluteGearComponent(id)
            {
                Teeth = teeth,
                Module = 2.0,
                PressureAngleDeg = 20.0,
                Clearance = 0.5,
                HelixAngleDeg = helix,
                HelicalDirection = direction,
                IsInternal = isInternal
            };
        }

        private static GearAssembly Pair(InvoluteGearComponent driver, InvoluteGearComponent driven, out Shaft shaft1, out Shaft shaft2)
        {
            var assembly = new GearAssembly();
            shaft1 = new Shaft("s1");
            shaft1.Mount(driver);
            shaft2 = new Shaft("s2");
            shaft2.Mount(driven);
            assembly.AddShaft(shaft1);
            assembly.AddShaft(shaft2);
            assembly.AddRelation(driver, driven);
            return assembly;
        }

        [Test]
        public void TestPrecision_RoundsToSevenDecimalPlaces()
        {
            Assert.AreEqual(7, GearPrecision.DecimalPlaces);
            Assert.AreEqual(1.2345679, GearPrecision.Round(1.23456789), 1e-12);
            Assert.AreEqual(-1.2345679, GearPrecision.Round(-1.23456789), 1e-12);
            Assert.AreEqual(0.0, GearPrecision.Round(4e-8), 0.0);
        }

        [Test]
        public void TestPrecision_NegativeZeroIsNormalised()
        {
            var rounded = GearPrecision.Round(-1e-9);
            Assert.AreEqual(0.0, rounded, 0.0);
            Assert.IsTrue(double.IsPositiveInfinity(1.0 / rounded), "Negative zero must be normalised to positive zero.");
        }

        [Test]
        public void TestTransform_RotationZ_MatchesGearPointRotate()
        {
            var angle = 0.7312;
            var expected = new GearPoint(3.2, -1.4).Rotate(angle);
            var actual = Transform3D.RotationZ(angle).TransformPoint(new Vector3D(3.2, -1.4, 9.0));

            Assert.AreEqual(expected.X, actual.X, 1e-12);
            Assert.AreEqual(expected.Y, actual.Y, 1e-12);
            Assert.AreEqual(9.0, actual.Z, 1e-12);
        }

        [Test]
        public void TestTransform_Compose_AppliesInnerFirst()
        {
            var composed = Transform3D.Translate(new Vector3D(1, 2, 3)).Compose(Transform3D.RotationZ(Math.PI / 2));
            var p = composed.TransformPoint(new Vector3D(1, 0, 0));

            // (1, 0, 0) rotated by 90 degrees is (0, 1, 0); translating by (1, 2, 3) gives (1, 3, 3).
            Assert.AreEqual(1.0, p.X, 1e-12);
            Assert.AreEqual(3.0, p.Y, 1e-12);
            Assert.AreEqual(3.0, p.Z, 1e-12);
        }

        [Test]
        public void TestPlacement_AxisAlongX_MapsLocalFrameToWorld()
        {
            var placement = new Placement3D(new Vector3D(10, 0, 0), Vector3D.UnitX, Vector3D.UnitY);
            var transform = placement.ToTransform();

            var alongAxis = transform.TransformPoint(new Vector3D(0, 0, 5));
            Assert.AreEqual(15.0, alongAxis.X, 1e-12);
            Assert.AreEqual(0.0, alongAxis.Y, 1e-12);

            var reference = transform.TransformPoint(new Vector3D(1, 0, 0));
            Assert.AreEqual(10.0, reference.X, 1e-12);
            Assert.AreEqual(1.0, reference.Y, 1e-12);
        }

        [Test]
        public void TestPlacement_ReferenceParallelToAxis_StillGivesOrthonormalFrame()
        {
            var placement = new Placement3D(Vector3D.Zero, Vector3D.UnitZ, Vector3D.UnitZ);
            Assert.AreEqual(1.0, placement.XAxis.Length, 1e-12);
            Assert.AreEqual(0.0, placement.XAxis.Dot(placement.ZAxis), 1e-12);
            Assert.AreEqual(0.0, placement.YAxis.Dot(placement.XAxis), 1e-12);
        }

        [Test]
        public void TestParallelSolver_ExternalPair_CentreDistanceIsSumOfPitchRadii()
        {
            var assembly = Pair(Gear("g1", 20), Gear("g2", 30), out _, out var shaft2);
            assembly.Solve();

            var result = assembly.Relations[0].Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(50.0, result.CentreDistance, Tol);
            Assert.AreEqual(1.5, result.GearRatio, Tol);
            Assert.AreEqual(50.0, shaft2.Placement.Origin.X, Tol);
            Assert.AreEqual(0.0, shaft2.Placement.Origin.Y, Tol);
            Assert.AreEqual(0.0, shaft2.Placement.Origin.Z, Tol);
        }

        [Test]
        public void TestParallelSolver_InternalPair_CentreDistanceIsDifferenceOfPitchRadii()
        {
            var assembly = Pair(Gear("g1", 20), Gear("g2", 40, isInternal: true), out _, out var shaft2);
            assembly.Solve();

            Assert.AreEqual(20.0, assembly.Relations[0].Result.CentreDistance, Tol);
            Assert.AreEqual(20.0, shaft2.Placement.Origin.X, Tol);
        }

        [TestCase(20, 30, false)]
        [TestCase(21, 30, false)]
        [TestCase(20, 41, true)]
        [TestCase(20, 40, true)]
        public void TestParallelSolver_DrivenPhase_MatchesExistingMeshRotation(int z1, int z2, bool internal2)
        {
            var assembly = Pair(Gear("g1", z1), Gear("g2", z2, internal2), out _, out _);
            assembly.Solve();

            var expected = AlibreGearGenerator.CalculateMeshRotationAngle(false, internal2, z1, z2);
            Assert.AreEqual(expected, assembly.Relations[0].Result.DrivenPhaseRad, Tol);
        }

        [Test]
        public void TestParallelSolver_DrivenPhase_FollowsDriverRotation()
        {
            var driver = Gear("g1", 20);
            var driven = Gear("g2", 30);
            var assembly = Pair(driver, driven, out var shaft1, out var shaft2);

            assembly.Solve();
            var basePhase = shaft2.Mounts[0].PhaseRad;

            shaft1.Mounts[0].PhaseRad = 2.0 * Math.PI / 20.0; // one tooth pitch of the driver
            assembly.Solve();

            Assert.AreEqual(basePhase - 2.0 * Math.PI / 30.0, shaft2.Mounts[0].PhaseRad, 2 * Tol,
                "One driver tooth pitch must advance the external driven gear by one tooth pitch in the opposite direction.");
        }

        [Test]
        public void TestParallelSolver_ModuleMismatch_Throws()
        {
            var g2 = Gear("g2", 30);
            g2.Module = 3.0;
            var assembly = Pair(Gear("g1", 20), g2, out _, out _);

            var ex = Assert.Throws<InvalidOperationException>(() => assembly.Solve());
            StringAssert.Contains("modules differ", ex.Message);
        }

        [Test]
        public void TestParallelSolver_PressureAngleMismatch_Throws()
        {
            var g2 = Gear("g2", 30);
            g2.PressureAngleDeg = 25.0;
            var assembly = Pair(Gear("g1", 20), g2, out _, out _);

            var ex = Assert.Throws<InvalidOperationException>(() => assembly.Solve());
            StringAssert.Contains("pressure angles differ", ex.Message);
        }

        [Test]
        public void TestAssembly_ThreeGearTrain_PlacesShaftsInSequenceAlongX()
        {
            var g1 = Gear("g1", 20);
            var g2 = Gear("g2", 30);
            var g3 = Gear("g3", 25);
            var assembly = new GearAssembly();
            var s1 = assembly.AddShaft(new Shaft("s1"));
            var s2 = assembly.AddShaft(new Shaft("s2"));
            var s3 = assembly.AddShaft(new Shaft("s3"));
            s1.Mount(g1);
            s2.Mount(g2);
            s3.Mount(g3);

            // Added out of dependency order on purpose.
            assembly.AddRelation(g2, g3);
            assembly.AddRelation(g1, g2);
            assembly.Solve();

            Assert.AreEqual(50.0, s2.Placement.Origin.X, Tol);
            Assert.AreEqual(105.0, s3.Placement.Origin.X, Tol);
            Assert.IsTrue(assembly.Relations.All(r => r.Result != null));
        }

        [Test]
        public void TestAssembly_AxialOffsets_AlignGearPlanes()
        {
            var g1 = Gear("g1", 20);
            var g2 = Gear("g2", 30);
            var assembly = new GearAssembly();
            var s1 = assembly.AddShaft(new Shaft("s1"));
            var s2 = assembly.AddShaft(new Shaft("s2"));
            s1.Mount(g1, 3.0);
            s2.Mount(g2, 5.0);
            assembly.AddRelation(g1, g2);
            assembly.Solve();

            Assert.AreEqual(-2.0, s2.Placement.Origin.Z, Tol, "Driver mid-plane at 3 and driven offset 5 means the driven shaft sits at Z = -2.");
        }

        [Test]
        public void TestAssembly_Solve_IsRepeatable()
        {
            var assembly = Pair(Gear("g1", 20), Gear("g2", 30), out _, out var shaft2);
            assembly.Solve();
            var first = shaft2.Placement.Origin;
            assembly.Solve();

            Assert.AreEqual(first.X, shaft2.Placement.Origin.X, 0.0);
        }

        [Test]
        public void TestAssembly_AddRelation_Validation()
        {
            var g1 = Gear("g1", 20);
            var g2 = Gear("g2", 30);
            var g3 = Gear("g3", 40);
            var orphan = Gear("orphan", 20);
            var assembly = new GearAssembly();
            var s1 = assembly.AddShaft(new Shaft("s1"));
            var s2 = assembly.AddShaft(new Shaft("s2"));
            s1.Mount(g1);
            s1.Mount(g3, 20.0);
            s2.Mount(g2);

            Assert.Throws<InvalidOperationException>(() => assembly.AddRelation(g1, g3), "Gears on the same shaft cannot mesh.");
            Assert.Throws<InvalidOperationException>(() => assembly.AddRelation(g1, orphan), "An unmounted gear cannot mesh.");
            Assert.Throws<ArgumentException>(() => assembly.AddRelation(g1, g1), "A gear cannot mesh with itself.");

            assembly.AddRelation(g1, g2);
            Assert.Throws<InvalidOperationException>(() => assembly.AddRelation(g3, g2), "A gear can be driven by one gear only.");
        }

        [Test]
        public void TestAssembly_AddShaft_RejectsDuplicates()
        {
            var g1 = Gear("g1", 20);
            var assembly = new GearAssembly();
            var s1 = new Shaft("s1");
            s1.Mount(g1);
            assembly.AddShaft(s1);

            Assert.Throws<ArgumentException>(() => assembly.AddShaft(new Shaft("s1")), "Duplicate shaft id.");

            var s2 = new Shaft("s2");
            s2.Mount(g1);
            Assert.Throws<ArgumentException>(() => assembly.AddShaft(s2), "A gear can only be on one shaft.");
        }

        [Test]
        public void TestAssembly_OverConstrainedShaft_Throws()
        {
            var g1 = Gear("g1", 20);
            var g2 = Gear("g2", 30);
            var g3 = Gear("g3", 40);
            var assembly = new GearAssembly();
            var s1 = assembly.AddShaft(new Shaft("s1"));
            var s2 = assembly.AddShaft(new Shaft("s2"));
            s1.Mount(g1);
            s2.Mount(g2);
            s2.Mount(g3, 20.0);
            assembly.AddRelation(g1, g2);
            assembly.AddRelation(g1, g3);

            var ex = Assert.Throws<InvalidOperationException>(() => assembly.Solve());
            StringAssert.Contains("over-constrained", ex.Message);
        }

        [Test]
        public void TestAssembly_DriverShaftNotReachable_Throws()
        {
            var g1 = Gear("g1", 20);
            var g2 = Gear("g2", 30);
            var assembly = new GearAssembly();
            var s2 = assembly.AddShaft(new Shaft("s2"));
            var s1 = assembly.AddShaft(new Shaft("s1"));
            s1.Mount(g1);
            s2.Mount(g2);
            assembly.AddRelation(g1, g2);

            Assert.Throws<InvalidOperationException>(() => assembly.Solve());
        }

        [Test]
        public void TestComponent_InvalidParameters_Throw()
        {
            Assert.Throws<ArgumentException>(() => new InvoluteGearComponent(" "));

            var gear = Gear("g", 20);
            gear.Teeth = 0;
            Assert.Throws<InvalidOperationException>(() => gear.BuildGeometry());

            gear = Gear("g", 20);
            gear.FaceWidth = 0;
            Assert.Throws<InvalidOperationException>(() => gear.BuildGeometry());
        }

        [Test]
        public void TestSceneBuilder_ProducesGeometryRoundedToSevenDecimalPlaces()
        {
            var assembly = Pair(Gear("g1", 20), Gear("g2", 30), out _, out _);
            var scene = GearAssemblySceneBuilder.Build(assembly);

            Assert.AreEqual(2, scene.Items.Count);

            foreach (var item in scene.Items)
            {
                Assert.IsNotEmpty(item.Mesh.Vertices);
                Assert.IsNotEmpty(item.Polylines);

                foreach (var v in item.Mesh.Vertices)
                {
                    AssertSevenDecimalPlaces(v.X);
                    AssertSevenDecimalPlaces(v.Y);
                    AssertSevenDecimalPlaces(v.Z);
                }

                foreach (var p in item.Polylines.SelectMany(l => l))
                {
                    AssertSevenDecimalPlaces(p.X);
                    AssertSevenDecimalPlaces(p.Y);
                }
            }
        }

        [Test]
        public void TestSceneBuilder_CombinedMesh_ContainsEveryComponent()
        {
            var assembly = Pair(Gear("g1", 20), Gear("g2", 30), out _, out _);
            var scene = GearAssemblySceneBuilder.Build(assembly);
            var combined = scene.CombinedMesh();

            Assert.AreEqual(scene.Items.Sum(i => i.Mesh.Vertices.Count), combined.Vertices.Count);
            Assert.AreEqual(scene.Items.Sum(i => i.Mesh.TriangleIndices.Count), combined.TriangleIndices.Count);
            Assert.AreEqual(scene.Items.Sum(i => i.Polylines.Count), scene.CombinedPolylines().Count);
        }

        [Test]
        public void TestSceneBuilder_ShaftAxisAlongX_ExtrudesAlongWorldX()
        {
            var gear = Gear("g1", 20);
            gear.FaceWidth = 15.0;
            var assembly = new GearAssembly();
            var shaft = new Shaft("s1", new Placement3D(new Vector3D(10, 0, 0), Vector3D.UnitX, Vector3D.UnitY));
            shaft.Mount(gear);
            assembly.AddShaft(shaft);

            var scene = GearAssemblySceneBuilder.Build(assembly);
            var vertices = scene.Items[0].Mesh.Vertices;

            var xSpan = vertices.Max(v => v.X) - vertices.Min(v => v.X);
            Assert.AreEqual(15.0, xSpan, 1e-6, "The face width must run along the shaft axis (world X).");
            var centre = (vertices.Max(v => v.X) + vertices.Min(v => v.X)) / 2.0;
            Assert.AreEqual(10.0, centre, 1e-6, "The gear mid-plane must sit at the shaft origin (world X = 10).");

            var radial = Math.Max(
                vertices.Max(v => Math.Abs(v.Y)),
                vertices.Max(v => Math.Abs(v.Z)));
            Assert.AreEqual(21.0, radial, 1.0, "The tip radius of a 20 tooth module 2 gear is about 22 mm.");
        }

        [Test]
        public void TestSceneBuilder_DoesNotMutateComponentGeometry()
        {
            var gear = Gear("g1", 20);
            var before = gear.BuildGeometry().Mesh.Vertices.Select(v => v.X).ToList();

            var assembly = Pair(gear, Gear("g2", 30), out _, out _);
            GearAssemblySceneBuilder.Build(assembly);

            var after = gear.BuildGeometry().Mesh.Vertices.Select(v => v.X).ToList();
            CollectionAssert.AreEqual(before, after);
        }

        private static void AssertSevenDecimalPlaces(double value)
        {
            Assert.AreEqual(Math.Round(value, 7), value, 1e-12, "Value " + value + " is not rounded to 7 decimal places.");
        }
    }
}
