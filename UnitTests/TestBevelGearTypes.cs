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
            var radii = LargeEndRadii(pinion, mesh);

            var cosDelta = Math.Cos(pinion.PitchConeAngleRad);
            Assert.AreEqual(pinion.PitchDiameter / 2.0 + pinion.Addendum * cosDelta, radii.Max(), 1e-6, "Tip radius at the large end.");
            Assert.AreEqual(pinion.PitchDiameter / 2.0 - pinion.Dedendum * cosDelta, radii.Min(), 1e-6, "Root radius at the large end.");

            var standard = Pair(out var plain, out _, BevelGearType.Standard, 20, 40);
            standard.Solve();
            var plainTip = LargeEndRadii(plain, plain.BuildGeometry().Mesh).Max();
            Assert.Greater(radii.Max(), plainTip, "The long pinion addendum raises the tip above the standard tip.");
        }

        /// <summary>Radii of the heel (large end) vertices: they are at least the cone distance away from the apex, the toe vertices are closer.</summary>
        private static System.Collections.Generic.List<double> LargeEndRadii(BevelGearComponent gear, Bolsover.Gears.Calculator.GearMesh3DBuilder.Mesh3D mesh)
        {
            var apexZ = gear.ApexAxialPosition;
            return mesh.Vertices
                .Where(v => Math.Sqrt(v.X * v.X + v.Y * v.Y + (v.Z - apexZ) * (v.Z - apexZ)) >= gear.ConeDistance - 1e-9)
                .Select(v => Math.Sqrt(v.X * v.X + v.Y * v.Y))
                .Where(r => r > 1e-6)
                .ToList();
        }

        [TestCase(BevelGearType.Standard, 20, 40, 90.0)]
        [TestCase(BevelGearType.Standard, 40, 20, 90.0)]
        [TestCase(BevelGearType.Gleason, 20, 36, 75.0)]
        public void TestSolid_ToothEndsAreSquareToThePitchConeAndEdgesRunToTheApex(BevelGearType type, int z1, int z2, double shaftAngleDeg)
        {
            Pair(out var gear, out _, type, z1, z2, shaftAngleDeg).Solve();

            var mesh = gear.BuildGeometry().Mesh;
            var apexZ = gear.ApexAxialPosition;
            var delta = gear.PitchConeAngleRad;
            var re = gear.ConeDistance;
            var scale = (re - gear.FaceWidth) / re;

            var heel = mesh.Vertices.Where(v => Math.Sqrt(v.X * v.X + v.Y * v.Y + (v.Z - apexZ) * (v.Z - apexZ)) >= re - 1e-9
                                                 && Math.Sqrt(v.X * v.X + v.Y * v.Y) > 1e-6).ToList();
            Assert.IsNotEmpty(heel);
            foreach (var v in heel)
            {
                var radial = Math.Sqrt(v.X * v.X + v.Y * v.Y);
                var along = radial * Math.Sin(delta) + (apexZ - v.Z) * Math.Cos(delta);
                Assert.AreEqual(re, along, 1e-6, "The heel face is perpendicular to the pitch cone line.");
            }

            var tip = heel.OrderByDescending(v => Math.Sqrt(v.X * v.X + v.Y * v.Y)).First();
            Assert.AreEqual(gear.PitchApexToCrown, apexZ - tip.Z, 1e-6, "Pitch apex to crown distance.");

            var toe = mesh.Vertices.Where(v => Math.Sqrt(v.X * v.X + v.Y * v.Y + (v.Z - apexZ) * (v.Z - apexZ)) < re - 1e-9
                                                && Math.Sqrt(v.X * v.X + v.Y * v.Y) > 1e-6).ToList();
            var toeTip = toe.OrderByDescending(v => Math.Sqrt(v.X * v.X + v.Y * v.Y)).First();
            Assert.AreEqual(scale * Math.Sqrt(tip.X * tip.X + tip.Y * tip.Y), Math.Sqrt(toeTip.X * toeTip.X + toeTip.Y * toeTip.Y), 1e-6);
            Assert.AreEqual(scale * (apexZ - tip.Z), apexZ - toeTip.Z, 1e-6, "The tip line runs to the apex.");
        }

        [Test]
        public void TestSolid_FacesPointOutwards()
        {
            Pair(out var gear, out _, BevelGearType.Standard).Solve();

            var mesh = gear.BuildGeometry().Mesh;
            var volume = 0.0;
            for (var i = 0; i + 2 < mesh.TriangleIndices.Count; i += 3)
            {
                var a = mesh.Vertices[mesh.TriangleIndices[i]];
                var b = mesh.Vertices[mesh.TriangleIndices[i + 1]];
                var c = mesh.Vertices[mesh.TriangleIndices[i + 2]];
                volume += (a.X * (b.Y * c.Z - b.Z * c.Y) - a.Y * (b.X * c.Z - b.Z * c.X) + a.Z * (b.X * c.Y - b.Y * c.X)) / 6.0;
            }

            Assert.Greater(volume, 0.0, "Faces must point outwards.");
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

        private static GearAssembly KhkPair(BevelGearType type, out BevelGearComponent pinion, out BevelGearComponent gear)
        {
            pinion = new BevelGearComponent("P") { Teeth = 20, Module = 3.0, PressureAngleDeg = 20.0, FaceWidth = 22.0, ShaftAngleDeg = 90.0, BevelType = type };
            gear = new BevelGearComponent("G") { Teeth = 40, Module = 3.0, PressureAngleDeg = 20.0, FaceWidth = 22.0 };
            var s1 = new Shaft("S1");
            s1.Mount(pinion, 0.0);
            var s2 = new Shaft("S2");
            s2.Mount(gear, 0.0);
            var assembly = new GearAssembly();
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            assembly.AddRelation(pinion, gear, MeshType.Bevel);
            return assembly;
        }

        [Test]
        public void TestStandard_MatchesPublishedStraightBevelExample()
        {
            // m = 3, 20:40 teeth, 90 degrees, face width 22 (KHK / HZPT Table "Standard straight bevel gears").
            KhkPair(BevelGearType.Standard, out var pinion, out var gear).Solve();

            Assert.AreEqual(26.56505, pinion.PitchConeAngleDeg, 1e-5);
            Assert.AreEqual(63.43495, gear.PitchConeAngleDeg, 1e-5);
            Assert.AreEqual(67.08204, pinion.ConeDistance, 1e-5);
            Assert.AreEqual(3.75, pinion.Dedendum, Tol);
            Assert.AreEqual(3.19960, pinion.DedendumAngleDeg, 1e-5);
            Assert.AreEqual(2.56064, pinion.AddendumAngleDeg, 1e-5);
            Assert.AreEqual(29.12569, pinion.TipConeAngleDeg, 1e-5);
            Assert.AreEqual(23.36545, pinion.RootConeAngleDeg, 1e-5);
            Assert.AreEqual(65.99559, gear.TipConeAngleDeg, 1e-5);
            Assert.AreEqual(60.23535, gear.RootConeAngleDeg, 1e-5);
            Assert.AreEqual(65.3666, pinion.TipDiameter, 1e-4);
            Assert.AreEqual(122.6833, gear.TipDiameter, 1e-4);
            Assert.AreEqual(58.6584, pinion.PitchApexToCrown, 1e-4);
            Assert.AreEqual(27.3167, gear.PitchApexToCrown, 1e-4);
            Assert.AreEqual(22.36068, pinion.RecommendedMaxFaceWidth, 1e-5, "Re / 3 is smaller than 10 m.");
        }

        [Test]
        public void TestGleason_MatchesPublishedExampleApartFromTheDepthConstant()
        {
            // KHK / HZPT Gleason example: ha1 = 4.035, ha2 = 1.965, hf1 = 2.529, hf2 = 4.599 (whole depth 2.188 m); the
            // program adds the 0.051 mm Gleason depth constant, so depths differ by that amount at most.
            KhkPair(BevelGearType.Gleason, out var pinion, out var gear).Solve();

            Assert.AreEqual(4.035, pinion.Addendum, 1e-9);
            Assert.AreEqual(1.965, gear.Addendum, 1e-9);
            Assert.AreEqual(2.529, pinion.Dedendum, GearTolerance);
            Assert.AreEqual(4.599, gear.Dedendum, GearTolerance);
            Assert.AreEqual(2.15903, pinion.DedendumAngleDeg, 0.05);
            Assert.AreEqual(3.92194, gear.DedendumAngleDeg, 0.05);
            Assert.AreEqual(gear.DedendumAngleDeg, pinion.AddendumAngleDeg, 1e-9, "Pinion addendum angle = gear dedendum angle.");
            Assert.AreEqual(pinion.DedendumAngleDeg, gear.AddendumAngleDeg, 1e-9, "Gear addendum angle = pinion dedendum angle.");
            Assert.AreEqual(67.2180, pinion.TipDiameter, 1e-4);
            Assert.AreEqual(121.7575, gear.TipDiameter, 1e-4);
            Assert.AreEqual(58.1955, pinion.PitchApexToCrown, 1e-4);
            Assert.AreEqual(28.2425, gear.PitchApexToCrown, 1e-4);
        }

        private const double GearTolerance = 0.0511;

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
