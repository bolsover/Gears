using System;
using System.ComponentModel;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>
    /// Editable view of an HTD timing belt pulley in the property grid: the belt series and number of grooves, the width and flanges,
    /// the axial position, and the diameters that follow from the series.
    /// </summary>
    public sealed class PulleyNodeProperties
    {
        private readonly HtdPulleyComponent _pulley;
        private readonly ShaftMount _mount;

        public PulleyNodeProperties(GearTrainPresenter presenter, HtdPulleyComponent pulley, ShaftMount mount, GearMeshRelation relation)
        {
            if (presenter == null) throw new ArgumentNullException(nameof(presenter));
            _pulley = pulley ?? throw new ArgumentNullException(nameof(pulley));
            _mount = mount ?? throw new ArgumentNullException(nameof(mount));
        }

        [Category("General")]
        [DisplayName("Id")]
        public string Id => _pulley.Id;

        [Category("General")]
        [DisplayName("Type")]
        public string Type => _pulley.TypeName;

        [Category("General")]
        [DisplayName("Name")]
        public string Name
        {
            get => _pulley.Name;
            set
            {
                if (!string.IsNullOrWhiteSpace(value)) _pulley.Name = value;
            }
        }

        [Category("General")]
        [DisplayName("Shaft")]
        public string ShaftId => _mount.Shaft?.Id;

        [Category("Teeth")]
        [DisplayName("HTD series")]
        [Description("The belt profile: Htd3M, Htd5M, Htd8M or Htd14M. The number is the belt pitch in millimetres.")]
        public HtdSeries Series
        {
            get => _pulley.Series;
            set => _pulley.Series = value;
        }

        [Category("Teeth")]
        [DisplayName("Grooves")]
        [Description("Number of grooves (teeth) of the pulley.")]
        public int Teeth
        {
            get => _pulley.Teeth;
            set => _pulley.Teeth = Math.Max(HtdPulleyComponent.AbsoluteMinimumTeeth, value);
        }

        [Category("Teeth")]
        [DisplayName("Below recommended minimum")]
        [Description("True when the pulley has fewer grooves than the belt makers recommend for the series.")]
        public bool BelowRecommendedMinimum => _pulley.IsBelowRecommendedTeeth;

        [Category("Dimensions")]
        [DisplayName("Width (mm)")]
        [Description("Width of the toothed part, along the axis. A belt of the same width or a little narrower fits.")]
        public double FaceWidth
        {
            get => _pulley.FaceWidth;
            set => _pulley.FaceWidth = value;
        }

        [Category("Dimensions")]
        [DisplayName("Flanges")]
        [Description("Adds a flange disc on each side of the toothed width.")]
        public bool HasFlanges
        {
            get => _pulley.HasFlanges;
            set => _pulley.HasFlanges = value;
        }

        [Category("Dimensions")]
        [DisplayName("Flange height (mm)")]
        [Description("How far a flange reaches beyond the outside diameter.")]
        public double FlangeHeight
        {
            get => _pulley.FlangeHeight;
            set => _pulley.FlangeHeight = value;
        }

        [Category("Dimensions")]
        [DisplayName("Flange thickness (mm)")]
        public double FlangeThickness
        {
            get => _pulley.FlangeThickness;
            set => _pulley.FlangeThickness = value;
        }

        [Category("Dimensions")]
        [DisplayName("Bore diameter (mm)")]
        [Description("Diameter of the bore through the pulley; zero for a solid pulley (the shaft of the editor passes through it).")]
        public double BoreDiameter
        {
            get => _pulley.BoreDiameter;
            set => _pulley.BoreDiameter = Math.Max(0.0, value);
        }

        [Category("Dimensions")]
        [DisplayName("Hub diameter (mm)")]
        [Description("Diameter of the hub on the +Z side of the pulley. The hub shows when it is larger than the bore and its length is positive.")]
        public double HubDiameter
        {
            get => _pulley.HubDiameter;
            set => _pulley.HubDiameter = Math.Max(0.0, value);
        }

        [Category("Dimensions")]
        [DisplayName("Hub length (mm)")]
        [Description("Length of the hub beyond the toothed width (or the flange); zero for no hub.")]
        public double HubLength
        {
            get => _pulley.HubLength;
            set => _pulley.HubLength = Math.Max(0.0, value);
        }

        [Category("Calculated")]
        [DisplayName("Belt pitch (mm)")]
        public double Pitch => _pulley.Profile.Pitch;

        [Category("Calculated")]
        [DisplayName("Pitch diameter (mm)")]
        [Description("Grooves x pitch / pi.")]
        public double PitchDiameter => _pulley.PitchDiameter;

        [Category("Calculated")]
        [DisplayName("Outside diameter (mm)")]
        [Description("Pitch diameter minus twice the pitch line differential.")]
        public double OutsideDiameter => _pulley.OutsideDiameter;

        [Category("Calculated")]
        [DisplayName("Root diameter (mm)")]
        public double RootDiameter => _pulley.RootDiameter;

        [Category("Calculated")]
        [DisplayName("Groove depth (mm)")]
        public double GrooveDepth => _pulley.Profile.ToothHeight;

        [Category("Calculated")]
        [DisplayName("Pitch line differential (mm)")]
        public double PitchLineDifferential => _pulley.Profile.PitchLineDifferential;

        [Category("Position")]
        [DisplayName("Axial offset (mm)")]
        [Description("Position of the pulley's mid-plane along its shaft.")]
        public double AxialOffset
        {
            get => _mount.AxialOffset;
            set => _mount.AxialOffset = value;
        }
    }
}
