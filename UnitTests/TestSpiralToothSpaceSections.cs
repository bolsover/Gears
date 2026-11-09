using System;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Alibre;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// The tooth-space sections for the multi-section loft cut of a spiral, zerol or hypoid bevel gear in Alibre. This is pure geometry;
    /// the Alibre calls themselves are not covered here.
    /// </summary>
    [TestFixture]
    public class TestSpiralToothSpaceSections
    {
        private static SpiralBevelGearComponent Spiral(SpiralHand hand = SpiralHand.Right)
        {
            return new SpiralBevelGearComponent("S1") { Teeth = 18, MateTeeth = 36, Module = 4.0, FaceWidth = 12.0, Hand = hand, MeanSpiralAngleDeg = 35.0 };
        }

        private static ZerolBevelGearComponent Zerol(SpiralHand hand = SpiralHand.Right)
        {
            return new ZerolBevelGearComponent("Z1") { Teeth = 18, MateTeeth = 36, Module = 4.0, FaceWidth = 12.0, Hand = hand };
        }

        private static HypoidGearComponent HypoidGear()
        {
            return new HypoidGearComponent("G") { Teeth = 37, MateTeeth = 12, Module = 4.0, FaceWidth = 12.0, IsPinion = false, Hand = SpiralHand.Right, OffsetMm = 18.0 };
        }

        [TestCase(5)]
        [TestCase(8)]
        [TestCase(12)]
        public void Build_GivesTheRequestedNumberOfSections_FromTheHeelToTheToe(int count)
        {
            var gear = Spiral();
            var blank = gear.CreateBlankProfile();

            var sections = SpiralToothSpaceSections.Build(gear, count);

            Assert.AreEqual(count, sections.Count);
            Assert.AreEqual(1.0, sections[0].Scale, 1e-12, "the first section is at the heel");
            Assert.AreEqual(blank.ConeDistance, sections[0].ConeDistance, 1e-9);
            Assert.AreEqual(blank.ToeScale, sections[count - 1].Scale, 1e-12, "the last section is at the toe");
            Assert.AreEqual(blank.ConeDistance * blank.ToeScale, sections[count - 1].ConeDistance, 1e-9);
            for (var k = 1; k < count; k++) Assert.Less(sections[k].Scale, sections[k - 1].Scale, "the scale falls from the heel to the toe");
        }

        [Test]
        public void Build_AllowsTwoSections_WhenTheTwistIsSmall()
        {
            var zerol = Zerol();
            var blank = zerol.CreateBlankProfile();

            var sections = SpiralToothSpaceSections.Build(zerol, 2);

            Assert.AreEqual(2, sections.Count);
            Assert.AreEqual(1.0, sections[0].Scale, 1e-12);
            Assert.AreEqual(blank.ToeScale, sections[1].Scale, 1e-12);
        }

        [Test]
        public void Build_RejectsTooFewSections_WhenTheTwistBetweenTwoSectionsIsOverHalfAPitch()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => SpiralToothSpaceSections.Build(Spiral(), 2));

            StringAssert.Contains("half a tooth pitch", ex.Message);
            Assert.DoesNotThrow(() => SpiralToothSpaceSections.Build(Spiral(), 5));
        }

        [Test]
        public void Build_ChoosesFiveToEightSections_WhenNoCountIsGiven()
        {
            var sections = SpiralToothSpaceSections.Build(Spiral());

            Assert.GreaterOrEqual(sections.Count, SpiralToothSpaceSections.DefaultMinSections);
            Assert.LessOrEqual(sections.Count, SpiralToothSpaceSections.DefaultMaxSections);
        }

        [Test]
        public void SectionCountFor_GrowsWithTheTwist_WithinFiveAndEight()
        {
            Assert.AreEqual(5, SpiralToothSpaceSections.SectionCountFor(0.0, 20));
            Assert.AreEqual(5, SpiralToothSpaceSections.SectionCountFor(Math.PI / 20.0 * 2.0, 20));
            Assert.AreEqual(7, SpiralToothSpaceSections.SectionCountFor(Math.PI / 20.0 * 6.0, 20));
            Assert.AreEqual(8, SpiralToothSpaceSections.SectionCountFor(10.0, 20), "at most 8 sections are chosen");
            Assert.AreEqual(SpiralToothSpaceSections.SectionCountFor(1.0, 20), SpiralToothSpaceSections.SectionCountFor(-1.0, 20), "the sign of the twist does not matter");
        }

        [Test]
        public void Twist_IsZeroAtTheMeanConeDistance_AndRunsFromHeelToToeWithOppositeSigns()
        {
            var gear = Spiral();
            var blank = gear.CreateBlankProfile();

            var sections = SpiralToothSpaceSections.Build(gear, 5);

            Assert.AreEqual(blank.MeanConeDistance, sections[2].ConeDistance, 1e-9, "the middle of five sections is at the mean cone distance");
            Assert.AreEqual(0.0, sections[2].TwistRad, 1e-9);
            Assert.Greater(Math.Abs(sections[0].TwistRad), 1e-6);
            Assert.Greater(Math.Abs(sections[4].TwistRad), 1e-6);
            Assert.Less(sections[0].TwistRad * sections[4].TwistRad, 0.0, "heel and toe are turned opposite ways about the mean section");
        }

        [Test]
        public void Twist_IsThatOfTheMeshBuilder_InTheBlankFrame()
        {
            var gear = Spiral();
            var blank = gear.CreateBlankProfile();
            var trace = gear.CreateBlankFrameTrace(blank);

            foreach (var section in SpiralToothSpaceSections.Build(gear, 7))
            {
                Assert.AreEqual(blank.TwistAtScale(trace, section.Scale), section.TwistRad, 1e-12);
            }
        }

        [Test]
        public void Twist_HasTheSignOfTheHand()
        {
            var right = SpiralToothSpaceSections.Build(Spiral(SpiralHand.Right), 5);
            var left = SpiralToothSpaceSections.Build(Spiral(SpiralHand.Left), 5);

            for (var k = 0; k < 5; k++) Assert.AreEqual(-right[k].TwistRad, left[k].TwistRad, 1e-12, $"section {k}");
        }

        [Test]
        public void Zerol_HasTwistAtHeelAndToe_ButTheConeDataOfTheStraightPair()
        {
            var zerol = Zerol();
            var straight = new BevelGearComponent("B1") { Teeth = 18, MateTeeth = 36, Module = 4.0, FaceWidth = 12.0 };

            var sections = SpiralToothSpaceSections.Build(zerol, 5);
            var zerolBlank = zerol.CreateBlankProfile();
            var straightBlank = straight.CreateBlankProfile();

            Assert.AreEqual(straightBlank.PitchConeAngleRad, zerolBlank.PitchConeAngleRad, 1e-12);
            Assert.AreEqual(straightBlank.ConeDistance, sections[0].ConeDistance, 1e-9);
            Assert.AreEqual(straightBlank.ToeScale, sections[4].Scale, 1e-12);
            Assert.AreEqual(0.0, sections[2].TwistRad, 1e-9, "the zerol tooth line is a cone element at the middle of the face");
            Assert.Greater(Math.Abs(sections[0].TwistRad), 1e-7);
            Assert.Greater(Math.Abs(sections[4].TwistRad), 1e-7);
        }

        [Test]
        public void Figures_AreTheStraightValleyScaledAboutTheApex_AndStayClosed()
        {
            var gear = Spiral();
            var plan = BevelGearSketch.Build(gear);

            var sections = SpiralToothSpaceSections.Build(gear, plan, 6);

            Assert.AreEqual(plan.BackConeApexX, sections[0].PlaneOriginX, 1e-12);
            Assert.AreEqual(plan.ApexNodeX + (plan.BackConeApexX - plan.ApexNodeX) * sections[5].Scale, sections[5].PlaneOriginX, 1e-12);
            foreach (var section in sections)
            {
                Assert.AreEqual(plan.ToothFigures.Count, section.Figures.Count);
                Assert.IsTrue(BevelGearSketch.IsClosed(section.Figures, 1e-6), $"the section at scale {section.Scale:0.###} is closed");
                for (var i = 0; i < section.Figures.Count; i++)
                {
                    var source = plan.ToothFigures[i];
                    var scaled = section.Figures[i];
                    Assert.AreEqual(source.Kind, scaled.Kind);
                    Assert.AreEqual(source.StartX * section.Scale, scaled.StartX, 1e-9);
                    Assert.AreEqual(source.StartY * section.Scale, scaled.StartY, 1e-9);
                    Assert.AreEqual(source.EndX * section.Scale, scaled.EndX, 1e-9);
                    Assert.AreEqual(source.EndY * section.Scale, scaled.EndY, 1e-9);
                    if (source.Points != null)
                    {
                        Assert.AreEqual(source.Points.Count, scaled.Points.Count);
                        Assert.AreEqual(source.Points.Last().X * section.Scale, scaled.Points.Last().X, 1e-9);
                    }
                }
            }
        }

        [Test]
        public void Figures_OfTheHeelSectionAreThoseOfThePlan()
        {
            var gear = Spiral();
            var plan = BevelGearSketch.Build(gear);

            var heel = SpiralToothSpaceSections.Build(gear, plan, 5)[0];

            for (var i = 0; i < plan.ToothFigures.Count; i++)
            {
                Assert.AreEqual(plan.ToothFigures[i].StartX, heel.Figures[i].StartX, 1e-12);
                Assert.AreEqual(plan.ToothFigures[i].EndY, heel.Figures[i].EndY, 1e-12);
            }
        }

        [Test]
        public void Hypoid_UsesTheHypoidConeAngleAndConeDistance()
        {
            var gear = HypoidGear();
            var straight = new BevelGearComponent("B") { Teeth = 37, MateTeeth = 12, Module = 4.0, FaceWidth = 12.0 };
            Assert.IsNotNull(gear.Pair, gear.PairError);

            var plan = BevelGearSketch.Build(gear);
            var sections = SpiralToothSpaceSections.Build(gear, plan, 5);

            Assert.AreEqual(gear.Pair.GearConeAngleRad, plan.BackConePlaneAngleRad, 1e-12, "the planes are inclined at the hypoid pitch cone angle");
            Assert.Greater(Math.Abs(gear.Pair.GearConeAngleRad - straight.CreateBlankProfile().PitchConeAngleRad), 1e-5, "the offset changes the cone angle");
            Assert.AreEqual(gear.ConeDistance, sections[0].ConeDistance, 1e-9);
            Assert.AreEqual(gear.CreateBlankProfile().MeanConeDistance, sections[2].ConeDistance, 1e-9, "the mean section is at the middle of the face");
            Assert.AreEqual(gear.Pair.GearMeanConeDistance, sections[2].ConeDistance, 0.05, "the middle of the face is the mean point");
            Assert.AreEqual(0.0, sections[2].TwistRad, 1e-9);
            Assert.Greater(Math.Abs(sections[0].TwistRad), 1e-7);
            foreach (var section in sections) Assert.IsTrue(BevelGearSketch.IsClosed(section.Figures, 1e-6));
        }

        [Test]
        public void Build_RejectsBadInput()
        {
            Assert.Throws<ArgumentNullException>(() => SpiralToothSpaceSections.Build(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => SpiralToothSpaceSections.Build(Spiral(), 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => SpiralToothSpaceSections.Build(Spiral(), SpiralToothSpaceSections.MaxSections + 1));

            var invalid = Spiral();
            invalid.FaceWidth = 5000.0;
            Assert.Throws<InvalidOperationException>(() => SpiralToothSpaceSections.Build(invalid));
        }

        private static SpiralBevelGearComponent GearOf(string kind)
        {
            switch (kind)
            {
                case "spiral-right": return Spiral();
                case "spiral-left": return Spiral(SpiralHand.Left);
                case "zerol": return Zerol();
                default: return HypoidGear();
            }
        }

        [TestCase("spiral-right")]
        [TestCase("spiral-left")]
        [TestCase("zerol")]
        [TestCase("hypoid")]
        public void Build_ExtendPastToe_AddsOneSectionAfterTheToe_AndKeepsTheOthers(string kind)
        {
            var gear = GearOf(kind);
            var blank = gear.CreateBlankProfile();
            var trace = gear.CreateBlankFrameTrace(blank);

            var plain = SpiralToothSpaceSections.Build(gear, 6);
            var extended = SpiralToothSpaceSections.Build(gear, 6, true);

            Assert.AreEqual(plain.Count + 1, extended.Count);
            for (var k = 0; k < plain.Count; k++)
            {
                Assert.AreEqual(plain[k].Scale, extended[k].Scale, 1e-12);
                Assert.AreEqual(plain[k].TwistRad, extended[k].TwistRad, 1e-12);
                Assert.IsFalse(extended[k].IsExtension);
                Assert.IsFalse(plain[k].IsExtension);
            }

            var extra = extended[extended.Count - 1];
            Assert.IsTrue(extra.IsExtension);
            Assert.Less(extra.Scale, blank.ToeScale, "the extension is past the toe");
            Assert.Greater(extra.Scale, 0.0);
            Assert.AreEqual(blank.ConeDistance * extra.Scale, extra.ConeDistance, 1e-9);
            Assert.AreEqual(blank.TwistAtScale(trace, extra.Scale), extra.TwistRad, 1e-12);
            Assert.Less(Math.Abs(extra.TwistRad - plain[plain.Count - 1].TwistRad), Math.PI / gear.Teeth, "the twist step to the extension is below half a pitch");
            Assert.IsTrue(BevelGearSketch.IsClosed(extra.Figures, 1e-6));
        }

        [Test]
        public void Build_WithoutExtendPastToe_GivesTheOldList()
        {
            var gear = Spiral();

            var plain = SpiralToothSpaceSections.Build(gear, 6);
            var explicitOff = SpiralToothSpaceSections.Build(gear, 6, false);

            Assert.AreEqual(plain.Count, explicitOff.Count);
            Assert.IsFalse(explicitOff.Any(s => s.IsExtension));
        }

        [TestCase("spiral-right")]
        [TestCase("zerol")]
        [TestCase("hypoid")]
        public void ToeExtensionScale_IsAtLeastTheGapOfAFlatPlaneAgainstTheToeCone_AndAtMostAQuarterOfTheToeDistance(string kind)
        {
            var gear = GearOf(kind);
            var blank = gear.CreateBlankProfile();
            var trace = gear.CreateBlankFrameTrace(blank);

            var scale = SpiralToothSpaceSections.ToeExtensionScale(blank, trace, gear.Teeth);

            Assert.IsTrue(scale.HasValue);
            var toeDistance = blank.ConeDistance * blank.ToeScale;
            var length = toeDistance - blank.ConeDistance * scale.Value;
            var toeRadius = blank.PitchDiameter / 2.0 * blank.ToeScale;
            var gap = Math.PI * Math.PI * toeRadius / ((double)gear.Teeth * gear.Teeth);
            Assert.GreaterOrEqual(length, Math.Min(gap, 0.25 * toeDistance) - 1e-9);
            Assert.LessOrEqual(length, 0.25 * toeDistance + 1e-9);
            Assert.IsTrue(trace.Reaches(blank.ConeDistance * scale.Value));
        }

        [Test]
        public void ToeExtensionScale_StraightTrace_UsesTheFullLength()
        {
            var gear = Zerol();
            var blank = gear.CreateBlankProfile();

            var scale = SpiralToothSpaceSections.ToeExtensionScale(blank, SpiralToothTrace.Straight, gear.Teeth);

            Assert.IsTrue(scale.HasValue);
            Assert.Less(scale.Value, blank.ToeScale);
        }

        private static SpiralToothTrace TraceWithLowestReach(BevelBlankProfile blank, double lowestReach)
        {
            const double cutterRadius = 100.0;
            var mean = blank.MeanConeDistance;
            var centre = cutterRadius + lowestReach;
            var sinBeta = (mean * mean + cutterRadius * cutterRadius - centre * centre) / (2.0 * mean * cutterRadius);
            return new SpiralToothTrace(mean, Math.Asin(sinBeta), cutterRadius, true);
        }

        [Test]
        public void ToeExtensionScale_IsShortenedToTheReachOfTheTrace()
        {
            var gear = Spiral();
            var blank = gear.CreateBlankProfile();
            var toeDistance = blank.ConeDistance * blank.ToeScale;
            var trace = TraceWithLowestReach(blank, 0.99 * toeDistance);

            var scale = SpiralToothSpaceSections.ToeExtensionScale(blank, trace, gear.Teeth);

            Assert.IsTrue(scale.HasValue);
            var distance = blank.ConeDistance * scale.Value;
            Assert.Less(distance, toeDistance, "still past the toe");
            Assert.Greater(distance, 0.99 * toeDistance, "but not beyond what the trace reaches");
            Assert.IsTrue(trace.Reaches(distance));
        }

        [Test]
        public void ToeExtensionScale_IsNull_WhenTheTraceReachesNothingBeyondTheToe()
        {
            var gear = Spiral();
            var blank = gear.CreateBlankProfile();
            var toeDistance = blank.ConeDistance * blank.ToeScale;
            var trace = TraceWithLowestReach(blank, 0.9995 * toeDistance);

            Assert.IsNull(SpiralToothSpaceSections.ToeExtensionScale(blank, trace, gear.Teeth));
        }

        [Test]
        public void ToeExtensionScale_RejectsBadInput()
        {
            var gear = Spiral();
            var blank = gear.CreateBlankProfile();

            Assert.Throws<ArgumentNullException>(() => SpiralToothSpaceSections.ToeExtensionScale(null, SpiralToothTrace.Straight, 18));
            Assert.Throws<ArgumentNullException>(() => SpiralToothSpaceSections.ToeExtensionScale(blank, null, 18));
            Assert.Throws<ArgumentOutOfRangeException>(() => SpiralToothSpaceSections.ToeExtensionScale(blank, SpiralToothTrace.Straight, 0));
        }
    }
}
