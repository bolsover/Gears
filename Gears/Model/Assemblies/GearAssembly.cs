using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator.Assemblies;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// A set of shafts carrying gears, plus the meshing relations between those gears.
    /// <para>
    /// <see cref="Solve"/> keeps the first shaft where it is and positions every other shaft from the
    /// shaft that drives it: at the working centre distance in the direction given by the relation's
    /// <see cref="GearMeshRelation.CentreLineAngleDeg"/> (default along the driver shaft's local X direction),
    /// parallel to the driver shaft's axis, with the gears' mid-planes aligned. The first relation that reaches a
    /// shaft sets <see cref="Shaft.RotationRad"/> so that its teeth mesh; further gears of a compound shaft are
    /// clocked onto the shaft through <see cref="ShaftMount.PhaseOffsetRad"/>. Several gears can share a shaft, so
    /// compound gears and idlers form branching trains. Relations must be solvable in dependency order,
    /// that is the driver's shaft has to be placed before the driven gear's shaft.
    /// </para>
    /// </summary>
    public class GearAssembly
    {
        private readonly List<Shaft> _shafts = new List<Shaft>();
        private readonly List<GearMeshRelation> _relations = new List<GearMeshRelation>();
        private readonly List<TimingBelt> _belts = new List<TimingBelt>();

        public IReadOnlyList<Shaft> Shafts => _shafts;
        public IReadOnlyList<GearMeshRelation> Relations => _relations;

        /// <summary>The timing belts that connect pulleys of different shafts.</summary>
        public IReadOnlyList<TimingBelt> Belts => _belts;

        /// <summary>All gears mounted on the assembly's shafts.</summary>
        public IEnumerable<GearComponent> Components => _shafts.SelectMany(s => s.Mounts.Select(m => m.Component));

        public Shaft AddShaft(Shaft shaft)
        {
            if (shaft == null) throw new ArgumentNullException(nameof(shaft));
            if (_shafts.Any(s => s.Id == shaft.Id))
            {
                throw new ArgumentException($"A shaft with identifier '{shaft.Id}' already exists in the assembly.", nameof(shaft));
            }

            foreach (var mount in shaft.Mounts)
            {
                if (FindMount(mount.Component, out _) != null)
                {
                    throw new ArgumentException($"Gear '{mount.Component.Id}' is already mounted on another shaft of the assembly.", nameof(shaft));
                }
            }

            _shafts.Add(shaft);
            return shaft;
        }

        /// <summary>Finds the mount of a gear and the shaft that carries it; returns null when the gear is not mounted.</summary>
        public ShaftMount FindMount(GearComponent component, out Shaft shaft)
        {
            foreach (var candidate in _shafts)
            {
                foreach (var mount in candidate.Mounts)
                {
                    if (ReferenceEquals(mount.Component, component))
                    {
                        shaft = candidate;
                        return mount;
                    }
                }
            }

            shaft = null;
            return null;
        }

        /// <summary>
        /// Declares that <paramref name="driven"/> meshes with <paramref name="driver"/>.
        /// Both gears must already be mounted on different shafts, and a gear can be driven by one gear only.
        /// </summary>
        public GearMeshRelation AddRelation(GearComponent driver, GearComponent driven, MeshType meshType = MeshType.Parallel, double centreLineAngleDeg = 0.0,
            double shaftAngleDeg = 0.0, double offsetMm = 0.0)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (driven == null) throw new ArgumentNullException(nameof(driven));
            if (ReferenceEquals(driver, driven)) throw new ArgumentException("A gear cannot mesh with itself.");

            if (FindMount(driver, out var driverShaft) == null)
            {
                throw new InvalidOperationException($"Gear '{driver.Id}' is not mounted on a shaft of this assembly.");
            }

            if (FindMount(driven, out var drivenShaft) == null)
            {
                throw new InvalidOperationException($"Gear '{driven.Id}' is not mounted on a shaft of this assembly.");
            }

            if (ReferenceEquals(driverShaft, drivenShaft))
            {
                throw new InvalidOperationException($"Gears '{driver.Id}' and '{driven.Id}' are on the same shaft and cannot mesh.");
            }

            if (_relations.Any(r => ReferenceEquals(r.Driven, driven)))
            {
                throw new InvalidOperationException($"Gear '{driven.Id}' is already driven by another gear.");
            }

            var relation = new GearMeshRelation(driver, driven, meshType)
            {
                CentreLineAngleDeg = centreLineAngleDeg,
                ShaftAngleDeg = shaftAngleDeg,
                OffsetMm = offsetMm
            };
            _relations.Add(relation);
            return relation;
        }

        /// <summary>Removes a relation. Returns false when it is not part of the assembly.</summary>
        public bool RemoveRelation(GearMeshRelation relation) => _relations.Remove(relation);

        /// <summary>
        /// Adds a belt round two pulleys. Both pulleys must be mounted on different shafts of the assembly, and a pulley can be driven by one
        /// belt only.
        /// </summary>
        public TimingBelt AddBelt(TimingBelt belt)
        {
            if (belt == null) throw new ArgumentNullException(nameof(belt));
            if (_belts.Any(b => b.Id == belt.Id))
            {
                throw new ArgumentException($"A belt with identifier '{belt.Id}' already exists in the assembly.", nameof(belt));
            }

            if (FindMount(belt.Driver, out var driverShaft) == null)
            {
                throw new InvalidOperationException($"Pulley '{belt.Driver.Id}' is not mounted on a shaft of this assembly.");
            }

            if (FindMount(belt.Driven, out var drivenShaft) == null)
            {
                throw new InvalidOperationException($"Pulley '{belt.Driven.Id}' is not mounted on a shaft of this assembly.");
            }

            if (ReferenceEquals(driverShaft, drivenShaft))
            {
                throw new InvalidOperationException($"Pulleys '{belt.Driver.Id}' and '{belt.Driven.Id}' are on the same shaft and cannot be joined by a belt.");
            }

            if (_belts.Any(b => ReferenceEquals(b.Driven, belt.Driven)) || _relations.Any(r => ReferenceEquals(r.Driven, belt.Driven)))
            {
                throw new InvalidOperationException($"Pulley '{belt.Driven.Id}' is already driven by another belt or gear.");
            }

            belt.Validate();
            _belts.Add(belt);
            return belt;
        }

        /// <summary>Removes a belt. Returns false when it is not part of the assembly.</summary>
        public bool RemoveBelt(TimingBelt belt) => _belts.Remove(belt);

        /// <summary>Returns the belt that drives the pulley, or null.</summary>
        public TimingBelt FindDrivingBelt(GearComponent component)
        {
            return _belts.FirstOrDefault(b => ReferenceEquals(b.Driven, component));
        }

        /// <summary>Returns the belts that run round the pulley (as driver or driven).</summary>
        public IEnumerable<TimingBelt> FindBelts(GearComponent component)
        {
            return _belts.Where(b => ReferenceEquals(b.Driver, component) || ReferenceEquals(b.Driven, component));
        }

        /// <summary>
        /// Removes a shaft with its gears and every relation that involves one of those gears.
        /// Returns false when the shaft is not part of the assembly.
        /// </summary>
        public bool RemoveShaft(Shaft shaft)
        {
            if (shaft == null || !_shafts.Contains(shaft)) return false;
            foreach (var mount in shaft.Mounts.ToList())
            {
                RemoveGear(mount.Component);
            }

            return _shafts.Remove(shaft);
        }

        /// <summary>
        /// Removes a gear from its shaft together with the relations that involve it.
        /// Returns false when the gear is not mounted in the assembly.
        /// </summary>
        public bool RemoveGear(GearComponent component)
        {
            if (FindMount(component, out var shaft) == null) return false;
            _relations.RemoveAll(r => ReferenceEquals(r.Driver, component) || ReferenceEquals(r.Driven, component));
            _belts.RemoveAll(b => ReferenceEquals(b.Driver, component) || ReferenceEquals(b.Driven, component));
            return shaft.Unmount(component);
        }

        /// <summary>Returns the relation in which the gear is the driven gear, or null.</summary>
        public GearMeshRelation FindDrivingRelation(GearComponent component)
        {
            return _relations.FirstOrDefault(r => ReferenceEquals(r.Driven, component));
        }

        /// <summary>
        /// Checks the layout for problems that do not stop the solver: gears on the same shaft whose axial
        /// extents overlap. Returns one message per problem; an empty list means no problems were found.
        /// </summary>
        public IReadOnlyList<string> Validate()
        {
            var problems = new List<string>();
            foreach (var shaft in _shafts)
            {
                for (var i = 0; i < shaft.Mounts.Count; i++)
                {
                    for (var j = i + 1; j < shaft.Mounts.Count; j++)
                    {
                        shaft.Mounts[i].GetAxialExtent(out var s1, out var e1);
                        shaft.Mounts[j].GetAxialExtent(out var s2, out var e2);
                        var overlap = Math.Min(e1, e2) - Math.Max(s1, s2);
                        if (overlap > GearPrecision.Tolerance)
                        {
                            problems.Add(
                                $"Gears '{shaft.Mounts[i].Component.Id}' and '{shaft.Mounts[j].Component.Id}' on shaft '{shaft.Id}' " +
                                $"overlap axially by {GearPrecision.Round(overlap)} mm.");
                        }
                    }
                }
            }

            return problems;
        }

        /// <summary>
        /// Solves the placement of every shaft and the phase of every driven gear. The results are rounded
        /// to <see cref="GearPrecision.DecimalPlaces"/> decimal places. Safe to call repeatedly.
        /// </summary>
        public void Solve()
        {
            foreach (var relation in _relations)
            {
                relation.Result = null;
            }

            if (_shafts.Count == 0 || (_relations.Count == 0 && _belts.Count == 0)) return;

            var placed = new HashSet<Shaft> { _shafts[0] };
            var pending = new List<GearMeshRelation>(_relations);
            var pendingBelts = new List<TimingBelt>(_belts);

            while (pending.Count > 0 || pendingBelts.Count > 0)
            {
                var progressed = false;
                for (var i = 0; i < pendingBelts.Count; i++)
                {
                    if (!SolveBelt(pendingBelts[i], placed)) continue;
                    pendingBelts.RemoveAt(i);
                    i--;
                    progressed = true;
                }
                for (var i = 0; i < pending.Count; i++)
                {
                    var relation = pending[i];
                    var driverMount = FindMount(relation.Driver, out var driverShaft);
                    var drivenMount = FindMount(relation.Driven, out var drivenShaft);
                    if (driverMount == null || drivenMount == null)
                    {
                        throw new InvalidOperationException(
                            $"The relation between '{relation.Driver.Id}' and '{relation.Driven.Id}' refers to a gear that is no longer mounted.");
                    }

                    if (!placed.Contains(driverShaft)) continue;

                    SolveRelation(relation, driverMount, driverShaft, drivenMount, drivenShaft, placed);
                    pending.RemoveAt(i);
                    i--;
                    progressed = true;
                }

                if (!progressed && pending.Count == 0)
                {
                    var firstBelt = pendingBelts[0];
                    throw new InvalidOperationException(
                        $"Cannot place belt '{firstBelt.Id}': neither of its pulleys is on a shaft that is connected to the first shaft of the assembly.");
                }

                if (!progressed)
                {
                    var first = pending[0];
                    throw new InvalidOperationException(
                        $"Cannot place the driven gear '{first.Driven.Id}': the shaft carrying its driver '{first.Driver.Id}' " +
                        "is not connected to the first shaft of the assembly through earlier relations.");
                }
            }
        }

        /// <summary>
        /// Places the shaft of the pulley that is not yet placed from the other one, at the centre distance of the belt along its line of
        /// centres with the mid-planes in line, and turns it at the ratio of the grooves, in the same direction. Returns false when neither
        /// shaft is placed yet.
        /// <para>
        /// The rotation of the new shaft also registers the belt on the grooves: a belt tooth is centred on a groove of each pulley, see
        /// <see cref="BeltRegistration"/>. The driven phase is the driver phase over the gear ratio, kept unwrapped, plus a bounded offset C of
        /// at most half a groove step of the driven pulley. The offset does not depend on the driver phase, so the driven pulley follows the
        /// driver exactly at the ratio, and when only the driven shaft is placed the driver phase follows from it in one step.
        /// </para>
        /// <para>
        /// Known limitation: when both shafts are already placed by other relations the belt is only checked against the placement; neither
        /// pulley is turned, so the driven pulley is not registered on the belt in that case.
        /// </para>
        /// </summary>
        private bool SolveBelt(TimingBelt belt, ISet<Shaft> placed)
        {
            var driverMount = FindMount(belt.Driver, out var driverShaft);
            var drivenMount = FindMount(belt.Driven, out var drivenShaft);
            if (driverMount == null || drivenMount == null)
            {
                throw new InvalidOperationException($"The belt '{belt.Id}' refers to a pulley that is no longer mounted.");
            }

            var driverPlaced = placed.Contains(driverShaft);
            var drivenPlaced = placed.Contains(drivenShaft);
            if (!driverPlaced && !drivenPlaced) return false;

            belt.Validate();
            var theta = belt.CentreLineAngleDeg * Math.PI / 180.0;
            var axialShift = driverMount.AxialOffset - drivenMount.AxialOffset;

            if (driverPlaced)
            {
                var frame = driverShaft.Placement;
                var direction = frame.XAxis * Math.Cos(theta) + frame.YAxis * Math.Sin(theta);
                var origin = GearPrecision.Round(frame.Origin + direction * belt.CentreDistance + frame.ZAxis * axialShift);

                if (drivenPlaced)
                {
                    var error = (drivenShaft.Placement.Origin - origin).Length;
                    if (error > 10 * GearPrecision.Tolerance)
                    {
                        throw new InvalidOperationException(
                            $"Shaft '{drivenShaft.Id}' is over-constrained: belt '{belt.Id}' requires its origin at {origin}, but it is already placed at {drivenShaft.Placement.Origin}.");
                    }

                    return true;
                }

                drivenShaft.Placement = new Placement3D(origin, frame.ZAxis, frame.XAxis);
                placed.Add(drivenShaft);
                var driverPhase = driverMount.PhaseRad;
                var offset = BeltRegistration.Compute(belt, driverPhase).DrivenOffsetRad;
                drivenShaft.RotationRad = driverPhase / belt.GearRatio + offset - drivenMount.PhaseOffsetRad;
                return true;
            }

            // Only the driven shaft is placed: place the driver from it, with the same orientation.
            var drivenFrame = drivenShaft.Placement;
            var back = drivenFrame.XAxis * Math.Cos(theta) + drivenFrame.YAxis * Math.Sin(theta);
            var driverOrigin = GearPrecision.Round(drivenFrame.Origin - back * belt.CentreDistance - drivenFrame.ZAxis * axialShift);
            driverShaft.Placement = new Placement3D(driverOrigin, drivenFrame.ZAxis, drivenFrame.XAxis);
            placed.Add(driverShaft);
            var drivenPhase = drivenMount.PhaseRad;
            var drivenOffset = BeltRegistration.Compute(belt, drivenPhase * belt.GearRatio).DrivenOffsetRad;
            driverShaft.RotationRad = (drivenPhase - drivenOffset) * belt.GearRatio - driverMount.PhaseOffsetRad;
            return true;
        }

        private static void SolveRelation(
            GearMeshRelation relation,
            ShaftMount driverMount,
            Shaft driverShaft,
            ShaftMount drivenMount,
            Shaft drivenShaft,
            ISet<Shaft> placed)
        {
            var solver = MeshSolvers.Get(relation.MeshType);

            // The solver works with the line of centres along X. For a line of centres at angle theta the whole
            // pair is turned by theta, which is a rigid motion: the driver angle seen by the solver is phi1 - theta
            // and the driven angle is turned back by theta. For theta = 0 the result is the solver's unchanged.
            var theta = relation.CentreLineAngleDeg * Math.PI / 180.0;
            MeshResult solved;
            if (solver is IAdjustableCentreSolver adjustable)
            {
                solved = adjustable.Solve(relation.Driver, relation.Driven, driverMount.PhaseRad - theta, relation.CentreDistanceOverride);
            }
            else if (relation.CentreDistanceOverride.HasValue)
            {
                throw new InvalidOperationException(
                    $"The centre distance between '{relation.Driver.Id}' and '{relation.Driven.Id}' cannot be set: " +
                    $"{relation.MeshType} meshes do not support a fixed centre distance.");
            }
            else if (solver is ICrossedAxisSolver crossed)
            {
                solved = crossed.Solve(relation.Driver, relation.Driven, driverMount.PhaseRad - theta, relation.ShaftAngleDeg);
            }
            else if (solver is IOffsetAxisSolver offset)
            {
                solved = offset.Solve(relation.Driver, relation.Driven, driverMount.PhaseRad - theta, relation.OffsetMm);
            }
            else
            {
                solved = solver.Solve(relation.Driver, relation.Driven, driverMount.PhaseRad - theta);
            }
            if (solved.DrivenFrame != null)
            {
                SolveFramed(relation, solved, theta, driverMount, driverShaft, drivenMount, drivenShaft, placed);
                return;
            }

            var result = Math.Abs(theta) < 1e-15
                ? solved
                : new MeshResult(solved.CentreDistance, solved.DrivenPhaseRad + theta, solved.GearRatio, solved.PairData);

            var frame = driverShaft.Placement;
            var direction = frame.XAxis * Math.Cos(theta) + frame.YAxis * Math.Sin(theta);
            var origin = GearPrecision.Round(
                frame.Origin
                + direction * result.CentreDistance
                + frame.ZAxis * (driverMount.AxialOffset - drivenMount.AxialOffset));

            if (placed.Contains(drivenShaft))
            {
                var error = (drivenShaft.Placement.Origin - origin).Length;
                if (error > 10 * GearPrecision.Tolerance)
                {
                    throw new InvalidOperationException(
                        $"Shaft '{drivenShaft.Id}' is over-constrained: meshing '{relation.Driven.Id}' with '{relation.Driver.Id}' " +
                        $"requires its origin at {origin}, but it is already placed at {drivenShaft.Placement.Origin}.");
                }
            }
            else
            {
                drivenShaft.Placement = new Placement3D(origin, frame.ZAxis, frame.XAxis);
                placed.Add(drivenShaft);

                // The first relation to reach a shaft decides how far the shaft is turned.
                drivenShaft.RotationRad = result.DrivenPhaseRad - drivenMount.PhaseOffsetRad;
                relation.Result = result;
                return;
            }

            // The shaft is already turned by an earlier relation (compound gear): clock this gear onto the shaft.
            drivenMount.PhaseOffsetRad = result.DrivenPhaseRad - drivenShaft.RotationRad;
            relation.Result = result;
        }

        /// <summary>
        /// Places the driven shaft from the frame returned by the solver (rack, bevel and worm meshes). The solver's frame is
        /// relative to the driver gear with the line of centres along X; the whole pair is then turned by theta about the
        /// driver axis, which is a rigid motion, so the solved phase needs no further correction.
        /// </summary>
        private static void SolveFramed(
            GearMeshRelation relation,
            MeshResult result,
            double theta,
            ShaftMount driverMount,
            Shaft driverShaft,
            ShaftMount drivenMount,
            Shaft drivenShaft,
            ISet<Shaft> placed)
        {
            var gearFrame = driverShaft.Placement.ToTransform()
                .Compose(Transform3D.Translate(new Vector3D(0, 0, driverMount.AxialOffset)))
                .Compose(Transform3D.RotationZ(theta))
                .Compose(result.DrivenFrame.ToTransform())
                .Compose(Transform3D.Translate(new Vector3D(0, 0, -drivenMount.AxialOffset)));

            var origin = GearPrecision.Round(gearFrame.Translation);
            var zAxis = gearFrame.TransformDirection(Vector3D.UnitZ);
            var xAxis = gearFrame.TransformDirection(Vector3D.UnitX);

            if (placed.Contains(drivenShaft))
            {
                var existing = drivenShaft.Placement;
                var originError = (existing.Origin - origin).Length;
                var axisError = existing.ZAxis.Cross(zAxis).Length;
                if (originError > 10 * GearPrecision.Tolerance || axisError > 1e-6)
                {
                    throw new InvalidOperationException(
                        $"Shaft '{drivenShaft.Id}' is over-constrained: meshing '{relation.Driven.Id}' with '{relation.Driver.Id}' " +
                        $"requires its origin at {origin} and axis {zAxis}, but it is already placed at {existing.Origin} with axis {existing.ZAxis}.");
                }

                drivenMount.PhaseOffsetRad = result.DrivenPhaseRad - drivenShaft.RotationRad;
                relation.Result = result;
                return;
            }

            drivenShaft.Placement = new Placement3D(origin, zAxis, xAxis);
            placed.Add(drivenShaft);
            drivenShaft.RotationRad = result.DrivenPhaseRad - drivenMount.PhaseOffsetRad;
            relation.Result = result;
        }
    }
}
