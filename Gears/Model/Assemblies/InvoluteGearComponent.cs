using System;
using Bolsover.Gears.Calculator;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// An involute spur or helical gear (external or internal), generated with the existing
    /// <see cref="AlibreGearGenerator"/> and <see cref="GearMesh3DBuilder"/>.
    /// A helix angle of zero gives a spur gear.
    /// </summary>
    public class InvoluteGearComponent : GearComponent
    {
        private string _helicalDirection = "R";

        public InvoluteGearComponent(string id) : base(id)
        {
        }

        public int Teeth { get; set; } = 20;

        /// <summary>Helix angle in degrees; zero for a spur gear.</summary>
        public double HelixAngleDeg { get; set; }

        /// <summary>"R" (right) or "L" (left). Any value starting with L/l is treated as left.</summary>
        public string HelicalDirection
        {
            get => _helicalDirection;
            set => _helicalDirection = value?.StartsWith("L", StringComparison.OrdinalIgnoreCase) == true ? "L" : "R";
        }

        public double Clearance { get; set; } = 0.5;
        public double Backlash { get; set; }
        public double ProfileShift { get; set; }
        public bool IsInternal { get; set; }

        /// <summary>Rim thickness (mm), used for internal gears only.</summary>
        public double RimThickness { get; set; } = 10.0;

        public bool OptimalShift { get; set; }

        /// <summary>True to build all teeth; false to build a single tooth.</summary>
        public bool ShowFullGear { get; set; } = true;

        public bool IsHelical => HelixAngleDeg > 1e-6;

        public override string TypeName => IsInternal ? "Internal gear" : (IsHelical ? "Helical gear" : "Spur gear");

        /// <summary>An internal gear has an open bore, so a shaft cylinder is not drawn through it.</summary>
        public override bool NeedsShaftMesh => !IsInternal;

        public override int ToothCount => Teeth;

        /// <summary>Reference (pitch) diameter: module times teeth, divided by the cosine of the helix angle.</summary>
        public override double PitchDiameter => Module * Teeth / Math.Cos(HelixAngleDeg * Math.PI / 180.0);

        /// <summary>Throws <see cref="InvalidOperationException"/> when the parameters cannot describe a gear.</summary>
        public void Validate()
        {
            if (Teeth < 1) throw new InvalidOperationException($"Gear '{Id}': the number of teeth must be at least 1.");
            if (Module <= 0) throw new InvalidOperationException($"Gear '{Id}': the module must be positive.");
            if (FaceWidth <= 0) throw new InvalidOperationException($"Gear '{Id}': the face width must be positive.");
            if (PressureAngleDeg <= 0 || PressureAngleDeg >= 90)
            {
                throw new InvalidOperationException($"Gear '{Id}': the pressure angle must be between 0 and 90 degrees.");
            }
        }

        public override GearGeometry BuildGeometry()
        {
            Validate();

            var profile = IsInternal
                ? AlibreGearGenerator.GenerateInternalToothProfile(Teeth, Module, PressureAngleDeg, RimThickness, ProfileShift, OptimalShift, null, Clearance, Backlash, HelixAngleDeg)
                : AlibreGearGenerator.GenerateExternalToothProfile(Teeth, Module, PressureAngleDeg, ProfileShift, OptimalShift, null, Clearance, Backlash, HelixAngleDeg);

            var tooth = AlibreGearGenerator.BuildToothOutline(profile, IsInternal);
            var polylines = ShowFullGear
                ? AlibreGearGenerator.BuildFullGear(Teeth, tooth)
                : tooth;

            var mesh = GearMesh3DBuilder.BuildGearMesh(profile, Teeth, FaceWidth, IsInternal, ShowFullGear, RimThickness, HelixAngleDeg, HelicalDirection);

            return new GearGeometry(profile, polylines, mesh);
        }
    }
}
