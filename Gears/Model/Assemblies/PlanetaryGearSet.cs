using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Bolsover.Gears.Calculator.Assemblies;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>The three rotating members of a simple planetary stage.</summary>
    public enum PlanetaryMember
    {
        Sun,
        Ring,
        Carrier
    }

    /// <summary>Speeds of a planetary stage (rpm; positive is anticlockwise seen from +Z).</summary>
    public sealed class PlanetarySpeeds
    {
        public PlanetarySpeeds(
            PlanetaryMember fixedMember,
            PlanetaryMember inputMember,
            double sunRpm,
            double ringRpm,
            double carrierRpm,
            double planetRpm,
            double planetRelativeRpm,
            PlanetaryMember outputMember,
            double? overallRatio)
        {
            FixedMember = fixedMember;
            InputMember = inputMember;
            SunRpm = sunRpm;
            RingRpm = ringRpm;
            CarrierRpm = carrierRpm;
            PlanetRpm = planetRpm;
            PlanetRelativeRpm = planetRelativeRpm;
            OutputMember = outputMember;
            OverallRatio = overallRatio;
        }

        public PlanetaryMember FixedMember { get; }
        public PlanetaryMember InputMember { get; }
        public double SunRpm { get; }
        public double RingRpm { get; }
        public double CarrierRpm { get; }

        /// <summary>Speed of each planet about its own axis, seen from the ground.</summary>
        public double PlanetRpm { get; }

        /// <summary>Speed of each planet about its own axis relative to the carrier.</summary>
        public double PlanetRelativeRpm { get; }

        public PlanetaryMember OutputMember { get; }

        /// <summary>Input speed divided by output speed (signed); null when the output is the fixed member.</summary>
        public double? OverallRatio { get; }

        public string ToText()
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine($"Planetary stage: {FixedMember} fixed, {InputMember} is the input, {OutputMember} is the output");
            sb.AppendLine($"  Sun:     {SunRpm.ToString("0.0000000", ci)} rpm");
            sb.AppendLine($"  Ring:    {RingRpm.ToString("0.0000000", ci)} rpm");
            sb.AppendLine($"  Carrier: {CarrierRpm.ToString("0.0000000", ci)} rpm");
            sb.AppendLine($"  Planets: {PlanetRpm.ToString("0.0000000", ci)} rpm about their own axes ({PlanetRelativeRpm.ToString("0.0000000", ci)} rpm relative to the carrier)");
            sb.AppendLine(OverallRatio.HasValue
                ? $"  Overall ratio (input / output): {OverallRatio.Value.ToString("0.0000000", ci)}"
                : "  Overall ratio: not available (the output member is fixed)");
            return sb.ToString();
        }
    }

    /// <summary>
    /// A simple planetary stage: a sun gear, N equally spaced planets, an internal ring gear and a carrier.
    /// <para>
    /// All gears share the module and pressure angle, so the ring has <c>Zr = Zs + 2 * Zp</c> teeth. The planets can only
    /// be spaced equally when <c>(Zs + Zr) / N</c> is a whole number (the assembly condition), which
    /// <see cref="Validate"/> checks. The kinematics follow the Willis equation
    /// <c>Zs * (ws - wc) + Zr * (wr - wc) = 0</c>: any one member can be held fixed.
    /// </para>
    /// <para>
    /// <see cref="BuildAssembly"/> returns the posed stage as an ordinary <see cref="GearAssembly"/> (without relations),
    /// so the existing scene builder, viewport, export and serialisation work on it. The tooth positions come from
    /// <see cref="ParallelAxisMeshSolver"/>, the same solver as for any parallel gear pair.
    /// </para>
    /// </summary>
    public sealed class PlanetaryGearSet
    {
        private const double PlateThickness = 4.0;
        private const double PlateGap = 2.0;

        public PlanetaryGearSet(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A planetary gear set requires a non-empty identifier.", nameof(id));
            }

            Id = id;
        }

        public string Id { get; }

        public int SunTeeth { get; set; } = 24;
        public int PlanetTeeth { get; set; } = 18;

        /// <summary>Number of planets.</summary>
        public int PlanetCount { get; set; } = 3;

        /// <summary>Teeth of the internal ring gear: <c>Zs + 2 * Zp</c>.</summary>
        public int RingTeeth => SunTeeth + 2 * PlanetTeeth;

        public double Module { get; set; } = 2.0;
        public double PressureAngleDeg { get; set; } = 20.0;
        public double FaceWidth { get; set; } = 15.0;
        public double Backlash { get; set; }

        /// <summary>Rim thickness of the ring gear (mm).</summary>
        public double RingRimThickness { get; set; } = 10.0;

        /// <summary>The member that is held fixed (default the ring).</summary>
        public PlanetaryMember Fixed { get; set; } = PlanetaryMember.Ring;

        /// <summary>Rotation of the sun gear (radians). Use <see cref="SolveAngles"/> to set it with the carrier angle.</summary>
        public double SunAngleRad { get; set; }

        /// <summary>Rotation of the carrier (radians).</summary>
        public double CarrierAngleRad { get; set; }

        /// <summary>Rotation of the ring gear; computed by <see cref="BuildAssembly"/> from the tooth mesh.</summary>
        public double RingAngleRad { get; private set; }

        /// <summary>Distance from the sun axis to the planet axes (mm).</summary>
        public double CentreDistance => GearPrecision.Round(Module * (SunTeeth + PlanetTeeth) / 2.0);

        /// <summary>The components of the last assembly built by <see cref="BuildAssembly"/>; null before that.</summary>
        public InvoluteGearComponent Sun { get; private set; }

        public InvoluteGearComponent Ring { get; private set; }
        public IReadOnlyList<InvoluteGearComponent> Planets { get; private set; } = new List<InvoluteGearComponent>();

        /// <summary>Throws when the stage cannot be assembled.</summary>
        public void Validate()
        {
            if (SunTeeth < 6) throw new InvalidOperationException($"Planetary set '{Id}': the sun needs at least 6 teeth.");
            if (PlanetTeeth < 6) throw new InvalidOperationException($"Planetary set '{Id}': the planets need at least 6 teeth.");
            if (PlanetCount < 1) throw new InvalidOperationException($"Planetary set '{Id}': at least one planet is required.");
            if (Module <= 0) throw new InvalidOperationException($"Planetary set '{Id}': the module must be positive.");
            if (FaceWidth <= 0) throw new InvalidOperationException($"Planetary set '{Id}': the face width must be positive.");
            if (PressureAngleDeg <= 0 || PressureAngleDeg >= 90)
            {
                throw new InvalidOperationException($"Planetary set '{Id}': the pressure angle must be between 0 and 90 degrees.");
            }

            if ((SunTeeth + RingTeeth) % PlanetCount != 0)
            {
                throw new InvalidOperationException(
                    $"Planetary set '{Id}': {PlanetCount} planets cannot be spaced equally. (Zs + Zr) / N = ({SunTeeth} + {RingTeeth}) / {PlanetCount} " +
                    "must be a whole number.");
            }

            if (PlanetCount > 1)
            {
                var spacing = 2.0 * CentreDistance * Math.Sin(Math.PI / PlanetCount);
                var tipDiameter = Module * (PlanetTeeth + 2);
                if (spacing <= tipDiameter)
                {
                    throw new InvalidOperationException(
                        $"Planetary set '{Id}': neighbouring planets would collide (centre spacing {GearPrecision.Round(spacing)} mm, tip diameter {GearPrecision.Round(tipDiameter)} mm).");
                }
            }
        }

        private double[] Coefficients()
        {
            // Willis: Zs * ws + Zr * wr - (Zs + Zr) * wc = 0.
            return new double[] { SunTeeth, RingTeeth, -(SunTeeth + RingTeeth) };
        }

        private double[] SolveLinear(PlanetaryMember input, double inputValue)
        {
            if (input == Fixed)
            {
                throw new ArgumentException("The input member cannot be the fixed member.", nameof(input));
            }

            var k = Coefficients();
            var values = new double[3];
            values[(int)input] = inputValue;
            values[(int)Fixed] = 0.0;

            var third = 3 - (int)input - (int)Fixed;
            values[third] = -(k[(int)input] * inputValue) / k[third];
            return values;
        }

        /// <summary>
        /// Sets <see cref="SunAngleRad"/> and <see cref="CarrierAngleRad"/> from the angle of the input member so that the
        /// fixed member stays where it is. The ring angle follows from the tooth mesh in <see cref="BuildAssembly"/>.
        /// </summary>
        public void SolveAngles(PlanetaryMember input, double inputAngleRad)
        {
            var values = SolveLinear(input, inputAngleRad);
            SunAngleRad = values[(int)PlanetaryMember.Sun];
            CarrierAngleRad = values[(int)PlanetaryMember.Carrier];
        }

        /// <summary>Speeds and the overall ratio for the given fixed member, input member and speed.</summary>
        public PlanetarySpeeds Analyze(PlanetaryMember input, double inputRpm, PlanetaryMember output)
        {
            Validate();
            var v = SolveLinear(input, inputRpm);
            var sun = v[(int)PlanetaryMember.Sun];
            var ring = v[(int)PlanetaryMember.Ring];
            var carrier = v[(int)PlanetaryMember.Carrier];

            var relative = -(sun - carrier) * SunTeeth / PlanetTeeth;
            var outputSpeed = v[(int)output];
            double? overall = output != Fixed && Math.Abs(outputSpeed) > 1e-12
                ? GearPrecision.Round(inputRpm / outputSpeed)
                : (double?)null;

            return new PlanetarySpeeds(
                Fixed,
                input,
                GearPrecision.Round(sun),
                GearPrecision.Round(ring),
                GearPrecision.Round(carrier),
                GearPrecision.Round(carrier + relative),
                GearPrecision.Round(relative),
                output,
                overall);
        }

        /// <summary>
        /// Builds the stage as an assembly posed for <see cref="SunAngleRad"/> and <see cref="CarrierAngleRad"/>:
        /// one shaft for the sun, one for each planet, one for the ring and a carrier plate.
        /// </summary>
        public GearAssembly BuildAssembly()
        {
            Validate();

            var clearance = 0.25 * Module;
            InvoluteGearComponent Create(string name, int teeth, bool internalGear)
            {
                return new InvoluteGearComponent($"{Id}.{name}")
                {
                    Teeth = teeth,
                    Module = Module,
                    PressureAngleDeg = PressureAngleDeg,
                    FaceWidth = FaceWidth,
                    Clearance = clearance,
                    Backlash = Backlash,
                    IsInternal = internalGear,
                    RimThickness = RingRimThickness
                };
            }

            var sun = Create("Sun", SunTeeth, false);
            var ring = Create("Ring", RingTeeth, true);
            var planets = new List<InvoluteGearComponent>();
            for (var i = 0; i < PlanetCount; i++)
            {
                planets.Add(Create($"Planet{i + 1}", PlanetTeeth, false));
            }

            var solver = new ParallelAxisMeshSolver();
            var assembly = new GearAssembly();

            var planetPitchDiameter = Module * PlanetTeeth;
            var sunShaft = new Shaft($"{Id}.SunShaft")
            {
                Diameter = Math.Max(2.0, 0.25 * Module * SunTeeth),
                EndMargin = PlateGap + PlateThickness + 4.0,
                RotationRad = SunAngleRad
            };
            sunShaft.Mount(sun);
            assembly.AddShaft(sunShaft);

            var centre = 0.0;
            var ringPhase = 0.0;
            for (var i = 0; i < PlanetCount; i++)
            {
                var theta = CarrierAngleRad + 2.0 * Math.PI * i / PlanetCount;
                var mesh = solver.Solve(sun, planets[i], SunAngleRad - theta);
                centre = mesh.CentreDistance;
                var planetPhase = mesh.DrivenPhaseRad + theta;

                if (i == 0)
                {
                    var ringMesh = solver.Solve(planets[0], ring, mesh.DrivenPhaseRad);
                    ringPhase = ringMesh.DrivenPhaseRad + theta;
                }

                var origin = GearPrecision.Round(new Vector3D(centre * Math.Cos(theta), centre * Math.Sin(theta), 0.0));
                var planetShaft = new Shaft($"{Id}.PlanetShaft{i + 1}", new Placement3D(origin))
                {
                    Diameter = Math.Max(2.0, 0.25 * planetPitchDiameter),
                    EndMargin = PlateGap,
                    RotationRad = planetPhase
                };
                planetShaft.Mount(planets[i]);
                assembly.AddShaft(planetShaft);
            }

            RingAngleRad = ringPhase;
            var ringShaft = new Shaft($"{Id}.RingShaft") { RotationRad = ringPhase };
            ringShaft.Mount(ring);
            assembly.AddShaft(ringShaft);

            var plateRadius = centre + 0.6 * planetPitchDiameter / 2.0;
            var carrierShaft = new Shaft(
                $"{Id}.Carrier",
                new Placement3D(new Vector3D(0, 0, -FaceWidth / 2.0 - PlateGap - PlateThickness)))
            {
                Diameter = 2.0 * plateRadius,
                Length = PlateThickness,
                RotationRad = CarrierAngleRad
            };
            assembly.AddShaft(carrierShaft);

            Sun = sun;
            Ring = ring;
            Planets = planets;
            return assembly;
        }
    }
}
