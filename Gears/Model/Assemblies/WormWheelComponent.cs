using System;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// A worm wheel: a helical involute gear whose helix angle equals the lead angle of the worm it meshes with
    /// (a non-throated wheel; the throat of an enveloping wheel is not modelled).
    /// <para>
    /// <see cref="GearComponent.Module"/> is the axial module of the worm (the transverse module of the wheel); the wheel
    /// teeth use the normal module <c>m * cos(gamma)</c>. The mesh solver sets <see cref="LeadAngleDeg"/> and
    /// <see cref="HelicalDirection"/> from the worm.
    /// </para>
    /// </summary>
    public class WormWheelComponent : GearComponent
    {
        private string _helicalDirection = "R";

        public WormWheelComponent(string id) : base(id)
        {
            FaceWidth = 12.0;
        }

        public int Teeth { get; set; } = 30;

        /// <summary>Lead angle of the mating worm in degrees; the helix angle of the wheel teeth.</summary>
        public double LeadAngleDeg { get; set; }

        /// <summary>"R" (right) or "L" (left); same hand as the worm.</summary>
        public string HelicalDirection
        {
            get => _helicalDirection;
            set => _helicalDirection = value?.StartsWith("L", StringComparison.OrdinalIgnoreCase) == true ? "L" : "R";
        }

        public double Backlash { get; set; }

        public override int ToothCount => Teeth;

        /// <summary>Pitch diameter: axial module times teeth.</summary>
        public override double PitchDiameter => Module * Teeth;

        public override string TypeName => "Worm wheel";

        public void Validate()
        {
            if (Teeth < 10) throw new InvalidOperationException($"Worm wheel '{Id}': the number of teeth must be at least 10.");
            if (Module <= 0) throw new InvalidOperationException($"Worm wheel '{Id}': the module must be positive.");
            if (FaceWidth <= 0) throw new InvalidOperationException($"Worm wheel '{Id}': the face width must be positive.");
            if (PressureAngleDeg <= 0 || PressureAngleDeg >= 45)
            {
                throw new InvalidOperationException($"Worm wheel '{Id}': the pressure angle must be between 0 and 45 degrees.");
            }

            if (LeadAngleDeg < 0 || LeadAngleDeg >= 45)
            {
                throw new InvalidOperationException($"Worm wheel '{Id}': the lead angle must be between 0 and 45 degrees.");
            }
        }

        public override GearGeometry BuildGeometry()
        {
            Validate();

            var normalModule = Module * Math.Cos(LeadAngleDeg * Math.PI / 180.0);
            var gear = new InvoluteGearComponent(Id)
            {
                Teeth = Teeth,
                Module = normalModule,
                PressureAngleDeg = PressureAngleDeg,
                FaceWidth = FaceWidth,
                HelixAngleDeg = LeadAngleDeg,
                HelicalDirection = HelicalDirection,
                Backlash = Backlash,
                Clearance = 0.25 * normalModule
            };

            return gear.BuildGeometry();
        }
    }
}
