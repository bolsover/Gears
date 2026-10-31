using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Works out shaft speeds, stage ratios and pitch line velocities of a gear train.
    /// Speeds travel from the input shaft through the meshing relations in both directions, so gears on any
    /// branch, compound gears (which share the speed of their shaft) and idlers are all handled.
    /// </summary>
    public static class GearTrainAnalyzer
    {
        /// <summary>
        /// Solves the assembly and builds the report.
        /// </summary>
        /// <param name="assembly">The assembly to analyse.</param>
        /// <param name="inputShaft">The shaft that is driven externally; the first shaft of the assembly when null.</param>
        /// <param name="inputSpeedRpm">Speed of the input shaft in rpm (positive is anticlockwise seen from +Z).</param>
        /// <param name="outputShaft">The shaft for the overall ratio; the last shaft of the assembly when null.</param>
        /// <exception cref="InvalidOperationException">The gear train locks up (two meshes demand different speeds from one shaft).</exception>
        public static GearTrainReport Analyze(GearAssembly assembly, Shaft inputShaft = null, double inputSpeedRpm = 1000.0, Shaft outputShaft = null)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (assembly.Shafts.Count == 0) throw new InvalidOperationException("The assembly has no shafts.");
            if (double.IsNaN(inputSpeedRpm) || double.IsInfinity(inputSpeedRpm))
            {
                throw new ArgumentException("The input speed must be a finite number.", nameof(inputSpeedRpm));
            }

            assembly.Solve();

            inputShaft = inputShaft ?? assembly.Shafts[0];
            if (!assembly.Shafts.Contains(inputShaft))
            {
                throw new ArgumentException("The input shaft is not part of the assembly.", nameof(inputShaft));
            }

            outputShaft = outputShaft ?? assembly.Shafts[assembly.Shafts.Count - 1];
            if (!assembly.Shafts.Contains(outputShaft))
            {
                throw new ArgumentException("The output shaft is not part of the assembly.", nameof(outputShaft));
            }

            var speeds = PropagateSpeeds(assembly, inputShaft, inputSpeedRpm);
            var warnings = new List<string>(assembly.Validate());

            var shaftRows = new List<ShaftSpeedRow>();
            foreach (var shaft in assembly.Shafts)
            {
                if (speeds.TryGetValue(shaft, out var speed))
                {
                    double? ratio = Math.Abs(speed) > 1e-12 ? GearPrecision.Round(inputSpeedRpm / speed) : (double?)null;
                    shaftRows.Add(new ShaftSpeedRow(shaft.Id, GearPrecision.Round(speed), ratio, IsLinearShaft(shaft)));
                }
                else
                {
                    shaftRows.Add(new ShaftSpeedRow(shaft.Id, null, null, IsLinearShaft(shaft)));
                    warnings.Add($"Shaft '{shaft.Id}' is not connected to input shaft '{inputShaft.Id}' by a meshing relation.");
                }
            }

            var gearRows = new List<GearSpeedRow>();
            foreach (var shaft in assembly.Shafts)
            {
                foreach (var mount in shaft.Mounts)
                {
                    var gear = mount.Component;
                    double? speed = speeds.TryGetValue(shaft, out var s) ? GearPrecision.Round(s) : (double?)null;
                    double? velocity = speed.HasValue
                        ? GearPrecision.Round(gear.IsLinear
                            ? Math.Abs(speed.Value) / 60000.0
                            : Math.PI * gear.PitchDiameter * Math.Abs(speed.Value) / 60000.0)
                        : (double?)null;
                    gearRows.Add(new GearSpeedRow(
                        gear.Id,
                        shaft.Id,
                        DescribeRole(assembly, shaft, inputShaft, gear),
                        gear.ToothCount,
                        GearPrecision.Round(gear.PitchDiameter),
                        speed,
                        velocity));
                }
            }

            var stageRows = new List<GearStageRow>();
            foreach (var relation in assembly.Relations)
            {
                var factor = SpeedFactor(relation);
                stageRows.Add(new GearStageRow(
                    relation.Driver.Id,
                    relation.Driven.Id,
                    GearPrecision.Round((double)relation.Driven.ToothCount / relation.Driver.ToothCount),
                    GearPrecision.Round(factor),
                    relation.Result != null ? relation.Result.CentreDistance : 0.0));
            }

            double? overall = null;
            string outputId = null;
            if (!ReferenceEquals(inputShaft, outputShaft))
            {
                outputId = outputShaft.Id;
                if (speeds.TryGetValue(outputShaft, out var outSpeed) && Math.Abs(outSpeed) > 1e-12)
                {
                    overall = GearPrecision.Round(inputSpeedRpm / outSpeed);
                }
            }

            return new GearTrainReport(inputShaft.Id, inputSpeedRpm, outputId, overall, shaftRows, gearRows, stageRows, warnings);
        }

        /// <summary>
        /// Driven speed divided by driver speed for one relation: minus the driver-to-driven tooth ratio for an external
        /// pair (the direction reverses) and plus the ratio when exactly one of the gears is internal.
        /// </summary>
        public static double SpeedFactor(GearMeshRelation relation)
        {
            if (relation == null) throw new ArgumentNullException(nameof(relation));
            return MeshSolvers.Get(relation.MeshType).SpeedFactor(relation.Driver, relation.Driven);
        }

        private static Dictionary<Shaft, double> PropagateSpeeds(GearAssembly assembly, Shaft inputShaft, double inputSpeedRpm)
        {
            var speeds = new Dictionary<Shaft, double> { { inputShaft, inputSpeedRpm } };
            var queue = new Queue<Shaft>();
            queue.Enqueue(inputShaft);

            var edges = new List<(GearMeshRelation Relation, Shaft DriverShaft, Shaft DrivenShaft)>();
            foreach (var relation in assembly.Relations)
            {
                assembly.FindMount(relation.Driver, out var driverShaft);
                assembly.FindMount(relation.Driven, out var drivenShaft);
                edges.Add((relation, driverShaft, drivenShaft));
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentSpeed = speeds[current];

                foreach (var edge in edges)
                {
                    var factor = SpeedFactor(edge.Relation);
                    Shaft other;
                    double otherSpeed;
                    if (ReferenceEquals(edge.DriverShaft, current))
                    {
                        other = edge.DrivenShaft;
                        otherSpeed = currentSpeed * factor;
                    }
                    else if (ReferenceEquals(edge.DrivenShaft, current))
                    {
                        other = edge.DriverShaft;
                        otherSpeed = currentSpeed / factor;
                    }
                    else
                    {
                        continue;
                    }

                    if (speeds.TryGetValue(other, out var known))
                    {
                        var tolerance = 1e-9 * Math.Max(1.0, Math.Max(Math.Abs(known), Math.Abs(otherSpeed)));
                        if (Math.Abs(known - otherSpeed) > tolerance)
                        {
                            throw new InvalidOperationException(
                                $"The gear train locks up: meshing '{edge.Relation.Driver.Id}' with '{edge.Relation.Driven.Id}' " +
                                $"requires shaft '{other.Id}' to run at {GearPrecision.Round(otherSpeed)} rpm, " +
                                $"but another mesh makes it run at {GearPrecision.Round(known)} rpm.");
                        }

                        continue;
                    }

                    speeds[other] = otherSpeed;
                    queue.Enqueue(other);
                }
            }

            return speeds;
        }

        private static bool IsLinearShaft(Shaft shaft)
        {
            return shaft.Mounts.Any(m => m.Component.IsLinear);
        }

        private static string DescribeRole(GearAssembly assembly, Shaft shaft, Shaft inputShaft, GearComponent gear)
        {
            var isDriven = assembly.Relations.Any(r => ReferenceEquals(r.Driven, gear));
            var isDriver = assembly.Relations.Any(r => ReferenceEquals(r.Driver, gear));

            if (isDriven && isDriver && shaft.Mounts.Count == 1) return "Idler";
            if (shaft.Mounts.Count > 1) return "Compound";
            if (ReferenceEquals(shaft, inputShaft)) return "Input";
            return isDriven ? "Driven" : "Driver";
        }
    }
}
