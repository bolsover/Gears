using System;
using System.ComponentModel;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>
    /// Editable view of a roller chain in the property grid: its name, the centre distance and direction of the line of centres (which place the
    /// driven sprocket's shaft), and the chain data that follows from them.
    /// </summary>
    public sealed class ChainNodeProperties
    {
        private readonly RollerChain _chain;

        public ChainNodeProperties(RollerChain chain)
        {
            _chain = chain ?? throw new ArgumentNullException(nameof(chain));
        }

        [Category("General")]
        [DisplayName("Id")]
        public string Id => _chain.Id;

        [Category("General")]
        [DisplayName("Name")]
        public string Name
        {
            get => _chain.Name;
            set
            {
                if (!string.IsNullOrWhiteSpace(value)) _chain.Name = value;
            }
        }

        [Category("General")]
        [DisplayName("Chain")]
        public string Series => _chain.ToString();

        [Category("General")]
        [DisplayName("Driver sprocket")]
        public string Driver => _chain.Driver.Name;

        [Category("General")]
        [DisplayName("Driven sprocket")]
        public string Driven => _chain.Driven.Name;

        [Category("Position")]
        [DisplayName("Centre distance (mm)")]
        [Description("Distance between the two shaft axes. It must be larger than the minimum so that the sprockets do not touch; chain makers advise 30 to 50 pitches.")]
        public double CentreDistance
        {
            get => _chain.CentreDistance;
            set => _chain.CentreDistance = value;
        }

        [Category("Position")]
        [DisplayName("Line of centres angle (deg)")]
        [Description("Direction of the driven sprocket's shaft seen from the driver's shaft, measured from the driver shaft's X axis.")]
        public double CentreLineAngleDeg
        {
            get => _chain.CentreLineAngleDeg;
            set => _chain.CentreLineAngleDeg = value;
        }

        [Category("Calculated")]
        [DisplayName("Minimum centre distance (mm)")]
        public double MinimumCentreDistance => _chain.MinimumCentreDistance;

        [Category("Calculated")]
        [DisplayName("Links")]
        [Description("Chain length in pitches rounded to the nearest even number, so that inner and outer links alternate without an offset link.")]
        public int Links => _chain.LinkCount;

        [Category("Calculated")]
        [DisplayName("Chain length (mm)")]
        [Description("Links times the pitch: the length of the chain as it is made.")]
        public double ChainLength => _chain.ChainLength;

        [Category("Calculated")]
        [DisplayName("Path round the pitch circles (mm)")]
        public double PitchPathLength => _chain.PitchPathLength;

        [Category("Calculated")]
        [DisplayName("Width over the plates (mm)")]
        public double Width => _chain.Width;

        [Category("Calculated")]
        [DisplayName("Speed ratio (driver : driven)")]
        public double Ratio => _chain.GearRatio;

        [Category("Calculated")]
        [DisplayName("Wrap on small sprocket (deg)")]
        public double Wrap => _chain.SmallSprocketWrapDegrees;

        [Category("Calculated")]
        [DisplayName("Wrap below recommended")]
        [Description("True when the chain wraps the smaller sprocket by less than 120 degrees.")]
        public bool WrapBelowRecommended => _chain.IsBelowRecommendedWrap;
    }
}
