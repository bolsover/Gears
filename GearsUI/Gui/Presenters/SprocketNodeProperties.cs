using System;
using System.ComponentModel;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>
    /// Editable view of an ISO 606 roller chain sprocket in the property grid: the chain series, number of teeth and tooth form, the width,
    /// bore and hub, the axial position, and the diameters and tooth form dimensions that follow from them.
    /// </summary>
    public sealed class SprocketNodeProperties
    {
        private readonly SprocketComponent _sprocket;
        private readonly ShaftMount _mount;

        public SprocketNodeProperties(GearTrainPresenter presenter, SprocketComponent sprocket, ShaftMount mount, GearMeshRelation relation)
        {
            if (presenter == null) throw new ArgumentNullException(nameof(presenter));
            _sprocket = sprocket ?? throw new ArgumentNullException(nameof(sprocket));
            _mount = mount ?? throw new ArgumentNullException(nameof(mount));
        }

        [Category("General")]
        [DisplayName("Id")]
        public string Id => _sprocket.Id;

        [Category("General")]
        [DisplayName("Type")]
        public string Type => _sprocket.TypeName;

        [Category("General")]
        [DisplayName("Name")]
        public string Name
        {
            get => _sprocket.Name;
            set
            {
                if (!string.IsNullOrWhiteSpace(value)) _sprocket.Name = value;
            }
        }

        [Category("General")]
        [DisplayName("Shaft")]
        public string ShaftId => _mount.Shaft?.Id;

        [Category("Teeth")]
        [DisplayName("Chain series")]
        [Description("The ISO 606 (DIN 8187) B-series simplex chain, for example B08 for 08B-1 (12.7 mm pitch). The number is the pitch in sixteenths of an inch.")]
        public ChainSeries Series
        {
            get => _sprocket.Series;
            set => _sprocket.Series = value;
        }

        [Category("Teeth")]
        [DisplayName("Teeth")]
        [Description("Number of teeth of the sprocket.")]
        public int Teeth
        {
            get => _sprocket.Teeth;
            set => _sprocket.Teeth = Math.Max(ChainProfile.AbsoluteMinimumTeeth, value);
        }

        [Category("Teeth")]
        [DisplayName("Below recommended minimum")]
        [Description("True when the sprocket has fewer teeth than the chain makers recommend.")]
        public bool BelowRecommendedMinimum => _sprocket.IsBelowRecommendedTeeth;

        [Category("Teeth")]
        [DisplayName("Tooth form")]
        [Description("Which end of the ISO 606 tolerance band is drawn: Maximum (the widest roller seat and the tallest tooth) or Minimum (the tightest seat and the shortest tooth).")]
        public SprocketToothForm ToothForm
        {
            get => _sprocket.ToothForm;
            set => _sprocket.ToothForm = value;
        }

        [Category("Dimensions")]
        [DisplayName("Width (mm)")]
        [Description("Width of the teeth, along the axis. ISO 606 gives 0.93 x the inner width of the chain for a pitch up to 12.7 mm and 0.95 x above that.")]
        public double FaceWidth
        {
            get => _sprocket.FaceWidth;
            set => _sprocket.FaceWidth = value;
        }

        [Category("Dimensions")]
        [DisplayName("Bore diameter (mm)")]
        [Description("Diameter of the bore through the sprocket; zero for a solid sprocket (the shaft of the editor passes through it).")]
        public double BoreDiameter
        {
            get => _sprocket.BoreDiameter;
            set => _sprocket.BoreDiameter = Math.Max(0.0, value);
        }

        [Category("Dimensions")]
        [DisplayName("Hub diameter (mm)")]
        [Description("Diameter of the hub on the +Z side of the sprocket. The hub shows when it is larger than the bore and its length is positive.")]
        public double HubDiameter
        {
            get => _sprocket.HubDiameter;
            set => _sprocket.HubDiameter = Math.Max(0.0, value);
        }

        [Category("Dimensions")]
        [DisplayName("Hub length (mm)")]
        [Description("Length of the hub beyond the toothed width; zero for no hub.")]
        public double HubLength
        {
            get => _sprocket.HubLength;
            set => _sprocket.HubLength = Math.Max(0.0, value);
        }

        [Category("Calculated")]
        [DisplayName("Chain pitch (mm)")]
        public double Pitch => _sprocket.Profile.Pitch;

        [Category("Calculated")]
        [DisplayName("Roller diameter (mm)")]
        public double RollerDiameter => _sprocket.Profile.RollerDiameter;

        [Category("Calculated")]
        [DisplayName("Chain inner width (mm)")]
        public double InnerWidth => _sprocket.Profile.InnerWidth;

        [Category("Calculated")]
        [DisplayName("Pitch diameter (mm)")]
        [Description("Chain pitch / sin(180 deg / teeth).")]
        public double PitchDiameter => _sprocket.PitchDiameter;

        [Category("Calculated")]
        [DisplayName("Root diameter (mm)")]
        [Description("Pitch diameter minus the roller diameter.")]
        public double RootDiameter => _sprocket.RootDiameter;

        [Category("Calculated")]
        [DisplayName("Tip diameter (mm)")]
        [Description("The tip diameter of the tooth form shown. A pointed tooth ends below it.")]
        public double OutsideDiameter => _sprocket.OutsideDiameter;

        [Category("Calculated")]
        [DisplayName("Roller seating radius (mm)")]
        public double SeatRadius => _sprocket.Profile.SeatRadius(_sprocket.ToothForm);

        [Category("Calculated")]
        [DisplayName("Roller seating angle (deg)")]
        public double SeatAngleDeg => ChainProfile.SeatAngle(_sprocket.Teeth, _sprocket.ToothForm) * 180.0 / Math.PI;

        [Category("Calculated")]
        [DisplayName("Tooth flank radius (mm)")]
        public double FlankRadius => _sprocket.Profile.FlankRadius(_sprocket.Teeth, _sprocket.ToothForm);

        [Category("Position")]
        [DisplayName("Axial offset (mm)")]
        [Description("Position of the sprocket's mid-plane along its shaft.")]
        public double AxialOffset
        {
            get => _mount.AxialOffset;
            set => _mount.AxialOffset = value;
        }
    }
}
