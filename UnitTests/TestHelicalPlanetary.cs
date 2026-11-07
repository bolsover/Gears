using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>A planetary stage can be made of helical or herringbone gears.</summary>
    [TestFixture]
    public class TestHelicalPlanetary
    {
        private const double Tol = 1e-6;

        private static PlanetaryGearSet Set(double helixAngleDeg, bool herringbone = false, string direction = "R")
        {
            return new PlanetaryGearSet("PS")
            {
                HelixAngleDeg = helixAngleDeg,
                IsHerringbone = herringbone,
                HelicalDirection = direction
            };
        }

        private static string Key(double x, double y, double z)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:F5}|{1:F5}|{2:F5}", x, y, z);
        }

        private static HashSet<string> VertexKeys(GearMesh3DBuilder.Mesh3D mesh, bool mirrorZ)
        {
            return new HashSet<string>(mesh.Vertices.Select(v => Key(v.X, v.Y, mirrorZ ? -v.Z : v.Z)));
        }

        [Test]
        public void TestPlanetary_ByDefault_IsMadeOfSpurGears()
        {
            var set = new PlanetaryGearSet("PS");
            set.BuildAssembly();

            Assert.IsFalse(set.IsHelical);
            foreach (var gear in new[] { set.Sun, set.Ring }.Concat(set.Planets))
            {
                Assert.AreEqual(0.0, gear.HelixAngleDeg, Tol);
                Assert.IsFalse(gear.IsHelical);
                Assert.IsFalse(gear.IsHerringbone);
            }
        }

        [Test]
        public void TestPlanetary_Helical_PlanetsHaveTheOppositeHandAndTheRingTheHandOfThePlanets()
        {
            foreach (var sunHand in new[] { "R", "L" })
            {
                var set = Set(20.0, false, sunHand);
                var opposite = sunHand == "R" ? "L" : "R";
                set.BuildAssembly();

                Assert.AreEqual(sunHand, set.Sun.HelicalDirection);
                Assert.AreEqual(opposite, set.Ring.HelicalDirection, "An internal gear has the hand of the gear that meshes with it.");
                Assert.AreEqual(3, set.Planets.Count);
                foreach (var planet in set.Planets) Assert.AreEqual(opposite, planet.HelicalDirection);
                foreach (var gear in new[] { set.Sun, set.Ring }.Concat(set.Planets))
                {
                    Assert.AreEqual(20.0, gear.HelixAngleDeg, Tol);
                    Assert.IsTrue(gear.IsHelical);
                    Assert.IsFalse(gear.IsHerringbone);
                }

                Assert.AreEqual("Helical gear", set.Sun.TypeName);
            }
        }

        [Test]
        public void TestPlanetary_Herringbone_AllGearsAreHerringbone()
        {
            var set = Set(20.0, true);
            set.BuildAssembly();

            foreach (var gear in new[] { set.Sun, set.Ring }.Concat(set.Planets))
            {
                Assert.IsTrue(gear.IsHerringbone);
                var mesh = gear.BuildGeometry().Mesh;
                Assert.IsTrue(VertexKeys(mesh, false).SetEquals(VertexKeys(mesh, true)), gear.Id + " is a mirror image of itself about Z = 0.");
            }
        }

        [Test]
        public void TestPlanetary_HerringboneWithoutAHelixAngle_IsASpurStage()
        {
            var set = Set(0.0, true);
            set.BuildAssembly();

            foreach (var gear in new[] { set.Sun, set.Ring }.Concat(set.Planets))
            {
                Assert.IsFalse(gear.IsHerringbone, "The option is ignored without a helix angle.");
            }

            Assert.AreEqual("Spur gear", set.Sun.TypeName);
        }

        [Test]
        public void TestPlanetary_Helical_KeepsTheToothCountsAndTheRingRelation()
        {
            var spur = new PlanetaryGearSet("PS");
            var helical = Set(25.0, true, "L");

            var spurAssembly = spur.BuildAssembly();
            var helicalAssembly = helical.BuildAssembly();

            Assert.AreEqual(spurAssembly.Shafts.Count, helicalAssembly.Shafts.Count);
            Assert.AreEqual(spur.RingTeeth, helical.Ring.Teeth);
            Assert.AreEqual(helical.SunTeeth + 2 * helical.PlanetTeeth, helical.Ring.Teeth);
            for (var i = 0; i < 3; i++)
            {
                var spurShaft = spurAssembly.Shafts.Single(s => s.Id == $"PS.PlanetShaft{i + 1}");
                var helicalShaft = helicalAssembly.Shafts.Single(s => s.Id == $"PS.PlanetShaft{i + 1}");
                Assert.AreEqual(helical.CentreDistance, helicalShaft.Placement.Origin.Length, 1e-4, "Planets sit on the pitch circle of the carrier.");
                Assert.AreEqual(spur.CentreDistance, spurShaft.Placement.Origin.Length, 1e-4);
                Assert.AreEqual(spur.CentreDistance / Math.Cos(25.0 * Math.PI / 180.0), helicalShaft.Placement.Origin.Length, 1e-4, "The normal module gives a larger transverse centre distance.");
            }
        }

        [Test]
        public void TestPlanetary_Helical_BuildsAScene()
        {
            foreach (var herringbone in new[] { false, true })
            {
                var assembly = Set(20.0, herringbone).BuildAssembly();

                var scene = GearAssemblySceneBuilder.Build(assembly);

                Assert.AreEqual(5, scene.Items.Count);
                foreach (var item in scene.Items) Assert.Greater(item.Mesh.Vertices.Count, 0);
            }
        }

        [Test]
        public void TestPlanetary_Validate_RejectsABadHelixAngleOrDirection()
        {
            Assert.Throws<InvalidOperationException>(() => Set(-1.0).Validate());
            Assert.Throws<InvalidOperationException>(() => Set(90.0).Validate());
            Assert.Throws<InvalidOperationException>(() => Set(20.0, false, "X").Validate());
            Assert.DoesNotThrow(() => Set(20.0, true, "L").Validate());
        }

        [Test]
        public void TestPlanetary_HelicalStage_IsKeptWhenTheAssemblyIsSavedAndLoaded()
        {
            var assembly = Set(20.0, true, "L").BuildAssembly();

            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            var gears = loaded.Components.OfType<InvoluteGearComponent>().ToList();
            Assert.AreEqual(5, gears.Count);
            Assert.IsTrue(gears.All(g => g.IsHerringbone && Math.Abs(g.HelixAngleDeg - 20.0) < Tol));
            Assert.AreEqual("L", gears.Single(g => g.Id == "PS.Sun").HelicalDirection);
            Assert.AreEqual("R", gears.Single(g => g.Id == "PS.Ring").HelicalDirection);
        }

    }
}
