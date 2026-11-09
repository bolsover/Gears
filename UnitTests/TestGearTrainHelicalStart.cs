using System.Collections.Generic;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>Starting a new gear train with each <see cref="GearFamily"/>, in particular the helical spur gear.</summary>
    [TestFixture]
    public class TestGearTrainHelicalStart
    {
        private sealed class FakeView : IGearTrainView
        {
            public double InputSpeedRpm { get; set; } = 1000.0;
            public string Message;
            public bool IsError;

            public void ShowTree(TrainTreeNode root, object selectedTag) { }
            public void ShowSelection(object selectedTag) { }
            public void ShowProperties(object properties) { }
            public void ShowScene(AssemblyScene scene, object selectedTag, ISet<GearComponent> hiddenGears) { }
            public void ShowReport(GearTrainReport report) { }

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
        public void TestNewTrain_Helical_StartsWithOneHelicalInvoluteGear()
        {
            var presenter = NewPresenter(out var view);

            presenter.NewTrain(GearFamily.Helical);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(1, presenter.Assembly.Shafts.Count);
            Assert.AreEqual(1, presenter.Assembly.Shafts[0].Mounts.Count);
            var gear = presenter.Selected as InvoluteGearComponent;
            Assert.IsNotNull(gear, "The first gear is an involute gear.");
            Assert.AreSame(gear, presenter.Assembly.Shafts[0].Mounts[0].Component);
            Assert.IsTrue(gear.IsHelical);
            Assert.AreEqual(15.0, gear.HelixAngleDeg, 1e-9);
            Assert.AreEqual("R", gear.HelicalDirection);
            Assert.AreEqual(20, gear.Teeth);
        }

        [Test]
        public void TestNewTrain_Spur_StaysStraight()
        {
            var presenter = NewPresenter(out var view);

            presenter.NewTrain();
            var defaultGear = (InvoluteGearComponent)presenter.Selected;
            presenter.NewTrain(GearFamily.Spur);
            var spur = (InvoluteGearComponent)presenter.Selected;

            Assert.IsFalse(view.IsError, view.Message);
            Assert.IsFalse(defaultGear.IsHelical);
            Assert.IsFalse(spur.IsHelical);
            Assert.AreEqual(20, spur.Teeth);
        }

        [Test]
        public void TestNewTrain_OtherFamilies_StartWithTheirOwnGearType()
        {
            var presenter = NewPresenter(out var view);

            presenter.NewTrain(GearFamily.Bevel);
            Assert.AreEqual(typeof(BevelGearComponent), presenter.Selected.GetType());
            presenter.NewTrain(GearFamily.Bevel, BevelGearType.Gleason);
            Assert.AreEqual(BevelGearType.Gleason, ((BevelGearComponent)presenter.Selected).BevelType);
            presenter.NewTrain(GearFamily.Worm);
            Assert.AreEqual(typeof(WormComponent), presenter.Selected.GetType());
            presenter.NewTrain(GearFamily.SpiralBevel);
            Assert.AreEqual(typeof(SpiralBevelGearComponent), presenter.Selected.GetType());
            presenter.NewTrain(GearFamily.ZerolBevel);
            Assert.AreEqual(typeof(ZerolBevelGearComponent), presenter.Selected.GetType());
            presenter.NewTrain(GearFamily.Hypoid);
            Assert.AreEqual(typeof(HypoidGearComponent), presenter.Selected.GetType());

            Assert.IsFalse(view.IsError, view.Message);
        }

        [Test]
        public void TestGearFamily_ExistingValuesAreUnchanged_AndHelicalIsAppended()
        {
            Assert.AreEqual(0, (int)GearFamily.Spur);
            Assert.AreEqual(1, (int)GearFamily.Bevel);
            Assert.AreEqual(2, (int)GearFamily.Worm);
            Assert.AreEqual(3, (int)GearFamily.SpiralBevel);
            Assert.AreEqual(4, (int)GearFamily.ZerolBevel);
            Assert.AreEqual(5, (int)GearFamily.Hypoid);
            Assert.AreEqual(6, (int)GearFamily.Helical);
        }
    }
}
