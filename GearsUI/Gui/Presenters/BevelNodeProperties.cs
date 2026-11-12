using System;
using System.ComponentModel;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
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
}
