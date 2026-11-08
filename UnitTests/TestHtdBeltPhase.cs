using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator.Alibre;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Registration of a timing belt on its pulleys: the centre of a belt tooth lies on the centre of a pulley groove (the tooth fills the
    /// cavity), on the driver and on the driven pulley, whatever the rotation of the driver.
    /// </summary>
    [TestFixture]
    public class TestHtdBeltPhase
    {
        private sealed class Fixture
        {
            public GearAssembly Assembly;
            public TimingBelt Belt;
            public ShaftMount DriverMount;
            public ShaftMount DrivenMount;
            public Shaft DriverShaft;
            public Shaft DrivenShaft;
        }

        private static Fixture Make(int teethA, int teethB, double centre, double thetaDeg, double rotation = 0.0, double offsetA = 0.0, double offsetB = 0.0)
        {
            var f = new Fixture { Assembly = new GearAssembly() };
            f.DriverShaft = new Shaft("S1");
            var a = new HtdPulleyComponent("A") { Series = HtdSeries.Htd5M, Teeth = teethA };
            f.DriverMount = f.DriverShaft.Mount(a, 0.0);
            f.DrivenShaft = new Shaft("S2");
            var b = new HtdPulleyComponent("B") { Series = HtdSeries.Htd5M, Teeth = teethB };
            f.DrivenMount = f.DrivenShaft.Mount(b, 3.0);
            f.Assembly.AddShaft(f.DriverShaft);
            f.Assembly.AddShaft(f.DrivenShaft);
            f.Belt = f.Assembly.AddBelt(new TimingBelt("Belt 1", a, b, centre) { CentreLineAngleDeg = thetaDeg });
            f.DriverShaft.RotationRad = rotation;
            f.DriverMount.PhaseOffsetRad = offsetA;
            f.DrivenMount.PhaseOffsetRad = offsetB;
            f.Assembly.Solve();
            return f;
        }

        private static double Wrap(double x, double step) => x - step * Math.Floor(x / step + 0.5);

        private static BeltToothedLoop BuildLoop(Fixture f) => BeltToothPath.Build(f.Belt, f.DriverMount.PhaseRad);

        /// <summary>
        /// Signed angular distances (radians, within half a groove step) from the centres of the belt teeth that lie on the wrap round the
        /// pulley at <paramref name="centre"/> to the nearest groove centre of that pulley, whose groove 0 is at <paramref name="phase"/>.
        /// The centre of a tooth is the middle of its bottom spline.
        /// </summary>
        private static List<double> ToothErrors(BeltToothedLoop loop, GearPoint centre, double phase, int grooves)
        {
            var step = 2.0 * Math.PI / grooves;
            var centres = new List<GearPoint>();
            foreach (var piece in loop.InnerPieces)
            {
                if (piece.Kind != BeltPieceKind.Spline || !piece.Name.EndsWith("Bottom")) continue;
                centres.Add(piece.Points[piece.Points.Count / 2]);
            }

            double Radius(GearPoint p) => Math.Sqrt((p.X - centre.X) * (p.X - centre.X) + (p.Y - centre.Y) * (p.Y - centre.Y));
            var root = centres.Min(Radius);
            var errors = new List<double>();
            foreach (var p in centres)
            {
                if (Radius(p) > root + 1e-7) continue;
                errors.Add(Wrap(Math.Atan2(p.Y - centre.Y, p.X - centre.X) - phase, step));
            }

            return errors;
        }

        private static void AssertAligned(Fixture f, double toleranceRadDriver, double toleranceRadDriven, bool allTeeth, string label)
        {
            var loop = BuildLoop(f);
            var theta = f.Belt.CentreLineAngleDeg * Math.PI / 180.0;
            var driven = new GearPoint(f.Belt.CentreDistance * Math.Cos(theta), f.Belt.CentreDistance * Math.Sin(theta));
            var pitch = f.Belt.Profile.Pitch;

            var onDriver = ToothErrors(loop, new GearPoint(0.0, 0.0), f.DriverMount.PhaseRad, f.Belt.Driver.Teeth);
            var onDriven = ToothErrors(loop, driven, f.DrivenMount.PhaseRad, f.Belt.Driven.Teeth);
            Console.WriteLine($"[DEBUG_LOG] {label}: driver errors (pitch fractions) min {onDriver.Min(Math.Abs) * f.Belt.Driver.PitchDiameter / 2.0 / pitch:0.000000}, " +
                              $"max {onDriver.Max(Math.Abs) * f.Belt.Driver.PitchDiameter / 2.0 / pitch:0.000000}; " +
                              $"driven min {onDriven.Min(Math.Abs) * f.Belt.Driven.PitchDiameter / 2.0 / pitch:0.000000}, " +
                              $"max {onDriven.Max(Math.Abs) * f.Belt.Driven.PitchDiameter / 2.0 / pitch:0.000000}");

            Assert.Greater(onDriver.Count, 3, label);
            Assert.Greater(onDriven.Count, 3, label);
            Assert.LessOrEqual(onDriver.Min(Math.Abs), toleranceRadDriver, label + ": a tooth sits on a driver groove.");
            Assert.LessOrEqual(onDriven.Min(Math.Abs), toleranceRadDriven, label + ": a tooth sits on a driven groove.");

            if (allTeeth)
            {
                Assert.LessOrEqual(onDriver.Max(Math.Abs), toleranceRadDriver, label + ": every driver tooth.");
                Assert.LessOrEqual(onDriven.Max(Math.Abs), toleranceRadDriven, label + ": every driven tooth.");
            }
            else
            {
                // Over a half wrap the drift is below a quarter of the pitch.
                Assert.LessOrEqual(onDriver.Max(Math.Abs) * f.Belt.Driver.PitchDiameter / 2.0, pitch / 4.0 + 1e-6, label);
                Assert.LessOrEqual(onDriven.Max(Math.Abs) * f.Belt.Driven.PitchDiameter / 2.0, pitch / 4.0 + 1e-6, label);
            }
        }

        [Test]
        public void TestBeltPhase_StrictFixture_TeethSitInTheGrooves()
        {
            var f = Make(20, 20, 120.0, 0.0);
            Assert.AreEqual(340.0, f.Belt.PitchPathLength, 1e-9, "A whole number of pitches.");
            Assert.AreEqual(68, f.Belt.BeltTeeth);

            AssertAligned(f, 1e-6, 1e-6, true, "strict 20/20 theta 0");
        }

        [Test]
        public void TestBeltPhase_WholePitchPath_IsExactForAnyAngleRotationAndOffset()
        {
            foreach (var theta in new[] { 0.0, 30.0, 90.0, -45.0 })
            {
                foreach (var rotation in new[] { 0.0, 1.7, -1.7 })
                {
                    var f = Make(20, 20, 120.0, theta, rotation, 0.3, 0.45);
                    AssertAligned(f, 1e-6, 1e-6, true, $"20/20 theta {theta} rotation {rotation}");
                }
            }
        }

        [Test]
        public void TestBeltPhase_OtherToothCountsAndAngles_AreAlignedAtTheWrapMiddlesAndWithinABoundElsewhere()
        {
            foreach (var teeth in new[] { Tuple.Create(20, 40), Tuple.Create(30, 30), Tuple.Create(40, 20) })
            {
                foreach (var theta in new[] { 0.0, 30.0, 90.0, -45.0 })
                {
                    foreach (var rotation in new[] { 0.0, 1.7, -1.7 })
                    {
                        var f = Make(teeth.Item1, teeth.Item2, 120.0, theta, rotation, 0.2, -0.35);
                        AssertAligned(f, 1e-6, 1e-6, false, $"{teeth.Item1}/{teeth.Item2} theta {theta} rotation {rotation}");
                    }
                }
            }
        }

        [Test]
        public void TestBeltPhase_DrivenRotationStaysUnwrappedAndContinuousAcrossGrooves()
        {
            var f = Make(20, 40, 123.0, 0.0);
            var belt = f.Belt;
            var stepA = 2.0 * Math.PI / belt.Driver.Teeth;
            var stepB = 2.0 * Math.PI / belt.Driven.Teeth;
            Assert.Greater(Math.Abs(belt.PitchPathLength - belt.BeltTeeth * belt.Profile.Pitch), 1e-3, "Not a whole number of pitches.");

            // A regular sweep through three groove steps of the driver, plus a very fine sampling either side of every groove flip of the
            // registration (the driver groove nearest to the middle of its wrap changes half a step from a groove).
            var drivers = new List<double>();
            for (var i = 0; i <= 600; i++) drivers.Add(3.0 * stepA * i / 600);
            for (var k = 0; k < 3; k++)
            {
                var flip = (k + 0.5) * stepA;
                for (var i = -50; i <= 50; i++) drivers.Add(flip + i * 1e-6);
                for (var i = -50; i <= 50; i++) drivers.Add(flip + i * stepA * 1e-4);
            }

            drivers.Sort();
            var previousDriver = 0.0;
            var previousDriven = double.NaN;
            var first = double.NaN;
            var last = 0.0;
            var rest = double.NaN;
            foreach (var driver in drivers)
            {
                f.DriverShaft.RotationRad = driver;
                f.Assembly.Solve();
                var driven = f.DrivenShaft.RotationRad;
                var offset = driven - driver / belt.GearRatio;
                Assert.GreaterOrEqual(offset, -stepB / 2.0 - 1e-9, "C is centred on a groove step.");
                Assert.Less(offset, stepB / 2.0 + 1e-9);
                if (double.IsNaN(rest)) rest = offset;
                Assert.AreEqual(rest, offset, 1e-9, $"C is the same for every driver phase (driver {driver}).");
                if (double.IsNaN(first)) first = driven;
                else
                {
                    var jump = (driven - previousDriven) - (driver - previousDriver) / belt.GearRatio;
                    Assert.AreEqual(0.0, jump, 1e-9, $"The driven rotation does not jump (driver {driver}).");
                }

                previousDriver = driver;
                previousDriven = driven;
                last = driven;
            }

            Assert.Greater(last - first, 2.0 * stepB, "The driven rotation is not wrapped into one groove step.");
        }

        [Test]
        public void TestBeltPhase_ToothAtTheSeam_KeepsTheLoopClosedAndAnticlockwise()
        {
            foreach (var shift in new[] { -1e-9, 0.0, 1e-9, 2.0 * Math.PI / 20.0 / 2.0 - 1e-9, 2.0 * Math.PI / 20.0 / 2.0 + 1e-9 })
            {
                var f = Make(20, 20, 120.0, 0.0, shift);
                var loop = BuildLoop(f);

                Assert.AreEqual(f.Belt.BeltTeeth, loop.ToothCount);
                Assert.Greater(SolidMeshBuilder.SignedArea(new List<GearPoint>(loop.InnerRing)), 0.0, $"Anticlockwise (shift {shift}).");
                Assert.AreEqual(loop.InnerRing.Count, loop.OuterRing.Count);
                for (var i = 0; i < loop.InnerPieces.Count; i++)
                {
                    var end = loop.InnerPieces[i].End;
                    var start = loop.InnerPieces[(i + 1) % loop.InnerPieces.Count].Start;
                    var gap = Math.Sqrt((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y));
                    Assert.Less(gap, 1e-9, $"The pieces follow each other (shift {shift}, piece {i}).");
                }

                AssertAligned(f, 1e-6, 1e-6, true, $"seam shift {shift}");
            }
        }

        [Test]
        public void TestBeltPhase_DriverPlacedFromTheDrivenShaft_IsAlignedOnWholePitchPath()
        {
            var assembly = new GearAssembly();
            var anchorShaft = new Shaft("S1");
            var anchor = new HtdPulleyComponent("Anchor") { Series = HtdSeries.Htd5M, Teeth = 20 };
            var anchorMount = anchorShaft.Mount(anchor, 0.0);
            var driverShaft = new Shaft("S2");
            var driver = new HtdPulleyComponent("Driver") { Series = HtdSeries.Htd5M, Teeth = 20 };
            var driverMount = driverShaft.Mount(driver, 2.0);
            assembly.AddShaft(anchorShaft);
            assembly.AddShaft(driverShaft);
            var belt = assembly.AddBelt(new TimingBelt("B2", driver, anchor, 120.0) { CentreLineAngleDeg = 30.0 });
            anchorShaft.RotationRad = 0.8;
            anchorMount.PhaseOffsetRad = 0.37;
            driverMount.PhaseOffsetRad = 0.21;

            assembly.Solve();

            var f = new Fixture
            {
                Assembly = assembly, Belt = belt, DriverMount = driverMount, DrivenMount = anchorMount, DriverShaft = driverShaft, DrivenShaft = anchorShaft
            };
            Assert.AreEqual(0.8 + 0.37, anchorMount.PhaseRad, 1e-12, "The placed driven shaft is not moved.");
            AssertAligned(f, 1e-6, 1e-6, true, "branch 2, 20/20");
        }

        [Test]
        public void TestBeltPhase_DriverPlacedFromTheDrivenShaft_UnequalPulleysAreWithinTheBound()
        {
            var assembly = new GearAssembly();
            var anchorShaft = new Shaft("S1");
            var anchor = new HtdPulleyComponent("Anchor") { Series = HtdSeries.Htd5M, Teeth = 20 };
            var anchorMount = anchorShaft.Mount(anchor, 0.0);
            var driverShaft = new Shaft("S2");
            var driver = new HtdPulleyComponent("Driver") { Series = HtdSeries.Htd5M, Teeth = 40 };
            var driverMount = driverShaft.Mount(driver, 2.0);
            assembly.AddShaft(anchorShaft);
            assembly.AddShaft(driverShaft);
            var belt = assembly.AddBelt(new TimingBelt("B2", driver, anchor, 100.0) { CentreLineAngleDeg = 90.0 });
            anchorShaft.RotationRad = 0.8;

            assembly.Solve();

            var f = new Fixture
            {
                Assembly = assembly, Belt = belt, DriverMount = driverMount, DrivenMount = anchorMount, DriverShaft = driverShaft, DrivenShaft = anchorShaft
            };
            AssertAligned(f, 1e-6, 1e-6, false, "branch 2, 40/20");
        }

        #region Scene, belt sketch and exported pulleys

        private static double SegmentDistance(GearPoint a, GearPoint b, double x, double y)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var length2 = dx * dx + dy * dy;
            var t = length2 < 1e-18 ? 0.0 : Math.Max(0.0, Math.Min(1.0, ((x - a.X) * dx + (y - a.Y) * dy) / length2));
            var px = a.X + t * dx - x;
            var py = a.Y + t * dy - y;
            return Math.Sqrt(px * px + py * py);
        }

        private static double DistanceToPolyline(IReadOnlyList<GearPoint> polyline, double x, double y)
        {
            var best = double.MaxValue;
            for (var i = 0; i + 1 < polyline.Count; i++) best = Math.Min(best, SegmentDistance(polyline[i], polyline[i + 1], x, y));
            return best;
        }

        /// <summary>The world groove centres (at the root of the groove) of a pulley that lie on its wrap of the belt: on the side away from the other pulley.</summary>
        private static List<GearPoint> WrappedGrooves(SceneItem item, HtdPulleyComponent pulley, GearPoint other, double phaseOffsetStep)
        {
            var root = pulley.OutsideDiameter / 2.0 - pulley.Profile.ToothHeight;
            var step = 2.0 * Math.PI / pulley.Teeth;
            var centre = item.LocalToWorld.TransformPoint(new Vector3D(0.0, 0.0, 0.0));
            var result = new List<GearPoint>();
            for (var j = 0; j < pulley.Teeth; j++)
            {
                var angle = (j + phaseOffsetStep) * step;
                var p = item.LocalToWorld.TransformPoint(new Vector3D(root * Math.Cos(angle), root * Math.Sin(angle), 0.0));
                var towardsOther = (p.X - centre.X) * (other.X - centre.X) + (p.Y - centre.Y) * (other.Y - centre.Y);
                if (towardsOther < -0.2 * root * Math.Sqrt((other.X - centre.X) * (other.X - centre.X) + (other.Y - centre.Y) * (other.Y - centre.Y))) result.Add(new GearPoint(p.X, p.Y));
            }

            return result;
        }

        [Test]
        public void TestBeltPhase_Scene_InnerOutlineRunsThroughTheWorldGrooveCentres()
        {
            foreach (var theta in new[] { 0.0, 30.0, -45.0 })
            {
                foreach (var rotation in new[] { 0.0, 0.37, -1.7 })
                {
                    var f = Make(20, 20, 120.0, theta, rotation, 0.21, 0.13);
                    var scene = GearAssemblySceneBuilder.Build(f.Assembly);
                    var belt = scene.BeltItems.Single();
                    var inner = belt.Polylines[1];

                    var driverItem = scene.Find(f.Belt.Driver);
                    var drivenItem = scene.Find(f.Belt.Driven);
                    var driverCentre = driverItem.LocalToWorld.TransformPoint(new Vector3D(0.0, 0.0, 0.0));
                    var drivenCentre = drivenItem.LocalToWorld.TransformPoint(new Vector3D(0.0, 0.0, 0.0));
                    var driverGrooves = WrappedGrooves(driverItem, (HtdPulleyComponent)f.Belt.Driver, new GearPoint(drivenCentre.X, drivenCentre.Y), 0.0);
                    var drivenGrooves = WrappedGrooves(drivenItem, (HtdPulleyComponent)f.Belt.Driven, new GearPoint(driverCentre.X, driverCentre.Y), 0.0);
                    var halfStepDriver = WrappedGrooves(driverItem, (HtdPulleyComponent)f.Belt.Driver, new GearPoint(drivenCentre.X, drivenCentre.Y), 0.5);

                    Assert.Greater(driverGrooves.Count, 5);
                    Assert.Greater(drivenGrooves.Count, 5);
                    var label = $"theta {theta}, rotation {rotation}";
                    foreach (var g in driverGrooves) Assert.Less(DistanceToPolyline(inner, g.X, g.Y), 0.02, label + ": driver groove centre is filled by a belt tooth.");
                    foreach (var g in drivenGrooves) Assert.Less(DistanceToPolyline(inner, g.X, g.Y), 0.02, label + ": driven groove centre is filled by a belt tooth.");
                    foreach (var g in halfStepDriver) Assert.Greater(DistanceToPolyline(inner, g.X, g.Y), 0.5, label + ": a land centre is not on a tooth.");
                }
            }
        }

        /// <summary>X of the tooth tips on the upper run of the belt (theta 0, driver at the origin, driven at x = 120) in a scene.</summary>
        private static List<double> UpperRunTips(IReadOnlyList<GearPoint> inner)
        {
            var run = inner.Where(p => p.Y > 0.0 && p.X > 1.0 && p.X < 119.0).ToList();
            var minY = run.Min(p => p.Y);
            return run.Where(p => p.Y < minY + 1e-6).Select(p => p.X).OrderBy(x => x).ToList();
        }

        [Test]
        public void TestBeltPhase_Scene_TeethSlideWithTheDriverByTheArcLength()
        {
            var first = Make(20, 20, 120.0, 0.0, 0.0);
            var second = Make(20, 20, 120.0, 0.0, 0.05);
            var radius = first.Belt.Driver.PitchDiameter / 2.0;
            var shift = radius * 0.05;

            var tips1 = UpperRunTips(GearAssemblySceneBuilder.Build(first.Assembly).BeltItems.Single().Polylines[1]);
            var tips2 = UpperRunTips(GearAssemblySceneBuilder.Build(second.Assembly).BeltItems.Single().Polylines[1]);
            Console.WriteLine($"[DEBUG_LOG] upper run tips: {tips1.Count} and {tips2.Count}, shift {shift:0.000000} mm");

            Assert.GreaterOrEqual(tips1.Count, 15);
            var matched = 0;
            foreach (var x in tips1)
            {
                var expected = x - shift;
                if (expected < 6.0 || expected > 114.0) continue;
                Assert.IsTrue(tips2.Any(t => Math.Abs(t - expected) < 1e-4), $"The tooth at x = {x} moved to x = {expected} on the upper run.");
                matched++;
            }

            Assert.GreaterOrEqual(matched, 15);
        }

        [Test]
        public void TestBeltPhase_Sketch_MatchesTheSceneInnerOutlineAtANonzeroPhase()
        {
            var f = Make(20, 40, 120.0, 30.0, 0.9, 0.21, 0.0);
            var scene = GearAssemblySceneBuilder.Build(f.Assembly);
            var inner = scene.BeltItems.Single().Polylines[1];

            var plan = BeltSketchBuilder.Build(f.Belt, f.DriverMount.PhaseRad);
            var placement = AlibreGearExporter.GetBeltPlacement(f.DriverShaft, f.DriverMount);
            var k = SingleToothSketchBuilder.MmToAlibreUnits;
            var atZero = BeltSketchBuilder.Build(f.Belt);

            foreach (var figure in plan.InnerLoop)
            {
                var world = placement.TransformPoint(new Vector3D(figure.StartX / k, figure.StartY / k, 0.0));
                Assert.Less(DistanceToPolyline(inner, world.X, world.Y), 1e-5, $"The start of '{figure.Name}' is on the scene outline.");
            }

            Assert.IsTrue(plan.IsClosed());
            var moved = plan.InnerLoop.Where((fig, i) => Math.Abs(fig.StartX - atZero.InnerLoop[i].StartX) > 1e-4 || Math.Abs(fig.StartY - atZero.InnerLoop[i].StartY) > 1e-4);
            Assert.IsTrue(moved.Any(), "The phase moves the teeth.");
        }

        [Test]
        public void TestBeltPhase_Export_PulleyPlacementTurnsWithTheMountPhase()
        {
            var f = Make(20, 20, 120.0, 30.0, 0.9, 0.21, 0.13);
            foreach (var pair in new[] { Tuple.Create(f.DriverShaft, f.DriverMount), Tuple.Create(f.DrivenShaft, f.DrivenMount) })
            {
                var shaftTransform = pair.Item1.Placement.ToTransform();
                var placement = AlibreGearExporter.GetPartPlacement(pair.Item1, pair.Item2);
                var a = placement.TransformDirection(Vector3D.UnitX);
                var b = shaftTransform.TransformDirection(Vector3D.UnitX);
                var turn = Math.Atan2(a.Y, a.X) - Math.Atan2(b.Y, b.X);
                Assert.Greater(Math.Abs(Wrap(pair.Item2.PhaseRad, 2.0 * Math.PI)), 1e-3, "The mount phase is not zero.");
                Assert.AreEqual(0.0, Wrap(turn - pair.Item2.PhaseRad, 2.0 * Math.PI), 1e-9, "The part is turned about the shaft axis by the mount phase.");

                var expected = shaftTransform.TransformPoint(new Vector3D(0.0, 0.0, pair.Item2.AxialOffset));
                Assert.AreEqual(expected.X, placement.Translation.X, 1e-9);
                Assert.AreEqual(expected.Y, placement.Translation.Y, 1e-9);
                Assert.AreEqual(expected.Z, placement.Translation.Z, 1e-9, "The axial translation is unchanged.");
            }
        }

        [Test]
        public void TestBeltPhase_Export_PulleyGroovesCoincideWithTheBeltSketchTeeth()
        {
            foreach (var rotation in new[] { 0.0, 0.9, -1.7 })
            {
                var f = Make(20, 20, 120.0, 30.0, rotation, 0.21, 0.13);
                var k = SingleToothSketchBuilder.MmToAlibreUnits;
                var plan = BeltSketchBuilder.Build(f.Belt, f.DriverMount.PhaseRad);
                var beltPlacement = AlibreGearExporter.GetBeltPlacement(f.DriverShaft, f.DriverMount);

                var teeth = new List<Vector3D>();
                foreach (var figure in plan.InnerLoop)
                {
                    if (figure.Kind != ToothSketchFigureKind.Spline || !figure.Name.EndsWith("Bottom")) continue;
                    var p = figure.Points[figure.Points.Count / 2];
                    teeth.Add(beltPlacement.TransformPoint(new Vector3D(p.X / k, p.Y / k, 0.0)));
                }

                var centres = new List<Vector3D>();
                var errors = new List<double>[2];
                var pulleys = new[] { Tuple.Create((HtdPulleyComponent)f.Belt.Driver, f.DriverShaft, f.DriverMount), Tuple.Create((HtdPulleyComponent)f.Belt.Driven, f.DrivenShaft, f.DrivenMount) };
                for (var n = 0; n < 2; n++)
                {
                    var pulley = pulleys[n].Item1;
                    var placement = AlibreGearExporter.GetPartPlacement(pulleys[n].Item2, pulleys[n].Item3);
                    var sketch = PulleySketchBuilder.Build(pulley);
                    var bottom = sketch.Tooth.Figures.Single(fig => fig.Name == "Bottom");
                    var groove = bottom.Points[bottom.Points.Count / 2];
                    var centre = placement.TransformPoint(new Vector3D(0.0, 0.0, 0.0));
                    var groove0 = placement.TransformPoint(new Vector3D(groove.X / k, groove.Y / k, 0.0));
                    var step = 2.0 * Math.PI / pulley.Teeth;
                    var groove0Angle = Math.Atan2(groove0.Y - centre.Y, groove0.X - centre.X);
                    centres.Add(centre);

                    errors[n] = new List<double>();
                    var other = pulleys[1 - n].Item2.Placement.ToTransform().TransformPoint(new Vector3D(0.0, 0.0, 0.0));
                    foreach (var tooth in teeth)
                    {
                        var dx = tooth.X - centre.X;
                        var dy = tooth.Y - centre.Y;
                        var radius = Math.Sqrt(dx * dx + dy * dy);
                        var toOther = dx * (other.X - centre.X) + dy * (other.Y - centre.Y);
                        var distance = Math.Sqrt((other.X - centre.X) * (other.X - centre.X) + (other.Y - centre.Y) * (other.Y - centre.Y));
                        if (radius > 30.0 || toOther > -0.2 * radius * distance) continue;
                        errors[n].Add(Wrap(Math.Atan2(dy, dx) - groove0Angle, step));
                    }
                }

                for (var n = 0; n < 2; n++)
                {
                    Assert.Greater(errors[n].Count, 5, $"rotation {rotation}: teeth on pulley {n}");
                    Assert.LessOrEqual(errors[n].Max(Math.Abs), 1e-6, $"rotation {rotation}: every belt tooth of the sketch is on an exported groove of pulley {n}.");
                }
            }
        }

        #endregion
    }
}
