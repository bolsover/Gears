using System;
using System.Linq;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>Standard and Gleason tooth proportions of straight bevel gears, and how a bevel pair keeps them consistent.</summary>
    [TestFixture]
    public class TestBevelGearTypes
    {
        private const double Tol = 1e-9;

        private static GearAssembly Pair(out BevelGearComponent g1, out BevelGearComponent g2, BevelGearType type, int z1 = 20, int z2 = 40, double shaftAngleDeg = 90.0)
        {
            g1 = new BevelGearComponent("B1") { Teeth = z1, Module = 2.0, PressureAngleDeg = 20.0, FaceWidth = 10.0, ShaftAngleDeg = shaftAngleDeg, BevelType = type };
            g2 = new BevelGearComponent("B2") { Teeth = z2, Module = 2.0, PressureAngleDeg = 20.0, FaceWidth = 10.0 };

            var s1 = new Shaft("S1");
            s1.Mount(g1, 3.0);
            var s2 = new Shaft("S2");
            s2.Mount(g2, -2.0);

            var assembly = new GearAssembly();
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            assembly.AddRelation(g1, g2, MeshType.Bevel);
            return assembly;
        }

        [Test]
        public void TestStandard_KeepsEqualAddendaAndTheOriginalClearance()
        {
            Pair(out var g1, out var g2, BevelGearType.Standard).Solve();

            foreach (var gear in new[] { g1, g2 })
            {
                Assert.AreEqual(gear.Module, gear.Addendum, Tol);
                Assert.AreEqual(0.0, gear.ProfileShiftMm, Tol);
                Assert.AreEqual(0.25 * gear.Module, gear.Clearance, Tol);
                Assert.AreEqual(2.25 * gear.Module, gear.WholeDepth, Tol);
                Assert.AreEqual(1.25 * gear.Module, gear.Dedendum, Tol);
            }
        }

        [Test]
        public void TestGleason_ProportionsFollowTheGleasonFormulas()
        {
            Pair(out var pinion, out var gear, BevelGearType.Gleason, 20, 40).Solve();

            var m = 2.0;
            Assert.AreEqual(2.188 * m + 0.051, gear.WholeDepth, Tol);
            Assert.AreEqual(0.188 * m + 0.051, gear.Clearance, Tol);

            var ratio = 2.0;
            var gearAddendum = m * (0.54 + 0.46 / (ratio * ratio));
            Assert.AreEqual(1.31, gearAddendum, 1e-12, "0.54 m + 0.46 m / 4 for m = 2 mm.");
            Assert.AreEqual(gearAddendum, gear.Addendum, 1e-9, "Gear addendum = 0.54 m + 0.46 m / u^2.");
            Assert.AreEqual(2.0 * m - gearAddendum, pinion.Addendum, 1e-9, "The pinion has the rest of the working depth of 2 m.");
            Assert.Greater(pinion.Addendum, gear.Addendum, "The pinion has the long addendum.");
            Assert.AreEqual(pinion.WholeDepth, pinion.Addendum + pinion.Dedendum, Tol);
            Assert.AreEqual(gear.WholeDepth, gear.Addendum + gear.Dedendum, Tol);
            Assert.AreEqual(pinion.Addendum - m, pinion.ProfileShiftMm, Tol);
            Assert.AreEqual(0.0, pinion.ProfileShiftMm + gear.ProfileShiftMm, Tol, "Equal and opposite shifts keep the working depth constant.");
        }

        [Test]
        public void TestGleason_EqualGears_HaveTheStandardAddendum()
        {
            Pair(out var g1, out var g2, BevelGearType.Gleason, 24, 24).Solve();

            Assert.AreEqual(g1.Module, g1.Addendum, 1e-9);
            Assert.AreEqual(g2.Module, g2.Addendum, 1e-9);
        }

        [TestCase(90.0, 20, 36)]
        [TestCase(75.0, 20, 36)]
        [TestCase(110.0, 18, 30)]
        public void TestGleason_AddendaAddUpToTheWorkingDepthForAnyShaftAngle(double shaftAngleDeg, int z1, int z2)
        {
            Pair(out var g1, out var g2, BevelGearType.Gleason, z1, z2, shaftAngleDeg).Solve();

            Assert.AreEqual(2.0 * g1.Module, g1.Addendum + g2.Addendum, 1e-9);
            Assert.AreEqual(g1.VirtualToothRatio, 1.0 / g2.VirtualToothRatio, 1e-9);
        }

        [Test]
        public void TestVirtualToothRatio_For90Degrees_IsTheSquareOfTheToothRatio()
        {
            Pair(out var g1, out var g2, BevelGearType.Gleason, 20, 50).Solve();

            Assert.AreEqual(Math.Pow(20.0 / 50.0, 2.0), g1.VirtualToothRatio, 1e-9);
            Assert.AreEqual(Math.Pow(50.0 / 20.0, 2.0), g2.VirtualToothRatio, 1e-9);
        }

        [Test]
        public void TestSolver_GivesTheDrivenGearTheDriversType()
        {
            var assembly = Pair(out _, out var driven, BevelGearType.Gleason);
            Assert.AreEqual(BevelGearType.Standard, driven.BevelType);

            assembly.Solve();

            Assert.AreEqual(BevelGearType.Gleason, driven.BevelType);
        }

        [Test]
        public void TestGleason_Geometry_UsesTheLongAndShortAddendum()
        {
            Pair(out var pinion, out var gear, BevelGearType.Gleason, 20, 40).Solve();

            foreach (var g in new[] { pinion, gear })
            {
                var profile = g.BuildGeometry().Profile;
                var virtualPitch = g.Module * g.VirtualTeeth / 2.0;
                Assert.AreEqual(g.Addendum, profile.Parameters["addendumRadius"] - virtualPitch, 1e-9, g.Id + " addendum");
                Assert.AreEqual(g.Dedendum, virtualPitch - profile.Parameters["dedendumRadius"], 1e-9, g.Id + " dedendum");
                Assert.AreEqual(g.Clearance, profile.Parameters["clearance"], 1e-9);
            }
        }

        [Test]
        public void TestGleason_Geometry_TipAndRootRadiiAtTheLargeEnd()
        {
            Pair(out var pinion, out _, BevelGearType.Gleason, 20, 40).Solve();

            var mesh = pinion.BuildGeometry().Mesh;
            var halfAxial = pinion.FaceWidth * Math.Cos(pinion.PitchConeAngleRad) / 2.0;
            var radii = mesh.Vertices.Where(v => Math.Abs(v.Z + halfAxial) < 1e-9).Select(v => Math.Sqrt(v.X * v.X + v.Y * v.Y)).Where(r => r > 1e-6).ToList();

            var scale = pinion.Teeth / (double)pinion.VirtualTeeth;
            var virtualPitch = pinion.Module * pinion.VirtualTeeth / 2.0;
            Assert.AreEqual((virtualPitch + pinion.Addendum) * scale, radii.Max(), 1e-3, "Tip radius at the large end.");
            Assert.AreEqual((virtualPitch - pinion.Dedendum) * scale, radii.Min(), 1e-3, "Root radius at the large end.");

            var standard = Pair(out var plain, out _, BevelGearType.Standard, 20, 40);
            standard.Solve();
            var plainTip = plain.BuildGeometry().Mesh.Vertices.Where(v => Math.Abs(v.Z + halfAxial) < 1e-9).Select(v => Math.Sqrt(v.X * v.X + v.Y * v.Y)).Max();
            Assert.Greater(radii.Max(), plainTip, "The long pinion addendum raises the tip above the standard tip.");
        }

        [Test]
        public void TestGleason_ConeApexesStillCoincide()
        {
            var assembly = Pair(out var g1, out var g2, BevelGearType.Gleason, 20, 36, 75.0);

            assembly.Solve();

            var s1 = assembly.Shafts[0];
            var s2 = assembly.Shafts[1];
            var apex1 = s1.Placement.Origin + s1.Placement.ZAxis * (3.0 + g1.ApexAxialPosition);
            var apex2 = s2.Placement.Origin + s2.Placement.ZAxis * (-2.0 + g2.ApexAxialPosition);
            Assert.AreEqual(0.0, (apex1 - apex2).Length, 1e-5);
        }

        [Test]
        public void TestSerializer_RoundTripsTheBevelType()
        {
            var assembly = Pair(out _, out _, BevelGearType.Gleason);
            assembly.Solve();

            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            var first = (BevelGearComponent)loaded.Shafts[0].Mounts[0].Component;
            Assert.AreEqual(BevelGearType.Gleason, first.BevelType);
            loaded.Solve();
            Assert.AreEqual(BevelGearType.Gleason, ((BevelGearComponent)loaded.Shafts[1].Mounts[0].Component).BevelType);
        }

        [Test]
        public void TestSerializer_FileWithoutATypeLoadsAsStandard()
        {
            var assembly = Pair(out _, out _, BevelGearType.Gleason);
            var json = AssemblySerializer.ToJson(assembly).Replace("\"BevelType\"", "\"Ignored\"");

            var loaded = AssemblySerializer.FromJson(json);

            Assert.AreEqual(BevelGearType.Standard, ((BevelGearComponent)loaded.Shafts[0].Mounts[0].Component).BevelType);
        }
    }
}
