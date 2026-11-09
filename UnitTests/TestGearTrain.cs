using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Phase 2: multi-shaft gear trains with compound gears and idlers, the ratio and speed report, shaft meshes,
    /// and the gear train presenter.
    /// </summary>
    [TestFixture]
    public class TestGearTrain
    {
        private const double Tol = 1e-6;

        private static InvoluteGearComponent Gear(string id, int teeth, bool isInternal = false)
        {
            return new InvoluteGearComponent(id)
            {
                Teeth = teeth,
                Module = 2.0,
                PressureAngleDeg = 20.0,
                Clearance = 0.5,
                IsInternal = isInternal
            };
        }

        private static Shaft ShaftWith(string id, params (GearComponent Gear, double Offset)[] gears)
        {
            var shaft = new Shaft(id);
            foreach (var (gear, offset) in gears) shaft.Mount(gear, offset);
            return shaft;
        }

        /// <summary>A(20) on s1 drives B(40) on s2; C(20) is on s2 as well and drives D(30) on s3.</summary>
        private static GearAssembly CompoundTrain(out Shaft s1, out Shaft s2, out Shaft s3, out InvoluteGearComponent d)
        {
            var a = Gear("A", 20);
            var b = Gear("B", 40);
            var c = Gear("C", 20);
            d = Gear("D", 30);
            s1 = ShaftWith("s1", (a, 0.0));
            s2 = ShaftWith("s2", (b, 0.0), (c, 20.0));
            s3 = ShaftWith("s3", (d, 0.0));

            var assembly = new GearAssembly();
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            assembly.AddShaft(s3);
            assembly.AddRelation(a, b);
            assembly.AddRelation(c, d);
            return assembly;
        }

        [Test]
        public void TestCompoundTrain_PlacesShaftsInAxialPlaneOfEachMesh()
        {
            var assembly = CompoundTrain(out _, out var s2, out var s3, out _);
            assembly.Solve();

            Assert.AreEqual(60.0, s2.Placement.Origin.X, Tol, "A:B centre distance is 2 * (20 + 40) / 2.");
            Assert.AreEqual(0.0, s2.Placement.Origin.Z, Tol);
            Assert.AreEqual(110.0, s3.Placement.Origin.X, Tol, "C:D centre distance 50 follows the shaft 2 position.");
            Assert.AreEqual(20.0, s3.Placement.Origin.Z, Tol, "D sits at offset 0 but meshes with C at offset 20, so shaft 3 moves up by 20.");
        }

        [Test]
        public void TestCompoundTrain_ReportsSpeedsRatiosAndPitchLineVelocity()
        {
            var assembly = CompoundTrain(out _, out _, out _, out _);
            var report = GearTrainAnalyzer.Analyze(assembly, null, 1000.0);

            Assert.AreEqual(3, report.Shafts.Count);
            Assert.AreEqual(1000.0, report.Shafts[0].SpeedRpm.Value, Tol);
            Assert.AreEqual(-500.0, report.Shafts[1].SpeedRpm.Value, Tol, "The first mesh halves the speed and reverses it.");
            Assert.AreEqual(1000.0 / 3.0, report.Shafts[2].SpeedRpm.Value, Tol, "The second mesh turns the direction back.");
            Assert.AreEqual("Opposite to input", report.Shafts[1].Direction);
            Assert.AreEqual("Same as input", report.Shafts[2].Direction);

            Assert.AreEqual(3.0, report.OverallRatio.Value, Tol, "(40 / 20) * (30 / 20) = 3.");
            Assert.AreEqual("s3", report.OutputShaftId);

            Assert.AreEqual(2, report.Stages.Count);
            Assert.AreEqual(2.0, report.Stages[0].ToothRatio, Tol);
            Assert.AreEqual(-0.5, report.Stages[0].SpeedRatio, Tol);
            Assert.AreEqual(60.0, report.Stages[0].CentreDistance, Tol);

            var gearD = report.Gears.Single(g => g.GearId == "D");
            Assert.AreEqual(60.0, gearD.PitchDiameter, Tol);
            Assert.AreEqual(Math.PI * 60.0 * (1000.0 / 3.0) / 60000.0, gearD.PitchLineVelocity.Value, Tol);

            Assert.AreEqual("Compound", report.Gears.Single(g => g.GearId == "B").Role);
            Assert.AreEqual("Compound", report.Gears.Single(g => g.GearId == "C").Role);
            Assert.AreEqual("Input", report.Gears.Single(g => g.GearId == "A").Role);
            Assert.AreEqual("Driven", gearD.Role);
        }

        [Test]
        public void TestCompoundTrain_ShaftRotationsFollowTheInputAngleBySpeedRatio()
        {
            var assembly = CompoundTrain(out var s1, out var s2, out var s3, out _);
            assembly.Solve();
            var base2 = s2.RotationRad;
            var base3 = s3.RotationRad;

            s1.RotationRad = 0.4;
            assembly.Solve();

            Assert.AreEqual(0.4 * -0.5, s2.RotationRad - base2, 1e-9);
            Assert.AreEqual(0.4 * (-20.0 / 40.0) * (-20.0 / 30.0), s3.RotationRad - base3, 1e-9);
        }

        [Test]
        public void TestIdler_IsReportedAndDoesNotChangeTheOverallRatio()
        {
            var a = Gear("A", 20);
            var idler = Gear("I", 30);
            var c = Gear("C", 40);
            var assembly = new GearAssembly();
            assembly.AddShaft(ShaftWith("s1", (a, 0.0)));
            assembly.AddShaft(ShaftWith("s2", (idler, 0.0)));
            assembly.AddShaft(ShaftWith("s3", (c, 0.0)));
            assembly.AddRelation(a, idler);
            assembly.AddRelation(idler, c);

            var report = GearTrainAnalyzer.Analyze(assembly, null, 600.0);

            Assert.AreEqual("Idler", report.Gears.Single(g => g.GearId == "I").Role);
            Assert.AreEqual(-400.0, report.Shafts[1].SpeedRpm.Value, Tol);
            Assert.AreEqual(300.0, report.Shafts[2].SpeedRpm.Value, Tol);
            Assert.AreEqual(2.0, report.OverallRatio.Value, Tol, "An idler does not change the ratio: 40 / 20.");
            Assert.AreEqual("Same as input", report.Shafts[2].Direction, "An idler restores the direction of rotation.");
        }

        [Test]
        public void TestBranchingTrain_PlacesShaftsAlongTheirCentreLines()
        {
            var a = Gear("A", 20);
            var b = Gear("B", 30);
            var c = Gear("C", 30);
            var s1 = ShaftWith("s1", (a, 0.0));
            var s2 = ShaftWith("s2", (b, 0.0));
            var s3 = ShaftWith("s3", (c, 0.0));
            var assembly = new GearAssembly();
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            assembly.AddShaft(s3);
            assembly.AddRelation(a, b, MeshType.Parallel, 0.0);
            assembly.AddRelation(a, c, MeshType.Parallel, 90.0);
            assembly.Solve();

            Assert.AreEqual(50.0, s2.Placement.Origin.X, Tol);
            Assert.AreEqual(0.0, s2.Placement.Origin.Y, Tol);
            Assert.AreEqual(0.0, s3.Placement.Origin.X, Tol);
            Assert.AreEqual(50.0, s3.Placement.Origin.Y, Tol);

            var report = GearTrainAnalyzer.Analyze(assembly, null, 1000.0);
            Assert.AreEqual(-2000.0 / 3.0, report.Shafts[1].SpeedRpm.Value, Tol);
            Assert.AreEqual(-2000.0 / 3.0, report.Shafts[2].SpeedRpm.Value, Tol);
        }

        [Test]
        public void TestCentreLineAngle_TurnsTheWholePairRigidly()
        {
            GearAssembly Build(double angleDeg, double driverRotation, out InvoluteGearComponent driven)
            {
                var driver = Gear("A", 20);
                driven = Gear("B", 30);
                var s1 = ShaftWith("s1", (driver, 0.0));
                s1.RotationRad = driverRotation;
                var assembly = new GearAssembly();
                assembly.AddShaft(s1);
                assembly.AddShaft(ShaftWith("s2", (driven, 0.0)));
                assembly.AddRelation(driver, driven, MeshType.Parallel, angleDeg);
                return assembly;
            }

            var reference = Build(0.0, 0.3, out var drivenRef);
            var turned = Build(90.0, 0.3 + Math.PI / 2.0, out var drivenTurned);

            var refPolylines = GearAssemblySceneBuilder.Build(reference).Find(drivenRef).Polylines;
            var turnedPolylines = GearAssemblySceneBuilder.Build(turned).Find(drivenTurned).Polylines;

            Assert.AreEqual(refPolylines.Count, turnedPolylines.Count);
            for (var i = 0; i < refPolylines.Count; i++)
            {
                for (var j = 0; j < refPolylines[i].Count; j++)
                {
                    var p = refPolylines[i][j];
                    var q = turnedPolylines[i][j];
                    Assert.AreEqual(-p.Y, q.X, 3e-7, $"polyline {i} point {j} X");
                    Assert.AreEqual(p.X, q.Y, 3e-7, $"polyline {i} point {j} Y");
                }
            }
        }

        [Test]
        public void TestCompoundShaft_SecondDrivenGearIsClockedOntoTheShaft()
        {
            var a1 = Gear("A1", 20);
            var a2 = Gear("A2", 20);
            var b1 = Gear("B1", 40);
            var b2 = Gear("B2", 40);
            var s1 = ShaftWith("s1", (a1, 0.0), (a2, 20.0));
            var s2 = ShaftWith("s2", (b1, 0.0), (b2, 20.0));
            var assembly = new GearAssembly();
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            assembly.AddRelation(a1, b1);
            assembly.AddRelation(a2, b2);

            // Turn the second driver gear on its shaft by 0.1 rad: its partner must follow by -0.05 rad.
            s1.Mounts[1].PhaseRad = 0.1;
            assembly.Solve();

            Assert.AreEqual(s2.Mounts[0].PhaseRad - 0.05, s2.Mounts[1].PhaseRad, 1e-9);
            Assert.AreEqual(-500.0, GearTrainAnalyzer.Analyze(assembly, null, 1000.0).Shafts[1].SpeedRpm.Value, Tol);
        }

        [Test]
        public void TestLockedTrain_IsRejectedByTheAnalyzer()
        {
            var a1 = Gear("A1", 20);
            var a2 = Gear("A2", 30);
            var b1 = Gear("B1", 30);
            var b2 = Gear("B2", 20);
            var s1 = ShaftWith("s1", (a1, 0.0), (a2, 20.0));
            var s2 = ShaftWith("s2", (b1, 0.0), (b2, 20.0));
            var assembly = new GearAssembly();
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            assembly.AddRelation(a1, b1); // centre distance 50, ratio 1.5
            assembly.AddRelation(a2, b2); // centre distance 50, ratio 0.667: the shafts cannot turn

            Assert.DoesNotThrow(assembly.Solve, "The geometry is consistent; only the kinematics lock.");
            var ex = Assert.Throws<InvalidOperationException>(() => GearTrainAnalyzer.Analyze(assembly, null, 1000.0));
            StringAssert.Contains("locks up", ex.Message);
        }

        [Test]
        public void TestSolve_RejectsCompoundGearsThatDemandDifferentCentreDistances()
        {
            var a1 = Gear("A1", 20);
            var a2 = Gear("A2", 20);
            var b1 = Gear("B1", 40);
            var b2 = Gear("B2", 30);
            var assembly = new GearAssembly();
            assembly.AddShaft(ShaftWith("s1", (a1, 0.0), (a2, 20.0)));
            assembly.AddShaft(ShaftWith("s2", (b1, 0.0), (b2, 20.0)));
            assembly.AddRelation(a1, b1);
            assembly.AddRelation(a2, b2);

            var ex = Assert.Throws<InvalidOperationException>(assembly.Solve);
            StringAssert.Contains("over-constrained", ex.Message);
        }

        [Test]
        public void TestAnalyzer_UnconnectedShaftIsReportedWithAWarning()
        {
            var assembly = new GearAssembly();
            assembly.AddShaft(ShaftWith("s1", (Gear("A", 20), 0.0)));
            assembly.AddShaft(ShaftWith("s2", (Gear("B", 20), 0.0)));

            var report = GearTrainAnalyzer.Analyze(assembly, null, 1000.0);

            Assert.IsNull(report.Shafts[1].SpeedRpm);
            Assert.AreEqual("Not connected", report.Shafts[1].Direction);
            Assert.IsNull(report.OverallRatio);
            Assert.IsTrue(report.Warnings.Any(w => w.Contains("not connected")), string.Join("; ", report.Warnings));
        }

        [Test]
        public void TestAnalyzer_SingleShaftHasNoOverallRatio()
        {
            var assembly = new GearAssembly();
            assembly.AddShaft(ShaftWith("s1", (Gear("A", 20), 0.0)));

            var report = GearTrainAnalyzer.Analyze(assembly, null, 250.0);

            Assert.IsNull(report.OverallRatio);
            Assert.IsNull(report.OutputShaftId);
            Assert.AreEqual("Input", report.Gears[0].Role);
        }

        [Test]
        public void TestAnalyzer_InternalGearKeepsTheDirectionOfRotation()
        {
            var driver = Gear("A", 20);
            var ring = Gear("R", 40, isInternal: true);
            var relation = new GearMeshRelation(driver, ring, MeshType.Parallel);

            Assert.AreEqual(0.5, GearTrainAnalyzer.SpeedFactor(relation), 1e-12, "External driving internal turns the same way at z1 / z2.");
        }

        [Test]
        public void TestAnalyzer_InputShaftCanBeAnyShaft()
        {
            var assembly = CompoundTrain(out _, out var s2, out _, out _);

            var report = GearTrainAnalyzer.Analyze(assembly, s2, 500.0, assembly.Shafts[0]);

            Assert.AreEqual(-1000.0, report.Shafts[0].SpeedRpm.Value, Tol, "Driving the middle shaft backwards turns the first shaft at twice the speed.");
            Assert.AreEqual(-500.0 / 1.5, report.Shafts[2].SpeedRpm.Value, Tol, "Shaft 3 turns at the shaft 2 speed times (-20 / 30).");
        }

        [Test]
        public void TestReport_TextContainsTheKeyFigures()
        {
            var assembly = CompoundTrain(out _, out _, out _, out _);
            var text = GearTrainAnalyzer.Analyze(assembly, null, 1000.0).ToText();

            StringAssert.Contains("Overall ratio", text);
            StringAssert.Contains("3.0000000", text);
            StringAssert.Contains("-500.0000000", text);
            StringAssert.Contains("[Compound]", text);
        }

        [Test]
        public void TestValidate_ReportsAxiallyOverlappingGearsOnOneShaft()
        {
            var assembly = new GearAssembly();
            var shaft = ShaftWith("s1", (Gear("A", 20), 0.0), (Gear("B", 20), 10.0));
            assembly.AddShaft(shaft);

            var problems = assembly.Validate();
            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("overlap", problems[0]);

            shaft.Mounts[1].AxialOffset = 20.0;
            Assert.AreEqual(0, assembly.Validate().Count);
        }

        [Test]
        public void TestRemoveGear_DropsItsRelations()
        {
            var assembly = CompoundTrain(out _, out var s2, out _, out _);
            var c = s2.Mounts[1].Component;

            Assert.IsTrue(assembly.RemoveGear(c));

            Assert.AreEqual(1, assembly.Relations.Count);
            Assert.AreEqual(1, s2.Mounts.Count);
            Assert.IsFalse(assembly.RemoveGear(c), "A removed gear cannot be removed twice.");
        }

        [Test]
        public void TestRemoveShaft_DropsGearsAndRelations()
        {
            var assembly = CompoundTrain(out _, out var s2, out _, out _);

            Assert.IsTrue(assembly.RemoveShaft(s2));

            Assert.AreEqual(2, assembly.Shafts.Count);
            Assert.AreEqual(0, assembly.Relations.Count);
        }

        [Test]
        public void TestScene_ContainsOneShaftMeshPerShaftAndKeepsGearOutputsSeparate()
        {
            var assembly = CompoundTrain(out var s1, out _, out _, out _);
            var scene = GearAssemblySceneBuilder.Build(assembly);

            Assert.AreEqual(3, scene.ShaftItems.Count);
            Assert.AreEqual(4, scene.Items.Count, "Shaft meshes are not mixed into the gear items.");

            var mesh = scene.ShaftItems[0].Mesh;
            var shaft = scene.ShaftItems[0].Shaft;
            Assert.AreSame(s1, shaft);
            Assert.AreEqual(mesh.Vertices.Count, mesh.Normals.Count);
            Assert.AreEqual(5.0, mesh.Vertices.Max(v => Math.Sqrt(v.X * v.X + v.Y * v.Y)), 1e-6, "Shaft radius is half the 10 mm diameter.");
            Assert.AreEqual(-17.5, mesh.Vertices.Min(v => v.Z), 1e-6, "Gear face -7.5 mm minus the 10 mm end margin.");
            Assert.AreEqual(17.5, mesh.Vertices.Max(v => v.Z), 1e-6);
            Assert.IsTrue(mesh.TriangleIndices.All(i => i >= 0 && i < mesh.Vertices.Count));

            Assert.AreEqual(scene.Items.Sum(i => i.Mesh.Vertices.Count), scene.CombinedMesh().Vertices.Count);
        }

        [Test]
        public void TestShaft_AutomaticSpanCoversAllGearsPlusMargin()
        {
            var shaft = ShaftWith("s", (Gear("A", 20), 0.0), (Gear("B", 20), 20.0));
            shaft.GetAxialSpan(out var start, out var end);

            Assert.AreEqual(-17.5, start, 1e-9);
            Assert.AreEqual(37.5, end, 1e-9);

            shaft.Length = 100.0;
            shaft.GetAxialSpan(out start, out end);
            Assert.AreEqual(100.0, end - start, 1e-9);
        }

        [Test]
        public void TestMount_PhaseRadIsShaftRotationPlusOffset()
        {
            var shaft = ShaftWith("s", (Gear("A", 20), 0.0));
            shaft.RotationRad = 0.5;
            var mount = shaft.Mounts[0];

            mount.PhaseOffsetRad = 0.25;
            Assert.AreEqual(0.75, mount.PhaseRad, 1e-12);

            mount.PhaseRad = 1.0;
            Assert.AreEqual(0.5, mount.PhaseOffsetRad, 1e-12);
        }

        #region Presenter

        private sealed class FakeTrainView : IGearTrainView
        {
            public double InputSpeedRpm { get; set; } = 1000.0;
            public TrainTreeNode Tree;
            public object Properties;
            public AssemblyScene Scene;
            public object SceneSelected;
            public List<object> SceneSelectedItems = new List<object>();
            public ISet<GearComponent> SceneHidden;
            public GearTrainReport Report;
            public string Message;
            public bool IsError;

            public void ShowTree(TrainTreeNode root, object selectedTag) => Tree = root;
            public void ShowSelection(object selectedTag) { }
            public void ShowProperties(object properties) => Properties = properties;

            public void ShowScene(AssemblyScene scene, IReadOnlyCollection<object> selectedItems, ISet<GearComponent> hiddenGears)
            {
                Scene = scene;
                SceneSelectedItems = new List<object>(selectedItems);
                SceneSelected = SceneSelectedItems.Count > 0 ? SceneSelectedItems[SceneSelectedItems.Count - 1] : null;
                SceneHidden = new HashSet<GearComponent>(hiddenGears);
            }

            public void ShowReport(GearTrainReport report) => Report = report;

            public void ShowMessage(string message, bool isError)
            {
                Message = message;
                IsError = isError;
            }
        }

        private static GearTrainPresenter NewPresenter(out FakeTrainView view)
        {
            view = new FakeTrainView();
            var presenter = new GearTrainPresenter(view);
            presenter.NewTrain();
            return presenter;
        }

        [Test]
        public void TestPresenter_NewTrain_ShowsOneShaftWithOneGear()
        {
            var presenter = NewPresenter(out var view);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(1, view.Tree.Children.Count);
            Assert.AreEqual(1, view.Tree.Children[0].Children.Count);
            Assert.AreEqual(TrainNodeKind.Gear, view.Tree.Children[0].Children[0].Kind);
            Assert.AreEqual(1, view.Scene.Items.Count);
            Assert.AreSame(presenter.Selected, view.SceneSelected);
            Assert.IsInstanceOf<GearNodeProperties>(view.Properties);
        }

        [Test]
        public void TestPresenter_AddMeshedGear_BuildsAMeshedPairAndReport()
        {
            var presenter = NewPresenter(out var view);
            var first = (InvoluteGearComponent)presenter.Selected;

            var second = presenter.AddMeshedGear(first, 40);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(2, presenter.Assembly.Shafts.Count);
            Assert.AreEqual(1, presenter.Assembly.Relations.Count);
            Assert.AreSame(second, presenter.Selected);
            Assert.AreEqual(60.0, presenter.Assembly.Shafts[1].Placement.Origin.X, Tol);
            Assert.AreEqual(-2.0, view.Report.OverallRatio.Value, Tol, "One external mesh doubles the ratio and reverses the direction.");
            Assert.AreEqual(2, view.Scene.Items.Count);
            Assert.AreEqual(2, view.Scene.ShaftItems.Count);
        }

        [Test]
        public void TestPresenter_AddMeshedGear_SpacesBranchesAtSixtyDegrees()
        {
            var presenter = NewPresenter(out _);
            var first = (InvoluteGearComponent)presenter.Selected;

            presenter.AddMeshedGear(first, 30);
            presenter.AddMeshedGear(first, 30);

            Assert.AreEqual(0.0, presenter.Assembly.Relations[0].CentreLineAngleDeg, 1e-12);
            Assert.AreEqual(60.0, presenter.Assembly.Relations[1].CentreLineAngleDeg, 1e-12);
        }

        [Test]
        public void TestPresenter_AddGearToShaft_CreatesACompoundGearBesideTheFirst()
        {
            var presenter = NewPresenter(out var view);
            var shaft = presenter.Assembly.Shafts[0];

            var second = presenter.AddGearToShaft(shaft, 30);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(2, shaft.Mounts.Count);
            Assert.AreEqual(17.0, shaft.Mounts[1].AxialOffset, 1e-9, "Face 7.5 + gap 2 + half of the new face 7.5.");
            Assert.AreEqual(0, presenter.Assembly.Validate().Count, "The compound gears must not overlap.");
            Assert.AreEqual(30, second.Teeth);
        }

        [Test]
        public void TestPresenter_MeshedGearFromACompoundGear_SitsInThePlaneOfThatGear()
        {
            var presenter = NewPresenter(out var view);
            var compound = presenter.AddGearToShaft(presenter.Assembly.Shafts[0], 20);

            var driven = presenter.AddMeshedGear(compound, 30);
            presenter.Assembly.FindMount(driven, out var drivenShaft);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(17.0, drivenShaft.Mounts[0].AxialOffset, 1e-9);
            Assert.AreEqual(0.0, drivenShaft.Placement.Origin.Z, Tol, "Gear and driver share the same offset, so the shafts share the same origin height.");
        }

        [Test]
        public void TestPresenter_BuildsAnIdlerChain()
        {
            var presenter = NewPresenter(out var view);
            var a = (InvoluteGearComponent)presenter.Selected;
            var idler = presenter.AddMeshedGear(a, 30, 0.0);
            presenter.AddMeshedGear(idler, 40, 0.0);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual("Idler", view.Report.Gears.Single(g => g.GearId == idler.Id).Role);
            Assert.AreEqual(2.0, view.Report.OverallRatio.Value, Tol);
        }

        [Test]
        public void TestPresenter_RemoveDrivenGear_RemovesItsShaftAndBranches()
        {
            var presenter = NewPresenter(out var view);
            var a = (InvoluteGearComponent)presenter.Selected;
            var b = presenter.AddMeshedGear(a, 30, 0.0);
            presenter.AddMeshedGear(b, 40, 0.0);
            Assert.AreEqual(3, presenter.Assembly.Shafts.Count);

            presenter.Select(b);
            presenter.RemoveSelected();

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(1, presenter.Assembly.Shafts.Count);
            Assert.AreEqual(0, presenter.Assembly.Relations.Count);
            Assert.IsNull(presenter.Selected);
        }

        [Test]
        public void TestPresenter_RemoveDriverGear_RemovesTheGearsItDrives()
        {
            var presenter = NewPresenter(out var view);
            var shaft = presenter.Assembly.Shafts[0];
            var a = (InvoluteGearComponent)presenter.Selected;
            presenter.AddMeshedGear(a, 30, 0.0);
            var compound = presenter.AddGearToShaft(shaft, 20);
            presenter.AddMeshedGear(compound, 30, 90.0);
            Assert.AreEqual(3, presenter.Assembly.Shafts.Count);

            presenter.Select(a);
            presenter.RemoveSelected();

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(2, presenter.Assembly.Shafts.Count, "Only the branch driven by the compound gear is left.");
            Assert.AreEqual(1, shaft.Mounts.Count);
            Assert.AreEqual(1, presenter.Assembly.Relations.Count);
        }

        [Test]
        public void TestPresenter_RemoveLastShaft_LeavesAnEmptyTrain()
        {
            var presenter = NewPresenter(out var view);
            presenter.Select(presenter.Assembly.Shafts[0]);

            presenter.RemoveSelected();

            Assert.AreEqual(0, presenter.Assembly.Shafts.Count);
            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(0, view.Scene.Items.Count);
            Assert.IsNull(view.Report);
        }

        [Test]
        public void TestPresenter_SetGearVisible_UpdatesTheSceneHiddenSet()
        {
            var presenter = NewPresenter(out var view);
            var gear = (GearComponent)presenter.Selected;

            presenter.SetGearVisible(gear, false);
            Assert.IsTrue(view.SceneHidden.Contains(gear));
            Assert.IsFalse(presenter.Scene == null);

            presenter.SetGearVisible(gear, true);
            Assert.IsFalse(view.SceneHidden.Contains(gear));
        }

        [Test]
        public void TestPresenter_DifferingModules_ShowAnErrorAndKeepThePreviousScene()
        {
            var presenter = NewPresenter(out var view);
            var a = (InvoluteGearComponent)presenter.Selected;
            var b = presenter.AddMeshedGear(a, 30, 0.0);
            var goodScene = presenter.Scene;

            b.Module = 3.0;
            presenter.Refresh();

            Assert.IsTrue(view.IsError);
            StringAssert.Contains("modules differ", view.Message);
            Assert.AreSame(goodScene, presenter.Scene, "The last good scene stays on screen.");
        }

        [Test]
        public void TestPresenter_ModuleChangedInThePropertyGrid_PropagatesToMeshingGears()
        {
            var presenter = NewPresenter(out var view);
            var a = (InvoluteGearComponent)presenter.Selected;
            var b = presenter.AddMeshedGear(a, 30, 0.0);
            presenter.Select(a);
            var properties = (GearNodeProperties)view.Properties;

            properties.Module = 3.0;
            properties.PressureAngle = 25.0;
            presenter.Refresh();

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(3.0, b.Module, 1e-12);
            Assert.AreEqual(25.0, b.PressureAngleDeg, 1e-12);
            Assert.AreEqual(75.0, presenter.Assembly.Shafts[1].Placement.Origin.X, Tol, "Centre distance 3 * (20 + 30) / 2.");
        }

        [Test]
        public void TestPresenter_SetInputAngle_TurnsTheDrivenShaft()
        {
            var presenter = NewPresenter(out var view);
            var a = (InvoluteGearComponent)presenter.Selected;
            presenter.AddMeshedGear(a, 40, 0.0);
            var before = presenter.Assembly.Shafts[1].RotationRad;

            presenter.SetInputAngle(90.0);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(before - Math.PI / 4.0, presenter.Assembly.Shafts[1].RotationRad, 1e-9, "A 90 degree turn of the 20 tooth driver turns the 40 tooth gear by -45 degrees.");
        }

        [Test]
        public void TestPresenter_ShaftProperties_OnlyTheInputShaftAcceptsAnInputAngle()
        {
            var presenter = NewPresenter(out var view);
            var a = (InvoluteGearComponent)presenter.Selected;
            presenter.AddMeshedGear(a, 40, 0.0);

            presenter.Select(presenter.Assembly.Shafts[0]);
            var input = (ShaftNodeProperties)view.Properties;
            input.RotationDeg = 30.0;
            Assert.AreEqual(30.0 * Math.PI / 180.0, presenter.Assembly.Shafts[0].RotationRad, 1e-12);
            Assert.IsTrue(input.IsInput);

            presenter.Select(presenter.Assembly.Shafts[1]);
            var driven = (ShaftNodeProperties)view.Properties;
            var before = presenter.Assembly.Shafts[1].RotationRad;
            driven.RotationDeg = 99.0;
            Assert.AreEqual(before, presenter.Assembly.Shafts[1].RotationRad, 1e-12);
            Assert.IsFalse(driven.IsInput);
        }

        [Test]
        public void TestPresenter_AddMeshedGear_RejectsAGearThatIsNotInTheAssembly()
        {
            var presenter = NewPresenter(out _);

            Assert.Throws<ArgumentException>(() => presenter.AddMeshedGear(Gear("X", 20)));
        }

        #endregion
    }
}
