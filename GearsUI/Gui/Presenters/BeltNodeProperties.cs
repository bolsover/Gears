using System;
using System.ComponentModel;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>
    /// Editable view of a timing belt in the property grid: its name, the centre distance and direction of the line of centres (which place
    /// the driven pulley's shaft), and the belt data that follows from them.
    /// </summary>
    public sealed class BeltNodeProperties
    {
        private readonly TimingBelt _belt;

        public BeltNodeProperties(TimingBelt belt)
        {
            _belt = belt ?? throw new ArgumentNullException(nameof(belt));
        }

        [Category("General")]
        [DisplayName("Id")]
        public string Id => _belt.Id;

        [Category("General")]
        [DisplayName("Name")]
        public string Name
        {
            get => _belt.Name;
            set
            {
                if (!string.IsNullOrWhiteSpace(value)) _belt.Name = value;
            }
        }

        [Category("General")]
        [DisplayName("Belt series")]
        public string Series => "HTD " + _belt.Profile.Name;

        [Category("General")]
        [DisplayName("Driver pulley")]
        public string Driver => _belt.Driver.Name;

        [Category("General")]
        [DisplayName("Driven pulley")]
        public string Driven => _belt.Driven.Name;

        [Category("Position")]
        [DisplayName("Centre distance (mm)")]
        [Description("Distance between the two shaft axes. It must be larger than the minimum so that the pulleys do not touch.")]
        public double CentreDistance
        {
            get => _belt.CentreDistance;
            set => _belt.CentreDistance = value;
        }

        [Category("Position")]
        [DisplayName("Line of centres angle (deg)")]
        [Description("Direction of the driven pulley's shaft seen from the driver's shaft, measured from the driver shaft's X axis.")]
        public double CentreLineAngleDeg
        {
            get => _belt.CentreLineAngleDeg;
            set => _belt.CentreLineAngleDeg = value;
        }

        [Category("Calculated")]
        [DisplayName("Minimum centre distance (mm)")]
        public double MinimumCentreDistance => _belt.MinimumCentreDistance;

        [Category("Calculated")]
        [DisplayName("Belt teeth")]
        [Description("Pitch path length divided by the pitch, rounded to a whole tooth.")]
        public int BeltTeeth => _belt.BeltTeeth;

        [Category("Calculated")]
        [DisplayName("Pitch length (mm)")]
        [Description("Belt teeth times the pitch: the length of the belt as it is made.")]
        public double PitchLength => _belt.PitchLength;

        [Category("Calculated")]
        [DisplayName("Pitch path at this distance (mm)")]
        public double PitchPathLength => _belt.PitchPathLength;

        [Category("Calculated")]
        [DisplayName("Width (mm)")]
        public double Width => _belt.Width;

        [Category("Calculated")]
        [DisplayName("Speed ratio (driver : driven)")]
        public double Ratio => _belt.GearRatio;

        [Category("Calculated")]
        [DisplayName("Wrap on small pulley (deg)")]
        public double Wrap => _belt.SmallPulleyWrapDegrees;
    }
}
