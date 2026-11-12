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
    /// <summary>ISO 606 roller chains between two sprockets: link count, placement, roller layout, shape and saving.</summary>
    [TestFixture]
    public class TestRollerChain
    {
        private static GearAssembly TwoSprockets(out SprocketComponent a, out SprocketComponent b, out RollerChain chain, double distance = 300.0,
            int teethA = 19, int teethB = 38, ChainSeries series = ChainSeries.B08)
        {
            a = new SprocketComponent("A") { Series = series, Teeth = teethA };
            b = new SprocketComponent("B") { Series = series, Teeth = teethB };
            var assembly = new GearAssembly();
            var shaftA = new Shaft("SA");
            shaftA.Mount(a, 0.0);
            var shaftB = new Shaft("SB");
            shaftB.Mount(b, 0.0);
            assembly.AddShaft(shaftA);
            assembly.AddShaft(shaftB);
            chain = new RollerChain("C1", a, b, distance);
            assembly.AddChain(chain);
            return assembly;
        }

        private static double Radius(GearPoint p) => Math.Sqrt(p.X * p.X + p.Y * p.Y);

        [Test]
        public void TestChain_EqualSprockets_LinkCountIsEvenAndFollowsTheChainLengthFormula()
        {
            TwoSprockets(out _, out _, out var chain, 300.0, 19, 19);

            var expected = 2.0 * 300.0 / 12.7 + 19.0; // 66.24 pitches
            Assert.AreEqual(66, chain.LinkCount);
            Assert.AreEqual(0, chain.LinkCount % 2);
            Assert.AreEqual(expected, chain.LinkCount, 1.0);
            Assert.AreEqual(66 * 12.7, chain.ChainLength, 1e-9);
            Assert.AreEqual(180.0, chain.SmallSprocketWrapDegrees, 1e-9, "Equal sprockets are wrapped over a half turn.");
            Assert.AreEqual(1.0, chain.GearRatio, 1e-12);
            Assert.AreEqual(2.0 * 300.0 + Math.PI * chain.Driver.PitchDiameter, chain.PitchPathLength, 1e-9);
        }

        [Test]
        public void TestChain_UnequalSprockets_HaveALongerChainAndASmallerWrap()
        {
            TwoSprockets(out var small, out var large, out var chain, 300.0, 17, 57);
            var equal = 2.0 * 300.0 / small.Profile.Pitch + (17 + 57) / 2.0;

            Assert.AreEqual(0, chain.LinkCount % 2);
            Assert.Greater(chain.LinkCount, equal - 1.0);
            Assert.Less(chain.SmallSprocketWrapDegrees, 180.0);
            Assert.AreEqual(57.0 / 17.0, chain.GearRatio, 1e-12);

            var alpha = Math.Asin((large.PitchDiameter - small.PitchDiameter) / (2.0 * 300.0));
            Assert.AreEqual(180.0 - 2.0 * alpha * 180.0 / Math.PI, chain.SmallSprocketWrapDegrees, 1e-9);
        }

        [Test]
        public void TestChain_Validate_RejectsDifferentSeriesAndSprocketsThatTouch()
        {
            var a = new SprocketComponent("A") { Series = ChainSeries.B08, Teeth = 19 };
            var b = new SprocketComponent("B") { Series = ChainSeries.B10, Teeth = 19 };

            Assert.Throws<InvalidOperationException>(() => new RollerChain("X", a, b, 300.0).Validate(), "An 08B and a 10B sprocket cannot share a chain.");

            b.Series = ChainSeries.B08;
            var close = new RollerChain("Y", a, b, a.OutsideDiameter - 1.0);
            Assert.Throws<InvalidOperationException>(() => close.Validate());
            Assert.Throws<ArgumentException>(() => new RollerChain("Z", a, a, 100.0), "A chain needs two different sprockets.");
            Assert.Throws<ArgumentException>(() => new RollerChain(" ", a, b, 100.0));
        }

        [Test]
        public void TestChain_AddChain_ChecksTheAssembly()
        {
            var assembly = TwoSprockets(out var a, out var b, out _);

            var third = new SprocketComponent("C") { Teeth = 19 };
            Assert.Throws<InvalidOperationException>(() => assembly.AddChain(new RollerChain("C2", a, third, 300.0)), "An unmounted sprocket.");
            Assert.Throws<InvalidOperationException>(() => assembly.AddChain(new RollerChain("C3", a, b, 300.0)), "The driven sprocket already has a chain.");
            Assert.Throws<ArgumentException>(() => assembly.AddChain(new RollerChain("C1", b, a, 300.0)), "Duplicate identifier.");

            var sameShaft = new GearAssembly();
            var shaft = new Shaft("S");
            var p = new SprocketComponent("P") { Teeth = 19 };
            var q = new SprocketComponent("Q") { Teeth = 19 };
            shaft.Mount(p, 0.0);
            shaft.Mount(q, 30.0);
            sameShaft.AddShaft(shaft);
            Assert.Throws<InvalidOperationException>(() => sameShaft.AddChain(new RollerChain("C4", p, q, 300.0)));
        }

        [Test]
        public void TestChain_Solve_PlacesTheDrivenShaftAtTheCentreDistanceAndTurnsItAtTheRatio()
        {
            var assembly = TwoSprockets(out var a, out var b, out var chain, 300.0, 19, 38);
            chain.CentreLineAngleDeg = 30.0;
            var driverShaft = assembly.Shafts[0];
            var drivenShaft = assembly.Shafts[1];

            assembly.Solve();

            var angle = 30.0 * Math.PI / 180.0;
            Assert.AreEqual(300.0 * Math.Cos(angle), drivenShaft.Placement.Origin.X, 1e-6);
            Assert.AreEqual(300.0 * Math.Sin(angle), drivenShaft.Placement.Origin.Y, 1e-6);
            Assert.AreSame(chain, assembly.FindDrivingChain(b));
            Assert.AreEqual(1, assembly.FindChains(a).Count());

            // Turning the driver turns the driven sprocket at the ratio of the teeth, in the same direction.
            var offset = drivenShaft.RotationRad;
            driverShaft.RotationRad = 0.5;
            assembly.Solve();
            Assert.AreEqual(offset + 0.5 / chain.GearRatio, drivenShaft.RotationRad, 1e-9);
        }

        [Test]
        public void TestChain_Solve_RegistersTheRollersInTheToothGapsOfBothSprockets()
        {
            foreach (var driverPhase in new[] { 0.0, 0.3, 1.1 })
            {
                var assembly = TwoSprockets(out var a, out var b, out var chain, 300.0, 19, 38);
                chain.CentreLineAngleDeg = 20.0;
                assembly.Shafts[0].RotationRad = driverPhase;
                assembly.Solve();

                var layout = RollerChainLayout.Build(chain, assembly.FindMount(a, out _).PhaseRad);
                var rA = a.PitchDiameter / 2.0;
                var rB = b.PitchDiameter / 2.0;
                var stepA = 2.0 * Math.PI / a.Teeth;
                var stepB = 2.0 * Math.PI / b.Teeth;
                var theta = 20.0 * Math.PI / 180.0;
                var centreB = new GearPoint(300.0 * Math.Cos(theta), 300.0 * Math.Sin(theta));
                var phaseA = assembly.FindMount(a, out _).PhaseRad;
                var phaseB = assembly.FindMount(b, out _).PhaseRad;

                double GapOffset(double angle, double phase, double step)
                {
                    var k = (angle - phase) / step;
                    return Math.Abs(k - Math.Round(k)) * step;
                }

                var onDriver = layout.Rollers.Where(p => Math.Abs(Radius(p) - rA) < 1e-6).ToList();
                Assert.IsNotEmpty(onDriver, "Some rollers are on the driver.");
                Assert.Less(onDriver.Min(p => GapOffset(Math.Atan2(p.Y, p.X), phaseA, stepA)), 1e-6, "A roller sits in a gap of the driver, phase " + driverPhase);

                var onDriven = layout.Rollers.Where(p => Math.Abs(Math.Sqrt(Math.Pow(p.X - centreB.X, 2) + Math.Pow(p.Y - centreB.Y, 2)) - rB) < 1e-6).ToList();
                Assert.IsNotEmpty(onDriven, "Some rollers are on the driven sprocket.");
                Assert.Less(onDriven.Min(p => GapOffset(Math.Atan2(p.Y - centreB.Y, p.X - centreB.X), phaseB, stepB)), 1e-6,
                    "A roller sits in a gap of the driven sprocket, phase " + driverPhase);
            }
        }

        [Test]
        public void TestChain_Layout_PutsEveryRollerOnThePathAtAboutOnePitchFromTheNext()
        {
            TwoSprockets(out var a, out var b, out var chain, 280.0, 17, 31);
            var layout = RollerChainLayout.Build(chain, 0.2);
            var rollers = layout.Rollers;
            var rA = a.PitchDiameter / 2.0;
            var rB = b.PitchDiameter / 2.0;
            var pitch = chain.Profile.Pitch;

            Assert.AreEqual(chain.LinkCount, rollers.Count);
            Assert.AreEqual(1.0, layout.Spacing, 0.03, "The rollers are about one pitch apart along the path.");
            Assert.AreEqual(layout.Registration.PathPitches / chain.LinkCount, layout.Spacing, 1e-12);

            for (var k = 0; k < rollers.Count; k++)
            {
                var next = rollers[(k + 1) % rollers.Count];
                var distance = Math.Sqrt(Math.Pow(next.X - rollers[k].X, 2) + Math.Pow(next.Y - rollers[k].Y, 2));
                Assert.AreEqual(pitch, distance, pitch * 0.04, "Rollers " + k + " and " + (k + 1) + " are about one pitch apart.");

                var onA = Math.Abs(Radius(rollers[k]) - rA) < 1e-6;
                var onB = Math.Abs(Math.Sqrt(Math.Pow(rollers[k].X - 280.0, 2) + Math.Pow(rollers[k].Y, 2)) - rB) < 1e-6;
                var onRun = IsOnATangentRun(rollers[k], rA, rB, 280.0);
                Assert.IsTrue(onA || onB || onRun, "Roller " + k + " lies on the path.");
            }
        }

        private static bool IsOnATangentRun(GearPoint p, double rA, double rB, double distance)
        {
            // A point on an external tangent line has the signed distance +-r from both centres' lines: n.p = rA for the line with normal n.
            var nx = (rA - rB) / distance;
            var ny = Math.Sqrt(1.0 - nx * nx);
            var upper = nx * p.X + ny * p.Y;
            var lower = nx * p.X - ny * p.Y;
            return Math.Abs(upper - rA) < 1e-6 || Math.Abs(lower - rA) < 1e-6;
        }

        [Test]
        public void TestChain_Mesh_HasRollersPinsAndAlternatingPlatesAndIsCentredOnTheMidPlane()
        {
            var assembly = TwoSprockets(out _, out _, out var chain, 300.0, 19, 19);
            assembly.Solve();
            var profile = chain.Profile;

            RollerChainMeshBuilder.Build(chain, Transform3D.Identity, out var mesh, out var polylines);

            var half = profile.InnerWidth / 2.0 + profile.InnerPlateThickness + profile.OuterPlateThickness;
            Assert.AreEqual(half, mesh.Vertices.Max(v => v.Z), 1e-9, "The outer plates are the widest part.");
            Assert.AreEqual(-half, mesh.Vertices.Min(v => v.Z), 1e-9);
            Assert.AreEqual(profile.OverallWidth, 2.0 * half, 1e-9);
            Assert.AreEqual(2 * chain.LinkCount, polylines.Count, "A plate and a roller outline for every link.");
            Assert.AreEqual(mesh.Vertices.Count, mesh.Normals.Count);

            // Inner plates lie against the rollers, outer plates outside them: both kinds have vertices at their own heights.
            var innerFace = profile.InnerWidth / 2.0 + profile.InnerPlateThickness;
            Assert.IsTrue(mesh.Vertices.Any(v => Math.Abs(v.Z - innerFace) < 1e-9));
            Assert.IsTrue(mesh.Vertices.Any(v => Math.Abs(v.Z - profile.InnerWidth / 2.0) < 1e-9));
        }

        [Test]
        public void TestChain_PlateOutline_IsAnAnticlockwiseStadium()
        {
            var outline = RollerChainMeshBuilder.PlateOutline(new GearPoint(0.0, 0.0), new GearPoint(10.0, 0.0), 8.0);

            Assert.Greater(SolidMeshBuilder.SignedArea(outline), 0.0);
            Assert.AreEqual(14.0, outline.Max(p => p.X), 1e-9, "The plate reaches one plate half height beyond the far pin.");
            Assert.AreEqual(-4.0, outline.Min(p => p.X), 1e-9);
            Assert.AreEqual(4.0, outline.Max(p => p.Y), 1e-9);
            Assert.AreEqual(-4.0, outline.Min(p => p.Y), 1e-9);
            Assert.AreEqual(10.0 * 8.0 + Math.PI * 16.0, SolidMeshBuilder.SignedArea(outline), 2.0, "A strip and two half discs.");
            Assert.Throws<InvalidOperationException>(() => RollerChainMeshBuilder.PlateOutline(new GearPoint(1.0, 1.0), new GearPoint(1.0, 1.0), 8.0));
        }

        [Test]
        public void TestChain_BuildsASceneForThe2DAnd3DViews()
        {
            var assembly = TwoSprockets(out _, out _, out var chain, 300.0, 19, 25);

            var scene = GearAssemblySceneBuilder.Build(assembly);

            Assert.AreEqual(2, scene.Items.Count);
            Assert.AreEqual(1, scene.ChainItems.Count);
            var item = scene.ChainItems[0];
            Assert.AreSame(chain, item.Chain);
            Assert.Greater(item.Mesh.Vertices.Count, 1000);
            Assert.AreEqual(2 * chain.LinkCount, item.Polylines.Count);
            Assert.AreEqual(0, scene.BeltItems.Count);
        }

        [Test]
        public void TestChain_IsKeptWhenTheAssemblyIsSavedAndLoaded()
        {
            var assembly = TwoSprockets(out var a, out _, out var chain, 310.0, 21, 42, ChainSeries.B12);
            chain.CentreLineAngleDeg = 15.0;
            chain.Name = "Primary chain";

            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            var copy = loaded.Chains.Single();
            Assert.AreEqual("C1", copy.Id);
            Assert.AreEqual("Primary chain", copy.Name);
            Assert.AreEqual(310.0, copy.CentreDistance, 1e-9);
            Assert.AreEqual(15.0, copy.CentreLineAngleDeg, 1e-9);
            Assert.AreEqual("A", copy.Driver.Id);
            Assert.AreEqual("B", copy.Driven.Id);
            Assert.AreEqual(chain.LinkCount, copy.LinkCount);
            Assert.AreEqual(ChainSeries.B12, copy.Driver.Series);
            Assert.AreEqual(a.PitchDiameter, copy.Driver.PitchDiameter, 1e-9);
        }

        [Test]
        public void TestChainSketch_Links_AreClosedStadiumsWithTheWidthsOfTheChain()
        {
            var assembly = TwoSprockets(out _, out _, out var chain, 300.0, 19, 38);
            assembly.Solve();
            var profile = chain.Profile;
            var unit = SingleToothSketchBuilder.MmToAlibreUnits;

            var plan = ChainSketchBuilder.Build(chain, 0.4);

            Assert.IsTrue(plan.IsClosed(), "Every plate outline is a closed loop.");
            Assert.AreEqual(chain.LinkCount, plan.LinkCount);
            Assert.AreEqual(4 * chain.LinkCount / 2, plan.InnerLinks.Count, "Four figures for each of the inner links.");
            Assert.AreEqual(4 * chain.LinkCount / 2, plan.OuterLinks.Count, "Four figures for each of the outer links.");
            Assert.AreEqual(chain.LinkCount, plan.Rollers.Count);
            Assert.AreEqual(chain.LinkCount, plan.Pins.Count);
            Assert.AreEqual(profile.RollerDiameter / 2.0 * unit, plan.Rollers[0].Radius, 1e-6);
            Assert.AreEqual(profile.PinDiameter / 2.0 * unit, plan.Pins[0].Radius, 1e-6);
            Assert.AreEqual(profile.OverallWidth * unit, plan.OverallWidth, 1e-6);
            Assert.AreEqual(profile.InnerWidth * unit, plan.RollerWidth, 1e-6);
            Assert.AreEqual((profile.InnerWidth + 2.0 * profile.InnerPlateThickness) * unit, plan.InnerLinkWidth, 1e-6);
            Assert.Less(plan.RollerWidth, plan.InnerLinkWidth);
            Assert.Less(plan.InnerLinkWidth, plan.OverallWidth);
            Assert.AreEqual($"RollerChain_08B_L{chain.LinkCount}", plan.PartName);

            foreach (var figure in plan.InnerLinks.Concat(plan.OuterLinks).Where(f => f.Kind == ToothSketchFigureKind.Arc))
            {
                var start = Math.Sqrt(Math.Pow(figure.StartX - figure.CentreX, 2) + Math.Pow(figure.StartY - figure.CentreY, 2));
                var end = Math.Sqrt(Math.Pow(figure.EndX - figure.CentreX, 2) + Math.Pow(figure.EndY - figure.CentreY, 2));
                Assert.AreEqual(profile.PlateHeight / 2.0 * unit, start, 1e-6, figure.Name + ": the plate ends are half circles round the pins");
                Assert.AreEqual(start, end, 1e-6);
            }
        }

        [Test]
        public void TestChainSketch_PlatesOfTheSameKind_NeverOverlapInOneSketch()
        {
            foreach (var series in new[] { ChainSeries.B06, ChainSeries.B12, ChainSeries.B24, ChainSeries.B40 })
            {
                var assembly = TwoSprockets(out _, out _, out var chain, 6.0 * ChainProfile.Get(series).Pitch * 12.0, 17, 25, series);
                assembly.Solve();

                var rollers = RollerChainLayout.Build(chain, 0.0).Rollers;
                var count = rollers.Count;
                var height = chain.Profile.PlateHeight;

                // Link k joins rollers k and k + 1. Links of the same kind are two apart, so rollers k + 1 and k + 2 are the nearest pins of two such links.
                for (var k = 0; k < count; k++)
                {
                    var a = rollers[(k + 1) % count];
                    var b = rollers[(k + 2) % count];
                    var distance = Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
                    Assert.Greater(distance, height, series + ": the half circles of neighbouring plates of one kind do not touch at roller " + k);
                }
            }
        }

        [Test]
        public void TestChainExport_IsSelectedAndPlacedLikeABelt()
        {
            var assembly = TwoSprockets(out _, out _, out var chain, 300.0, 19, 38);
            assembly.Shafts[0].Mounts[0].AxialOffset = 4.0;

            var parts = AlibreGearExporter.GetChainAssemblyParts(assembly);

            Assert.AreEqual(1, parts.Count);
            Assert.AreSame(chain, parts[0].Key);
            var origin = parts[0].Value.TransformPoint(new Vector3D(0.0, 0.0, 0.0));
            Assert.AreEqual(4.0, origin.Z, 1e-9, "The chain lies on the mid-plane of the driver sprocket.");
            Assert.AreEqual(0.0, origin.X, 1e-9);

            Assert.AreEqual(0, AlibreGearExporter.GetChainAssemblyParts(assembly, new object[] { assembly.Shafts[0] }).Count);
            Assert.AreEqual(1, AlibreGearExporter.GetChainAssemblyParts(assembly, new object[] { chain }).Count);
            Assert.AreEqual(2, AlibreGearExporter.GetAssemblyParts(assembly).Count, "The sprockets are parts of their own.");
            Assert.AreEqual(1, AlibreGearExporter.GetBeltAssemblyParts(assembly).Count + 1, "Belts and chains are separate lists.");
        }

        [Test]
        public void TestChain_IsRemovedWithItsSprocket()
        {
            var assembly = TwoSprockets(out var a, out var b, out _);

            Assert.IsTrue(assembly.RemoveGear(b));

            Assert.AreEqual(0, assembly.Chains.Count);
            Assert.IsNull(assembly.FindDrivingChain(b));
            Assert.AreEqual(0, assembly.FindChains(a).Count());
        }

        [Test]
        public void TestChain_RemoveChain_LeavesTheSprockets()
        {
            var assembly = TwoSprockets(out _, out _, out var chain);

            Assert.IsTrue(assembly.RemoveChain(chain));
            Assert.IsFalse(assembly.RemoveChain(chain));
            Assert.AreEqual(2, assembly.Components.Count());
        }
    }
}
