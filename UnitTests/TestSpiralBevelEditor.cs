using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator.Alibre;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>Spiral, zerol and hypoid bevel gears in the gear train editor: starting a train, adding mates, editing, and the export guard.</summary>
    [TestFixture]
    public class TestSpiralBevelEditor
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

        private static GearTrainPresenter NewPresenter(out FakeView view)
        {
            view = new FakeView();
            return new GearTrainPresenter(view);
        }

        [Test]
        public void TestEditor_SpiralTrain_StartsWithASpiralGearAndAddsAMateOfTheOppositeHand()
        {
            var presenter = NewPresenter(out var view);

            presenter.NewTrain(GearFamily.SpiralBevel);
            var first = (SpiralBevelGearComponent)presenter.Selected;
            Assert.IsInstanceOf<SpiralBevelNodeProperties>(view.Properties);
            Assert.IsFalse(view.IsError, view.Message);

            var mate = (SpiralBevelGearComponent)presenter.AddMate(first, MeshType.Bevel);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(typeof(SpiralBevelGearComponent), mate.GetType());
            Assert.AreEqual(SpiralHand.Right, first.Hand);
            Assert.AreEqual(SpiralHand.Left, mate.Hand, "The mate has the opposite hand.");
            Assert.AreEqual(first.MeanSpiralAngleDeg, mate.MeanSpiralAngleDeg, Tol);
            Assert.AreEqual(first.CutterRadius, mate.CutterRadius, Tol);
            Assert.AreEqual(MeshType.Bevel, presenter.Assembly.Relations[0].MeshType);
            Assert.AreEqual(2, view.Scene.Items.Count);
            Assert.AreEqual(-2.0, view.Report.OverallRatio.Value, Tol);
            Assert.IsInstanceOf<SpiralBevelNodeProperties>(view.Properties);
        }

        [Test]
        public void TestEditor_ZerolTrain_StartsWithAZerolGearAndAddsAZerolMate()
        {
            var presenter = NewPresenter(out var view);

            presenter.NewTrain(GearFamily.ZerolBevel);
            var first = (ZerolBevelGearComponent)presenter.Selected;
            var mate = presenter.AddMate(first, MeshType.Bevel);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.IsInstanceOf<ZerolBevelGearComponent>(mate);
            Assert.AreEqual(SpiralHand.Left, ((ZerolBevelGearComponent)mate).Hand);
            Assert.AreEqual(0.0, ((ZerolBevelGearComponent)mate).MeanSpiralAngleDeg, 0.0);
        }

        [Test]
        public void TestEditor_StraightBevelMate_IsStillAStraightBevelGear()
        {
            var presenter = NewPresenter(out var view);

            presenter.NewTrain(GearFamily.Bevel);
            var mate = presenter.AddMate((BevelGearComponent)presenter.Selected, MeshType.Bevel);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(typeof(BevelGearComponent), mate.GetType());
            Assert.AreEqual(typeof(BevelNodeProperties), view.Properties.GetType());
        }

        [Test]
        public void TestEditor_SpiralHandAndParameters_AreSetOnBothGears()
        {
            var presenter = NewPresenter(out var view);
            presenter.NewTrain(GearFamily.SpiralBevel);
            var first = (SpiralBevelGearComponent)presenter.Selected;
            var mate = (SpiralBevelGearComponent)presenter.AddMate(first, MeshType.Bevel);

            presenter.SetSpiralHand(mate, SpiralHand.Right);
            presenter.SetSpiralParameters(first, 30.0, 120.0);
            presenter.Refresh();

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(SpiralHand.Right, mate.Hand);
            Assert.AreEqual(SpiralHand.Left, first.Hand);
            Assert.AreEqual(30.0, mate.MeanSpiralAngleDeg, Tol);
            Assert.AreEqual(120.0, mate.CutterRadius, Tol);
            Assert.AreEqual(30.0, first.MeanSpiralAngleDeg, Tol);
        }

        [Test]
        public void TestEditor_ASpiralMateWithTheSameHand_IsReportedByTheSolver()
        {
            var presenter = NewPresenter(out var view);
            presenter.NewTrain(GearFamily.SpiralBevel);
            var first = (SpiralBevelGearComponent)presenter.Selected;
            var mate = (SpiralBevelGearComponent)presenter.AddMate(first, MeshType.Bevel);

            mate.Hand = SpiralHand.Right;
            presenter.Refresh();

            Assert.IsTrue(view.IsError);
            StringAssert.Contains("must be Left hand", view.Message);
        }

        [Test]
        public void TestEditor_HypoidTrain_StartsWithAPinionAndAddsTheGearOnAnOffsetShaft()
        {
            var presenter = NewPresenter(out var view);

            presenter.NewTrain(GearFamily.Hypoid);
            var pinion = (HypoidGearComponent)presenter.Selected;
            Assert.IsTrue(pinion.IsPinion);
            Assert.IsInstanceOf<HypoidNodeProperties>(view.Properties);
            Assert.IsFalse(view.IsError, view.Message);

            var gear = (HypoidGearComponent)presenter.AddMate(pinion, MeshType.Hypoid);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.IsFalse(gear.IsPinion);
            Assert.AreEqual(37, gear.Teeth);
            Assert.AreEqual(12, gear.MateTeeth);
            Assert.AreEqual(MeshType.Hypoid, presenter.Assembly.Relations[0].MeshType);
            var offset = presenter.Assembly.Relations[0].OffsetMm;
            Assert.AreEqual(0.25 * pinion.Module * 37.0 / 2.0, offset, Tol);
            Assert.AreEqual(offset, gear.OffsetMm, Tol);
            Assert.AreEqual(SpiralHand.Right, gear.Hand);
            Assert.AreEqual(SpiralHand.Left, pinion.Hand);
            Assert.AreEqual(2, view.Scene.Items.Count);
            Assert.AreEqual(37.0 / 12.0, Math.Abs(view.Report.OverallRatio.Value), Tol);

            var first = presenter.Assembly.Shafts[0];
            var second = presenter.Assembly.Shafts[1];
            var cross = first.Placement.ZAxis.Cross(second.Placement.ZAxis);
            var distance = Math.Abs((second.Placement.Origin - first.Placement.Origin).Dot(cross)) / cross.Length;
            Assert.AreEqual(offset, distance, 1e-4, "The shafts are offset by the relation offset.");
        }

        [Test]
        public void TestEditor_HypoidOffset_SetsTheHandsOfBothGearsAndTheRelation()
        {
            var presenter = NewPresenter(out var view);
            presenter.NewTrain(GearFamily.Hypoid);
            var pinion = (HypoidGearComponent)presenter.Selected;
            var gear = (HypoidGearComponent)presenter.AddMate(pinion, MeshType.Hypoid);

            presenter.SetHypoidOffset(gear, -12.0);
            presenter.Refresh();

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(-12.0, presenter.Assembly.Relations[0].OffsetMm, Tol);
            Assert.AreEqual(-12.0, pinion.OffsetMm, Tol);
            Assert.AreEqual(SpiralHand.Left, gear.Hand);
            Assert.AreEqual(SpiralHand.Right, pinion.Hand);
        }

        [Test]
        public void TestEditor_HypoidHandCannotBeSetDirectly_AndZeroOffsetIsReported()
        {
            var presenter = NewPresenter(out var view);
            presenter.NewTrain(GearFamily.Hypoid);
            var pinion = (HypoidGearComponent)presenter.Selected;
            var gear = (HypoidGearComponent)presenter.AddMate(pinion, MeshType.Hypoid);

            var ex = Assert.Throws<InvalidOperationException>(() => presenter.SetSpiralHand(gear, SpiralHand.Left));
            StringAssert.Contains("offset", ex.Message);

            presenter.SetHypoidOffset(gear, 0.0);
            presenter.Refresh();
            Assert.IsTrue(view.IsError);
            StringAssert.Contains("offset", view.Message);
        }

        [Test]
        public void TestEditor_MateKindsMustMatchTheDriver()
        {
            var presenter = NewPresenter(out _);
            presenter.NewTrain(GearFamily.Hypoid);
            var pinion = (HypoidGearComponent)presenter.Selected;

            Assert.Throws<ArgumentException>(() => presenter.AddMate(pinion, MeshType.Bevel));

            presenter.NewTrain(GearFamily.SpiralBevel);
            Assert.Throws<ArgumentException>(() => presenter.AddMate((GearComponent)presenter.Selected, MeshType.Hypoid));
        }

        [Test]
        public void TestEditor_SavedHypoidTrain_LoadsAgain()
        {
            var presenter = NewPresenter(out _);
            presenter.NewTrain(GearFamily.Hypoid);
            presenter.AddMate((GearComponent)presenter.Selected, MeshType.Hypoid);

            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(presenter.Assembly));
            loaded.Solve();

            Assert.AreEqual(MeshType.Hypoid, loaded.Relations[0].MeshType);
            Assert.AreEqual(presenter.Assembly.Relations[0].OffsetMm, loaded.Relations[0].OffsetMm, Tol);
            Assert.IsInstanceOf<HypoidGearComponent>(loaded.Components.First());
        }

        [Test]
        public void TestAlibreExport_AcceptsSpiralZerolAndHypoidGears_AndReachesTheFirstAlibreCall()
        {
            // An exporter without an Alibre root: the plan and the sections are built from the gear before the first Alibre call, which then fails
            // with the name of its stage (the gears are no longer declined).
            var exporter = (AlibreGearExporter)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(AlibreGearExporter));

            var spiral = new SpiralBevelGearComponent("S1") { Teeth = 18, MateTeeth = 36, Module = 4.0, FaceWidth = 12.0 };
            var zerol = new ZerolBevelGearComponent("Z1") { Teeth = 18, MateTeeth = 36, Module = 4.0, FaceWidth = 12.0 };
            var hypoid = new HypoidGearComponent("H1") { Teeth = 12, MateTeeth = 37, Module = 4.0, FaceWidth = 12.0, IsPinion = true, OffsetMm = 20.0 };

            foreach (var gear in new BevelGearComponent[] { spiral, zerol, hypoid })
            {
                var ex = Assert.Throws<InvalidOperationException>(() => exporter.Export(gear), gear.TypeName);
                StringAssert.Contains("failed while creating the part", ex.Message);
                StringAssert.Contains(gear.Id, ex.Message);
            }
        }

        [Test]
        public void TestAlibreExport_RejectsAnInvalidSpiralGearBeforeAnyAlibreCall()
        {
            var exporter = (AlibreGearExporter)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(AlibreGearExporter));
            var spiral = new SpiralBevelGearComponent("S1") { Teeth = 18, MateTeeth = 36, Module = 4.0, FaceWidth = 5000.0 };

            var ex = Assert.Throws<InvalidOperationException>(() => exporter.Export(spiral));

            StringAssert.Contains(spiral.Id, ex.Message);
            StringAssert.DoesNotContain("failed while", ex.Message);
        }
    }
}
