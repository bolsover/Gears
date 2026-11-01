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

        [Category("Mesh")]
        [DisplayName("Centre distance (mm)")]
        [Description("Working distance between this gear's shaft and its driver's shaft. Entering a value fixes it; 0 goes back to the " +
                     "distance that follows from the profile shifts. A larger distance raises the working pressure angle; it adds backlash to an " +
                     "external pair and takes backlash away from an internal pair.")]
        public double CentreDistance
        {
            get => _relation?.CentreDistanceOverride ?? _relation?.Result?.CentreDistance ?? 0.0;
            set
            {
                if (_relation != null) _relation.CentreDistanceOverride = value > 0.0 ? value : (double?)null;
            }
        }

        [Category("Mesh")]
        [DisplayName("Fixed centre distance")]
        [Description("True while the centre distance is held at a chosen value; set to False to derive it from the profile shifts again.")]
        public bool CentreDistanceFixed
        {
            get => _relation?.CentreDistanceOverride.HasValue == true;
            set
            {
                if (_relation == null) return;
                if (value)
                {
                    if (!_relation.CentreDistanceOverride.HasValue && _relation.Result != null)
                    {
                        _relation.CentreDistanceOverride = _relation.Result.CentreDistance;
                    }
                }
                else
                {
                    _relation.CentreDistanceOverride = null;
                }
            }
        }

        [Category("Mesh")]
        [DisplayName("Standard centre distance (mm)")]
        [Description("Centre distance of the pair without profile shift.")]
        public double StandardCentreDistance => _relation?.Result?.PairData?.StandardCentreDistance ?? 0.0;

        [Category("Mesh")]
        [DisplayName("Working pressure angle (deg)")]
        public double WorkingPressureAngle => _relation?.Result?.PairData?.WorkingPressureAngleDeg ?? 0.0;

        [Category("Mesh")]
        [DisplayName("Working backlash (mm)")]
        [Description("Circular backlash on the working pitch circles; negative means the teeth jam.")]
        public double WorkingBacklash => _relation?.Result?.PairData?.WorkingCircularBacklash ?? 0.0;

        [Category("Mesh")]
        [DisplayName("Contact ratio")]
        public double ContactRatio => _relation?.Result?.PairData?.ContactRatio ?? 0.0;

        [Category("Mesh")]
        [DisplayName("Sum of profile shifts")]
        [Description("Profile shift of this gear plus that of its driver (for an internal pair: internal minus external).")]
        public double ProfileShiftSum
        {
            get
            {
                var pair = _relation?.Result?.PairData;
                return pair != null ? pair.SumProfileShift : 0.0;
            }
        }
    }

    /// <summary>Properties that every kind of gear shares in the property grid.</summary>
    public abstract class ComponentNodePropertiesBase
    {
        private readonly GearTrainPresenter _presenter;
        private readonly GearComponent _component;
        private readonly ShaftMount _mount;
        private readonly GearMeshRelation _relation;

        protected ComponentNodePropertiesBase(GearTrainPresenter presenter, GearComponent component, ShaftMount mount, GearMeshRelation relation)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _component = component ?? throw new ArgumentNullException(nameof(component));
            _mount = mount ?? throw new ArgumentNullException(nameof(mount));
            _relation = relation;
        }

        [Category("General")]
        [DisplayName("Id")]
        public string Id => _component.Id;

        [Category("General")]
        [DisplayName("Type")]
        public string Type => _component.TypeName;

        [Category("General")]
        [DisplayName("Name")]
        public string Name
        {
            get => _component.Name;
            set
            {
                if (!string.IsNullOrWhiteSpace(value)) _component.Name = value;
            }
        }

        [Category("General")]
        [DisplayName("Shaft")]
        public string ShaftId => _mount.Shaft?.Id;

        [Category("Teeth")]
        [DisplayName("Module (mm)")]
        [Description("Meshing gears share one module: changing it here changes every gear that meshes with this one.")]
        public double Module
        {
            get => _component.Module;
            set
            {
                _component.Module = value;
                _presenter.ApplyMeshCommon(_component);
            }
        }

        [Category("Teeth")]
        [DisplayName("Pressure angle (deg)")]
        [Description("Meshing gears share one pressure angle: changing it here changes every gear that meshes with this one.")]
        public double PressureAngle
        {
            get => _component.PressureAngleDeg;
            set
            {
                _component.PressureAngleDeg = value;
                _presenter.ApplyMeshCommon(_component);
            }
        }

        [Category("Dimensions")]
        [DisplayName("Face width (mm)")]
        public double FaceWidth
        {
            get => _component.FaceWidth;
            set => _component.FaceWidth = value;
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

    /// <summary>Editable view of a bevel gear in the property grid.</summary>
    public sealed class BevelNodeProperties : ComponentNodePropertiesBase
    {
        private readonly BevelGearComponent _gear;

        public BevelNodeProperties(GearTrainPresenter presenter, BevelGearComponent gear, ShaftMount mount, GearMeshRelation relation)
            : base(presenter, gear, mount, relation)
        {
            _gear = gear;
        }

        [Category("Teeth")]
        [DisplayName("Teeth")]
        public int Teeth
        {
            get => _gear.Teeth;
            set => _gear.Teeth = Math.Max(1, value);
        }

        [Category("Teeth")]
        [DisplayName("Backlash (mm)")]
        public double Backlash
        {
            get => _gear.Backlash;
            set => _gear.Backlash = value;
        }

        [Category("Cone")]
        [DisplayName("Shaft angle (deg)")]
        [Description("Angle between the two shafts of the pair; set it on the driving gear.")]
        public double ShaftAngle
        {
            get => _gear.ShaftAngleDeg;
            set => _gear.ShaftAngleDeg = value;
        }

        [Category("Cone")]
        [DisplayName("Pitch cone angle (deg)")]
        public double PitchConeAngle => _gear.PitchConeAngleDeg;

        [Category("Cone")]
        [DisplayName("Cone distance (mm)")]
        public double ConeDistance => _gear.ConeDistance;

        [Category("Cone")]
        [DisplayName("Virtual teeth (Tredgold)")]
        public int VirtualTeeth => _gear.VirtualTeeth;
    }

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
