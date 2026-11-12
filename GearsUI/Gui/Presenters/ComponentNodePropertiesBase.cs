using System;
using System.ComponentModel;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
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
}
