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
        [DisplayName("Herringbone")]
        [Description("A helical gear mirrored about its middle plane: each half of the face width has the helix angle, with opposite hands. Needs a helix angle above zero.")]
        public bool IsHerringbone
        {
            get => _gear.IsHerringbone;
            set => _gear.IsHerringbone = value;
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
        [DisplayName("Shaft angle (deg)")]
        [Description("Angle between this gear's shaft and its driver's shaft. Only a crossed helical mesh uses it, and it must match the helix angles " +
                     "and hands of the two gears (see the expected shaft angle); 0 for any other mesh.")]
        public double ShaftAngle
        {
            get => _relation?.ShaftAngleDeg ?? 0.0;
            set
            {
                if (_relation != null && _relation.MeshType == MeshType.CrossedHelical) _relation.ShaftAngleDeg = value;
            }
        }

        [Category("Mesh")]
        [DisplayName("Expected shaft angle (deg)")]
        [Description("The shaft angle that the helix angles and hands of this gear and its driver need on a crossed helical mesh: the sum of the " +
                     "helix angles for the same hand, their difference for opposite hands. 0 when the mesh is not a crossed helical mesh.")]
        public double ExpectedShaftAngle
        {
            get
            {
                if (_relation == null || _relation.MeshType != MeshType.CrossedHelical) return 0.0;
                var driver = _relation.Driver as InvoluteGearComponent;
                if (driver == null) return 0.0;
                return Bolsover.Gears.Calculator.HelicalPairGeometry.ExpectedShaftAngleDeg(
                    driver.HelixAngleDeg, driver.HelicalDirection, _gear.HelixAngleDeg, _gear.HelicalDirection);
            }
        }

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

    /// <summary>
    /// Editable view of a timing belt in the property grid: its name, the centre distance and direction of the line of centres (which place
    /// the driven pulley's shaft), and the belt data that follows from them.
    /// </summary>
    public sealed class BeltNodeProperties
    {
        private readonly TimingBelt _belt;

        public BeltNodeProperties(TimingBelt belt)
        {
            _belt = belt ?? throw new ArgumentNullException(nameof(belt));
        }

        [Category("General")]
        [DisplayName("Id")]
        public string Id => _belt.Id;

        [Category("General")]
        [DisplayName("Name")]
        public string Name
        {
            get => _belt.Name;
            set
            {
                if (!string.IsNullOrWhiteSpace(value)) _belt.Name = value;
            }
        }

        [Category("General")]
        [DisplayName("Belt series")]
        public string Series => "HTD " + _belt.Profile.Name;

        [Category("General")]
        [DisplayName("Driver pulley")]
        public string Driver => _belt.Driver.Name;

        [Category("General")]
        [DisplayName("Driven pulley")]
        public string Driven => _belt.Driven.Name;

        [Category("Position")]
        [DisplayName("Centre distance (mm)")]
        [Description("Distance between the two shaft axes. It must be larger than the minimum so that the pulleys do not touch.")]
        public double CentreDistance
        {
            get => _belt.CentreDistance;
            set => _belt.CentreDistance = value;
        }

        [Category("Position")]
        [DisplayName("Line of centres angle (deg)")]
        [Description("Direction of the driven pulley's shaft seen from the driver's shaft, measured from the driver shaft's X axis.")]
        public double CentreLineAngleDeg
        {
            get => _belt.CentreLineAngleDeg;
            set => _belt.CentreLineAngleDeg = value;
        }

        [Category("Calculated")]
        [DisplayName("Minimum centre distance (mm)")]
        public double MinimumCentreDistance => _belt.MinimumCentreDistance;

        [Category("Calculated")]
        [DisplayName("Belt teeth")]
        [Description("Pitch path length divided by the pitch, rounded to a whole tooth.")]
        public int BeltTeeth => _belt.BeltTeeth;

        [Category("Calculated")]
        [DisplayName("Pitch length (mm)")]
        [Description("Belt teeth times the pitch: the length of the belt as it is made.")]
        public double PitchLength => _belt.PitchLength;

        [Category("Calculated")]
        [DisplayName("Pitch path at this distance (mm)")]
        public double PitchPathLength => _belt.PitchPathLength;

        [Category("Calculated")]
        [DisplayName("Width (mm)")]
        public double Width => _belt.Width;

        [Category("Calculated")]
        [DisplayName("Speed ratio (driver : driven)")]
        public double Ratio => _belt.GearRatio;

        [Category("Calculated")]
        [DisplayName("Wrap on small pulley (deg)")]
        public double Wrap => _belt.SmallPulleyWrapDegrees;
    }

    /// <summary>Editable view of a bevel gear in the property grid.</summary>
    public class BevelNodeProperties : ComponentNodePropertiesBase
    {
        private readonly BevelGearComponent _gear;
        private readonly GearTrainPresenter _owner;

        public BevelNodeProperties(GearTrainPresenter presenter, BevelGearComponent gear, ShaftMount mount, GearMeshRelation relation)
            : base(presenter, gear, mount, relation)
        {
            _gear = gear;
            _owner = presenter;
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

        [Category("Teeth")]
        [DisplayName("Bevel type")]
        [Description("Standard: equal addendum m and clearance 0.25 m. Gleason: whole depth 2.188 m + 0.051 mm, with a long pinion addendum and a short gear addendum. Set it on the driving gear; the meshed gear follows.")]
        public BevelGearType BevelType
        {
            get => _gear.BevelType;
            set => _gear.BevelType = value;
        }

        [Category("Teeth")]
        [DisplayName("Mating teeth")]
        [Description("Tooth count of the mating bevel gear, which sets the pitch cone angle. For a meshed gear this changes the teeth of the gear it meshes with.")]
        public int MateTeeth
        {
            get => _gear.MateTeeth > 0 ? _gear.MateTeeth : _gear.Teeth;
            set
            {
                var teeth = Math.Max(1, value);
                _gear.MateTeeth = teeth;
                foreach (var relation in _owner.Assembly.Relations)
                {
                    if (relation.MeshType != MeshType.Bevel) continue;
                    var partner = ReferenceEquals(relation.Driver, _gear) ? relation.Driven : ReferenceEquals(relation.Driven, _gear) ? relation.Driver : null;
                    if (partner is BevelGearComponent mate) mate.Teeth = teeth;
                }
            }
        }

        [Category("Teeth")]
        [DisplayName("Rack tip radius (x module)")]
        [Description("Corner radius of the generating rack tip as a multiple of the module. 0 gives a sharp corner and a trochoidal root fillet; a larger value gives a larger root fillet. Must be below 1.")]
        public double RackTipRadiusFactor
        {
            get => _gear.RackTipRadiusFactor;
            set => _gear.RackTipRadiusFactor = Math.Max(0.0, value);
        }

        [Category("Tooth depth")]
        [DisplayName("Addendum (mm)")]
        public double Addendum => _gear.Addendum;

        [Category("Tooth depth")]
        [DisplayName("Dedendum (mm)")]
        public double Dedendum => _gear.Dedendum;

        [Category("Tooth depth")]
        [DisplayName("Whole depth (mm)")]
        public double WholeDepth => _gear.WholeDepth;

        [Category("Tooth depth")]
        [DisplayName("Clearance (mm)")]
        public double Clearance => _gear.Clearance;

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

        [Category("Cone")]
        [DisplayName("Dedendum angle (deg)")]
        public double DedendumAngle => _gear.DedendumAngleDeg;

        [Category("Cone")]
        [DisplayName("Addendum angle (deg)")]
        public double AddendumAngle => _gear.AddendumAngleDeg;

        [Category("Cone")]
        [DisplayName("Tip cone angle (deg)")]
        public double TipConeAngle => _gear.TipConeAngleDeg;

        [Category("Cone")]
        [DisplayName("Root cone angle (deg)")]
        public double RootConeAngle => _gear.RootConeAngleDeg;

        [Category("Cone")]
        [DisplayName("Back cone angle (deg)")]
        [Description("Half angle of the back cone, 90 degrees less the pitch cone angle. The back face of the teeth lies on this cone.")]
        public double BackConeAngle => _gear.BackConeAngleDeg;

        [Category("Cone")]
        [DisplayName("Back cone distance (mm)")]
        public double BackConeDistance => _gear.BackConeDistance;

        [Category("Hub and bore")]
        [DisplayName("Bore diameter (mm, 0 = shaft)")]
        [Description("0 uses the diameter of the shaft that carries the gear. The bore is limited to 0.6 times the root diameter.")]
        public double BoreDiameter
        {
            get => _gear.BoreDiameter;
            set => _gear.BoreDiameter = Math.Max(0.0, value);
        }

        [Category("Hub and bore")]
        [DisplayName("Hub")]
        [Description("Draws a hub behind the back cone.")]
        public bool HasHub
        {
            get => _gear.HasHub;
            set => _gear.HasHub = value;
        }

        [Category("Hub and bore")]
        [DisplayName("Hub diameter (mm, 0 = automatic)")]
        [Description("0 gives 1.8 times the bore. It must be larger than the bore and not more than 0.85 times the root diameter.")]
        public double HubDiameter
        {
            get => _gear.HubDiameter;
            set => _gear.HubDiameter = Math.Max(0.0, value);
        }

        [Category("Hub and bore")]
        [DisplayName("Hub length (mm, 0 = automatic)")]
        [Description("Projection of the hub behind the back cone, along the axis. 0 gives 0.75 times the bore, but at least half the face width.")]
        public double HubLength
        {
            get => _gear.HubLength;
            set => _gear.HubLength = Math.Max(0.0, value);
        }

        [Category("Hub and bore")]
        [DisplayName("Bore used (mm)")]
        public double EffectiveBoreDiameter => _gear.EffectiveBoreDiameter;

        [Category("Hub and bore")]
        [DisplayName("Hub diameter used (mm)")]
        public double EffectiveHubDiameter => _gear.EffectiveHubDiameter;

        [Category("Hub and bore")]
        [DisplayName("Hub length used (mm)")]
        public double EffectiveHubLength => _gear.EffectiveHubLength;

        [Category("Hub and bore")]
        [DisplayName("Overall length along axis (mm)")]
        [Description("From the back of the hub to the front of the teeth.")]
        public double OverallLength => _gear.OverallAxialLength;

        [Category("Cone")]
        [DisplayName("Tip diameter (mm)")]
        public double TipDiameter => _gear.TipDiameter;

        [Category("Cone")]
        [DisplayName("Pitch apex to crown (mm)")]
        public double PitchApexToCrown => _gear.PitchApexToCrown;

        [Category("Cone")]
        [DisplayName("Recommended max face width (mm)")]
        [Description("One third of the cone distance, but not more than ten modules.")]
        public double RecommendedMaxFaceWidth => _gear.RecommendedMaxFaceWidth;
    }

    /// <summary>
    /// Editable view of a spiral or zerol bevel gear in the property grid: the bevel gear properties and the tooth trace (spiral angle, hand,
    /// cutter radius). The values are set on both gears of a pair, and the mate gets the opposite hand.
    /// </summary>
    public class SpiralBevelNodeProperties : BevelNodeProperties
    {
        private readonly SpiralBevelGearComponent _spiral;
        private readonly GearTrainPresenter _presenter;

        public SpiralBevelNodeProperties(GearTrainPresenter presenter, SpiralBevelGearComponent gear, ShaftMount mount, GearMeshRelation relation)
            : base(presenter, gear, mount, relation)
        {
            _spiral = gear;
            _presenter = presenter;
        }

        [Category("Spiral teeth")]
        [DisplayName("Mean spiral angle (deg)")]
        [Description("Spiral angle of the tooth line at the middle of the face, from 0 to 60 degrees (typically 35). It is zero for a zerol gear. For a hypoid pair it is the angle of the pinion. Both gears of a pair have the same angle.")]
        public double MeanSpiralAngle
        {
            get => _spiral.MeanSpiralAngleDeg;
            set => _presenter.SetSpiralParameters(_spiral, value, null);
        }

        [Category("Spiral teeth")]
        [DisplayName("Hand")]
        [Description("Right: the tooth line advances counter-clockwise from the heel to the toe, looking along the axis towards the apex. The mate gets the opposite hand. The hands of a hypoid pair follow from the side of the offset.")]
        public SpiralHand Hand
        {
            get => _spiral.Hand;
            set => _presenter.SetSpiralHand(_spiral, value);
        }

        [Category("Spiral teeth")]
        [DisplayName("Cutter radius (mm, 0 = automatic)")]
        [Description("Radius of the face-mill cutter that cuts the tooth line as a circular arc. 0 uses the mean cone distance. It must be large enough to reach the whole face.")]
        public double CutterRadius
        {
            get => _spiral.CutterRadius;
            set => _presenter.SetSpiralParameters(_spiral, null, value);
        }

        [Category("Spiral teeth")]
        [DisplayName("Spiral angle at mid face (deg)")]
        [Description("The spiral angle of this gear at the middle of the face; it differs from the mean spiral angle for the gear of a hypoid pair.")]
        public double SpiralAngleAtMidFace => _spiral.EffectiveSpiralAngleDeg;

        [Category("Spiral teeth")]
        [DisplayName("Transverse pressure angle (deg)")]
        [Description("tan(transverse) = tan(normal pressure angle) / cos(spiral angle): the angle of the tooth profile in the plane perpendicular to the axis.")]
        public double TransversePressureAngle => _spiral.TransversePressureAngleDeg;
    }

    /// <summary>
    /// Editable view of a hypoid gear in the property grid: the spiral properties, the offset of the shafts and the values of the pair at the
    /// mean point.
    /// </summary>
    public sealed class HypoidNodeProperties : SpiralBevelNodeProperties
    {
        private readonly HypoidGearComponent _hypoid;
        private readonly GearTrainPresenter _presenter;

        public HypoidNodeProperties(GearTrainPresenter presenter, HypoidGearComponent gear, ShaftMount mount, GearMeshRelation relation)
            : base(presenter, gear, mount, relation)
        {
            _hypoid = gear;
            _presenter = presenter;
        }

        [Category("Hypoid")]
        [DisplayName("Offset (mm)")]
        [Description("Offset of the pinion shaft from the gear shaft along their common perpendicular; it must not be zero and must be smaller than the mean pitch radius of the gear. The sign gives the side: for a positive offset the gear is right hand and the pinion left hand. Changing it sets the hands of both gears.")]
        public double Offset
        {
            get => _hypoid.OffsetMm;
            set => _presenter.SetHypoidOffset(_hypoid, value);
        }

        [Category("Hypoid")]
        [DisplayName("Pinion")]
        [Description("True for the small gear of the pair.")]
        public bool IsPinion => _hypoid.IsPinion;

        [Category("Hypoid")]
        [DisplayName("Offset angle (deg)")]
        public double OffsetAngle => _hypoid.Pair?.OffsetAngleDeg ?? 0.0;

        [Category("Hypoid")]
        [DisplayName("Pinion pitch radius at mean point (mm)")]
        [Description("Larger than the tooth count suggests: r1 / r2 = z1 cos(beta2) / (z2 cos(beta1)).")]
        public double PinionPitchRadius => _hypoid.Pair?.PinionPitchRadius ?? 0.0;

        [Category("Hypoid")]
        [DisplayName("Gear pitch radius at mean point (mm)")]
        public double GearPitchRadius => _hypoid.Pair?.GearPitchRadius ?? 0.0;

        [Category("Hypoid")]
        [DisplayName("Pinion spiral angle (deg)")]
        public double PinionSpiralAngle => _hypoid.Pair?.PinionSpiralAngleDeg ?? 0.0;

        [Category("Hypoid")]
        [DisplayName("Gear spiral angle (deg)")]
        public double GearSpiralAngle => _hypoid.Pair?.GearSpiralAngleDeg ?? 0.0;

        [Category("Hypoid")]
        [DisplayName("Pinion pitch cone angle (deg)")]
        public double PinionConeAngle => _hypoid.Pair?.PinionConeAngleDeg ?? 0.0;

        [Category("Hypoid")]
        [DisplayName("Gear pitch cone angle (deg)")]
        public double GearConeAngle => _hypoid.Pair?.GearConeAngleDeg ?? 0.0;

        [Category("Hypoid")]
        [DisplayName("Mean cone distance (mm)")]
        [Description("Distance from the apex of this gear to the middle of the face. The apexes of the two gears do not coincide.")]
        public double MeanConeDistance => _hypoid.MeanConeDistance;
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
