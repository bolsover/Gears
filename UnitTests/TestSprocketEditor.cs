using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>The gear train editor commands for ISO 606 sprockets and roller chains: presenter, tree and property grids.</summary>
    [TestFixture]
    public class TestSprocketEditor
    {
        private sealed class FakeView : IGearTrainView
        {
            public double InputSpeedRpm { get; set; } = 1000.0;
            public TrainTreeNode Tree;
            public object Properties;
            public AssemblyScene Scene;
            public string Message;
            public bool IsError;

            public void ShowTree(TrainTreeNode root, object selectedTag) => Tree = root;
            public void ShowSelection(object selectedTag) { }
            public void ShowProperties(object properties) => Properties = properties;
            public void ShowScene(AssemblyScene scene, IReadOnlyCollection<object> selectedItems, ISet<GearComponent> hiddenGears) => Scene = scene;
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
        public void TestEditor_NewSprocket_StartsATrainWithOneSprocketThatIsSelectedAndDrawn()
        {
            var presenter = NewPresenter(out var view);

            var sprocket = presenter.NewSprocket(ChainSeries.B10);

            Assert.AreEqual(1, presenter.Assembly.Shafts.Count);
            Assert.AreSame(sprocket, presenter.Assembly.Components.Single());
            Assert.AreSame(sprocket, presenter.Selected);
            Assert.AreEqual(ChainSeries.B10, sprocket.Series);
            Assert.AreEqual(19, sprocket.Teeth, "The default is 19 teeth.");
            Assert.AreEqual("Sprocket 1", sprocket.Id);
            Assert.AreEqual(1, view.Scene.Items.Count);
            Assert.IsInstanceOf<SprocketNodeProperties>(view.Properties);

            Assert.AreEqual(25, presenter.NewSprocket(ChainSeries.B06, 25).Teeth);
        }

        [Test]
        public void TestEditor_AddSprocketToShaft_PutsItBesideTheExistingPartsWithAGap()
        {
            var presenter = NewPresenter(out _);
            var first = presenter.NewSprocket(ChainSeries.B08);
            var shaft = presenter.Assembly.Shafts[0];

            var second = presenter.AddSprocketToShaft(shaft, ChainSeries.B08, 30);

            Assert.AreEqual(2, shaft.Mounts.Count);
            Assert.AreEqual(30, second.Teeth);
            shaft.Mounts[0].GetAxialExtent(out _, out var firstEnd);
            shaft.Mounts[1].GetAxialExtent(out var secondStart, out _);
            Assert.AreEqual(2.0, secondStart - firstEnd, 1e-6, "A gap of 2 mm between the sprockets.");
            Assert.AreSame(second, presenter.Selected);
            Assert.AreNotSame(first, second);
            Assert.Throws<ArgumentException>(() => presenter.AddSprocketToShaft(new Shaft("X"), ChainSeries.B08));
        }

        [Test]
        public void TestEditor_AddChainedSprocket_AddsAShaftASprocketAndAChain()
        {
            var presenter = NewPresenter(out var view);
            var driver = presenter.NewSprocket(ChainSeries.B08);

            var driven = presenter.AddChainedSprocket(driver, 38);

            Assert.AreEqual(2, presenter.Assembly.Shafts.Count);
            Assert.AreEqual(1, presenter.Assembly.Chains.Count);
            var chain = presenter.Assembly.Chains[0];
            Assert.AreSame(driver, chain.Driver);
            Assert.AreSame(driven, chain.Driven);
            Assert.AreEqual("Chain 1", chain.Id);
            Assert.AreEqual(38, driven.Teeth);
            Assert.AreEqual(driver.Series, driven.Series);
            Assert.GreaterOrEqual(chain.CentreDistance, 30.0 * 12.7 - 1e-6, "At least 30 pitches by default.");
            Assert.Greater(chain.CentreDistance, chain.MinimumCentreDistance);
            Assert.AreSame(driven, presenter.Selected);
            Assert.AreEqual(1, view.Scene.ChainItems.Count, "The chain is in the scene.");
            Assert.AreEqual(2, view.Scene.Items.Count);

            var drivenShaft = presenter.Assembly.Shafts[1];
            Assert.AreEqual(chain.CentreDistance, drivenShaft.Placement.Origin.X, 1e-6, "The chain places the driven shaft.");
        }

        [Test]
        public void TestEditor_AddChainedSprocket_WithATooSmallCentreDistanceLeavesTheTrainAsItWas()
        {
            var presenter = NewPresenter(out _);
            var driver = presenter.NewSprocket(ChainSeries.B08);

            Assert.Throws<InvalidOperationException>(() => presenter.AddChainedSprocket(driver, 0, 20.0));

            Assert.AreEqual(1, presenter.Assembly.Shafts.Count, "The new shaft is taken out again.");
            Assert.AreEqual(0, presenter.Assembly.Chains.Count);
        }

        [Test]
        public void TestEditor_Tree_ListsTheChainAndWhereEachSprocketGetsItsDrive()
        {
            var presenter = NewPresenter(out var view);
            var driver = presenter.NewSprocket(ChainSeries.B08);
            presenter.AddChainedSprocket(driver, 25);

            var chainNode = view.Tree.Children.Single(n => n.Kind == TrainNodeKind.Chain);
            Assert.AreSame(presenter.Assembly.Chains[0], chainNode.Tag);
            StringAssert.Contains("ISO 606 08B", chainNode.Text);
            StringAssert.Contains("links", chainNode.Text);

            var drivenText = view.Tree.Children.Where(n => n.Kind == TrainNodeKind.Shaft).SelectMany(s => s.Children).Last().Text;
            StringAssert.Contains("chain from Sprocket 1", drivenText);
        }

        [Test]
        public void TestEditor_SelectingAChain_ShowsItsPropertiesAndTheCentreDistanceCanBeEdited()
        {
            var presenter = NewPresenter(out var view);
            var driver = presenter.NewSprocket(ChainSeries.B08);
            presenter.AddChainedSprocket(driver, 38);
            var chain = presenter.Assembly.Chains[0];

            presenter.Select(chain);

            var properties = (ChainNodeProperties)view.Properties;
            Assert.AreEqual(chain.LinkCount, properties.Links);
            Assert.AreEqual(0, properties.Links % 2);
            Assert.AreEqual(2.0, properties.Ratio, 1e-12);
            var links = properties.Links;

            properties.CentreDistance = chain.CentreDistance + 100.0;
            properties.CentreLineAngleDeg = 25.0;
            presenter.Refresh();

            Assert.Greater(chain.LinkCount, links, "A longer centre distance needs a longer chain.");
            Assert.AreEqual(25.0, chain.CentreLineAngleDeg, 1e-12);
            var drivenShaft = presenter.Assembly.Shafts[1];
            Assert.AreEqual(chain.CentreDistance * Math.Cos(25.0 * Math.PI / 180.0), drivenShaft.Placement.Origin.X, 1e-6);
        }

        [Test]
        public void TestEditor_SprocketProperties_EditTheSprocketAndShowTheIsoDimensions()
        {
            var presenter = NewPresenter(out var view);
            var sprocket = presenter.NewSprocket(ChainSeries.B08, 19);
            var properties = (SprocketNodeProperties)view.Properties;

            Assert.AreEqual(12.7, properties.Pitch, 1e-12);
            Assert.AreEqual(sprocket.PitchDiameter, properties.PitchDiameter, 1e-12);
            Assert.AreEqual(sprocket.RootDiameter, properties.RootDiameter, 1e-12);

            properties.Series = ChainSeries.B12;
            properties.Teeth = 3;
            properties.ToothForm = SprocketToothForm.Minimum;
            properties.BoreDiameter = 20.0;
            properties.HubDiameter = 40.0;
            properties.HubLength = 15.0;
            properties.BoreDiameter = -5.0;
            properties.BoreDiameter = 20.0;

            Assert.AreEqual(ChainSeries.B12, sprocket.Series);
            Assert.AreEqual(ChainProfile.AbsoluteMinimumTeeth, sprocket.Teeth, "Fewer than the minimum is raised to it.");
            Assert.AreEqual(SprocketToothForm.Minimum, sprocket.ToothForm);
            Assert.IsTrue(sprocket.HasHub);
            Assert.AreEqual(ChainProfile.Get(ChainSeries.B12).ToothWidth, sprocket.FaceWidth, 1e-9, "The width follows the series.");
            Assert.AreEqual(ChainProfile.Get(ChainSeries.B12).Pitch, properties.Pitch, 1e-12);
            Assert.AreEqual(sprocket.Profile.FlankRadius(sprocket.Teeth, SprocketToothForm.Minimum), properties.FlankRadius, 1e-12);
            Assert.IsTrue(properties.BelowRecommendedMinimum);
        }

        [Test]
        public void TestEditor_RemoveSelectedChain_LeavesTheSprockets()
        {
            var presenter = NewPresenter(out _);
            var driver = presenter.NewSprocket(ChainSeries.B08);
            presenter.AddChainedSprocket(driver);
            presenter.Select(presenter.Assembly.Chains[0]);

            presenter.RemoveSelected();

            Assert.AreEqual(0, presenter.Assembly.Chains.Count);
            Assert.AreEqual(2, presenter.Assembly.Components.Count());
            Assert.AreEqual(2, presenter.Assembly.Shafts.Count);
            Assert.IsNull(presenter.Selected);
        }

        [Test]
        public void TestEditor_RemovingTheDrivenSprocket_TakesTheChainWithItAndLeavesTheShaftLikeABelt()
        {
            var presenter = NewPresenter(out _);
            var driver = presenter.NewSprocket(ChainSeries.B08);
            var driven = presenter.AddChainedSprocket(driver);
            presenter.Select(driven);

            presenter.RemoveSelected();

            Assert.AreEqual(0, presenter.Assembly.Chains.Count);
            Assert.AreEqual(1, presenter.Assembly.Components.Count());
            Assert.AreSame(driver, presenter.Assembly.Components.Single());
            Assert.AreEqual(2, presenter.Assembly.Shafts.Count, "As with a belted pulley, the shaft stays.");
        }

        [Test]
        public void TestEditor_ChainsAreMultiSelectableAndPrunedWhenTheyGo()
        {
            var presenter = NewPresenter(out _);
            var driver = presenter.NewSprocket(ChainSeries.B08);
            var driven = presenter.AddChainedSprocket(driver);
            var chain = presenter.Assembly.Chains[0];

            presenter.Select(driver);
            presenter.ToggleSelection(chain);
            presenter.ToggleSelection(driven);

            CollectionAssert.AreEqual(new object[] { driver, chain, driven }, presenter.SelectedItems);

            presenter.Assembly.RemoveChain(chain);
            presenter.Refresh();
            CollectionAssert.AreEqual(new object[] { driver, driven }, presenter.SelectedItems);
        }

        [Test]
        public void TestEditor_SaveAndLoad_KeepTheChainAndNumberTheNextOneAfterIt()
        {
            var presenter = NewPresenter(out _);
            var driver = presenter.NewSprocket(ChainSeries.B16, 21);
            presenter.AddChainedSprocket(driver, 42);
            var path = Path.Combine(Path.GetTempPath(), "sprocket_editor_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                Assert.IsTrue(presenter.Save(path));

                var other = NewPresenter(out _);
                Assert.IsTrue(other.Load(path));

                var chain = other.Assembly.Chains.Single();
                Assert.AreEqual("Chain 1", chain.Id);
                Assert.AreEqual(ChainSeries.B16, chain.Driver.Series);
                Assert.AreEqual(42, chain.Driven.Teeth);

                var third = other.AddChainedSprocket((SprocketComponent)chain.Driven, 20);
                Assert.AreEqual("Chain 2", other.Assembly.Chains[1].Id, "The numbering goes on after the loaded chain.");
                Assert.AreSame(third, other.Selected);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
