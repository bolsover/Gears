using System;
using System.ComponentModel;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>
    /// Editable view of a gear in the property grid: its tooth parameters, its axial position on the shaft and,
    /// for a driven gear, the direction of the line of centres from its driver.
    /// </summary>
    public sealed class GearNodeProperties
    {
        private readonly GearTrainPresenter _presenter;
        private readonly InvoluteGearComponent _gear;
        private readonly ShaftMount _mount;
        private readonly GearMeshRelation _relation;

        public GearNodeProperties(GearTrainPresenter presenter, InvoluteGearComponent gear, ShaftMount mount, GearMeshRelation relation)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _gear = gear ?? throw new ArgumentNullException(nameof(gear));
            _mount = mount ?? throw new ArgumentNullException(nameof(mount));
            _relation = relation;
        }

        [Category("General")]
        [DisplayName("Id")]
        public string Id => _gear.Id;

        [Category("General")]
        [DisplayName("Name")]
        public string Name
        {
            get => _gear.Name;
            set
            {
                if (!string.IsNullOrWhiteSpace(value)) _gear.Name = value;
            }
        }

        [Category("General")]
        [DisplayName("Shaft")]
        public string ShaftId => _mount.Shaft?.Id;

        [Category("Teeth")]
        [DisplayName("Teeth")]
        public int Teeth
        {
            get => _gear.Teeth;
            set => _gear.Teeth = Math.Max(1, value);
        }

        [Category("Teeth")]
        [DisplayName("Module (mm)")]
        [Description("Meshing gears share one module: changing it here changes every gear that meshes with this one.")]
        public double Module
        {
            get => _gear.Module;
            set
            {
                _gear.Module = value;
                _presenter.ApplyMeshCommon(_gear);
            }
        }

        [Category("Teeth")]
        [DisplayName("Pressure angle (deg)")]
        [Description("Meshing gears share one pressure angle: changing it here changes every gear that meshes with this one.")]
        public double PressureAngle
        {
            get => _gear.PressureAngleDeg;
            set
            {
                _gear.PressureAngleDeg = value;
                _presenter.ApplyMeshCommon(_gear);
            }
        }

        [Category("Teeth")]
        [DisplayName("Helix angle (deg)")]
        public double HelixAngle
        {
            get => _gear.HelixAngleDeg;
            set => _gear.HelixAngleDeg = value;
        }

        [Category("Teeth")]
        [DisplayName("Helix direction (R/L)")]
        public string HelicalDirection
        {
            get => _gear.HelicalDirection;
            set => _gear.HelicalDirection = value;
        }

        [Category("Teeth")]
        [DisplayName("Internal gear")]
        public bool IsInternal
        {
            get => _gear.IsInternal;
            set => _gear.IsInternal = value;
        }

        [Category("Teeth")]
        [DisplayName("Rim thickness (mm)")]
        public double RimThickness
        {
            get => _gear.RimThickness;
            set => _gear.RimThickness = value;
        }

        [Category("Teeth")]
        [DisplayName("Profile shift")]
        public double ProfileShift
        {
            get => _gear.ProfileShift;
            set => _gear.ProfileShift = value;
        }

        [Category("Teeth")]
        [DisplayName("Clearance")]
        public double Clearance
        {
            get => _gear.Clearance;
            set => _gear.Clearance = value;
        }

        [Category("Teeth")]
        [DisplayName("Backlash (mm)")]
        public double Backlash
        {
            get => _gear.Backlash;
            set => _gear.Backlash = value;
        }

        [Category("Dimensions")]
        [DisplayName("Face width (mm)")]
        public double FaceWidth
        {
            get => _gear.FaceWidth;
            set => _gear.FaceWidth = value;
        }

        [Category("Position")]
        [DisplayName("Axial offset (mm)")]
        [Description("Position of the gear's mid-plane along its shaft. Gears on one shaft must not overlap.")]
        public double AxialOffset
        {
            get => _mount.AxialOffset;
            set => _mount.AxialOffset = value;
        }

        [Category("Position")]
        [DisplayName("Centre line angle (deg)")]
        [Description("Direction of this gear's shaft from its driver's shaft. Only used for driven gears.")]
        public double CentreLineAngle
        {
            get => _relation?.CentreLineAngleDeg ?? 0.0;
            set
            {
                if (_relation != null) _relation.CentreLineAngleDeg = value;
            }
        }

        [Category("Position")]
        [DisplayName("Driven by")]
        public string DrivenBy => _relation?.Driver.Name;
    }

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
