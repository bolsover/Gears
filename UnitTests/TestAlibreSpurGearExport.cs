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
            double shift = 0.0, double clearance = 0.5, double backlash = 0.0, double faceWidth = 15.0,
            bool isInternal = false, double helixAngle = 0.0, string hand = "R", double rim = 10.0)
        {
            return new InvoluteGearComponent("G1")
            {
                Teeth = teeth,
                Module = module,
                PressureAngleDeg = pressureAngle,
                ProfileShift = shift,
                Clearance = clearance,
                Backlash = backlash,
                FaceWidth = faceWidth,
                IsInternal = isInternal,
                HelixAngleDeg = helixAngle,
                HelicalDirection = hand,
                RimThickness = rim
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
        public void Build_RejectsGearsThatCannotBeExported()
        {
            Assert.Throws<InvalidOperationException>(() => SingleToothSketchBuilder.Build(CreateGear(2)));
            Assert.Throws<InvalidOperationException>(() => SingleToothSketchBuilder.Build(CreateGear(20, 0.0)));
            Assert.Throws<InvalidOperationException>(() => SingleToothSketchBuilder.Build(CreateGear(helixAngle: -5.0)));
            Assert.Throws<InvalidOperationException>(() => SingleToothSketchBuilder.Build(CreateGear(helixAngle: 75.0)));
            StringAssert.Contains("helix angle", Assert.Throws<InvalidOperationException>(() => SingleToothSketchBuilder.Build(CreateGear(helixAngle: 75.0))).Message);
            Assert.Throws<ArgumentNullException>(() => SingleToothSketchBuilder.Build(null));
        }

        // Internal gears: the tooth is a wedge of the ring, between the inner tooth contour and the rim.
        [TestCase(40, 2.0, 20.0, 0.0, 0.5, 0.0, 10.0)]
        [TestCase(60, 1.5, 20.0, 0.0, 0.375, 0.0, 8.0)]
        [TestCase(30, 2.5, 25.0, 0.0, 0.625, 0.05, 12.0)]
        [TestCase(48, 2.0, 20.0, 0.5, 0.5, 0.0, 6.0)]
        [TestCase(36, 3.0, 20.0, -0.4, 0.75, 0.1, 0.5)]
        public void InternalToothSketch_IsAClosedRingWedgeOnePitchWide(int teeth, double module, double alpha, double shift, double clearance,
            double backlash, double rim)
        {
            var gear = CreateGear(teeth, module, alpha, shift, clearance, backlash, isInternal: true, rim: rim);
            var sketch = SingleToothSketchBuilder.Build(gear);

            Assert.IsTrue(sketch.IsInternal);
            Assert.IsFalse(sketch.IsHelical);
            Assert.IsTrue(sketch.IsClosed(), "Every figure end point must meet exactly one other end point.");

            var lines = sketch.Figures.Where(f => f.Kind == ToothSketchFigureKind.Line).ToList();
            Assert.AreEqual(2, lines.Count, "Two radial lines run from the tooth contour out to the rim.");

            var reference = sketch.ReferenceCircles.ToDictionary(c => c.Name, c => c.Radius);
            var rimRadius = reference["Rim circle"];
            var rootRadius = reference["Root circle"];
            var expectedRim = (Math.Round(rootRadius / CmPerMm, 7) + Math.Max(rim, SingleToothSketchBuilder.MinimumRimThickness)) * CmPerMm;
            Assert.AreEqual(expectedRim, rimRadius, 1e-6, "The rim is the root circle plus the rim thickness.");
            Assert.Greater(rimRadius, rootRadius);

            var lineAngles = new List<double>();
            foreach (var line in lines)
            {
                var inner = Radius(line.StartX, line.StartY) < Radius(line.EndX, line.EndY) ? new[] { line.StartX, line.StartY } : new[] { line.EndX, line.EndY };
                var outer = Radius(line.StartX, line.StartY) < Radius(line.EndX, line.EndY) ? new[] { line.EndX, line.EndY } : new[] { line.StartX, line.StartY };
                Assert.AreEqual(rimRadius, Radius(outer[0], outer[1]), 1e-6, "A radial line ends on the rim circle.");
                Assert.AreEqual(Math.Atan2(inner[1], inner[0]), Math.Atan2(outer[1], outer[0]), 1e-6, "A radial line is radial.");
                lineAngles.Add(Math.Atan2(outer[1], outer[0]));
            }

            Assert.AreEqual(2.0 * Math.PI / teeth, Math.Abs(Normalise(lineAngles[0] - lineAngles[1])), 1e-6,
                "The radial lines are one tooth pitch apart, so the patterned wedges tile the ring.");

            var radii = AllProfilePoints(sketch).Select(p => Radius(p[0], p[1])).Where(r => r > 0).ToList();
            Assert.AreEqual(rimRadius, radii.Max(), 1e-6, "Nothing lies outside the rim.");
            Assert.AreEqual(Math.Max(reference["Inside circle"], reference["Base circle"]), radii.Min(), 1e-6,
                "The tooth tips are at the inside radius, but an involute cannot start below the base circle.");
            Assert.IsFalse(sketch.Figures.Any(f => Radius(f.StartX, f.StartY) < 1e-9 || Radius(f.EndX, f.EndY) < 1e-9),
                "An internal gear has an open bore, so no figure touches the axis.");
        }

        [Test]
        public void InternalToothSketch_ArcsAreCentredOnTheAxisAndDrawnAnticlockwise()
        {
            foreach (var teeth in new[] { 24, 40, 80 })
            {
                var sketch = SingleToothSketchBuilder.Build(CreateGear(teeth, isInternal: true));
                var arcs = sketch.Figures.Where(f => f.Kind == ToothSketchFigureKind.Arc).ToList();

                Assert.GreaterOrEqual(arcs.Count, 3, "Root arc, rim arc and at least one inside arc.");
                foreach (var arc in arcs)
                {
                    Assert.AreEqual(0.0, arc.CentreX, 0.0);
                    Assert.AreEqual(0.0, arc.CentreY, 0.0);
                    Assert.AreEqual(Radius(arc.StartX, arc.StartY), Radius(arc.EndX, arc.EndY), 1e-6, "Both end points of an arc are on one circle.");

                    var sweep = Normalise(Math.Atan2(arc.EndY, arc.EndX) - Math.Atan2(arc.StartY, arc.StartX));
                    Assert.Greater(sweep, 0.0, arc.Name + " runs anticlockwise from its start to its end.");
                    Assert.LessOrEqual(sweep, 2.0 * Math.PI / teeth + 1e-9, arc.Name + " is no longer than one pitch.");
                }
            }
        }

        [Test]
        public void InternalToothSketch_IsExtrudedStraightAndNamed()
        {
            var sketch = SingleToothSketchBuilder.Build(CreateGear(40, 2.0, faceWidth: 18.0, isInternal: true));

            Assert.IsNull(sketch.Helix, "A spur ring gear is extruded straight.");
            Assert.AreEqual(18.0 * CmPerMm, sketch.ExtrusionDepth, 1e-9);
            Assert.AreEqual(40, sketch.Teeth);
            Assert.AreEqual("InternalGear_Z40_M2", sketch.PartName);
        }

        // Helical gears: the tooth sketch is the transverse section, swept along a helix about the gear axis.
        [TestCase(20, 2.0, 15.0, "R", 0.0, 15.0)]
        [TestCase(20, 2.0, 15.0, "L", 0.0, 15.0)]
        [TestCase(30, 1.5, 30.0, "R", 0.0, 25.0)]
        [TestCase(24, 3.0, 8.0, "L", 0.4, 40.0)]
        [TestCase(17, 2.5, 22.5, "R", 0.3, 12.0)]
        public void HelicalToothSketch_DescribesTheHelixFromTheHelixAngleAndPitchRadius(int teeth, double module, double beta, string hand,
            double shift, double faceWidth)
        {
            var gear = CreateGear(teeth, module, shift: shift, faceWidth: faceWidth, helixAngle: beta, hand: hand);
            var sketch = SingleToothSketchBuilder.Build(gear);

            Assert.IsTrue(sketch.IsHelical);
            Assert.IsTrue(sketch.IsClosed());
            var helix = sketch.Helix;

            var betaRad = beta * Math.PI / 180.0;
            var pitchRadiusMm = module / Math.Cos(betaRad) * teeth / 2.0 + shift;
            Assert.AreEqual(pitchRadiusMm * CmPerMm, helix.PitchRadius, 1e-6, "The helix is about the transverse pitch radius, including profile shift.");
            Assert.AreEqual(beta, helix.AngleDeg, 0.0);
            Assert.AreEqual(hand == "R", helix.RightHand);
            Assert.AreEqual(faceWidth * CmPerMm, helix.Height, 1e-9, "The helix runs the face width.");
            Assert.AreEqual(sketch.ExtrusionDepth, helix.Height, 0.0);

            var leadMm = 2.0 * Math.PI * pitchRadiusMm / Math.Tan(betaRad);
            Assert.AreEqual(leadMm * CmPerMm, helix.Lead, 1e-6, "One turn advances 2 pi r / tan(beta).");
            Assert.AreEqual(faceWidth * CmPerMm / (leadMm * CmPerMm), helix.Revolutions, 1e-6);
            Assert.AreEqual(Math.Tan(betaRad) * faceWidth / pitchRadiusMm, helix.TurnAngle, 1e-6,
                "The tooth section turns through face width x tan(beta) / pitch radius, as in the 3D preview.");
        }

        [Test]
        public void HelicalToothSketch_UsesTheTransverseProfile()
        {
            const int teeth = 20;
            const double module = 2.0;
            const double beta = 20.0;
            var sketch = SingleToothSketchBuilder.Build(CreateGear(teeth, module, helixAngle: beta));

            var transversePitchRadius = module / Math.Cos(beta * Math.PI / 180.0) * teeth / 2.0;
            var reference = sketch.ReferenceCircles.ToDictionary(c => c.Name, c => c.Radius);
            Assert.AreEqual(transversePitchRadius * CmPerMm, reference["Pitch circle"], 1e-6);
            Assert.AreEqual((transversePitchRadius + module) * CmPerMm, reference["Outside circle"], 1e-6);

            var lines = sketch.Figures.Where(f => f.Kind == ToothSketchFigureKind.Line).ToList();
            Assert.AreEqual(2.0 * Math.PI / teeth,
                Math.Abs(Normalise(Math.Atan2(lines[0].EndY, lines[0].EndX) - Math.Atan2(lines[1].StartY, lines[1].StartX))), 1e-6);
        }

        [Test]
        public void HelicalToothSketch_IsNamedByHelixAngleAndHand()
        {
            Assert.AreEqual("HelicalGear_Z20_M2_B15R", SingleToothSketchBuilder.PartName(CreateGear(helixAngle: 15.0)));
            Assert.AreEqual("HelicalGear_Z20_M2p5_B22p5L", SingleToothSketchBuilder.PartName(CreateGear(module: 2.5, helixAngle: 22.5, hand: "L")));
            Assert.AreEqual("InternalHelicalGear_Z40_M2_B10R", SingleToothSketchBuilder.PartName(CreateGear(40, isInternal: true, helixAngle: 10.0)));
        }

        [Test]
        public void SpurToothSketch_HasNoHelix()
        {
            var sketch = SingleToothSketchBuilder.Build(CreateGear());

            Assert.IsFalse(sketch.IsInternal);
            Assert.IsNull(sketch.Helix);
            Assert.IsFalse(sketch.IsHelical);
        }

        [TestCase(40, 2.0, 12.0, "R", 10.0)]
        [TestCase(60, 1.5, 25.0, "L", 8.0)]
        public void InternalHelicalToothSketch_IsAClosedRingWedgeWithAHelix(int teeth, double module, double beta, string hand, double rim)
        {
            var gear = CreateGear(teeth, module, isInternal: true, helixAngle: beta, hand: hand, rim: rim);
            var sketch = SingleToothSketchBuilder.Build(gear);

            Assert.IsTrue(sketch.IsInternal);
            Assert.IsTrue(sketch.IsHelical);
            Assert.IsTrue(sketch.IsClosed());
            Assert.AreEqual(hand == "R", sketch.Helix.RightHand);

            var betaRad = beta * Math.PI / 180.0;
            var pitchRadiusMm = module / Math.Cos(betaRad) * teeth / 2.0;
            Assert.AreEqual(pitchRadiusMm * CmPerMm, sketch.Helix.PitchRadius, 1e-6);
            Assert.AreEqual(Math.Tan(betaRad) * gear.FaceWidth / pitchRadiusMm, sketch.Helix.TurnAngle, 1e-6);
        }

        [Test]
        public void Exporter_RequiresAnAlibreRoot()
        {
            Assert.Throws<ArgumentNullException>(() => new AlibreGearExporter(null));
        }

        [Test]
        public void Exporter_UsesTheDocumentedPlaneAndAxisNames()
        {
            Assert.AreEqual("XY-Plane", AlibreGearExporter.SketchPlaneName);
            Assert.AreEqual("Z-Axis", AlibreGearExporter.PatternAxisName);
        }

        [Test]
        public void Exporter_OrientsTheHelicalBossParallelToTheAxis()
        {
            Assert.IsTrue(AlibreGearExporter.HelicalBossParallelOriented,
                "The helical boss must keep the transverse tooth sketch parallel to the gear axis, not normal to it.");
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
