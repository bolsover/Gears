using System;
using System.ComponentModel;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>
    /// Editable view of a cycloidal gear in the property grid: wheel or pinion, the number of teeth, the clearance and the centre hole,
    /// and the tooth dimensions that follow from the pair.
    /// </summary>
    public sealed class CycloidalNodeProperties : ComponentNodePropertiesBase
    {
        private readonly CycloidalGearComponent _gear;

        public CycloidalNodeProperties(GearTrainPresenter presenter, CycloidalGearComponent gear, ShaftMount mount, GearMeshRelation relation)
            : base(presenter, gear, mount, relation)
        {
            _gear = gear;
        }

        [Category("Teeth")]
        [DisplayName("Role")]
        [Description("Cycloidal gears are designed as a pair: a wheel and a pinion. Meshing gears must be one of each.")]
        public CycloidalGearRole Role
        {
            get => _gear.Role;
            set => _gear.Role = value;
        }

        [Category("Teeth")]
        [DisplayName("Teeth")]
        [Description("Number of teeth of the wheel, or leaves of the pinion.")]
        public int Teeth
        {
            get => _gear.Teeth;
            set => _gear.Teeth = Math.Max(CycloidalGearComponent.AbsoluteMinimumTeeth, value);
        }

        [Category("Teeth")]
        [DisplayName("Teeth of mate")]
        [Description("Number of teeth of the gear that this one works with. The tooth profile of the pair depends on both numbers; the editor sets it from the meshing gear.")]
        public int MateTeeth => _gear.EffectiveMateTeeth;

        [Category("Teeth")]
        [DisplayName("Custom clearance")]
        [Description("Gives the pinion root a clearance over the wheel tip that is set below, instead of the standard 0.4 module.")]
        public bool CustomSlopEnabled
        {
            get => _gear.CustomSlopEnabled;
            set => _gear.CustomSlopEnabled = value;
        }

        [Category("Teeth")]
        [DisplayName("Clearance (mm)")]
        [Description("Clearance between the wheel tip and the pinion root; used when Custom clearance is checked.")]
        public double CustomSlop
        {
            get => _gear.CustomSlop;
            set => _gear.CustomSlop = Math.Max(0.0, value);
        }

        [Category("Dimensions")]
        [DisplayName("Centre hole (mm)")]
        [Description("Diameter of a hole through the centre; zero for none. The hole is part of the Alibre export; the 3D view shows the solid gear.")]
        public double CentreHole
        {
            get => _gear.CentreHole;
            set => _gear.CentreHole = Math.Max(0.0, value);
        }

        [Category("Calculated")]
        [DisplayName("Pitch diameter (mm)")]
        public double PitchDiameter => _gear.PitchDiameter;

        [Category("Calculated")]
        [DisplayName("Outside diameter (mm)")]
        public double OutsideDiameter => _gear.OutsideDiameter;

        [Category("Calculated")]
        [DisplayName("Root diameter (mm)")]
        public double RootDiameter => _gear.RootDiameter;

        [Category("Calculated")]
        [DisplayName("Addendum (mm)")]
        public double Addendum => _gear.Addendum;

        [Category("Calculated")]
        [DisplayName("Dedendum (mm)")]
        public double Dedendum => _gear.Dedendum;

        [Category("Calculated")]
        [DisplayName("Addendum arc radius (mm)")]
        public double AddendumRadius => _gear.AddendumRadius;

        [Category("Calculated")]
        [DisplayName("Circular pitch (mm)")]
        public double CircularPitch => _gear.CircularPitch;

        [Category("Calculated")]
        [DisplayName("Pair ratio (wheel : pinion)")]
        public double PairRatio => _gear.PairRatio;
    }
}
