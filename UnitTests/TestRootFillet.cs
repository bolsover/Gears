using System;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Alibre;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class TestRootFillet
    {
        private const double Module = 2.0;
        private const double PressureAngle = 20.0;
        private const double Tolerance = 1e-6;

        private static AlibreGearGenerator.AlibreGearProfile Generate(int teeth, bool isInternal, double helixAngle = 0.0)
        {
            return isInternal
                ? AlibreGearGenerator.GenerateInternalToothProfile(teeth, Module, PressureAngle, 10.0, helixAngle: helixAngle,
                    rootFillet: RootFilletType.Radius)
                : AlibreGearGenerator.GenerateExternalToothProfile(teeth, Module, PressureAngle, helixAngle: helixAngle,
                    rootFillet: RootFilletType.Radius);
        }

        private static double Distance(GearPoint first, GearPoint second)
        {
            var dx = first.X - second.X;
            var dy = first.Y - second.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double Radius(GearPoint point)
        {
            return Math.Sqrt(point.X * point.X + point.Y * point.Y);
        }

        private static void AssertProfileSegmentsJoin(AlibreGearGenerator.AlibreGearProfile profile, bool isInternal)
        {
            var segments = AlibreGearGenerator.BuildToothOutline(profile, isInternal);
            if (isInternal) segments.RemoveAt(segments.Count - 1); // The outer rim arc is a separate boundary, not part of the tooth contour.
            Assert.Greater(segments.Count, 0);
            for (var i = 1; i < segments.Count; i++)
            {
                Assert.LessOrEqual(Distance(segments[i - 1][segments[i - 1].Count - 1], segments[i][0]), Tolerance,
                    $"Segments {i - 1} and {i} should meet.");
            }
        }

        private static void AssertFillet(AlibreGearGenerator.AlibreGearProfile profile, bool isInternal)
        {
            const double radius = 0.38 * Module;
            Assert.AreEqual(radius, profile.Parameters["rootFilletRadius"], 1e-12);
            var rootRadius = profile.Parameters["dedendumRadius"];
            var centres = new[] { profile.RootFilletCentre1, profile.RootFilletCentre2 };
            var fillets = new[] { profile.RootFillet1, profile.RootFillet2 };
            for (var i = 0; i < fillets.Length; i++)
            {
                Assert.IsNotNull(centres[i]);
                Assert.GreaterOrEqual(fillets[i].Count, 2);
                foreach (var point in fillets[i])
                {
                    Assert.AreEqual(radius, Distance(point, centres[i]), Tolerance, "Each sampled point lies on the dedendum fillet circle.");
                }

                Assert.AreEqual(isInternal ? rootRadius - radius : rootRadius + radius, Radius(centres[i]), Tolerance,
                    "The fillet circle is tangent to the root circle.");
            }

            var rootPoints = isInternal
                ? new[] { profile.RootFillet1[profile.RootFillet1.Count - 1], profile.RootFillet2[0] }
                : new[] { profile.RootFillet1[0], profile.RootFillet2[profile.RootFillet2.Count - 1] };
            foreach (var point in rootPoints) Assert.AreEqual(rootRadius, Radius(point), Tolerance);
        }

        [TestCase(8, false)]
        [TestCase(12, false)]
        [TestCase(20, false)]
        [TestCase(40, false)]
        [TestCase(100, false)]
        [TestCase(8, true)]
        [TestCase(12, true)]
        [TestCase(20, true)]
        [TestCase(40, true)]
        [TestCase(100, true)]
        public void RadiusProfiles_JoinContinuouslyAndHaveTangentFillets(int teeth, bool isInternal)
        {
            var profile = Generate(teeth, isInternal);

            AssertProfileSegmentsJoin(profile, isInternal);
            AssertFillet(profile, isInternal);
        }

        [Test]
        public void RadiusProfiles_SupportHelicalGears()
        {
            foreach (var isInternal in new[] { false, true })
            {
                var profile = Generate(20, isInternal, 25.0);
                AssertProfileSegmentsJoin(profile, isInternal);
                AssertFillet(profile, isInternal);
                Assert.AreEqual(25.0, profile.Parameters["helixAngle"], 0.0);
            }
        }

        [Test]
        public void StandardProfiles_AreIdenticalToTheDefaultGeneratorOverload()
        {
            foreach (var isInternal in new[] { false, true })
            {
                var defaultProfile = isInternal
                    ? AlibreGearGenerator.GenerateInternalToothProfile(40, Module, PressureAngle, 10.0)
                    : AlibreGearGenerator.GenerateExternalToothProfile(40, Module, PressureAngle);
                var standardProfile = isInternal
                    ? AlibreGearGenerator.GenerateInternalToothProfile(40, Module, PressureAngle, 10.0, rootFillet: RootFilletType.Standard)
                    : AlibreGearGenerator.GenerateExternalToothProfile(40, Module, PressureAngle, rootFillet: RootFilletType.Standard);

                AssertProfilesEqual(defaultProfile, standardProfile);
            }
        }

        [Test]
        public void AlibreSketch_RadiusProfilesAreClosedAndSpanOnePitch()
        {
            foreach (var isInternal in new[] { false, true })
            {
                const int teeth = 20;
                var gear = new InvoluteGearComponent("G")
                {
                    Teeth = teeth,
                    Module = Module,
                    PressureAngleDeg = PressureAngle,
                    IsInternal = isInternal,
                    RimThickness = 10.0,
                    RootFillet = RootFilletType.Radius
                };
                var sketch = SingleToothSketchBuilder.Build(gear);

                Assert.IsTrue(sketch.IsClosed(1e-6));
                var radialLines = sketch.Figures.Where(figure => figure.Kind == ToothSketchFigureKind.Line &&
                    figure.Name.StartsWith("Radial line", StringComparison.Ordinal)).ToList();
                Assert.AreEqual(2, radialLines.Count);
                var angles = radialLines.Select(line =>
                {
                    var first = new GearPoint(line.StartX, line.StartY);
                    var last = new GearPoint(line.EndX, line.EndY);
                    var contourPoint = isInternal
                        ? (Radius(first) < Radius(last) ? first : last)
                        : (Radius(first) > Radius(last) ? first : last);
                    return Math.Atan2(contourPoint.Y, contourPoint.X);
                }).ToList();
                var span = Math.Abs(Normalise(angles[0] - angles[1]));
                Assert.AreEqual(2.0 * Math.PI / teeth, span, 1e-6);
                Assert.AreEqual(2, sketch.Figures.Count(figure => figure.Name.StartsWith("Root fillet", StringComparison.Ordinal) &&
                    figure.Kind == ToothSketchFigureKind.Arc));
            }
        }

        [Test]
        public void AssemblySerializer_RoundTripsRootFilletForInvoluteGearsAndWormWheels()
        {
            var assembly = new GearAssembly();
            var involute = new InvoluteGearComponent("I") { RootFillet = RootFilletType.Radius };
            var wheel = new WormWheelComponent("W") { RootFillet = RootFilletType.Radius };
            var involuteShaft = new Shaft("I-shaft");
            involuteShaft.Mount(involute);
            var wheelShaft = new Shaft("W-shaft");
            wheelShaft.Mount(wheel);
            assembly.AddShaft(involuteShaft);
            assembly.AddShaft(wheelShaft);

            var json = AssemblySerializer.ToJson(assembly);
            StringAssert.Contains("\"RootFillet\": \"Radius\"", json);
            var loaded = AssemblySerializer.FromJson(json);

            Assert.AreEqual(RootFilletType.Radius, loaded.Components.OfType<InvoluteGearComponent>().Single().RootFillet);
            Assert.AreEqual(RootFilletType.Radius, loaded.Components.OfType<WormWheelComponent>().Single().RootFillet);
        }

        [Test]
        public void PlanetaryGearSet_PropagatesRootFilletToEveryGear()
        {
            var set = new PlanetaryGearSet("P") { RootFillet = RootFilletType.Radius };

            set.BuildAssembly();

            Assert.IsTrue(new[] { set.Sun, set.Ring }.Concat(set.Planets).All(gear => gear.RootFillet == RootFilletType.Radius));
        }

        [Test]
        public void InvoluteGear_BuildsMeshesWithRadiusRootFillets()
        {
            foreach (var isInternal in new[] { false, true })
            {
                var gear = new InvoluteGearComponent("G")
                {
                    Teeth = 20,
                    Module = Module,
                    PressureAngleDeg = PressureAngle,
                    IsInternal = isInternal,
                    RimThickness = 10.0,
                    RootFillet = RootFilletType.Radius,
                    ShowFullGear = false
                };

                var geometry = gear.BuildGeometry();

                Assert.Greater(geometry.Mesh.Vertices.Count, 0);
                Assert.Greater(geometry.Mesh.TriangleIndices.Count, 0);
            }
        }

        private static void AssertProfilesEqual(AlibreGearGenerator.AlibreGearProfile expected, AlibreGearGenerator.AlibreGearProfile actual)
        {
            var expectedSegments = AlibreGearGenerator.BuildToothOutline(expected, false)
                .Concat(AlibreGearGenerator.BuildToothOutline(expected, true)).ToList();
            var actualSegments = AlibreGearGenerator.BuildToothOutline(actual, false)
                .Concat(AlibreGearGenerator.BuildToothOutline(actual, true)).ToList();
            Assert.AreEqual(expectedSegments.Count, actualSegments.Count);
            for (var i = 0; i < expectedSegments.Count; i++)
            {
                Assert.AreEqual(expectedSegments[i].Count, actualSegments[i].Count);
                for (var j = 0; j < expectedSegments[i].Count; j++)
                {
                    Assert.AreEqual(expectedSegments[i][j].X, actualSegments[i][j].X, 0.0);
                    Assert.AreEqual(expectedSegments[i][j].Y, actualSegments[i][j].Y, 0.0);
                }
            }

            Assert.AreEqual(expected.Parameters.Count, actual.Parameters.Count);
            foreach (var parameter in expected.Parameters)
            {
                Assert.AreEqual(parameter.Value, actual.Parameters[parameter.Key], 0.0, parameter.Key);
            }
        }

        private static double Normalise(double angle)
        {
            while (angle > Math.PI) angle -= 2.0 * Math.PI;
            while (angle <= -Math.PI) angle += 2.0 * Math.PI;
            return angle;
        }
    }
}
