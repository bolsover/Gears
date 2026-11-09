using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Gui;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>The 2D view of a gear assembly: parts drawn, fit bounds, hidden gears and picking.</summary>
    [TestFixture]
    public class TestGearScene2D
    {
        private InvoluteGearComponent _gearA;
        private InvoluteGearComponent _gearB;
        private Shaft _shaft1;
        private Shaft _shaft2;
        private AssemblyScene _scene;

        private static InvoluteGearComponent Gear(string id, int teeth)
        {
            return new InvoluteGearComponent(id)
            {
                Teeth = teeth,
                Module = 2.0,
                PressureAngleDeg = 20.0,
                Clearance = 0.5
            };
        }

        /// <summary>A(20 teeth) on shaft 1 at the origin drives B(40 teeth) on shaft 2 at X = 60 mm.</summary>
        [SetUp]
        public void SetUp()
        {
            _gearA = Gear("A", 20);
            _gearB = Gear("B", 40);
            _shaft1 = new Shaft("s1");
            _shaft1.Mount(_gearA, 0.0);
            _shaft2 = new Shaft("s2");
            _shaft2.Mount(_gearB, 0.0);

            var assembly = new GearAssembly();
            assembly.AddShaft(_shaft1);
            assembly.AddShaft(_shaft2);
            assembly.AddRelation(_gearA, _gearB);
            assembly.Solve();
            _scene = GearAssemblySceneBuilder.Build(assembly);
        }

        [Test]
        public void TestSetScene_DrawsEveryGearAndShaft()
        {
            using (var view = new GearScene2DControl())
            {
                view.SetScene(_scene, null, null, true);
                Assert.AreEqual(4, view.PartCount, "Two gears and two shafts.");

                view.SetScene(_scene, null, null, false);
                Assert.AreEqual(2, view.PartCount, "Without the shafts only the gears remain.");
            }
        }

        [Test]
        public void TestSetScene_SkipsHiddenGears()
        {
            using (var view = new GearScene2DControl())
            {
                view.SetScene(_scene, null, new HashSet<GearComponent> { _gearB }, false);

                Assert.AreEqual(1, view.PartCount);
                Assert.AreSame(_gearA, view.PickAtWorld(16.0, 0.0, 0.0));
                Assert.IsNull(view.PickAtWorld(92.0, 0.0, 0.0), "The hidden gear cannot be picked.");
            }
        }

        [Test]
        public void TestSetScene_AcceptsSeveralSelectedItems()
        {
            using (var view = new GearScene2DControl())
            {
                view.SetScene(_scene, new object[] { _gearA, _gearB }, null, true);

                Assert.AreEqual(4, view.PartCount, "Selecting parts does not change what is drawn.");
            }
        }

        [Test]
        public void TestSetScene_NullClearsTheView()
        {
            using (var view = new GearScene2DControl())
            {
                view.SetScene(_scene, null, null, true);
                view.SetScene(null, null, null, true);

                Assert.AreEqual(0, view.PartCount);
                Assert.IsFalse(view.TryGetWorldBounds(out _, out _, out _, out _));
            }
        }

        [Test]
        public void TestWorldBounds_EncloseBothGears()
        {
            using (var view = new GearScene2DControl())
            {
                view.SetScene(_scene, null, null, false);

                Assert.IsTrue(view.TryGetWorldBounds(out var minX, out var minY, out var maxX, out var maxY));
                Assert.LessOrEqual(minX, -20.0, "Gear A has a pitch radius of 20 mm around the origin.");
                Assert.GreaterOrEqual(maxX, 60.0 + 40.0, "Gear B has a pitch radius of 40 mm around X = 60 mm.");
                Assert.LessOrEqual(minY, -20.0);
                Assert.GreaterOrEqual(maxY, 40.0);
            }
        }

        [Test]
        public void TestFitView_ScalesTheTrainIntoTheControl()
        {
            using (var view = new GearScene2DControl { Size = new Size(600, 400) })
            {
                view.SetScene(_scene, null, null, true);
                view.FitView();

                view.TryGetWorldBounds(out var minX, out var minY, out var maxX, out var maxY);
                Assert.Greater(view.Scale, 0.0);
                Assert.LessOrEqual((maxX - minX) * view.Scale, 600.0, "The train fits the width.");
                Assert.LessOrEqual((maxY - minY) * view.Scale, 400.0, "The train fits the height.");
            }
        }

        [Test]
        public void TestPick_SelectsTheGearUnderThePoint()
        {
            using (var view = new GearScene2DControl())
            {
                view.SetScene(_scene, null, null, true);

                Assert.AreSame(_gearA, view.PickAtWorld(16.0, 0.0, 0.0), "Inside the body of gear A.");
                Assert.AreSame(_gearB, view.PickAtWorld(60.0 + 32.0, 0.0, 0.0), "Inside the body of gear B.");
                Assert.IsNull(view.PickAtWorld(30.0, 50.0, 0.0), "Empty space selects nothing.");
            }
        }

        [Test]
        public void TestPick_ToleranceSelectsANearbyOutline()
        {
            using (var view = new GearScene2DControl())
            {
                view.SetScene(_scene, null, null, false);

                // 3 mm beyond the tip circle of gear A (22 mm), on the side away from gear B: only picked when the tolerance reaches the outline.
                Assert.IsNull(view.PickAtWorld(-25.0, 0.0, 0.0));
                Assert.AreSame(_gearA, view.PickAtWorld(-25.0, 0.0, 4.0));
            }
        }

        [Test]
        public void TestOptions_CanBeToggledWithoutAScene()
        {
            using (var view = new GearScene2DControl())
            {
                view.ShowPitchCircles = false;
                view.ShowLabels = false;

                Assert.IsFalse(view.ShowPitchCircles);
                Assert.IsFalse(view.ShowLabels);
                Assert.IsNull(view.PickAtWorld(0.0, 0.0, 1.0));
            }
        }
    }
}
