using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Phases 3 to 7: rack and pinion, planetary sets, bevel gears (Tredgold), worm gear sets, JSON persistence,
    /// STL/OBJ export and the animation stepper.
    /// </summary>
    [TestFixture]
    public class TestGearTrainPhases3To7
    {
        private const double Tol = 1e-6;

        #region Helpers

        private static InvoluteGearComponent Spur(string id, int teeth)
        {
            return new InvoluteGearComponent(id)
            {
                Teeth = teeth,
                Module = 2.0,
                PressureAngleDeg = 20.0,
                Clearance = 0.5
            };
        }

        /// <summary>A pinion on the first shaft driving a rack on a carrier shaft.</summary>
        private static GearAssembly RackAssembly(out InvoluteGearComponent pinion, out RackComponent rack, int pinionTeeth = 20, double centreLineAngleDeg = 0.0)
        {
            pinion = Spur("P", pinionTeeth);
            rack = new RackComponent("R") { Teeth = 14, Module = 2.0, PressureAngleDeg = 20.0, FaceWidth = 15.0 };

            var assembly = new GearAssembly();
            var s1 = new Shaft("S1");
            s1.Mount(pinion);
            var s2 = new Shaft("Carrier");
            s2.Mount(rack);
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            assembly.AddRelation(pinion, rack, MeshType.Rack, centreLineAngleDeg);
            return assembly;
        }

        private static GearAssembly BevelAssembly(out BevelGearComponent g1, out BevelGearComponent g2, int z1 = 20, int z2 = 40, double shaftAngleDeg = 90.0)
        {
            g1 = new BevelGearComponent("B1") { Teeth = z1, Module = 2.0, PressureAngleDeg = 20.0, FaceWidth = 10.0, ShaftAngleDeg = shaftAngleDeg };
            g2 = new BevelGearComponent("B2") { Teeth = z2, Module = 2.0, PressureAngleDeg = 20.0, FaceWidth = 10.0 };

            var assembly = new GearAssembly();
            var s1 = new Shaft("S1");
            s1.Mount(g1, 3.0);
            var s2 = new Shaft("S2");
            s2.Mount(g2, -2.0);
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            assembly.AddRelation(g1, g2, MeshType.Bevel);
            return assembly;
        }

        private static GearAssembly WormAssembly(out WormComponent worm, out WormWheelComponent wheel, int starts = 1, int wheelTeeth = 30, string hand = "R")
        {
            worm = new WormComponent("W") { Starts = starts, Module = 2.0, PressureAngleDeg = 20.0, DiameterQuotient = 10.0, HelicalDirection = hand };
            wheel = new WormWheelComponent("G") { Teeth = wheelTeeth, Module = 2.0, PressureAngleDeg = 20.0 };

            var assembly = new GearAssembly();
            var s1 = new Shaft("S1");
            s1.Mount(worm);
            var s2 = new Shaft("S2");
            s2.Mount(wheel);
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            assembly.AddRelation(worm, wheel, MeshType.Worm);
            return assembly;
        }

        /// <summary>Signed volume of a closed mesh; positive when all faces point outwards.</summary>
        private static double Volume(GearMesh3DBuilder.Mesh3D mesh)
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

        private static void AssertValidMesh(GearMesh3DBuilder.Mesh3D mesh, string name, bool checkOrientation = true)
        {
            Assert.Greater(mesh.Vertices.Count, 0, name + ": no vertices");
            Assert.AreEqual(mesh.Vertices.Count, mesh.Normals.Count, name + ": one normal per vertex");
            Assert.AreEqual(0, mesh.TriangleIndices.Count % 3, name + ": whole triangles");
            Assert.IsTrue(mesh.TriangleIndices.All(i => i >= 0 && i < mesh.Vertices.Count), name + ": indices in range");
            if (checkOrientation) Assert.Greater(Volume(mesh), 0.0, name + ": faces must point outwards");
        }

        private static bool Inside(IReadOnlyList<GearPoint> polygon, double x, double y)
        {
            var inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var pi = polygon[i];
                var pj = polygon[j];
                if ((pi.Y > y) != (pj.Y > y) && x < (pj.X - pi.X) * (y - pi.Y) / (pj.Y - pi.Y) + pi.X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static double DistanceToBoundary(IReadOnlyList<GearPoint> polygon, double x, double y)
        {
            var best = double.MaxValue;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var ax = polygon[j].X;
                var ay = polygon[j].Y;
                var bx = polygon[i].X;
                var by = polygon[i].Y;
                var dx = bx - ax;
                var dy = by - ay;
                var length2 = dx * dx + dy * dy;
                var t = length2 < 1e-18 ? 0.0 : Math.Max(0.0, Math.Min(1.0, ((x - ax) * dx + (y - ay) * dy) / length2));
                var px = ax + t * dx - x;
                var py = ay + t * dy - y;
                best = Math.Min(best, Math.Sqrt(px * px + py * py));
            }

            return best;
        }

        /// <summary>Deepest penetration (mm) of the points of one polygon into the other polygon.</summary>
        private static double Penetration(IReadOnlyList<GearPoint> points, IReadOnlyList<GearPoint> polygon)
        {
            var worst = 0.0;
            foreach (var p in points)
            {
                if (Inside(polygon, p.X, p.Y)) worst = Math.Max(worst, DistanceToBoundary(polygon, p.X, p.Y));
            }

            return worst;
        }

        #endregion

        #region Phase 3: rack and pinion

        [Test]
        public void TestRack_Outline_IsClosedAndAnticlockwise()
        {
            var rack = new RackComponent("R") { Teeth = 10, Module = 2.0 };

            var outline = rack.BuildOutline();

            Assert.AreEqual(4 * 10 + 4, outline.Count);
            Assert.Greater(SolidMeshBuilder.SignedArea(outline), 0.0);
            Assert.AreEqual(rack.Length, outline.Max(p => p.X) - outline.Min(p => p.X), Tol, "The rack is Teeth * pitch long.");
            Assert.AreEqual(rack.Module, outline.Max(p => p.Y), Tol, "Tooth tips are one module above the datum line.");
            Assert.AreEqual(-(rack.Module + rack.Clearance + rack.BodyHeight), outline.Min(p => p.Y), Tol);
        }

        [Test]
        public void TestRack_Mesh_IsClosedWithOutwardFaces()
        {
            var rack = new RackComponent("R") { Teeth = 8, Module = 2.0, FaceWidth = 12.0 };

            var geometry = rack.BuildGeometry();

            AssertValidMesh(geometry.Mesh, "rack");
            Assert.AreEqual(6.0, geometry.Mesh.Vertices.Max(v => v.Z), Tol);
            Assert.AreEqual(-6.0, geometry.Mesh.Vertices.Min(v => v.Z), Tol);
            var area = SolidMeshBuilder.SignedArea(rack.BuildOutline());
            Assert.AreEqual(area * 12.0, Volume(geometry.Mesh), 1e-6, "The volume of an extrusion is area times face width.");
        }

        [Test]
        public void TestRack_Validate_RejectsBadParameters()
        {
            Assert.Throws<InvalidOperationException>(() => new RackComponent("R") { Teeth = 0 }.Validate());
            Assert.Throws<InvalidOperationException>(() => new RackComponent("R") { PressureAngleDeg = 75 }.Validate());
            Assert.Throws<InvalidOperationException>(() => new RackComponent("R") { Backlash = 100 }.Validate());
        }

        [Test]
        public void TestRackSolver_PlacesTheCarrierAtThePitchRadiusParallelToThePinion()
        {
            var assembly = RackAssembly(out _, out var rack, 20);

            assembly.Solve();

            var carrier = assembly.Shafts[1];
            Assert.AreEqual(20.0, carrier.Placement.Origin.X, Tol, "Pitch radius m * z / 2.");
            Assert.AreEqual(0.0, carrier.Placement.Origin.Y, Tol);
            Assert.AreEqual(1.0, carrier.Placement.ZAxis.Z, Tol);
            Assert.AreEqual(-1.0, carrier.Placement.YAxis.X, Tol, "The tooth tips of the rack point back at the pinion.");
            Assert.AreEqual(0.0, carrier.RotationRad, Tol, "At zero pinion angle the rack is not displaced.");
            Assert.IsNotNull(assembly.Relations[0].Result);
            Assert.AreSame(rack, assembly.Relations[0].Driven);
        }

        [Test]
        public void TestRackSolver_TheRackSlidesByPitchRadiusTimesPinionAngle()
        {
            var assembly = RackAssembly(out _, out _, 20);
            assembly.Shafts[0].RotationRad = 0.3;

            assembly.Solve();

            Assert.AreEqual(20.0 * 0.3, assembly.Shafts[1].RotationRad, 1e-7, "Displacement = r * phi.");
        }

        [Test]
        public void TestRackSolver_CentreLineAngleTurnsTheWholePair()
        {
            var assembly = RackAssembly(out _, out _, 20, 90.0);

            assembly.Solve();

            var origin = assembly.Shafts[1].Placement.Origin;
            Assert.AreEqual(0.0, origin.X, Tol);
            Assert.AreEqual(20.0, origin.Y, Tol);
            Assert.AreEqual(-1.0, assembly.Shafts[1].Placement.YAxis.Y, Tol, "The rack tips still point at the pinion.");
        }

        [TestCase(0.0)]
        [TestCase(0.2)]
        [TestCase(0.9)]
        [TestCase(2.4)]
        public void TestRackSolver_TeethMeshWithoutInterference(double pinionAngle)
        {
            var assembly = RackAssembly(out var pinion, out var rack, 20);
            assembly.Shafts[0].RotationRad = pinionAngle;

            var scene = GearAssemblySceneBuilder.Build(assembly);

            var pinionOutline = SolidMeshBuilder.Flatten(scene.Find(pinion).Polylines);
            var rackOutline = scene.Find(rack).Polylines[0];

            Assert.Less(Penetration(rackOutline, pinionOutline), 0.01, "Rack corners must stay out of the pinion.");
            Assert.Less(Penetration(pinionOutline, rackOutline), 0.01, "Pinion points must stay out of the rack.");

            // The pair is also in contact, not just apart: the closest approach is close to the clearance or less.
            var closest = rackOutline.Min(r => pinionOutline.Min(p => Math.Sqrt((p.X - r.X) * (p.X - r.X) + (p.Y - r.Y) * (p.Y - r.Y))));
            Assert.Less(closest, 1.0, "The pinion and the rack are in mesh.");
        }

        [Test]
        public void TestRackSolver_RejectsHelicalOrInternalPinionsAndMismatchedModules()
        {
            var assembly = RackAssembly(out var pinion, out var rack);
            pinion.HelixAngleDeg = 15.0;
            Assert.Throws<InvalidOperationException>(() => assembly.Solve());

            pinion.HelixAngleDeg = 0.0;
            rack.Module = 3.0;
            Assert.Throws<InvalidOperationException>(() => assembly.Solve());

            rack.Module = 2.0;
            pinion.IsInternal = true;
            Assert.Throws<InvalidOperationException>(() => assembly.Solve());
        }

        [Test]
        public void TestRackReport_ShowsLinearSpeedAndVelocity()
        {
            var assembly = RackAssembly(out _, out _, 20);

            var report = GearTrainAnalyzer.Analyze(assembly, null, 100.0);

            var carrierRow = report.Shafts.Single(s => s.ShaftId == "Carrier");
            Assert.IsTrue(carrierRow.IsLinear);
            Assert.AreEqual("mm/min", carrierRow.Unit);
            Assert.AreEqual(2.0 * Math.PI * 20.0 * 100.0, carrierRow.SpeedRpm.Value, 1e-6);

            var rackRow = report.Gears.Single(g => g.GearId == "R");
            Assert.AreEqual(2.0 * Math.PI * 20.0 * 100.0 / 60000.0, rackRow.PitchLineVelocity.Value, 1e-6);
            StringAssert.Contains("mm/min", report.ToText());
        }

        [Test]
        public void TestRack_Scene_DoesNotDrawTheCarrierShaft()
        {
            var assembly = RackAssembly(out _, out _);

            var scene = GearAssemblySceneBuilder.Build(assembly);

            Assert.AreEqual(2, scene.ShaftItems.Count);
            Assert.Greater(scene.ShaftItems[0].Mesh.Vertices.Count, 0);
            Assert.AreEqual(0, scene.ShaftItems[1].Mesh.Vertices.Count);
        }

        #endregion

        #region Phase 4: planetary sets

        [Test]
        public void TestPlanetary_Defaults_AreAValidStage()
        {
            var set = new PlanetaryGearSet("PS");

            set.Validate();

            Assert.AreEqual(60, set.RingTeeth);
            Assert.AreEqual(42.0, set.CentreDistance, Tol);
        }

        [Test]
        public void TestPlanetary_Validate_ChecksTheAssemblyConditionAndSpacing()
        {
            var set = new PlanetaryGearSet("PS") { PlanetCount = 5 };
            Assert.Throws<InvalidOperationException>(() => set.Validate(), "(24 + 60) / 5 is not a whole number.");

            set = new PlanetaryGearSet("PS") { SunTeeth = 9, PlanetTeeth = 60, PlanetCount = 3 };
            var ex = Assert.Throws<InvalidOperationException>(() => set.Validate(), "Fat planets collide.");
            StringAssert.Contains("collide", ex.Message);

            Assert.Throws<InvalidOperationException>(() => new PlanetaryGearSet("PS") { SunTeeth = 3 }.Validate());
        }

        [Test]
        public void TestPlanetary_Ratios_FollowTheWillisEquation()
        {
            var set = new PlanetaryGearSet("PS");

            set.Fixed = PlanetaryMember.Ring;
            Assert.AreEqual(3.5, set.Analyze(PlanetaryMember.Sun, 1000.0, PlanetaryMember.Carrier).OverallRatio.Value, Tol, "1 + Zr / Zs");

            set.Fixed = PlanetaryMember.Sun;
            Assert.AreEqual(1.4, set.Analyze(PlanetaryMember.Ring, 1000.0, PlanetaryMember.Carrier).OverallRatio.Value, Tol, "1 + Zs / Zr");

            set.Fixed = PlanetaryMember.Carrier;
            Assert.AreEqual(-2.5, set.Analyze(PlanetaryMember.Sun, 1000.0, PlanetaryMember.Ring).OverallRatio.Value, Tol, "-Zr / Zs");

            var speeds = set.Analyze(PlanetaryMember.Sun, 1000.0, PlanetaryMember.Sun);
            Assert.AreEqual(-1000.0 * 24 / 18, speeds.PlanetRpm, Tol, "With a fixed carrier the planets turn at -Zs / Zp times the sun.");
            StringAssert.Contains("Planetary stage", speeds.ToText());
        }

        [Test]
        public void TestPlanetary_Analyze_RejectsFixedInput()
        {
            var set = new PlanetaryGearSet("PS");

            Assert.Throws<ArgumentException>(() => set.Analyze(PlanetaryMember.Ring, 100.0, PlanetaryMember.Sun));
        }

        [Test]
        public void TestPlanetary_Assembly_HasSunPlanetsRingAndCarrier()
        {
            var set = new PlanetaryGearSet("PS");

            var assembly = set.BuildAssembly();
            var scene = GearAssemblySceneBuilder.Build(assembly);

            Assert.AreEqual(6, assembly.Shafts.Count, "Sun, three planets, ring and carrier plate.");
            Assert.AreEqual(5, assembly.Components.Count());
            Assert.AreEqual(5, scene.Items.Count);
            Assert.AreEqual(0, assembly.Relations.Count);
            foreach (var planetShaft in assembly.Shafts.Where(s => s.Id.Contains("PlanetShaft")))
            {
                Assert.AreEqual(42.0, planetShaft.Placement.Origin.Length, 1e-6, "Planets sit on the pitch circle of the carrier.");
            }

            Assert.AreEqual(0, scene.ShaftItems.Single(s => s.Shaft.Id == "PS.RingShaft").Mesh.Vertices.Count, "The bore of the ring is open.");
            Assert.Greater(scene.ShaftItems.Single(s => s.Shaft.Id == "PS.Carrier").Mesh.Vertices.Count, 0);
        }

        [Test]
        public void TestPlanetary_Planets_AreSpacedEvenly()
        {
            var set = new PlanetaryGearSet("PS") { CarrierAngleRad = 0.4 };

            var assembly = set.BuildAssembly();

            var angles = assembly.Shafts.Where(s => s.Id.Contains("PlanetShaft"))
                .Select(s => Math.Atan2(s.Placement.Origin.Y, s.Placement.Origin.X)).ToList();
            var third = 2.0 * Math.PI / 3.0;
            Assert.AreEqual(0.4, angles[0], 1e-6);
            Assert.AreEqual(Math.Sin(0.4 + third), Math.Sin(angles[1]), 1e-6);
            Assert.AreEqual(Math.Cos(0.4 + third), Math.Cos(angles[1]), 1e-6);
            Assert.AreEqual(Math.Sin(0.4 + 2.0 * third), Math.Sin(angles[2]), 1e-6);
            Assert.AreEqual(Math.Cos(0.4 + 2.0 * third), Math.Cos(angles[2]), 1e-6);
        }

        [Test]
        public void TestPlanetary_EveryPlanetMeshesWithTheRing()
        {
            var set = new PlanetaryGearSet("PS") { SunAngleRad = 0.7, CarrierAngleRad = 0.25 };

            var assembly = set.BuildAssembly();

            var solver = new ParallelAxisMeshSolver();
            var ringPitch = 2.0 * Math.PI / set.RingTeeth;
            var index = 0;
            foreach (var planetShaft in assembly.Shafts.Where(s => s.Id.Contains("PlanetShaft")))
            {
                var planet = planetShaft.Mounts[0].Component;
                var theta = set.CarrierAngleRad + 2.0 * Math.PI * index++ / set.PlanetCount;

                var sunMesh = solver.Solve(set.Sun, planet, set.SunAngleRad - theta);
                Assert.AreEqual(sunMesh.DrivenPhaseRad + theta, planetShaft.RotationRad, 1e-9, "The planet is meshed with the sun.");
                Assert.AreEqual(set.CentreDistance, sunMesh.CentreDistance, Tol);

                var ringMesh = solver.Solve(planet, set.Ring, planetShaft.RotationRad - theta);
                var ringPhase = ringMesh.DrivenPhaseRad + theta;
                var difference = (ringPhase - set.RingAngleRad) / ringPitch;
                Assert.AreEqual(Math.Round(difference), difference, 1e-6, "Every planet gives the ring the same tooth position (modulo one tooth pitch).");
                Assert.AreEqual(set.CentreDistance, ringMesh.CentreDistance, Tol, "The planet-ring distance equals the sun-planet distance.");
            }
        }

        [Test]
        public void TestPlanetary_FixedRing_StaysStillWhenTheSunTurns()
        {
            var set = new PlanetaryGearSet("PS") { Fixed = PlanetaryMember.Ring };

            set.SolveAngles(PlanetaryMember.Sun, 0.0);
            set.BuildAssembly();
            var ringAtZero = set.RingAngleRad;

            set.SolveAngles(PlanetaryMember.Sun, 1.234);
            set.BuildAssembly();

            Assert.AreEqual(ringAtZero, set.RingAngleRad, 1e-9);
            Assert.AreEqual(1.234 * 24.0 / 84.0, set.CarrierAngleRad, 1e-12, "The carrier turns Zs / (Zs + Zr) of the sun.");
        }

        [Test]
        public void TestPlanetary_FixedCarrier_TurnsTheRingBackwards()
        {
            var set = new PlanetaryGearSet("PS") { Fixed = PlanetaryMember.Carrier };

            set.SolveAngles(PlanetaryMember.Sun, 0.0);
            set.BuildAssembly();
            var ringAtZero = set.RingAngleRad;

            set.SolveAngles(PlanetaryMember.Sun, 1.0);
            set.BuildAssembly();

            Assert.AreEqual(-24.0 / 60.0, set.RingAngleRad - ringAtZero, 1e-9, "wr = -ws * Zs / Zr");
        }

        [Test]
        public void TestPlanetary_Assembly_CanBeSerialisedAndExported()
        {
            var set = new PlanetaryGearSet("PS");
            var assembly = set.BuildAssembly();

            var copy = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));
            var scene = GearAssemblySceneBuilder.Build(copy);

            Assert.AreEqual(6, copy.Shafts.Count);
            StringAssert.Contains("o PS.Ring", MeshExporter.ToObj(scene));
        }

        #endregion

        #region Phase 5: bevel gears

        [Test]
        public void TestBevel_ConeAngles_AddUpToTheShaftAngle()
        {
            BevelAssembly(out var g1, out var g2, 20, 40, 90.0).Solve();

            Assert.AreEqual(Math.Atan(20.0 / 40.0) * 180.0 / Math.PI, g1.PitchConeAngleDeg, 1e-9);
            Assert.AreEqual(90.0, g1.PitchConeAngleDeg + g2.PitchConeAngleDeg, 1e-9);

            BevelAssembly(out var h1, out var h2, 18, 27, 70.0).Solve();
            Assert.AreEqual(70.0, h1.PitchConeAngleDeg + h2.PitchConeAngleDeg, 1e-9);
        }

        [TestCase(90.0)]
        [TestCase(75.0)]
        [TestCase(110.0)]
        public void TestBevelSolver_TheConeApexesCoincide(double shaftAngleDeg)
        {
            var assembly = BevelAssembly(out var g1, out var g2, 20, 36, shaftAngleDeg);

            assembly.Solve();

            var s1 = assembly.Shafts[0];
            var s2 = assembly.Shafts[1];
            var apex1 = s1.Placement.Origin + s1.Placement.ZAxis * (3.0 + g1.ApexAxialPosition);
            var apex2 = s2.Placement.Origin + s2.Placement.ZAxis * (-2.0 + g2.ApexAxialPosition);
            Assert.AreEqual(0.0, (apex1 - apex2).Length, 1e-5, "Both pitch cones share one apex.");
            Assert.AreEqual(Math.Cos(shaftAngleDeg * Math.PI / 180.0), s1.Placement.ZAxis.Dot(s2.Placement.ZAxis), 1e-9, "The shaft axes meet at the shaft angle.");
        }

        [Test]
        public void TestBevelSolver_RatioAndPhase_MatchAnExternalSpurPair()
        {
            var assembly = BevelAssembly(out var g1, out var g2, 20, 40);
            assembly.Shafts[0].RotationRad = 0.5;

            assembly.Solve();

            var expected = AlibreGearGenerator.CalculateMeshRotationAngle(false, false, 20, 40) - 0.5 * 20.0 / 40.0;
            Assert.AreEqual(expected, assembly.Shafts[1].RotationRad, 1e-12);
            var report = GearTrainAnalyzer.Analyze(assembly, null, 600.0);
            Assert.AreEqual(-2.0, report.OverallRatio.Value, Tol);
            Assert.AreEqual(2.0, assembly.Relations[0].Result.GearRatio, Tol);
        }

        [Test]
        public void TestBevel_Geometry_TapersTowardsTheApex()
        {
            var assembly = BevelAssembly(out var g1, out _, 20, 40);
            assembly.Solve();

            var geometry = g1.BuildGeometry();

            AssertValidMesh(geometry.Mesh, "bevel");
            // The tooth ends are square to the pitch cone, so the ends are told apart by their distance from the apex.
            var apexZ = g1.ApexAxialPosition;
            double FromApex(Bolsover.Gears.Model.GearPoint3D v) => Math.Sqrt(v.X * v.X + v.Y * v.Y + (v.Z - apexZ) * (v.Z - apexZ));
            var bigEnd = geometry.Mesh.Vertices.Where(v => FromApex(v) >= g1.ConeDistance - 1e-9).Select(v => Math.Sqrt(v.X * v.X + v.Y * v.Y)).Max();
            var smallEnd = geometry.Mesh.Vertices.Where(v => FromApex(v) < g1.ConeDistance - 1e-9).Select(v => Math.Sqrt(v.X * v.X + v.Y * v.Y)).Max();
            Assert.AreEqual(g1.CreateBlankProfile().ToeScale, smallEnd / bigEnd, 1e-6);

            var expectedTip = g1.PitchDiameter / 2.0 + g1.Addendum * Math.Cos(g1.PitchConeAngleRad);
            Assert.AreEqual(expectedTip, bigEnd, 1e-6, "Tip radius = pitch radius + addendum * cos(delta).");
            Assert.Greater(g1.VirtualTeeth, g1.Teeth);
        }

        [Test]
        public void TestBevel_Validate_RejectsAFaceWidthLongerThanTheCone()
        {
            var assembly = BevelAssembly(out var g1, out _, 20, 40);
            g1.FaceWidth = 500.0;

            Assert.Throws<InvalidOperationException>(() => assembly.Solve());
        }

        [Test]
        public void TestBevelSolver_RejectsMismatchedModulesAndForeignGears()
        {
            var assembly = BevelAssembly(out _, out var g2);
            g2.Module = 3.0;
            Assert.Throws<InvalidOperationException>(() => assembly.Solve());

            var mixed = new GearAssembly();
            var spur = Spur("A", 20);
            var bevel = new BevelGearComponent("B");
            var s1 = new Shaft("S1");
            s1.Mount(spur);
            var s2 = new Shaft("S2");
            s2.Mount(bevel);
            mixed.AddShaft(s1);
            mixed.AddShaft(s2);
            mixed.AddRelation(spur, bevel, MeshType.Bevel);
            Assert.Throws<InvalidOperationException>(() => mixed.Solve());
        }

        [Test]
        public void TestBevel_Scene_PlacesTheGearsInWorldSpace()
        {
            var assembly = BevelAssembly(out var g1, out var g2);

            var scene = GearAssemblySceneBuilder.Build(assembly);

            var item2 = scene.Find(g2);
            var axis = assembly.Shafts[1].Placement.ZAxis;
            Assert.AreEqual(-1.0, axis.X, 1e-9, "For a 90 degree pair the second shaft lies along -X.");
            Assert.AreEqual(0.0, item2.LocalToWorld.TransformDirection(Vector3D.UnitZ).Dot(Vector3D.UnitZ), 1e-9);
            AssertValidMesh(scene.Find(g1).Mesh, "world bevel 1");
            AssertValidMesh(scene.Find(g2).Mesh, "world bevel 2");
        }

        #endregion

        #region Phase 6: worm gear sets

        [Test]
        public void TestWorm_Parameters_FollowTheStandardFormulas()
        {
            var worm = new WormComponent("W") { Starts = 2, Module = 2.0, DiameterQuotient = 10.0, PressureAngleDeg = 20.0 };

            Assert.AreEqual(20.0, worm.PitchDiameter, Tol);
            Assert.AreEqual(Math.PI * 2.0, worm.AxialPitch, Tol);
            Assert.AreEqual(2.0 * Math.PI * 2.0, worm.Lead, Tol);
            Assert.AreEqual(Math.Atan(2.0 / 10.0) * 180.0 / Math.PI, worm.LeadAngleDeg, 1e-9);
            Assert.AreEqual(12.0, worm.TipRadius, Tol);
            Assert.AreEqual(10.0 - 2.0 - 0.5, worm.RootRadius, Tol);
            Assert.AreEqual(Math.Atan(Math.Tan(20.0 * Math.PI / 180.0) / Math.Cos(worm.LeadAngleRad)), worm.AxialPressureAngleRad, 1e-12);
        }

        [Test]
        public void TestWorm_ThreadRadius_StaysBetweenRootAndTip()
        {
            var worm = new WormComponent("W") { Starts = 2 };
            var min = double.MaxValue;
            var max = double.MinValue;
            for (var i = 0; i < 400; i++)
            {
                for (var j = 0; j < 40; j++)
                {
                    var r = worm.RadiusAt(2.0 * Math.PI * i / 400.0, j * 0.37);
                    min = Math.Min(min, r);
                    max = Math.Max(max, r);
                }
            }

            Assert.AreEqual(worm.RootRadius, min, 1e-9);
            Assert.AreEqual(worm.TipRadius, max, 1e-9);
        }

        [Test]
        public void TestWorm_LeftHandThread_IsTheMirrorOfTheRightHandThread()
        {
            var right = new WormComponent("R") { Starts = 1, HelicalDirection = "R" };
            var left = new WormComponent("L") { Starts = 1, HelicalDirection = "L" };

            for (var i = 0; i < 50; i++)
            {
                var theta = 0.123 * i;
                var z = 0.31 * i;
                Assert.AreEqual(right.RadiusAt(-theta, z), left.RadiusAt(theta, z), 1e-12);
            }
        }

        [Test]
        public void TestWorm_ThreadRepeatsEveryLeadAlongTheAxis()
        {
            var worm = new WormComponent("W") { Starts = 3 };

            Assert.AreEqual(worm.RadiusAt(1.1, 4.0), worm.RadiusAt(1.1, 4.0 + worm.AxialPitch), 1e-9, "One axial pitch is one thread.");
            Assert.AreEqual(worm.RadiusAt(0.0, 1.0), worm.RadiusAt(2.0 * Math.PI / 3.0, 1.0 + worm.AxialPitch), 1e-9, "A start repeats every third of a turn.");
        }

        [Test]
        public void TestWorm_Mesh_IsClosedWithOutwardFaces()
        {
            var worm = new WormComponent("W") { Starts = 2, FaceWidth = 25.0 };

            var geometry = worm.BuildGeometry();

            AssertValidMesh(geometry.Mesh, "worm");
            Assert.AreEqual(12.5, geometry.Mesh.Vertices.Max(v => v.Z), Tol);
            Assert.AreEqual(-12.5, geometry.Mesh.Vertices.Min(v => v.Z), Tol);
            Assert.AreEqual(worm.AngularSegments + 1, geometry.Polylines[0].Count, "The outline is the closed cross-section.");
        }

        [Test]
        public void TestWorm_Validate_RejectsBadParameters()
        {
            Assert.Throws<InvalidOperationException>(() => new WormComponent("W") { Starts = 0 }.Validate());
            Assert.Throws<InvalidOperationException>(() => new WormComponent("W") { DiameterQuotient = 2.0 }.Validate());
            Assert.Throws<InvalidOperationException>(() => new WormWheelComponent("G") { Teeth = 5 }.Validate());
        }

        [Test]
        public void TestWormSolver_PlacesTheWheelOnACrossedShaft()
        {
            var assembly = WormAssembly(out var worm, out var wheel);

            assembly.Solve();

            var wheelShaft = assembly.Shafts[1];
            Assert.AreEqual(40.0, wheelShaft.Placement.Origin.X, Tol, "(d_worm + d_wheel) / 2 = (20 + 60) / 2.");
            Assert.AreEqual(1.0, wheelShaft.Placement.ZAxis.Y, Tol, "The wheel axis is perpendicular to the worm axis and to the line of centres.");
            Assert.AreEqual(0.0, wheelShaft.Placement.ZAxis.Dot(assembly.Shafts[0].Placement.ZAxis), Tol);
            Assert.AreEqual(worm.LeadAngleDeg, wheel.LeadAngleDeg, 1e-12);
            Assert.AreEqual(30.0, assembly.Relations[0].Result.GearRatio, Tol);
        }

        [TestCase("R", 1, 30, -1.0 / 30.0)]
        [TestCase("L", 1, 30, 1.0 / 30.0)]
        [TestCase("R", 2, 40, -2.0 / 40.0)]
        public void TestWormSolver_SpeedRatio_UsesStartsAndHand(string hand, int starts, int teeth, double expectedFactor)
        {
            var assembly = WormAssembly(out var worm, out var wheel, starts, teeth, hand);

            Assert.AreEqual(expectedFactor, GearTrainAnalyzer.SpeedFactor(assembly.Relations[0]), 1e-12);
            var report = GearTrainAnalyzer.Analyze(assembly, null, 1500.0);
            Assert.AreEqual(1500.0 * expectedFactor, report.Shafts[1].SpeedRpm.Value, 1e-6);
            Assert.AreEqual(wheel.Teeth / (double)worm.Starts * Math.Sign(1.0 / expectedFactor), report.OverallRatio.Value, 1e-6);
        }

        [Test]
        public void TestWormSolver_ThePhaseFollowsTheWormAngle()
        {
            var assembly = WormAssembly(out _, out _, 2, 40);
            assembly.Solve();
            var atZero = assembly.Shafts[1].RotationRad;

            assembly.Shafts[0].RotationRad = 1.0;
            assembly.Solve();

            Assert.AreEqual(-1.0 * 2.0 / 40.0, assembly.Shafts[1].RotationRad - atZero, 1e-12);
        }

        [TestCase("R", 1, 0.0)]
        [TestCase("R", 1, 0.9)]
        [TestCase("R", 1, 2.3)]
        [TestCase("R", 2, 0.4)]
        [TestCase("L", 1, 1.7)]
        public void TestWormSolver_TheThreadMeshesWithTheWheelInTheCentralPlane(string hand, int starts, double wormAngle)
        {
            var assembly = WormAssembly(out var worm, out var wheel, starts, 30, hand);
            assembly.Shafts[0].RotationRad = wormAngle;
            assembly.Solve();
            var result = assembly.Relations[0].Result;

            // Wheel outline in the central plane, turned by the solved phase, in the wheel's frame (x along the worm axis).
            var phase = result.DrivenPhaseRad;
            var wheelOutline = SolidMeshBuilder.Flatten(wheel.BuildGeometry().Polylines)
                .Select(p => new GearPoint(p.X * Math.Cos(phase) - p.Y * Math.Sin(phase), p.X * Math.Sin(phase) + p.Y * Math.Cos(phase)))
                .ToList();

            // The thread surface in the same plane on the wheel's side of the worm (angle 0 of the world frame).
            var thread = new List<GearPoint>();
            for (var z = -12.0; z <= 12.0; z += 0.05)
            {
                thread.Add(new GearPoint(z, worm.RadiusAt(-wormAngle, z) - result.CentreDistance));
            }

            Assert.Less(Penetration(thread, wheelOutline), 0.05, "The thread must not cut into the wheel teeth.");

            // And the wheel teeth must be in the thread space, not far away from it.
            var closest = thread.Min(t => wheelOutline.Min(w => Math.Sqrt((w.X - t.X) * (w.X - t.X) + (w.Y - t.Y) * (w.Y - t.Y))));
            Assert.Less(closest, 0.6, "The thread and the wheel are in contact.");
        }

        [Test]
        public void TestWormWheel_UsesTheLeadAngleAsHelixAngle()
        {
            WormAssembly(out var worm, out var wheel).Solve();

            var geometry = wheel.BuildGeometry();

            // The existing helical gear mesher is reused unchanged, so only the structure of its mesh is checked here.
            AssertValidMesh(geometry.Mesh, "worm wheel", false);
            Assert.AreEqual(worm.LeadAngleDeg, geometry.Profile.Parameters["helixAngle"], 1e-9);
        }

        [Test]
        public void TestWorm_Scene_BuildsWormAndWheelWithOutwardFaces()
        {
            var assembly = WormAssembly(out var worm, out var wheel);

            var scene = GearAssemblySceneBuilder.Build(assembly);

            AssertValidMesh(scene.Find(worm).Mesh, "world worm");
            AssertValidMesh(scene.Find(wheel).Mesh, "world wheel", false);
        }

        #endregion

        #region Mixed trains and the generic solver path

        [Test]
        public void TestMixedTrain_WormThenSpurAndRack_PropagatesSpeedsThroughEveryStage()
        {
            var worm = new WormComponent("W") { Starts = 1, Module = 2.0 };
            var wheel = new WormWheelComponent("G") { Teeth = 30, Module = 2.0 };
            var pinion = Spur("P", 20);
            var rack = new RackComponent("R") { Teeth = 10, Module = 2.0 };

            var assembly = new GearAssembly();
            var sw = new Shaft("SW");
            sw.Mount(worm);
            var sg = new Shaft("SG");
            sg.Mount(wheel, 0.0);
            sg.Mount(pinion, 20.0);
            var sr = new Shaft("SR");
            sr.Mount(rack);
            assembly.AddShaft(sw);
            assembly.AddShaft(sg);
            assembly.AddShaft(sr);
            assembly.AddRelation(worm, wheel, MeshType.Worm);
            assembly.AddRelation(pinion, rack, MeshType.Rack, 180.0);

            var report = GearTrainAnalyzer.Analyze(assembly, null, 3000.0);

            Assert.AreEqual(-100.0, report.Shafts[1].SpeedRpm.Value, 1e-6, "3000 rpm / 30 teeth, reversed.");
            Assert.AreEqual(-100.0 * 2.0 * Math.PI * 20.0, report.Shafts[2].SpeedRpm.Value, 1e-4);
            Assert.AreEqual(2, report.Stages.Count);
            Assert.AreEqual(0, assembly.Validate().Count);

            var scene = GearAssemblySceneBuilder.Build(assembly);
            Assert.AreEqual(4, scene.Items.Count);
            var pinionCentre = sg.Placement.Origin + sg.Placement.ZAxis * 20.0;
            Assert.AreEqual(20.0, (sr.Placement.Origin - pinionCentre).Length, 1e-6, "The rack datum line is one pitch radius from the pinion centre.");
            Assert.AreEqual(0.0, sr.Placement.ZAxis.Cross(sg.Placement.ZAxis).Length, 1e-9, "The rack carrier is parallel to the pinion shaft.");
        }

        [Test]
        public void TestSolve_ParallelOnlyAssemblies_AreUnchangedByTheGenericPath()
        {
            var a = Spur("A", 20);
            var b = Spur("B", 30);
            var assembly = new GearAssembly();
            var s1 = new Shaft("S1");
            s1.Mount(a);
            var s2 = new Shaft("S2");
            s2.Mount(b);
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            assembly.AddRelation(a, b);

            assembly.Solve();

            Assert.IsNull(assembly.Relations[0].Result.DrivenFrame, "Parallel meshes still use the original placement path.");
            Assert.AreEqual(50.0, s2.Placement.Origin.X, Tol);
        }

        #endregion

        #region Phase 7: persistence

        private static void AssertSameSolution(GearAssembly expected, GearAssembly actual)
        {
            expected.Solve();
            actual.Solve();
            Assert.AreEqual(expected.Shafts.Count, actual.Shafts.Count);
            Assert.AreEqual(expected.Relations.Count, actual.Relations.Count);
            for (var i = 0; i < expected.Shafts.Count; i++)
            {
                var e = expected.Shafts[i];
                var a = actual.Shafts[i];
                Assert.AreEqual(e.Id, a.Id);
                Assert.AreEqual(0.0, (e.Placement.Origin - a.Placement.Origin).Length, 1e-9, "origin of " + e.Id);
                Assert.AreEqual(0.0, (e.Placement.ZAxis - a.Placement.ZAxis).Length, 1e-9, "axis of " + e.Id);
                Assert.AreEqual(e.RotationRad, a.RotationRad, 1e-9, "rotation of " + e.Id);
                Assert.AreEqual(e.Mounts.Count, a.Mounts.Count);
                for (var j = 0; j < e.Mounts.Count; j++)
                {
                    Assert.AreEqual(e.Mounts[j].Component.GetType(), a.Mounts[j].Component.GetType());
                    Assert.AreEqual(e.Mounts[j].Component.Id, a.Mounts[j].Component.Id);
                    Assert.AreEqual(e.Mounts[j].AxialOffset, a.Mounts[j].AxialOffset, 1e-12);
                    Assert.AreEqual(e.Mounts[j].PhaseRad, a.Mounts[j].PhaseRad, 1e-9);
                }
            }

            for (var i = 0; i < expected.Relations.Count; i++)
            {
                Assert.AreEqual(expected.Relations[i].MeshType, actual.Relations[i].MeshType);
                Assert.AreEqual(expected.Relations[i].Driver.Id, actual.Relations[i].Driver.Id);
                Assert.AreEqual(expected.Relations[i].Driven.Id, actual.Relations[i].Driven.Id);
            }
        }

        [Test]
        public void TestSerializer_RoundTrip_Rack()
        {
            var assembly = RackAssembly(out _, out var rack, 18, 30.0);
            rack.Backlash = 0.1;
            assembly.Shafts[0].RotationRad = 0.8;

            var copy = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            AssertSameSolution(assembly, copy);
            Assert.AreEqual(0.1, ((RackComponent)copy.Shafts[1].Mounts[0].Component).Backlash, 1e-12);
        }

        [Test]
        public void TestSerializer_RoundTrip_Bevel()
        {
            var assembly = BevelAssembly(out _, out _, 21, 33, 80.0);
            assembly.Shafts[0].RotationRad = 1.1;

            var copy = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            AssertSameSolution(assembly, copy);
        }

        [Test]
        public void TestSerializer_RoundTrip_Worm()
        {
            var assembly = WormAssembly(out _, out _, 2, 36, "L");
            assembly.Shafts[0].RotationRad = 0.6;

            var copy = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            AssertSameSolution(assembly, copy);
            var worm = (WormComponent)copy.Shafts[0].Mounts[0].Component;
            Assert.AreEqual(2, worm.Starts);
            Assert.IsFalse(worm.IsRightHand);
        }

        [Test]
        public void TestSerializer_RoundTrip_CompoundTrainWithInternalGearAndHelicalGear()
        {
            var a = Spur("A", 20);
            var b = Spur("B", 40);
            b.HelixAngleDeg = 0.0;
            var c = Spur("C", 20);
            var d = new InvoluteGearComponent("D") { Teeth = 50, Module = 2.0, PressureAngleDeg = 20.0, IsInternal = true, ProfileShift = 0.0 };
            var assembly = new GearAssembly();
            var s1 = new Shaft("S1") { Diameter = 8.0 };
            s1.Mount(a);
            var s2 = new Shaft("S2");
            s2.Mount(b, 0.0);
            s2.Mount(c, 25.0);
            var s3 = new Shaft("S3");
            s3.Mount(d, 25.0);
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            assembly.AddShaft(s3);
            assembly.AddRelation(a, b);
            assembly.AddRelation(c, d, MeshType.Parallel, 45.0);
            s1.RotationRad = 0.3;

            var json = AssemblySerializer.ToJson(assembly);
            var copy = AssemblySerializer.FromJson(json);

            AssertSameSolution(assembly, copy);
            StringAssert.Contains("\"Type\": \"Involute\"", json);
            Assert.AreEqual(8.0, copy.Shafts[0].Diameter, 1e-12);
        }

        [Test]
        public void TestSerializer_SaveAndLoad_UseAFile()
        {
            var assembly = RackAssembly(out _, out _);
            var path = Path.Combine(Path.GetTempPath(), "gears_test_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                AssemblySerializer.Save(assembly, path);
                var copy = AssemblySerializer.Load(path);

                AssertSameSolution(assembly, copy);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void TestSerializer_RejectsInvalidFiles()
        {
            Assert.Throws<InvalidDataException>(() => AssemblySerializer.FromJson("{ not json"));
            Assert.Throws<InvalidDataException>(() => AssemblySerializer.FromJson("{\"Version\": 99}"));
            Assert.Throws<ArgumentException>(() => AssemblySerializer.FromJson("  "));

            const string unknownGear =
                "{\"Version\":1,\"Shafts\":[{\"Id\":\"S\",\"Origin\":[0,0,0],\"Axis\":[0,0,1],\"Reference\":[1,0,0],\"Gears\":[{\"Id\":\"X\",\"Type\":\"Hyperboloid\"}]}]}";
            Assert.Throws<InvalidDataException>(() => AssemblySerializer.FromJson(unknownGear));

            const string badRelation =
                "{\"Version\":1,\"Shafts\":[],\"Relations\":[{\"Driver\":\"A\",\"Driven\":\"B\",\"MeshType\":\"Parallel\"}]}";
            Assert.Throws<InvalidDataException>(() => AssemblySerializer.FromJson(badRelation));
        }

        #endregion

        #region Phase 7: export

        [Test]
        public void TestExporter_Stl_HasOneFacetPerTriangle()
        {
            var assembly = RackAssembly(out _, out _);
            var scene = GearAssemblySceneBuilder.Build(assembly);

            var stl = MeshExporter.ToStl(scene);

            var triangles = scene.Items.Sum(i => i.Mesh.TriangleIndices.Count / 3);
            var facets = stl.Split('\n').Count(l => l.TrimStart().StartsWith("facet normal", StringComparison.Ordinal));
            Assert.LessOrEqual(facets, triangles);
            Assert.GreaterOrEqual(facets, triangles - triangles / 100, "Only degenerate (zero area) triangles are left out.");
            StringAssert.StartsWith("solid P", stl);
            StringAssert.Contains("endsolid R", stl);
        }

        [Test]
        public void TestExporter_Obj_NumbersVerticesAcrossObjects()
        {
            var assembly = WormAssembly(out _, out _);
            var scene = GearAssemblySceneBuilder.Build(assembly);

            var obj = MeshExporter.ToObj(scene, true);

            var lines = obj.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
            var vertexCount = lines.Count(l => l.StartsWith("v ", StringComparison.Ordinal));
            var maxIndex = lines.Where(l => l.StartsWith("f ", StringComparison.Ordinal))
                .SelectMany(l => l.Substring(2).Split(' ').Select(t => int.Parse(t.Split('/')[0], CultureInfo.InvariantCulture))).Max();
            Assert.AreEqual(maxIndex, vertexCount, "The last face refers to the last vertex of the whole file.");
            Assert.AreEqual(scene.Items.Sum(i => i.Mesh.Vertices.Count) + scene.ShaftItems.Sum(i => i.Mesh.Vertices.Count), vertexCount);
            StringAssert.Contains("o W", obj);
            StringAssert.Contains("o S1", obj);
        }

        [Test]
        public void TestExporter_UsesTheInvariantCultureAndSevenDecimals()
        {
            var previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                var mesh = new RackComponent("R") { Teeth = 2 }.BuildGeometry().Mesh;

                var stl = MeshExporter.ToStl(mesh, "my rack");

                StringAssert.Contains("solid my_rack", stl);
                var vertexLine = stl.Split('\n').First(l => l.TrimStart().StartsWith("vertex", StringComparison.Ordinal)).Trim();
                var numbers = vertexLine.Split(' ').Skip(1).ToList();
                Assert.AreEqual(3, numbers.Count);
                foreach (var number in numbers)
                {
                    StringAssert.DoesNotContain(",", number);
                    Assert.AreEqual(7, number.Substring(number.IndexOf('.') + 1).Length, number);
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Test]
        public void TestExporter_RejectsANullScene()
        {
            Assert.Throws<ArgumentNullException>(() => MeshExporter.ToStl((AssemblyScene)null));
            Assert.Throws<ArgumentNullException>(() => MeshExporter.ToObj(null));
        }

        #endregion

        #region Phase 7: animation

        [Test]
        public void TestAnimator_AdvancesTheInputAngleAndWraps()
        {
            var assembly = RackAssembly(out _, out _);
            var animator = new GearTrainAnimator(assembly) { SpeedRpm = 60.0 };

            animator.Advance(0.25);
            Assert.AreEqual(Math.PI / 2.0, animator.AngleRad, 1e-12);

            animator.Advance(1.0);
            Assert.AreEqual(Math.PI / 2.0, animator.AngleRad, 1e-9, "A full turn returns to the same angle.");

            animator.SpeedRpm = -60.0;
            animator.Advance(0.5);
            Assert.AreEqual(Math.PI * 3.0 / 2.0, animator.AngleRad, 1e-9, "Running backwards past zero wraps into [0, 2 pi).");
        }

        [Test]
        public void TestAnimator_Step_MovesTheDrivenParts()
        {
            var assembly = BevelAssembly(out _, out var g2);
            var animator = new GearTrainAnimator(assembly) { SpeedRpm = 30.0 };
            var before = GearAssemblySceneBuilder.Build(assembly).Find(g2).Mesh.Vertices[0];

            var scene = animator.Step(0.5);

            var after = scene.Find(g2).Mesh.Vertices[0];
            Assert.IsTrue(Math.Abs(before.X - after.X) + Math.Abs(before.Y - after.Y) + Math.Abs(before.Z - after.Z) > 1e-3, "The driven bevel gear has turned.");
            Assert.AreEqual(Math.PI / 2.0, animator.AngleRad, 1e-12);
        }

        [Test]
        public void TestAnimator_RejectsBadArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new GearTrainAnimator(null));
            Assert.Throws<InvalidOperationException>(() => new GearTrainAnimator(new GearAssembly()));
            var assembly = RackAssembly(out _, out _);
            Assert.Throws<ArgumentException>(() => new GearTrainAnimator(assembly, new Shaft("X")));
            Assert.Throws<ArgumentException>(() => new GearTrainAnimator(assembly).Advance(double.NaN));
        }

        #endregion
    }
}
