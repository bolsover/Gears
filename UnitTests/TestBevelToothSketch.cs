using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>The Tredgold valley sketch on the back cone of the 18 tooth bevel gear blank and the teeth cut with it.</summary>
    [TestFixture]
    public class TestBevelToothSketch
    {
        private const double Module = 10.16;
        private const double FaceWidth = 63.5;

        private static BevelBlankProfile Sample() => new BevelBlankProfile(18, 45, Module, FaceWidth);

        [Test]
        public void TestVirtualGear_FollowsTredgold()
        {
            var profile = Sample();
            var sketch = new BevelToothSketch(profile);

            Assert.AreEqual(91.44 / Math.Cos(profile.PitchConeAngleRad), sketch.ConeRadius, 1e-9, "The back cone distance is the pitch radius over cos(delta).");
            Assert.AreEqual(18.0 / Math.Cos(profile.PitchConeAngleRad), sketch.VirtualTeeth, 1e-9);
            Assert.AreEqual(sketch.ConeRadius - 1.25 * Module, sketch.RootRadius, 1e-9);
            Assert.AreEqual(sketch.ConeRadius + Module, sketch.TipRadius, 1e-9);
            Assert.AreEqual(sketch.ConeRadius * Math.Cos(20.0 * Math.PI / 180.0), sketch.BaseRadius, 1e-9);
            Assert.GreaterOrEqual(sketch.OuterRadius, sketch.TipRadius, "The closing arc is at least the tooth outside radius.");
        }

        [Test]
        public void TestValley_IsSymmetricAndHalfThePitchAtThePitchRadius()
        {
            var sketch = new BevelToothSketch(Sample());

            Assert.AreEqual(Math.PI / (2.0 * sketch.VirtualTeeth), sketch.ValleyHalfAngle(sketch.ConeRadius), 1e-12, "Space width equals tooth thickness at the pitch circle.");
            Assert.AreEqual(sketch.ValleyHalfAngle(sketch.ConeRadius), sketch.ToothHalfAngle(sketch.ConeRadius), 1e-12);
            Assert.AreEqual(sketch.RightFlank.Count, sketch.LeftFlank.Count);
            for (var i = 0; i < sketch.RightFlank.Count; i++)
            {
                Assert.AreEqual(sketch.RightFlank[i].Radius, sketch.LeftFlank[i].Radius, 1e-12);
                Assert.AreEqual(sketch.RightFlank[i].Angle, -sketch.LeftFlank[i].Angle, 1e-12);
            }

            Assert.Greater(sketch.ToothHalfAngle(sketch.TipRadius), 0.0, "The teeth have a land at the tip.");
        }

        [Test]
        public void TestFlanks_AreInvolutesOfTheBaseCircle()
        {
            var sketch = new BevelToothSketch(Sample(), 20.0, 60);

            // The normal of an involute is tangent to the base circle: its distance from the centre is the base radius.
            var flank = sketch.RightFlank;
            for (var i = 0; i + 1 < flank.Count; i++)
            {
                double dx = flank[i + 1].X - flank[i].X, dy = flank[i + 1].Y - flank[i].Y;
                var length = Math.Sqrt(dx * dx + dy * dy);
                double mx = 0.5 * (flank[i].X + flank[i + 1].X), my = 0.5 * (flank[i].Y + flank[i + 1].Y);
                var distance = Math.Abs(mx * dx + my * dy) / length;
                Assert.AreEqual(sketch.BaseRadius, distance, sketch.BaseRadius * 2e-3, "Flank segment " + i);
            }
        }

        [Test]
        public void TestSketch_IsClosedAndRunsFromRootToOutsideTheTips()
        {
            var sketch = new BevelToothSketch(Sample());
            var loop = sketch.Loop;

            Assert.AreEqual(sketch.RootRadius, loop.Min(p => p.Radius), 1e-9);
            Assert.AreEqual(sketch.OuterRadius, loop.Max(p => p.Radius), 1e-9);
            Assert.That(sketch.OuterArc.All(p => Math.Abs(p.Radius - sketch.OuterRadius) < 1e-9), "The outer part is an arc.");
            Assert.That(sketch.RootArc.All(p => Math.Abs(p.Radius - sketch.RootRadius) < 1e-9), "The bottom is a root arc.");
            Assert.AreEqual(sketch.RightFlank.Last().Angle, sketch.OuterArc.First().Angle, 1e-12, "The outer arc links the flank end points.");
            Assert.AreEqual(sketch.LeftFlank.Last().Angle, sketch.OuterArc.Last().Angle, 1e-12);

            // The loop encloses a positive area with no repeated points.
            var area = 0.0;
            for (var i = 0; i < loop.Count; i++)
            {
                var a = loop[i];
                var b = loop[(i + 1) % loop.Count];
                Assert.Greater(Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y), 1e-9);
                area += a.X * b.Y - b.X * a.Y;
            }

            Assert.AreNotEqual(0.0, area);
        }

        [TestCase(20, 20, 3.0, false)]
        [TestCase(45, 18, Module, false)]
        [TestCase(18, 45, Module, true)]
        public void TestRootFillets_AreTrochoidsOfTheRackTipCorner(int teeth, int mateTeeth, double module, bool undercut)
        {
            var sketch = new BevelToothSketch(new BevelBlankProfile(teeth, mateTeeth, module, 20.0));
            Assert.AreEqual(undercut, sketch.IsUndercut);

            var alpha = 20.0 * Math.PI / 180.0;
            var depth = sketch.RackDepth;
            var flat = Math.PI * module / 4.0 - depth * Math.Tan(alpha);
            Assert.AreEqual(flat / sketch.ConeRadius, sketch.RootHalfAngle, 1e-12, "The root arc is the flat of the rack tip.");

            // Every fillet point is where the rack corner is when the rack has rolled by t: the rack frame rotated about the pitch point.
            var rootRadius = sketch.ConeRadius - depth;
            foreach (var p in sketch.RightFillet.Take(sketch.RightFillet.Count - 1))
            {
                // The corner is 'along' from the pitch point along the tangent, the rack has rolled to t = (flat - along) / R.
                var along = Math.Sqrt(Math.Max(0.0, p.Radius * p.Radius - rootRadius * rootRadius));
                var t = (flat - along) / sketch.ConeRadius;
                var x = rootRadius * Math.Cos(t) - along * Math.Sin(t);
                var y = rootRadius * Math.Sin(t) + along * Math.Cos(t);
                Assert.AreEqual(p.Radius, Math.Sqrt(x * x + y * y), 1e-9, "Fillet point " + p + " radius.");
                Assert.AreEqual(p.Angle, Math.Atan2(y, x), 1e-9, "Fillet point " + p + " lies on the rack corner path.");
            }

            // Starts on the root circle where the root arc ends, tangentially, and rises to the flank.
            Assert.AreEqual(sketch.RootRadius, sketch.RightFillet.First().Radius, 1e-9);
            Assert.AreEqual(sketch.RootArc.Last().Angle, sketch.RightFillet.First().Angle, 1e-12);
            Assert.AreEqual(sketch.FlankStartRadius, sketch.RightFillet.Last().Radius, 1e-12);
            Assert.AreEqual(sketch.RightFlank.First().Angle, sketch.RightFillet.Last().Angle, 1e-12, "The fillet runs into the involute.");
            Assert.Greater(sketch.FlankStartRadius, sketch.RootRadius);
            for (var i = 1; i < sketch.RightFillet.Count; i++)
            {
                Assert.Greater(sketch.RightFillet[i].Radius, sketch.RightFillet[i - 1].Radius, "The fillet rises.");
                Assert.AreEqual(sketch.RightFillet[i].Radius, sketch.LeftFillet[i].Radius, 1e-12);
                Assert.AreEqual(sketch.RightFillet[i].Angle, -sketch.LeftFillet[i].Angle, 1e-12);
            }

            // Not an arc: a trochoid is not a circle, so no radial sides and no constant radius.
            Assert.Greater(sketch.RightFillet.Last().Radius - sketch.RightFillet.First().Radius, 1e-3);
        }

        [Test]
        public void TestRootFillet_MeetsTheInvoluteTangentiallyWhenThereIsNoUndercut()
        {
            var sketch = new BevelToothSketch(new BevelBlankProfile(45, 18, Module, 20.0));
            Assert.IsFalse(sketch.IsUndercut);

            // The corner is on the line of action depth / tan(alpha) from the pitch point, where the trochoid joins the involute.
            var alpha = 20.0 * Math.PI / 180.0;
            var along = sketch.RackDepth / Math.Tan(alpha);
            var expectedRadius = Math.Sqrt(sketch.RootRadius * sketch.RootRadius + along * along);
            Assert.AreEqual(expectedRadius, sketch.FlankStartRadius, 1e-9);
            var angle = (sketch.RootHalfAngle * sketch.ConeRadius - along) / sketch.ConeRadius + Math.Atan2(along, sketch.RootRadius);
            Assert.AreEqual(sketch.ValleyHalfAngle(sketch.FlankStartRadius), angle, 1e-9, "The trochoid and the involute agree where they join.");
        }

        [Test]
        public void TestRootFillet_UndercutEndsWhereItCrossesTheInvolute()
        {
            var sketch = new BevelToothSketch(Sample());
            Assert.IsTrue(sketch.IsUndercut);

            var alpha = 20.0 * Math.PI / 180.0;
            var along = Math.Sqrt(sketch.FlankStartRadius * sketch.FlankStartRadius - sketch.RootRadius * sketch.RootRadius);
            var angle = (sketch.RootHalfAngle * sketch.ConeRadius - along) / sketch.ConeRadius + Math.Atan2(along, sketch.RootRadius);
            Assert.AreEqual(sketch.ValleyHalfAngle(sketch.FlankStartRadius), angle, 1e-9, "The fillet crosses the involute at the start of the flank.");
            Assert.GreaterOrEqual(sketch.FlankStartRadius, sketch.BaseRadius - 1e-9);
            Assert.LessOrEqual(along, sketch.RackDepth / Math.Tan(alpha) + 1e-9);
        }

        private static double VolumeOf(Bolsover.Gears.Calculator.GearMesh3DBuilder.Mesh3D mesh)
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

        [TestCase(18, 45)]
        [TestCase(45, 18)]
        [TestCase(30, 30)]
        public void TestGleason_ToothThicknessAtThePitchCircleFollowsTheProfileShift(int teeth, int mateTeeth)
        {
            var profile = new BevelBlankProfile(teeth, mateTeeth, Module, FaceWidth, bevelType: BevelGearType.Gleason);
            var sketch = new BevelToothSketch(profile);
            var alpha = 20.0 * Math.PI / 180.0;

            Assert.AreEqual(profile.ProfileShift, sketch.ProfileShift, 1e-12);
            Assert.AreEqual(sketch.ConeRadius - profile.Dedendum, sketch.RootRadius, 1e-9);
            Assert.AreEqual(sketch.ConeRadius + profile.Addendum, sketch.TipRadius, 1e-9);

            var tooth = 2.0 * sketch.ToothHalfAngle(sketch.ConeRadius) * sketch.ConeRadius;
            var space = 2.0 * sketch.ValleyHalfAngle(sketch.ConeRadius) * sketch.ConeRadius;
            Assert.AreEqual(Math.PI * Module / 2.0 + 2.0 * profile.ProfileShift * Math.Tan(alpha), tooth, 1e-9, "Thickness m (pi/2 + 2 x tan a).");
            Assert.AreEqual(Math.PI * Module, tooth + space, 1e-9, "Tooth and space make up the circular pitch.");
        }

        [Test]
        public void TestGleason_ThePinionToothIsThickerThanTheGearTooth()
        {
            var pinion = new BevelToothSketch(new BevelBlankProfile(18, 45, Module, FaceWidth, bevelType: BevelGearType.Gleason));
            var gear = new BevelToothSketch(new BevelBlankProfile(45, 18, Module, FaceWidth, bevelType: BevelGearType.Gleason));

            var pinionTooth = 2.0 * pinion.ToothHalfAngle(pinion.ConeRadius) * pinion.ConeRadius;
            var gearTooth = 2.0 * gear.ToothHalfAngle(gear.ConeRadius) * gear.ConeRadius;
            Assert.Greater(pinionTooth, gearTooth);
            Assert.AreEqual(Math.PI * Module, pinionTooth + gearTooth, 1e-9, "Equal and opposite shifts: the two teeth add up to the circular pitch.");
        }

        [TestCase(18, 45)]
        [TestCase(45, 18)]
        [TestCase(24, 24)]
        public void TestGleason_RootFilletsStillStartAtTheRackFlatAndJoinTheInvolute(int teeth, int mateTeeth)
        {
            var sketch = new BevelToothSketch(new BevelBlankProfile(teeth, mateTeeth, Module, FaceWidth, bevelType: BevelGearType.Gleason));
            var alpha = 20.0 * Math.PI / 180.0;

            var flat = Math.PI * Module / 4.0 - (sketch.ProfileShift + sketch.RackDepth) * Math.Tan(alpha);
            Assert.AreEqual(flat / sketch.ConeRadius, sketch.RootHalfAngle, 1e-12);
            Assert.AreEqual(sketch.RootArc.Last().Angle, sketch.RightFillet.First().Angle, 1e-12);
            Assert.AreEqual(sketch.RightFlank.First().Angle, sketch.RightFillet.Last().Angle, 1e-12);
            Assert.AreEqual(sketch.ValleyHalfAngle(sketch.FlankStartRadius), sketch.RightFillet.Last().Angle, 1e-12);
            Assert.Greater(sketch.FlankStartRadius, sketch.RootRadius);
            Assert.Less(sketch.FlankStartRadius, sketch.TipRadius);
        }

        [TestCase(45, 18, 0.25, false)]
        [TestCase(45, 18, 0.38, false)]
        [TestCase(18, 45, 0.25, false)]
        [TestCase(24, 24, 0.3, false)]
        public void TestRoundedRackTip_FilletIsTheOffsetOfTheArcCentreTrochoid(int teeth, int mateTeeth, double factor, bool undercut)
        {
            var profile = new BevelBlankProfile(teeth, mateTeeth, Module, FaceWidth);
            var sharp = new BevelToothSketch(profile);
            var sketch = new BevelToothSketch(profile, 20.0, 12, factor);
            var rho = factor * Module;
            var alpha = 20.0 * Math.PI / 180.0;
            var radius = sketch.ConeRadius;
            Assert.AreEqual(rho, sketch.RackTipRadius, 1e-12);
            Assert.AreEqual(undercut, sketch.IsUndercut);

            // The arc centre is a tip radius above the tip and a tangent length in from the sharp corner.
            var flat = Math.PI * Module / 4.0 - sketch.RackDepth * Math.Tan(alpha);
            var centreU = flat - rho * (1.0 - Math.Sin(alpha)) / Math.Cos(alpha);
            var centreV = sketch.RackDepth - rho;
            Assert.AreEqual(centreU / radius, sketch.RootHalfAngle, 1e-12, "The flat of the rounded tip is shorter.");
            Assert.Less(sketch.RootHalfAngle, sharp.RootHalfAngle);

            var centres = new List<BevelToothPoint>();
            var end = centreV / Math.Tan(alpha) * 1.3;
            for (var i = 0; i <= 4000; i++)
            {
                var along = end * i / 4000.0;
                centres.Add(new BevelToothPoint(Math.Sqrt(Math.Pow(radius - centreV, 2.0) + along * along), (centreU - along) / radius + Math.Atan2(along, radius - centreV)));
            }

            Assert.AreEqual(sketch.RootRadius, sketch.RightFillet.First().Radius, 1e-9);
            foreach (var p in sketch.RightFillet.Take(sketch.RightFillet.Count - 1))
            {
                var nearest = centres.Min(c => Math.Sqrt(Math.Pow(c.X - p.X, 2.0) + Math.Pow(c.Y - p.Y, 2.0)));
                Assert.AreEqual(rho, nearest, 0.02 * rho + 1e-3, "Fillet point " + p + " is a tip radius from the arc centre path.");
            }

            Assert.AreEqual(sketch.RightFlank.First().Angle, sketch.RightFillet.Last().Angle, 1e-12);
            for (var i = 1; i < sketch.RightFillet.Count; i++)
            {
                Assert.Greater(sketch.RightFillet[i].Radius, sketch.RightFillet[i - 1].Radius, "The fillet rises.");
            }
        }

        [Test]
        public void TestRoundedRackTip_JoinsTheInvoluteTangentiallyWhenThereIsNoUndercut()
        {
            var sketch = new BevelToothSketch(new BevelBlankProfile(45, 18, Module, FaceWidth), 20.0, 12, 0.3);
            Assert.IsFalse(sketch.IsUndercut);

            // The last fillet segment runs the same way as the first flank segment, so there is no kink.
            var f = sketch.RightFillet;
            var k = sketch.RightFlank;
            var fillet = Math.Atan2(f[f.Count - 1].Y - f[f.Count - 2].Y, f[f.Count - 1].X - f[f.Count - 2].X);
            var flank = Math.Atan2(k[1].Y - k[0].Y, k[1].X - k[0].X);
            Assert.AreEqual(flank, fillet, 10.0 * Math.PI / 180.0, "No kink where the fillet meets the flank.");
        }

        [TestCase(18, 45, BevelGearType.Standard, 0.25)]
        [TestCase(45, 18, BevelGearType.Standard, 0.38)]
        [TestCase(18, 45, BevelGearType.Gleason, 0.0)]
        [TestCase(45, 18, BevelGearType.Gleason, 0.0)]
        [TestCase(18, 45, BevelGearType.Gleason, 0.25)]
        [TestCase(45, 18, BevelGearType.Gleason, 0.25)]
        public void TestToothedSolid_ForGleasonAndRoundedTipIsAClosedSolidBetweenTheRootAndTheBlank(int teeth, int mateTeeth, BevelGearType type, double factor)
        {
            var profile = new BevelBlankProfile(teeth, mateTeeth, Module, FaceWidth, bevelType: type);
            var sketch = new BevelToothSketch(profile, 20.0, 12, factor);

            var all = VolumeOf(profile.BuildToothedMesh(sketch));
            var allButOne = VolumeOf(profile.BuildToothedMesh(sketch, teeth - 1));
            var blank = VolumeOf(profile.BuildMesh(720));
            Assert.Greater(all, 0.0, "Faces point outwards.");
            Assert.Less(all, blank);
            Assert.Greater(allButOne, all, "Every valley removes material.");
            Assert.AreEqual(blank - all, teeth * (allButOne - all), 0.01 * (blank - all), "One valley is 1/z of the cut.");
        }

        [Test]
        public void TestRoundedRackTip_BadValuesAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new BevelToothSketch(Sample(), 20.0, 12, -0.1));
            Assert.Throws<ArgumentException>(() => new BevelToothSketch(Sample(), 20.0, 12, 1.0));
            Assert.Throws<InvalidOperationException>(() => new BevelToothSketch(new BevelBlankProfile(45, 18, Module, FaceWidth), 20.0, 12, 0.9), "The rounded corners would meet.");
        }

        [Test]
        public void TestSketchPointsLieOnTheBackCone()
        {
            var profile = Sample();
            var sketch = new BevelToothSketch(profile);
            var cosDelta = Math.Cos(profile.PitchConeAngleRad);

            foreach (var p in sketch.Loop)
            {
                var onCone = profile.BackConePoint(p.Radius);
                Assert.AreEqual(p.Radius, (onCone - profile.PointC).Length, 1e-9, "The point is its radius from pointC along refc.");
                var point = BevelBlankProfile.Revolve(onCone, p.Angle / cosDelta);
                Assert.AreEqual(Math.Sqrt(point.Y * point.Y + point.Z * point.Z), onCone.Y, 1e-9);
            }

            Assert.AreEqual(profile.PointD.X, profile.BackConePoint(sketch.RootRadius).X, 1e-9);
            Assert.AreEqual(profile.PointE.Y, profile.BackConePoint(sketch.TipRadius).Y, 1e-9);
        }

        [Test]
        public void TestToeSketchIsTheHeelSketchScaledAboutTheApex()
        {
            var profile = Sample();

            var scaledD = profile.ToToe(profile.PointD);
            var scaledE = profile.ToToe(profile.PointE);
            Assert.AreEqual(profile.PointH.X, scaledD.X, 1e-9, "The root of the toe is pointH.");
            Assert.AreEqual(profile.PointH.Y, scaledD.Y, 1e-9);
            Assert.AreEqual(profile.PointG.X, scaledE.X, 1e-9, "The tip of the toe is pointG.");
            Assert.AreEqual(profile.PointG.Y, scaledE.Y, 1e-9);
            Assert.Less(profile.ToeScale, 1.0);
        }

        [Test]
        public void TestOutline_PatternsTheValleyAroundAFullTurn()
        {
            var profile = Sample();
            var sketch = new BevelToothSketch(profile);
            var outline = sketch.BuildOutline(profile.Teeth);

            Assert.AreEqual(sketch.RootRadius, outline.Min(p => p.Radius), 1e-9);
            Assert.AreEqual(sketch.TipRadius, outline.Max(p => p.Radius), 1e-9);
            var width = 0.5 * sketch.PitchAngle;
            for (var i = 1; i < outline.Count; i++)
            {
                Assert.Less(Math.Abs(outline[i].Angle - outline[i - 1].Angle), width, "The outline runs round once.");
            }

            var turn = profile.Teeth * sketch.PitchAngle;
            Assert.AreEqual(2.0 * Math.PI * Math.Cos(profile.PitchConeAngleRad), turn, 1e-9, "The developed turn is 2 pi cos(delta).");
            Assert.Less(outline.Last().Angle - outline.First().Angle, turn);

            var rootArcs = outline.Count(p => Math.Abs(p.Radius - sketch.RootRadius) < 1e-9);
            Assert.AreEqual(profile.Teeth * sketch.RootArc.Count, rootArcs);
            Assert.Throws<ArgumentException>(() => sketch.BuildOutline(0));
            Assert.Throws<ArgumentException>(() => sketch.BuildOutline(profile.Teeth + 1));
        }

        [Test]
        public void TestToothedSolid_IsClosedWithOutwardFacesAndBetweenTheRootAndTheBlank()
        {
            var profile = Sample();
            var sketch = new BevelToothSketch(profile);

            var teeth = profile.BuildToothedMesh(sketch);
            AssertMeshIsClosed(teeth);
            var volume = SignedVolume(teeth);
            Assert.Greater(volume, 0.0, "Faces must point outwards.");
            Assert.AreEqual(teeth.Vertices.Count, teeth.Normals.Count);

            var blank = SignedVolume(profile.BuildMesh(360));
            var root = RevolvedVolume(new[] { profile.PointF, profile.PointD, profile.PointH, profile.PointI });
            Assert.Less(volume, blank);
            Assert.Greater(volume, root);
            var fraction = (volume - root) / (blank - root);
            Assert.That(fraction, Is.InRange(0.35, 0.65), "About half of the depth between the root and tip cones is tooth.");
        }

        [Test]
        public void TestOneValley_RemovesJustTheVolumeOfOneValley()
        {
            var profile = Sample();
            var sketch = new BevelToothSketch(profile);

            var one = profile.BuildToothedMesh(sketch, 1);
            var all = profile.BuildToothedMesh(sketch);
            AssertMeshIsClosed(one);
            var blank = SignedVolume(profile.BuildMesh(720));
            var cutOne = blank - SignedVolume(one);
            var cutAll = blank - SignedVolume(all);

            Assert.Greater(cutOne, 0.0);
            Assert.AreEqual(cutAll / profile.Teeth, cutOne, cutOne * 0.05, "The cut is patterned: every valley removes the same volume.");
        }

        [Test]
        public void TestToothedSolid_StaysWithinTheBlankAndUsesTheRootAndTipCones()
        {
            var profile = Sample();
            var mesh = profile.BuildToothedMesh(new BevelToothSketch(profile));

            var tip = mesh.Vertices.Max(v => Math.Sqrt(v.Y * v.Y + v.Z * v.Z));
            Assert.AreEqual(profile.PointE.Y, tip, 1e-6, "The tips reach the blank at pointE.");
            Assert.AreEqual(profile.PointG.X, mesh.Vertices.Min(v => v.X), 1e-6);
            Assert.AreEqual(profile.PointD.X, mesh.Vertices.Max(v => v.X), 1e-6);

            // Every vertex lies between the root cone line and the tip cone line (as seen from the apex).
            foreach (var v in mesh.Vertices)
            {
                var radial = Math.Sqrt(v.Y * v.Y + v.Z * v.Z);
                if (radial < 1e-9) continue;
                var slope = radial / (v.X - profile.PointB.X);
                var rootSlope = profile.PointD.Y / (profile.PointD.X - profile.PointB.X);
                var tipSlope = profile.PointE.Y / (profile.PointE.X - profile.PointB.X);
                Assert.GreaterOrEqual(tipSlope + 1e-9, Math.Min(slope, tipSlope));
                if (v.X > profile.PointF.X - 1e-9) continue;
                Assert.LessOrEqual(slope, tipSlope + 1e-9);
                Assert.Greater(rootSlope, 0.0);
            }
        }

        [TestCase(20, 20, 90.0)]
        [TestCase(45, 18, 90.0)]
        [TestCase(24, 36, 75.0)]
        public void TestOtherGears_AreClosedSolids(int teeth, int mateTeeth, double shaftAngleDeg)
        {
            var profile = new BevelBlankProfile(teeth, mateTeeth, 3.0, 15.0, shaftAngleDeg);
            var mesh = profile.BuildToothedMesh(new BevelToothSketch(profile));

            Assert.Greater(SignedVolume(mesh), 0.0);
            AssertMeshIsClosed(mesh);
        }

        [Test]
        public void TestBadInputs_AreRejected()
        {
            var profile = Sample();
            Assert.Throws<ArgumentNullException>(() => new BevelToothSketch(null));
            Assert.Throws<ArgumentException>(() => new BevelToothSketch(profile, 0.0));
            Assert.Throws<ArgumentException>(() => new BevelToothSketch(profile, 20.0, 1));
            Assert.Throws<ArgumentNullException>(() => profile.BuildToothedMesh(null));
            Assert.Throws<ArgumentException>(() => profile.BuildToothedMesh(new BevelToothSketch(Sample())), "A sketch of another blank is rejected.");
        }

        private static double RevolvedVolume(IList<BevelBlankPoint> polygon)
        {
            double moment = 0.0;
            for (var i = 0; i < polygon.Count; i++)
            {
                var p = polygon[i];
                var q = polygon[(i + 1) % polygon.Count];
                moment += (p.Y + q.Y) * (p.X * q.Y - q.X * p.Y);
            }

            return Math.Abs(Math.PI * moment / 3.0);
        }

        private static double SignedVolume(GearMesh3DBuilder.Mesh3D mesh)
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

        /// <summary>Every triangle edge is shared by exactly two triangles (compared by position), so the solid has no holes.</summary>
        private static void AssertMeshIsClosed(GearMesh3DBuilder.Mesh3D mesh)
        {
            string Key(Bolsover.Gears.Model.GearPoint3D v) => Math.Round(v.X, 5) + "|" + Math.Round(v.Y, 5) + "|" + Math.Round(v.Z, 5);
            var edges = new Dictionary<string, int>();
            for (var i = 0; i + 2 < mesh.TriangleIndices.Count; i += 3)
            {
                var keys = new[]
                {
                    Key(mesh.Vertices[mesh.TriangleIndices[i]]),
                    Key(mesh.Vertices[mesh.TriangleIndices[i + 1]]),
                    Key(mesh.Vertices[mesh.TriangleIndices[i + 2]])
                };
                for (var e = 0; e < 3; e++)
                {
                    var a = keys[e];
                    var b = keys[(e + 1) % 3];
                    if (a == b) continue;
                    var edge = string.CompareOrdinal(a, b) < 0 ? a + ">" + b : b + ">" + a;
                    edges[edge] = edges.TryGetValue(edge, out var count) ? count + 1 : 1;
                }
            }

            Assert.IsEmpty(edges.Where(kv => kv.Value != 2).Take(5).ToList(), "Every edge must be shared by two triangles.");
        }
    }
}
