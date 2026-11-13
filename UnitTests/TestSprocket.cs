using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Alibre;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>ISO 606 B-series roller chain sprockets: chain data, tooth form, outline, 3D shape.</summary>
    [TestFixture]
    public class TestSprocket
    {
        private static readonly int[] TeethCounts = { ChainProfile.AbsoluteMinimumTeeth, 9, 12, 19, 30, 57, 100 };

        private static SprocketComponent Sprocket(ChainSeries series, int teeth, SprocketToothForm form = SprocketToothForm.Maximum)
        {
            return new SprocketComponent("SP1") { Series = series, Teeth = teeth, ToothForm = form };
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

        [Test]
        public void TestChainProfile_Table_HasTheTenIso606BSeriesChains()
        {
            Assert.AreEqual(10, ChainProfile.Profiles.Count);
            Assert.AreEqual(
                new[] { "06B", "08B", "10B", "12B", "16B", "20B", "24B", "28B", "32B", "40B" },
                ChainProfile.Profiles.Select(p => p.Name).ToArray());
            Assert.AreEqual(new[] { 9.525, 12.7, 15.875, 19.05, 25.4, 31.75, 38.1, 44.45, 50.8, 63.5 }, ChainProfile.Profiles.Select(p => p.Pitch).ToArray());

            var eight = ChainProfile.Get(ChainSeries.B08);
            Assert.AreEqual(8.51, eight.RollerDiameter, 1e-9);
            Assert.AreEqual(7.75, eight.InnerWidth, 1e-9);
            Assert.AreEqual(4.45, eight.PinDiameter, 1e-9);
            Assert.AreEqual("ISO 606 08B-1", eight.ToString());

            // Pitch is 1/16 inch per chain number, and every roller is smaller than the pitch.
            foreach (var profile in ChainProfile.Profiles)
            {
                var number = int.Parse(profile.Name.Substring(0, 2));
                Assert.AreEqual(number * 25.4 / 16.0, profile.Pitch, 0.01, profile.Name + ": pitch");
                Assert.Less(profile.RollerDiameter, profile.Pitch, profile.Name);
                Assert.Less(profile.InnerWidth, profile.Pitch, profile.Name);
                Assert.Less(profile.PinDiameter, profile.RollerDiameter, profile.Name);
                Assert.Greater(profile.PlateHeight, profile.RollerDiameter * 0.9, profile.Name);
            }
        }

        [TestCase(ChainSeries.B08, 19, 77.16)]
        [TestCase(ChainSeries.B08, 100, 404.32)]
        [TestCase(ChainSeries.B06, 25, 76.00)]
        public void TestSprocket_PitchDiameter_MatchesThePublishedTables(ChainSeries series, int teeth, double pitchDiameter)
        {
            var sprocket = Sprocket(series, teeth);

            Assert.AreEqual(pitchDiameter, sprocket.PitchDiameter, 0.02, "pitch diameter");
            Assert.AreEqual(sprocket.PitchDiameter - sprocket.Profile.RollerDiameter, sprocket.RootDiameter, 1e-9, "root diameter");
            Assert.AreEqual(pitchDiameter / 2.0, sprocket.RollerCentreRadius, 0.005);
            Assert.AreEqual("ISO 606 " + sprocket.Profile.Name + " sprocket", sprocket.TypeName);
        }

        [Test]
        public void TestChainProfile_ToothForm_FollowsTheIso606Formulae()
        {
            var profile = ChainProfile.Get(ChainSeries.B10);
            var d1 = profile.RollerDiameter;
            var p = profile.Pitch;
            const int z = 17;
            var d = p / Math.Sin(Math.PI / z);

            Assert.AreEqual(0.505 * d1 + 0.069 * Math.Pow(d1, 1.0 / 3.0), profile.SeatRadius(SprocketToothForm.Maximum), 1e-12);
            Assert.AreEqual(0.505 * d1, profile.SeatRadius(SprocketToothForm.Minimum), 1e-12);
            Assert.AreEqual((120.0 - 90.0 / z) * Math.PI / 180.0, ChainProfile.SeatAngle(z, SprocketToothForm.Maximum), 1e-12);
            Assert.AreEqual((140.0 - 90.0 / z) * Math.PI / 180.0, ChainProfile.SeatAngle(z, SprocketToothForm.Minimum), 1e-12);
            Assert.AreEqual(0.008 * d1 * (z * z + 180.0), profile.FlankRadius(z, SprocketToothForm.Maximum), 1e-9);
            Assert.AreEqual(0.12 * d1 * (z + 2.0), profile.FlankRadius(z, SprocketToothForm.Minimum), 1e-9);
            Assert.AreEqual(d + 1.25 * p - d1, profile.TipDiameter(z, SprocketToothForm.Maximum), 1e-9);
            Assert.AreEqual(d + p * (1.0 - 1.6 / z) - d1, profile.TipDiameter(z, SprocketToothForm.Minimum), 1e-9);
            Assert.Less(profile.TipDiameter(z, SprocketToothForm.Minimum), profile.TipDiameter(z, SprocketToothForm.Maximum));
        }

        [Test]
        public void TestChainProfile_ToothWidth_IsLessThanTheInnerWidth()
        {
            Assert.AreEqual(0.93 * 5.72, ChainProfile.Get(ChainSeries.B06).ToothWidth, 1e-9, "pitch up to 12.7 mm");
            Assert.AreEqual(0.93 * 7.75, ChainProfile.Get(ChainSeries.B08).ToothWidth, 1e-9, "12.7 mm is still the narrow factor");
            Assert.AreEqual(0.95 * 17.02, ChainProfile.Get(ChainSeries.B16).ToothWidth, 1e-9, "pitch above 12.7 mm");
            Assert.AreEqual(ChainProfile.Get(ChainSeries.B12).ToothWidth, Sprocket(ChainSeries.B12, 20).FaceWidth, 1e-9, "default width of the sprocket follows the series");

            var custom = Sprocket(ChainSeries.B08, 20);
            custom.FaceWidth = 9.0;
            custom.Series = ChainSeries.B12;
            Assert.AreEqual(9.0, custom.FaceWidth, 1e-9, "a width that was set stays");
        }

        [Test]
        public void TestSprocket_Outline_IsAnticlockwiseSymmetricAndHasOneSeatBottomPerTooth()
        {
            foreach (var profile in ChainProfile.Profiles)
            {
                foreach (var form in new[] { SprocketToothForm.Maximum, SprocketToothForm.Minimum })
                {
                    foreach (var teeth in new[] { 9, 19, 57 })
                    {
                        var label = profile.Name + " " + form + " z=" + teeth;
                        var sprocket = Sprocket(profile.Series, teeth, form);
                        var outline = sprocket.BuildOutline();
                        var bottom = sprocket.RollerCentreRadius - profile.SeatRadius(form);

                        Assert.Greater(SolidMeshBuilder.SignedArea(outline), 0.0, label + ": anticlockwise");
                        Assert.AreEqual(bottom, outline.Min(Radius), 1e-9, label + ": the seat bottom is the lowest point");
                        Assert.AreEqual(teeth, outline.Count(pt => Math.Abs(Radius(pt) - bottom) < 1e-9), label + ": one bottom per tooth");
                        Assert.LessOrEqual(outline.Max(Radius), sprocket.OutsideDiameter / 2.0 + 1e-9, label + ": within the tip circle");
                        if (!sprocket.HasPointedTeeth)
                        {
                            Assert.AreEqual(sprocket.OutsideDiameter / 2.0, outline.Max(Radius), 1e-9, label + ": the tips are on the tip circle");
                        }

                        // Gap 0 is centred on +X: reflecting the outline in the X axis gives the same set of points.
                        var keys = new HashSet<string>(outline.Select(pt => $"{pt.X:F6}|{pt.Y:F6}"));
                        foreach (var pt in outline) Assert.IsTrue(keys.Contains($"{pt.X:F6}|{-pt.Y:F6}"), label + ": symmetric about the gap axis");
                    }
                }
            }
        }

        [Test]
        public void TestSprocket_Outline_RepeatsEveryToothPitchAngle()
        {
            var sprocket = Sprocket(ChainSeries.B12, 24);
            var outline = sprocket.BuildOutline();
            var step = 2.0 * Math.PI / 24;
            var perTooth = outline.Count / 24;

            Assert.AreEqual(0, outline.Count % 24);
            for (var i = 0; i < outline.Count; i++)
            {
                var a = outline[i];
                var b = outline[(i + perTooth) % outline.Count];
                Assert.AreEqual(a.X * Math.Cos(step) - a.Y * Math.Sin(step), b.X, 1e-9);
                Assert.AreEqual(a.X * Math.Sin(step) + a.Y * Math.Cos(step), b.Y, 1e-9);
            }
        }

        [Test]
        public void TestSprocket_Gap_SeatsTheRollerOnTheSeatingArcAndNeverCutsIntoTheTeeth()
        {
            foreach (var profile in ChainProfile.Profiles)
            {
                foreach (var form in new[] { SprocketToothForm.Maximum, SprocketToothForm.Minimum })
                {
                    foreach (var teeth in TeethCounts)
                    {
                        var label = profile.Name + " " + form + " z=" + teeth;
                        var half = SprocketComponent.HalfGap(profile, teeth, form, out _);
                        var centreX = profile.PitchDiameter(teeth) / 2.0;
                        var seatRadius = profile.SeatRadius(form);

                        for (var i = 0; i <= SprocketComponent.SeatArcSteps; i++)
                        {
                            var dx = half[i].X - centreX;
                            Assert.AreEqual(seatRadius, Math.Sqrt(dx * dx + half[i].Y * half[i].Y), 1e-9, label + ": seating arc point " + i);
                        }

                        // The roller (diameter d1) fits in the gap: no point of the gap is closer to its centre than the roller radius.
                        foreach (var pt in half)
                        {
                            var dx = pt.X - centreX;
                            Assert.GreaterOrEqual(Math.Sqrt(dx * dx + pt.Y * pt.Y), profile.RollerDiameter / 2.0 - 1e-9, label + ": the roller does not cut into the tooth");
                        }

                        // The half gap rises from the seat bottom to the tip and stays on its own side of the gap.
                        Assert.AreEqual(0.0, half[0].Y, 1e-12, label);
                        for (var i = 1; i < half.Count; i++)
                        {
                            Assert.Greater(half[i].Y, 0.0, label);
                            Assert.Greater(half[i].X, half[0].X, label + ": point " + i + " is above the bottom");
                            Assert.LessOrEqual(Math.Atan2(half[i].Y, half[i].X), Math.PI / teeth + 1e-9, label + ": inside the tooth pitch");
                        }
                    }
                }
            }
        }

        [Test]
        public void TestSprocket_Flank_IsTangentToTheSeatingArc()
        {
            var profile = ChainProfile.Get(ChainSeries.B08);
            var half = SprocketComponent.HalfGap(profile, 19, SprocketToothForm.Maximum, out _);
            var end = SprocketComponent.SeatArcSteps;

            // Direction of the last seating chord and of the first flank chord differ by about the sum of half their arc steps.
            double Heading(int from) => Math.Atan2(half[from + 1].Y - half[from].Y, half[from + 1].X - half[from].X);
            var step = Math.Abs(Heading(end) - Heading(end - 1));
            var seatStep = ChainProfile.SeatAngle(19, SprocketToothForm.Maximum) / 2.0 / SprocketComponent.SeatArcSteps;
            Assert.Less(step, seatStep * 1.5, "The heading does not jump where the flank starts.");
        }

        [Test]
        public void TestSprocket_ManyTeethBuildAndTheTipsOfSmallSprocketsAreNotCut()
        {
            foreach (var profile in ChainProfile.Profiles)
            {
                foreach (var form in new[] { SprocketToothForm.Maximum, SprocketToothForm.Minimum })
                {
                    foreach (var teeth in TeethCounts)
                    {
                        var sprocket = Sprocket(profile.Series, teeth, form);
                        Assert.DoesNotThrow(() => sprocket.BuildGeometry(), profile.Name + " " + form + " z=" + teeth);
                        Assert.AreEqual(teeth < ChainProfile.RecommendedMinimumTeeth, sprocket.IsBelowRecommendedTeeth);
                    }
                }
            }
        }

        [Test]
        public void TestSprocket_Mesh_IsAClosedSolidWithTheWidthOfTheTeeth()
        {
            foreach (var series in new[] { ChainSeries.B06, ChainSeries.B10, ChainSeries.B24 })
            {
                var sprocket = Sprocket(series, 21);
                var geometry = sprocket.BuildGeometry();
                var mesh = geometry.Mesh;
                var width = sprocket.FaceWidth;

                Assert.AreEqual(width / 2.0, mesh.Vertices.Max(v => v.Z), 1e-9);
                Assert.AreEqual(-width / 2.0, mesh.Vertices.Min(v => v.Z), 1e-9);
                Assert.AreEqual(sprocket.BuildOutline().Max(Radius), mesh.Vertices.Max(v => Math.Sqrt(v.X * v.X + v.Y * v.Y)), 1e-9);
                var prism = SolidMeshBuilder.SignedArea(sprocket.BuildOutline()) * width;
                var tip = sprocket.OutsideDiameter / 2.0;
                Assert.IsTrue(sprocket.TrySideRelief(out var reliefStart));
                var upper = 2.0 * Math.PI * (tip * tip - reliefStart * reliefStart) / 2.0 * sprocket.SideDrop(tip, reliefStart) * 2.0;
                var volume = Volume(mesh);
                Assert.Less(volume, prism, series + ": the side relief removes material");
                Assert.Greater(volume, prism - upper, series + ": the side relief removes no more than the full ring above its start");
                Assert.AreEqual(1, geometry.Polylines.Count);
            }
        }

        [Test]
        public void TestSprocket_Mesh_HasTheSideReliefOfTheAlibreExport()
        {
            foreach (var series in new[] { ChainSeries.B06, ChainSeries.B08, ChainSeries.B16 })
            {
                var sprocket = Sprocket(series, 19);
                var mesh = sprocket.BuildGeometry().Mesh;
                var half = sprocket.FaceWidth / 2.0;
                var tip = sprocket.OutsideDiameter / 2.0;
                Assert.IsTrue(sprocket.TrySideRelief(out var start));

                foreach (var v in mesh.Vertices)
                {
                    var r = Math.Sqrt(v.X * v.X + v.Y * v.Y);
                    var limit = half - sprocket.SideDrop(r, start);
                    Assert.LessOrEqual(Math.Abs(v.Z), limit + 1e-6, series + ": a vertex at r = " + r + " is outside the relief");
                }

                var atTip = mesh.Vertices.Where(v => Math.Abs(Math.Sqrt(v.X * v.X + v.Y * v.Y) - tip) < 1e-6).Max(v => v.Z);
                Assert.AreEqual(half - sprocket.Profile.SideReliefWidth, atTip, 1e-6, series + ": the side is cut back by C at the tip circle");
                Assert.AreEqual(half, mesh.Vertices.Max(v => v.Z), 1e-9, series + ": the flat side face is kept");
                Assert.AreEqual(mesh.Vertices.Max(v => v.Z), -mesh.Vertices.Min(v => v.Z), 1e-9, series + ": the relief is mirrored");
            }
        }

        [Test]
        public void TestSprocket_BoreAndHub_AddRingsAndACylinder()
        {
            var sprocket = Sprocket(ChainSeries.B08, 21);
            sprocket.BoreDiameter = 20.0;
            sprocket.HubDiameter = 40.0;
            sprocket.HubLength = 12.0;

            var geometry = sprocket.BuildGeometry();
            var mesh = geometry.Mesh;
            var width = sprocket.FaceWidth;
            var cellSteps = Math.Max(2, (int)Math.Ceiling(64.0 / 21));
            var segments = 21 * cellSteps;
            double Disc(double radius) => 0.5 * segments * radius * radius * Math.Sin(2.0 * Math.PI / segments);

            Assert.IsTrue(sprocket.HasHub);
            Assert.AreEqual(-width / 2.0, mesh.Vertices.Min(v => v.Z), 1e-9);
            Assert.AreEqual(width / 2.0 + 12.0, mesh.Vertices.Max(v => v.Z), 1e-9);
            sprocket.GetBodyAxialExtent(out var start, out var end);
            Assert.AreEqual(-width / 2.0, start, 1e-9);
            Assert.AreEqual(width / 2.0 + 12.0, end, 1e-9);

            // The bore and the hub are inside the flat part of the side faces, so they change the volume of the relieved toothed part by their own volume.
            var toothed = Volume(Sprocket(ChainSeries.B08, 21).BuildGeometry().Mesh);
            var expected = toothed - Disc(10.0) * width + (Disc(20.0) - Disc(10.0)) * 12.0;
            Assert.AreEqual(expected, Volume(mesh), expected * 1e-4, "outline prism minus the bore plus the hub");
            Assert.AreEqual(3, geometry.Polylines.Count, "outline, hub and bore circles");
            Assert.AreEqual(10.0, mesh.Vertices.Where(v => Math.Abs(v.Z - width / 2.0) < 1e-9).Min(v => Math.Sqrt(v.X * v.X + v.Y * v.Y)), 1e-9);
        }

        [Test]
        public void TestSprocket_BoreWithoutHub_AndHubWithoutBore_AreClosedSolids()
        {
            var bored = Sprocket(ChainSeries.B10, 18);
            bored.BoreDiameter = 18.0;
            var width = bored.FaceWidth;
            var cellSteps = Math.Max(2, (int)Math.Ceiling(64.0 / 18));
            var segments = 18 * cellSteps;
            var disc = 0.5 * segments * 81.0 * Math.Sin(2.0 * Math.PI / segments);
            var toothed = Volume(Sprocket(ChainSeries.B10, 18).BuildGeometry().Mesh);
            Assert.IsFalse(bored.HasHub);
            Assert.AreEqual(toothed - disc * width, Volume(bored.BuildGeometry().Mesh), toothed * 1e-4);

            var hubbed = Sprocket(ChainSeries.B10, 18);
            hubbed.HubDiameter = 30.0;
            hubbed.HubLength = 10.0;
            Assert.IsTrue(hubbed.HasHub);
            var hubDisc = 0.5 * segments * 225.0 * Math.Sin(2.0 * Math.PI / segments);
            Assert.AreEqual(toothed + hubDisc * 10.0, Volume(hubbed.BuildGeometry().Mesh), toothed * 1e-4);
        }

        [Test]
        public void TestSprocket_ChangingASettingRebuildsTheGeometry()
        {
            var sprocket = Sprocket(ChainSeries.B08, 19);
            var first = sprocket.BuildGeometry();
            Assert.AreSame(first, sprocket.BuildGeometry(), "The geometry is cached.");

            sprocket.Teeth = 25;
            var second = sprocket.BuildGeometry();
            Assert.AreNotSame(first, second);
            Assert.Greater(second.Polylines[0].Count, first.Polylines[0].Count);

            sprocket.ToothForm = SprocketToothForm.Minimum;
            Assert.AreNotSame(second, sprocket.BuildGeometry());
        }

        [Test]
        public void TestSprocket_ParameterTable_ListsTheIso606Dimensions()
        {
            var sprocket = Sprocket(ChainSeries.B10, 17, SprocketToothForm.Minimum);

            var table = GearParameterTables.ForGear(sprocket);

            Assert.AreEqual(sprocket.PitchDiameter, table.Rows.Single(r => r.ParameterName == "Pitch Diameter").Value, 1e-9);
            Assert.AreEqual(sprocket.RootDiameter, table.Rows.Single(r => r.ParameterName == "Root Diameter").Value, 1e-9);
            Assert.AreEqual(sprocket.OutsideDiameter, table.Rows.Single(r => r.ParameterName.StartsWith("Tip Diameter")).Value, 1e-9);
            Assert.AreEqual(0.505 * 10.16, table.Rows.Single(r => r.ParameterName.StartsWith("Roller Seating Radius")).Value, 1e-9);
            Assert.AreEqual(15.875, table.Rows.Single(r => r.ParameterName == "Chain Pitch").Value, 1e-9);
            Assert.IsFalse(table.Rows.Any(r => r.ParameterName == "Hub Diameter"));
        }

        [Test]
        public void TestSprocket_BuildsASceneForThe2DAnd3DViews()
        {
            var assembly = new GearAssembly();
            var shaft = new Shaft("S1");
            var sprocket = Sprocket(ChainSeries.B12, 25);
            sprocket.BoreDiameter = 20.0;
            sprocket.HubDiameter = 36.0;
            sprocket.HubLength = 10.0;
            shaft.Mount(sprocket, 0.0);
            assembly.AddShaft(shaft);

            var scene = GearAssemblySceneBuilder.Build(assembly);

            Assert.AreEqual(1, scene.Items.Count);
            var item = scene.Items[0];
            Assert.Greater(item.Mesh.Vertices.Count, 100);
            Assert.AreEqual(item.Mesh.Vertices.Count, item.Mesh.Normals.Count);
            Assert.AreEqual(3, item.Polylines.Count);
            Assert.AreEqual(sprocket.BuildOutline().Max(Radius), item.Polylines[0].Max(Radius), 1e-6);
        }

        [Test]
        public void TestSprocket_IsKeptWhenTheAssemblyIsSavedAndLoaded()
        {
            var assembly = new GearAssembly();
            var shaft = new Shaft("S1");
            var sprocket = Sprocket(ChainSeries.B16, 23, SprocketToothForm.Minimum);
            sprocket.FaceWidth = 15.0;
            sprocket.BoreDiameter = 25.0;
            sprocket.HubDiameter = 50.0;
            sprocket.HubLength = 20.0;
            sprocket.Name = "Drive sprocket";
            shaft.Mount(sprocket, 4.0);
            assembly.AddShaft(shaft);

            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            var copy = loaded.Components.OfType<SprocketComponent>().Single();
            Assert.AreEqual(ChainSeries.B16, copy.Series);
            Assert.AreEqual(23, copy.Teeth);
            Assert.AreEqual(SprocketToothForm.Minimum, copy.ToothForm);
            Assert.AreEqual(15.0, copy.FaceWidth, 1e-9);
            Assert.AreEqual(25.0, copy.BoreDiameter, 1e-9);
            Assert.AreEqual(50.0, copy.HubDiameter, 1e-9);
            Assert.AreEqual(20.0, copy.HubLength, 1e-9);
            Assert.AreEqual("Drive sprocket", copy.Name);
            Assert.AreEqual(sprocket.PitchDiameter, copy.PitchDiameter, 1e-9);
            Assert.AreEqual(4.0, loaded.Shafts[0].Mounts[0].AxialOffset, 1e-9);
        }

        /// <summary>The area of the closed loop of sketch figures (in cm^2), found by chaining them end to end and sampling the arcs.</summary>
        private static double LoopArea(IReadOnlyList<ToothSketchFigure> figures)
        {
            var remaining = new List<ToothSketchFigure>(figures);
            var points = new List<double[]>();
            var current = remaining[0];
            remaining.RemoveAt(0);
            var cursor = new[] { current.StartX, current.StartY };
            var forward = true;
            while (true)
            {
                SamplePoints(current, forward, points);
                cursor = forward ? new[] { current.EndX, current.EndY } : new[] { current.StartX, current.StartY };
                var next = remaining.FirstOrDefault(f => Near(f.StartX, f.StartY, cursor) || Near(f.EndX, f.EndY, cursor));
                if (next == null) break;
                remaining.Remove(next);
                forward = Near(next.StartX, next.StartY, cursor);
                current = next;
            }

            Assert.AreEqual(0, remaining.Count, "The figures form one loop.");
            var area = 0.0;
            for (var i = 0; i < points.Count; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Count];
                area += a[0] * b[1] - b[0] * a[1];
            }

            return Math.Abs(area) / 2.0;
        }

        private static bool Near(double x, double y, double[] point) => Math.Abs(x - point[0]) < 1e-6 && Math.Abs(y - point[1]) < 1e-6;

        private static void SamplePoints(ToothSketchFigure figure, bool forward, List<double[]> points)
        {
            var startX = forward ? figure.StartX : figure.EndX;
            var startY = forward ? figure.StartY : figure.EndY;
            if (figure.Kind == ToothSketchFigureKind.Line)
            {
                points.Add(new[] { startX, startY });
                return;
            }

            // An arc is anticlockwise from its start to its end; walked backwards it is clockwise.
            var a0 = Math.Atan2(figure.StartY - figure.CentreY, figure.StartX - figure.CentreX);
            var a1 = Math.Atan2(figure.EndY - figure.CentreY, figure.EndX - figure.CentreX);
            var sweep = a1 - a0;
            while (sweep < 0.0) sweep += 2.0 * Math.PI;
            var radius = Math.Sqrt(Math.Pow(figure.StartX - figure.CentreX, 2) + Math.Pow(figure.StartY - figure.CentreY, 2));
            const int steps = 64;
            for (var i = 0; i < steps; i++)
            {
                var t = forward ? (double)i / steps : 1.0 - (double)i / steps;
                var angle = a0 + sweep * t;
                points.Add(new[] { figure.CentreX + radius * Math.Cos(angle), figure.CentreY + radius * Math.Sin(angle) });
            }
        }

        [Test]
        public void TestSprocketSketch_ToothCell_IsClosedAndEveryArcIsACircularArc()
        {
            foreach (var profile in ChainProfile.Profiles)
            {
                foreach (var form in new[] { SprocketToothForm.Maximum, SprocketToothForm.Minimum })
                {
                    foreach (var teeth in TeethCounts)
                    {
                        var label = profile.Name + " " + form + " z=" + teeth;
                        var sprocket = Sprocket(profile.Series, teeth, form);
                        var plan = SprocketSketchBuilder.Build(sprocket);

                        Assert.IsTrue(plan.Tooth.IsClosed(), label + ": the tooth cell is closed");
                        Assert.AreEqual(teeth, plan.Tooth.Teeth);
                        Assert.AreEqual(2.0 * Math.PI / teeth, plan.Tooth.PatternAngle, 1e-12);
                        Assert.AreEqual(sprocket.FaceWidth * SingleToothSketchBuilder.MmToAlibreUnits, plan.Tooth.ExtrusionDepth, 1e-6);
                        Assert.AreEqual(sprocket.HasPointedTeeth ? 5 : 7, plan.Tooth.Figures.Count, label + ": figures");
                        foreach (var figure in plan.Tooth.Figures.Where(f => f.Kind == ToothSketchFigureKind.Arc))
                        {
                            var start = Math.Sqrt(Math.Pow(figure.StartX - figure.CentreX, 2) + Math.Pow(figure.StartY - figure.CentreY, 2));
                            var end = Math.Sqrt(Math.Pow(figure.EndX - figure.CentreX, 2) + Math.Pow(figure.EndY - figure.CentreY, 2));
                            Assert.AreEqual(start, end, 1e-6, label + ": " + figure.Name + " has equal radii at both ends");
                        }
                    }
                }
            }
        }

        [Test]
        public void TestSprocketSketch_ToothCell_HasTheAreaOfOneToothOfTheOutline()
        {
            foreach (var profile in new[] { ChainProfile.Get(ChainSeries.B06), ChainProfile.Get(ChainSeries.B12), ChainProfile.Get(ChainSeries.B24) })
            {
                foreach (var teeth in new[] { 9, 17, 40 })
                {
                    var sprocket = Sprocket(profile.Series, teeth);
                    var plan = SprocketSketchBuilder.Build(sprocket);

                    var cellArea = LoopArea(plan.Tooth.Figures) / (SingleToothSketchBuilder.MmToAlibreUnits * SingleToothSketchBuilder.MmToAlibreUnits);
                    var outlineArea = SolidMeshBuilder.SignedArea(sprocket.BuildOutline());

                    Assert.AreEqual(outlineArea, cellArea * teeth, outlineArea * 0.005, profile.Name + " z=" + teeth + ": the cells tile the outline");
                }
            }
        }

        [Test]
        public void TestSprocketSketch_Seat_IsTheRollerCircleAndBottomsOnTheRootDiameter()
        {
            var sprocket = Sprocket(ChainSeries.B10, 19);
            var plan = SprocketSketchBuilder.Build(sprocket);
            var seat = plan.Tooth.Figures.Single(f => f.Name == "Seat");
            var unit = SingleToothSketchBuilder.MmToAlibreUnits;

            Assert.AreEqual(sprocket.RollerCentreRadius * unit, seat.CentreX, 1e-6, "The seat is centred on the pitch circle.");
            Assert.AreEqual(0.0, seat.CentreY, 1e-9);
            Assert.AreEqual(sprocket.Profile.SeatRadius(SprocketToothForm.Maximum) * unit, Math.Sqrt(Math.Pow(seat.StartX - seat.CentreX, 2) + Math.Pow(seat.StartY, 2)), 1e-6);
            Assert.AreEqual(sprocket.PitchDiameter / 2.0 * unit, plan.Tooth.ReferenceCircles.Single().Radius, 1e-6);
            Assert.AreEqual("Sprocket_10B_Z19_B9.17", plan.Tooth.PartName);
        }

        [Test]
        public void TestSprocketSketch_HubAndBore_AreDescribedInAlibreUnits()
        {
            var plain = SprocketSketchBuilder.Build(Sprocket(ChainSeries.B08, 19));
            Assert.IsFalse(plain.HasHub);
            Assert.IsFalse(plain.HasBore);
            Assert.IsTrue(plain.IsHubClosed());

            var sprocket = Sprocket(ChainSeries.B08, 19);
            sprocket.BoreDiameter = 20.0;
            sprocket.HubDiameter = 36.0;
            sprocket.HubLength = 10.0;
            var plan = SprocketSketchBuilder.Build(sprocket);
            var unit = SingleToothSketchBuilder.MmToAlibreUnits;

            Assert.IsTrue(plan.HasHub);
            Assert.IsTrue(plan.IsHubClosed());
            Assert.AreEqual(18.0 * unit, plan.Hub.Max(f => Math.Max(f.StartX, f.EndX)), 1e-6, "hub radius");
            var z0 = sprocket.FaceWidth / 2.0 * unit;
            Assert.AreEqual(z0, plan.Hub.Min(f => Math.Min(f.StartY, f.EndY)), 1e-6, "the hub starts at the face");
            Assert.AreEqual((sprocket.FaceWidth / 2.0 + 10.0) * unit, plan.Hub.Max(f => Math.Max(f.StartY, f.EndY)), 1e-6, "the hub ends 10 mm further");
            Assert.IsTrue(plan.HasBore);
            Assert.AreEqual(10.0 * unit, plan.BoreRadius, 1e-6);
            Assert.Greater(plan.BoreDepth, (sprocket.FaceWidth + 2.0 * 10.0) * unit, "the cut is deeper than the whole body, both ways from the mid-plane");
        }

        [Test]
        public void TestChainProfile_SideRelief_FollowsTheSprocketCatalogue()
        {
            // Tooth radius r3 and radius width C of the SIT sprocket catalogue (DIN 8187 / ISO 606).
            var expected = new (ChainSeries Series, double R3, double C)[]
            {
                (ChainSeries.B06, 10.0, 1.0), (ChainSeries.B08, 13.0, 1.3), (ChainSeries.B10, 16.0, 1.6), (ChainSeries.B12, 19.0, 2.0),
                (ChainSeries.B16, 26.0, 2.5), (ChainSeries.B20, 32.0, 3.5), (ChainSeries.B24, 38.0, 4.0), (ChainSeries.B28, 44.0, 5.0),
                (ChainSeries.B32, 51.0, 6.0)
            };

            foreach (var (series, r3, c) in expected)
            {
                var profile = ChainProfile.Get(series);
                Assert.AreEqual(r3, profile.SideRadius, 1e-9, series + " tooth radius r3");
                Assert.AreEqual(c, profile.SideReliefWidth, 1e-9, series + " radius width C");
            }
        }

        [Test]
        public void TestSprocketSketch_Relief_IsATangentArcThatCutsBackByTheRadiusWidthAtTheTip()
        {
            var sprocket = Sprocket(ChainSeries.B08, 19);
            var plan = SprocketSketchBuilder.Build(sprocket);
            var unit = SingleToothSketchBuilder.MmToAlibreUnits;
            var r3 = sprocket.Profile.SideRadius;
            var c = sprocket.Profile.SideReliefWidth;
            var halfWidth = sprocket.FaceWidth / 2.0;
            var tipRadius = sprocket.OutsideDiameter / 2.0;

            Assert.IsTrue(plan.HasRelief);
            Assert.IsTrue(plan.IsReliefClosed(), "The relief is one closed sketch.");
            Assert.AreEqual(4, plan.Relief.Count);

            var arc = plan.Relief.Single(f => f.Kind == ToothSketchFigureKind.Arc);
            Assert.AreEqual(r3 * unit, Math.Sqrt(Math.Pow(arc.StartX - arc.CentreX, 2) + Math.Pow(arc.StartY - arc.CentreY, 2)), 1e-6, "arc radius r3 (start)");
            Assert.AreEqual(r3 * unit, Math.Sqrt(Math.Pow(arc.EndX - arc.CentreX, 2) + Math.Pow(arc.EndY - arc.CentreY, 2)), 1e-6, "arc radius r3 (end)");

            // Anticlockwise from the tip end to the tangent point, which is straight above the centre: the arc is tangent to the side face.
            Assert.AreEqual(tipRadius * unit, arc.StartX, 1e-6, "the arc reaches the tip circle");
            Assert.AreEqual((halfWidth - c) * unit, arc.StartY, 1e-6, "the side is cut back by C at the tip circle");
            Assert.AreEqual(arc.CentreX, arc.EndX, 1e-6, "the tangent point is above the centre");
            Assert.AreEqual(halfWidth * unit, arc.EndY, 1e-6, "the arc is tangent to the tooth side face");
            Assert.AreEqual((halfWidth - r3) * unit, arc.CentreY, 1e-6, "the centre is r3 inside the tooth side face");
            Assert.AreEqual((tipRadius - Math.Sqrt(2.0 * r3 * c - c * c)) * unit, arc.EndX, 1e-6, "the relief starts at R0");

            var bottom = (sprocket.RollerCentreRadius - sprocket.Profile.SeatRadius(sprocket.ToothForm)) * unit;
            Assert.Greater(arc.EndX, bottom, "the relief starts above the bottom of the roller seats");
            Assert.LessOrEqual(plan.Relief.Max(f => Math.Max(f.StartY, f.EndY)), halfWidth * unit + 1e-9, "nothing is cut beyond the side face");
        }

        [Test]
        public void TestSprocketSketch_Relief_IsClosedForEverySeriesAndToothForm()
        {
            foreach (ChainSeries series in Enum.GetValues(typeof(ChainSeries)))
            {
                foreach (var form in new[] { SprocketToothForm.Maximum, SprocketToothForm.Minimum })
                {
                    foreach (var teeth in new[] { 9, 19, 40 })
                    {
                        var sprocket = Sprocket(series, teeth);
                        sprocket.ToothForm = form;
                        var plan = SprocketSketchBuilder.Build(sprocket);
                        var text = $"{series} z={teeth} {form}";
                        Assert.IsTrue(plan.HasRelief, text);
                        Assert.IsTrue(plan.IsReliefClosed(), text);
                    }
                }
            }
        }

        [Test]
        public void TestSprocketExport_IsExportableAndIsPlacedOnItsMidPlane()
        {
            var sprocket = Sprocket(ChainSeries.B08, 19);
            var shaft = new Shaft("S1");
            var mount = shaft.Mount(sprocket, 5.0);

            Assert.IsTrue(AlibreGearExporter.IsExportable(sprocket));

            var placement = AlibreGearExporter.GetPartPlacement(shaft, mount);
            var origin = placement.TransformPoint(new Vector3D(0.0, 0.0, 0.0));
            Assert.AreEqual(5.0, origin.Z, 1e-9, "Extruded about its sketch plane, the sprocket needs no half-width shift.");
        }

        [Test]
        public void TestSprocket_Validate_RejectsBadSettings()
        {
            Assert.Throws<InvalidOperationException>(() => Sprocket(ChainSeries.B08, ChainProfile.AbsoluteMinimumTeeth - 1).Validate());
            var wide = Sprocket(ChainSeries.B08, 19);
            wide.FaceWidth = 0.0;
            Assert.Throws<InvalidOperationException>(() => wide.Validate());
            var bore = Sprocket(ChainSeries.B08, 19);
            bore.BoreDiameter = bore.RootDiameter;
            Assert.Throws<InvalidOperationException>(() => bore.Validate());
            var negative = Sprocket(ChainSeries.B08, 19);
            negative.HubLength = -1.0;
            Assert.Throws<InvalidOperationException>(() => negative.Validate());
            Assert.DoesNotThrow(() => Sprocket(ChainSeries.B08, 19).Validate());
        }
    }
}
