using System;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// A closed simplex ISO 606 roller chain that runs round two sprockets on different, parallel shafts. Like a mesh relation it positions the
    /// driven sprocket's shaft: <see cref="CentreDistance"/> from the driver's shaft along the line of centres at <see cref="CentreLineAngleDeg"/>,
    /// with the mid-planes of the two sprockets in line. Both sprockets turn the same way, the driven one at the ratio of the teeth.
    /// <para>
    /// The chain path is the open chain (external tangents) round the two pitch circles. The chain has <see cref="LinkCount"/> links, always an
    /// even number so that inner and outer links alternate without an offset link, and so <see cref="LinkCount"/> rollers, spaced evenly along
    /// the path. A roller seats in a tooth gap of each sprocket where the chain wraps it.
    /// </para>
    /// </summary>
    public sealed class RollerChain
    {
        /// <summary>The fewest links of a chain.</summary>
        public const int MinimumLinks = 4;

        /// <summary>The wrap angle round the smaller sprocket below which the chain makers advise against the drive (degrees).</summary>
        public const double RecommendedMinimumWrapDegrees = 120.0;

        public RollerChain(string id, SprocketComponent driver, SprocketComponent driven, double centreDistance)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A chain requires a non-empty identifier.", nameof(id));
            Id = id;
            Name = id;
            Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            Driven = driven ?? throw new ArgumentNullException(nameof(driven));
            if (ReferenceEquals(driver, driven)) throw new ArgumentException("A chain needs two different sprockets.");
            CentreDistance = centreDistance;
        }

        public string Id { get; }

        /// <summary>Display name; the identifier by default.</summary>
        public string Name { get; set; }

        /// <summary>The sprocket that drives the chain.</summary>
        public SprocketComponent Driver { get; }

        /// <summary>The sprocket that the chain drives; its shaft is placed by the chain.</summary>
        public SprocketComponent Driven { get; }

        /// <summary>Distance between the two shaft axes (mm).</summary>
        public double CentreDistance { get; set; }

        /// <summary>Direction of the line of centres in the driver shaft's XY plane, measured from its X axis (degrees).</summary>
        public double CentreLineAngleDeg { get; set; }

        public ChainProfile Profile => Driver.Profile;

        /// <summary>Width over the outer plates of the simplex chain (mm).</summary>
        public double Width => Profile.OverallWidth;

        /// <summary>Smallest centre distance at which the two sprockets do not touch: half the sum of their tip diameters.</summary>
        public double MinimumCentreDistance => (Driver.OutsideDiameter + Driven.OutsideDiameter) / 2.0;

        /// <summary>Length of the path round the two pitch circles for <see cref="CentreDistance"/> (mm).</summary>
        public double PitchPathLength => PathLength(CentreDistance);

        /// <summary>
        /// Number of links: the length of the chain in pitches (the straight runs, and the number of pitches that wrap each sprocket), rounded to
        /// the nearest even number.
        /// </summary>
        public int LinkCount
        {
            get
            {
                var big = Math.Max(Driver.Teeth, Driven.Teeth);
                var small = Math.Min(Driver.Teeth, Driven.Teeth);
                var alpha = HalfAngle(CentreDistance);
                var pitches = 2.0 * CentreDistance * Math.Cos(alpha) / Profile.Pitch
                              + (big * (Math.PI + 2.0 * alpha) + small * (Math.PI - 2.0 * alpha)) / (2.0 * Math.PI);
                var even = 2 * (int)Math.Round(pitches / 2.0);
                return Math.Max(MinimumLinks, even);
            }
        }

        /// <summary>Length of the chain as made (mm): <see cref="LinkCount"/> times the pitch.</summary>
        public double ChainLength => LinkCount * Profile.Pitch;

        /// <summary>Speed ratio driver : driven, which is the number of teeth of the driven sprocket over those of the driver.</summary>
        public double GearRatio => (double)Driven.Teeth / Driver.Teeth;

        /// <summary>Angle of the smaller sprocket over which the chain wraps it, in degrees.</summary>
        public double SmallSprocketWrapDegrees
        {
            get
            {
                var alpha = HalfAngle(CentreDistance);
                return (Math.PI - 2.0 * alpha) * 180.0 / Math.PI;
            }
        }

        /// <summary>True when the chain wraps the smaller sprocket by less than <see cref="RecommendedMinimumWrapDegrees"/>.</summary>
        public bool IsBelowRecommendedWrap => SmallSprocketWrapDegrees < RecommendedMinimumWrapDegrees;

        /// <summary>The length of the open chain round two pitch circles at the given centre distance (mm).</summary>
        public double PathLength(double centreDistance)
        {
            var a = Driver.PitchDiameter;
            var b = Driven.PitchDiameter;
            var small = Math.Min(a, b);
            var large = Math.Max(a, b);
            var alpha = HalfAngle(centreDistance);
            return 2.0 * centreDistance * Math.Cos(alpha) + (Math.PI + 2.0 * alpha) * large / 2.0 + (Math.PI - 2.0 * alpha) * small / 2.0;
        }

        private double HalfAngle(double centreDistance)
        {
            var difference = Math.Abs(Driver.PitchDiameter - Driven.PitchDiameter);
            return Math.Asin(Math.Max(-1.0, Math.Min(1.0, difference / (2.0 * centreDistance))));
        }

        /// <summary>Throws <see cref="InvalidOperationException"/> when the chain cannot be drawn.</summary>
        public void Validate()
        {
            if (Driver.Series != Driven.Series)
            {
                throw new InvalidOperationException(
                    $"Chain '{Id}': sprockets '{Driver.Id}' (ISO 606 {Driver.Profile.Name}) and '{Driven.Id}' (ISO 606 {Driven.Profile.Name}) are for different chains.");
            }

            if (CentreDistance <= MinimumCentreDistance)
            {
                throw new InvalidOperationException(
                    $"Chain '{Id}': the centre distance of {CentreDistance:0.###} mm must exceed {MinimumCentreDistance:0.###} mm so that the sprockets do not touch.");
            }
        }
    }
}
