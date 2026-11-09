using System;
using System.Collections.Generic;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>
    /// One cross section of the tooth space of a spiral, zerol or hypoid bevel gear, for a multi-section loft cut in Alibre.
    /// The section is the valley sketch of the straight bevel plan scaled about the cone apex (pointB) by <see cref="Scale"/> and turned
    /// about the gear axis (the X axis of the blank sketch) by <see cref="TwistRad"/>, which is what <see cref="BevelBlankProfile"/> does
    /// for the sections of the 3D model.
    /// </summary>
    public sealed class ToothSpaceSection
    {
        public ToothSpaceSection(double coneDistance, double scale, double twistRad, double planeOriginX, IReadOnlyList<ToothSketchFigure> figures, bool isExtension = false)
        {
            IsExtension = isExtension;
            ConeDistance = coneDistance;
            Scale = scale;
            TwistRad = twistRad;
            PlaneOriginX = planeOriginX;
            Figures = figures;
        }

        /// <summary>
        /// True for the section past the toe (towards the apex) that lets the flat end plane of the loft clear the conical toe face of the blank.
        /// </summary>
        public bool IsExtension { get; }

        /// <summary>Pitch cone distance of the section from the apex, mm.</summary>
        public double ConeDistance { get; }

        /// <summary>Scale about the apex: 1 at the heel and the toe scale (<see cref="BevelBlankProfile.ToeScale"/>) at the toe.</summary>
        public double Scale { get; }

        /// <summary>
        /// Angle (rad) by which the section is turned about the gear axis relative to the straight (untwisted) section, in the direction of
        /// the positive y axis of the valley sketch (the direction of the tooth angle of <see cref="BevelToothPoint"/>). Zero at the mean cone
        /// distance; its sign follows the hand in the blank frame.
        /// </summary>
        public double TwistRad { get; }

        /// <summary>
        /// X (Alibre units) where the plane of the untwisted section meets the gear axis: the back cone apex (pointC) scaled about the apex
        /// (pointB). The plane is that of the straight plan offset by this amount and inclined at the pitch cone angle.
        /// </summary>
        public double PlaneOriginX { get; }

        /// <summary>The closed valley sketch of the section in Alibre units, in the plane coordinates of the straight plan scaled by <see cref="Scale"/>.</summary>
        public IReadOnlyList<ToothSketchFigure> Figures { get; }
    }

    /// <summary>
    /// The tooth-space sections of a spiral, zerol or hypoid bevel gear, from the heel to the toe. This is pure geometry that does not call
    /// Alibre. A hypoid gear takes its cone angle and cone distance from its own blank profile (<see cref="HypoidGearComponent"/>), so the
    /// same sections serve intersecting and offset shafts.
    /// </summary>
    public static class SpiralToothSpaceSections
    {
        /// <summary>Fewest sections of a loft.</summary>
        public const int MinSections = 2;

        /// <summary>Fewest sections chosen automatically.</summary>
        public const int DefaultMinSections = 5;

        /// <summary>Most sections chosen automatically.</summary>
        public const int DefaultMaxSections = 8;

        /// <summary>Most sections that can be asked for (not counting the section past the toe that <c>extendPastToe</c> adds).</summary>
        public const int MaxSections = BevelBlankProfile.MaxSections + 1;

        /// <summary>Smallest length of the extension past the toe, as a fraction of the toe cone distance.</summary>
        public const double ToeExtensionMinFraction = 0.02;

        /// <summary>Largest length of the extension past the toe, as a fraction of the toe cone distance.</summary>
        public const double ToeExtensionMaxFraction = 0.25;

        /// <summary>Extensions shorter than this fraction of the toe cone distance are left out.</summary>
        private const double ToeExtensionTinyFraction = 0.001;

        /// <summary>Inset from the cone distance where the trace stops, as a fraction of that distance.</summary>
        private const double TraceLimitInset = 1e-3;

        /// <summary>
        /// The scale about the apex of the section past the toe, or null when the trace reaches nothing beyond the toe.
        /// The toe face of the blank is a cone, but the last loft section is a flat plane that touches it along one line only; at a sideways
        /// distance s the cone is about s^2 / (2 r) on the material side of the plane. With s one circular pitch at the toe (pi r / teeth) and a
        /// factor of two for margin the extension is pi^2 r / teeth^2 along the pitch cone, with a floor of
        /// <see cref="ToeExtensionMinFraction"/> and a cap of <see cref="ToeExtensionMaxFraction"/> of the toe cone distance, and it is
        /// shortened to the cone distances the cutter trace reaches.
        /// </summary>
        public static double? ToeExtensionScale(BevelBlankProfile blank, SpiralToothTrace trace, int teeth)
        {
            if (blank == null) throw new ArgumentNullException(nameof(blank));
            if (trace == null) throw new ArgumentNullException(nameof(trace));
            if (teeth < 1) throw new ArgumentOutOfRangeException(nameof(teeth));

            var toeDistance = blank.ConeDistance * blank.ToeScale;
            var toeRadius = blank.PitchDiameter / 2.0 * blank.ToeScale;
            var length = Math.PI * Math.PI * toeRadius / ((double)teeth * teeth);
            length = Math.Max(length, ToeExtensionMinFraction * toeDistance);
            length = Math.Min(length, ToeExtensionMaxFraction * toeDistance);

            var target = toeDistance - length;
            if (!trace.Reaches(target))
            {
                var lowest = Math.Abs(trace.CutterCentreDistance - trace.CutterRadius) * (1.0 + TraceLimitInset);
                if (!(lowest < toeDistance * (1.0 - ToeExtensionTinyFraction)) || !trace.Reaches(lowest)) return null;
                target = lowest;
            }

            return target / blank.ConeDistance;
        }

        /// <summary>
        /// The number of sections chosen from the total twist between heel and toe: one interval for every half tooth pitch of twist, with
        /// 5 to 8 sections in all.
        /// </summary>
        public static int SectionCountFor(double totalTwistRad, int teeth)
        {
            if (teeth < 1) throw new ArgumentOutOfRangeException(nameof(teeth));
            var halfPitch = Math.PI / teeth;
            var intervals = (int)Math.Ceiling(Math.Abs(totalTwistRad) / halfPitch - 1e-9);
            return Math.Max(DefaultMinSections, Math.Min(DefaultMaxSections, intervals + 1));
        }

        /// <summary>
        /// Builds the sections of a gear. Throws <see cref="InvalidOperationException"/> when the gear is not valid, as
        /// <see cref="BevelGearSketch.Build"/> does.
        /// </summary>
        /// <param name="gear">A spiral, zerol or hypoid bevel gear.</param>
        /// <param name="sectionCount">Number of sections from <see cref="MinSections"/> to <see cref="MaxSections"/>; zero or less chooses it from the twist.</param>
        /// <param name="extendPastToe">Adds one more section past the toe, towards the apex (see <see cref="ToeExtensionScale"/>), in addition to <paramref name="sectionCount"/>.</param>
        public static IReadOnlyList<ToothSpaceSection> Build(SpiralBevelGearComponent gear, int sectionCount = 0, bool extendPastToe = false)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            return Build(gear, BevelGearSketch.Build(gear), sectionCount, extendPastToe);
        }

        /// <summary>Builds the sections from a plan that was made for the same gear.</summary>
        public static IReadOnlyList<ToothSpaceSection> Build(SpiralBevelGearComponent gear, BevelGearSketch plan, int sectionCount = 0, bool extendPastToe = false)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (sectionCount > MaxSections)
            {
                throw new ArgumentOutOfRangeException(nameof(sectionCount), $"At most {MaxSections} sections are supported.");
            }

            if (sectionCount == 1) throw new ArgumentOutOfRangeException(nameof(sectionCount), $"A loft needs at least {MinSections} sections.");

            BevelBlankProfile blank;
            SpiralToothTrace trace;
            try
            {
                blank = gear.CreateBlankProfile();
                trace = gear.CreateBlankFrameTrace(blank);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException($"Bevel gear '{gear.Id}' cannot be exported to Alibre: {ex.Message}", ex);
            }

            var toeScale = blank.ToeScale;
            var heelTwist = blank.TwistAtScale(trace, 1.0);
            var toeTwist = blank.TwistAtScale(trace, toeScale);
            var count = sectionCount > 0 ? sectionCount : SectionCountFor(toeTwist - heelTwist, gear.Teeth);

            var extensionScale = extendPastToe ? ToeExtensionScale(blank, trace, gear.Teeth) : null;
            var total = extensionScale.HasValue ? count + 1 : count;
            var scales = new double[total];
            var twists = new double[total];
            for (var k = 0; k < count; k++)
            {
                scales[k] = 1.0 + (toeScale - 1.0) * k / (count - 1);
                twists[k] = blank.TwistAtScale(trace, scales[k]);
            }

            if (extensionScale.HasValue)
            {
                scales[count] = extensionScale.Value;
                twists[count] = blank.TwistAtScale(trace, extensionScale.Value);
            }

            // Between two sections a tooth space must not turn by more than half a pitch, or the loft would fold over.
            var halfPitch = Math.PI / gear.Teeth;
            for (var k = 0; k + 1 < total; k++)
            {
                if (Math.Abs(twists[k + 1] - twists[k]) >= halfPitch)
                {
                    throw new InvalidOperationException(
                        $"Bevel gear '{gear.Id}' cannot be exported to Alibre: the spiral twist between two sections is more than half a tooth pitch, " +
                        "so the lofted tooth space would fold over. Use more sections, a smaller spiral angle or a narrower face.");
                }
            }

            var sections = new List<ToothSpaceSection>(total);
            for (var k = 0; k < total; k++)
            {
                var scale = scales[k];
                var planeX = plan.ApexNodeX + (plan.BackConeApexX - plan.ApexNodeX) * scale;
                sections.Add(new ToothSpaceSection(blank.ConeDistance * scale, scale, twists[k], planeX, ScaleFigures(plan.ToothFigures, scale), k >= count));
            }

            return sections;
        }

        /// <summary>Scales the figures of a sketch about its origin (arcs are about the origin, so they stay arcs about it).</summary>
        public static IReadOnlyList<ToothSketchFigure> ScaleFigures(IReadOnlyList<ToothSketchFigure> figures, double scale)
        {
            if (figures == null) throw new ArgumentNullException(nameof(figures));
            var result = new List<ToothSketchFigure>(figures.Count);
            foreach (var figure in figures)
            {
                List<GearPoint> points = null;
                if (figure.Points != null)
                {
                    points = new List<GearPoint>(figure.Points.Count);
                    foreach (var point in figure.Points) points.Add(new GearPoint(point.X * scale, point.Y * scale));
                }

                result.Add(new ToothSketchFigure(figure.Kind, figure.Name, figure.StartX * scale, figure.StartY * scale, figure.EndX * scale, figure.EndY * scale,
                    figure.CentreX * scale, figure.CentreY * scale, points));
            }

            return result;
        }
    }
}
