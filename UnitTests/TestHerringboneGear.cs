using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Alibre;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>A herringbone gear is a helical gear mirrored about one face, so that the teeth form a V along the face width.</summary>
    [TestFixture]
    public class TestHerringboneGear
    {
        private const double Tol = 1e-9;

        private static InvoluteGearComponent Gear(bool herringbone, double helixAngleDeg = 25.0, string direction = "R")
        {
            return new InvoluteGearComponent("G1")
            {
                Teeth = 12,
                Module = 2.0,
                PressureAngleDeg = 20.0,
                FaceWidth = 20.0,
                HelixAngleDeg = helixAngleDeg,
                HelicalDirection = direction,
                IsHerringbone = herringbone
            };
        }

        private static string Key(double x, double y, double z)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:F6}|{1:F6}|{2:F6}", x, y, z);
        }

        private static HashSet<string> VertexKeys(GearMesh3DBuilder.Mesh3D mesh, bool mirrorZ)
        {
            return new HashSet<string>(mesh.Vertices.Select(v => Key(v.X, v.Y, mirrorZ ? -v.Z : v.Z)));
        }

        [Test]
        public void TestHerringbone_Mesh_IsSymmetricAboutTheMiddlePlane()
        {
            var mesh = Gear(true).BuildGeometry().Mesh;

            Assert.Greater(mesh.Vertices.Count, 0);
            Assert.IsTrue(VertexKeys(mesh, false).SetEquals(VertexKeys(mesh, true)), "Every vertex has a mirror image on the other side of Z = 0.");
        }

        [Test]
        public void TestHelical_Mesh_IsNotSymmetricAboutTheMiddlePlane()
        {
            var mesh = Gear(false).BuildGeometry().Mesh;

            Assert.IsFalse(VertexKeys(mesh, false).SetEquals(VertexKeys(mesh, true)), "A plain helical gear is not a mirror image of itself.");
        }

        [Test]
        public void TestHerringbone_EachHalfIsTheHelicalGearOfHalfTheWidth()
        {
            var herringbone = Gear(true).BuildGeometry().Mesh;
            var helical = Gear(false).BuildGeometry().Mesh;

            // On the middle plane the teeth are not turned; at an end face they are turned by the helix over half the face width,
            // exactly as the helical gear is turned at its end face.
            var helicalFront = new HashSet<string>(helical.Vertices.Where(v => Math.Abs(v.Z - 10.0) < Tol).Select(v => Key(v.X, v.Y, v.Z)));
            var herringboneFront = new HashSet<string>(herringbone.Vertices.Where(v => Math.Abs(v.Z - 10.0) < Tol).Select(v => Key(v.X, v.Y, v.Z)));
            Assert.IsTrue(helicalFront.SetEquals(herringboneFront), "The front end face is the same as that of the helical gear.");

            var herringboneBack = new HashSet<string>(herringbone.Vertices.Where(v => Math.Abs(v.Z + 10.0) < Tol).Select(v => Key(v.X, v.Y, -v.Z)));
            Assert.IsTrue(herringboneFront.SetEquals(herringboneBack), "The back end face is turned like the front one.");

            // The helical gear passes through the middle plane untwisted as well, so its middle slice is the herringbone's middle slice.
            var middle = new HashSet<string>(herringbone.Vertices.Where(v => Math.Abs(v.Z) < Tol).Select(v => Key(v.X, v.Y, v.Z)));
            var helicalMiddle = new HashSet<string>(helical.Vertices.Where(v => Math.Abs(v.Z) < Tol).Select(v => Key(v.X, v.Y, v.Z)));
            Assert.Greater(middle.Count, 0, "A slice lies on the middle plane.");
            Assert.IsTrue(helicalMiddle.SetEquals(middle), "The middle slice is the untwisted section.");
        }

        [Test]
        public void TestHerringbone_KeepsTheFaceWidthAndHasFlatEndFaces()
        {
            var mesh = Gear(true).BuildGeometry().Mesh;

            Assert.AreEqual(10.0, mesh.Vertices.Max(v => v.Z), Tol);
            Assert.AreEqual(-10.0, mesh.Vertices.Min(v => v.Z), Tol);
            Assert.AreEqual(mesh.Vertices.Count, mesh.Normals.Count);
        }

        [Test]
        public void TestHerringbone_LeftHand_IsAlsoSymmetric_AndDiffersFromRightHand()
        {
            var left = Gear(true, 25.0, "L").BuildGeometry().Mesh;
            var right = Gear(true, 25.0, "R").BuildGeometry().Mesh;

            Assert.IsTrue(VertexKeys(left, false).SetEquals(VertexKeys(left, true)));
            Assert.IsFalse(VertexKeys(left, false).SetEquals(VertexKeys(right, false)));
        }

        [Test]
        public void TestHerringbone_WithoutHelixAngle_IsASpurGear()
        {
            var gear = Gear(true, 0.0);

            Assert.AreEqual("Spur gear", gear.TypeName);
            var spur = Gear(false, 0.0).BuildGeometry().Mesh;
            Assert.IsTrue(VertexKeys(gear.BuildGeometry().Mesh, false).SetEquals(VertexKeys(spur, false)));
        }

        [Test]
        public void TestHerringbone_TypeName()
        {
            Assert.AreEqual("Herringbone gear", Gear(true).TypeName);
            Assert.AreEqual("Helical gear", Gear(false).TypeName);
        }

        [Test]
        public void TestHerringbone_InternalGear_IsSymmetricAboutTheMiddlePlane()
        {
            var gear = Gear(true);
            gear.IsInternal = true;
            gear.Teeth = 30;

            var mesh = gear.BuildGeometry().Mesh;

            Assert.AreEqual("Internal gear", gear.TypeName);
            Assert.IsTrue(VertexKeys(mesh, false).SetEquals(VertexKeys(mesh, true)));
        }

        [Test]
        public void TestHerringbone_IsKeptWhenTheAssemblyIsSavedAndLoaded()
        {
            var assembly = new GearAssembly();
            var shaft = new Shaft("s1");
            shaft.Mount(Gear(true), 0.0);
            assembly.AddShaft(shaft);
            var plain = new Shaft("s2");
            plain.Mount(new InvoluteGearComponent("G2") { Teeth = 20, HelixAngleDeg = 15.0 }, 0.0);
            assembly.AddShaft(plain);

            var json = AssemblySerializer.ToJson(assembly);
            var loaded = AssemblySerializer.FromJson(json);

            var gears = loaded.Shafts.SelectMany(s => s.Mounts).Select(m => m.Component).OfType<InvoluteGearComponent>().ToList();
            Assert.IsTrue(gears.Single(g => g.Id == "G1").IsHerringbone);
            Assert.IsFalse(gears.Single(g => g.Id == "G2").IsHerringbone);
        }

        [Test]
        public void TestAlibreExport_Herringbone_SweepsHalfTheFaceWidthAndIsMirrored()
        {
            var herringbone = SingleToothSketchBuilder.Build(Gear(true));
            var helical = SingleToothSketchBuilder.Build(Gear(false));

            Assert.IsTrue(herringbone.IsHerringbone);
            Assert.IsTrue(herringbone.IsHelical);
            Assert.IsFalse(helical.IsHerringbone);
            Assert.AreEqual(helical.Helix.Height / 2.0, herringbone.Helix.Height, 1e-9, "Each half has half the face width.");
            Assert.AreEqual(helical.Helix.Lead, herringbone.Helix.Lead, 1e-9, "The helix angle is the same.");
            Assert.AreEqual(helical.Helix.Revolutions / 2.0, herringbone.Helix.Revolutions, 1e-9);
            Assert.AreEqual(helical.Helix.RightHand, herringbone.Helix.RightHand);
            Assert.AreEqual(1.0, herringbone.Helix.Height, 1e-9, "Half of the 20 mm face width is 1 cm in Alibre units.");
            Assert.AreEqual(2.0, helical.Helix.Height, 1e-9, "A plain helical gear is swept over the whole 20 mm.");
        }

        [Test]
        public void TestAlibreExport_Herringbone_PartName()
        {
            Assert.AreEqual("HerringboneGear_Z12_M2_B25R", SingleToothSketchBuilder.Build(Gear(true)).PartName);
            Assert.AreEqual("HelicalGear_Z12_M2_B25R", SingleToothSketchBuilder.Build(Gear(false)).PartName);

            var internalGear = Gear(true);
            internalGear.IsInternal = true;
            internalGear.Teeth = 30;
            Assert.AreEqual("InternalHerringboneGear_Z30_M2_B25R", SingleToothSketchBuilder.PartName(internalGear));
        }

        [Test]
        public void TestAlibreExport_HerringboneWithoutHelixAngle_IsAnOrdinarySpurExport()
        {
            var sketch = SingleToothSketchBuilder.Build(Gear(true, 0.0));

            Assert.IsFalse(sketch.IsHerringbone);
            Assert.IsFalse(sketch.IsHelical);
            Assert.AreEqual("SpurGear_Z12_M2", sketch.PartName);
        }

        [Test]
        public void TestAlibreExport_Herringbone_PartIsCentredOnTheMountOffset()
        {
            var shaft = new Shaft("s1");
            var herringbone = shaft.Mount(Gear(true), 3.0);
            var helical = new Shaft("s2").Mount(Gear(false), 3.0);

            var herringbonePlacement = AlibreGearExporter.GetPartPlacement(shaft, herringbone).Translation;
            var helicalPlacement = AlibreGearExporter.GetPartPlacement(new Shaft("s3"), helical).Translation;

            Assert.AreEqual(3.0, herringbonePlacement.Z, 1e-9, "The mirrored part already lies on the mid-plane, which is the mount offset.");
            Assert.AreEqual(3.0 - 10.0, helicalPlacement.Z, 1e-9, "A helical part starts at its sketch plane, half a face width below the mid-plane.");
            Assert.IsTrue(AlibreGearExporter.IsHerringbone(herringbone.Component));
            Assert.IsFalse(AlibreGearExporter.IsHerringbone(helical.Component));
        }

        private static RackComponent Rack(bool herringbone, double helixAngleDeg = 25.0, string direction = "R")
        {
            return new RackComponent("R1")
            {
                Teeth = 3,
                Module = 2.0,
                PressureAngleDeg = 20.0,
                FaceWidth = 20.0,
                HelixAngleDeg = helixAngleDeg,
                HelicalDirection = direction,
                IsHerringbone = herringbone
            };
        }

        [Test]
        public void TestHerringboneRack_Mesh_IsSymmetricAboutTheMiddlePlane()
        {
            var herringbone = Rack(true).BuildGeometry().Mesh;
            var helical = Rack(false).BuildGeometry().Mesh;

            Assert.AreEqual("Herringbone rack", Rack(true).TypeName);
            Assert.AreEqual("Helical rack", Rack(false).TypeName);
            Assert.IsTrue(VertexKeys(herringbone, false).SetEquals(VertexKeys(herringbone, true)), "Every vertex has a mirror image on the other side of Z = 0.");
            Assert.IsFalse(VertexKeys(helical, false).SetEquals(VertexKeys(helical, true)), "A plain helical rack is not a mirror image of itself.");
            Assert.AreEqual(10.0, herringbone.Vertices.Max(v => v.Z), Tol);
            Assert.AreEqual(-10.0, herringbone.Vertices.Min(v => v.Z), Tol);
            Assert.AreEqual(herringbone.Vertices.Count, herringbone.Normals.Count);

            var helicalFront = new HashSet<string>(helical.Vertices.Where(v => Math.Abs(v.Z - 10.0) < Tol).Select(v => Key(v.X, v.Y, v.Z)));
            var herringboneFront = new HashSet<string>(herringbone.Vertices.Where(v => Math.Abs(v.Z - 10.0) < Tol).Select(v => Key(v.X, v.Y, v.Z)));
            Assert.IsTrue(helicalFront.SetEquals(herringboneFront), "The front end face is that of the helical rack.");
        }

        [Test]
        public void TestHerringboneRack_WithoutHelixAngle_IsAStraightRack()
        {
            var rack = Rack(true, 0.0);

            Assert.AreEqual("Rack", rack.TypeName);
            Assert.IsTrue(VertexKeys(rack.BuildGeometry().Mesh, false).SetEquals(VertexKeys(Rack(false, 0.0).BuildGeometry().Mesh, false)));
        }

        [Test]
        public void TestHerringboneRack_IsKeptWhenTheAssemblyIsSavedAndLoaded()
        {
            var assembly = new GearAssembly();
            var shaft = new Shaft("s1");
            shaft.Mount(Rack(true), 0.0);
            assembly.AddShaft(shaft);
            var plain = new Shaft("s2");
            plain.Mount(new RackComponent("R2") { HelixAngleDeg = 15.0 }, 0.0);
            assembly.AddShaft(plain);

            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            var racks = loaded.Shafts.SelectMany(s => s.Mounts).Select(m => m.Component).OfType<RackComponent>().ToList();
            Assert.IsTrue(racks.Single(r => r.Id == "R1").IsHerringbone);
            Assert.IsFalse(racks.Single(r => r.Id == "R2").IsHerringbone);
        }

        [Test]
        public void TestAlibreExport_HerringboneRack_SweepsHalfTheFaceWidthAndIsMirrored()
        {
            var herringbone = RackToothSketchBuilder.Build(Rack(true));
            var helical = RackToothSketchBuilder.Build(Rack(false));

            Assert.IsTrue(herringbone.IsHerringbone);
            Assert.IsFalse(helical.IsHerringbone);
            Assert.AreEqual(2.0, herringbone.ExtrusionDepth, 1e-9, "The rack is still 20 mm thick.");
            Assert.AreEqual(1.0, herringbone.SweepDepth, 1e-9, "Each half is swept over half of it.");
            Assert.AreEqual(2.0, helical.SweepDepth, 1e-9);
            Assert.AreEqual(helical.ShearTotal / 2.0, herringbone.ShearTotal, 1e-9, "The helix angle is the same.");
            Assert.AreEqual("HerringboneRack_Z3_M2_B25R", herringbone.PartName);
            Assert.AreEqual("HelicalRack_Z3_M2_B25R", helical.PartName);
        }

        [Test]
        public void TestAlibreExport_HerringboneRackWithoutHelixAngle_IsAnOrdinaryRack()
        {
            var plan = RackToothSketchBuilder.Build(Rack(true, 0.0));

            Assert.IsFalse(plan.IsHerringbone);
            Assert.AreEqual("Rack_Z3_M2", plan.PartName);
            Assert.AreEqual(plan.ExtrusionDepth, plan.SweepDepth, 1e-9);
        }

        [Test]
        public void TestAlibreExport_HerringboneRack_PartIsCentredOnTheMountOffset()
        {
            var shaft = new Shaft("s1");
            var herringbone = shaft.Mount(Rack(true), 3.0);
            var helical = new Shaft("s2").Mount(Rack(false), 3.0);

            Assert.AreEqual(3.0, AlibreGearExporter.GetPartPlacement(shaft, herringbone).Translation.Z, 1e-9);
            Assert.AreEqual(3.0 - 10.0, AlibreGearExporter.GetPartPlacement(new Shaft("s3"), helical).Translation.Z, 1e-9);
            Assert.IsTrue(AlibreGearExporter.IsHerringbone(herringbone.Component));
            Assert.IsFalse(AlibreGearExporter.IsHerringbone(helical.Component));
        }

        [Test]
        public void TestHerringbone_FileWithoutTheSetting_LoadsAsHelical()
        {
            var assembly = new GearAssembly();
            var shaft = new Shaft("s1");
            shaft.Mount(Gear(false), 0.0);
            assembly.AddShaft(shaft);

            var json = AssemblySerializer.ToJson(assembly);

            StringAssert.DoesNotContain("IsHerringbone", json);
            var loaded = (InvoluteGearComponent)AssemblySerializer.FromJson(json).Shafts[0].Mounts[0].Component;
            Assert.IsFalse(loaded.IsHerringbone);
        }
    }
}
