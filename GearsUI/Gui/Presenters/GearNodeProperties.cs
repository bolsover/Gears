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
}
