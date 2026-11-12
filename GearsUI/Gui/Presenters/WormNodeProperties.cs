using System;
using System.ComponentModel;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>Editable view of a worm in the property grid.</summary>
    public sealed class WormNodeProperties : ComponentNodePropertiesBase
    {
        private readonly WormComponent _worm;

        public WormNodeProperties(GearTrainPresenter presenter, WormComponent worm, ShaftMount mount, GearMeshRelation relation)
            : base(presenter, worm, mount, relation)
        {
            _worm = worm;
        }

        [Category("Teeth")]
        [DisplayName("Starts")]
        public int Starts
        {
            get => _worm.Starts;
            set => _worm.Starts = Math.Max(1, value);
        }

        [Category("Teeth")]
        [DisplayName("Diameter quotient q")]
        [Description("Pitch diameter divided by the module.")]
        public double DiameterQuotient
        {
            get => _worm.DiameterQuotient;
            set => _worm.DiameterQuotient = value;
        }

        [Category("Teeth")]
        [DisplayName("Hand (R/L)")]
        public string HelicalDirection
        {
            get => _worm.HelicalDirection;
            set => _worm.HelicalDirection = value;
        }

        [Category("Teeth")]
        [DisplayName("Lead angle (deg)")]
        public double LeadAngle => _worm.LeadAngleDeg;

        [Category("Teeth")]
        [DisplayName("Lead (mm)")]
        public double Lead => _worm.Lead;
    }
}
