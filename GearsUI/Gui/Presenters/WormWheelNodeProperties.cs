using System;
using System.ComponentModel;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>Editable view of a worm wheel in the property grid.</summary>
    public sealed class WormWheelNodeProperties : ComponentNodePropertiesBase
    {
        private readonly WormWheelComponent _wheel;

        public WormWheelNodeProperties(GearTrainPresenter presenter, WormWheelComponent wheel, ShaftMount mount, GearMeshRelation relation)
            : base(presenter, wheel, mount, relation)
        {
            _wheel = wheel;
        }

        [Category("Teeth")]
        [DisplayName("Teeth")]
        public int Teeth
        {
            get => _wheel.Teeth;
            set => _wheel.Teeth = Math.Max(1, value);
        }

        [Category("Teeth")]
        [DisplayName("Backlash (mm)")]
        public double Backlash
        {
            get => _wheel.Backlash;
            set => _wheel.Backlash = value;
        }

        [Category("Teeth")]
        [DisplayName("Lead angle (deg)")]
        [Description("Taken from the worm that drives this wheel.")]
        public double LeadAngle => _wheel.LeadAngleDeg;

        [Category("Teeth")]
        [DisplayName("Hand (R/L)")]
        public string HelicalDirection => _wheel.HelicalDirection;
    }
}
