using System;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>Meshing of spiral, zerol and hypoid bevel gears in a solved assembly: pairing rules, placement, tooth lines, phase and speed.</summary>
    [TestFixture]
    public class TestHypoidMesh
    {
        private const double Tol = 1e-6;

        private static GearAssembly SpiralPair<T>(out T g1, out T g2, Func<string, T> create, SpiralHand hand1 = SpiralHand.Right, SpiralHand hand2 = SpiralHand.Left,
            double angle2 = 35.0, double cutter2 = 0.0) where T : SpiralBevelGearComponent
        {
            g1 = create("S1");
            g1.Teeth = 18;
            g1.Module = 4.0;
            g1.FaceWidth = 12.0;
            g1.Hand = hand1;
            g1.MeanSpiralAngleDeg = 35.0;
            g2 = create("S2");
            g2.Teeth = 36;
            g2.Module = 4.0;
            g2.FaceWidth = 12.0;
            g2.Hand = hand2;
            g2.MeanSpiralAngleDeg = angle2;
            g2.CutterRadius = cutter2;
            return Assemble(g1, g2, MeshType.Bevel, 0.0);
        }

        private static GearAssembly Assemble(GearComponent driver, GearComponent driven, MeshType type, double offset)
        {
            var s1 = new Shaft("A");
            s1.Mount(driver, 3.0);
            var s2 = new Shaft("B");
            s2.Mount(driven, -2.0);
            var assembly = new GearAssembly();
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            assembly.AddRelation(driver, driven, type, 0.0, 0.0, offset);
            return assembly;
        }

        private static GearAssembly HypoidPair(out HypoidGearComponent pinion, out HypoidGearComponent gear, double offset, bool gearDrives = true, bool correctHands = true)
        {
            var gearHand = offset > 0 ? SpiralHand.Right : SpiralHand.Left;
            var pinionHand = offset > 0 ? SpiralHand.Left : SpiralHand.Right;
            if (!correctHands) (gearHand, pinionHand) = (pinionHand, gearHand);
            pinion = new HypoidGearComponent("P") { Teeth = 12, Module = 4.0, FaceWidth = 12.0, IsPinion = true, Hand = pinionHand, MeanSpiralAngleDeg = 45.0 };
            gear = new HypoidGearComponent("G") { Teeth = 37, Module = 4.0, FaceWidth = 12.0, IsPinion = false, Hand = gearHand };
            return gearDrives ? Assemble(gear, pinion, MeshType.Hypoid, offset) : Assemble(pinion, gear, MeshType.Hypoid, offset);
        }

        private static Transform3D WorldOf(GearAssembly assembly, GearComponent gear)
        {
            var mount = assembly.FindMount(gear, out var shaft);
            return shaft.Placement.ToTransform()
                .Compose(Transform3D.Translate(new Vector3D(0, 0, mount.AxialOffset)))
                .Compose(gear.GetLocalMotion(mount.PhaseRad));
        }

        private static Vector3D Axis(Transform3D t) => t.TransformDirection(Vector3D.UnitZ);

        private static Vector3D Local(Transform3D t, Vector3D world)
        {
            var d = world - t.Translation;
            return new Vector3D(d.Dot(t.TransformDirection(Vector3D.UnitX)), d.Dot(t.TransformDirection(Vector3D.UnitY)), d.Dot(Axis(t)));
        }

        /// <summary>Tooth line direction (towards the apex) of a gear through a world point on its pitch cone.</summary>
        private static Vector3D ToothLine(Transform3D t, Vector3D world, double deltaRad, double betaDeg, SpiralHand hand)
        {
            var local = Local(t, world);
            var r = Math.Sqrt(local.X * local.X + local.Y * local.Y);
            var radial = new Vector3D(local.X / r, local.Y / r, 0);
            var tangential = new Vector3D(-local.Y / r, local.X / r, 0);
            var s = hand == SpiralHand.Right ? 1.0 : -1.0;
            var tanBeta = Math.Tan(betaDeg * Math.PI / 180.0);
            var l = radial * -Math.Sin(deltaRad) + tangential * (s * tanBeta) + Vector3D.UnitZ * Math.Cos(deltaRad);
            var x = t.TransformDirection(Vector3D.UnitX);
            var y = t.TransformDirection(Vector3D.UnitY);
            var z = Axis(t);
            return (x * l.X + y * l.Y + z * l.Z).Normalized();
        }

        private static double ToothFraction(Transform3D t, Vector3D world, int teeth)
        {
            var local = Local(t, world);
            var turns = Math.Atan2(local.Y, local.X) * teeth / (2.0 * Math.PI);
            return turns - Math.Floor(turns);
        }

        private static void AssertTooth(double fraction, string message)
        {
            Assert.IsTrue(fraction < 0.03 || fraction > 0.97, $"{message}: a tooth centre expected, fraction {fraction:0.####}.");
        }

        private static void AssertGap(double fraction, string message)
        {
            Assert.AreEqual(0.5, fraction, 0.03, $"{message}: a gap centre expected.");
        }

        [Test]
        public void TestSpiralPair_IsPlacedLikeAStraightBevelPair()
        {
            var assembly = SpiralPair(out SpiralBevelGearComponent s1, out SpiralBevelGearComponent s2, id => new SpiralBevelGearComponent(id));
            assembly.Solve();

            var straight1 = new BevelGearComponent("B1") { Teeth = 18, Module = 4.0, FaceWidth = 12.0 };
            var straight2 = new BevelGearComponent("B2") { Teeth = 36, Module = 4.0, FaceWidth = 12.0 };
            var reference = Assemble(straight1, straight2, MeshType.Bevel, 0.0);
            reference.Solve();

            var placed = assembly.Shafts[1].Placement;
            var expected = reference.Shafts[1].Placement;
            Assert.AreEqual(0.0, (placed.Origin - expected.Origin).Length, Tol);
            Assert.AreEqual(0.0, (placed.ZAxis - expected.ZAxis).Length, Tol);
            Assert.AreEqual(0.0, (placed.XAxis - expected.XAxis).Length, Tol);
            Assert.AreEqual(reference.Shafts[1].RotationRad, assembly.Shafts[1].RotationRad, Tol, "The tooth phase is that of the straight pair.");
            Assert.AreEqual(2.0, assembly.Relations[0].Result.GearRatio, Tol);
            Assert.AreEqual(Math.Atan2(18.0, 36.0), s1.PitchConeAngleRad, Tol);
            Assert.AreEqual(36, s1.MateTeeth);
            Assert.AreEqual(18, s2.MateTeeth);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestSpiralAndZerolPairs_HaveTheSameToothLineAtTheMeanPoint(bool zerol)
        {
            GearAssembly assembly;
            SpiralBevelGearComponent g1, g2;
            if (zerol)
            {
                assembly = SpiralPair(out ZerolBevelGearComponent z1, out ZerolBevelGearComponent z2, id => new ZerolBevelGearComponent(id));
                g1 = z1;
                g2 = z2;
            }
            else
            {
                assembly = SpiralPair(out SpiralBevelGearComponent p1, out SpiralBevelGearComponent p2, id => new SpiralBevelGearComponent(id));
                g1 = p1;
                g2 = p2;
            }

            assembly.Solve();

            var w1 = WorldOf(assembly, g1);
            var w2 = WorldOf(assembly, g2);
            var meanRadius = (g1.ConeDistance - g1.FaceWidth / 2.0) * Math.Sin(g1.PitchConeAngleRad);
            var contact = w1.TransformPoint(new Vector3D(meanRadius, 0, 0));

            var local2 = Local(w2, contact);
            Assert.AreEqual(0.0, local2.Z, 1e-6, "The mean point lies in the mid-plane of the driven gear too.");
            Assert.AreEqual((g2.ConeDistance - g2.FaceWidth / 2.0) * Math.Sin(g2.PitchConeAngleRad), Math.Sqrt(local2.X * local2.X + local2.Y * local2.Y), 1e-6);

            var angle = g1.EffectiveSpiralAngleDeg;
            var t1 = ToothLine(w1, contact, g1.PitchConeAngleRad, angle, g1.Hand);
            var t2 = ToothLine(w2, contact, g2.PitchConeAngleRad, angle, g2.Hand);
            Assert.AreEqual(1.0, t1.Dot(t2), 1e-9, "Opposite hands give the same tooth line in world coordinates.");

            if (!zerol)
            {
                var wrong = ToothLine(w2, contact, g2.PitchConeAngleRad, angle, g1.Hand);
                Assert.Less(wrong.Dot(t1), 0.99, "The same hand on the mate would give a different line.");
            }

            AssertTooth(ToothFraction(w1, contact, g1.Teeth), "driver");
            AssertGap(ToothFraction(w2, contact, g2.Teeth), "driven");
        }

        [Test]
        public void TestZerolPair_IsPlacedLikeAStraightBevelPair()
        {
            var assembly = SpiralPair(out ZerolBevelGearComponent z1, out ZerolBevelGearComponent z2, id => new ZerolBevelGearComponent(id));
            assembly.Solve();

            var reference = Assemble(
                new BevelGearComponent("B1") { Teeth = 18, Module = 4.0, FaceWidth = 12.0 },
                new BevelGearComponent("B2") { Teeth = 36, Module = 4.0, FaceWidth = 12.0 }, MeshType.Bevel, 0.0);
            reference.Solve();

            Assert.AreEqual(0.0, (assembly.Shafts[1].Placement.Origin - reference.Shafts[1].Placement.Origin).Length, Tol);
            Assert.AreEqual(0.0, (assembly.Shafts[1].Placement.ZAxis - reference.Shafts[1].Placement.ZAxis).Length, Tol);
            Assert.AreEqual(Math.Atan2(18.0, 36.0), z1.PitchConeAngleRad, Tol);
            Assert.AreEqual(Math.Atan2(36.0, 18.0), z2.PitchConeAngleRad, Tol);
        }

        [Test]
        public void TestSpiralPair_RejectsTheSameHand()
        {
            var assembly = SpiralPair(out SpiralBevelGearComponent _, out SpiralBevelGearComponent _, id => new SpiralBevelGearComponent(id), SpiralHand.Right, SpiralHand.Right);

            var ex = Assert.Throws<InvalidOperationException>(() => assembly.Solve());
            StringAssert.Contains("Right hand", ex.Message);
            StringAssert.Contains("must be Left hand", ex.Message);
        }

        [Test]
        public void TestSpiralPair_RejectsADifferentSpiralAngle_AndCutterRadius()
        {
            var assembly = SpiralPair(out SpiralBevelGearComponent _, out SpiralBevelGearComponent _, id => new SpiralBevelGearComponent(id), angle2: 30.0);
            var ex = Assert.Throws<InvalidOperationException>(() => assembly.Solve());
            StringAssert.Contains("mean spiral angles differ", ex.Message);
            StringAssert.Contains("expected 35", ex.Message);

            assembly = SpiralPair(out SpiralBevelGearComponent _, out SpiralBevelGearComponent _, id => new SpiralBevelGearComponent(id), cutter2: 90.0);
            ex = Assert.Throws<InvalidOperationException>(() => assembly.Solve());
            StringAssert.Contains("cutter radii differ", ex.Message);
        }

        [Test]
        public void TestPairs_RejectAMixOfFamilies()
        {
            var spiral = new SpiralBevelGearComponent("S1") { Teeth = 18, Module = 4.0, FaceWidth = 12.0, Hand = SpiralHand.Right };
            var zerol = new ZerolBevelGearComponent("Z2") { Teeth = 36, Module = 4.0, FaceWidth = 12.0, Hand = SpiralHand.Left };
            var ex = Assert.Throws<InvalidOperationException>(() => Assemble(spiral, zerol, MeshType.Bevel, 0.0).Solve());
            StringAssert.Contains("spiral bevel gear", ex.Message);
            StringAssert.Contains("zerol bevel gear", ex.Message);

            var straight = new BevelGearComponent("B2") { Teeth = 36, Module = 4.0, FaceWidth = 12.0 };
            ex = Assert.Throws<InvalidOperationException>(() => Assemble(
                new SpiralBevelGearComponent("S3") { Teeth = 18, Module = 4.0, FaceWidth = 12.0 }, straight, MeshType.Bevel, 0.0).Solve());
            StringAssert.Contains("cannot mesh", ex.Message);

            ex = Assert.Throws<InvalidOperationException>(() => HypoidAsBevel());
            StringAssert.Contains("hypoid mesh", ex.Message);
        }

        private static void HypoidAsBevel()
        {
            var pinion = new HypoidGearComponent("P") { Teeth = 12, Module = 4.0, FaceWidth = 12.0, IsPinion = true };
            var gear = new HypoidGearComponent("G") { Teeth = 37, Module = 4.0, FaceWidth = 12.0 };
            Assemble(gear, pinion, MeshType.Bevel, 0.0).Solve();
        }

        [Test]
        public void TestPairs_RejectDifferentModulesAndPressureAngles()
        {
            var g1 = new SpiralBevelGearComponent("S1") { Teeth = 18, Module = 4.0, FaceWidth = 12.0, Hand = SpiralHand.Right };
            var g2 = new SpiralBevelGearComponent("S2") { Teeth = 36, Module = 3.0, FaceWidth = 12.0, Hand = SpiralHand.Left };
            var ex = Assert.Throws<InvalidOperationException>(() => Assemble(g1, g2, MeshType.Bevel, 0.0).Solve());
            StringAssert.Contains("modules differ", ex.Message);

            g2.Module = 4.0;
            g2.PressureAngleDeg = 25.0;
            ex = Assert.Throws<InvalidOperationException>(() => Assemble(g1, g2, MeshType.Bevel, 0.0).Solve());
            StringAssert.Contains("pressure angles differ", ex.Message);
        }

        [TestCase(20.0, true)]
        [TestCase(20.0, false)]
        [TestCase(-20.0, true)]
        [TestCase(-20.0, false)]
        public void TestHypoidPair_ShaftsAreOffsetAndAtTheShaftAngle(double offset, bool gearDrives)
        {
            var assembly = HypoidPair(out var pinion, out var gear, offset, gearDrives);
            assembly.Solve();

            var first = assembly.Shafts[0];
            var second = assembly.Shafts[1];
            var z1 = first.Placement.ZAxis;
            var z2 = second.Placement.ZAxis;
            Assert.AreEqual(0.0, z1.Dot(z2), 1e-6, "The shafts are at 90 degrees.");

            var cross = z1.Cross(z2);
            var distance = Math.Abs((second.Placement.Origin - first.Placement.Origin).Dot(cross)) / cross.Length;
            Assert.AreEqual(Math.Abs(offset), distance, 1e-4, "The shafts are offset by E along the common perpendicular.");
            Assert.IsNotNull(pinion.Pair);
            Assert.AreEqual(offset, gear.OffsetMm, 0.0);
            Assert.AreEqual(offset, pinion.OffsetMm, 0.0);
            Assert.AreEqual(45.0, gear.MeanSpiralAngleDeg, 0.0, "The pinion spiral angle is set on the gear too.");
            Assert.AreEqual(37, pinion.MateTeeth);
            Assert.AreEqual(12, gear.MateTeeth);
        }

        [TestCase(20.0, true)]
        [TestCase(20.0, false)]
        [TestCase(-20.0, true)]
        [TestCase(-20.0, false)]
        public void TestHypoidPair_MeanPointsCoincide_AndToothLinesAgree(double offset, bool gearDrives)
        {
            var assembly = HypoidPair(out var pinion, out var gear, offset, gearDrives);
            assembly.Solve();

            var driver = gearDrives ? gear : pinion;
            var driven = gearDrives ? pinion : gear;
            var wd = WorldOf(assembly, driver);
            var wn = WorldOf(assembly, driven);

            var radiusDriver = driver.MeanConeDistance * Math.Sin(driver.PitchConeAngleRad);
            var contact = wd.TransformPoint(new Vector3D(radiusDriver, 0, 0));
            var local = Local(wn, contact);
            Assert.AreEqual(0.0, local.Z, 1e-6, "The mean point lies in the mid-plane of the driven gear.");
            Assert.AreEqual(driven.MeanConeDistance * Math.Sin(driven.PitchConeAngleRad), Math.Sqrt(local.X * local.X + local.Y * local.Y), 1e-6,
                "The mean point lies on the pitch radius of the driven gear.");

            var tDriver = ToothLine(wd, contact, driver.PitchConeAngleRad, driver.EffectiveSpiralAngleDeg, driver.Hand);
            var tDriven = ToothLine(wn, contact, driven.PitchConeAngleRad, driven.EffectiveSpiralAngleDeg, driven.Hand);
            Assert.AreEqual(1.0, Math.Abs(tDriver.Dot(tDriven)), 1e-9, "The tooth line through the mean point is the same line for both gears.");

            AssertTooth(ToothFraction(wd, contact, driver.Teeth), "driver");
            AssertGap(ToothFraction(wn, contact, driven.Teeth), "driven");
        }

        [TestCase(20.0, true)]
        [TestCase(-20.0, false)]
        public void TestHypoidPair_SurfaceSpeedsAgreeAcrossTheTooth(double offset, bool gearDrives)
        {
            var assembly = HypoidPair(out var pinion, out var gear, offset, gearDrives);
            assembly.Solve();

            var driver = gearDrives ? gear : pinion;
            var driven = gearDrives ? pinion : gear;
            var wd = WorldOf(assembly, driver);
            var wn = WorldOf(assembly, driven);
            var contact = wd.TransformPoint(new Vector3D(driver.MeanConeDistance * Math.Sin(driver.PitchConeAngleRad), 0, 0));

            var speedFactor = MeshSolvers.Get(MeshType.Hypoid).SpeedFactor(driver, driven);
            Assert.AreEqual(-(double)driver.Teeth / driven.Teeth, speedFactor, Tol);
            Assert.AreEqual((double)driven.Teeth / driver.Teeth, assembly.Relations[0].Result.GearRatio, Tol);

            var v1 = Axis(wd).Cross(contact - wd.Translation);
            var v2 = Axis(wn).Cross(contact - wn.Translation) * speedFactor;
            var tooth = ToothLine(wd, contact, driver.PitchConeAngleRad, driver.EffectiveSpiralAngleDeg, driver.Hand);

            var relative = v1 - v2;
            var across = relative - tooth * relative.Dot(tooth);
            Assert.AreEqual(0.0, across.Length / v1.Length, 1e-6, "The relative velocity of the surfaces lies along the tooth.");
            Assert.Greater(v1.Dot(v2), 0.0, "The surfaces move the same way at the mean point.");
        }

        [Test]
        public void TestHypoidPair_TheMirrorImageHasTheOppositeHands()
        {
            HypoidPair(out var pinionA, out var gearA, 20.0);
            HypoidPair(out var pinionB, out var gearB, -20.0);

            Assert.AreEqual(SpiralHand.Right, gearA.Hand);
            Assert.AreEqual(SpiralHand.Left, pinionA.Hand);
            Assert.AreEqual(SpiralHand.Left, gearB.Hand);
            Assert.AreEqual(SpiralHand.Right, pinionB.Hand);
        }

        [Test]
        public void TestHypoidPair_RejectsTheWrongHands_AndZeroOffset()
        {
            var assembly = HypoidPair(out _, out _, 20.0, correctHands: false);
            var ex = Assert.Throws<InvalidOperationException>(() => assembly.Solve());
            StringAssert.Contains("must be Right hand", ex.Message);
            StringAssert.Contains("pinion", ex.Message);

            assembly = HypoidPair(out _, out _, 0.0);
            ex = Assert.Throws<InvalidOperationException>(() => assembly.Solve());
            StringAssert.Contains("offset must not be zero", ex.Message);
        }

        [Test]
        public void TestHypoidPair_RejectsAnOffsetLargerThanTheGearRadius_AndAFixedCentreDistance()
        {
            var assembly = HypoidPair(out _, out _, 90.0);
            var ex = Assert.Throws<InvalidOperationException>(() => assembly.Solve());
            StringAssert.Contains("smaller than the gear mean pitch radius", ex.Message);

            assembly = HypoidPair(out _, out _, 20.0);
            assembly.Relations[0].CentreDistanceOverride = 80.0;
            ex = Assert.Throws<InvalidOperationException>(() => assembly.Solve());
            StringAssert.Contains("fixed centre distance", ex.Message);
        }

        [Test]
        public void TestHypoidPair_RejectsTwoPinionsAndDifferentModules()
        {
            var a = new HypoidGearComponent("A") { Teeth = 12, Module = 4.0, FaceWidth = 12.0, IsPinion = true, Hand = SpiralHand.Left };
            var b = new HypoidGearComponent("B") { Teeth = 37, Module = 4.0, FaceWidth = 12.0, IsPinion = true, Hand = SpiralHand.Right };
            var ex = Assert.Throws<InvalidOperationException>(() => Assemble(a, b, MeshType.Hypoid, 20.0).Solve());
            StringAssert.Contains("one of them must be the pinion", ex.Message);

            b.IsPinion = false;
            b.Module = 3.0;
            ex = Assert.Throws<InvalidOperationException>(() => Assemble(a, b, MeshType.Hypoid, 20.0).Solve());
            StringAssert.Contains("modules differ", ex.Message);
        }

        [Test]
        public void TestHypoidPair_ZeroOffsetGeometryReducesToASpiralPair()
        {
            var pair = Bolsover.Gears.Calculator.HypoidGeometry.Solve(18, 36, 4.0, 0.0, 90.0, 35.0);
            var spiral = new SpiralBevelGearComponent("S") { Teeth = 18, MateTeeth = 36, Module = 4.0, FaceWidth = 12.0 };

            Assert.AreEqual(spiral.PitchConeAngleRad, pair.PinionConeAngleRad, Tol);
            Assert.AreEqual(pair.GearMeanConeDistance, pair.PinionMeanConeDistance, Tol, "The apexes coincide without an offset.");
        }

        [Test]
        public void TestSerializer_RoundTripsTheRelationOffset_AndTheSolvedPairIsTheSame()
        {
            var assembly = HypoidPair(out _, out _, -18.0);
            assembly.Solve();

            var json = AssemblySerializer.ToJson(assembly);
            StringAssert.Contains("OffsetMm", json);
            var loaded = AssemblySerializer.FromJson(json);

            Assert.AreEqual(MeshType.Hypoid, loaded.Relations[0].MeshType);
            Assert.AreEqual(-18.0, loaded.Relations[0].OffsetMm, Tol);
            loaded.Solve();
            Assert.AreEqual(0.0, (loaded.Shafts[1].Placement.Origin - assembly.Shafts[1].Placement.Origin).Length, 1e-6);
            Assert.AreEqual(assembly.Shafts[1].RotationRad, loaded.Shafts[1].RotationRad, 1e-9);
        }

        [Test]
        public void TestSerializer_OlderRelationsLoadWithoutAnOffset()
        {
            var assembly = SpiralPair(out SpiralBevelGearComponent _, out SpiralBevelGearComponent _, id => new SpiralBevelGearComponent(id));
            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly).Replace("\"OffsetMm\"", "\"Ignored\""));

            Assert.AreEqual(0.0, loaded.Relations[0].OffsetMm, 0.0);
            Assert.AreEqual(MeshType.Bevel, loaded.Relations[0].MeshType);
        }
    }
}
