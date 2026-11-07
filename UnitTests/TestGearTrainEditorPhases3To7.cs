using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Phases 3 to 7 in the gear train editor: starting from a bevel gear or a worm, adding rack, bevel and worm wheel
    /// mates, planetary stages, saving and loading, exporting and the animation step.
    /// </summary>
    [TestFixture]
    public class TestGearTrainEditorPhases3To7
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

            public void ShowScene(AssemblyScene scene, object selectedTag, ISet<GearComponent> hiddenGears) => Scene = scene;

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
            var presenter = new GearTrainPresenter(view);
            presenter.NewTrain();
            return presenter;
        }

        private static string TempFile(string extension)
        {
            return Path.Combine(Path.GetTempPath(), "gears_editor_" + Guid.NewGuid().ToString("N") + extension);
        }

        #region Starting gears and mates

        [Test]
        public void TestEditor_BevelTrain_StartsWithABevelGearAndAddsAMate()
        {
            var presenter = NewPresenter(out var view);

            presenter.NewTrain(GearFamily.Bevel);
            var first = (BevelGearComponent)presenter.Selected;
            var mate = (BevelGearComponent)presenter.AddMate(first, MeshType.Bevel);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(2, presenter.Assembly.Shafts.Count);
            Assert.AreEqual(MeshType.Bevel, presenter.Assembly.Relations[0].MeshType);
            Assert.AreEqual(40, mate.Teeth);
            Assert.AreEqual(first.Module, mate.Module, Tol);
            Assert.AreEqual(-2.0, view.Report.OverallRatio.Value, Tol);
            Assert.AreEqual(2, view.Scene.Items.Count);
            Assert.AreEqual(0.0, presenter.Assembly.Shafts[1].Placement.ZAxis.Dot(presenter.Assembly.Shafts[0].Placement.ZAxis), 1e-9, "A 90 degree bevel pair.");
            Assert.IsInstanceOf<BevelNodeProperties>(view.Properties);
        }

        [Test]
        public void TestEditor_WormTrain_StartsWithAWormAndAddsAWheel()
        {
            var presenter = NewPresenter(out var view);

            presenter.NewTrain(GearFamily.Worm);
            var worm = (WormComponent)presenter.Selected;
            var wheel = (WormWheelComponent)presenter.AddMate(worm, MeshType.Worm, 40);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(40, wheel.Teeth);
            Assert.AreEqual(-40.0, view.Report.OverallRatio.Value, Tol, "A one start right-hand worm: ratio -40 for 40 teeth.");
            Assert.AreEqual(worm.LeadAngleDeg, wheel.LeadAngleDeg, 1e-12);
            Assert.IsInstanceOf<WormWheelNodeProperties>(view.Properties);

            presenter.Select(worm);
            Assert.IsInstanceOf<WormNodeProperties>(view.Properties);
        }

        [Test]
        public void TestEditor_MeshedGear_OfAHerringboneGear_IsHerringboneWithTheOppositeHand()
        {
            var presenter = NewPresenter(out var view);
            var driver = (InvoluteGearComponent)presenter.Selected;
            driver.HelixAngleDeg = 20.0;
            driver.HelicalDirection = "R";
            driver.IsHerringbone = true;

            var meshed = presenter.AddMeshedGear(driver, 30);
            var viaMate = (InvoluteGearComponent)presenter.AddMate(driver, MeshType.Parallel, 25);

            Assert.IsFalse(view.IsError, view.Message);
            foreach (var gear in new[] { meshed, viaMate })
            {
                Assert.IsTrue(gear.IsHerringbone, "The meshed gear is herringbone like its driver.");
                Assert.AreEqual("Herringbone gear", gear.TypeName);
                Assert.AreEqual(20.0, gear.HelixAngleDeg, Tol);
                Assert.AreEqual("L", gear.HelicalDirection);
            }
        }

        [Test]
        public void TestEditor_MeshedGear_OfAHelicalOrSpurGear_IsNotHerringbone()
        {
            var presenter = NewPresenter(out _);
            var driver = (InvoluteGearComponent)presenter.Selected;

            Assert.IsFalse(presenter.AddMeshedGear(driver, 30).IsHerringbone, "A spur driver gives a spur gear.");

            driver.HelixAngleDeg = 20.0;
            Assert.IsFalse(presenter.AddMeshedGear(driver, 30).IsHerringbone, "A helical driver gives a helical gear.");
        }

        [Test]
        public void TestEditor_Rack_OfAHerringbonePinion_IsHerringbone()
        {
            var presenter = NewPresenter(out var view);
            var pinion = (InvoluteGearComponent)presenter.Selected;
            pinion.HelixAngleDeg = 20.0;
            pinion.HelicalDirection = "R";
            pinion.IsHerringbone = true;

            var rack = (RackComponent)presenter.AddMate(pinion, MeshType.Rack);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.IsTrue(rack.IsHerringbone, "The rack is herringbone like its pinion.");
            Assert.AreEqual("Herringbone rack", rack.TypeName);
            Assert.AreEqual(20.0, rack.HelixAngleDeg, Tol);
            Assert.AreEqual("L", rack.HelicalDirection);
        }

        [Test]
        public void TestEditor_Rack_OfAHelicalOrSpurPinion_IsNotHerringbone()
        {
            var presenter = NewPresenter(out _);
            var pinion = (InvoluteGearComponent)presenter.Selected;
            Assert.IsFalse(((RackComponent)presenter.AddMate(pinion, MeshType.Rack)).IsHerringbone);

            pinion.HelixAngleDeg = 20.0;
            Assert.IsFalse(((RackComponent)presenter.AddMate(pinion, MeshType.Rack)).IsHerringbone);
        }

        [Test]
        public void TestEditor_Rack_CanBeAddedToAPinion()
        {
            var presenter = NewPresenter(out var view);
            var pinion = (InvoluteGearComponent)presenter.Selected;

            var rack = (RackComponent)presenter.AddMate(pinion, MeshType.Rack);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(12, rack.Teeth);
            Assert.AreEqual(pinion.Module, rack.Module, Tol);
            var carrierRow = view.Report.Shafts.Single(s => s.ShaftId == presenter.Assembly.Shafts[1].Id);
            Assert.IsTrue(carrierRow.IsLinear);
            Assert.IsInstanceOf<RackNodeProperties>(view.Properties);
        }

        [Test]
        public void TestEditor_ModuleChangeOnAWormPropagatesToTheWheel()
        {
            var presenter = NewPresenter(out var view);
            presenter.NewTrain(GearFamily.Worm);
            var worm = (WormComponent)presenter.Selected;
            var wheel = (WormWheelComponent)presenter.AddMate(worm, MeshType.Worm);

            presenter.Select(worm);
            var properties = (WormNodeProperties)view.Properties;
            properties.Module = 3.0;

            Assert.AreEqual(3.0, wheel.Module, Tol);
            presenter.Refresh();
            Assert.IsFalse(view.IsError, view.Message);
        }

        [Test]
        public void TestEditor_AddMate_RejectsTheWrongKindOfDriverAndKeepsTheTrain()
        {
            var presenter = NewPresenter(out _);
            var spur = presenter.Selected as GearComponent;
            var shafts = presenter.Assembly.Shafts.Count;

            Assert.Throws<ArgumentException>(() => presenter.AddMate(spur, MeshType.Worm));
            Assert.Throws<ArgumentException>(() => presenter.AddMate(spur, MeshType.Bevel));
            Assert.Throws<ArgumentNullException>(() => presenter.AddMate(null, MeshType.Rack));

            Assert.AreEqual(shafts, presenter.Assembly.Shafts.Count, "Nothing is added when the mate is refused.");
        }

        [Test]
        public void TestEditor_AMateThatCannotBeSolvedIsReportedNotThrown()
        {
            var presenter = NewPresenter(out var view);
            presenter.NewTrain(GearFamily.Bevel);
            var first = (BevelGearComponent)presenter.Selected;
            first.FaceWidth = 500.0;

            presenter.AddMate(first, MeshType.Bevel);

            Assert.IsTrue(view.IsError);
            StringAssert.Contains("cone distance", view.Message);
        }

        #endregion

        #region Planetary

        [Test]
        public void TestEditor_Planetary_ShowsTheStageAndItsRatio()
        {
            var presenter = NewPresenter(out var view);

            presenter.NewPlanetary();

            Assert.IsFalse(view.IsError, view.Message);
            Assert.IsNotNull(presenter.Planetary);
            Assert.AreEqual(6, presenter.Assembly.Shafts.Count);
            Assert.AreEqual(5, view.Scene.Items.Count);
            Assert.AreEqual(3.5, view.Report.OverallRatio.Value, Tol, "Ring fixed, sun in, carrier out: 1 + Zr / Zs.");
            Assert.IsInstanceOf<PlanetaryNodeProperties>(view.Properties);
            StringAssert.Contains("Planetary", view.Message);
        }

        [Test]
        public void TestEditor_Planetary_TheRingStaysStillWhileTheSunTurns()
        {
            var presenter = NewPresenter(out _);
            presenter.NewPlanetary();
            var ringBefore = presenter.Planetary.RingAngleRad;

            presenter.SetInputAngle(90.0);

            Assert.AreEqual(ringBefore, presenter.Planetary.RingAngleRad, 1e-9);
            Assert.AreEqual(Math.PI / 2.0, presenter.InputAngleRad, 1e-12);
            Assert.AreEqual(Math.PI / 2.0 * 24.0 / 84.0, presenter.Planetary.CarrierAngleRad, 1e-9);
        }

        [Test]
        public void TestEditor_Planetary_EditingThePropertiesRebuildsTheStage()
        {
            var presenter = NewPresenter(out var view);
            presenter.NewPlanetary();
            var properties = (PlanetaryNodeProperties)view.Properties;

            properties.PlanetCount = 4;
            presenter.Refresh();

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(4 + 1 + 1 + 1, presenter.Assembly.Shafts.Count);

            properties.Fixed = PlanetaryMember.Sun;
            presenter.Refresh();
            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreNotEqual(PlanetaryMember.Sun, presenter.PlanetaryInput, "The input moves off the fixed member.");
        }

        [Test]
        public void TestEditor_Planetary_AnImpossibleStageKeepsThePreviousSceneAndShowsAnError()
        {
            var presenter = NewPresenter(out var view);
            presenter.NewPlanetary();
            var sceneBefore = view.Scene;
            var properties = (PlanetaryNodeProperties)view.Properties;

            properties.PlanetCount = 5;
            presenter.Refresh();

            Assert.IsTrue(view.IsError);
            StringAssert.Contains("spaced equally", view.Message);
            Assert.AreSame(sceneBefore, view.Scene);
        }

        [Test]
        public void TestEditor_Planetary_OrdinaryEditingCommandsAreRefused()
        {
            var presenter = NewPresenter(out _);
            presenter.NewPlanetary();

            Assert.Throws<InvalidOperationException>(() => presenter.AddGearToShaft(presenter.Assembly.Shafts[0]));
            Assert.Throws<InvalidOperationException>(() => presenter.AddMate(presenter.Assembly.Components.First(), MeshType.Rack));
            Assert.Throws<InvalidOperationException>(() => presenter.RemoveSelected());

            presenter.NewTrain();
            Assert.IsNull(presenter.Planetary, "Starting a new train leaves the planetary mode.");
        }

        [Test]
        public void TestEditor_Planetary_HelicalStageIsShownAsHelicalGears()
        {
            var presenter = NewPresenter(out var view);
            var set = new PlanetaryGearSet("PS") { HelixAngleDeg = 20.0, HelicalDirection = "L", IsHerringbone = true };

            presenter.NewPlanetary(set);

            Assert.IsFalse(view.IsError, view.Message);
            var gears = presenter.Assembly.Components.OfType<InvoluteGearComponent>().ToList();
            Assert.AreEqual(5, gears.Count);
            Assert.IsTrue(gears.All(g => g.IsHerringbone && Math.Abs(g.HelixAngleDeg - 20.0) < Tol));
        }

        [Test]
        public void TestEditor_Planetary_PropertyGridEditsTheHelixSettings()
        {
            var presenter = NewPresenter(out _);
            presenter.NewPlanetary();
            var set = presenter.Planetary;

            var properties = new PlanetaryNodeProperties(presenter, set)
            {
                HelixAngle = 15.0,
                HelicalDirection = "l",
                IsHerringbone = true
            };

            Assert.AreEqual(15.0, set.HelixAngleDeg, Tol);
            Assert.AreEqual("L", set.HelicalDirection);
            Assert.IsTrue(set.IsHerringbone);
            Assert.AreEqual("L", properties.HelicalDirection);
        }

        [Test]
        public void TestEditor_NewPulley_ShowsAnHtdPulleyInTheScene()
        {
            var presenter = NewPresenter(out var view);

            var pulley = presenter.NewPulley(HtdSeries.Htd8M, 30);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(1, presenter.Assembly.Shafts.Count);
            Assert.AreSame(pulley, presenter.Selected);
            Assert.AreEqual(HtdSeries.Htd8M, pulley.Series);
            Assert.AreEqual(30, pulley.Teeth);
            Assert.AreEqual(1, presenter.Scene.Items.Count);
            Assert.Greater(presenter.Scene.Items[0].Mesh.Vertices.Count, 100);
            Assert.AreEqual(pulley.PitchDiameter, presenter.Report.Gears.Single().PitchDiameter, 1e-6);
        }

        [Test]
        public void TestEditor_NewPulley_WithoutATeethCountChoosesAUsableOne()
        {
            var presenter = NewPresenter(out var view);

            foreach (var profile in HtdProfile.Profiles)
            {
                var pulley = presenter.NewPulley(profile.Series);

                Assert.IsFalse(view.IsError, view.Message);
                Assert.GreaterOrEqual(pulley.Teeth, profile.MinimumTeeth);
                Assert.AreEqual(profile.StandardBeltWidth, pulley.FaceWidth, Tol);
            }
        }

        [Test]
        public void TestEditor_AddPulleyToShaft_PlacesItBesideTheExistingParts()
        {
            var presenter = NewPresenter(out var view);
            presenter.NewTrain();
            var shaft = presenter.InputShaft;

            var first = presenter.AddPulleyToShaft(shaft, HtdSeries.Htd5M, 24);
            var second = presenter.AddPulleyToShaft(shaft, HtdSeries.Htd5M, 24);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(3, shaft.Mounts.Count);
            var offsets = shaft.Mounts.Select(m => m.AxialOffset).ToList();
            Assert.Less(offsets[0], offsets[1]);
            Assert.Less(offsets[1], offsets[2]);
            first.GetBodyAxialExtent(out var start, out var end);
            var firstMount = presenter.Assembly.FindMount(first, out _);
            var secondMount = presenter.Assembly.FindMount(second, out _);
            Assert.GreaterOrEqual(secondMount.AxialOffset + start, firstMount.AxialOffset + end, "The pulleys do not overlap.");
            Assert.AreEqual(3, presenter.Scene.Items.Count);
        }

        [Test]
        public void TestEditor_PulleyPropertyGrid_EditsTheSeriesAndTheGrooves()
        {
            var presenter = NewPresenter(out _);
            var pulley = presenter.NewPulley(HtdSeries.Htd5M, 20);
            var mount = presenter.Assembly.FindMount(pulley, out _);

            var properties = new PulleyNodeProperties(presenter, pulley, mount, null)
            {
                Series = HtdSeries.Htd14M,
                Teeth = 40,
                HasFlanges = true
            };

            Assert.AreEqual(HtdSeries.Htd14M, pulley.Series);
            Assert.AreEqual(40, pulley.Teeth);
            Assert.IsTrue(pulley.HasFlanges);
            Assert.AreEqual(pulley.OutsideDiameter, properties.OutsideDiameter, Tol);
            Assert.AreEqual(14.0, properties.Pitch, Tol);
            Assert.AreEqual("HTD 14M pulley", properties.Type);

            properties.Teeth = 1;
            Assert.AreEqual(HtdPulleyComponent.AbsoluteMinimumTeeth, pulley.Teeth, "Too few grooves are raised to the smallest accepted number.");
        }

        #endregion

        #region Save, load and export

        [Test]
        public void TestEditor_SaveAndLoad_RestoreTheTrain()
        {
            var presenter = NewPresenter(out _);
            var pinion = (InvoluteGearComponent)presenter.Selected;
            presenter.AddMate(pinion, MeshType.Rack);
            presenter.AddMeshedGear(pinion, 30);
            var path = TempFile(".json");
            try
            {
                Assert.IsTrue(presenter.Save(path));

                var other = new GearTrainPresenter(new FakeView());
                Assert.IsTrue(other.Load(path));

                Assert.AreEqual(presenter.Assembly.Shafts.Count, other.Assembly.Shafts.Count);
                Assert.AreEqual(presenter.Assembly.Relations.Count, other.Assembly.Relations.Count);
                Assert.IsNotNull(other.Scene);

                // New parts continue the numbering instead of repeating an identifier.
                var added = other.AddMeshedGear(other.Assembly.Components.OfType<InvoluteGearComponent>().First(), 25);
                Assert.AreEqual(1, other.Assembly.Components.Count(c => c.Id == added.Id));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void TestEditor_Load_AnInvalidFileIsReportedAndTheTrainStays()
        {
            var presenter = NewPresenter(out var view);
            var gears = presenter.Assembly.Components.Count();
            var path = TempFile(".json");
            try
            {
                File.WriteAllText(path, "this is not json");

                Assert.IsFalse(presenter.Load(path));

                Assert.IsTrue(view.IsError);
                Assert.AreEqual(gears, presenter.Assembly.Components.Count());
                Assert.IsFalse(presenter.Load(TempFile(".json")), "A missing file is reported as well.");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void TestEditor_Export_WritesStlAndObj()
        {
            var presenter = NewPresenter(out var view);
            presenter.AddMeshedGear((GearComponent)presenter.Selected, 30);
            var stl = TempFile(".stl");
            var obj = TempFile(".obj");
            try
            {
                Assert.IsTrue(presenter.Export(stl));
                Assert.IsTrue(presenter.Export(obj, true));

                StringAssert.StartsWith("solid", File.ReadAllText(stl));
                var objText = File.ReadAllText(obj);
                StringAssert.Contains("o Gear_1", objText);
                StringAssert.Contains("o Shaft_1", objText);
                Assert.IsFalse(view.IsError, view.Message);
            }
            finally
            {
                if (File.Exists(stl)) File.Delete(stl);
                if (File.Exists(obj)) File.Delete(obj);
            }
        }

        [Test]
        public void TestEditor_Export_WithoutASolvedSceneIsRefused()
        {
            var view = new FakeView();
            var presenter = new GearTrainPresenter(view);

            Assert.IsFalse(presenter.Export(TempFile(".stl")));
            Assert.IsTrue(view.IsError);
        }

        [Test]
        public void TestEditor_Export_ToAnInvalidPathIsReported()
        {
            var presenter = NewPresenter(out var view);

            Assert.IsFalse(presenter.Export(Path.Combine(Path.GetTempPath(), "no_such_folder_" + Guid.NewGuid().ToString("N"), "x.stl")));
            Assert.IsTrue(view.IsError);
        }

        #endregion

        #region Animation

        [Test]
        public void TestEditor_Animation_TurnsTheInputShaftAndTheDrivenGears()
        {
            var presenter = NewPresenter(out var view);
            presenter.AddMeshedGear((GearComponent)presenter.Selected, 40, 0.0);
            var drivenBefore = presenter.Assembly.Shafts[1].RotationRad;

            presenter.AdvanceAnimation(0.5, 60.0);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(Math.PI, presenter.InputAngleRad, 1e-9, "Half a second at 60 rpm is half a turn.");
            Assert.AreEqual(drivenBefore - Math.PI / 2.0, presenter.Assembly.Shafts[1].RotationRad, 1e-9, "The 40 tooth gear turns by -z1/z2 times as far.");
        }

        [Test]
        public void TestEditor_Animation_AlsoTurnsAPlanetaryStage()
        {
            var presenter = NewPresenter(out var view);
            presenter.NewPlanetary();

            presenter.AdvanceAnimation(0.25, 60.0);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(Math.PI / 2.0, presenter.InputAngleRad, 1e-9);
        }

        #endregion
    }
}
