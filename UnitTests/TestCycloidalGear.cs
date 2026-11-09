using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Alibre;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Cycloidal gears: the wheel and pinion tooth outline, the pair as a mesh (centre distance, phase, ratio), the Alibre sketch plan,
    /// saving and the editor commands for the Cycloidal menu.
    /// </summary>
    [TestFixture]
    public class TestCycloidalGear
    {
        private const double Tol = 1e-6;

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

        private static CycloidalGearComponent Gear(string id, CycloidalGearRole role, int teeth, int mate = 0, double module = 2.0)
        {
            return new CycloidalGearComponent(id) { Role = role, Teeth = teeth, MateTeeth = mate, Module = module };
        }

        private static GearAssembly Pair(out CycloidalGearComponent wheel, out CycloidalGearComponent pinion, int wheelTeeth = 30, int pinionTeeth = 8, double module = 2.0)
        {
            var assembly = new GearAssembly();
            var shaftA = new Shaft("S1");
            wheel = Gear("W", CycloidalGearRole.Wheel, wheelTeeth, 0, module);
            shaftA.Mount(wheel, 0.0);
            var shaftB = new Shaft("S2");
            pinion = Gear("P", CycloidalGearRole.Pinion, pinionTeeth, 0, module);
            shaftB.Mount(pinion, 0.0);
            assembly.AddShaft(shaftA);
            assembly.AddShaft(shaftB);
            assembly.AddRelation(wheel, pinion, MeshType.Cycloidal);
            return assembly;
        }

        /// <summary>Wheel and pinion combinations that are drawn in the tests: few and many teeth, all the pinion leaf classes.</summary>
        private static IEnumerable<(CycloidalGearRole Role, int Teeth, int Mate)> Combinations()
        {
            foreach (var pinion in new[] { 6, 7, 8, 9, 10, 11, 12, 16 })
            {
                yield return (CycloidalGearRole.Pinion, pinion, 60);
                yield return (CycloidalGearRole.Wheel, 60, pinion);
                yield return (CycloidalGearRole.Wheel, 30, pinion);
            }

            yield return (CycloidalGearRole.Wheel, 120, 8);
            yield return (CycloidalGearRole.Pinion, 12, 30);
        }

        private static bool Inside(IReadOnlyList<GearPoint> polygon, double x, double y)
        {
            var inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if ((a.Y > y) != (b.Y > y) && x < (b.X - a.X) * (y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
            }

            return inside;
        }

        #region Dimensions and outline

        [Test]
        public void TestDimensions_WheelOfThirtyTeeth()
        {
            var wheel = Gear("W", CycloidalGearRole.Wheel, 30, 8);

            Assert.AreEqual(60.0, wheel.PitchDiameter, Tol);
            Assert.AreEqual(Math.PI / 60.0, wheel.HalfToothAngle, Tol, "A wheel tooth is half a pitch wide at the pitch circle.");
            Assert.AreEqual(Math.PI * 2.0 / 2.0, wheel.Dedendum, Tol, "The wheel dedendum is half the circular pitch.");
            Assert.Greater(wheel.AddendumFactor, 0.0);
            Assert.AreEqual(2.0 * 0.95 * wheel.AddendumFactor, wheel.Addendum, Tol);
            Assert.AreEqual(2.0 * 1.4 * wheel.AddendumFactor, wheel.AddendumRadius, Tol);
            Assert.Greater(wheel.OutsideDiameter, wheel.PitchDiameter);
            Assert.Less(wheel.RootDiameter, wheel.PitchDiameter);
            Assert.AreEqual(30 / 8.0, wheel.PairRatio, Tol);
            Assert.AreEqual("Cycloidal wheel", wheel.TypeName);
        }

        [Test]
        public void TestDimensions_PinionLeafTables()
        {
            Assert.AreEqual(0.855 * 2.0, CycloidalGearComponent.PinionAddendum(7, 2.0), Tol);
            Assert.AreEqual(0.67 * 2.0, CycloidalGearComponent.PinionAddendum(9, 2.0), Tol);
            Assert.AreEqual(0.525 * 2.0, CycloidalGearComponent.PinionAddendum(10, 2.0), Tol);
            Assert.AreEqual(0.625 * 2.0, CycloidalGearComponent.PinionAddendum(11, 2.0), Tol);
            Assert.AreEqual(1.05 * 2.0, CycloidalGearComponent.PinionAddendumRadius(6, 2.0), Tol);
            Assert.AreEqual(0.7 * 2.0, CycloidalGearComponent.PinionAddendumRadius(8, 2.0), Tol);

            var small = Gear("P", CycloidalGearRole.Pinion, 8, 30);
            var large = Gear("Q", CycloidalGearRole.Pinion, 12, 30);
            Assert.AreEqual(1.05 / 8.0, small.HalfToothAngle, Tol, "Up to ten leaves the tooth is 1.05 module wide.");
            Assert.AreEqual(1.25 / 12.0, large.HalfToothAngle, Tol, "Above ten leaves the tooth is 1.25 module wide.");
            Assert.AreEqual("Cycloidal pinion", small.TypeName);
        }

        [Test]
        public void TestDimensions_CustomClearanceChangesOnlyThePinionRoot()
        {
            var pinion = Gear("P", CycloidalGearRole.Pinion, 8, 30);
            var standard = pinion.Dedendum;
            pinion.CustomSlopEnabled = true;
            pinion.CustomSlop = 0.3;

            Assert.AreEqual(pinion.WheelAddendum + 0.3, pinion.Dedendum, Tol);
            Assert.AreNotEqual(standard, pinion.Dedendum);

            var wheel = Gear("W", CycloidalGearRole.Wheel, 30, 8);
            wheel.CustomSlopEnabled = true;
            wheel.CustomSlop = 5.0;
            Assert.AreEqual(Math.PI, wheel.Dedendum, Tol, "A wheel root does not depend on the clearance.");
        }

        [Test]
        public void TestOutline_IsASimpleAnticlockwiseRingInsideTheTipAndRootCircles()
        {
            foreach (var (role, teeth, mate) in Combinations())
            {
                var gear = Gear("G", role, teeth, mate);
                var outline = CycloidalProfile.Build(gear);
                var ring = outline.Ring;
                var label = $"{role} z={teeth} mate={mate}";

                Assert.Greater(SolidMeshBuilder.SignedArea(ring), 0.0, label + ": anticlockwise");
                Assert.IsTrue(ring.All(p => !double.IsNaN(p.X) && !double.IsNaN(p.Y)), label);

                var outer = gear.OutsideDiameter / 2.0;
                var root = gear.RootDiameter / 2.0;
                var radii = ring.Select(p => Math.Sqrt(p.X * p.X + p.Y * p.Y)).ToList();
                Assert.AreEqual(outer, radii.Max(), 1e-6, label + ": the tips reach the outside diameter");
                Assert.AreEqual(root, radii.Min(), 1e-6, label + ": the roots reach the root diameter");

                // Every point is seen from the axis, so the caps of the 3D solid can be fans from the axis.
                var total = 0.0;
                for (var i = 0; i < ring.Count; i++)
                {
                    var a = ring[i];
                    var b = ring[(i + 1) % ring.Count];
                    var turn = Math.Atan2(a.X * b.Y - a.Y * b.X, a.X * b.X + a.Y * b.Y);
                    Assert.GreaterOrEqual(turn, -1e-9, label + ": the angle about the axis never goes back at point " + i);
                    total += turn;
                }

                Assert.AreEqual(2.0 * Math.PI, total, 1e-6, label + ": the outline goes once round the axis");
            }
        }

        [Test]
        public void TestOutline_ToothTipIsOnThePositiveXAxis()
        {
            var gear = Gear("G", CycloidalGearRole.Wheel, 24, 8);
            var ring = CycloidalProfile.Build(gear).Ring;

            var onAxis = ring.Where(p => Math.Abs(p.Y) < 1e-9 && p.X > 0.0).OrderByDescending(p => p.X).First();

            Assert.AreEqual(gear.OutsideDiameter / 2.0, onAxis.X, 1e-9, "Tooth 0 has its tip on the +X axis.");
        }

        [Test]
        public void TestOutline_PitchPointsAreAHalfToothAngleFromTheTip()
        {
            var pinion = Gear("P", CycloidalGearRole.Pinion, 8, 30);
            var outline = CycloidalProfile.Build(pinion);
            var pitchRadius = pinion.PitchDiameter / 2.0;

            var onPitch = outline.Pieces.Where(p => p.Name == "Tooth0FlankRight").Select(p => p.Start).Single();

            Assert.AreEqual(pitchRadius, Math.Sqrt(onPitch.X * onPitch.X + onPitch.Y * onPitch.Y), 1e-9);
            Assert.AreEqual(pinion.HalfToothAngle, Math.Atan2(onPitch.Y, onPitch.X), 1e-9);
        }

        [Test]
        public void TestOutline_PiecesCloseIntoOneLoop()
        {
            var gear = Gear("G", CycloidalGearRole.Wheel, 30, 8);
            var pieces = CycloidalProfile.Build(gear).Pieces;

            Assert.AreEqual(5 * 30, pieces.Count, "Per tooth: two flanks, two tip arcs and a root arc.");
            for (var i = 0; i < pieces.Count; i++)
            {
                var ends = pieces.Select(p => p.Start).Concat(pieces.Select(p => p.End)).ToList();
                var end = pieces[i].End;
                var matches = ends.Count(p => Math.Abs(p.X - end.X) < 1e-9 && Math.Abs(p.Y - end.Y) < 1e-9);
                Assert.AreEqual(2, matches, "End of " + pieces[i].Name + " must meet exactly one other end.");
            }
        }

        [Test]
        public void TestOutline_ArcsRunAnticlockwiseAboutTheirCentres()
        {
            var gear = Gear("G", CycloidalGearRole.Pinion, 8, 30);

            foreach (var piece in CycloidalProfile.Build(gear).Pieces.Where(p => p.Kind == CycloidalPieceKind.Arc))
            {
                var a0 = Math.Atan2(piece.Start.Y - piece.Centre.Y, piece.Start.X - piece.Centre.X);
                var a1 = Math.Atan2(piece.End.Y - piece.Centre.Y, piece.End.X - piece.Centre.X);
                var sweep = a1 - a0;
                while (sweep < 0) sweep += 2.0 * Math.PI;

                Assert.Less(sweep, Math.PI, piece.Name + " is the shorter arc, drawn anticlockwise");
                var r0 = Math.Sqrt(Math.Pow(piece.Start.X - piece.Centre.X, 2) + Math.Pow(piece.Start.Y - piece.Centre.Y, 2));
                var r1 = Math.Sqrt(Math.Pow(piece.End.X - piece.Centre.X, 2) + Math.Pow(piece.End.Y - piece.Centre.Y, 2));
                Assert.AreEqual(r0, r1, 1e-9, piece.Name + " has equal start and end radii");
            }
        }

        [Test]
        public void TestValidate_RejectsImpossibleGears()
        {
            Assert.Throws<InvalidOperationException>(() => Gear("G", CycloidalGearRole.Wheel, 5).BuildGeometry());
            Assert.Throws<InvalidOperationException>(() => Gear("G", CycloidalGearRole.Wheel, 20, 4).Validate());
            Assert.Throws<InvalidOperationException>(() => Gear("G", CycloidalGearRole.Wheel, 20, 8, 0.0).Validate());
            Assert.Throws<InvalidOperationException>(() => new CycloidalGearComponent("G") { CentreHole = 500.0 }.Validate());
            Assert.Throws<InvalidOperationException>(() => new CycloidalGearComponent("G") { FaceWidth = 0.0 }.Validate());
        }

        [Test]
        public void TestGeometry_SolidHasTheOutlineAndTheFaceWidth()
        {
            var gear = Gear("G", CycloidalGearRole.Wheel, 30, 8);
            gear.FaceWidth = 6.0;

            var geometry = gear.BuildGeometry();

            Assert.AreEqual(1, geometry.Polylines.Count);
            Assert.AreEqual(geometry.Polylines[0][0].X, geometry.Polylines[0][geometry.Polylines[0].Count - 1].X, 1e-12, "The outline is closed.");
            Assert.AreEqual(3.0, geometry.Mesh.Vertices.Max(v => v.Z), Tol);
            Assert.AreEqual(-3.0, geometry.Mesh.Vertices.Min(v => v.Z), Tol);

            var area = SolidMeshBuilder.SignedArea(CycloidalProfile.Build(gear).Ring);
            var volume = 0.0;
            var m = geometry.Mesh;
            for (var i = 0; i + 2 < m.TriangleIndices.Count; i += 3)
            {
                var a = m.Vertices[m.TriangleIndices[i]];
                var b = m.Vertices[m.TriangleIndices[i + 1]];
                var c = m.Vertices[m.TriangleIndices[i + 2]];
                volume += (a.X * (b.Y * c.Z - b.Z * c.Y) - a.Y * (b.X * c.Z - b.Z * c.X) + a.Z * (b.X * c.Y - b.Y * c.X)) / 6.0;
            }

            Assert.AreEqual(area * 6.0, volume, area * 6.0 * 1e-6, "The mesh is closed, faces point outwards and its volume is area times width.");
        }

        [Test]
        public void TestGeometry_IsCachedAndRebuiltWhenAParameterChanges()
        {
            var gear = Gear("G", CycloidalGearRole.Wheel, 30, 8);
            var first = gear.BuildGeometry();

            Assert.AreSame(first, gear.BuildGeometry());

            gear.Teeth = 32;
            Assert.AreNotSame(first, gear.BuildGeometry());
        }

        #endregion

        #region Pair as a mesh

        [Test]
        public void TestMesh_CentreDistanceIsTheSumOfThePitchRadii()
        {
            var assembly = Pair(out var wheel, out var pinion, 30, 8, 2.5);

            assembly.Solve();

            var relation = assembly.Relations.Single();
            Assert.AreEqual((30 * 2.5 + 8 * 2.5) / 2.0, relation.Result.CentreDistance, 1e-6);
            Assert.AreEqual(relation.Result.CentreDistance, assembly.Shafts[1].Placement.Origin.X, 1e-6);
            Assert.AreEqual(8 / 30.0, relation.Result.GearRatio, 1e-6);
        }

        [Test]
        public void TestMesh_SolverTellsEachGearTheTeethOfTheOther()
        {
            var assembly = Pair(out var wheel, out var pinion, 48, 12);

            assembly.Solve();

            Assert.AreEqual(12, wheel.MateTeeth);
            Assert.AreEqual(48, pinion.MateTeeth);
            Assert.AreEqual(12, wheel.PinionTeeth);
            Assert.AreEqual(48, pinion.WheelTeeth);
        }

        [Test]
        public void TestMesh_ChangingTheTeethChangesTheOtherGearsProfile()
        {
            var assembly = Pair(out var wheel, out var pinion, 30, 8);
            assembly.Solve();
            var before = pinion.Dedendum;

            wheel.Teeth = 60;
            assembly.Solve();

            Assert.AreEqual(60, pinion.MateTeeth);
            Assert.AreNotEqual(before, pinion.Dedendum, "The pinion root depends on the wheel addendum, which depends on both tooth counts.");
        }

        [Test]
        public void TestMesh_TwoWheelsOrTwoPinionsCannotMesh()
        {
            var assembly = new GearAssembly();
            var shaftA = new Shaft("S1");
            var a = Gear("A", CycloidalGearRole.Wheel, 30);
            shaftA.Mount(a, 0.0);
            var shaftB = new Shaft("S2");
            var b = Gear("B", CycloidalGearRole.Wheel, 20);
            shaftB.Mount(b, 0.0);
            assembly.AddShaft(shaftA);
            assembly.AddShaft(shaftB);
            assembly.AddRelation(a, b, MeshType.Cycloidal);

            var ex = Assert.Throws<InvalidOperationException>(() => assembly.Solve());

            StringAssert.Contains("wheel and a pinion", ex.Message);
        }

        [Test]
        public void TestMesh_DifferentModulesCannotMesh()
        {
            var assembly = Pair(out _, out var pinion, 30, 8);
            pinion.Module = 3.0;

            var ex = Assert.Throws<InvalidOperationException>(() => assembly.Solve());

            StringAssert.Contains("modules differ", ex.Message);
        }

        [Test]
        public void TestMesh_InvoluteGearCannotMeshWithACycloidalGear()
        {
            var assembly = new GearAssembly();
            var shaftA = new Shaft("S1");
            var a = Gear("A", CycloidalGearRole.Wheel, 30);
            shaftA.Mount(a, 0.0);
            var shaftB = new Shaft("S2");
            var b = new InvoluteGearComponent("B") { Teeth = 20 };
            shaftB.Mount(b, 0.0);
            assembly.AddShaft(shaftA);
            assembly.AddShaft(shaftB);
            assembly.AddRelation(a, b, MeshType.Cycloidal);

            Assert.Throws<InvalidOperationException>(() => assembly.Solve());
        }

        [Test]
        public void TestMesh_TeethInterleaveAtEveryAngleWithoutOverlap()
        {
            foreach (var (wheelTeeth, pinionTeeth) in new[] { (30, 8), (48, 12), (60, 10), (40, 7) })
            {
                var assembly = Pair(out var wheel, out var pinion, wheelTeeth, pinionTeeth);
                var worst = 0.0;

                for (var step = 0; step < 24; step++)
                {
                    assembly.Shafts[0].RotationRad = step * 2.0 * Math.PI / 24 / wheelTeeth * 3.7;
                    var scene = GearAssemblySceneBuilder.Build(assembly);
                    var wheelOutline = scene.Items.First(i => ReferenceEquals(i.Component, wheel)).Polylines[0];
                    var pinionOutline = scene.Items.First(i => ReferenceEquals(i.Component, pinion)).Polylines[0];

                    foreach (var p in pinionOutline)
                    {
                        if (!Inside(wheelOutline, p.X, p.Y)) continue;
                        var depth = wheelOutline.Min(q => Math.Sqrt((q.X - p.X) * (q.X - p.X) + (q.Y - p.Y) * (q.Y - p.Y)));
                        worst = Math.Max(worst, depth);
                    }
                }

                // Cycloidal profiles from the clock tables are approximations of the true conjugate profile: allow a very small interference.
                Assert.Less(worst, 0.12 * wheel.Module, $"z={wheelTeeth}/{pinionTeeth}: the teeth must not run into each other.");
            }
        }

        [Test]
        public void TestMesh_AtPhaseZeroAToothOfTheDriverFacesAGapOfTheDriven()
        {
            foreach (var (wheelTeeth, pinionTeeth) in new[] { (30, 8), (31, 9), (45, 12) })
            {
                var assembly = Pair(out var wheel, out var pinion, wheelTeeth, pinionTeeth);
                var scene = GearAssemblySceneBuilder.Build(assembly);
                var pinionOutline = scene.Items.First(i => ReferenceEquals(i.Component, pinion)).Polylines[0];
                var centre = assembly.Shafts[1].Placement.Origin;

                var tipOfWheel = wheel.OutsideDiameter / 2.0;
                Assert.IsFalse(Inside(pinionOutline, tipOfWheel - 0.2, 0.0), "The wheel tip lies in a gap of the pinion.");
                Assert.AreEqual(wheel.PitchDiameter / 2.0 + pinion.PitchDiameter / 2.0, centre.X, 1e-6);
            }
        }

        [Test]
        public void TestMesh_TurnsTheDrivenGearTheOtherWayAtTheRatioOfTheTeeth()
        {
            var assembly = Pair(out var wheel, out var pinion, 30, 8);

            var report = GearTrainAnalyzer.Analyze(assembly, assembly.Shafts[0], 1000.0, assembly.Shafts[1]);

            Assert.AreEqual(-30.0 / 8.0 * 1000.0, report.Shafts.Single(s => s.ShaftId == "S2").SpeedRpm.Value, 1e-3);
            Assert.AreEqual(-30.0 / 8.0, report.Stages.Single().SpeedRatio, 1e-6);
        }

        [Test]
        public void TestMesh_PinionTurnsAtTheRatioInTheSceneToo()
        {
            var assembly = Pair(out _, out _, 30, 8);

            assembly.Shafts[0].RotationRad = 8.0 * Math.PI / 180.0;
            assembly.Solve();

            // The driven shaft is turned by the solver from the driver's rotation: minus z1 / z2 times the angle, plus the base phase.
            var expected = Math.PI + Math.PI / 8.0 - 8.0 * Math.PI / 180.0 * 30.0 / 8.0;
            Assert.AreEqual(expected, assembly.Shafts[1].RotationRad, 1e-6);
        }

        [Test]
        public void TestMesh_BranchingCentreLineAngleMovesTheDrivenShaft()
        {
            var assembly = Pair(out _, out _, 30, 8);
            assembly.Relations.Single().CentreLineAngleDeg = 90.0;

            assembly.Solve();

            var distance = (30 * 2.0 + 8 * 2.0) / 2.0;
            Assert.AreEqual(0.0, assembly.Shafts[1].Placement.Origin.X, 1e-6);
            Assert.AreEqual(distance, assembly.Shafts[1].Placement.Origin.Y, 1e-6);
        }

        #endregion

        #region Alibre export plan

        [Test]
        public void TestSketch_IsClosedForEveryCombination()
        {
            foreach (var (role, teeth, mate) in Combinations())
            {
                var plan = CycloidalSketchBuilder.Build(Gear("G", role, teeth, mate));

                Assert.IsTrue(plan.IsClosed(), $"{role} z={teeth} mate={mate}");
                Assert.AreEqual(5 * teeth, plan.Figures.Count);
            }
        }

        [Test]
        public void TestSketch_HasLinesAndArcsAndNoSplines()
        {
            var plan = CycloidalSketchBuilder.Build(Gear("G", CycloidalGearRole.Wheel, 30, 8));

            Assert.AreEqual(2 * 30, plan.Figures.Count(f => f.Kind == ToothSketchFigureKind.Line));
            Assert.AreEqual(3 * 30, plan.Figures.Count(f => f.Kind == ToothSketchFigureKind.Arc));
            Assert.IsTrue(plan.Figures.All(f => f.Kind != ToothSketchFigureKind.Spline));
        }

        [Test]
        public void TestSketch_ArcsStartAndEndOnTheirCircle()
        {
            var gear = Gear("G", CycloidalGearRole.Pinion, 8, 30);
            var plan = CycloidalSketchBuilder.Build(gear);

            foreach (var arc in plan.Figures.Where(f => f.Kind == ToothSketchFigureKind.Arc))
            {
                var r0 = Math.Sqrt(Math.Pow(arc.StartX - arc.CentreX, 2) + Math.Pow(arc.StartY - arc.CentreY, 2));
                var r1 = Math.Sqrt(Math.Pow(arc.EndX - arc.CentreX, 2) + Math.Pow(arc.EndY - arc.CentreY, 2));
                Assert.AreEqual(r0, r1, 1e-6, arc.Name);
            }
        }

        [Test]
        public void TestSketch_ValuesAreInCentimetresWithPitchReferenceAndHole()
        {
            var gear = Gear("G", CycloidalGearRole.Wheel, 30, 8);
            gear.CentreHole = 6.0;
            gear.FaceWidth = 12.0;

            var plan = CycloidalSketchBuilder.Build(gear);

            Assert.AreEqual(1.2, plan.Width, 1e-9);
            Assert.AreEqual(0.3, plan.CentreHoleRadius, 1e-9);
            Assert.AreEqual(3.0, plan.ReferenceCircles.Single().Radius, 1e-9, "Pitch radius 30 mm = 3 cm.");
            Assert.AreEqual("CycloidalWheel_Z30_M2_B12", plan.PartName);
            Assert.AreEqual(plan.PartName, CycloidalSketchBuilder.PartName(gear));
            Assert.AreEqual(0.0, CycloidalSketchBuilder.Build(Gear("H", CycloidalGearRole.Wheel, 30, 8)).CentreHoleRadius);
        }

        [Test]
        public void TestSketch_PinionAndWheelHaveDifferentPartNames()
        {
            Assert.AreEqual("CycloidalPinion_Z8_M2_B10", CycloidalSketchBuilder.PartName(Gear("P", CycloidalGearRole.Pinion, 8, 30)));
        }

        [Test]
        public void TestExporter_CycloidalGearsAreExportableAndSitOnTheirMidPlane()
        {
            var assembly = Pair(out var wheel, out var pinion);
            var mount = assembly.FindMount(wheel, out var shaft);
            mount.AxialOffset = 7.5;

            Assert.IsTrue(AlibreGearExporter.IsExportable(wheel));
            Assert.IsTrue(AlibreGearExporter.IsExportable(pinion));
            var placement = AlibreGearExporter.GetPartPlacement(shaft, mount);
            Assert.AreEqual(7.5, placement.Translation.Z, 1e-9, "The part is extruded about the sketch plane, so the offset is the mid-plane.");
        }

        [Test]
        public void TestExporter_AssemblyExportHoldsBothGearsOfThePair()
        {
            var assembly = Pair(out var wheel, out var pinion);

            var parts = AlibreGearExporter.GetAssemblyParts(assembly);

            Assert.AreEqual(2, parts.Count);
            CollectionAssert.AreEquivalent(new GearComponent[] { wheel, pinion }, parts.Select(p => p.Key));
        }

        #endregion

        #region Saving

        [Test]
        public void TestSerializer_KeepsTheCycloidalPair()
        {
            var assembly = Pair(out var wheel, out var pinion, 36, 9, 3.0);
            wheel.CentreHole = 8.0;
            pinion.CustomSlopEnabled = true;
            pinion.CustomSlop = 0.25;
            assembly.Relations.Single().CentreLineAngleDeg = 30.0;
            assembly.Solve();

            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            var gears = loaded.Components.OfType<CycloidalGearComponent>().ToList();
            Assert.AreEqual(2, gears.Count);
            var loadedWheel = gears.Single(g => g.Id == "W");
            var loadedPinion = gears.Single(g => g.Id == "P");
            Assert.AreEqual(CycloidalGearRole.Wheel, loadedWheel.Role);
            Assert.AreEqual(CycloidalGearRole.Pinion, loadedPinion.Role);
            Assert.AreEqual(36, loadedWheel.Teeth);
            Assert.AreEqual(9, loadedPinion.Teeth);
            Assert.AreEqual(9, loadedWheel.MateTeeth);
            Assert.AreEqual(3.0, loadedWheel.Module, 1e-12);
            Assert.AreEqual(8.0, loadedWheel.CentreHole, 1e-12);
            Assert.IsTrue(loadedPinion.CustomSlopEnabled);
            Assert.AreEqual(0.25, loadedPinion.CustomSlop, 1e-12);
            Assert.AreEqual(MeshType.Cycloidal, loaded.Relations.Single().MeshType);
            Assert.AreEqual(30.0, loaded.Relations.Single().CentreLineAngleDeg, 1e-12);

            loaded.Solve();
            Assert.AreEqual(assembly.Shafts[1].Placement.Origin.X, loaded.Shafts[1].Placement.Origin.X, 1e-6);
        }

        #endregion

        #region Editor commands

        [Test]
        public void TestPresenter_NewCycloidalPairDrawsBothGears()
        {
            var view = new FakeView();
            var presenter = new GearTrainPresenter(view);

            var wheel = presenter.NewCycloidalPair();

            Assert.AreEqual(CycloidalGearRole.Wheel, wheel.Role);
            Assert.AreEqual(2, presenter.Assembly.Shafts.Count);
            Assert.AreEqual(2, presenter.Assembly.Components.Count());
            var relation = presenter.Assembly.Relations.Single();
            Assert.AreEqual(MeshType.Cycloidal, relation.MeshType);
            Assert.AreSame(wheel, relation.Driver);
            var pinion = (CycloidalGearComponent)relation.Driven;
            Assert.AreEqual(CycloidalGearRole.Pinion, pinion.Role);
            Assert.AreEqual(30, wheel.Teeth);
            Assert.AreEqual(8, pinion.Teeth);
            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(2, view.Scene.Items.Count(i => i.Component is CycloidalGearComponent), "Both gears are in the scene.");
            Assert.IsTrue(view.Scene.Items.All(i => i.Mesh.Vertices.Count > 0 && i.Polylines.Count > 0));
            Assert.AreSame(pinion, presenter.Selected);
            Assert.IsInstanceOf<CycloidalNodeProperties>(view.Properties);
        }

        [Test]
        public void TestPresenter_NewCycloidalPairHonoursTheTeeth()
        {
            var presenter = new GearTrainPresenter(new FakeView());

            var wheel = presenter.NewCycloidalPair(45, 10);

            Assert.AreEqual(45, wheel.Teeth);
            Assert.AreEqual(10, ((CycloidalGearComponent)presenter.Assembly.Relations.Single().Driven).Teeth);
        }

        [Test]
        public void TestPresenter_AddingGearsAlternatesWheelAndPinion()
        {
            var view = new FakeView();
            var presenter = new GearTrainPresenter(view);
            presenter.NewCycloidalPair();
            var pinion = (CycloidalGearComponent)presenter.Selected;

            var second = presenter.AddCycloidalMate(pinion);
            var third = presenter.AddCycloidalMate(second);

            Assert.AreEqual(CycloidalGearRole.Wheel, second.Role);
            Assert.AreEqual(CycloidalGearRole.Pinion, third.Role);
            Assert.AreEqual(4, presenter.Assembly.Components.Count());
            Assert.AreEqual(3, presenter.Assembly.Relations.Count);
            Assert.IsTrue(presenter.Assembly.Relations.All(r => r.MeshType == MeshType.Cycloidal));
            Assert.AreEqual(pinion.Module, second.Module, 1e-12);
            Assert.GreaterOrEqual(second.Teeth, pinion.Teeth * 3, "A wheel for a pinion is large.");
            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(4, view.Scene.Items.Count(i => i.Component is CycloidalGearComponent));
        }

        [Test]
        public void TestPresenter_AddingSeveralGearsToOneGearBranchesTheTrain()
        {
            var view = new FakeView();
            var presenter = new GearTrainPresenter(view);
            var wheel = presenter.NewCycloidalPair();

            presenter.AddCycloidalMate(wheel);
            presenter.AddCycloidalMate(wheel);

            Assert.AreEqual(3, presenter.Assembly.Relations.Count);
            Assert.AreEqual(3, presenter.Assembly.Relations.Select(r => r.CentreLineAngleDeg).Distinct().Count(), "Branches go in different directions.");
            Assert.IsFalse(view.IsError, view.Message);
        }

        [Test]
        public void TestPresenter_AddMateWithCycloidalMeshTypeAddsACycloidalGear()
        {
            var presenter = new GearTrainPresenter(new FakeView());
            var wheel = presenter.NewCycloidalPair();

            var mate = presenter.AddMate(wheel, MeshType.Cycloidal);

            Assert.IsInstanceOf<CycloidalGearComponent>(mate);
            Assert.AreEqual(3, presenter.Assembly.Components.Count());
        }

        [Test]
        public void TestPresenter_CycloidalMateNeedsACycloidalGear()
        {
            var presenter = new GearTrainPresenter(new FakeView());
            presenter.NewTrain();
            var spur = presenter.Assembly.Components.First();

            Assert.Throws<ArgumentException>(() => presenter.AddCycloidalMate(spur));
            Assert.Throws<ArgumentException>(() => presenter.AddMate(spur, MeshType.Cycloidal));
        }

        [Test]
        public void TestPresenter_AddCycloidalGearToShaftMakesACompoundGear()
        {
            var view = new FakeView();
            var presenter = new GearTrainPresenter(view);
            presenter.NewCycloidalPair();
            var shaft = presenter.Assembly.Shafts[1];

            var added = presenter.AddCycloidalGearToShaft(shaft);

            Assert.AreEqual(2, shaft.Mounts.Count);
            Assert.AreEqual(CycloidalGearRole.Wheel, added.Role, "The shaft carries a pinion, so the new gear is a wheel.");
            var first = shaft.Mounts[0];
            var second = shaft.Mounts[1];
            first.GetAxialExtent(out _, out var firstEnd);
            second.GetAxialExtent(out var secondStart, out _);
            Assert.Greater(secondStart, firstEnd, "The gears of a shaft do not overlap.");
            Assert.IsFalse(view.IsError, view.Message);
        }

        [Test]
        public void TestPresenter_CompoundPairOfWheelAndPinionDrivesASecondStage()
        {
            var view = new FakeView();
            var presenter = new GearTrainPresenter(view);
            presenter.NewCycloidalPair(40, 10);
            var shaft = presenter.Assembly.Shafts[1];
            var compoundWheel = presenter.AddCycloidalGearToShaft(shaft);
            var next = presenter.AddCycloidalMate(compoundWheel, 10);

            Assert.AreEqual(CycloidalGearRole.Pinion, next.Role);
            presenter.Refresh();
            Assert.IsFalse(view.IsError, view.Message);
            var inputSpeed = 1000.0;
            var output = presenter.Report.Shafts.Single(s => s.ShaftId == presenter.Assembly.Shafts.Last().Id).SpeedRpm.Value;
            Assert.AreEqual(inputSpeed * (-40.0 / 10.0) * (-compoundWheel.Teeth / 10.0), output, 1e-3);
        }

        [Test]
        public void TestPresenter_RemovingAGearTakesItsBranchWithIt()
        {
            var presenter = new GearTrainPresenter(new FakeView());
            presenter.NewCycloidalPair();
            var pinion = (CycloidalGearComponent)presenter.Selected;
            presenter.AddCycloidalMate(pinion);

            presenter.Select(pinion);
            presenter.RemoveSelected();

            Assert.AreEqual(1, presenter.Assembly.Components.Count());
        }

        [Test]
        public void TestPresenter_PropertiesEditTheGearAndShareTheModule()
        {
            var view = new FakeView();
            var presenter = new GearTrainPresenter(view);
            var wheel = presenter.NewCycloidalPair();
            var pinion = (CycloidalGearComponent)presenter.Assembly.Relations.Single().Driven;
            presenter.Select(wheel);
            var properties = (CycloidalNodeProperties)view.Properties;

            properties.Teeth = 42;
            properties.Module = 3.0;
            properties.CentreHole = 6.0;

            Assert.AreEqual(42, wheel.Teeth);
            Assert.AreEqual(3.0, pinion.Module, 1e-12, "A pair shares one module.");
            Assert.AreEqual(6.0, wheel.CentreHole, 1e-12);
            Assert.AreEqual(wheel.PitchDiameter, properties.PitchDiameter, 1e-12);
            Assert.AreEqual(CycloidalGearRole.Wheel, properties.Role);

            properties.Teeth = 1;
            Assert.AreEqual(CycloidalGearComponent.AbsoluteMinimumTeeth, wheel.Teeth);
        }

        [Test]
        public void TestPresenter_SaveAndLoadKeepsThePair()
        {
            var presenter = new GearTrainPresenter(new FakeView());
            presenter.NewCycloidalPair(36, 9);
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cycloidal_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                Assert.IsTrue(presenter.Save(path));
                var other = new GearTrainPresenter(new FakeView());

                Assert.IsTrue(other.Load(path));

                Assert.AreEqual(2, other.Assembly.Components.OfType<CycloidalGearComponent>().Count());
                Assert.AreEqual(MeshType.Cycloidal, other.Assembly.Relations.Single().MeshType);
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }

        [Test]
        public void TestParameterTable_ShowsTheCycloidalDimensions()
        {
            var gear = Gear("G", CycloidalGearRole.Wheel, 30, 8);

            var table = GearParameterTables.ForGear(gear);

            Assert.IsTrue(table.Rows.Any(r => r.ParameterName == "Addendum" && Math.Abs(r.Value - gear.Addendum) < 1e-9));
            Assert.IsTrue(table.Rows.Any(r => r.ParameterName == "Dedendum" && Math.Abs(r.Value - gear.Dedendum) < 1e-9));
            Assert.IsTrue(table.Rows.Any(r => r.ParameterName == "Teeth of Mating Gear" && Math.Abs(r.Value - 8) < 1e-9));
            StringAssert.Contains("Cycloidal wheel", table.Title);
        }

        #endregion
    }
}
