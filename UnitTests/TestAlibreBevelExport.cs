using System;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Alibre;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Tests of the bevel gear export to Alibre that need no running Alibre: the plan (blank profile, apex node, back cone plane and tooth
    /// valley sketch) and the location of the part in an assembly. The Alibre calls themselves are not covered here.
    /// </summary>
    [TestFixture]
    public class TestAlibreBevelExport
    {
        /// <summary>The 18 tooth pinion of the 18/45 sample pair: module 10.16, 20 degrees, 63.5 mm face width, 90 degree shafts.</summary>
        private static BevelGearComponent Pinion(BevelGearType type = BevelGearType.Standard, double rackTipRadiusFactor = 0.0)
        {
            return new BevelGearComponent("b1")
            {
                Teeth = 18,
                MateTeeth = 45,
                Module = 10.16,
                PressureAngleDeg = 20.0,
                FaceWidth = 63.5,
                ShaftAngleDeg = 90.0,
                BevelType = type,
                RackTipRadiusFactor = rackTipRadiusFactor,
                HasHub = false
            };
        }

        [Test]
        public void Plan_HasTheKeyPointsOfTheSampleInCentimetres()
        {
            var plan = BevelGearSketch.Build(Pinion());

            Assert.AreEqual(18, plan.Teeth);
            Assert.AreEqual(3.6576, plan.BackConeApexX, 1e-3, "pointC is 36.576 mm along +X.");
            Assert.AreEqual(-22.86, plan.ApexNodeX, 1e-3, "pointB, where the front cone meets the X axis, is 228.6 mm along -X.");
            Assert.AreEqual(9.144, plan.PointAY, 1e-6, "pointA is half the pitch diameter up the Y axis.");
            Assert.AreEqual(21.801, plan.BackConePlaneAngleRad * 180.0 / Math.PI, 1e-3, "The plane is turned by the pitch cone angle.");
            Assert.AreEqual(2.0 * Math.PI / 18.0, plan.PatternAngle, 1e-12);
            StringAssert.Contains("Z18", plan.PartName);
        }

        [Test]
        public void Plan_BlankProfileIsTheClosedSixLineSketch()
        {
            var plan = BevelGearSketch.Build(Pinion());

            Assert.AreEqual(6, plan.BlankFigures.Count);
            Assert.IsTrue(plan.BlankFigures.All(f => f.Kind == ToothSketchFigureKind.Line));
            Assert.IsTrue(BevelGearSketch.IsClosed(plan.BlankFigures));
        }

        [Test]
        public void Plan_BlankProfileMatchesTheBlankProfileOfTheEditor()
        {
            var gear = Pinion();
            var blank = gear.CreateBlankProfile();

            var plan = BevelGearSketch.Build(gear);

            var outline = blank.Outline;
            for (var i = 0; i < outline.Count; i++)
            {
                Assert.AreEqual(outline[i].X * 0.1, plan.BlankFigures[i].StartX, 1e-6, "start X of the line from point " + i);
                Assert.AreEqual(outline[i].Y * 0.1, plan.BlankFigures[i].StartY, 1e-6, "start Y of the line from point " + i);
            }
        }

        [Test]
        public void Plan_BackConeDirectionIsThePitchConeAngleTurnedFromTheYAxis()
        {
            var plan = BevelGearSketch.Build(Pinion());

            plan.GetBackConeDirection(out var dx, out var dy);

            Assert.AreEqual(-Math.Sin(plan.BackConePlaneAngleRad), dx, 1e-9, "Turning +Y about Z by the pitch cone angle gives the back cone line.");
            Assert.AreEqual(Math.Cos(plan.BackConePlaneAngleRad), dy, 1e-9);
            Assert.AreEqual(1.0, dx * dx + dy * dy, 1e-12);
        }

        [TestCase(BevelGearType.Standard, 0.0)]
        [TestCase(BevelGearType.Standard, 0.25)]
        [TestCase(BevelGearType.Gleason, 0.0)]
        public void Plan_ToothValleySketchIsClosedAndSymmetricAboutTheBackConeLine(BevelGearType type, double rackTipRadiusFactor)
        {
            var gear = Pinion(type, rackTipRadiusFactor);
            var blank = gear.CreateBlankProfile();

            var plan = BevelGearSketch.Build(gear);

            Assert.IsTrue(BevelGearSketch.IsClosed(plan.ToothFigures), "The valley sketch must be a closed loop.");
            Assert.AreEqual(2, plan.ToothFigures.Count(f => f.Kind == ToothSketchFigureKind.Arc), "A root arc and an outer arc.");
            Assert.GreaterOrEqual(plan.ToothFigures.Count(f => f.Kind == ToothSketchFigureKind.Spline), 4, "Two fillets and two flanks.");
            foreach (var arc in plan.ToothFigures.Where(f => f.Kind == ToothSketchFigureKind.Arc))
            {
                Assert.AreEqual(0.0, arc.CentreX, 1e-12, arc.Name + " is about pointC.");
                Assert.AreEqual(0.0, arc.CentreY, 1e-12);
            }

            var points = plan.ToothFigures.Where(f => f.Kind == ToothSketchFigureKind.Spline).SelectMany(f => f.Points).ToList();
            Assert.AreEqual(points.Max(p => p.Y), -points.Min(p => p.Y), 1e-6, "The valley is symmetric about the back cone line.");

            var rootRadius = (blank.PointD - blank.PointC).Length * 0.1;
            var tipRadius = (blank.PointE - blank.PointC).Length * 0.1;
            var radii = points.Select(p => Math.Sqrt(p.X * p.X + p.Y * p.Y)).ToList();
            Assert.GreaterOrEqual(radii.Min(), rootRadius - 1e-6, "No point is below the root radius.");
            Assert.Greater(radii.Max(), tipRadius, "The valley reaches beyond the tip circle so that it cuts through the tooth tips.");
        }

        [Test]
        public void Plan_IsRejectedForAnInvalidGear()
        {
            var gear = Pinion();
            gear.FaceWidth = 5000.0;

            Assert.Throws<InvalidOperationException>(() => BevelGearSketch.Build(gear));
            Assert.Throws<ArgumentNullException>(() => BevelGearSketch.Build(null));
        }

        [Test]
        public void PartName_ShowsTheSystemTeethAndModule()
        {
            Assert.AreEqual("BevelGear_Z18_M10p16", BevelGearSketch.PartNameOf(Pinion()));
            Assert.AreEqual("BevelGearGleason_Z18_M10p16", BevelGearSketch.PartNameOf(Pinion(BevelGearType.Gleason)));
        }

        [Test]
        public void PartFrame_PutsTheFrontConeApexOnTheLocalAxisAtTheApexPosition()
        {
            var gear = Pinion();
            var blank = gear.CreateBlankProfile();

            var apex = AlibreGearExporter.GetBevelPartFrame(gear, 0.0).TransformPoint(new Vector3D(blank.PointB.X, 0.0, 0.0));

            Assert.AreEqual(0.0, apex.X, 1e-9);
            Assert.AreEqual(0.0, apex.Y, 1e-9);
            Assert.AreEqual(gear.ApexAxialPosition, apex.Z, 1e-9, "The cone apex is on +Z at the apex position of the component.");
        }

        [Test]
        public void PartFrame_CarriesTheExportedBlankOntoTheMeshOfTheEditor()
        {
            var gear = Pinion();
            var blank = gear.CreateBlankProfile();
            var sketch = new BevelToothSketch(blank, gear.PressureAngleDeg);
            var exported = blank.BuildToothedMesh(sketch);
            var frame = AlibreGearExporter.GetBevelPartFrame(gear, 0.0);

            var editor = gear.BuildGeometry().Mesh;

            Assert.AreEqual(editor.Vertices.Count, exported.Vertices.Count);
            for (var i = 0; i < exported.Vertices.Count; i++)
            {
                var v = exported.Vertices[i];
                var p = frame.TransformPoint(new Vector3D(v.X, v.Y, v.Z));
                Assert.AreEqual(editor.Vertices[i].X, p.X, 1e-6, "vertex " + i + " X");
                Assert.AreEqual(editor.Vertices[i].Y, p.Y, 1e-6, "vertex " + i + " Y");
                Assert.AreEqual(editor.Vertices[i].Z, p.Z, 1e-6, "vertex " + i + " Z");
            }
        }

        [Test]
        public void PartFrame_IsAProperRotation()
        {
            var frame = AlibreGearExporter.GetBevelPartFrame(Pinion(), 0.0);

            var x = frame.TransformDirection(Vector3D.UnitX);
            var y = frame.TransformDirection(Vector3D.UnitY);
            var z = frame.TransformDirection(Vector3D.UnitZ);

            Assert.AreEqual(1.0, x.X * x.X + x.Y * x.Y + x.Z * x.Z, 1e-12);
            Assert.AreEqual(0.0, x.X * y.X + x.Y * y.Y + x.Z * y.Z, 1e-12);
            Assert.AreEqual(z.X, x.Y * y.Z - x.Z * y.Y, 1e-12, "x cross y is z: no mirroring.");
            Assert.AreEqual(z.Y, x.Z * y.X - x.X * y.Z, 1e-12);
            Assert.AreEqual(z.Z, x.X * y.Y - x.Y * y.X, 1e-12);
        }

        [Test]
        public void PartPlacement_OfABevelGearUsesTheBevelFrameAndTheMountOffset()
        {
            var assembly = new GearAssembly();
            var shaft = assembly.AddShaft(new Shaft("s1"));
            var gear = Pinion();
            var mount = shaft.Mount(gear, 7.0);

            var placement = AlibreGearExporter.GetPartPlacement(shaft, mount);

            var expected = shaft.Placement.ToTransform().Compose(AlibreGearExporter.GetBevelPartFrame(gear, 7.0));
            Assert.AreEqual(expected.Translation.X, placement.Translation.X, 1e-9);
            Assert.AreEqual(expected.Translation.Y, placement.Translation.Y, 1e-9);
            Assert.AreEqual(expected.Translation.Z, placement.Translation.Z, 1e-9);
            Assert.AreEqual(7.0 + gear.BlankAxialOffset, placement.Translation.Z, 1e-9, "Not the half face width shift of the other gears.");
        }
    }
}
