using System;
using System.Collections.Generic;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// A gear fixed to a shaft at an axial offset, with a rotational (tooth) phase.
    /// </summary>
    public sealed class ShaftMount
    {
        public ShaftMount(GearComponent component, double axialOffset)
        {
            Component = component ?? throw new ArgumentNullException(nameof(component));
            AxialOffset = axialOffset;
        }

        public GearComponent Component { get; }

        /// <summary>The shaft carrying this gear; set when the gear is mounted.</summary>
        public Shaft Shaft { get; internal set; }

        /// <summary>Position of the gear's mid-plane along the shaft axis, measured from the shaft origin (mm).</summary>
        public double AxialOffset { get; set; }

        /// <summary>
        /// Fixed angular position of the gear on its shaft, relative to the shaft's rotation (radians).
        /// The mesh solver clocks a gear onto its shaft with this value when the shaft is already
        /// turned by an earlier relation (for example the second gear of a compound pair).
        /// </summary>
        public double PhaseOffsetRad { get; set; }

        /// <summary>
        /// Total rotation of the gear about the shaft axis (radians): the shaft rotation plus <see cref="PhaseOffsetRad"/>.
        /// Setting it changes <see cref="PhaseOffsetRad"/> so that the total matches the value.
        /// </summary>
        public double PhaseRad
        {
            get => (Shaft?.RotationRad ?? 0.0) + PhaseOffsetRad;
            set => PhaseOffsetRad = value - (Shaft?.RotationRad ?? 0.0);
        }

        /// <summary>Axial extent of the gear along the shaft axis (mm): from the lower to the upper face.</summary>
        public void GetAxialExtent(out double start, out double end)
        {
            var half = Component.FaceWidth / 2.0;
            start = AxialOffset - half;
            end = AxialOffset + half;
        }

        /// <summary>Axial extent of the whole gear body along the shaft axis (mm), including a hub outside the face width.</summary>
        public void GetBodyAxialExtent(out double start, out double end)
        {
            Component.GetBodyAxialExtent(out var lo, out var hi);
            start = AxialOffset + lo;
            end = AxialOffset + hi;
        }
    }

    /// <summary>
    /// A shaft with a placement in the assembly and an ordered list of gears mounted on it.
    /// </summary>
    public sealed class Shaft
    {
        private readonly List<ShaftMount> _mounts = new List<ShaftMount>();
        private Placement3D _placement;

        public Shaft(string id, Placement3D placement = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A shaft requires a non-empty identifier.", nameof(id));
            }

            Id = id;
            _placement = placement ?? Placement3D.Identity;
        }

        public string Id { get; }

        /// <summary>Shaft diameter (mm), used for display and for the shaft mesh.</summary>
        public double Diameter { get; set; } = 10.0;

        /// <summary>
        /// Shaft length (mm). Zero means automatic: the shaft spans all mounted gears plus <see cref="EndMargin"/> at each end.
        /// </summary>
        public double Length { get; set; }

        /// <summary>Extra shaft length beyond the outermost gear faces when <see cref="Length"/> is automatic (mm).</summary>
        public double EndMargin { get; set; } = 10.0;

        /// <summary>
        /// Rotation of the whole shaft about its axis (radians). For the first shaft of an assembly this is the input angle
        /// (set it to animate the train); for every other shaft it is solved from the meshing relations.
        /// </summary>
        public double RotationRad { get; set; }

        /// <summary>Computes the shaft's axial start and end (mm, from the shaft origin) honouring <see cref="Length"/>.</summary>
        public void GetAxialSpan(out double start, out double end)
        {
            if (_mounts.Count == 0)
            {
                start = 0.0;
                end = Length > 0 ? Length : 2.0 * EndMargin;
                return;
            }

            var min = double.MaxValue;
            var max = double.MinValue;
            foreach (var mount in _mounts)
            {
                mount.GetBodyAxialExtent(out var s, out var e);
                if (s < min) min = s;
                if (e > max) max = e;
            }

            if (Length > 0)
            {
                var centre = (min + max) / 2.0;
                start = centre - Length / 2.0;
                end = centre + Length / 2.0;
            }
            else
            {
                start = min - EndMargin;
                end = max + EndMargin;
            }
        }

        public Placement3D Placement
        {
            get => _placement;
            set => _placement = value ?? throw new ArgumentNullException(nameof(value));
        }

        public IReadOnlyList<ShaftMount> Mounts => _mounts;

        /// <summary>Mounts a gear on this shaft at the given axial offset.</summary>
        public ShaftMount Mount(GearComponent component, double axialOffset = 0.0)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            foreach (var existing in _mounts)
            {
                if (ReferenceEquals(existing.Component, component))
                {
                    throw new InvalidOperationException($"Gear '{component.Id}' is already mounted on shaft '{Id}'.");
                }
            }

            var mount = new ShaftMount(component, axialOffset) { Shaft = this };
            component.CarrierShaft = this;
            _mounts.Add(mount);
            return mount;
        }

        /// <summary>Removes a gear from this shaft. Returns false when it was not mounted here.</summary>
        public bool Unmount(GearComponent component)
        {
            for (var i = 0; i < _mounts.Count; i++)
            {
                if (!ReferenceEquals(_mounts[i].Component, component)) continue;
                _mounts[i].Shaft = null;
                if (ReferenceEquals(component.CarrierShaft, this)) component.CarrierShaft = null;
                _mounts.RemoveAt(i);
                return true;
            }

            return false;
        }
    }
}
