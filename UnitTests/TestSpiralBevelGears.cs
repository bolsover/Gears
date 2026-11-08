using System;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>Spiral, zerol and hypoid bevel gear components: geometry values, validation, mesh build and the file format.</summary>
    [TestFixture]
    public class TestSpiralBevelGears
    {
        private const double Tol = 1e-9;

        private static SpiralBevelGearComponent Spiral(int z = 18, int mate = 36, double faceWidth = 12.0, SpiralHand hand = SpiralHand.Right)
        {
            return new SpiralBevelGearComponent("SP") { Teeth = z, MateTeeth = mate, Module = 4.0, FaceWidth = faceWidth, Hand = hand, MeanSpiralAngleDeg = 35.0 };
        }

        private static HypoidGearComponent Hypoid(bool pinion, double offset = 20.0, double faceWidth = 12.0)
        {
            return new HypoidGearComponent(pinion ? "HP" : "HG")
            {
                Teeth = pinion ? 12 : 37,
                MateTeeth = pinion ? 37 : 12,
                IsPinion = pinion,
                Module = 4.0,
                FaceWidth = faceWidth,
                OffsetMm = offset,
                MeanSpiralAngleDeg = 45.0,
                Hand = pinion ? SpiralHand.Left : SpiralHand.Right
            };
        }

        private static double SignedVolume(GearMesh3DBuilder.Mesh3D mesh)
        {
            var volume = 0.0;
            var t = mesh.TriangleIndices;
            for (var i = 0; i + 2 < t.Count; i += 3)
            {
                var a = mesh.Vertices[t[i]];
                var b = mesh.Vertices[t[i + 1]];
                var c = mesh.Vertices[t[i + 2]];
                volume += (a.X * (b.Y * c.Z - b.Z * c.Y) - a.Y * (b.X * c.Z - b.Z * c.X) + a.Z * (b.X * c.Y - b.Y * c.X)) / 6.0;
            }

            return volume;
        }

        [Test]
        public void TestSpiral_HasTheConeGeometryOfAStraightBevelPair()
        {
            var spiral = Spiral();
            var straight = new BevelGearComponent("B") { Teeth = 18, MateTeeth = 36, Module = 4.0, FaceWidth = 12.0 };

            Assert.AreEqual(straight.PitchConeAngleRad, spiral.PitchConeAngleRad, Tol);
            Assert.AreEqual(straight.ConeDistance, spiral.ConeDistance, Tol);
            Assert.AreEqual(straight.ApexAxialPosition, spiral.ApexAxialPosition, Tol);
            Assert.AreEqual(straight.PitchDiameter, spiral.PitchDiameter, Tol);
            Assert.AreEqual(Math.Atan2(18.0, 36.0), spiral.PitchConeAngleRad, Tol);
        }

        [Test]
        public void TestTransversePressureAngle_FollowsTheSpiralAngle()
        {
            var spiral = Spiral();
            spiral.PressureAngleDeg = 20.0;

            var expected = Math.Atan(Math.Tan(20.0 * Math.PI / 180.0) / Math.Cos(35.0 * Math.PI / 180.0)) * 180.0 / Math.PI;
            Assert.AreEqual(expected, spiral.TransversePressureAngleDeg, Tol);
            Assert.Greater(spiral.TransversePressureAngleDeg, 20.0);
            Assert.AreEqual("Spiral bevel gear", spiral.TypeName);
        }

        [Test]
        public void TestZerol_HasAZeroSpiralAngle_AndTheConeGeometryOfAStraightPair()
        {
            var zerol = new ZerolBevelGearComponent("Z") { Teeth = 18, MateTeeth = 36, Module = 4.0, FaceWidth = 12.0, MeanSpiralAngleDeg = 25.0 };
            var straight = new BevelGearComponent("B") { Teeth = 18, MateTeeth = 36, Module = 4.0, FaceWidth = 12.0 };

            Assert.AreEqual(0.0, zerol.MeanSpiralAngleDeg, 0.0, "A spiral angle set on a zerol gear is ignored.");
            Assert.AreEqual(zerol.PressureAngleDeg, zerol.TransversePressureAngleDeg, Tol);
            Assert.AreEqual(straight.PitchConeAngleRad, zerol.PitchConeAngleRad, Tol);
            Assert.AreEqual(straight.ApexAxialPosition, zerol.ApexAxialPosition, Tol);
            Assert.AreEqual("Zerol bevel gear", zerol.TypeName);
        }

        [TestCase(SpiralHand.Right)]
        [TestCase(SpiralHand.Left)]
        public void TestSpiral_BuildsAClosedSolidWithMoreSectionsThanAStraightGear(SpiralHand hand)
        {
            var spiral = Spiral(hand: hand);
            var straight = new BevelGearComponent("B") { Teeth = 18, MateTeeth = 36, Module = 4.0, FaceWidth = 12.0, PressureAngleDeg = spiral.TransversePressureAngleDeg };

            var mesh = spiral.BuildGeometry().Mesh;
            var straightMesh = straight.BuildGeometry().Mesh;

            Assert.Greater(mesh.TriangleIndices.Count, straightMesh.TriangleIndices.Count);
            Assert.Greater(SignedVolume(mesh), 0.0, "Faces must point outwards.");
            Assert.AreEqual(SignedVolume(straightMesh), SignedVolume(mesh), 0.05 * SignedVolume(straightMesh), "Curved teeth keep about the same volume.");
        }

        [Test]
        public void TestZerol_BuildsAClosedSolid()
        {
            var zerol = new ZerolBevelGearComponent("Z") { Teeth = 18, MateTeeth = 36, Module = 4.0, FaceWidth = 12.0 };

            var geometry = zerol.BuildGeometry();

            Assert.Greater(SignedVolume(geometry.Mesh), 0.0);
            Assert.Greater(geometry.Polylines[0].Count, 10, "The tooth outline.");
        }

        [Test]
        public void TestSpiral_OutlineIsTurnedByTheHeelTwist_AndTheHandsAreMirrored()
        {
            var right = Spiral(hand: SpiralHand.Right);
            var left = Spiral(hand: SpiralHand.Left);
            var straight = new BevelGearComponent("B") { Teeth = 18, MateTeeth = 36, Module = 4.0, FaceWidth = 12.0, PressureAngleDeg = right.TransversePressureAngleDeg };

            double Angle(GearPoint p) => Math.Atan2(p.Y, p.X);
            var a0 = Angle(straight.BuildGeometry().Polylines[0][0]);
            var aRight = Angle(right.BuildGeometry().Polylines[0][0]);
            var aLeft = Angle(left.BuildGeometry().Polylines[0][0]);

            Assert.Greater(Math.Abs(aRight - a0), 1e-6);
            Assert.AreEqual(aRight - a0, -(aLeft - a0), 1e-9, "The two hands turn the heel outline in opposite directions.");
        }

        [Test]
        public void TestValidate_RejectsASpiralAngleOutsideTheRange()
        {
            var spiral = Spiral();
            spiral.MeanSpiralAngleDeg = 65.0;
            var ex = Assert.Throws<InvalidOperationException>(() => spiral.Validate());
            StringAssert.Contains("mean spiral angle", ex.Message);

            spiral.MeanSpiralAngleDeg = -5.0;
            Assert.Throws<InvalidOperationException>(() => spiral.Validate());
        }

        [Test]
        public void TestValidate_RejectsACutterThatIsTooSmallForTheFace()
        {
            var spiral = Spiral();
            spiral.CutterRadius = 10.0;

            var ex = Assert.Throws<InvalidOperationException>(() => spiral.Validate());
            StringAssert.Contains("cutter radius", ex.Message);
            StringAssert.Contains("too small", ex.Message);

            spiral.CutterRadius = -1.0;
            Assert.Throws<InvalidOperationException>(() => spiral.Validate());
        }

        [Test]
        public void TestValidate_RejectsATransversePressureAngleOfAtLeast45Degrees()
        {
            var spiral = Spiral();
            spiral.PressureAngleDeg = 40.0;
            spiral.MeanSpiralAngleDeg = 60.0;

            var ex = Assert.Throws<InvalidOperationException>(() => spiral.Validate());
            StringAssert.Contains("transverse pressure angle", ex.Message);
        }

        [Test]
        public void TestValidate_AcceptsTheUsualSpiralGear()
        {
            Assert.DoesNotThrow(() => Spiral().Validate());
            Assert.DoesNotThrow(() => new ZerolBevelGearComponent("Z") { Teeth = 18, MateTeeth = 36, Module = 4.0, FaceWidth = 12.0 }.Validate());
        }

        [Test]
        public void TestHypoid_PairValuesComeFromTheHypoidGeometry_AndAreTheSameOnBothGears()
        {
            var pinion = Hypoid(true);
            var gear = Hypoid(false);
            var pair = HypoidGeometry.Solve(12, 37, 4.0, 20.0, 90.0, 45.0);

            Assert.IsNotNull(pinion.Pair);
            Assert.AreEqual(pair.PinionConeAngleRad, pinion.PitchConeAngleRad, Tol);
            Assert.AreEqual(pair.GearConeAngleRad, gear.PitchConeAngleRad, Tol);
            Assert.AreEqual(pair.PinionSpiralAngleDeg, pinion.EffectiveSpiralAngleDeg, Tol);
            Assert.AreEqual(pair.GearSpiralAngleDeg, gear.EffectiveSpiralAngleDeg, Tol);
            Assert.Greater(pinion.PitchDiameter, 4.0 * 12.0, "The pinion is larger than its tooth count suggests.");
            Assert.AreEqual(pair.GearMeanConeDistance + 6.0, gear.ConeDistance, Tol);
            Assert.AreEqual(pair.PinionMeanConeDistance + 6.0, pinion.ConeDistance, Tol);
            Assert.Greater(Math.Abs(pinion.PitchConeAngleDeg - (90.0 - gear.PitchConeAngleDeg)), 0.01, "The cone angles of a hypoid pair do not add up to the shaft angle.");
        }

        [Test]
        public void TestHypoid_BothGearsHaveTheSameToothDepth()
        {
            var pinion = Hypoid(true);
            var gear = Hypoid(false);

            Assert.AreEqual(gear.WholeDepth, pinion.WholeDepth, Tol);
            Assert.AreEqual(gear.Addendum, pinion.Addendum, Tol);
            Assert.Greater(gear.Addendum, gear.Module, "The depth is that of the gear at its large end.");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestHypoid_BuildsAClosedSolid(bool pinion)
        {
            var gear = Hypoid(pinion);

            var geometry = gear.BuildGeometry();

            Assert.Greater(SignedVolume(geometry.Mesh), 0.0, "Faces must point outwards.");
            Assert.Greater(geometry.Mesh.TriangleIndices.Count, 0);
        }

        [Test]
        public void TestHypoid_ValidationMessages()
        {
            var gear = Hypoid(false, offset: 0.0);
            var ex = Assert.Throws<InvalidOperationException>(() => gear.Validate());
            StringAssert.Contains("offset", ex.Message);
            StringAssert.Contains("spiral bevel", ex.Message);

            gear = Hypoid(false, offset: 90.0);
            ex = Assert.Throws<InvalidOperationException>(() => gear.Validate());
            StringAssert.Contains("smaller than the gear mean pitch radius", ex.Message);

            gear = Hypoid(false);
            gear.BevelType = BevelGearType.Gleason;
            ex = Assert.Throws<InvalidOperationException>(() => gear.Validate());
            StringAssert.Contains("standard", ex.Message);

            Assert.DoesNotThrow(() => Hypoid(false).Validate());
            Assert.DoesNotThrow(() => Hypoid(true).Validate());
        }

        [Test]
        public void TestSerializer_RoundTripsTheSpiralAndZerolGears()
        {
            var assembly = new GearAssembly();
            var s1 = new Shaft("S1");
            var spiral = Spiral(hand: SpiralHand.Left);
            spiral.MeanSpiralAngleDeg = 30.0;
            spiral.CutterRadius = 150.0;
            s1.Mount(spiral, 0.0);
            var s2 = new Shaft("S2");
            s2.Mount(new ZerolBevelGearComponent("Z") { Teeth = 20, MateTeeth = 40, Module = 3.0, FaceWidth = 10.0, Hand = SpiralHand.Right, CutterRadius = 90.0 }, 0.0);
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);

            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            var loadedSpiral = loaded.Shafts[0].Mounts[0].Component as SpiralBevelGearComponent;
            Assert.IsNotNull(loadedSpiral);
            Assert.AreEqual(typeof(SpiralBevelGearComponent), loadedSpiral.GetType());
            Assert.AreEqual(SpiralHand.Left, loadedSpiral.Hand);
            Assert.AreEqual(30.0, loadedSpiral.MeanSpiralAngleDeg, Tol);
            Assert.AreEqual(150.0, loadedSpiral.CutterRadius, Tol);
            Assert.AreEqual(18, loadedSpiral.Teeth);
            Assert.AreEqual(36, loadedSpiral.MateTeeth);

            var loadedZerol = loaded.Shafts[1].Mounts[0].Component as ZerolBevelGearComponent;
            Assert.IsNotNull(loadedZerol);
            Assert.AreEqual(SpiralHand.Right, loadedZerol.Hand);
            Assert.AreEqual(90.0, loadedZerol.CutterRadius, Tol);
            Assert.AreEqual(0.0, loadedZerol.MeanSpiralAngleDeg, 0.0);
        }

        [Test]
        public void TestSerializer_RoundTripsTheHypoidGears()
        {
            var assembly = new GearAssembly();
            var s1 = new Shaft("S1");
            s1.Mount(Hypoid(true, offset: -18.0), 0.0);
            var s2 = new Shaft("S2");
            s2.Mount(Hypoid(false, offset: -18.0), 0.0);
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);

            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            var pinion = (HypoidGearComponent)loaded.Shafts[0].Mounts[0].Component;
            var gear = (HypoidGearComponent)loaded.Shafts[1].Mounts[0].Component;
            Assert.IsTrue(pinion.IsPinion);
            Assert.IsFalse(gear.IsPinion);
            Assert.AreEqual(-18.0, pinion.OffsetMm, Tol);
            Assert.AreEqual(45.0, pinion.MeanSpiralAngleDeg, Tol);
            Assert.AreEqual(SpiralHand.Left, pinion.Hand);
            Assert.AreEqual(SpiralHand.Right, gear.Hand);
            Assert.AreEqual(12, pinion.Teeth);
            Assert.AreEqual(37, pinion.MateTeeth);
            Assert.IsNotNull(pinion.Pair);
        }

        [Test]
        public void TestSerializer_StraightBevelFilesStillLoadAsStraightBevelGears()
        {
            var assembly = new GearAssembly();
            var s1 = new Shaft("S1");
            s1.Mount(new BevelGearComponent("B") { Teeth = 20, MateTeeth = 40, Module = 2.0, FaceWidth = 10.0 }, 0.0);
            assembly.AddShaft(s1);

            var json = AssemblySerializer.ToJson(assembly);
            var loaded = AssemblySerializer.FromJson(json);

            StringAssert.Contains("\"Bevel\"", json);
            Assert.AreEqual(typeof(BevelGearComponent), loaded.Shafts[0].Mounts[0].Component.GetType());
        }
    }
}
