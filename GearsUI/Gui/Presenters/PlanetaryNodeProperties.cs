using System;
using System.ComponentModel;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>Editable view of a planetary stage in the property grid.</summary>
    public sealed class PlanetaryNodeProperties
    {
        private readonly GearTrainPresenter _presenter;
        private readonly PlanetaryGearSet _set;

        public PlanetaryNodeProperties(GearTrainPresenter presenter, PlanetaryGearSet set)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _set = set ?? throw new ArgumentNullException(nameof(set));
        }

        [Category("General")]
        [DisplayName("Id")]
        public string Id => _set.Id;

        [Category("Kinematics")]
        [DisplayName("Fixed member")]
        public PlanetaryMember Fixed
        {
            get => _set.Fixed;
            set => _set.Fixed = value;
        }

        [Category("Kinematics")]
        [DisplayName("Input member")]
        [Description("The member that is turned from outside. It must differ from the fixed member.")]
        public PlanetaryMember Input
        {
            get => _presenter.PlanetaryInput;
            set => _presenter.PlanetaryInput = value;
        }

        [Category("Teeth")]
        [DisplayName("Sun teeth")]
        public int SunTeeth
        {
            get => _set.SunTeeth;
            set => _set.SunTeeth = value;
        }

        [Category("Teeth")]
        [DisplayName("Planet teeth")]
        public int PlanetTeeth
        {
            get => _set.PlanetTeeth;
            set => _set.PlanetTeeth = value;
        }

        [Category("Teeth")]
        [DisplayName("Ring teeth (Zs + 2 Zp)")]
        public int RingTeeth => _set.RingTeeth;

        [Category("Teeth")]
        [DisplayName("Number of planets")]
        [Description("(Zs + Zr) / N must be a whole number for the planets to be spaced equally.")]
        public int PlanetCount
        {
            get => _set.PlanetCount;
            set => _set.PlanetCount = value;
        }

        [Category("Teeth")]
        [DisplayName("Module (mm)")]
        public double Module
        {
            get => _set.Module;
            set => _set.Module = value;
        }

        [Category("Teeth")]
        [DisplayName("Pressure angle (deg)")]
        public double PressureAngle
        {
            get => _set.PressureAngleDeg;
            set => _set.PressureAngleDeg = value;
        }

        [Category("Teeth")]
        [DisplayName("Backlash (mm)")]
        public double Backlash
        {
            get => _set.Backlash;
            set => _set.Backlash = value;
        }

        [Category("Teeth")]
        [DisplayName("Helix angle (deg)")]
        [Description("Helix angle of the sun, planets and ring. Zero gives spur gears.")]
        public double HelixAngle
        {
            get => _set.HelixAngleDeg;
            set => _set.HelixAngleDeg = value;
        }

        [Category("Teeth")]
        [DisplayName("Sun helix direction (R/L)")]
        [Description("Hand of the sun gear. The planets get the opposite hand and the ring the hand of the planets.")]
        public string HelicalDirection
        {
            get => _set.HelicalDirection;
            set => _set.HelicalDirection = string.Equals(value?.Trim(), "L", StringComparison.OrdinalIgnoreCase) ? "L" : "R";
        }

        [Category("Teeth")]
        [DisplayName("Herringbone")]
        [Description("Every gear is a helical gear mirrored about its middle plane. Needs a helix angle above zero.")]
        public bool IsHerringbone
        {
            get => _set.IsHerringbone;
            set => _set.IsHerringbone = value;
        }

        [Category("Dimensions")]
        [DisplayName("Face width (mm)")]
        public double FaceWidth
        {
            get => _set.FaceWidth;
            set => _set.FaceWidth = value;
        }

        [Category("Dimensions")]
        [DisplayName("Ring rim thickness (mm)")]
        public double RingRimThickness
        {
            get => _set.RingRimThickness;
            set => _set.RingRimThickness = value;
        }

        [Category("Dimensions")]
        [DisplayName("Centre distance (mm)")]
        public double CentreDistance => _set.CentreDistance;
    }
}
