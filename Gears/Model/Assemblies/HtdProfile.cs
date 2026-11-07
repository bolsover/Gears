using System;
using System.Collections.Generic;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>The HTD (High Torque Drive) belt series that have pulleys here. The number is the pitch in millimetres.</summary>
    public enum HtdSeries
    {
        Htd3M,
        Htd5M,
        Htd8M,
        Htd14M
    }

    /// <summary>
    /// Dimensions of one HTD series (all in millimetres). HTD belts have a curvilinear tooth: a circular head and a rounded root. The
    /// pulley groove is the negative of the belt tooth, so the groove bottom has the head radius and the groove edge at the outside
    /// diameter has the foot radius.
    /// <para>
    /// Sources: Gates PowerGrip HTD and ISO 13050 belt data (pitch, tooth height, head and foot radius, tooth width, belt thickness) as
    /// published by the belt makers (Gates, SIT, Protorque), and the pitch line differential of the pulley tables
    /// (outside diameter = pitch diameter - 2 x pitch line differential). The 3M head radius, foot radius and tooth width are not
    /// given in the sources that were found; they are scaled from the 5M tooth and are approximate.
    /// </para>
    /// </summary>
    public sealed class HtdProfile
    {
        private HtdProfile(
            HtdSeries series, string name, double pitch, double pitchLineDifferential, double toothHeight, double headRadius,
            double footRadius, double toothWidth, double beltThickness, int minimumTeeth)
        {
            Series = series;
            Name = name;
            Pitch = pitch;
            PitchLineDifferential = pitchLineDifferential;
            ToothHeight = toothHeight;
            HeadRadius = headRadius;
            FootRadius = footRadius;
            ToothWidth = toothWidth;
            BeltThickness = beltThickness;
            MinimumTeeth = minimumTeeth;
        }

        /// <summary>Included angle between the two flanks of a groove (degrees).</summary>
        public const double FlankAngleDeg = 14.0;

        public HtdSeries Series { get; }

        /// <summary>Designation, for example "5M".</summary>
        public string Name { get; }

        /// <summary>Distance between two teeth along the pitch line (mm).</summary>
        public double Pitch { get; }

        /// <summary>Distance between the pitch circle and the outside diameter (mm): the pitch line lies inside the belt's tension cord.</summary>
        public double PitchLineDifferential { get; }

        /// <summary>Height of the belt tooth, and so the depth of the pulley groove (mm).</summary>
        public double ToothHeight { get; }

        /// <summary>Radius of the belt tooth head, and so of the groove bottom (mm).</summary>
        public double HeadRadius { get; }

        /// <summary>Radius of the belt tooth root, and so of the rounded groove edge (mm).</summary>
        public double FootRadius { get; }

        /// <summary>Width of the belt tooth, and so of the groove at the outside diameter (mm), for reference.</summary>
        public double ToothWidth { get; }

        /// <summary>Overall belt thickness (mm), for reference.</summary>
        public double BeltThickness { get; }

        /// <summary>Smallest number of teeth that the belt makers recommend.</summary>
        public int MinimumTeeth { get; }

        /// <summary>A common belt width for this series (mm), used as the default width of a pulley.</summary>
        public double StandardBeltWidth
        {
            get
            {
                switch (Series)
                {
                    case HtdSeries.Htd3M: return 9.0;
                    case HtdSeries.Htd5M: return 15.0;
                    case HtdSeries.Htd8M: return 20.0;
                    default: return 40.0;
                }
            }
        }

        /// <summary>Pitch diameter of a pulley with <paramref name="teeth"/> grooves: z p / pi.</summary>
        public double PitchDiameter(int teeth) => teeth * Pitch / Math.PI;

        /// <summary>Outside diameter of a pulley with <paramref name="teeth"/> grooves: pitch diameter - 2 x pitch line differential.</summary>
        public double OutsideDiameter(int teeth) => PitchDiameter(teeth) - 2.0 * PitchLineDifferential;

        /// <summary>Diameter at the bottom of the grooves: outside diameter - 2 x groove depth.</summary>
        public double RootDiameter(int teeth) => OutsideDiameter(teeth) - 2.0 * ToothHeight;

        private static readonly HtdProfile[] All =
        {
            new HtdProfile(HtdSeries.Htd3M, "3M", 3.0, 0.381, 1.14, 0.88, 0.25, 1.96, 2.41, 10),
            new HtdProfile(HtdSeries.Htd5M, "5M", 5.0, 0.5715, 2.06, 1.49, 0.43, 3.05, 3.80, 14),
            new HtdProfile(HtdSeries.Htd8M, "8M", 8.0, 0.6858, 3.38, 2.46, 0.70, 5.15, 5.60, 22),
            new HtdProfile(HtdSeries.Htd14M, "14M", 14.0, 1.397, 6.02, 4.50, 1.25, 9.40, 10.00, 28)
        };

        /// <summary>Every series, smallest pitch first.</summary>
        public static IReadOnlyList<HtdProfile> Profiles => All;

        /// <summary>The dimensions of <paramref name="series"/>.</summary>
        public static HtdProfile Get(HtdSeries series)
        {
            foreach (var profile in All)
            {
                if (profile.Series == series) return profile;
            }

            throw new ArgumentOutOfRangeException(nameof(series), series, "Unknown HTD series.");
        }

        public override string ToString() => "HTD " + Name;
    }
}
