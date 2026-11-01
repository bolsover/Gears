using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator.Alibre;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Tests of the single-tooth sketch that is sent to Alibre. They need no running Alibre: the sketch plan is pure
    /// geometry, and the Alibre calls only replay it.
    /// </summary>
    [TestFixture]
    public class TestAlibreSpurGearExport
    {
        private const double CmPerMm = 0.1;

        private static InvoluteGearComponent CreateGear(int teeth = 20, double module = 2.0, double pressureAngle = 20.0,
            double shift = 0.0, double clearance = 0.5, double backlash = 0.0, double faceWidth = 15.0)
        {
            return new InvoluteGearComponent("G1")
            {
                Teeth = teeth,
                Module = module,
                PressureAngleDeg = pressureAngle,
                ProfileShift = shift,
                Clearance = clearance,
                Backlash = backlash,
                FaceWidth = faceWidth
            };
        }

        private static double Radius(double x, double y) => Math.Sqrt(x * x + y * y);

        private static IEnumerable<double[]> AllProfilePoints(SingleToothSketch sketch)
        {
            foreach (var figure in sketch.Figures)
            {
                yield return new[] { figure.StartX, figure.StartY };
                yield return new[] { figure.EndX, figure.EndY };
                if (figure.Points == null) continue;
                foreach (var point in figure.Points) yield return new[] { point.X, point.Y };
            }
        }

        private static double Normalise(double angle)
        {
            while (angle > Math.PI) angle -= 2.0 * Math.PI;
            while (angle <= -Math.PI) angle += 2.0 * Math.PI;
            return angle;
        }

        // 12 teeth is undercut territory (trochoid root fillet); 60 teeth has the base circle inside the root circle.
        [TestCase(20, 2.0, 20.0, 0.0, 0.5, 0.0)]
        [TestCase(12, 2.0, 20.0, 0.0, 0.5, 0.0)]
        [TestCase(40, 1.5, 20.0, 0.0, 0.375, 0.0)]
        [TestCase(60, 2.0, 20.0, 0.0, 0.5, 0.0)]
        [TestCase(18, 2.5, 25.0, 0.4, 0.625, 0.0)]
        [TestCase(24, 3.0, 20.0, -0.3, 0.75, 0.1)]
        [TestCase(8, 4.0, 20.0, 0.8, 1.0, 0.05)]
        public void ToothSketch_IsClosedAndSpansOnePitch(int teeth, double module, double alpha, double shift, double clearance, double backlash)
        {
            var sketch = SingleToothSketchBuilder.Build(CreateGear(teeth, module, alpha, shift, clearance, backlash));

            Assert.IsTrue(sketch.IsClosed(), "Every figure end point must meet exactly one other end point.");

            var lines = sketch.Figures.Where(f => f.Kind == ToothSketchFigureKind.Line).ToList();
            Assert.AreEqual(2, lines.Count, "The profile is closed by two radial lines to the axis.");
            Assert.AreEqual(0.0, lines[0].StartX, 0.0);
            Assert.AreEqual(0.0, lines[0].StartY, 0.0);
            Assert.AreEqual(0.0, lines[1].EndX, 0.0);
            Assert.AreEqual(0.0, lines[1].EndY, 0.0);

            var startAngle = Math.Atan2(lines[0].EndY, lines[0].EndX);
            var endAngle = Math.Atan2(lines[1].StartY, lines[1].StartX);
            Assert.AreEqual(2.0 * Math.PI / teeth, Math.Abs(Normalise(startAngle - endAngle)), 1e-6,
                "The two radial lines are exactly one tooth pitch apart, so the patterned wedges tile the disc.");
        }

        [Test]
        public void ToothSketch_UsesCentimetresAndExactRadii()
        {
            const int teeth = 20;
            const double module = 2.0;
            var gear = CreateGear(teeth, module, shift: 0.4);
            var sketch = SingleToothSketchBuilder.Build(gear);

            var pitchRadius = module * teeth / 2.0 + 0.4;
            var addendum = (pitchRadius + module) * CmPerMm;
            var dedendum = (pitchRadius - module - gear.Clearance) * CmPerMm;

            var radii = AllProfilePoints(sketch).Select(p => Radius(p[0], p[1])).Where(r => r > 0).ToList();
            Assert.AreEqual(addendum, radii.Max(), 1e-6, "The tooth tip is at the outside radius, in centimetres.");
            Assert.AreEqual(dedendum, radii.Min(), 1e-6, "The tooth root is at the root radius, in centimetres.");

            var reference = sketch.ReferenceCircles.ToDictionary(c => c.Name, c => c.Radius);
            Assert.AreEqual(addendum, reference["Outside circle"], 1e-9);
            Assert.AreEqual(pitchRadius * CmPerMm, reference["Pitch circle"], 1e-9);
            Assert.AreEqual(dedendum, reference["Root circle"], 1e-9);
            var baseRadius = module * teeth / 2.0 * Math.Cos(20.0 * Math.PI / 180.0) * CmPerMm;
            Assert.AreEqual(baseRadius, reference["Base circle"], 1e-6, "The base circle comes from the standard pitch radius, not the shifted one.");
        }

        [Test]
        public void ToothSketch_CoordinatesAreRoundedToSevenDecimalPlacesInMillimetres()
        {
            var sketch = SingleToothSketchBuilder.Build(CreateGear(17, 2.25, 20.0, 0.31, 0.5625, 0.07));

            foreach (var point in AllProfilePoints(sketch))
            {
                foreach (var value in point)
                {
                    var millimetres = value / CmPerMm;
                    Assert.AreEqual(Math.Round(millimetres, 7), millimetres, 1e-9, "Gear geometry is calculated to 7 decimal places.");
                }
            }
        }

        [Test]
        public void ToothSketch_ArcsAreCentredOnTheAxisAndDrawnAnticlockwise()
        {
            foreach (var teeth in new[] { 12, 20, 60 })
            {
                var sketch = SingleToothSketchBuilder.Build(CreateGear(teeth));
                var arcs = sketch.Figures.Where(f => f.Kind == ToothSketchFigureKind.Arc).ToList();

                Assert.AreEqual(2, arcs.Count, "One addendum arc and one root arc.");
                foreach (var arc in arcs)
                {
                    Assert.AreEqual(0.0, arc.CentreX, 0.0);
                    Assert.AreEqual(0.0, arc.CentreY, 0.0);
                    Assert.AreEqual(Radius(arc.StartX, arc.StartY), Radius(arc.EndX, arc.EndY), 1e-6, "Both end points of an arc are on one circle.");

                    var sweep = Normalise(Math.Atan2(arc.EndY, arc.EndX) - Math.Atan2(arc.StartY, arc.StartX));
                    Assert.Greater(sweep, 0.0, arc.Name + " runs anticlockwise from its start to its end.");
                    Assert.Less(sweep, 2.0 * Math.PI / teeth, arc.Name + " is shorter than one pitch.");
                }
            }
        }

        [Test]
        public void ToothSketch_UsesSplinesForInvolutesAndTrochoids()
        {
            var undercut = SingleToothSketchBuilder.Build(CreateGear(20));
            var noFillet = SingleToothSketchBuilder.Build(CreateGear(60));

            var undercutSplines = undercut.Figures.Where(f => f.Kind == ToothSketchFigureKind.Spline).ToList();
            var plainSplines = noFillet.Figures.Where(f => f.Kind == ToothSketchFigureKind.Spline).ToList();

            Assert.AreEqual(4, undercutSplines.Count, "Two involutes and two trochoids when the base circle is outside the root circle.");
            Assert.AreEqual(2, plainSplines.Count, "Two involutes when the root circle is outside the base circle.");
            Assert.IsTrue(undercutSplines.Concat(plainSplines).All(s => s.Points.Count >= 10), "Splines are interpolated through many points.");
            Assert.IsTrue(noFillet.IsClosed());
        }

        [Test]
        public void ToothSketch_ExtrudesToFaceWidthAndPatternsAroundTheGear()
        {
            var sketch = SingleToothSketchBuilder.Build(CreateGear(24, 2.5, faceWidth: 12.5));

            Assert.AreEqual(24, sketch.Teeth);
            Assert.AreEqual(12.5 * CmPerMm, sketch.ExtrusionDepth, 1e-9, "The tooth is extruded to the face width, in centimetres.");
            Assert.AreEqual(2.0 * Math.PI / 24, sketch.PatternAngle, 1e-12);
            Assert.AreEqual(2.0 * Math.PI, sketch.PatternAngle * sketch.Teeth, 1e-12, "The pattern covers the full circle.");
            Assert.AreEqual("SpurGear_Z24_M2p5", sketch.PartName);
            Assert.AreEqual("SpurGear_Z20_M2", SingleToothSketchBuilder.PartName(CreateGear()));
        }

        [Test]
        public void Build_RejectsGearsThatAreNotSimpleExternalSpurGears()
        {
            var internalGear = CreateGear();
            internalGear.IsInternal = true;
            var helical = CreateGear();
            helical.HelixAngleDeg = 15.0;

            StringAssert.Contains("internal", Assert.Throws<InvalidOperationException>(() => SingleToothSketchBuilder.Build(internalGear)).Message);
            StringAssert.Contains("helical", Assert.Throws<InvalidOperationException>(() => SingleToothSketchBuilder.Build(helical)).Message);
            Assert.Throws<InvalidOperationException>(() => SingleToothSketchBuilder.Build(CreateGear(2)));
            Assert.Throws<InvalidOperationException>(() => SingleToothSketchBuilder.Build(CreateGear(20, 0.0)));
            Assert.Throws<ArgumentNullException>(() => SingleToothSketchBuilder.Build(null));
        }

        [Test]
        public void Exporter_RequiresAnAlibreRoot()
        {
            Assert.Throws<ArgumentNullException>(() => new AlibreSpurGearExporter(null));
        }

        [Test]
        public void Exporter_UsesTheDocumentedPlaneAndAxisNames()
        {
            Assert.AreEqual("XY-Plane", AlibreSpurGearExporter.SketchPlaneName);
            Assert.AreEqual("Z-Axis", AlibreSpurGearExporter.PatternAxisName);
        }

        [Test]
        public void ToothSketch_NotClosedWhenAFigureIsMissing()
        {
            var sketch = SingleToothSketchBuilder.Build(CreateGear());
            var broken = new SingleToothSketch(sketch.Teeth, sketch.ExtrusionDepth, sketch.PartName,
                sketch.Figures.Skip(1).ToList(), sketch.ReferenceCircles);

            Assert.IsFalse(broken.IsClosed(), "Without the first radial line the profile is open.");
        }
    }
}
