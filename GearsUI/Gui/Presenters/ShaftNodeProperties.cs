using System;
using System.ComponentModel;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>Editable view of a shaft in the property grid.</summary>
    public sealed class ShaftNodeProperties
    {
        private readonly GearTrainPresenter _presenter;
        private readonly Shaft _shaft;

        public ShaftNodeProperties(GearTrainPresenter presenter, Shaft shaft)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _shaft = shaft ?? throw new ArgumentNullException(nameof(shaft));
        }

        [Category("General")]
        [DisplayName("Id")]
        public string Id => _shaft.Id;

        [Category("General")]
        [DisplayName("Input shaft")]
        public bool IsInput => _presenter.IsInputShaft(_shaft);

        [Category("Dimensions")]
        [DisplayName("Diameter (mm)")]
        public double Diameter
        {
            get => _shaft.Diameter;
            set => _shaft.Diameter = value;
        }

        [Category("Dimensions")]
        [DisplayName("Length (mm, 0 = automatic)")]
        public double Length
        {
            get => _shaft.Length;
            set => _shaft.Length = Math.Max(0.0, value);
        }

        [Category("Dimensions")]
        [DisplayName("End margin (mm)")]
        public double EndMargin
        {
            get => _shaft.EndMargin;
            set => _shaft.EndMargin = Math.Max(0.0, value);
        }

        [Category("Position")]
        [DisplayName("Rotation (deg)")]
        [Description("Input angle of the input shaft. For other shafts this is solved from the meshing and cannot be edited.")]
        public double RotationDeg
        {
            get => _shaft.RotationRad * 180.0 / Math.PI;
            set
            {
                if (_presenter.IsInputShaft(_shaft)) _shaft.RotationRad = value * Math.PI / 180.0;
            }
        }

        [Category("Position")]
        [DisplayName("Origin (mm)")]
        public string Origin => _shaft.Placement.Origin.ToString();
    }
}
