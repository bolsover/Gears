using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Alibre;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// The pose of an exported part: <see cref="AlibreGearExporter.GetPartPlacement"/> turns every part by the phase of its mount, so a
    /// meshed train exported to Alibre is in the same angular pose as the 3D scene (a cycloidal pair meshes in Alibre as in the editor,
    /// without a half tooth offset).
    /// </summary>
    [TestFixture]
    public class TestAlibrePartPhase
    {
        private const double Tol = 1e-6;

        private static CycloidalGearComponent Gear(string id, CycloidalGearRole role, int teeth)
        {
            return new CycloidalGearComponent(id) { Role = role, Teeth = teeth, Module = 2.0 };
        }

        /// <summary>
        /// Largest distance (mm) from a point of the outline of a part, placed by the exporter, to the nearest point of the outline of its scene
        /// item. The start tooth of the outline does not matter: a part turned by whole teeth is in the same pose.
        /// </summary>
        private static double PlacementError(AssemblyScene scene, Shaft shaft, ShaftMount mount, IReadOnlyList<GearPoint> localRing)
        {
            var item = scene.Find(mount.Component);
            Assert.IsNotNull(item, "The gear is in the scene.");
            var placement = AlibreGearExporter.GetPartPlacement(shaft, mount);
            var sceneRing = item.Polylines[0];
            Assert.GreaterOrEqual(sceneRing.Count, localRing.Count);

            var worst = 0.0;
            foreach (var point in localRing)
            {
                var world = placement.TransformPoint(new Vector3D(point.X, point.Y, 0.0));
                var nearest = double.MaxValue;
                foreach (var scenePoint in sceneRing)
                {
                    var dx = world.X - scenePoint.X;
                    var dy = world.Y - scenePoint.Y;
                    nearest = Math.Min(nearest, Math.Sqrt(dx * dx + dy * dy));
                }

                worst = Math.Max(worst, nearest);
            }

            return worst;
        }

        /// <summary>Distance (mm) from a point to the nearest point of an open or closed polyline.</summary>
        private static double DistanceToPolyline(double x, double y, IReadOnlyList<GearPoint> polyline)
        {
            var best = double.MaxValue;
            for (var i = 0; i + 1 < polyline.Count; i++)
            {
                var ax = polyline[i].X;
                var ay = polyline[i].Y;
                var bx = polyline[i + 1].X;
                var by = polyline[i + 1].Y;
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

        /// <summary>Distance (mm) from a point to the nearest point of any of the polylines (an involute gear has one outline per tooth).</summary>
        private static double DistanceToPolylines(double x, double y, IEnumerable<List<GearPoint>> polylines)
        {
            return polylines.Min(polyline => DistanceToPolyline(x, y, polyline));
        }

        /// <summary>
        /// Checks that the part is placed with the same orientation as the scene item, and at the same place apart from a shift along the
        /// shaft axis (the export extrudes from the sketch plane, so its axial start differs from the scene's mid-plane).
        /// </summary>
        private static void AssertSamePose(AssemblyScene scene, Shaft shaft, ShaftMount mount, string label)
        {
            var item = scene.Find(mount.Component);
            Assert.IsNotNull(item, label + ": the gear is in the scene.");
            var placement = AlibreGearExporter.GetPartPlacement(shaft, mount);
            var local = item.LocalToWorld;

            foreach (var axis in new[] { Vector3D.UnitX, Vector3D.UnitY, Vector3D.UnitZ })
            {
                var a = placement.TransformDirection(axis);
                var b = local.TransformDirection(axis);
                Assert.AreEqual(0.0, Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z)), 1e-9,
                    label + ": the part has the orientation of the scene.");
            }

            var shift = new Vector3D(placement.Translation.X - local.Translation.X, placement.Translation.Y - local.Translation.Y,
                placement.Translation.Z - local.Translation.Z);
            var along = placement.TransformDirection(Vector3D.UnitZ);
            var cross = new Vector3D(shift.Y * along.Z - shift.Z * along.Y, shift.Z * along.X - shift.X * along.Z, shift.X * along.Y - shift.Y * along.X);
            Assert.AreEqual(0.0, Math.Sqrt(cross.X * cross.X + cross.Y * cross.Y + cross.Z * cross.Z), 1e-9, label + ": the parts differ only along the shaft axis.");
        }

        [TestCase(20, 18, 0.0, 0.0, TestName = "Involute_EvenDriven")]
        [TestCase(20, 17, 0.0, 0.0, TestName = "Involute_OddDriven")]
        [TestCase(21, 18, 0.0, 0.0, TestName = "Involute_OddDriver_EvenDriven")]
        [TestCase(20, 18, 0.0, 0.8, TestName = "Involute_DriverRotation")]
        [TestCase(24, 15, 30.0, -0.45, TestName = "Involute_CentreLine30_DriverRotation")]
        public void TestInvoluteSpurPair_ExportedToothSketchLiesOnTheSceneOutline(int driverTeeth, int drivenTeeth, double thetaDeg, double driverRotation)
        {
            var assembly = new GearAssembly();
            var shaftA = new Shaft("S1") { RotationRad = driverRotation };
            var shaftB = new Shaft("S2");
            var driver = new InvoluteGearComponent("A") { Teeth = driverTeeth, Module = 2.0, PressureAngleDeg = 20.0, FaceWidth = 10.0 };
            var driven = new InvoluteGearComponent("B") { Teeth = drivenTeeth, Module = 2.0, PressureAngleDeg = 20.0, FaceWidth = 10.0 };
            var driverMount = shaftA.Mount(driver, 0.0);
            var drivenMount = shaftB.Mount(driven, 0.0);
            assembly.AddShaft(shaftA);
            assembly.AddShaft(shaftB);
            assembly.AddRelation(driver, driven, MeshType.Parallel, thetaDeg);

            var scene = GearAssemblySceneBuilder.Build(assembly);

            foreach (var pair in new[] { Tuple.Create(shaftA, driverMount), Tuple.Create(shaftB, drivenMount) })
            {
                var gear = (InvoluteGearComponent)pair.Item2.Component;
                var placement = AlibreGearExporter.GetPartPlacement(pair.Item1, pair.Item2);
                var sceneOutlines = scene.Find(gear).Polylines;
                var worst = 0.0;
                var checkedPoints = 0;
                foreach (var figure in SingleToothSketchBuilder.Build(gear).Figures)
                {
                    var points = figure.Points ?? new List<GearPoint> { new GearPoint(figure.StartX, figure.StartY), new GearPoint(figure.EndX, figure.EndY) };
                    foreach (var point in points)
                    {
                        var x = point.X / SingleToothSketchBuilder.MmToAlibreUnits;
                        var y = point.Y / SingleToothSketchBuilder.MmToAlibreUnits;

                        // The radial closing lines of the wedge run to the axis: only the points of the tooth outline are on the scene outline.
                        if (Math.Sqrt(x * x + y * y) < gear.PitchDiameter / 2.0 - 1.4 * gear.Module) continue;

                        var world = placement.TransformPoint(new Vector3D(x, y, 0.0));
                        worst = Math.Max(worst, DistanceToPolylines(world.X, world.Y, sceneOutlines));
                        checkedPoints++;
                    }
                }

                Console.WriteLine($"[DEBUG_LOG] involute {gear.Teeth} teeth: phase {pair.Item2.PhaseRad:0.000000} rad, {checkedPoints} points, worst distance {worst:0.000000} mm");
                Assert.Greater(checkedPoints, 10);
                Assert.Less(worst, 0.05, $"The exported tooth of gear {gear.Id} lies on the outline of the scene.");
            }
        }

        [Test]
        public void TestHerringbonePair_ExportedPartsHaveTheScenePose()
        {
            var assembly = new GearAssembly();
            var shaftA = new Shaft("S1") { RotationRad = 0.6 };
            var shaftB = new Shaft("S2");
            var driver = new InvoluteGearComponent("A") { Teeth = 20, Module = 2.0, FaceWidth = 12.0, HelixAngleDeg = 20.0, HelicalDirection = "R", IsHerringbone = true };
            var driven = new InvoluteGearComponent("B") { Teeth = 18, Module = 2.0, FaceWidth = 12.0, HelixAngleDeg = 20.0, HelicalDirection = "L", IsHerringbone = true };
            var driverMount = shaftA.Mount(driver, 6.0);
            var drivenMount = shaftB.Mount(driven, 6.0);
            assembly.AddShaft(shaftA);
            assembly.AddShaft(shaftB);
            assembly.AddRelation(driver, driven, MeshType.Parallel, 20.0);

            var scene = GearAssemblySceneBuilder.Build(assembly);

            Assert.IsTrue(AlibreGearExporter.IsHerringbone(driven));
            Assert.Greater(Math.Abs(drivenMount.PhaseRad % (2.0 * Math.PI)), 1e-3, "The driven gear is turned in the scene.");
            AssertSamePose(scene, shaftA, driverMount, "driver");
            AssertSamePose(scene, shaftB, drivenMount, "driven");
        }

        [TestCase("R", 0.0, TestName = "Worm_RightHand")]
        [TestCase("L", 0.9, TestName = "Worm_LeftHand_WormRotation")]
        public void TestWormPair_ExportedPartsHaveTheScenePose(string hand, double wormRotation)
        {
            var assembly = new GearAssembly();
            var shaftA = new Shaft("S1") { RotationRad = wormRotation };
            var shaftB = new Shaft("S2");
            var worm = new WormComponent("W") { Starts = 2, Module = 2.0, PressureAngleDeg = 20.0, DiameterQuotient = 10.0, HelicalDirection = hand, FaceWidth = 40.0 };
            var wheel = new WormWheelComponent("G") { Teeth = 30, Module = 2.0, PressureAngleDeg = 20.0, FaceWidth = 12.0 };
            var wormMount = shaftA.Mount(worm, 0.0);
            var wheelMount = shaftB.Mount(wheel, 0.0);
            assembly.AddShaft(shaftA);
            assembly.AddShaft(shaftB);
            assembly.AddRelation(worm, wheel, MeshType.Worm, 0.0);

            var scene = GearAssemblySceneBuilder.Build(assembly);

            Console.WriteLine($"[DEBUG_LOG] worm phase {wormMount.PhaseRad:0.000000} rad, wheel phase {wheelMount.PhaseRad:0.000000} rad");
            AssertSamePose(scene, shaftA, wormMount, "worm");
            AssertSamePose(scene, shaftB, wheelMount, "worm wheel");
        }

        [Test]
        public void TestWormWheel_ExportedToothSketchLiesOnTheSceneOutline()
        {
            var assembly = new GearAssembly();
            var shaftA = new Shaft("S1") { RotationRad = 0.3 };
            var shaftB = new Shaft("S2");
            var worm = new WormComponent("W") { Starts = 1, Module = 2.0, PressureAngleDeg = 20.0, DiameterQuotient = 10.0, FaceWidth = 40.0 };
            var wheel = new WormWheelComponent("G") { Teeth = 30, Module = 2.0, PressureAngleDeg = 20.0, FaceWidth = 12.0 };
            shaftA.Mount(worm, 0.0);
            var wheelMount = shaftB.Mount(wheel, 0.0);
            assembly.AddShaft(shaftA);
            assembly.AddShaft(shaftB);
            assembly.AddRelation(worm, wheel, MeshType.Worm, 0.0);

            var scene = GearAssemblySceneBuilder.Build(assembly);

            var placement = AlibreGearExporter.GetPartPlacement(shaftB, wheelMount);
            var gear = wheel.ToInvoluteGear();
            var sceneOutlines = scene.Find(wheel).Polylines;
            var worst = 0.0;
            foreach (var figure in SingleToothSketchBuilder.Build(gear).Figures)
            {
                var points = figure.Points ?? new List<GearPoint> { new GearPoint(figure.StartX, figure.StartY), new GearPoint(figure.EndX, figure.EndY) };
                foreach (var point in points)
                {
                    var x = point.X / SingleToothSketchBuilder.MmToAlibreUnits;
                    var y = point.Y / SingleToothSketchBuilder.MmToAlibreUnits;
                    if (Math.Sqrt(x * x + y * y) < gear.PitchDiameter / 2.0 - 1.4 * gear.Module) continue;

                    // The scene outline is the mid-plane section projected on the world XY plane; the exported part starts half a face width
                    // lower along the (tilted) shaft axis, so the point is taken at the mid-plane of the part.
                    var world = placement.TransformPoint(new Vector3D(x, y, wheel.FaceWidth / 2.0));
                    worst = Math.Max(worst, DistanceToPolylines(world.X, world.Y, sceneOutlines));
                }
            }

            Console.WriteLine($"[DEBUG_LOG] worm wheel: phase {wheelMount.PhaseRad:0.000000} rad, worst distance {worst:0.000000} mm");
            Assert.Less(worst, 0.1, "The exported worm wheel tooth lies on the outline of the scene (the face section of the helical wheel).");
        }

        [TestCase(0.9, 0.0, TestName = "Rack_PinionRotated")]
        [TestCase(-1.4, 30.0, TestName = "Rack_PinionRotated_CentreLine30")]
        public void TestRack_ExportedRackSlidesByThePhaseOfTheScene(double pinionRotation, double thetaDeg)
        {
            var assembly = new GearAssembly();
            var shaftA = new Shaft("S1") { RotationRad = pinionRotation };
            var shaftB = new Shaft("S2");
            var pinion = new InvoluteGearComponent("P") { Teeth = 20, Module = 2.0, PressureAngleDeg = 20.0, FaceWidth = 15.0 };
            var rack = new RackComponent("R") { Teeth = 12, Module = 2.0, PressureAngleDeg = 20.0, FaceWidth = 15.0 };
            var pinionMount = shaftA.Mount(pinion, 0.0);
            var rackMount = shaftB.Mount(rack, 0.0);
            assembly.AddShaft(shaftA);
            assembly.AddShaft(shaftB);
            assembly.AddRelation(pinion, rack, MeshType.Rack, thetaDeg);

            var scene = GearAssemblySceneBuilder.Build(assembly);

            Console.WriteLine($"[DEBUG_LOG] rack slide {rackMount.PhaseRad:0.000000} mm, pinion phase {pinionMount.PhaseRad:0.000000} rad");
            Assert.Greater(Math.Abs(rackMount.PhaseRad), 1.0, "The rack has slid.");
            AssertSamePose(scene, shaftA, pinionMount, "pinion");
            AssertSamePose(scene, shaftB, rackMount, "rack");

            var slide = AlibreGearExporter.GetPartPlacement(shaftB, rackMount).Translation;
            var atZero = AlibreGearExporter.GetPartPlacement(shaftB, new ShaftMount(rack, rackMount.AxialOffset)).Translation;
            var along = shaftB.Placement.ToTransform().TransformDirection(Vector3D.UnitX);
            var moved = new Vector3D(slide.X - atZero.X, slide.Y - atZero.Y, slide.Z - atZero.Z);
            Assert.AreEqual(rackMount.PhaseRad, moved.X * along.X + moved.Y * along.Y + moved.Z * along.Z, 1e-9, "The part is moved along the rack by its slide.");
        }

        [TestCase(20, 40, 90.0, 0.0, 0.0, TestName = "Bevel_Pair_PhaseOnly")]
        [TestCase(20, 40, 90.0, 0.5, 0.0, TestName = "Bevel_Pair_DriverRotation")]
        [TestCase(21, 40, 75.0, -1.3, 0.0, TestName = "Bevel_Pair_ShaftAngle75_DriverRotation")]
        [TestCase(20, 40, 90.0, 0.5, 0.2, TestName = "Bevel_Pair_PhaseOffset")]
        public void TestBevelPair_ExportedPartsHaveTheScenePose(int driverTeeth, int drivenTeeth, double shaftAngleDeg, double driverRotation, double driverOffset)
        {
            var driver = new BevelGearComponent("B1") { Teeth = driverTeeth, Module = 2.0, PressureAngleDeg = 20.0, FaceWidth = 10.0, ShaftAngleDeg = shaftAngleDeg };
            var driven = new BevelGearComponent("B2") { Teeth = drivenTeeth, Module = 2.0, PressureAngleDeg = 20.0, FaceWidth = 10.0 };
            var shaftA = new Shaft("S1") { RotationRad = driverRotation };
            var shaftB = new Shaft("S2");
            var driverMount = shaftA.Mount(driver, 3.0);
            var drivenMount = shaftB.Mount(driven, -2.0);
            driverMount.PhaseOffsetRad = driverOffset;
            var assembly = new GearAssembly();
            assembly.AddShaft(shaftA);
            assembly.AddShaft(shaftB);
            assembly.AddRelation(driver, driven, MeshType.Bevel);

            var scene = GearAssemblySceneBuilder.Build(assembly);

            var probes = new[]
            {
                new Vector3D(0.0, 0.0, 0.0), new Vector3D(10.0, 0.0, 0.0), new Vector3D(0.0, 10.0, 0.0), new Vector3D(0.0, 0.0, 10.0),
                new Vector3D(-7.0, 4.0, 3.0)
            };
            foreach (var pair in new[] { Tuple.Create(shaftA, driverMount), Tuple.Create(shaftB, drivenMount) })
            {
                var gear = (BevelGearComponent)pair.Item2.Component;

                // The scene moves the geometry of the editor model (the part frame at axial offset 0) by the shaft, the axial offset and the phase.
                var expected = scene.Find(gear).LocalToWorld.Compose(AlibreGearExporter.GetBevelPartFrame(gear, 0.0));
                var placement = AlibreGearExporter.GetPartPlacement(pair.Item1, pair.Item2);

                var worst = 0.0;
                foreach (var probe in probes)
                {
                    var a = placement.TransformPoint(probe);
                    var b = expected.TransformPoint(probe);
                    worst = Math.Max(worst, Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z)));
                }

                Console.WriteLine($"[DEBUG_LOG] bevel {gear.Teeth} teeth: phase {pair.Item2.PhaseRad:0.000000} rad, worst probe distance {worst:0.000000} mm");
                Assert.AreEqual(0.0, worst, Tol, $"The exported bevel gear {gear.Id} has the pose of the scene.");
            }
        }

        [Test]
        public void TestPlacement_AtPhaseZeroIsOnlyTheShaftPlacementAndTheAxialShift()
        {
            var shaft = new Shaft("S1");
            var gear = new InvoluteGearComponent("A") { Teeth = 20, Module = 2.0, FaceWidth = 10.0 };
            var mount = shaft.Mount(gear, 7.0);

            var placement = AlibreGearExporter.GetPartPlacement(shaft, mount);

            Assert.AreEqual(2.0, placement.Translation.Z, 1e-9, "The offset locates the mid-plane: 7 - half the face width.");
            var x = placement.TransformDirection(Vector3D.UnitX);
            Assert.AreEqual(1.0, x.X, 1e-12);
            Assert.AreEqual(0.0, x.Y, 1e-12);
        }

        [TestCase(30, 12, false, 0.0, 0.0, 0.0, TestName = "Cycloidal_EvenPinion_WheelDrives")]
        [TestCase(30, 12, true, 0.0, 0.0, 0.0, TestName = "Cycloidal_EvenWheel_PinionDrives")]
        [TestCase(12, 30, false, 0.0, 0.0, 0.0, TestName = "Cycloidal_EvenWheelDriven_Reversed")]
        [TestCase(30, 11, false, 0.0, 0.0, 0.0, TestName = "Cycloidal_OddPinion_WheelDrives")]
        [TestCase(31, 12, false, 0.0, 0.0, 0.0, TestName = "Cycloidal_OddWheel_WheelDrives")]
        [TestCase(30, 12, false, 0.0, 0.7, 0.0, TestName = "Cycloidal_DriverRotation")]
        [TestCase(30, 12, true, 0.0, -1.9, 0.0, TestName = "Cycloidal_DriverRotation_PinionDrives")]
        [TestCase(30, 12, false, 30.0, 0.0, 0.0, TestName = "Cycloidal_CentreLine30")]
        [TestCase(30, 12, false, -45.0, 0.37, 0.0, TestName = "Cycloidal_CentreLineMinus45_DriverRotation")]
        [TestCase(30, 12, true, 30.0, 0.0, 0.25, TestName = "Cycloidal_PhaseOffset")]
        public void TestCycloidalPair_ExportedPartsLieOnTheSceneOutlines(int wheelTeeth, int pinionTeeth, bool pinionDrives, double thetaDeg,
            double driverRotation, double driverOffset)
        {
            var assembly = new GearAssembly();
            var shaftA = new Shaft("S1");
            var shaftB = new Shaft("S2");
            var wheel = Gear("W", CycloidalGearRole.Wheel, wheelTeeth);
            var pinion = Gear("P", CycloidalGearRole.Pinion, pinionTeeth);
            var wheelMount = shaftA.Mount(wheel, 0.0);
            var pinionMount = shaftB.Mount(pinion, 2.0);

            // The first shaft of an assembly is the one that is placed at the origin: it must be the driver.
            if (pinionDrives)
            {
                assembly.AddShaft(shaftB);
                assembly.AddShaft(shaftA);
            }
            else
            {
                assembly.AddShaft(shaftA);
                assembly.AddShaft(shaftB);
            }

            var driverShaft = pinionDrives ? shaftB : shaftA;
            var driverMount = pinionDrives ? pinionMount : wheelMount;
            assembly.AddRelation(pinionDrives ? (GearComponent)pinion : wheel, pinionDrives ? (GearComponent)wheel : pinion, MeshType.Cycloidal, thetaDeg);
            driverShaft.RotationRad = driverRotation;
            driverMount.PhaseOffsetRad = driverOffset;

            var scene = GearAssemblySceneBuilder.Build(assembly);

            var wheelError = PlacementError(scene, shaftA, wheelMount, CycloidalProfile.Build(wheel).Ring);
            var pinionError = PlacementError(scene, shaftB, pinionMount, CycloidalProfile.Build(pinion).Ring);
            Console.WriteLine($"[DEBUG_LOG] wheel {wheelTeeth} pinion {pinionTeeth}: wheel phase {wheelMount.PhaseRad:0.000000} rad, pinion phase " +
                              $"{pinionMount.PhaseRad:0.000000} rad; outline error wheel {wheelError:0.000000} mm, pinion {pinionError:0.000000} mm");

            Assert.AreEqual(0.0, wheelError, Tol, "The exported wheel is in the pose of the scene.");
            Assert.AreEqual(0.0, pinionError, Tol, "The exported pinion is in the pose of the scene.");
        }
    }
}
