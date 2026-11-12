using System;
using System.ComponentModel;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>
    /// Editable view of a spiral or zerol bevel gear in the property grid: the bevel gear properties and the tooth trace (spiral angle, hand,
    /// cutter radius). The values are set on both gears of a pair, and the mate gets the opposite hand.
    /// </summary>
    public class SpiralBevelNodeProperties : BevelNodeProperties
    {
        private readonly SpiralBevelGearComponent _spiral;
        private readonly GearTrainPresenter _presenter;

        public SpiralBevelNodeProperties(GearTrainPresenter presenter, SpiralBevelGearComponent gear, ShaftMount mount, GearMeshRelation relation)
            : base(presenter, gear, mount, relation)
        {
            _spiral = gear;
            _presenter = presenter;
        }

        [Category("Spiral teeth")]
        [DisplayName("Mean spiral angle (deg)")]
        [Description("Spiral angle of the tooth line at the middle of the face, from 0 to 60 degrees (typically 35). It is zero for a zerol gear. For a hypoid pair it is the angle of the pinion. Both gears of a pair have the same angle.")]
        public double MeanSpiralAngle
        {
            get => _spiral.MeanSpiralAngleDeg;
            set => _presenter.SetSpiralParameters(_spiral, value, null);
        }

        [Category("Spiral teeth")]
        [DisplayName("Hand")]
        [Description("Right: the tooth line advances counter-clockwise from the heel to the toe, looking along the axis towards the apex. The mate gets the opposite hand. The hands of a hypoid pair follow from the side of the offset.")]
        public SpiralHand Hand
        {
            get => _spiral.Hand;
            set => _presenter.SetSpiralHand(_spiral, value);
        }

        [Category("Spiral teeth")]
        [DisplayName("Cutter radius (mm, 0 = automatic)")]
        [Description("Radius of the face-mill cutter that cuts the tooth line as a circular arc. 0 uses the mean cone distance. It must be large enough to reach the whole face.")]
        public double CutterRadius
        {
            get => _spiral.CutterRadius;
            set => _presenter.SetSpiralParameters(_spiral, null, value);
        }

        [Category("Spiral teeth")]
        [DisplayName("Spiral angle at mid face (deg)")]
        [Description("The spiral angle of this gear at the middle of the face; it differs from the mean spiral angle for the gear of a hypoid pair.")]
        public double SpiralAngleAtMidFace => _spiral.EffectiveSpiralAngleDeg;

        [Category("Spiral teeth")]
        [DisplayName("Transverse pressure angle (deg)")]
        [Description("tan(transverse) = tan(normal pressure angle) / cos(spiral angle): the angle of the tooth profile in the plane perpendicular to the axis.")]
        public double TransversePressureAngle => _spiral.TransversePressureAngleDeg;
    }
}
