using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Alibre;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// HTD pulleys: the Alibre export plan (one groove cell and the flanges), the bore and hub of the 3D model, and the timing belt
    /// that joins two pulleys (placement, rotation, shape, saving and the editor commands).
    /// </summary>
    [TestFixture]
    public class TestHtdBeltAndExport
    {
        private sealed class FakeView : IGearTrainView
        {
            public double InputSpeedRpm { get; set; } = 1000.0;
            public object Properties;
            public AssemblyScene Scene;
            public string Message;
            public bool IsError;

            public void ShowTree(TrainTreeNode root, object selectedTag) { }
            public void ShowSelection(object selectedTag) { }
            public void ShowProperties(object properties) => Properties = properties;
            public void ShowScene(AssemblyScene scene, object selectedTag, ISet<GearComponent> hiddenGears) => Scene = scene;
            public void ShowReport(GearTrainReport report) { }

            public void ShowMessage(string message, bool isError)
            {
                Message = message;
                IsError = isError;
            }
        }

        private static HtdPulleyComponent Pulley(HtdSeries series, int teeth, bool flanges = false)
        {
            return new HtdPulleyComponent("P1") { Series = series, Teeth = teeth, HasFlanges = flanges };
        }

        private static GearAssembly TwoPulleys(out HtdPulleyComponent a, out HtdPulleyComponent b, out TimingBelt belt, double centre = 120.0, int teethA = 20, int teethB = 40)
        {
            var assembly = new GearAssembly();
            var shaftA = new Shaft("S1");
            a = new HtdPulleyComponent("A") { Series = HtdSeries.Htd5M, Teeth = teethA };
            shaftA.Mount(a, 0.0);
            var shaftB = new Shaft("S2");
            b = new HtdPulleyComponent("B") { Series = HtdSeries.Htd5M, Teeth = teethB };
            shaftB.Mount(b, 3.0);
            assembly.AddShaft(shaftA);
            assembly.AddShaft(shaftB);
            belt = assembly.AddBelt(new TimingBelt("Belt 1", a, b, centre));
            return assembly;
        }

        /// <summary>Volume enclosed by a mesh whose faces point outwards: the sum of the signed tetrahedra to the origin.</summary>
        private static double Volume(GearMesh3DBuilder.Mesh3D mesh)
        {
            var volume = 0.0;
            var v = mesh.Vertices;
            for (var i = 0; i + 2 < mesh.TriangleIndices.Count; i += 3)
            {
                var a = v[mesh.TriangleIndices[i]];
                var b = v[mesh.TriangleIndices[i + 1]];
                var c = v[mesh.TriangleIndices[i + 2]];
                volume += (a.X * (b.Y * c.Z - b.Z * c.Y) - a.Y * (b.X * c.Z - b.Z * c.X) + a.Z * (b.X * c.Y - b.Y * c.X)) / 6.0;
            }

            return volume;
        }

        /// <summary>Area of a regular polygon with <paramref name="segments"/> sides inscribed in a circle.</summary>
        private static double PolygonArea(double radius, int segments)
        {
            return 0.5 * segments * radius * radius * Math.Sin(2.0 * Math.PI / segments);
        }

        private static double OutlineArea(HtdPulleyComponent pulley) => SolidMeshBuilder.SignedArea(pulley.BuildOutline());

        private static int Segments(HtdPulleyComponent pulley) => pulley.Teeth * Math.Max(2, (int)Math.Ceiling(64.0 / pulley.Teeth));

        #region Alibre export plan

        private static IEnumerable<GearPoint> FigurePoints(IEnumerable<ToothSketchFigure> figures)
        {
            foreach (var figure in figures)
            {
                yield return new GearPoint(figure.StartX, figure.StartY);
                yield return new GearPoint(figure.EndX, figure.EndY);
                if (figure.Points == null) continue;
                foreach (var p in figure.Points) yield return p;
            }
        }

        /// <summary>The wedge as a polygon (arcs sampled, splines through their points), for its area.</summary>
        private static double WedgeArea(IReadOnlyList<ToothSketchFigure> figures)
        {
            var points = new List<GearPoint>();
            foreach (var figure in figures)
            {
                switch (figure.Kind)
                {
                    case ToothSketchFigureKind.Line:
                        points.Add(new GearPoint(figure.StartX, figure.StartY));
                        break;
                    case ToothSketchFigureKind.Arc:
                    {
                        var a0 = Math.Atan2(figure.StartY - figure.CentreY, figure.StartX - figure.CentreX);
                        var a1 = Math.Atan2(figure.EndY - figure.CentreY, figure.EndX - figure.CentreX);
                        if (a1 <= a0) a1 += 2.0 * Math.PI;
                        var radius = Math.Sqrt(Math.Pow(figure.StartX - figure.CentreX, 2) + Math.Pow(figure.StartY - figure.CentreY, 2));
                        for (var i = 0; i < 16; i++)
                        {
                            var a = a0 + (a1 - a0) * i / 16.0;
                            points.Add(new GearPoint(figure.CentreX + radius * Math.Cos(a), figure.CentreY + radius * Math.Sin(a)));
                        }

                        break;
                    }
                    default:
                        for (var i = 0; i < figure.Points.Count - 1; i++) points.Add(figure.Points[i]);
                        break;
                }
            }

            return SolidMeshBuilder.SignedArea(points);
        }

        [Test]
        public void TestPulleySketch_IsClosedAndLiesBetweenTheRootAndTheOutsideDiameter()
        {
            foreach (var profile in HtdProfile.Profiles)
            {
                foreach (var teeth in new[] { HtdPulleyComponent.AbsoluteMinimumTeeth + 4, profile.MinimumTeeth, 60 })
                {
                    var pulley = Pulley(profile.Series, teeth);
                    var plan = PulleySketchBuilder.Build(pulley);

                    Assert.IsTrue(plan.Tooth.IsClosed(), $"{profile.Name} {teeth}: the groove cell is a closed loop.");
                    var outside = pulley.OutsideDiameter / 2.0 * SingleToothSketchBuilder.MmToAlibreUnits;
                    var root = pulley.RootDiameter / 2.0 * SingleToothSketchBuilder.MmToAlibreUnits;
                    foreach (var p in FigurePoints(plan.Tooth.Figures))
                    {
                        var radius = Math.Sqrt(p.X * p.X + p.Y * p.Y);
                        Assert.LessOrEqual(radius, outside + 1e-6, $"{profile.Name} {teeth}: no point is outside the outside diameter.");
                        Assert.IsTrue(radius < 1e-9 || radius >= root - 1e-6, $"{profile.Name} {teeth}: no point is inside the root diameter (radius {radius}).");
                    }
                }
            }
        }

        [Test]
        public void TestPulleySketch_WedgesTileToTheAreaOfTheOutline()
        {
            foreach (var profile in HtdProfile.Profiles)
            {
                var pulley = Pulley(profile.Series, Math.Max(profile.MinimumTeeth, 20));
                var plan = PulleySketchBuilder.Build(pulley);

                var wedgeArea = WedgeArea(plan.Tooth.Figures) * 100.0; // square centimetres to square millimetres
                Assert.AreEqual(OutlineArea(pulley), wedgeArea * pulley.Teeth, OutlineArea(pulley) * 0.01, $"{profile.Name}: the patterned cells fill the pulley.");
            }
        }

        [Test]
        public void TestPulleySketch_HasTheGroovesWidthAndPartName()
        {
            var pulley = Pulley(HtdSeries.Htd5M, 20);
            pulley.FaceWidth = 15.0;

            var plan = PulleySketchBuilder.Build(pulley);

            Assert.AreEqual(20, plan.Tooth.Teeth);
            Assert.AreEqual(1.5, plan.Tooth.ExtrusionDepth, 1e-9, "The width is extruded in centimetres.");
            Assert.AreEqual(2.0 * Math.PI / 20.0, plan.Tooth.PatternAngle, 1e-12);
            Assert.AreEqual("HtdPulley_5M_Z20_B15", plan.Tooth.PartName);
            Assert.AreEqual("HtdPulley_5M_Z20_B15", PulleySketchBuilder.PartName(pulley));
            Assert.IsFalse(plan.HasFlanges);
            Assert.IsNull(plan.Flange);
            Assert.IsTrue(plan.IsFlangeClosed(), "A pulley without flanges has nothing to close.");
        }

        [Test]
        public void TestPulleySketch_FlangeIsOneClosedRectangleAboveTheMidPlaneSlab()
        {
            var pulley = Pulley(HtdSeries.Htd8M, 30, true);
            pulley.FaceWidth = 20.0;
            pulley.FlangeHeight = 3.0;
            pulley.FlangeThickness = 2.0;

            var plan = PulleySketchBuilder.Build(pulley);

            Assert.IsTrue(plan.HasFlanges);
            Assert.IsNotNull(plan.Flange);
            Assert.IsTrue(plan.IsFlangeClosed());

            var radius = pulley.FlangeDiameter / 2.0 * 0.1;
            var zs = FigurePoints(plan.Flange).Select(p => p.Y).Distinct().OrderBy(y => y).ToList();
            Assert.AreEqual(2, zs.Count);
            Assert.AreEqual(1.0, zs[0], 1e-9, "Flange 1 starts at half the width above the mid-plane (the sketch plane).");
            Assert.AreEqual(1.2, zs[1], 1e-9, "It is one thickness thick; flange 2 is its mirror image, so it is not in the plan.");
            Assert.AreEqual(radius, FigurePoints(plan.Flange).Max(p => p.X), 1e-7, "Lengths are rounded to 7 decimals in millimetres.");
        }

        [Test]
        public void TestPulleySketch_TooFewGroovesAreRejected()
        {
            var pulley = Pulley(HtdSeries.Htd14M, HtdPulleyComponent.AbsoluteMinimumTeeth - 1);

            Assert.Throws<InvalidOperationException>(() => PulleySketchBuilder.Build(pulley), "Fewer grooves than the accepted minimum.");
        }

        [Test]
        public void TestAlibreExport_AcceptsPulleysInAnAssembly()
        {
            var assembly = TwoPulleys(out var a, out var b, out _);

            var parts = AlibreGearExporter.GetAssemblyParts(assembly);

            Assert.AreEqual(2, parts.Count);
            Assert.AreSame(a, parts[0].Key);
            Assert.AreSame(b, parts[1].Key);
            Assert.AreEqual(0.0, parts[0].Value.Translation.Z, 1e-9, "The pulley is extruded mid-plane, so its sketch plane is the mid-plane.");
            Assert.AreEqual(3.0, parts[1].Value.Translation.Z, 1e-9, "Only the axial offset of the mount locates the part.");
            Assert.IsNotNull(a);
        }

        #endregion

        #region Alibre belt export plan

        /// <summary>Area of a belt loop (arcs sampled, lines straight) in square millimetres.</summary>
        private static double LoopArea(IReadOnlyList<ToothSketchFigure> figures)
        {
            return WedgeArea(figures) * 100.0;
        }

        [Test]
        public void TestBeltSketch_HasTwoClosedLoopsAndAPartName()
        {
            TwoPulleys(out _, out _, out var belt, 120.0, 20, 40);
            belt.CentreLineAngleDeg = 25.0;

            var plan = BeltSketchBuilder.Build(belt);

            Assert.IsTrue(plan.IsClosed());
            Assert.AreEqual(4, plan.OuterLoop.Count, "Two arcs and two straight runs.");
            Assert.AreEqual(6 * belt.BeltTeeth, plan.InnerLoop.Count, "Per tooth: three splines, two flanks and the land to the next tooth.");
            Assert.AreEqual(4 + 6 * belt.BeltTeeth, plan.Figures.Count);
            Assert.AreEqual(belt.Width * 0.1, plan.Width, 1e-9, "The width is extruded in centimetres.");
            Assert.AreEqual($"HtdBelt_5M_T{belt.BeltTeeth}_W{belt.Width:0.##}", plan.PartName);
            Assert.AreEqual(plan.PartName, BeltSketchBuilder.PartName(belt));
            Assert.AreEqual(3 * belt.BeltTeeth, plan.InnerLoop.Count(f => f.Kind == ToothSketchFigureKind.Spline), "Every tooth has two edges and a bottom.");
            Assert.IsTrue(plan.OuterLoop.All(f => f.Kind != ToothSketchFigureKind.Spline), "The back of the belt is plain.");
        }

        [Test]
        public void TestBeltSketch_IsClosedForEveryAngleAndSeries()
        {
            foreach (var series in new[] { HtdSeries.Htd3M, HtdSeries.Htd5M, HtdSeries.Htd8M, HtdSeries.Htd14M })
            {
                foreach (var angle in new[] { 0.0, 25.0, 135.0, -70.0 })
                {
                    var assembly = new GearAssembly();
                    var shaftA = new Shaft("S1");
                    var a = new HtdPulleyComponent("A") { Series = series, Teeth = 30 };
                    shaftA.Mount(a, 0.0);
                    var shaftB = new Shaft("S2");
                    var b = new HtdPulleyComponent("B") { Series = series, Teeth = 60 };
                    shaftB.Mount(b, 0.0);
                    assembly.AddShaft(shaftA);
                    assembly.AddShaft(shaftB);
                    var belt = assembly.AddBelt(new TimingBelt("Belt 1", a, b, 400.0) { CentreLineAngleDeg = angle });

                    var plan = BeltSketchBuilder.Build(belt);

                    Assert.IsTrue(plan.IsClosed(), $"{series} at {angle} degrees: both loops close.");
                    Assert.AreEqual(6 * belt.BeltTeeth, plan.InnerLoop.Count, $"{series} at {angle} degrees");
                }
            }
        }

        [Test]
        public void TestBeltSketch_LoopsMatchTheBeltOutlinesOfTheScene()
        {
            TwoPulleys(out _, out _, out var belt, 120.0, 20, 40);
            belt.CentreLineAngleDeg = 25.0;
            var plan = BeltSketchBuilder.Build(belt);

            var inner = TimingBeltMeshBuilder.BuildLoop(belt, -belt.Profile.PitchLineDifferential);
            var outer = TimingBeltMeshBuilder.BuildLoop(belt, -belt.Profile.PitchLineDifferential + belt.BackThickness);

            var toothArea = belt.BeltTeeth * ToothArea(belt.Profile);
            var plain = SolidMeshBuilder.SignedArea(inner);
            Assert.AreEqual(plain - toothArea, LoopArea(plan.InnerLoop), plain * 0.01, "The teeth reach into the loop, so they take away their area.");
            Assert.AreEqual(SolidMeshBuilder.SignedArea(outer), LoopArea(plan.OuterLoop), SolidMeshBuilder.SignedArea(outer) * 0.01);
            Assert.Greater(LoopArea(plan.OuterLoop), LoopArea(plan.InnerLoop));
        }

        /// <summary>Area of one belt tooth (the groove profile above its curve, up to the land) in square millimetres.</summary>
        private static double ToothArea(HtdProfile profile)
        {
            var half = HtdPulleyComponent.HalfGroove(profile);
            var area = 0.0;
            for (var i = 0; i + 1 < half.Count; i++)
            {
                area += (half[i + 1].X - half[i].X) * (profile.ToothHeight - (half[i].Y + half[i + 1].Y) / 2.0);
            }

            return 2.0 * area;
        }

        [Test]
        public void TestBeltSketch_ArcsRoundThePulleysAreConcentricWithThem()
        {
            TwoPulleys(out var a, out var b, out var belt, 120.0, 20, 40);
            var plan = BeltSketchBuilder.Build(belt);

            double Radius(ToothSketchFigure f) => Math.Sqrt(Math.Pow(f.StartX - f.CentreX, 2) + Math.Pow(f.StartY - f.CentreY, 2));

            var arcs = plan.InnerLoop.Where(f => f.Kind == ToothSketchFigureKind.Arc).ToList();
            Assert.Greater(arcs.Count, 0, "The lands between teeth round a pulley are arcs.");
            var driverArcs = arcs.Where(f => Math.Abs(f.CentreX) < 1e-9).ToList();
            var drivenArcs = arcs.Where(f => Math.Abs(f.CentreX) >= 1e-9).ToList();
            Assert.AreEqual(arcs.Count, driverArcs.Count + drivenArcs.Count);
            Assert.Greater(driverArcs.Count, 0);
            Assert.Greater(drivenArcs.Count, 0);
            foreach (var arc in drivenArcs)
            {
                Assert.AreEqual(12.0, arc.CentreX, 1e-6, "The driven pulley is 120 mm along the line of centres, in centimetres.");
                Assert.AreEqual(b.OutsideDiameter / 2.0 * 0.1, Radius(arc), 1e-6, "The belt lands rest on the outside diameter.");
            }

            foreach (var arc in driverArcs) Assert.AreEqual(a.OutsideDiameter / 2.0 * 0.1, Radius(arc), 1e-6);

            var outerArcs = plan.OuterLoop.Where(f => f.Kind == ToothSketchFigureKind.Arc).ToList();
            Assert.AreEqual(2, outerArcs.Count);
            var back = (a.OutsideDiameter / 2.0 + belt.BackThickness) * 0.1;
            Assert.AreEqual(back, Radius(outerArcs.First(f => Math.Abs(f.CentreX) < 1e-9)), 1e-6, "The back of the belt is the back thickness outside the pulley.");
        }

        [Test]
        public void TestBeltSketch_TeethReachTheRootDiameter()
        {
            TwoPulleys(out var a, out var b, out var belt, 120.0, 20, 40);
            var plan = BeltSketchBuilder.Build(belt);

            var points = FigurePoints(plan.InnerLoop).ToList();
            var nearest = points.Min(p => Math.Min(Math.Sqrt(p.X * p.X + p.Y * p.Y), Math.Sqrt((p.X - 12.0) * (p.X - 12.0) + p.Y * p.Y)));
            Assert.AreEqual(a.RootDiameter / 2.0 * 0.1, nearest, 1e-6, "The tooth tips are at the groove bottoms of the smaller pulley.");
            Assert.AreEqual(b.OutsideDiameter - b.RootDiameter, 2.0 * belt.Profile.ToothHeight, 1e-9);
        }

        [Test]
        public void TestBeltSketch_AnInvalidBeltIsRejected()
        {
            var a = new HtdPulleyComponent("A") { Series = HtdSeries.Htd5M, Teeth = 20 };
            var b = new HtdPulleyComponent("B") { Series = HtdSeries.Htd8M, Teeth = 20 };

            Assert.Throws<InvalidOperationException>(() => BeltSketchBuilder.Build(new TimingBelt("X", a, b, 200.0)));
        }

        [Test]
        public void TestAlibreExport_BeltsAreOnlyIncludedWhenAsked()
        {
            var assembly = TwoPulleys(out _, out _, out var belt, 120.0, 20, 40);

            Assert.AreEqual(0, AlibreGearExporter.GetBeltAssemblyParts(assembly, null).Count);
            Assert.AreEqual(0, AlibreGearExporter.GetBeltAssemblyParts(assembly, new AssemblyExportOptions { IncludeShafts = true }).Count);

            var parts = AlibreGearExporter.GetBeltAssemblyParts(assembly, new AssemblyExportOptions { IncludeBelts = true });
            Assert.AreEqual(1, parts.Count);
            Assert.AreSame(belt, parts[0].Key);
        }

        [Test]
        public void TestAlibreExport_BeltIsPlacedOnTheMidPlaneOfTheDriverPulley()
        {
            var assembly = TwoPulleys(out _, out _, out _, 120.0, 20, 40);
            assembly.Solve();

            var parts = AlibreGearExporter.GetBeltAssemblyParts(assembly, new AssemblyExportOptions { IncludeBelts = true });

            var translation = parts[0].Value.Translation;
            Assert.AreEqual(0.0, translation.X, 1e-9, "The driver shaft is at the origin.");
            Assert.AreEqual(0.0, translation.Y, 1e-9);
            Assert.AreEqual(0.0, translation.Z, 1e-9, "The driver is mounted at offset 0 and the belt part is centred on its sketch plane.");
        }

        [Test]
        public void TestAlibreExport_BeltFollowsTheDriverMountOffset()
        {
            var assembly = TwoPulleys(out var a, out _, out _, 120.0, 20, 40);
            assembly.Shafts[0].Mounts[0].AxialOffset = 4.0;
            assembly.Solve();

            var parts = AlibreGearExporter.GetBeltAssemblyParts(assembly, new AssemblyExportOptions { IncludeBelts = true });

            Assert.AreEqual(4.0, parts[0].Value.Translation.Z, 1e-9);
            Assert.IsNotNull(a);
        }

        #endregion

        #region Bore and hub

        [Test]
        public void TestBoreAndHub_HubExtendsTheBodyOnThePositiveSide()
        {
            var pulley = Pulley(HtdSeries.Htd5M, 20);
            pulley.FaceWidth = 15.0;

            pulley.GetBodyAxialExtent(out var start, out var end);
            Assert.AreEqual(-7.5, start, 1e-9);
            Assert.AreEqual(7.5, end, 1e-9);

            pulley.BoreDiameter = 8.0;
            pulley.HubDiameter = 20.0;
            pulley.HubLength = 6.0;
            pulley.GetBodyAxialExtent(out start, out end);
            Assert.IsTrue(pulley.HasHub);
            Assert.AreEqual(-7.5, start, 1e-9);
            Assert.AreEqual(13.5, end, 1e-9);
        }

        [Test]
        public void TestBoreAndHub_AHubNotLargerThanTheBoreIsNoHub()
        {
            var pulley = Pulley(HtdSeries.Htd5M, 20);
            pulley.BoreDiameter = 10.0;
            pulley.HubDiameter = 10.0;
            pulley.HubLength = 5.0;

            Assert.IsFalse(pulley.HasHub);
            Assert.IsTrue(pulley.HasBoreOrHub, "The bore alone still gives ring-shaped end faces.");

            pulley.HubDiameter = 12.0;
            pulley.HubLength = 0.0;
            Assert.IsFalse(pulley.HasHub, "A hub needs a length.");
        }

        [Test]
        public void TestBoreAndHub_ABoreAsLargeAsTheRootDiameterIsRejected()
        {
            var pulley = Pulley(HtdSeries.Htd5M, 20);
            pulley.BoreDiameter = pulley.RootDiameter;

            Assert.Throws<InvalidOperationException>(() => pulley.BuildGeometry());

            pulley.BoreDiameter = pulley.RootDiameter - 1.0;
            Assert.DoesNotThrow(() => pulley.BuildGeometry());
        }

        [Test]
        public void TestBoreAndHub_AnUnboredPulleyIsASolidOfTheOutlineArea()
        {
            var pulley = Pulley(HtdSeries.Htd5M, 20);
            pulley.FaceWidth = 15.0;

            var volume = Volume(pulley.BuildGeometry().Mesh);

            Assert.AreEqual(OutlineArea(pulley) * 15.0, volume, volume * 1e-6, "The mesh is closed and its faces point outwards.");
        }

        [Test]
        public void TestBoreAndHub_ABoreRemovesItsCylinder()
        {
            var pulley = Pulley(HtdSeries.Htd5M, 20);
            pulley.FaceWidth = 15.0;
            pulley.BoreDiameter = 8.0;

            var volume = Volume(pulley.BuildGeometry().Mesh);

            var expected = (OutlineArea(pulley) - PolygonArea(4.0, Segments(pulley))) * 15.0;
            Assert.AreEqual(expected, volume, expected * 0.01);
            Assert.Less(volume, OutlineArea(pulley) * 15.0);
        }

        [Test]
        public void TestBoreAndHub_ASolidHubAddsItsCylinder()
        {
            var pulley = Pulley(HtdSeries.Htd5M, 20);
            pulley.FaceWidth = 15.0;
            pulley.HubDiameter = 20.0;
            pulley.HubLength = 6.0;

            var volume = Volume(pulley.BuildGeometry().Mesh);

            var expected = OutlineArea(pulley) * 15.0 + PolygonArea(10.0, Segments(pulley)) * 6.0;
            Assert.AreEqual(expected, volume, expected * 0.01);
        }

        [Test]
        public void TestBoreAndHub_BoreHubAndFlangesTogether()
        {
            var pulley = Pulley(HtdSeries.Htd8M, 30, true);
            pulley.FaceWidth = 20.0;
            pulley.FlangeHeight = 3.0;
            pulley.FlangeThickness = 2.0;
            pulley.BoreDiameter = 12.0;
            pulley.HubDiameter = 30.0;
            pulley.HubLength = 8.0;

            var geometry = pulley.BuildGeometry();
            var volume = Volume(geometry.Mesh);

            var segments = Segments(pulley);
            var flangeRadius = pulley.FlangeDiameter / 2.0;
            var expected = (OutlineArea(pulley) - PolygonArea(6.0, segments)) * 20.0
                           + 2.0 * (PolygonArea(flangeRadius, segments) - PolygonArea(6.0, segments)) * 2.0
                           + (PolygonArea(15.0, segments) - PolygonArea(6.0, segments)) * 8.0;
            Assert.AreEqual(expected, volume, expected * 0.01);

            var zs = geometry.Mesh.Vertices.Select(v => v.Z).ToList();
            Assert.AreEqual(-12.0, zs.Min(), 1e-9, "The lower flange ends 10 + 2 mm below the mid-plane.");
            Assert.AreEqual(20.0, zs.Max(), 1e-9, "The hub ends 12 mm above the mid-plane plus its length.");
            Assert.AreEqual(geometry.Mesh.Vertices.Count, geometry.Mesh.Normals.Count, "Every vertex has a normal.");
        }

        [Test]
        public void TestBoreAndHub_ShowTheBoreAsACircleInTheOutlines()
        {
            var pulley = Pulley(HtdSeries.Htd5M, 20);
            pulley.BoreDiameter = 8.0;

            var polylines = pulley.BuildGeometry().Polylines;

            Assert.AreEqual(2, polylines.Count, "The groove outline and the bore.");
            Assert.AreEqual(4.0, polylines[1].Max(p => p.X), 1e-9);
        }

        [Test]
        public void TestBoreAndHub_AreSavedAndLoaded()
        {
            var assembly = TwoPulleys(out var a, out _, out _);
            a.BoreDiameter = 8.0;
            a.HubDiameter = 20.0;
            a.HubLength = 6.0;

            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            var copy = (HtdPulleyComponent)loaded.Components.First(c => c.Id == "A");
            Assert.AreEqual(8.0, copy.BoreDiameter, 1e-12);
            Assert.AreEqual(20.0, copy.HubDiameter, 1e-12);
            Assert.AreEqual(6.0, copy.HubLength, 1e-12);
            var other = (HtdPulleyComponent)loaded.Components.First(c => c.Id == "B");
            Assert.AreEqual(0.0, other.BoreDiameter, 1e-12);
            Assert.IsFalse(other.HasHub);
        }

        [Test]
        public void TestBoreAndHub_ThePropertyGridEditsThem()
        {
            var presenter = new GearTrainPresenter(new FakeView());
            var pulley = presenter.NewPulley(HtdSeries.Htd5M, 20);
            var properties = (PulleyNodeProperties)new FakeViewProperties(presenter).For(pulley);

            properties.BoreDiameter = 8.0;
            properties.HubDiameter = 20.0;
            properties.HubLength = 6.0;
            properties.BoreDiameter = -3.0;

            Assert.AreEqual(0.0, pulley.BoreDiameter, 1e-12, "A negative bore is made zero.");
            Assert.AreEqual(20.0, pulley.HubDiameter, 1e-12);
            Assert.AreEqual(6.0, pulley.HubLength, 1e-12);
        }

        private sealed class FakeViewProperties
        {
            private readonly GearTrainPresenter _presenter;

            public FakeViewProperties(GearTrainPresenter presenter)
            {
                _presenter = presenter;
            }

            public object For(object selected)
            {
                var view = new FakeView();
                var presenter = new GearTrainPresenter(view);
                presenter.LoadAssembly(_presenter.Assembly);
                presenter.Select(selected);
                return view.Properties;
            }
        }

        #endregion

        #region Timing belt

        [Test]
        public void TestBelt_EqualPulleys_PathIsTwoCentresPlusACircumference()
        {
            TwoPulleys(out var a, out var b, out var belt, 120.0, 30, 30);

            var expected = 2.0 * 120.0 + Math.PI * a.PitchDiameter;
            Assert.AreEqual(expected, belt.PitchPathLength, 1e-9);
            Assert.AreEqual((int)Math.Round(expected / 5.0), belt.BeltTeeth);
            Assert.AreEqual(belt.BeltTeeth * 5.0, belt.PitchLength, 1e-9);
            Assert.AreEqual(1.0, belt.GearRatio, 1e-12);
            Assert.AreEqual(180.0, belt.SmallPulleyWrapDegrees, 1e-9, "Equal pulleys are wrapped over a half turn.");
            Assert.AreSame(a.Profile, belt.Profile);
            Assert.IsNotNull(b);
        }

        [Test]
        public void TestBelt_UnequalPulleys_PathIsLongerThanTheEqualCase_AndWrapIsSmaller()
        {
            TwoPulleys(out var small, out var large, out var belt, 120.0, 20, 60);
            var equalPath = 2.0 * 120.0 + Math.PI * (small.PitchDiameter + large.PitchDiameter) / 2.0;

            Assert.Greater(belt.PitchPathLength, equalPath, "The open belt is longer than the average circumference suggests.");
            Assert.Less(belt.SmallPulleyWrapDegrees, 180.0);
            Assert.AreEqual(3.0, belt.GearRatio, 1e-12);

            var alpha = Math.Asin((large.PitchDiameter - small.PitchDiameter) / (2.0 * 120.0));
            Assert.AreEqual(180.0 - 2.0 * alpha * 180.0 / Math.PI, belt.SmallPulleyWrapDegrees, 1e-9);
        }

        [Test]
        public void TestBelt_ValidationRejectsDifferentSeriesAndPulleysThatTouch()
        {
            var a = new HtdPulleyComponent("A") { Series = HtdSeries.Htd5M, Teeth = 20 };
            var b = new HtdPulleyComponent("B") { Series = HtdSeries.Htd8M, Teeth = 20 };

            Assert.Throws<InvalidOperationException>(() => new TimingBelt("X", a, b, 200.0).Validate(), "A 5M and an 8M pulley cannot share a belt.");

            b.Series = HtdSeries.Htd5M;
            var close = new TimingBelt("Y", a, b, a.OutsideDiameter - 1.0);
            Assert.Throws<InvalidOperationException>(() => close.Validate());
            Assert.Throws<ArgumentException>(() => new TimingBelt("Z", a, a, 100.0), "A belt needs two different pulleys.");
        }

        [Test]
        public void TestBelt_AddBelt_ChecksTheAssembly()
        {
            var assembly = TwoPulleys(out var a, out var b, out _);

            var third = new HtdPulleyComponent("C") { Series = HtdSeries.Htd5M, Teeth = 20 };
            Assert.Throws<InvalidOperationException>(() => assembly.AddBelt(new TimingBelt("B2", a, third, 100.0)), "An unmounted pulley.");
            Assert.Throws<InvalidOperationException>(() => assembly.AddBelt(new TimingBelt("B3", a, b, 100.0)), "The driven pulley already has a belt.");
            Assert.Throws<ArgumentException>(() => assembly.AddBelt(new TimingBelt("Belt 1", b, a, 100.0)), "The identifier is taken.");

            var sameShaft = new Shaft("S3");
            var p = new HtdPulleyComponent("P") { Series = HtdSeries.Htd5M, Teeth = 20 };
            var q = new HtdPulleyComponent("Q") { Series = HtdSeries.Htd5M, Teeth = 20 };
            sameShaft.Mount(p, 0.0);
            sameShaft.Mount(q, 30.0);
            assembly.AddShaft(sameShaft);
            Assert.Throws<InvalidOperationException>(() => assembly.AddBelt(new TimingBelt("B4", p, q, 100.0)), "Two pulleys of one shaft.");
        }

        [Test]
        public void TestBelt_Solve_PlacesTheDrivenShaftAtTheCentreDistanceAndAlignsTheMidPlanes()
        {
            var assembly = TwoPulleys(out _, out _, out var belt, 120.0);
            belt.CentreLineAngleDeg = 30.0;

            assembly.Solve();

            var origin = assembly.Shafts[1].Placement.Origin;
            Assert.AreEqual(120.0 * Math.Cos(Math.PI / 6.0), origin.X, 1e-6);
            Assert.AreEqual(120.0 * Math.Sin(Math.PI / 6.0), origin.Y, 1e-6);
            Assert.AreEqual(-3.0, origin.Z, 1e-6, "The mount offsets 0 and 3 put both mid-planes at the same height.");
            Assert.AreEqual(0.0, assembly.Shafts[1].Placement.ZAxis.Dot(assembly.Shafts[0].Placement.ZAxis) - 1.0, 1e-9, "The shafts are parallel.");
        }

        [Test]
        public void TestBelt_Solve_TurnsTheDrivenPulleyTheSameWayAtTheRatioOfTheGrooves()
        {
            var assembly = TwoPulleys(out _, out _, out _, 120.0, 20, 40);
            assembly.Shafts[0].RotationRad = 1.2;

            assembly.Solve();

            Assert.AreEqual(0.6, assembly.Shafts[1].RotationRad, 1e-9, "Half the speed, same direction.");

            var animator = new GearTrainAnimator(assembly) { SpeedRpm = 60.0 };
            animator.Advance(0.25);
            assembly.Solve();
            var input = assembly.Shafts[0].RotationRad;
            Assert.AreEqual(input / 2.0, assembly.Shafts[1].RotationRad, 1e-9);
        }

        [Test]
        public void TestBelt_Solve_CanPlaceTheDriverFromThePlacedDrivenShaft()
        {
            var assembly = new GearAssembly();
            var first = new Shaft("S1");
            var anchor = new HtdPulleyComponent("Anchor") { Series = HtdSeries.Htd5M, Teeth = 20 };
            first.Mount(anchor, 0.0);
            var shaftA = new Shaft("S2");
            var driver = new HtdPulleyComponent("Driver") { Series = HtdSeries.Htd5M, Teeth = 40 };
            shaftA.Mount(driver, 2.0);
            assembly.AddShaft(first);
            assembly.AddShaft(shaftA);

            // The first shaft is placed; the driver's shaft is not, but the belt runs to the anchor, which it drives.
            assembly.AddBelt(new TimingBelt("B2", driver, anchor, 100.0) { CentreLineAngleDeg = 90.0 });
            first.RotationRad = 0.8;

            assembly.Solve();

            var origin = shaftA.Placement.Origin;
            Assert.AreEqual(0.0, origin.X, 1e-6);
            Assert.AreEqual(-100.0, origin.Y, 1e-6, "The driver lies 100 mm 'behind' the anchor on the line of centres at 90 degrees.");
            Assert.AreEqual(-2.0, origin.Z, 1e-6, "The driver's mid-plane is 2 mm up its shaft, so its shaft starts 2 mm below the anchor's.");
            Assert.AreEqual(0.4, shaftA.RotationRad, 1e-9, "The 20 groove anchor is driven by the 40 groove pulley, which turns half as far in the same direction.");
        }

        [Test]
        public void TestBelt_RemovingAPulleyRemovesItsBelt()
        {
            var assembly = TwoPulleys(out _, out var b, out var belt);

            Assert.IsTrue(assembly.RemoveGear(b));

            Assert.AreEqual(0, assembly.Belts.Count);
            Assert.IsFalse(assembly.Belts.Contains(belt));
        }

        [Test]
        public void TestBelt_SceneHasABeltMeshOfTheStripVolume()
        {
            var assembly = TwoPulleys(out _, out _, out var belt, 120.0, 20, 40);
            belt.CentreLineAngleDeg = 25.0;

            var scene = GearAssemblySceneBuilder.Build(assembly);

            Assert.AreEqual(1, scene.BeltItems.Count);
            var item = scene.BeltItems[0];
            Assert.AreSame(belt, item.Belt);
            Assert.AreEqual(2, item.Polylines.Count, "The outer and the inner outline.");

            var loop = BeltToothPath.Build(belt);
            Assert.AreEqual(loop.InnerRing.Count, loop.OuterRing.Count);
            Assert.AreEqual(belt.BeltTeeth, loop.ToothCount);
            var areaInner = SolidMeshBuilder.SignedArea(new List<GearPoint>(loop.InnerRing));
            var areaOuter = SolidMeshBuilder.SignedArea(new List<GearPoint>(loop.OuterRing));
            Assert.Greater(areaInner, 0.0, "The loops run anticlockwise.");
            Assert.Greater(areaOuter, areaInner);

            var expected = belt.Width * (areaOuter - areaInner);
            Assert.AreEqual(expected, Volume(item.Mesh), expected * 1e-4, "The belt mesh is closed with its faces pointing outwards.");

            var plainInner = TimingBeltMeshBuilder.BuildLoop(belt, -belt.Profile.PitchLineDifferential);
            var plainOuter = TimingBeltMeshBuilder.BuildLoop(belt, -belt.Profile.PitchLineDifferential + belt.BackThickness);
            var strip = belt.Width * (SolidMeshBuilder.SignedArea(plainOuter) - SolidMeshBuilder.SignedArea(plainInner));
            Assert.AreEqual(strip + belt.Width * belt.BeltTeeth * ToothArea(belt.Profile), Volume(item.Mesh), strip * 0.02, "The teeth add their volume to the strip.");

            var zs = item.Mesh.Vertices.Select(v => v.Z).ToList();
            Assert.AreEqual(-belt.Width / 2.0, zs.Min(), 1e-6, "The belt lies on the mid-planes of the pulleys.");
            Assert.AreEqual(belt.Width / 2.0, zs.Max(), 1e-6);
        }

        [Test]
        public void TestBelt_LoopTouchesBothPulleysAtTheirOutsideDiameter()
        {
            TwoPulleys(out var a, out var b, out var belt, 120.0, 20, 40);

            var inner = TimingBeltMeshBuilder.BuildLoop(belt, -belt.Profile.PitchLineDifferential);

            var nearDriver = inner.Min(p => Math.Abs(Math.Sqrt(p.X * p.X + p.Y * p.Y) - a.OutsideDiameter / 2.0));
            var nearDriven = inner.Min(p => Math.Abs(Math.Sqrt((p.X - 120.0) * (p.X - 120.0) + p.Y * p.Y) - b.OutsideDiameter / 2.0));
            Assert.AreEqual(0.0, nearDriver, 1e-9);
            Assert.AreEqual(0.0, nearDriven, 1e-9);
            Assert.AreEqual(-a.OutsideDiameter / 2.0, inner.Min(p => p.X), 0.05, "The belt wraps the driver on its far side (arcs are sampled every 5 degrees).");
            Assert.AreEqual(120.0 + b.OutsideDiameter / 2.0, inner.Max(p => p.X), 0.05);
        }

        [Test]
        public void TestBelt_IsSavedAndLoaded()
        {
            var assembly = TwoPulleys(out _, out _, out var belt, 135.5);
            belt.CentreLineAngleDeg = 40.0;
            belt.Name = "Drive belt";

            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            Assert.AreEqual(1, loaded.Belts.Count);
            var copy = loaded.Belts[0];
            Assert.AreEqual("Belt 1", copy.Id);
            Assert.AreEqual("Drive belt", copy.Name);
            Assert.AreEqual("A", copy.Driver.Id);
            Assert.AreEqual("B", copy.Driven.Id);
            Assert.AreEqual(135.5, copy.CentreDistance, 1e-12);
            Assert.AreEqual(40.0, copy.CentreLineAngleDeg, 1e-12);
            Assert.AreEqual(belt.BeltTeeth, copy.BeltTeeth);
        }

        [Test]
        public void TestBelt_AFileWithoutBeltsStillLoads()
        {
            var assembly = new GearAssembly();
            var shaft = new Shaft("S1");
            shaft.Mount(new HtdPulleyComponent("A") { Series = HtdSeries.Htd5M, Teeth = 20 }, 0.0);
            assembly.AddShaft(shaft);
            var json = AssemblySerializer.ToJson(assembly);
            var withoutBelts = System.Text.RegularExpressions.Regex.Replace(json, ",\\s*\"Belts\"\\s*:\\s*\\[\\s*\\]", string.Empty);

            Assert.AreNotEqual(json, withoutBelts, "The test removed the belts entry.");
            var loaded = AssemblySerializer.FromJson(withoutBelts);

            Assert.AreEqual(0, loaded.Belts.Count);
            Assert.AreEqual(1, loaded.Components.Count());
        }

        [Test]
        public void TestBelt_FileWithABeltToAnUnknownPulleyIsRejected()
        {
            var assembly = TwoPulleys(out _, out _, out _);
            var json = AssemblySerializer.ToJson(assembly).Replace("\"Driven\": \"B\"", "\"Driven\": \"Nowhere\"");

            Assert.Throws<System.IO.InvalidDataException>(() => AssemblySerializer.FromJson(json));
        }

        #endregion

        #region Editor

        [Test]
        public void TestEditor_AddBeltedPulley_AddsAShaftAPulleyAndABelt()
        {
            var view = new FakeView();
            var presenter = new GearTrainPresenter(view);
            var driver = presenter.NewPulley(HtdSeries.Htd8M, 24);

            var driven = presenter.AddBeltedPulley(driver, 48);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(2, presenter.Assembly.Shafts.Count);
            Assert.AreEqual(1, presenter.Assembly.Belts.Count);
            var belt = presenter.Assembly.Belts[0];
            Assert.AreSame(driver, belt.Driver);
            Assert.AreSame(driven, belt.Driven);
            Assert.AreEqual(HtdSeries.Htd8M, driven.Series);
            Assert.AreEqual(48, driven.Teeth);
            Assert.AreEqual(driver.FaceWidth, driven.FaceWidth, 1e-12);
            Assert.Greater(belt.CentreDistance, belt.MinimumCentreDistance + 9.9, "At least 10 mm between the pulleys.");
            Assert.AreSame(driven, presenter.Selected);
            Assert.AreEqual(1, view.Scene.BeltItems.Count);
            Assert.AreEqual(2, view.Scene.Items.Count);
            Assert.AreEqual(belt.CentreDistance, view.Scene.Items[1].LocalToWorld.Translation.X, 1e-6, "The driven pulley is on the line of centres.");
        }

        [Test]
        public void TestEditor_AddBeltedPulley_DefaultsToTheDriversGrooves_AndHonoursAGivenDistance()
        {
            var view = new FakeView();
            var presenter = new GearTrainPresenter(view);
            var driver = presenter.NewPulley(HtdSeries.Htd5M, 30);

            var driven = presenter.AddBeltedPulley(driver, 0, 150.0);

            Assert.AreEqual(30, driven.Teeth);
            Assert.AreEqual(150.0, presenter.Assembly.Belts[0].CentreDistance, 1e-12);
            Assert.IsFalse(view.IsError, view.Message);
        }

        [Test]
        public void TestEditor_AddBeltedPulley_ATooShortDistanceLeavesTheTrainUnchanged()
        {
            var view = new FakeView();
            var presenter = new GearTrainPresenter(view);
            var driver = presenter.NewPulley(HtdSeries.Htd5M, 30);

            Assert.Throws<InvalidOperationException>(() => presenter.AddBeltedPulley(driver, 30, 10.0));

            Assert.AreEqual(1, presenter.Assembly.Shafts.Count, "The new shaft was taken out again.");
            Assert.AreEqual(0, presenter.Assembly.Belts.Count);
        }

        [Test]
        public void TestEditor_BeltCanBeSelected_ShownInThePropertyGrid_AndRemoved()
        {
            var view = new FakeView();
            var presenter = new GearTrainPresenter(view);
            var driver = presenter.NewPulley(HtdSeries.Htd5M, 20);
            presenter.AddBeltedPulley(driver, 40, 100.0);
            var belt = presenter.Assembly.Belts[0];

            presenter.Select(belt);

            Assert.AreSame(belt, presenter.Selected);
            var properties = (BeltNodeProperties)view.Properties;
            Assert.AreEqual(100.0, properties.CentreDistance, 1e-12);
            Assert.AreEqual(2.0, properties.Ratio, 1e-12);
            Assert.AreEqual(belt.BeltTeeth, properties.BeltTeeth);
            Assert.AreEqual("HTD 5M", properties.Series);

            properties.CentreDistance = 130.0;
            presenter.Refresh();
            Assert.AreEqual(130.0, view.Scene.Items[1].LocalToWorld.Translation.X, 1e-6, "Editing the distance moves the driven pulley.");

            presenter.RemoveSelected();
            Assert.AreEqual(0, presenter.Assembly.Belts.Count);
            Assert.AreEqual(2, presenter.Assembly.Shafts.Count, "Removing the belt leaves both shafts.");
            Assert.IsNull(presenter.Selected);
            Assert.AreEqual(0, view.Scene.BeltItems.Count);
        }

        [Test]
        public void TestEditor_BeltsAreKeptWhenTheTrainIsSavedAndLoaded()
        {
            var view = new FakeView();
            var presenter = new GearTrainPresenter(view);
            var driver = presenter.NewPulley(HtdSeries.Htd5M, 20);
            presenter.AddBeltedPulley(driver, 40, 100.0);
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gears_belt_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                Assert.IsTrue(presenter.Save(path));
                var other = new GearTrainPresenter(new FakeView());
                Assert.IsTrue(other.Load(path));

                Assert.AreEqual(1, other.Assembly.Belts.Count);
                Assert.AreEqual(100.0, other.Assembly.Belts[0].CentreDistance, 1e-12);

                var next = other.AddBeltedPulley(other.Assembly.Belts[0].Driven, 20);
                Assert.AreEqual(2, other.Assembly.Belts.Count);
                Assert.AreNotEqual(other.Assembly.Belts[0].Id, other.Assembly.Belts[1].Id, "New belts are numbered after the loaded ones.");
                Assert.IsNotNull(next);
            }
            finally
            {
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
        }

        [Test]
        public void TestEditor_ABeltedPulleyCanBeExportedToAnAssemblyPlan()
        {
            var view = new FakeView();
            var presenter = new GearTrainPresenter(view);
            var driver = presenter.NewPulley(HtdSeries.Htd5M, 20);
            presenter.AddBeltedPulley(driver, 40, 100.0);

            var parts = AlibreGearExporter.GetAssemblyParts(presenter.Assembly);

            Assert.AreEqual(2, parts.Count);
        }

        #endregion
    }
}
