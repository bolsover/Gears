using System;
using System.ComponentModel;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>Editable view of a rack in the property grid.</summary>
    public sealed class RackNodeProperties : ComponentNodePropertiesBase
    {
        private readonly RackComponent _rack;

        public RackNodeProperties(GearTrainPresenter presenter, RackComponent rack, ShaftMount mount, GearMeshRelation relation)
            : base(presenter, rack, mount, relation)
        {
            _rack = rack;
        }

        [Category("Teeth")]
        [DisplayName("Teeth")]
        public int Teeth
        {
            get => _rack.Teeth;
            set => _rack.Teeth = Math.Max(1, value);
        }

        [Category("Teeth")]
        [DisplayName("Backlash (mm)")]
        public double Backlash
        {
            get => _rack.Backlash;
            set => _rack.Backlash = value;
        }

        [Category("Teeth")]
        [DisplayName("Helix angle (deg)")]
        [Description("0 for straight teeth. A helical rack meshes with a pinion of the same helix angle and the opposite hand.")]
        public double HelixAngle
        {
            get => _rack.HelixAngleDeg;
            set => _rack.HelixAngleDeg = Math.Max(0.0, value);
        }

        [Category("Teeth")]
        [DisplayName("Helix hand (R/L)")]
        [Description("Hand of the rack teeth: R or L. The meshing pinion has the opposite hand.")]
        public string HelixHand
        {
            get => _rack.HelicalDirection;
            set => _rack.HelicalDirection = value;
        }

        [Category("Teeth")]
        [DisplayName("Herringbone")]
        [Description("A helical rack mirrored about its middle plane: each half of the face width has the helix angle, with opposite hands. Needs a helix angle above zero.")]
        public bool IsHerringbone
        {
            get => _rack.IsHerringbone;
            set => _rack.IsHerringbone = value;
        }

        [Category("Dimensions")]
        [DisplayName("Body height (mm)")]
        public double BodyHeight
        {
            get => _rack.BodyHeight;
            set => _rack.BodyHeight = value;
        }

        [Category("Dimensions")]
        [DisplayName("Length (mm)")]
        public double Length => _rack.Length;
    }
}
