using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Alibre;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Helical gears on crossed shafts: per-gear transverse geometry, the shaft angle rule and the checks that keep the
    /// parallel and the crossed mesh apart.
    /// </summary>
    [TestFixture]
    public class TestCrossedHelicalMesh
    {
        private const double Module = 2.0;
        private const double Pressure = 20.0;
        private const double Tol = 1e-6;

        private static InvoluteGearComponent Gear(string id, int teeth, double helix, string hand = "R")
        {
            return new InvoluteGearComponent(id)
            {
                Teeth = teeth, Module = Module, PressureAngleDeg = Pressure, FaceWidth = 12.0, HelixAngleDeg = helix, HelicalDirection = hand
            };
        }

        private static double Mt(double helix) => Module / Math.Cos(helix * Math.PI / 180.0);

        // ----- Per-gear geometry -----

        [Test]
        public void TestReferencePair_HasEqualPitchDiametersAndTheirMeanAsCentreDistance()
        {
            var d1 = HelicalPairGeometry.PitchDiameter(24, Module, 26.6);
            var d2 = HelicalPairGeometry.PitchDiameter(12, Module, 63.4);
            Console.WriteLine($"[DEBUG_LOG] d1 {d1:0.0000} d2 {d2:0.0000}");

            Assert.AreEqual(24 * Mt(26.6), d1, 1e-12);
            Assert.AreEqual(12 * Mt(63.4), d2, 1e-12);
            Assert.AreEqual(53.67, d1, 0.05);
            Assert.AreEqual(d1, d2, 0.1, "The pitch diameters of the reference pair are nearly equal (26.6 and 63.4 are rounded angles) while the tooth counts are not.");
            Assert.AreEqual((d1 + d2) / 2.0, HelicalPairGeometry.StandardCentreDistance(24, 26.6, false, 12, 63.4, false, Module), 1e-12);
        }

        [Test]
        public void TestStandardCentreDistance_ForEqualHelixAnglesIsTheSingleModuleFormula()
        {
            Assert.AreEqual(Mt(20.0) * (30 + 45) / 2.0, HelicalPairGeometry.StandardCentreDistance(30, 20.0, false, 45, 20.0, false, Module), 0.0);
            Assert.AreEqual(Mt(20.0) * (45 - 30) / 2.0, HelicalPairGeometry.StandardCentreDistance(30, 20.0, false, 45, 20.0, true, Module), 0.0);
            Assert.AreEqual(Mt(20.0) * (45 - 30) / 2.0, HelicalPairGeometry.StandardCentreDistance(45, 20.0, true, 30, 20.0, false, Module), 0.0);
            Assert.AreEqual(Module * 50 / 2.0, HelicalPairGeometry.StandardCentreDistance(20, 0.0, false, 30, 0.0, false, Module), 0.0);
        }

        [Test]
        public void TestCalculatePair_UsesTheOwnHelixAngleOfEachGear()
        {
            var pair = GearParameterCalculator.CalculatePair(
                24, Module, Pressure, 12.0, 0.0, false, 10.0,
                12, 12.0, 0.0, false, 10.0,
                null, null, null, null, 26.6, 63.4, "R", "R");

            var expected = (24 * Mt(26.6) + 12 * Mt(63.4)) / 2.0;
            var singleModule = Mt(26.6) * 36 / 2.0;
            Assert.AreEqual(expected, pair.StandardCentreDistance, 1e-9);
            Assert.Greater(Math.Abs(singleModule - expected), 1.0, "Gear 1's transverse module alone would give a clearly wrong distance.");
        }

        [Test]
        public void TestCalculatePair_ForEqualHelixAnglesIsUnchanged()
        {
            var pair = GearParameterCalculator.CalculatePair(
                20, Module, Pressure, 12.0, 0.0, false, 10.0,
                30, 12.0, 0.0, false, 10.0,
                null, null, null, null, 20.0, 20.0, "R", "L");

            Assert.AreEqual(Mt(20.0) * 50 / 2.0, pair.StandardCentreDistance, 0.0);
            Assert.AreEqual(Mt(20.0) * 50 / 2.0, pair.WorkingCentreDistance, 1e-9);
            Assert.IsFalse(pair.IsCrossedAxis);
            Assert.AreEqual(0.0, pair.ShaftAngleDeg);
        }

        // ----- Shaft angle rule -----

        [TestCase(26.6, "R", 63.4, "R", 90.0)]
        [TestCase(20.0, "R", 30.0, "R", 50.0)]
        [TestCase(20.0, "R", 30.0, "L", 10.0)]
        [TestCase(30.0, "L", 20.0, "R", 10.0)]
        [TestCase(45.0, "R", 45.0, "R", 90.0)]
        [TestCase(45.0, "R", 45.0, "L", 0.0)]
        [TestCase(0.0, "R", 30.0, "R", 30.0)]
        public void TestExpectedShaftAngle(double beta1, string hand1, double beta2, string hand2, double expected)
        {
            Assert.AreEqual(expected, HelicalPairGeometry.ExpectedShaftAngleDeg(beta1, hand1, beta2, hand2), 1e-12);
        }

        [Test]
        public void TestCheckShaftAngle_AcceptsTheExpectedAngleWithinTolerance()
        {
            Assert.DoesNotThrow(() => HelicalPairGeometry.CheckShaftAngle("A", "B", 90.0, 26.6, "R", 63.4, "R"));
            Assert.DoesNotThrow(() => HelicalPairGeometry.CheckShaftAngle("A", "B", 90.005, 26.6, "R", 63.4, "R"));
        }

        [Test]
        public void TestCheckShaftAngle_RejectsAWrongAngleAndNamesTheExpectedOne()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => HelicalPairGeometry.CheckShaftAngle("A", "B", 80.0, 26.6, "R", 63.4, "R"));
            StringAssert.Contains("90", ex.Message);
            StringAssert.Contains("80", ex.Message);
            Assert.Throws<InvalidOperationException>(() => HelicalPairGeometry.CheckShaftAngle("A", "B", 90.1, 26.6, "R", 63.4, "R"));
            Assert.Throws<InvalidOperationException>(() => HelicalPairGeometry.CheckShaftAngle("A", "B", double.NaN, 26.6, "R", 63.4, "R"));
            var opposite = Assert.Throws<InvalidOperationException>(() => HelicalPairGeometry.CheckShaftAngle("A", "B", 90.0, 26.6, "R", 63.4, "L"));
            StringAssert.Contains("36.8", opposite.Message);
        }

        // ----- Crossed pair calculation -----

        private static CalculatedGearPairData Reference(double shaftAngle = 90.0)
        {
            return GearParameterCalculator.CalculateCrossedPair(
                24, Module, Pressure, 12.0, 0.0, false,
                12, 12.0, 0.0, false,
                shaftAngle,
                null, null, null, null, 26.6, 63.4, "R", "R");
        }

        [Test]
        public void TestCalculateCrossedPair_ReferenceCase()
        {
            var pair = Reference();

            Assert.IsTrue(pair.IsCrossedAxis);
            Assert.AreEqual(90.0, pair.ShaftAngleDeg);
            Assert.AreEqual(24 * Mt(26.6), pair.Gear1.PitchDiameter, 1e-9);
            Assert.AreEqual(12 * Mt(63.4), pair.Gear2.PitchDiameter, 1e-9);
            Assert.AreEqual(Mt(26.6), pair.Gear1.RadialModule, 1e-12);
            Assert.AreEqual(Mt(63.4), pair.Gear2.RadialModule, 1e-12);
            Assert.AreEqual((pair.Gear1.PitchDiameter + pair.Gear2.PitchDiameter) / 2.0, pair.WorkingCentreDistance, 1e-9);
            Assert.AreEqual(pair.WorkingCentreDistance, pair.StandardCentreDistance, 1e-12);
            Assert.AreEqual(0.5, pair.GearRatio, 1e-12, "The ratio comes from the teeth, not from the equal pitch diameters.");
            Assert.AreEqual(pair.Gear1.PitchDiameter, pair.Gear1WorkingPitchDiameter, 1e-12);
            Assert.AreEqual(pair.Gear2.PitchDiameter, pair.Gear2WorkingPitchDiameter, 1e-12);
        }

        [Test]
        public void TestCalculateCrossedPair_HasPointContactAndNoContactRatio()
        {
            var pair = Reference();

            Assert.AreEqual(0.0, pair.ContactRatio);
            Assert.AreEqual(0.0, pair.AxialContactRatio);
            Assert.IsTrue(pair.ParameterRows.Any(r => r.ParameterName.IndexOf("Contact", StringComparison.OrdinalIgnoreCase) >= 0
                                                       && r.Unit.IndexOf("point contact", StringComparison.OrdinalIgnoreCase) >= 0),
                "The report says that the contact is a point.");
            Assert.IsFalse(pair.ParameterRows.Any(r => r.ParameterName == "Contact Ratio" || r.ParameterName == "Total Contact Ratio"));
        }

        [Test]
        public void TestCalculateCrossedPair_ListsBothTransverseModulesDiametersAndTheShaftAngle()
        {
            var rows = Reference().ParameterRows;
            double Value(string name) => rows.Single(r => r.ParameterName == name).Value;

            Assert.AreEqual(Mt(26.6), Value("Gear 1 Radial (Transverse) Module"), 1e-12);
            Assert.AreEqual(Mt(63.4), Value("Gear 2 Radial (Transverse) Module"), 1e-12);
            Assert.AreEqual(24 * Mt(26.6), Value("Gear 1 Pitch Diameter"), 1e-9);
            Assert.AreEqual(12 * Mt(63.4), Value("Gear 2 Pitch Diameter"), 1e-9);
            Assert.AreEqual(90.0, Value("Shaft Angle"), 1e-12);
            Assert.AreEqual(Reference().WorkingCentreDistance, Value("Centre Distance"), 1e-12);
        }

        [Test]
        public void TestCalculateCrossedPair_RejectsInternalGears()
        {
            Assert.Throws<InvalidOperationException>(() => GearParameterCalculator.CalculateCrossedPair(
                24, Module, Pressure, 12.0, 0.0, true, 12, 12.0, 0.0, false, 90.0, null, null, null, null, 26.6, 63.4, "R", "R"));
            Assert.Throws<InvalidOperationException>(() => GearParameterCalculator.CalculateCrossedPair(
                24, Module, Pressure, 12.0, 0.0, false, 12, 12.0, 0.0, true, 90.0, null, null, null, null, 26.6, 63.4, "R", "R"));
        }

        [Test]
        public void TestCalculateCrossedPair_RejectsAProfileShift()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => GearParameterCalculator.CalculateCrossedPair(
                24, Module, Pressure, 12.0, 0.2, false, 12, 12.0, 0.0, false, 90.0, null, null, null, null, 26.6, 63.4, "R", "R"));
            StringAssert.Contains("profile shift", ex.Message);
            Assert.Throws<InvalidOperationException>(() => GearParameterCalculator.CalculateCrossedPair(
                24, Module, Pressure, 12.0, 0.0, false, 12, 12.0, -0.1, false, 90.0, null, null, null, null, 26.6, 63.4, "R", "R"));
        }

        [Test]
        public void TestCalculateCrossedPair_RejectsBadHelixAnglesAndTeeth()
        {
            Assert.Throws<InvalidOperationException>(() => GearParameterCalculator.CalculateCrossedPair(
                24, Module, Pressure, 12.0, 0.0, false, 12, 12.0, 0.0, false, 90.0, null, null, null, null, 90.0, 0.0, "R", "R"));
            Assert.Throws<InvalidOperationException>(() => GearParameterCalculator.CalculateCrossedPair(
                24, Module, Pressure, 12.0, 0.0, false, 12, 12.0, 0.0, false, 90.0, null, null, null, null, -5.0, 0.0, "R", "R"));
            Assert.Throws<InvalidOperationException>(() => GearParameterCalculator.CalculateCrossedPair(
                0, Module, Pressure, 12.0, 0.0, false, 12, 12.0, 0.0, false, 90.0, null, null, null, null, 20.0, 30.0, "R", "R"));
        }

        // ----- The parallel solver -----

        [Test]
        public void TestParallelSolver_RejectsDifferentHelixAnglesAndPointsToTheCrossedMesh()
        {
            var solver = new ParallelAxisMeshSolver();
            var ex = Assert.Throws<InvalidOperationException>(() => solver.Solve(Gear("A", 24, 26.6), Gear("B", 12, 63.4, "L"), 0.0));
            StringAssert.Contains("crossed", ex.Message.ToLowerInvariant());
            StringAssert.Contains("26.6", ex.Message);
            StringAssert.Contains("63.4", ex.Message);
        }

        [Test]
        public void TestParallelSolver_RejectsTheSameHandOnParallelShafts()
        {
            var solver = new ParallelAxisMeshSolver();
            var ex = Assert.Throws<InvalidOperationException>(() => solver.Solve(Gear("A", 20, 20.0, "R"), Gear("B", 30, 20.0, "R"), 0.0));
            StringAssert.Contains("crossed", ex.Message.ToLowerInvariant());
        }

        [Test]
        public void TestParallelSolver_StillSolvesEqualAnglesWithOppositeHands()
        {
            var solver = new ParallelAxisMeshSolver();
            var result = solver.Solve(Gear("A", 20, 20.0, "R"), Gear("B", 30, 20.0, "L"), 0.0);

            Assert.AreEqual(Mt(20.0) * 50 / 2.0, result.CentreDistance, 1e-6);
            Assert.AreEqual(1.5, result.GearRatio, 1e-9);
        }

        [Test]
        public void TestParallelSolver_StillSolvesSpurAndHerringbonePairs()
        {
            var solver = new ParallelAxisMeshSolver();
            Assert.DoesNotThrow(() => solver.Solve(Gear("A", 20, 0.0), Gear("B", 30, 0.0), 0.0));

            var a = Gear("A", 20, 20.0, "R");
            var b = Gear("B", 30, 20.0, "R");
            a.IsHerringbone = true;
            b.IsHerringbone = true;
            Assert.DoesNotThrow(() => solver.Solve(a, b, 0.0));
        }

        // ----- Relation, registry and file format -----

        private static GearAssembly CrossedAssembly(double shaftAngle, out GearMeshRelation relation, bool solve = true)
        {
            var driver = Gear("A", 24, 26.6);
            var driven = Gear("B", 12, 63.4);
            var assembly = new GearAssembly();
            var s1 = new Shaft("s1");
            s1.Mount(driver, 0.0);
            var s2 = new Shaft("s2");
            s2.Mount(driven, 0.0);
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            relation = assembly.AddRelation(driver, driven, MeshType.CrossedHelical, 0.0, shaftAngle);
            if (solve) assembly.Solve();
            return assembly;
        }

        [Test]
        public void TestRelation_KeepsTheMeshTypeAndTheShaftAngle()
        {
            CrossedAssembly(90.0, out var relation, solve: false);

            Assert.AreEqual(MeshType.CrossedHelical, relation.MeshType);
            Assert.AreEqual(90.0, relation.ShaftAngleDeg);
        }

        [Test]
        public void TestRelation_ShaftAngleDefaultsToZero()
        {
            var a = Gear("A", 20, 20.0, "R");
            var b = Gear("B", 30, 20.0, "L");
            var assembly = new GearAssembly();
            var s1 = new Shaft("s1");
            s1.Mount(a, 0.0);
            var s2 = new Shaft("s2");
            s2.Mount(b, 0.0);
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);

            Assert.AreEqual(0.0, assembly.AddRelation(a, b).ShaftAngleDeg);
        }

        [Test]
        public void TestRegistry_ProvidesACrossedAxisSolver()
        {
            Assert.IsInstanceOf<ICrossedAxisSolver>(MeshSolvers.Get(MeshType.CrossedHelical));
        }

        [Test]
        public void TestSerializer_RoundTripKeepsTheShaftAngleAndTheMeshType()
        {
            var assembly = CrossedAssembly(90.0, out _);

            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            var relation = loaded.Relations.Single();
            Assert.AreEqual(MeshType.CrossedHelical, relation.MeshType);
            Assert.AreEqual(90.0, relation.ShaftAngleDeg, 1e-12);
            Assert.AreEqual(26.6, ((InvoluteGearComponent)relation.Driver).HelixAngleDeg, 1e-12);
            Assert.AreEqual(63.4, ((InvoluteGearComponent)relation.Driven).HelixAngleDeg, 1e-12);
        }

        [Test]
        public void TestSerializer_OlderFileWithoutAShaftAngleLoadsWithZero()
        {
            var assembly = CrossedAssembly(90.0, out _, solve: false);
            var json = AssemblySerializer.ToJson(assembly);
            StringAssert.Contains("ShaftAngleDeg", json);
            var older = json.Replace("\"ShaftAngleDeg\"", "\"IgnoredShaftAngle\"");

            var loaded = AssemblySerializer.FromJson(older);

            Assert.AreEqual(0.0, loaded.Relations.Single().ShaftAngleDeg);
            Assert.AreEqual(MeshType.CrossedHelical, loaded.Relations.Single().MeshType);
        }

        [Test]
        public void TestSerializer_ParallelRelationKeepsAZeroShaftAngle()
        {
            var a = Gear("A", 20, 20.0, "R");
            var b = Gear("B", 30, 20.0, "L");
            var assembly = new GearAssembly();
            var s1 = new Shaft("s1");
            s1.Mount(a, 0.0);
            var s2 = new Shaft("s2");
            s2.Mount(b, 0.0);
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            assembly.AddRelation(a, b);

            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            Assert.AreEqual(MeshType.Parallel, loaded.Relations.Single().MeshType);
            Assert.AreEqual(0.0, loaded.Relations.Single().ShaftAngleDeg);
        }

        [Test]
        public void TestSolve_RejectsAFixedCentreDistanceOnACrossedRelation()
        {
            var assembly = CrossedAssembly(90.0, out var relation, solve: false);
            relation.CentreDistanceOverride = 60.0;

            var ex = Assert.Throws<InvalidOperationException>(() => assembly.Solve());
            StringAssert.Contains("centre distance", ex.Message);
            StringAssert.Contains("CrossedHelical", ex.Message);
        }

        // ----- Solved assembly: placement, geometry and signs -----

        private sealed class Solved
        {
            public GearAssembly Assembly;
            public InvoluteGearComponent Driver, Driven;
            public Shaft Shaft1, Shaft2;
            public GearMeshRelation Relation;
            public double Axial1, Axial2;
        }

        private static Solved Solve(int z1, double b1, string h1, int z2, double b2, string h2, double shaftAngle,
            double centreLineDeg = 0.0, double axial1 = 0.0, double axial2 = 0.0)
        {
            var driver = Gear("A", z1, b1, h1);
            var driven = Gear("B", z2, b2, h2);
            var assembly = new GearAssembly();
            var s1 = new Shaft("s1");
            s1.Mount(driver, axial1);
            var s2 = new Shaft("s2");
            s2.Mount(driven, axial2);
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            var relation = assembly.AddRelation(driver, driven, MeshType.CrossedHelical, centreLineDeg, shaftAngle);
            assembly.Solve();
            return new Solved { Assembly = assembly, Driver = driver, Driven = driven, Shaft1 = s1, Shaft2 = s2, Relation = relation, Axial1 = axial1, Axial2 = axial2 };
        }

        /// <summary>The helix direction of the gear at a point on its pitch cylinder, in world coordinates.</summary>
        private static Vector3D HelixTangent(Shaft shaft, InvoluteGearComponent gear, Vector3D point, out double radius)
        {
            var placement = shaft.Placement;
            var d = point - placement.Origin;
            double lx = d.Dot(placement.XAxis), ly = d.Dot(placement.YAxis);
            radius = Math.Sqrt(lx * lx + ly * ly);
            double ex = -ly / radius, ey = lx / radius;
            var hand = gear.HelicalDirection == "R" ? 1.0 : -1.0;
            var tan = Math.Tan(gear.HelixAngleDeg * Math.PI / 180.0);
            var tangent = placement.XAxis * (hand * tan * ex) + placement.YAxis * (hand * tan * ey) + placement.ZAxis;
            return tangent.Normalized();
        }

        private static Vector3D ContactPoint(Solved s, out Vector3D normal)
        {
            var c1 = s.Shaft1.Placement.Origin + s.Shaft1.Placement.ZAxis * s.Axial1;
            var c2 = s.Shaft2.Placement.Origin + s.Shaft2.Placement.ZAxis * s.Axial2;
            normal = (c2 - c1).Normalized();
            return c1 + normal * (s.Driver.PitchDiameter / 2.0);
        }

        [Test]
        public void TestReferencePair_PlacesTheDrivenShaftAtRightAnglesAndAtTheCentreDistance()
        {
            var s = Solve(24, 26.6, "R", 12, 63.4, "R", 90.0);

            var a = (s.Driver.PitchDiameter + s.Driven.PitchDiameter) / 2.0;
            Assert.AreEqual(0.0, s.Shaft1.Placement.ZAxis.Dot(s.Shaft2.Placement.ZAxis), 1e-9, "The driven axis is perpendicular to the driver axis.");
            Assert.AreEqual(a, s.Relation.Result.CentreDistance, 1e-5);
            Assert.AreEqual(a, (s.Shaft2.Placement.Origin - s.Shaft1.Placement.Origin).Length, 1e-5);
            Assert.AreEqual(a, s.Shaft2.Placement.Origin.X, 1e-5);
            Assert.AreEqual(0.0, s.Shaft2.Placement.Origin.Y, 1e-9);
            Assert.AreEqual(0.0, s.Shaft2.Placement.Origin.Z, 1e-9);
            Assert.IsTrue(s.Relation.Result.PairData.IsCrossedAxis);
            Assert.AreEqual(0.5, s.Relation.Result.GearRatio, 1e-9);
        }

        [Test]
        public void TestReferencePair_SpeedFactorIsMinusTheToothRatio()
        {
            var s = Solve(24, 26.6, "R", 12, 63.4, "R", 90.0);

            // As for every mesh, the factor is the driven speed over the driver speed: driver teeth over driven teeth.
            // The 12 tooth gear turns twice as fast as the 24 tooth gear (the gear ratio, driven over driver teeth, is 12 / 24),
            // although the pitch diameters are equal.
            var factor = GearTrainAnalyzer.SpeedFactor(s.Relation);
            Assert.AreEqual(2.0, Math.Abs(factor), 1e-12);
            Assert.AreEqual(-2.0, factor, 1e-12);
            Assert.AreEqual(0.5, s.Relation.Result.GearRatio, 1e-12);
            Assert.AreEqual(s.Driven.PitchDiameter, s.Driver.PitchDiameter, 0.1, "The ratio is not derived from the diameters.");
        }

        [TestCase(26.6, "R", 63.4, "R", 90.0, 0.0)]
        [TestCase(26.6, "R", 63.4, "R", 90.0, 30.0)]
        [TestCase(26.6, "R", 63.4, "R", 90.0, -45.0)]
        [TestCase(26.6, "L", 63.4, "L", 90.0, 0.0)]
        [TestCase(20.0, "R", 30.0, "R", 50.0, 0.0)]
        [TestCase(20.0, "R", 30.0, "R", 50.0, 30.0)]
        [TestCase(20.0, "L", 30.0, "L", 50.0, -45.0)]
        [TestCase(20.0, "R", 30.0, "L", 10.0, 0.0)]
        [TestCase(30.0, "R", 20.0, "L", 10.0, 30.0)]
        [TestCase(20.0, "L", 30.0, "R", 10.0, -45.0)]
        [TestCase(30.0, "L", 20.0, "R", 10.0, 0.0)]
        [TestCase(45.0, "R", 45.0, "R", 90.0, 0.0)]
        [TestCase(45.0, "L", 45.0, "L", 90.0, 30.0)]
        public void TestGeometry_BothHelicesAreTheSameLineAndTheNormalSurfaceSpeedsAgree(
            double b1, string h1, double b2, string h2, double sigma, double centreLineDeg)
        {
            var s = Solve(24, b1, h1, 12, b2, h2, sigma, centreLineDeg, axial1: 3.0, axial2: -2.0);

            var p = ContactPoint(s, out var u);
            var z1 = s.Shaft1.Placement.ZAxis;
            var z2 = s.Shaft2.Placement.ZAxis;
            var c2 = s.Shaft2.Placement.Origin + z2 * s.Axial2;

            Assert.AreEqual(sigma, Math.Acos(Math.Min(1.0, Math.Abs(z1.Dot(z2)))) * 180.0 / Math.PI, 1e-6, "The shaft angle.");
            Assert.AreEqual(0.0, u.Dot(z1), 1e-9, "The line of centres is the common normal of the axes.");
            Assert.AreEqual(0.0, u.Dot(z2), 1e-9);

            var t1 = HelixTangent(s.Shaft1, s.Driver, p, out var r1);
            var t2 = HelixTangent(s.Shaft2, s.Driven, p, out var r2);
            Console.WriteLine($"[DEBUG_LOG] {b1}{h1}/{b2}{h2} theta {centreLineDeg}: r1 {r1:0.0000} r2 {r2:0.0000} cross {t1.Cross(t2).Length:0.0E+00}");

            Assert.AreEqual(s.Driver.PitchDiameter / 2.0, r1, 1e-5);
            Assert.AreEqual(s.Driven.PitchDiameter / 2.0, r2, 1e-5, "The contact point is on the pitch cylinder of both gears.");
            Assert.AreEqual(0.0, t1.Cross(t2).Length, 1e-6, "The helix tangents of both gears at the contact point are the same line.");

            // Surface velocities of the two gears at the contact point, for the driver turning at 1 rad per unit time.
            var factor = GearTrainAnalyzer.SpeedFactor(s.Relation);
            var v1 = z1.Cross(p - s.Shaft1.Placement.Origin);
            var v2 = z2.Cross(p - c2) * factor;
            var w = t1.Cross(u).Normalized();
            Assert.AreEqual(v1.Dot(w), v2.Dot(w), 1e-6, "The surface speeds along the tooth normal are equal.");
            Assert.Greater(Math.Abs(v1.Dot(w)), 1.0, "The check is not trivial.");
            Assert.Greater(Math.Abs(v1.Dot(w) + v2.Dot(w)), 1.0, "The opposite sign of the speed factor would not satisfy it.");
        }

        [Test]
        public void TestShaftAngleThatDoesNotMatchTheHelixAnglesIsRejectedWithTheExpectedAngle()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => Solve(24, 26.6, "R", 12, 63.4, "R", 80.0));
            StringAssert.Contains("90", ex.Message);
            StringAssert.Contains("80", ex.Message);
        }

        [Test]
        public void TestSolverRejectsOtherPairsThatDoNotMeshOnCrossedShafts()
        {
            var solver = new CrossedHelicalMeshSolver();
            var internalGear = Gear("A", 24, 26.6);
            internalGear.IsInternal = true;
            Assert.Throws<InvalidOperationException>(() => solver.Solve(internalGear, Gear("B", 12, 63.4), 0.0, 90.0));

            var herringbone = Gear("A", 24, 26.6);
            herringbone.IsHerringbone = true;
            Assert.Throws<InvalidOperationException>(() => solver.Solve(herringbone, Gear("B", 12, 63.4), 0.0, 90.0));

            var otherModule = Gear("B", 12, 63.4);
            otherModule.Module = 3.0;
            Assert.Throws<InvalidOperationException>(() => solver.Solve(Gear("A", 24, 26.6), otherModule, 0.0, 90.0));

            var otherPressure = Gear("B", 12, 63.4);
            otherPressure.PressureAngleDeg = 25.0;
            Assert.Throws<InvalidOperationException>(() => solver.Solve(Gear("A", 24, 26.6), otherPressure, 0.0, 90.0));

            var shifted = Gear("B", 12, 63.4);
            shifted.ProfileShift = 0.3;
            Assert.Throws<InvalidOperationException>(() => solver.Solve(Gear("A", 24, 26.6), shifted, 0.0, 90.0));
        }

        [Test]
        public void TestSolver_TurnsTheDrivenGearAgainstTheDriverAtTheToothRatio()
        {
            var solver = new CrossedHelicalMeshSolver();
            var driver = Gear("A", 24, 26.6);
            var driven = Gear("B", 12, 63.4);

            var at0 = solver.Solve(driver, driven, 0.0, 90.0);
            var at1 = solver.Solve(driver, driven, 0.3, 90.0);

            var basePhase = AlibreGearGenerator.CalculateMeshRotationAngle(false, false, 24, 12);
            Assert.AreEqual(basePhase, at0.DrivenPhaseRad, 1e-12, "At phase zero a tooth space of the driven gear faces the contact point.");
            Assert.AreEqual(-0.3 * 24.0 / 12.0, at1.DrivenPhaseRad - at0.DrivenPhaseRad, 1e-12);
            Assert.AreEqual(0.5, at0.GearRatio, 1e-12);
            Assert.AreEqual(at0.CentreDistance, at1.CentreDistance, 0.0);
        }

        [Test]
        public void TestSolver_WithoutAShaftAngleUsesTheExpectedOne()
        {
            var solver = new CrossedHelicalMeshSolver();
            var result = solver.Solve(Gear("A", 24, 26.6), Gear("B", 12, 63.4), 0.0);

            Assert.AreEqual(90.0, result.PairData.ShaftAngleDeg, 1e-12);
            Assert.IsNotNull(result.DrivenFrame);
        }

        [Test]
        public void TestAssembly_AGearDrivenFromTheCrossedGearsShaftIsPlacedFromIt()
        {
            var driver = Gear("A", 24, 26.6, "R");
            var crossed = Gear("B", 12, 63.4, "R");
            var third = Gear("C", 20, 63.4, "L");
            var assembly = new GearAssembly();
            var s1 = new Shaft("s1");
            s1.Mount(driver, 0.0);
            var s2 = new Shaft("s2");
            s2.Mount(crossed, 0.0);
            var s3 = new Shaft("s3");
            s3.Mount(third, 0.0);
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            assembly.AddShaft(s3);
            assembly.AddRelation(driver, crossed, MeshType.CrossedHelical, 30.0, 90.0);
            var parallel = assembly.AddRelation(crossed, third, MeshType.Parallel, 0.0);
            assembly.Solve();

            var distance = (s3.Placement.Origin - s2.Placement.Origin).Length;
            Assert.AreEqual(Mt(63.4) * (12 + 20) / 2.0, distance, 1e-5);
            Assert.AreEqual(distance, parallel.Result.CentreDistance, 1e-5);
            Assert.AreEqual(1.0, Math.Abs(s3.Placement.ZAxis.Dot(s2.Placement.ZAxis)), 1e-9, "The third shaft is parallel to the second.");
            Assert.AreEqual(0.0, (s3.Placement.Origin - s2.Placement.Origin).Dot(s2.Placement.ZAxis), 1e-6);
            Assert.AreEqual(1.0, (s3.Placement.Origin - s2.Placement.Origin).Normalized().Dot(s2.Placement.XAxis), 1e-6,
                "With a centre line angle of 0 the third shaft lies along the local X axis of the second.");

            Assert.AreEqual(-12.0 / 20.0, GearTrainAnalyzer.SpeedFactor(parallel), 1e-12);
        }

        // ----- Editor -----

        private sealed class FakeView : IGearTrainView
        {
            public double InputSpeedRpm { get; set; } = 1000.0;
            public object Properties;
            public AssemblyScene Scene;
            public GearTrainReport Report;
            public string Message;
            public bool IsError;

            public void ShowTree(TrainTreeNode root, object selectedTag) { }
            public void ShowSelection(object selectedTag) { }
            public void ShowProperties(object properties) => Properties = properties;
            public void ShowScene(AssemblyScene scene, IReadOnlyCollection<object> selectedItems, ISet<GearComponent> hiddenGears) => Scene = scene;
            public void ShowReport(GearTrainReport report) => Report = report;

            public void ShowMessage(string message, bool isError)
            {
                Message = message;
                IsError = isError;
            }
        }

        private static GearTrainPresenter HelicalPresenter(out FakeView view, out InvoluteGearComponent driver)
        {
            view = new FakeView();
            var presenter = new GearTrainPresenter(view);
            presenter.NewTrain();
            driver = (InvoluteGearComponent)presenter.Selected;
            driver.Teeth = 24;
            driver.HelixAngleDeg = 26.6;
            driver.HelicalDirection = "R";
            driver.IsHerringbone = false;
            return presenter;
        }

        [Test]
        public void TestEditor_AddCrossedMate_GivesTheComplementaryHelixAngleTheSameHandAndTheDerivedShaftAngle()
        {
            var presenter = HelicalPresenter(out var view, out var driver);

            var mate = (InvoluteGearComponent)presenter.AddMate(driver, MeshType.CrossedHelical);

            Assert.IsFalse(view.IsError, view.Message);
            var relation = presenter.Assembly.Relations.Single();
            Assert.AreEqual(MeshType.CrossedHelical, relation.MeshType);
            Assert.AreEqual(63.4, mate.HelixAngleDeg, 1e-9);
            Assert.AreEqual("R", mate.HelicalDirection);
            Assert.AreEqual(12, mate.Teeth);
            Assert.AreEqual(driver.Module, mate.Module, 1e-12);
            Assert.AreEqual(driver.PressureAngleDeg, mate.PressureAngleDeg, 1e-12);
            Assert.AreEqual(driver.FaceWidth, mate.FaceWidth, 1e-12);
            Assert.IsFalse(mate.IsHerringbone);
            Assert.AreEqual(90.0, relation.ShaftAngleDeg, 1e-9);
            Assert.AreEqual(2, presenter.Assembly.Shafts.Count);
            Assert.AreEqual(0.0, presenter.Assembly.Shafts[1].Placement.ZAxis.Dot(presenter.Assembly.Shafts[0].Placement.ZAxis), 1e-9);
            Assert.AreEqual(-0.5, view.Report.OverallRatio.Value, Tol);
            Assert.AreEqual(2, view.Scene.Items.Count);
        }

        [Test]
        public void TestEditor_AddCrossedMate_FromASpurDriverUses45DegreesAndAShaftAngleOf45()
        {
            var presenter = HelicalPresenter(out var view, out var driver);
            driver.HelixAngleDeg = 0.0;

            var mate = (InvoluteGearComponent)presenter.AddMate(driver, MeshType.CrossedHelical, 30);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(45.0, mate.HelixAngleDeg, 1e-12);
            Assert.AreEqual(45.0, presenter.Assembly.Relations.Single().ShaftAngleDeg, 1e-9);
            Assert.AreEqual(30, mate.Teeth);
        }

        [Test]
        public void TestEditor_CrossedMate_ShowsTheShaftAngleAndTheExpectedOneInTheProperties()
        {
            var presenter = HelicalPresenter(out var view, out var driver);
            presenter.AddMate(driver, MeshType.CrossedHelical);

            var properties = (GearNodeProperties)view.Properties;
            Assert.AreEqual(90.0, properties.ShaftAngle, 1e-9);
            Assert.AreEqual(90.0, properties.ExpectedShaftAngle, 1e-9);

            presenter.Select(driver);
            var driverProperties = (GearNodeProperties)view.Properties;
            Assert.AreEqual(0.0, driverProperties.ShaftAngle, "A gear that is not driven has no shaft angle.");
            Assert.AreEqual(0.0, driverProperties.ExpectedShaftAngle);
        }

        [Test]
        public void TestEditor_ChangingTheHelixAngleOfAGear_RevalidatesTheShaftAngleAndShowsTheExpectedOne()
        {
            var presenter = HelicalPresenter(out var view, out var driver);
            var mate = (InvoluteGearComponent)presenter.AddMate(driver, MeshType.CrossedHelical);

            mate.HelixAngleDeg = 53.4;
            presenter.Refresh();

            Assert.IsTrue(view.IsError);
            StringAssert.Contains("80", view.Message, "The message gives the shaft angle that the new helix angles need.");

            var properties = (GearNodeProperties)view.Properties;
            Assert.AreEqual(80.0, properties.ExpectedShaftAngle, 1e-9);
            properties.ShaftAngle = properties.ExpectedShaftAngle;
            presenter.Refresh();

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(80.0, presenter.Assembly.Relations.Single().ShaftAngleDeg, 1e-9);
        }

        [Test]
        public void TestEditor_ChangingTheHandOfAGear_RevalidatesTheShaftAngle()
        {
            var presenter = HelicalPresenter(out var view, out var driver);
            var mate = (InvoluteGearComponent)presenter.AddMate(driver, MeshType.CrossedHelical);

            driver.HelicalDirection = "L";
            presenter.Refresh();

            Assert.IsTrue(view.IsError);
            StringAssert.Contains("36.8", view.Message, "Opposite hands need the difference of the helix angles.");
            Assert.AreEqual(36.8, ((GearNodeProperties)view.Properties).ExpectedShaftAngle, 1e-9);
            Assert.AreEqual("R", mate.HelicalDirection);
        }

        [Test]
        public void TestEditor_AddCrossedMate_RejectsInternalAndHerringboneDriversAndLeavesTheTrainUnchanged()
        {
            var presenter = HelicalPresenter(out _, out var driver);
            driver.IsHerringbone = true;
            Assert.Throws<ArgumentException>(() => presenter.AddMate(driver, MeshType.CrossedHelical));
            Assert.AreEqual(1, presenter.Assembly.Shafts.Count);
            Assert.AreEqual(0, presenter.Assembly.Relations.Count);

            driver.IsHerringbone = false;
            driver.IsInternal = true;
            Assert.Throws<ArgumentException>(() => presenter.AddMate(driver, MeshType.CrossedHelical));
            Assert.AreEqual(1, presenter.Assembly.Shafts.Count);
        }

        [Test]
        public void TestEditor_AddCrossedMate_ThenSaveAndLoadKeepsTheShaftAngle()
        {
            var presenter = HelicalPresenter(out _, out var driver);
            presenter.AddMate(driver, MeshType.CrossedHelical);

            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(presenter.Assembly));

            Assert.AreEqual(MeshType.CrossedHelical, loaded.Relations.Single().MeshType);
            Assert.AreEqual(90.0, loaded.Relations.Single().ShaftAngleDeg, 1e-9);
        }

        // ----- Alibre export parity -----

        private static double Distance(Vector3D a, Vector3D b) => (a - b).Length;

        [TestCase(0.0, 0.8, "R", "R", 26.6, 63.4, 90.0)]
        [TestCase(30.0, -0.45, "R", "R", 26.6, 63.4, 90.0)]
        [TestCase(-45.0, 1.7, "L", "L", 26.6, 63.4, 90.0)]
        [TestCase(30.0, 0.8, "R", "L", 20.0, 30.0, 10.0)]
        [TestCase(-45.0, 0.37, "R", "R", 20.0, 30.0, 50.0)]
        public void TestExport_PartPlacementsOfBothCrossedGearsHaveTheScenePose(double thetaDeg, double driverRotation, string hand1, string hand2,
            double beta1, double beta2, double sigma)
        {
            var driver = Gear("A", 24, beta1, hand1);
            var driven = Gear("B", 12, beta2, hand2);
            var assembly = new GearAssembly();
            var shaftA = new Shaft("S1") { RotationRad = driverRotation };
            var shaftB = new Shaft("S2");
            var driverMount = shaftA.Mount(driver, 6.0);
            var drivenMount = shaftB.Mount(driven, 4.0);
            assembly.AddShaft(shaftA);
            assembly.AddShaft(shaftB);
            assembly.AddRelation(driver, driven, MeshType.CrossedHelical, thetaDeg, sigma);

            var scene = GearAssemblySceneBuilder.Build(assembly);

            Assert.Greater(Math.Abs(drivenMount.PhaseRad % (2.0 * Math.PI)), 1e-3, "The driven gear is turned in the scene.");
            foreach (var pair in new[] { Tuple.Create(shaftA, driverMount), Tuple.Create(shaftB, drivenMount) })
            {
                var shaft = pair.Item1;
                var mount = pair.Item2;
                var item = scene.Find(mount.Component);
                Assert.IsNotNull(item, "The gear is in the scene.");
                var placement = AlibreGearExporter.GetPartPlacement(shaft, mount);
                var local = item.LocalToWorld;

                foreach (var axis in new[] { Vector3D.UnitX, Vector3D.UnitY, Vector3D.UnitZ })
                {
                    Assert.AreEqual(0.0, Distance(placement.TransformDirection(axis), local.TransformDirection(axis)), 1e-9,
                        $"Gear {mount.Component.Id}: the exported part has the orientation of the scene.");
                }

                // The sketch plane is the start face of the gear: its mid-plane is half the face width further along the shaft axis.
                var half = mount.Component.FaceWidth / 2.0;
                Assert.AreEqual(0.0, Distance(placement.TransformPoint(new Vector3D(0, 0, half)), local.TransformPoint(Vector3D.Zero)), 1e-6,
                    $"Gear {mount.Component.Id}: the mid-plane of the exported part is the mid-plane of the scene gear.");
            }

            // The two exported gears are the centre distance apart, with the shafts at the shaft angle.
            var a = (driver.PitchDiameter + driven.PitchDiameter) / 2.0;
            var p1 = AlibreGearExporter.GetPartPlacement(shaftA, driverMount);
            var p2 = AlibreGearExporter.GetPartPlacement(shaftB, drivenMount);
            var mid1 = p1.TransformPoint(new Vector3D(0, 0, driver.FaceWidth / 2.0));
            var mid2 = p2.TransformPoint(new Vector3D(0, 0, driven.FaceWidth / 2.0));
            Assert.AreEqual(a, Distance(mid1, mid2), 1e-5);
            var axis1 = p1.TransformDirection(Vector3D.UnitZ);
            var axis2 = p2.TransformDirection(Vector3D.UnitZ);
            Assert.AreEqual(sigma, Math.Acos(Math.Min(1.0, Math.Abs(axis1.Dot(axis2)))) * 180.0 / Math.PI, 1e-6);
        }

        [TestCase("R", true)]
        [TestCase("L", false)]
        public void TestExport_TheSteepGearOfTheReferencePairBuildsAHelicalToothSketch(string hand, bool rightHand)
        {
            var plan = SingleToothSketchBuilder.Build(Gear("B", 12, 63.4, hand));

            Assert.IsTrue(plan.IsHelical);
            Assert.AreEqual(rightHand, plan.Helix.RightHand);
            Assert.Greater(plan.Helix.Lead, 0.0);
            Assert.Greater(SingleToothSketchBuilder.MaximumHelixAngleDeg, 63.4);
            Assert.Throws<InvalidOperationException>(() => SingleToothSketchBuilder.Build(Gear("B", 12, 75.0)), "A steeper helix is still refused.");
        }

        [Test]
        public void TestAssembly_ResultIsKeptOnTheRelation()
        {
            var s = Solve(24, 26.6, "R", 12, 63.4, "R", 90.0);

            Assert.IsNotNull(s.Relation.Result);
            Assert.IsNotNull(s.Relation.Result.DrivenFrame);
            Assert.AreEqual(s.Relation.Result.DrivenPhaseRad - 0.0, s.Shaft2.RotationRad, 1e-12);
        }
    }
}
