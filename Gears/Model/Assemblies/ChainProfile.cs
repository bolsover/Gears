using System;
using System.Collections.Generic;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>The ISO 606 (DIN 8187) B-series simplex roller chains that have sprockets here. The number is the pitch in sixteenths of an inch.</summary>
    public enum ChainSeries
    {
        B06,
        B08,
        B10,
        B12,
        B16,
        B20,
        B24,
        B28,
        B32,
        B40
    }

    /// <summary>
    /// Which end of the tolerance band of the ISO 606 tooth form is drawn. The standard gives each of the roller seating radius, the seating
    /// angle, the tooth flank radius and the tip diameter as a minimum and a maximum; the two forms use the values the standard groups
    /// together.
    /// </summary>
    public enum SprocketToothForm
    {
        /// <summary>Maximum seating radius and flank radius, minimum seating angle, maximum tip diameter: the widest gap and the tallest tooth.</summary>
        Maximum,

        /// <summary>Minimum seating radius and flank radius, maximum seating angle, minimum tip diameter: the tightest gap and the shortest tooth.</summary>
        Minimum
    }

    /// <summary>
    /// Dimensions of one ISO 606 B-series chain (all in millimetres) and the ISO 606 sprocket tooth-form formulae for it.
    /// <para>
    /// Sources: ISO 606:2004 / DIN 8187 as published by the chain and sprocket makers (SIT, DH Chain, Renold, Ametric, ZMC, HZPT): pitch p,
    /// roller diameter d1 (maximum) and inner width b1 (minimum), and for the drawing of the chain the pin diameter, the link plate height and
    /// the plate thicknesses (maximum values, indicative only). Tooth form, with z teeth:
    /// pitch diameter d = p / sin(180/z); root diameter df = d - d1; roller seating radius ri from 0.505 d1 (minimum) to
    /// 0.505 d1 + 0.069 d1^(1/3) (maximum); seating angle from 120 - 90/z (minimum) to 140 - 90/z (maximum) degrees; tooth flank radius re
    /// from 0.12 d1 (z + 2) (minimum) to 0.008 d1 (z^2 + 180) (maximum); tip diameter da from d + p (1 - 1.6/z) - d1 (minimum) to
    /// d + 1.25 p - d1 (maximum); tooth width of a simplex sprocket 0.93 b1 (p up to 12.7 mm) or 0.95 b1 (larger pitch).
    /// The tooth side relief (the rounding of the tooth sides towards the tip) has the tooth radius r3 and the radius width C of the SIT
    /// sprocket catalogue (DIN 8187 / ISO 606 sprockets): r3 is about the pitch, C about a tenth of it. 40B is not in the catalogue; its
    /// values follow the same trend.
    /// </para>
    /// </summary>
    public sealed class ChainProfile
    {
        private ChainProfile(
            ChainSeries series, string name, double pitch, double rollerDiameter, double innerWidth, double pinDiameter, double plateHeight,
            double innerPlateThickness, double outerPlateThickness, double sideRadius, double sideReliefWidth)
        {
            Series = series;
            Name = name;
            Pitch = pitch;
            RollerDiameter = rollerDiameter;
            InnerWidth = innerWidth;
            PinDiameter = pinDiameter;
            PlateHeight = plateHeight;
            InnerPlateThickness = innerPlateThickness;
            OuterPlateThickness = outerPlateThickness;
            SideRadius = sideRadius;
            SideReliefWidth = sideReliefWidth;
        }

        /// <summary>The fewest teeth that are accepted: fewer leave no tooth between the seats.</summary>
        public const int AbsoluteMinimumTeeth = 6;

        /// <summary>The fewest teeth the chain makers recommend for a sprocket.</summary>
        public const int RecommendedMinimumTeeth = 9;

        public ChainSeries Series { get; }

        /// <summary>Designation, for example "08B".</summary>
        public string Name { get; }

        /// <summary>Distance between two rollers of the straight chain (mm).</summary>
        public double Pitch { get; }

        /// <summary>Largest roller diameter d1 (mm).</summary>
        public double RollerDiameter { get; }

        /// <summary>Smallest width between the inner plates b1, which is the width of the roller (mm).</summary>
        public double InnerWidth { get; }

        /// <summary>Pin diameter (mm).</summary>
        public double PinDiameter { get; }

        /// <summary>Height of the link plates (mm).</summary>
        public double PlateHeight { get; }

        /// <summary>Thickness of an inner link plate (mm).</summary>
        public double InnerPlateThickness { get; }

        /// <summary>Thickness of an outer link plate (mm).</summary>
        public double OuterPlateThickness { get; }

        /// <summary>Tooth radius r3 of the side relief: the radius of the arc that rounds the tooth side towards the tip (mm).</summary>
        public double SideRadius { get; }

        /// <summary>Radius width C of the side relief: how far the tooth side is cut back at the tip circle, on each side (mm).</summary>
        public double SideReliefWidth { get; }

        /// <summary>Width over the outer plates of the simplex chain (mm): inner width plus two inner and two outer plates.</summary>
        public double OverallWidth => InnerWidth + 2.0 * (InnerPlateThickness + OuterPlateThickness);

        /// <summary>Width of the teeth of a simplex sprocket (mm): 0.93 b1 for a pitch up to 12.7 mm, else 0.95 b1.</summary>
        public double ToothWidth => (Pitch <= 12.7 + 1e-9 ? 0.93 : 0.95) * InnerWidth;

        /// <summary>Pitch diameter of a sprocket with <paramref name="teeth"/> teeth: p / sin(180 / z).</summary>
        public double PitchDiameter(int teeth) => Pitch / Math.Sin(Math.PI / teeth);

        /// <summary>Diameter at the bottom of the roller seats: pitch diameter minus the roller diameter.</summary>
        public double RootDiameter(int teeth) => PitchDiameter(teeth) - RollerDiameter;

        /// <summary>Radius of the roller seating arc (mm).</summary>
        public double SeatRadius(SprocketToothForm form)
        {
            var minimum = 0.505 * RollerDiameter;
            return form == SprocketToothForm.Maximum ? minimum + 0.069 * Math.Pow(RollerDiameter, 1.0 / 3.0) : minimum;
        }

        /// <summary>Angle of the roller seating arc (radians).</summary>
        public static double SeatAngle(int teeth, SprocketToothForm form)
        {
            var degrees = (form == SprocketToothForm.Maximum ? 120.0 : 140.0) - 90.0 / teeth;
            return degrees * Math.PI / 180.0;
        }

        /// <summary>Radius of the tooth flank arc (mm).</summary>
        public double FlankRadius(int teeth, SprocketToothForm form)
        {
            return form == SprocketToothForm.Maximum
                ? 0.008 * RollerDiameter * ((double)teeth * teeth + 180.0)
                : 0.12 * RollerDiameter * (teeth + 2.0);
        }

        /// <summary>Tip diameter of the teeth (mm).</summary>
        public double TipDiameter(int teeth, SprocketToothForm form)
        {
            var d = PitchDiameter(teeth);
            return form == SprocketToothForm.Maximum
                ? d + 1.25 * Pitch - RollerDiameter
                : d + Pitch * (1.0 - 1.6 / teeth) - RollerDiameter;
        }

        private static readonly ChainProfile[] All =
        {
            new ChainProfile(ChainSeries.B06, "06B", 9.525, 6.35, 5.72, 3.28, 8.26, 1.30, 1.04, 10.0, 1.0),
            new ChainProfile(ChainSeries.B08, "08B", 12.70, 8.51, 7.75, 4.45, 11.81, 1.55, 1.55, 13.0, 1.3),
            new ChainProfile(ChainSeries.B10, "10B", 15.875, 10.16, 9.65, 5.08, 14.73, 1.55, 1.55, 16.0, 1.6),
            new ChainProfile(ChainSeries.B12, "12B", 19.05, 12.07, 11.68, 5.72, 16.13, 1.80, 1.80, 19.0, 2.0),
            new ChainProfile(ChainSeries.B16, "16B", 25.40, 15.88, 17.02, 8.28, 21.08, 4.11, 3.10, 26.0, 2.5),
            new ChainProfile(ChainSeries.B20, "20B", 31.75, 19.05, 19.56, 10.19, 26.42, 4.62, 3.61, 32.0, 3.5),
            new ChainProfile(ChainSeries.B24, "24B", 38.10, 25.40, 25.40, 14.63, 33.40, 6.10, 5.08, 38.0, 4.0),
            new ChainProfile(ChainSeries.B28, "28B", 44.45, 27.94, 30.99, 15.90, 37.08, 7.62, 6.35, 44.0, 5.0),
            new ChainProfile(ChainSeries.B32, "32B", 50.80, 29.21, 30.99, 17.81, 42.29, 7.11, 6.35, 51.0, 6.0),
            new ChainProfile(ChainSeries.B40, "40B", 63.50, 39.37, 38.10, 22.89, 52.96, 8.13, 8.13, 64.0, 7.5)
        };

        /// <summary>Every series, smallest pitch first.</summary>
        public static IReadOnlyList<ChainProfile> Profiles => All;

        /// <summary>The dimensions of <paramref name="series"/>.</summary>
        public static ChainProfile Get(ChainSeries series)
        {
            foreach (var profile in All)
            {
                if (profile.Series == series) return profile;
            }

            throw new ArgumentOutOfRangeException(nameof(series), series, "Unknown chain series.");
        }

        public override string ToString() => "ISO 606 " + Name + "-1";
    }
}
