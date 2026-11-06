using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator.Alibre;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Tests of the gear train export to an Alibre assembly: which gears are exported and where each part is located. These steps need no
    /// running Alibre; the Alibre calls themselves (CreateEmptyAssemblyEx, Occurrences.AddEmptyPart) are not covered here.
    /// </summary>
    [TestFixture]
    public class TestAlibreAssemblyExport
    {
        private static InvoluteGearComponent Gear(string id, int teeth = 20, double faceWidth = 10.0)
        {
            return new InvoluteGearComponent(id)
            {
                Teeth = teeth,
                Module = 2.0,
                PressureAngleDeg = 20.0,
                FaceWidth = faceWidth
            };
        }

        private static GearAssembly TwoGearTrain(out Shaft s1, out Shaft s2)
        {
            var assembly = new GearAssembly();
            s1 = assembly.AddShaft(new Shaft("s1"));
            s2 = assembly.AddShaft(new Shaft("s2"));
            s1.Mount(Gear("g1", 20, 10.0), 3.0);
            s2.Mount(Gear("g2", 30, 12.0), 5.0);
            return assembly;
        }

        [Test]
        public void GetAssemblyParts_ReturnsEveryGearInShaftOrder()
        {
            var assembly = TwoGearTrain(out _, out _);

            var parts = AlibreGearExporter.GetAssemblyParts(assembly);

            CollectionAssert.AreEqual(new[] { "g1", "g2" }, parts.Select(p => p.Key.Id).ToArray());
        }

        [Test]
        public void GetAssemblyParts_RequiresAtLeastTwoGears()
        {
            var assembly = new GearAssembly();
            assembly.AddShaft(new Shaft("s1")).Mount(Gear("g1"));

            var ex = Assert.Throws<InvalidOperationException>(() => AlibreGearExporter.GetAssemblyParts(assembly));
            StringAssert.Contains("at least 2", ex.Message);
            Assert.Throws<InvalidOperationException>(() => AlibreGearExporter.GetAssemblyParts(new GearAssembly()));
            Assert.Throws<ArgumentNullException>(() => AlibreGearExporter.GetAssemblyParts(null));
        }

        [Test]
        public void GetAssemblyParts_IncludesBevelGears()
        {
            var assembly = new GearAssembly();
            var s1 = assembly.AddShaft(new Shaft("s1"));
            s1.Mount(Gear("g1"));
            s1.Mount(new BevelGearComponent("b1"), 20.0);

            var parts = AlibreGearExporter.GetAssemblyParts(assembly);

            CollectionAssert.AreEqual(new[] { "g1", "b1" }, parts.Select(p => p.Key.Id).ToArray());
        }

        [Test]
        public void PartPlacement_StartsHalfAFaceWidthBelowTheMountOffsetAlongTheShaft()
        {
            var assembly = TwoGearTrain(out var s1, out _);
            var mount = s1.Mounts[0];

            var origin = AlibreGearExporter.GetPartPlacement(s1, mount).Translation;

            Assert.AreEqual(0.0, origin.X, 1e-9);
            Assert.AreEqual(0.0, origin.Y, 1e-9);
            Assert.AreEqual(3.0 - 10.0 / 2.0, origin.Z, 1e-9, "The part runs from z = 0 to its face width, the offset is its mid-plane.");
            Assert.IsNotNull(assembly);
        }

        [Test]
        public void PartPlacement_FollowsTheShaftOriginAndAxis()
        {
            var shaft = new Shaft("s", new Placement3D(new Vector3D(100.0, 0.0, 0.0), Vector3D.UnitX, Vector3D.UnitY));
            var gear = Gear("g", 20, 10.0);
            var mount = shaft.Mount(gear, 5.0);

            var transform = AlibreGearExporter.GetPartPlacement(shaft, mount);

            Assert.AreEqual(100.0, transform.Translation.X, 1e-9, "The part start is on the shaft axis (offset 5 - half face 5 = 0).");
            var axis = transform.TransformDirection(Vector3D.UnitZ);
            Assert.AreEqual(1.0, axis.X, 1e-9, "The part axis (local Z) is the shaft axis.");
        }

        [Test]
        public void AlibreMatrix_HasTheDocumentedLayoutInCentimetres()
        {
            var shaft = new Shaft("s", new Placement3D(new Vector3D(100.0, 20.0, 0.0), Vector3D.UnitX, Vector3D.UnitY));
            var transform = shaft.Placement.ToTransform();

            var m = AlibreGearExporter.ToAlibreMatrix(transform);

            Assert.AreEqual(16, m.Length);
            // The frame of the shaft: X = world Y, Y = Z x X = world Z, Z = world X.
            CollectionAssert.AreEqual(new[] { 0.0, 1.0, 0.0, 0.0 }, new[] { m[0], m[1], m[2], m[3] });
            CollectionAssert.AreEqual(new[] { 0.0, 0.0, 1.0, 0.0 }, new[] { m[4], m[5], m[6], m[7] });
            CollectionAssert.AreEqual(new[] { 1.0, 0.0, 0.0, 0.0 }, new[] { m[8], m[9], m[10], m[11] });
            Assert.AreEqual(10.0, m[12], 1e-9, "100 mm is 10 cm.");
            Assert.AreEqual(2.0, m[13], 1e-9, "20 mm is 2 cm.");
            Assert.AreEqual(0.0, m[14], 1e-9);
            Assert.AreEqual(1.0, m[15]);
        }

        [Test]
        public void AlibreMatrix_OfTheIdentityIsTheIdentityMatrix()
        {
            var m = AlibreGearExporter.ToAlibreMatrix(Transform3D.Identity);

            CollectionAssert.AreEqual(new[] { 1.0, 0, 0, 0, 0, 1.0, 0, 0, 0, 0, 1.0, 0, 0, 0, 0, 1.0 }, m);
        }

        [Test]
        public void UniqueName_AddsASuffixToRepeatedNames()
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Assert.AreEqual("Gear", AlibreGearExporter.UniqueName("Gear", used));
            Assert.AreEqual("Gear_2", AlibreGearExporter.UniqueName("Gear", used));
            Assert.AreEqual("gear_3", AlibreGearExporter.UniqueName("gear", used), "Names are compared ignoring case; the given spelling is kept.");
            Assert.AreEqual("Other", AlibreGearExporter.UniqueName("Other", used));
        }

        [Test]
        public void ShaftPartSketch_IsACircleOfTheShaftRadiusExtrudedToItsLengthInCentimetres()
        {
            var shaft = new Shaft("s1") { Diameter = 12.0, Length = 50.0 };
            shaft.Mount(Gear("g1"), 0.0);

            var plan = ShaftPartSketchBuilder.Build(shaft);

            Assert.AreEqual(0.6, plan.Radius, 1e-9, "6 mm radius is 0.6 cm.");
            Assert.AreEqual(5.0, plan.Length, 1e-9, "50 mm is 5 cm.");
            Assert.IsFalse(plan.IsCarrier);
            StringAssert.Contains("s1", plan.PartName);
        }

        [Test]
        public void ShaftPartSketch_RejectsAShaftWithoutSize()
        {
            Assert.Throws<InvalidOperationException>(() => ShaftPartSketchBuilder.Build(new Shaft("s") { Diameter = 0.0 }));
            Assert.Throws<ArgumentNullException>(() => ShaftPartSketchBuilder.Build(null));
        }

        [Test]
        public void PlanetaryStage_CarrierIsRecognisedAndSizedAsAPlate()
        {
            var stage = new PlanetaryGearSet("P");
            var assembly = stage.BuildAssembly();

            var carriers = assembly.Shafts.Where(PlanetaryGearSet.IsCarrier).ToList();

            Assert.AreEqual(1, carriers.Count);
            var plan = ShaftPartSketchBuilder.Build(carriers[0]);
            Assert.IsTrue(plan.IsCarrier);
            Assert.AreEqual(carriers[0].Diameter / 2.0 * 0.1, plan.Radius, 1e-9);
            Assert.AreEqual(carriers[0].Length * 0.1, plan.Length, 1e-9, "The carrier plate is as thick as its length.");
            Assert.IsFalse(assembly.Shafts.Where(s => !ReferenceEquals(s, carriers[0])).Any(PlanetaryGearSet.IsCarrier));
        }

        [Test]
        public void GetShaftAssemblyParts_FollowsTheOptions()
        {
            var assembly = new PlanetaryGearSet("P").BuildAssembly();
            var carrier = assembly.Shafts.Single(PlanetaryGearSet.IsCarrier);
            // The shaft of the internal ring gear is not drawn (it has an open bore), so it has no part either.
            var ringShaft = assembly.Shafts.Single(s => s.Mounts.Any(m => m.Component is InvoluteGearComponent g && g.IsInternal));
            var gearShafts = assembly.Shafts.Count - 2;

            Assert.AreEqual(0, AlibreGearExporter.GetShaftAssemblyParts(assembly, null).Count);
            Assert.AreEqual(0, AlibreGearExporter.GetShaftAssemblyParts(assembly, new AssemblyExportOptions()).Count);

            var carriersOnly = AlibreGearExporter.GetShaftAssemblyParts(assembly, new AssemblyExportOptions { IncludeCarriers = true });
            Assert.AreEqual(1, carriersOnly.Count);
            Assert.AreSame(carrier, carriersOnly[0].Key);

            var shaftsOnly = AlibreGearExporter.GetShaftAssemblyParts(assembly, new AssemblyExportOptions { IncludeShafts = true });
            Assert.AreEqual(gearShafts, shaftsOnly.Count);
            CollectionAssert.DoesNotContain(shaftsOnly.Select(p => p.Key).ToList(), carrier);
            CollectionAssert.DoesNotContain(shaftsOnly.Select(p => p.Key).ToList(), ringShaft);

            var both = AlibreGearExporter.GetShaftAssemblyParts(assembly, new AssemblyExportOptions { IncludeShafts = true, IncludeCarriers = true });
            Assert.AreEqual(gearShafts + 1, both.Count);
        }

        [Test]
        public void GetShaftAssemblyParts_SkipsAShaftThatOnlyCarriesARack()
        {
            var assembly = new GearAssembly();
            assembly.AddShaft(new Shaft("rackCarrier")).Mount(new RackComponent("r"));
            assembly.AddShaft(new Shaft("s1")).Mount(Gear("g1"));

            var parts = AlibreGearExporter.GetShaftAssemblyParts(assembly, new AssemblyExportOptions { IncludeShafts = true });

            CollectionAssert.AreEqual(new[] { "s1" }, parts.Select(p => p.Key.Id).ToArray());
        }

        [Test]
        public void ShaftPlacement_StartsAtTheStartOfTheShaftAlongItsAxis()
        {
            var shaft = new Shaft("s", new Placement3D(new Vector3D(10.0, 0.0, 0.0))) { Length = 40.0 };
            shaft.Mount(Gear("g", 20, 10.0), 5.0);
            shaft.GetAxialSpan(out var start, out _);

            var origin = AlibreGearExporter.GetShaftPlacement(shaft).Translation;

            Assert.AreEqual(10.0, origin.X, 1e-9);
            Assert.AreEqual(start, origin.Z, 1e-9, "The part runs from z = 0 to its length, the span starts at the shaft start.");
        }

        [Test]
        public void IsExportable_CoversTheSupportedFamilies()
        {
            Assert.IsTrue(AlibreGearExporter.IsExportable(Gear("g")));
            Assert.IsTrue(AlibreGearExporter.IsExportable(new RackComponent("r")));
            Assert.IsTrue(AlibreGearExporter.IsExportable(new WormComponent("w")));
            Assert.IsTrue(AlibreGearExporter.IsExportable(new WormWheelComponent("ww")));
            Assert.IsTrue(AlibreGearExporter.IsExportable(new BevelGearComponent("b")));
        }
    }
}
