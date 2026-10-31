using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>Speed of one shaft in a gear train. Speeds are signed: positive turns like the input shaft.</summary>
    public sealed class ShaftSpeedRow
    {
        public ShaftSpeedRow(string shaftId, double? speedRpm, double? ratioToInput, bool isLinear = false)
        {
            ShaftId = shaftId;
            SpeedRpm = speedRpm;
            RatioToInput = ratioToInput;
            IsLinear = isLinear;
        }

        public string ShaftId { get; }

        /// <summary>True for the carrier of a rack: the speed is then a linear speed in mm/min instead of rpm.</summary>
        public bool IsLinear { get; }

        /// <summary>The unit of <see cref="SpeedRpm"/>: "rpm", or "mm/min" for a rack carrier.</summary>
        public string Unit => IsLinear ? "mm/min" : "rpm";

        /// <summary>Shaft speed (rpm, or mm/min when <see cref="IsLinear"/>); null when the shaft is not connected to the input shaft.</summary>
        public double? SpeedRpm { get; }

        /// <summary>Input speed divided by this shaft's speed (greater than 1 is a speed reduction); null when unknown.</summary>
        public double? RatioToInput { get; }

        /// <summary>"Same as input", "Opposite to input" or "Not connected".</summary>
        public string Direction => !SpeedRpm.HasValue
            ? "Not connected"
            : (SpeedRpm.Value < 0 ? "Opposite to input" : "Same as input");
    }

    /// <summary>Speed of one gear of a gear train.</summary>
    public sealed class GearSpeedRow
    {
        public GearSpeedRow(string gearId, string shaftId, string role, int teeth, double pitchDiameter, double? speedRpm, double? pitchLineVelocity)
        {
            GearId = gearId;
            ShaftId = shaftId;
            Role = role;
            Teeth = teeth;
            PitchDiameter = pitchDiameter;
            SpeedRpm = speedRpm;
            PitchLineVelocity = pitchLineVelocity;
        }

        public string GearId { get; }
        public string ShaftId { get; }

        /// <summary>"Input", "Driver", "Driven", "Idler" or "Compound".</summary>
        public string Role { get; }

        public int Teeth { get; }

        /// <summary>Pitch diameter (mm).</summary>
        public double PitchDiameter { get; }

        public double? SpeedRpm { get; }

        /// <summary>Speed of the pitch circle (m/s); null when the speed is unknown.</summary>
        public double? PitchLineVelocity { get; }
    }

    /// <summary>One meshing stage: a driver gear and the gear it drives.</summary>
    public sealed class GearStageRow
    {
        public GearStageRow(string driverId, string drivenId, double toothRatio, double speedRatio, double centreDistance)
        {
            DriverId = driverId;
            DrivenId = drivenId;
            ToothRatio = toothRatio;
            SpeedRatio = speedRatio;
            CentreDistance = centreDistance;
        }

        public string DriverId { get; }
        public string DrivenId { get; }

        /// <summary>Driven teeth divided by driver teeth.</summary>
        public double ToothRatio { get; }

        /// <summary>Driven speed divided by driver speed; negative when the rotation direction reverses.</summary>
        public double SpeedRatio { get; }

        /// <summary>Working centre distance (mm).</summary>
        public double CentreDistance { get; }
    }

    /// <summary>
    /// Gear ratio and speed report of a solved gear train. All values are rounded to
    /// <see cref="GearPrecision.DecimalPlaces"/> decimal places.
    /// </summary>
    public sealed class GearTrainReport
    {
        public GearTrainReport(
            string inputShaftId,
            double inputSpeedRpm,
            string outputShaftId,
            double? overallRatio,
            IReadOnlyList<ShaftSpeedRow> shafts,
            IReadOnlyList<GearSpeedRow> gears,
            IReadOnlyList<GearStageRow> stages,
            IReadOnlyList<string> warnings)
        {
            InputShaftId = inputShaftId;
            InputSpeedRpm = inputSpeedRpm;
            OutputShaftId = outputShaftId;
            OverallRatio = overallRatio;
            Shafts = shafts;
            Gears = gears;
            Stages = stages;
            Warnings = warnings;
        }

        public string InputShaftId { get; }
        public double InputSpeedRpm { get; }

        /// <summary>The shaft that the overall ratio refers to; null when the train has a single shaft.</summary>
        public string OutputShaftId { get; }

        /// <summary>Input speed divided by output speed (signed, negative when the direction reverses); null when not available.</summary>
        public double? OverallRatio { get; }

        public IReadOnlyList<ShaftSpeedRow> Shafts { get; }
        public IReadOnlyList<GearSpeedRow> Gears { get; }
        public IReadOnlyList<GearStageRow> Stages { get; }
        public IReadOnlyList<string> Warnings { get; }

        /// <summary>Formats the report as plain text (invariant culture).</summary>
        public string ToText()
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine($"Gear train report - input shaft '{InputShaftId}' at {InputSpeedRpm.ToString("0.###", ci)} rpm");
            sb.AppendLine();

            sb.AppendLine("Stages");
            if (Stages.Count == 0) sb.AppendLine("  (none)");
            foreach (var s in Stages)
            {
                sb.AppendLine(
                    $"  {s.DriverId} -> {s.DrivenId}: tooth ratio {s.ToothRatio.ToString("0.0000000", ci)}, " +
                    $"speed ratio {s.SpeedRatio.ToString("0.0000000", ci)}, centre distance {s.CentreDistance.ToString("0.0000000", ci)} mm");
            }

            sb.AppendLine();
            sb.AppendLine("Shafts");
            foreach (var s in Shafts)
            {
                sb.AppendLine(
                    $"  {s.ShaftId}: {Format(s.SpeedRpm, ci)} {s.Unit} ({s.Direction}), ratio to input {Format(s.RatioToInput, ci)}");
            }

            sb.AppendLine();
            sb.AppendLine("Gears");
            foreach (var g in Gears)
            {
                sb.AppendLine(
                    $"  {g.GearId} [{g.Role}] on {g.ShaftId}: z={g.Teeth}, d={g.PitchDiameter.ToString("0.0000000", ci)} mm, " +
                    $"{Format(g.SpeedRpm, ci)} rpm, pitch line velocity {Format(g.PitchLineVelocity, ci)} m/s");
            }

            sb.AppendLine();
            sb.AppendLine(OverallRatio.HasValue
                ? $"Overall ratio (input / '{OutputShaftId}'): {OverallRatio.Value.ToString("0.0000000", ci)}"
                : "Overall ratio: not available");

            foreach (var w in Warnings)
            {
                sb.AppendLine($"Warning: {w}");
            }

            return sb.ToString();
        }

        private static string Format(double? value, CultureInfo ci)
        {
            return value.HasValue ? value.Value.ToString("0.0000000", ci) : "-";
        }
    }
}
