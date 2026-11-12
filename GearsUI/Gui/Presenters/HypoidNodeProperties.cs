using System;
using System.ComponentModel;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>
    /// Editable view of a hypoid gear in the property grid: the spiral properties, the offset of the shafts and the values of the pair at the
    /// mean point.
    /// </summary>
    public sealed class HypoidNodeProperties : SpiralBevelNodeProperties
    {
        private readonly HypoidGearComponent _hypoid;
        private readonly GearTrainPresenter _presenter;

        public HypoidNodeProperties(GearTrainPresenter presenter, HypoidGearComponent gear, ShaftMount mount, GearMeshRelation relation)
            : base(presenter, gear, mount, relation)
        {
            _hypoid = gear;
            _presenter = presenter;
        }

        [Category("Hypoid")]
        [DisplayName("Offset (mm)")]
        [Description("Offset of the pinion shaft from the gear shaft along their common perpendicular; it must not be zero and must be smaller than the mean pitch radius of the gear. The sign gives the side: for a positive offset the gear is right hand and the pinion left hand. Changing it sets the hands of both gears.")]
        public double Offset
        {
            get => _hypoid.OffsetMm;
            set => _presenter.SetHypoidOffset(_hypoid, value);
        }

        [Category("Hypoid")]
        [DisplayName("Pinion")]
        [Description("True for the small gear of the pair.")]
        public bool IsPinion => _hypoid.IsPinion;

        [Category("Hypoid")]
        [DisplayName("Offset angle (deg)")]
        public double OffsetAngle => _hypoid.Pair?.OffsetAngleDeg ?? 0.0;

        [Category("Hypoid")]
        [DisplayName("Pinion pitch radius at mean point (mm)")]
        [Description("Larger than the tooth count suggests: r1 / r2 = z1 cos(beta2) / (z2 cos(beta1)).")]
        public double PinionPitchRadius => _hypoid.Pair?.PinionPitchRadius ?? 0.0;

        [Category("Hypoid")]
        [DisplayName("Gear pitch radius at mean point (mm)")]
        public double GearPitchRadius => _hypoid.Pair?.GearPitchRadius ?? 0.0;

        [Category("Hypoid")]
        [DisplayName("Pinion spiral angle (deg)")]
        public double PinionSpiralAngle => _hypoid.Pair?.PinionSpiralAngleDeg ?? 0.0;

        [Category("Hypoid")]
        [DisplayName("Gear spiral angle (deg)")]
        public double GearSpiralAngle => _hypoid.Pair?.GearSpiralAngleDeg ?? 0.0;

        [Category("Hypoid")]
        [DisplayName("Pinion pitch cone angle (deg)")]
        public double PinionConeAngle => _hypoid.Pair?.PinionConeAngleDeg ?? 0.0;

        [Category("Hypoid")]
        [DisplayName("Gear pitch cone angle (deg)")]
        public double GearConeAngle => _hypoid.Pair?.GearConeAngleDeg ?? 0.0;

        [Category("Hypoid")]
        [DisplayName("Mean cone distance (mm)")]
        [Description("Distance from the apex of this gear to the middle of the face. The apexes of the two gears do not coincide.")]
        public double MeanConeDistance => _hypoid.MeanConeDistance;
    }
}
