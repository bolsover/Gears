using System.Collections.Generic;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>Selecting several gears, shafts and belts in <see cref="GearTrainPresenter"/>; the last one feeds the property editor.</summary>
    [TestFixture]
    public class TestMultiSelection
    {
        private sealed class FakeView : IGearTrainView
        {
            public double InputSpeedRpm { get; set; } = 1000.0;
            public object Properties;
            public object SelectionShown;
            public List<object> SceneSelectedItems = new List<object>();
            public string Message;
            public bool IsError;

            public void ShowTree(TrainTreeNode root, object selectedTag) { }
            public void ShowSelection(object selectedTag) => SelectionShown = selectedTag;
            public void ShowProperties(object properties) => Properties = properties;

            public void ShowScene(AssemblyScene scene, IReadOnlyCollection<object> selectedItems, ISet<GearComponent> hiddenGears)
            {
                SceneSelectedItems = new List<object>(selectedItems);
            }

            public void ShowReport(GearTrainReport report) { }

            public void ShowMessage(string message, bool isError)
            {
                Message = message;
                IsError = isError;
            }
        }

        private FakeView _view;
        private GearTrainPresenter _presenter;
        private GearComponent _first;
        private GearComponent _second;
        private Shaft _firstShaft;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeView();
            _presenter = new GearTrainPresenter(_view);
            _presenter.NewTrain();
            _first = (GearComponent)_presenter.Selected;
            _second = _presenter.AddMeshedGear(_first);
            _firstShaft = _presenter.Assembly.Shafts[0];
            _presenter.Select(null);
        }

        [Test]
        public void TestToggleSelection_AddsItemsInOrder_AndLastIsSelected()
        {
            _presenter.ToggleSelection(_first);
            _presenter.ToggleSelection(_second);

            CollectionAssert.AreEqual(new object[] { _first, _second }, _presenter.SelectedItems);
            Assert.AreSame(_second, _presenter.Selected);
            Assert.AreSame(_second, _view.SelectionShown);
        }

        [Test]
        public void TestToggleSelection_SecondToggleRemoves_AndPreviousBecomesLast()
        {
            _presenter.ToggleSelection(_first);
            _presenter.ToggleSelection(_second);

            _presenter.ToggleSelection(_second);

            CollectionAssert.AreEqual(new object[] { _first }, _presenter.SelectedItems);
            Assert.AreSame(_first, _presenter.Selected);
            Assert.IsInstanceOf<GearNodeProperties>(_view.Properties);
        }

        [Test]
        public void TestToggleSelection_LastItemToggledOff_ClearsSelectionAndProperties()
        {
            _presenter.ToggleSelection(_first);

            _presenter.ToggleSelection(_first);

            Assert.AreEqual(0, _presenter.SelectedItems.Count);
            Assert.IsNull(_presenter.Selected);
            Assert.IsNull(_view.Properties);
            Assert.AreEqual(0, _view.SceneSelectedItems.Count);
        }

        [Test]
        public void TestToggleSelection_IgnoresItemsThatCannotBeSelected()
        {
            _presenter.ToggleSelection(null);
            _presenter.ToggleSelection(_presenter.Assembly);
            _presenter.ToggleSelection("text");

            Assert.AreEqual(0, _presenter.SelectedItems.Count);
            Assert.IsNull(_presenter.Selected);
        }

        [Test]
        public void TestSelect_ReplacesAMultiSelection()
        {
            _presenter.ToggleSelection(_first);
            _presenter.ToggleSelection(_second);

            _presenter.Select(_first);

            CollectionAssert.AreEqual(new object[] { _first }, _presenter.SelectedItems);
            Assert.AreSame(_first, _presenter.Selected);
            CollectionAssert.AreEqual(new object[] { _first }, _view.SceneSelectedItems);
        }

        [Test]
        public void TestSelect_UnknownTag_ClearsTheSelection()
        {
            _presenter.ToggleSelection(_first);

            _presenter.Select(_presenter.Assembly);

            Assert.AreEqual(0, _presenter.SelectedItems.Count);
            Assert.IsNull(_presenter.Selected);
        }

        [Test]
        public void TestPropertyView_FollowsTheLastSelectedItem()
        {
            _presenter.ToggleSelection(_first);
            _presenter.ToggleSelection(_firstShaft);
            Assert.IsInstanceOf<ShaftNodeProperties>(_view.Properties);

            _presenter.ToggleSelection(_firstShaft);

            Assert.IsInstanceOf<GearNodeProperties>(_view.Properties);
        }

        [Test]
        public void TestShowScene_ReceivesEverySelectedItem()
        {
            _presenter.ToggleSelection(_first);
            _presenter.ToggleSelection(_firstShaft);
            _presenter.ToggleSelection(_second);

            CollectionAssert.AreEqual(new object[] { _first, _firstShaft, _second }, _view.SceneSelectedItems);
        }

        [Test]
        public void TestRemoveSelected_RemovesOnlyTheLastSelectedItem_AndPrunesTheRest()
        {
            _presenter.ToggleSelection(_first);
            _presenter.ToggleSelection(_second);

            _presenter.RemoveSelected();

            Assert.IsFalse(_view.IsError, _view.Message);
            Assert.AreEqual(1, _presenter.Assembly.Shafts.Count);
            CollectionAssert.AreEqual(new object[] { _first }, _presenter.SelectedItems);
            Assert.AreSame(_first, _presenter.Selected);
            CollectionAssert.AreEqual(new object[] { _first }, _view.SceneSelectedItems);
        }

        [Test]
        public void TestRefresh_DropsSelectedItemsThatAreNoLongerInTheAssembly()
        {
            _presenter.ToggleSelection(_second);
            _presenter.ToggleSelection(_first);

            // Removing the driving gear takes the driven gear and its shaft with it.
            _presenter.RemoveSelected();

            Assert.AreEqual(0, _presenter.SelectedItems.Count);
            CollectionAssert.DoesNotContain(_presenter.SelectedItems, _first);
            CollectionAssert.DoesNotContain(_presenter.SelectedItems, _second);
            Assert.IsNull(_presenter.Selected);
        }

        [Test]
        public void TestNewTrain_StartsWithOneSelectedGear()
        {
            _presenter.ToggleSelection(_first);
            _presenter.ToggleSelection(_second);

            _presenter.NewTrain(GearFamily.Helical);

            Assert.AreEqual(1, _presenter.SelectedItems.Count);
            Assert.AreSame(_presenter.Selected, _presenter.SelectedItems[0]);
        }
    }
}
