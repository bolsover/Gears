using System;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// A closed HTD timing belt that runs round two pulleys on different, parallel shafts. Like a mesh relation it positions the driven
    /// pulley's shaft: <see cref="CentreDistance"/> from the driver's shaft along the line of centres at <see cref="CentreLineAngleDeg"/>,
    /// with the mid-planes of the two pulleys in line. Both pulleys turn the same way, the driven one at the ratio of the grooves.
    /// <para>
    /// The belt path is the pitch line: the open belt (external tangents) round the two pitch circles. The belt is drawn as a strip
    /// from the outside diameter of the pulleys outwards by the back thickness (belt thickness minus tooth height), with the HTD teeth
    /// on its pulley side: <see cref="BeltTeeth"/> teeth spaced evenly along the pitch line, each the negative of a pulley groove.
    /// </para>
    /// </summary>
    public sealed class TimingBelt
    {
        public TimingBelt(string id, HtdPulleyComponent driver, HtdPulleyComponent driven, double centreDistance)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A belt requires a non-empty identifier.", nameof(id));
            Id = id;
            Name = id;
            Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            Driven = driven ?? throw new ArgumentNullException(nameof(driven));
            if (ReferenceEquals(driver, driven)) throw new ArgumentException("A belt needs two different pulleys.");
            CentreDistance = centreDistance;
        }

        public string Id { get; }

        /// <summary>Display name; the identifier by default.</summary>
        public string Name { get; set; }

        /// <summary>The pulley that drives the belt.</summary>
        public HtdPulleyComponent Driver { get; }

        /// <summary>The pulley that the belt drives; its shaft is placed by the belt.</summary>
        public HtdPulleyComponent Driven { get; }

        /// <summary>Distance between the two shaft axes (mm).</summary>
        public double CentreDistance { get; set; }

        /// <summary>Direction of the line of centres in the driver shaft's XY plane, measured from its X axis (degrees).</summary>
        public double CentreLineAngleDeg { get; set; }

        public HtdProfile Profile => Driver.Profile;

        /// <summary>Width of the belt (mm): the narrower of the two pulleys, since a belt cannot be wider than its pulley.</summary>
        public double Width => Math.Min(Driver.FaceWidth, Driven.FaceWidth);

        /// <summary>Thickness of the belt back, outside the outside diameter of the pulleys (mm): belt thickness minus tooth height.</summary>
        public double BackThickness => Math.Max(0.1, Profile.BeltThickness - Profile.ToothHeight);

        /// <summary>
        /// Smallest centre distance at which the two pulleys (flanges included) do not touch: half the sum of their largest diameters.
        /// </summary>
        public double MinimumCentreDistance => (Driver.FlangeDiameter + Driven.FlangeDiameter) / 2.0;

        /// <summary>Length of the belt along the pitch line for <see cref="CentreDistance"/> (mm).</summary>
        public double PitchPathLength => PathLength(CentreDistance);

        /// <summary>Number of belt teeth: the pitch path length divided by the pitch, rounded to the nearest whole tooth.</summary>
        public int BeltTeeth => (int)Math.Round(PitchPathLength / Profile.Pitch);

        /// <summary>Pitch length of the belt as made (mm): <see cref="BeltTeeth"/> times the pitch.</summary>
        public double PitchLength => BeltTeeth * Profile.Pitch;

        /// <summary>Speed ratio driver : driven, which is the number of grooves of the driven pulley over those of the driver.</summary>
        public double GearRatio => (double)Driven.Teeth / Driver.Teeth;

        /// <summary>Angle of the smaller pulley over which the belt wraps it, in degrees (a measure of how well the belt grips).</summary>
        public double SmallPulleyWrapDegrees
        {
            get
            {
                var small = Math.Min(Driver.PitchDiameter, Driven.PitchDiameter);
                var large = Math.Max(Driver.PitchDiameter, Driven.PitchDiameter);
                var alpha = Math.Asin(Math.Min(1.0, (large - small) / (2.0 * CentreDistance)));
                return (Math.PI - 2.0 * alpha) * 180.0 / Math.PI;
            }
        }

        /// <summary>The length of the open belt round two pitch circles at the given centre distance (mm).</summary>
        public double PathLength(double centreDistance)
        {
            var a = Driver.PitchDiameter;
            var b = Driven.PitchDiameter;
            var small = Math.Min(a, b);
            var large = Math.Max(a, b);
            var alpha = Math.Asin(Math.Max(-1.0, Math.Min(1.0, (large - small) / (2.0 * centreDistance))));
            return 2.0 * centreDistance * Math.Cos(alpha) + (Math.PI + 2.0 * alpha) * large / 2.0 + (Math.PI - 2.0 * alpha) * small / 2.0;
        }

        /// <summary>Throws <see cref="InvalidOperationException"/> when the belt cannot be drawn.</summary>
        public void Validate()
        {
            if (Driver.Series != Driven.Series)
            {
                throw new InvalidOperationException(
                    $"Belt '{Id}': pulleys '{Driver.Id}' (HTD {Driver.Profile.Name}) and '{Driven.Id}' (HTD {Driven.Profile.Name}) are for different belt series.");
            }

            if (CentreDistance <= MinimumCentreDistance)
            {
                throw new InvalidOperationException(
                    $"Belt '{Id}': the centre distance of {CentreDistance:0.###} mm must exceed {MinimumCentreDistance:0.###} mm so that the pulleys do not touch.");
            }
        }
    }
}
