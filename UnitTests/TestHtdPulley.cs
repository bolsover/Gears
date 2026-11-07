using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>HTD timing belt pulleys: dimensions, groove geometry, 2D and 3D shapes and saving.</summary>
    [TestFixture]
    public class TestHtdPulley
    {
        private static HtdPulleyComponent Pulley(HtdSeries series, int teeth, bool flanges = false)
        {
            return new HtdPulleyComponent("P1") { Series = series, Teeth = teeth, HasFlanges = flanges };
        }

        private static double Radius(GearPoint p) => Math.Sqrt(p.X * p.X + p.Y * p.Y);

        /// <summary>Volume enclosed by a triangle mesh (divergence theorem); positive when the faces point outwards.</summary>
        private static double Volume(GearMesh3DBuilder.Mesh3D mesh)
        {
            var volume = 0.0;
            for (var i = 0; i + 2 < mesh.TriangleIndices.Count; i += 3)
            {
                var a = mesh.Vertices[mesh.TriangleIndices[i]];
                var b = mesh.Vertices[mesh.TriangleIndices[i + 1]];
                var c = mesh.Vertices[mesh.TriangleIndices[i + 2]];
                volume += (a.X * (b.Y * c.Z - b.Z * c.Y) - a.Y * (b.X * c.Z - b.Z * c.X) + a.Z * (b.X * c.Y - b.Y * c.X)) / 6.0;
            }

            return volume;
        }

        [TestCase(HtdSeries.Htd5M, 12, 19.10, 17.96)]
        [TestCase(HtdSeries.Htd5M, 20, 31.83, 30.69)]
        [TestCase(HtdSeries.Htd5M, 60, 95.49, 94.35)]
        [TestCase(HtdSeries.Htd3M, 22, 21.01, 20.25)]
        [TestCase(HtdSeries.Htd8M, 22, 56.02, 54.65)]
        [TestCase(HtdSeries.Htd14M, 70, 311.94, 309.15)]
        public void TestPulley_Diameters_MatchThePublishedTables(HtdSeries series, int teeth, double pitchDiameter, double outsideDiameter)
        {
            var pulley = Pulley(series, teeth);

            Assert.AreEqual(pitchDiameter, pulley.PitchDiameter, 0.01, "pitch diameter");
            Assert.AreEqual(outsideDiameter, pulley.OutsideDiameter, 0.01, "outside diameter");
            Assert.AreEqual(pulley.PitchDiameter - 2.0 * pulley.Profile.PitchLineDifferential, pulley.OutsideDiameter, 1e-9);
            Assert.AreEqual(pulley.OutsideDiameter - 2.0 * pulley.Profile.ToothHeight, pulley.RootDiameter, 1e-9);
        }

        [Test]
        public void TestPulley_Profiles_HaveThePublishedBeltDimensions()
        {
            Assert.AreEqual(4, HtdProfile.Profiles.Count);
            Assert.AreEqual(new[] { 3.0, 5.0, 8.0, 14.0 }, HtdProfile.Profiles.Select(p => p.Pitch).ToArray());

            var five = HtdProfile.Get(HtdSeries.Htd5M);
            Assert.AreEqual("5M", five.Name);
            Assert.AreEqual(2.06, five.ToothHeight, 1e-9);
            Assert.AreEqual(1.49, five.HeadRadius, 1e-9);
            Assert.AreEqual(0.5715, five.PitchLineDifferential, 1e-9);
            Assert.AreEqual(1.397, HtdProfile.Get(HtdSeries.Htd14M).PitchLineDifferential, 1e-9);
            Assert.AreEqual("HTD 8M pulley", Pulley(HtdSeries.Htd8M, 30).TypeName);
        }

        [Test]
        public void TestPulley_Outline_LiesBetweenTheRootAndOutsideCirclesAndHasOneBottomPerGroove()
        {
            foreach (var series in new[] { HtdSeries.Htd3M, HtdSeries.Htd5M, HtdSeries.Htd8M, HtdSeries.Htd14M })
            {
                var pulley = Pulley(series, 30);
                var outline = pulley.BuildOutline();
                var outside = pulley.OutsideDiameter / 2.0;
                var root = pulley.RootDiameter / 2.0;

                Assert.Greater(SolidMeshBuilder.SignedArea(outline), 0.0, series + ": the outline runs anticlockwise.");
                Assert.AreEqual(outside, outline.Max(Radius), 1e-9, series + ": the land is on the outside diameter.");
                Assert.AreEqual(root, outline.Min(Radius), 1e-9, series + ": the groove bottom is on the root diameter.");
                Assert.AreEqual(30, outline.Count(p => Math.Abs(Radius(p) - root) < 1e-9), series + ": one bottom per groove.");
            }
        }

        [Test]
        public void TestPulley_Outline_RepeatsEveryGroovePitchAngle()
        {
            var pulley = Pulley(HtdSeries.Htd5M, 24);
            var outline = pulley.BuildOutline();
            var step = 2.0 * Math.PI / 24;

            var perGroove = outline.Count / 24;
            Assert.AreEqual(0, outline.Count % 24);
            for (var i = 0; i < outline.Count; i++)
            {
                var a = outline[i];
                var b = outline[(i + perGroove) % outline.Count];
                var rotatedX = a.X * Math.Cos(step) - a.Y * Math.Sin(step);
                var rotatedY = a.X * Math.Sin(step) + a.Y * Math.Cos(step);
                Assert.AreEqual(rotatedX, b.X, 1e-9);
                Assert.AreEqual(rotatedY, b.Y, 1e-9);
            }
        }

        [Test]
        public void TestPulley_Groove_IsMirrorSymmetricAboutItsAxisAndAboutAsDeepAsTheBeltTooth()
        {
            var pulley = Pulley(HtdSeries.Htd8M, 36);
            var outline = pulley.BuildOutline();

            // Groove 0 is centred on +X: reflecting the outline in the X axis gives the same set of points.
            var keys = new HashSet<string>(outline.Select(p => $"{p.X:F6}|{p.Y:F6}"));
            foreach (var p in outline) Assert.IsTrue(keys.Contains($"{p.X:F6}|{-p.Y:F6}"), "The outline is symmetric about the groove axis.");

            var half = HtdPulleyComponent.HalfGroove(pulley.Profile);
            Assert.AreEqual(0.0, half[0].X, 1e-12);
            Assert.AreEqual(0.0, half[0].Y, 1e-12);
            Assert.AreEqual(pulley.Profile.ToothHeight, half[half.Count - 1].Y, 1e-12);
            Assert.Greater(half[half.Count - 1].X, pulley.Profile.HeadRadius * 0.9, "The groove is about as wide as its bottom arc.");
        }

        [TestCase(HtdSeries.Htd5M)]
        [TestCase(HtdSeries.Htd8M)]
        [TestCase(HtdSeries.Htd14M)]
        public void TestPulley_Groove_IsAsWideAtTheOutsideDiameterAsTheBeltTooth(HtdSeries series)
        {
            var profile = HtdProfile.Get(series);
            var half = HtdPulleyComponent.HalfGroove(profile);

            // The last point of the bottom arc (radius = head radius about (0, head radius)) is where the straight flank starts.
            var arcEnd = half.Last(p => Math.Abs(Math.Sqrt(p.X * p.X + (p.Y - profile.HeadRadius) * (p.Y - profile.HeadRadius)) - profile.HeadRadius) < 1e-9);
            var theta = (90.0 - HtdProfile.FlankAngleDeg / 2.0) * Math.PI / 180.0;
            var cornerU = arcEnd.X + (profile.ToothHeight - arcEnd.Y) * Math.Cos(theta) / Math.Sin(theta);

            Assert.AreEqual(profile.ToothWidth, 2.0 * cornerU, profile.ToothWidth * 0.05, "Width of the groove where its flanks reach the outside diameter.");
            Assert.Greater(half[half.Count - 1].X, cornerU, "The edge rounding ends beyond the sharp corner.");
        }

        [Test]
        public void TestPulley_Mesh_IsAClosedSolidWithTheWidthOfThePulley()
        {
            foreach (var series in new[] { HtdSeries.Htd3M, HtdSeries.Htd5M, HtdSeries.Htd14M })
            {
                var pulley = Pulley(series, 28);
                pulley.FaceWidth = 12.0;

                var geometry = pulley.BuildGeometry();
                var mesh = geometry.Mesh;

                Assert.AreEqual(6.0, mesh.Vertices.Max(v => v.Z), 1e-9);
                Assert.AreEqual(-6.0, mesh.Vertices.Min(v => v.Z), 1e-9);
                Assert.AreEqual(pulley.OutsideDiameter / 2.0, mesh.Vertices.Max(v => Math.Sqrt(v.X * v.X + v.Y * v.Y)), 1e-9);
                var expected = SolidMeshBuilder.SignedArea(pulley.BuildOutline()) * 12.0;
                Assert.AreEqual(expected, Volume(mesh), expected * 1e-6, series + ": the caps cover the whole outline.");
                Assert.AreEqual(1, geometry.Polylines.Count);
            }
        }

        [Test]
        public void TestPulley_Flanges_AreLargerDiscsOnBothSides()
        {
            var pulley = Pulley(HtdSeries.Htd5M, 24, true);
            pulley.FaceWidth = 15.0;
            pulley.FlangeHeight = 3.0;
            pulley.FlangeThickness = 2.0;

            var geometry = pulley.BuildGeometry();

            Assert.AreEqual(pulley.OutsideDiameter + 6.0, pulley.FlangeDiameter, 1e-9);
            Assert.AreEqual(9.5, geometry.Mesh.Vertices.Max(v => v.Z), 1e-9);
            Assert.AreEqual(-9.5, geometry.Mesh.Vertices.Min(v => v.Z), 1e-9);
            Assert.AreEqual(pulley.FlangeDiameter / 2.0, geometry.Mesh.Vertices.Max(v => Math.Sqrt(v.X * v.X + v.Y * v.Y)), 1e-9);
            Assert.AreEqual(2, geometry.Polylines.Count, "The flange circle is drawn in 2D as well.");
            pulley.GetBodyAxialExtent(out var start, out var end);
            Assert.AreEqual(-9.5, start, 1e-9);
            Assert.AreEqual(9.5, end, 1e-9);

            pulley.HasFlanges = false;
            Assert.AreEqual(1, pulley.BuildGeometry().Polylines.Count, "Changing a setting rebuilds the geometry.");
            Assert.AreEqual(7.5, pulley.BuildGeometry().Mesh.Vertices.Max(v => v.Z), 1e-9);
        }

        [Test]
        public void TestPulley_SmallAndLargeCountsOfGroovesBuild()
        {
            foreach (var profile in HtdProfile.Profiles)
            {
                foreach (var teeth in new[] { HtdPulleyComponent.AbsoluteMinimumTeeth, profile.MinimumTeeth, 80 })
                {
                    var pulley = Pulley(profile.Series, teeth);

                    Assert.DoesNotThrow(() => pulley.BuildGeometry(), profile.Name + " with " + teeth + " grooves");
                    Assert.AreEqual(teeth < profile.MinimumTeeth, pulley.IsBelowRecommendedTeeth);
                }
            }
        }

        [Test]
        public void TestPulley_Validate_RejectsBadSettings()
        {
            Assert.Throws<InvalidOperationException>(() => Pulley(HtdSeries.Htd5M, 5).Validate());
            var wide = Pulley(HtdSeries.Htd5M, 20);
            wide.FaceWidth = 0.0;
            Assert.Throws<InvalidOperationException>(() => wide.Validate());
            var flange = Pulley(HtdSeries.Htd5M, 20, true);
            flange.FlangeThickness = 0.0;
            Assert.Throws<InvalidOperationException>(() => flange.Validate());
            Assert.DoesNotThrow(() => Pulley(HtdSeries.Htd5M, 20).Validate());
        }

        [Test]
        public void TestPulley_ParameterTable_ListsTheHtdDimensions()
        {
            var pulley = Pulley(HtdSeries.Htd5M, 20);

            var table = GearParameterTables.ForGear(pulley);

            Assert.AreEqual(pulley.PitchDiameter, table.Rows.Single(r => r.ParameterName == "Pitch Diameter").Value, 1e-9);
            Assert.AreEqual(pulley.OutsideDiameter, table.Rows.Single(r => r.ParameterName == "Outside Diameter").Value, 1e-9);
            Assert.AreEqual(2.06, table.Rows.Single(r => r.ParameterName.StartsWith("Groove Depth")).Value, 1e-9);
            Assert.IsFalse(table.Rows.Any(r => r.ParameterName == "Flange Diameter"));
        }

        [Test]
        public void TestPulley_BuildsASceneForThe2DAnd3DViews()
        {
            var assembly = new GearAssembly();
            var shaft = new Shaft("S1");
            var pulley = Pulley(HtdSeries.Htd8M, 30, true);
            shaft.Mount(pulley, 0.0);
            assembly.AddShaft(shaft);

            var scene = GearAssemblySceneBuilder.Build(assembly);

            Assert.AreEqual(1, scene.Items.Count);
            var item = scene.Items[0];
            Assert.Greater(item.Mesh.Vertices.Count, 100);
            Assert.AreEqual(item.Mesh.Vertices.Count, item.Mesh.Normals.Count);
            Assert.AreEqual(2, item.Polylines.Count);
            Assert.AreEqual(pulley.FlangeDiameter / 2.0, item.Polylines[1].Max(Radius), 1e-6);
        }

        [Test]
        public void TestPulley_IsKeptWhenTheAssemblyIsSavedAndLoaded()
        {
            var assembly = new GearAssembly();
            var shaft = new Shaft("S1");
            var pulley = Pulley(HtdSeries.Htd14M, 40, true);
            pulley.FaceWidth = 42.0;
            pulley.FlangeHeight = 5.0;
            pulley.FlangeThickness = 3.0;
            pulley.Name = "Drive pulley";
            shaft.Mount(pulley, 4.0);
            assembly.AddShaft(shaft);

            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            var copy = loaded.Components.OfType<HtdPulleyComponent>().Single();
            Assert.AreEqual(HtdSeries.Htd14M, copy.Series);
            Assert.AreEqual(40, copy.Teeth);
            Assert.AreEqual(42.0, copy.FaceWidth, 1e-9);
            Assert.IsTrue(copy.HasFlanges);
            Assert.AreEqual(5.0, copy.FlangeHeight, 1e-9);
            Assert.AreEqual(3.0, copy.FlangeThickness, 1e-9);
            Assert.AreEqual("Drive pulley", copy.Name);
            Assert.AreEqual(pulley.PitchDiameter, copy.PitchDiameter, 1e-9);
            Assert.AreEqual(4.0, loaded.Shafts[0].Mounts[0].AxialOffset, 1e-9);
        }
    }
}
