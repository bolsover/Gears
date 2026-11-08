using System;
using Bolsover.Gears.Calculator;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// A hypoid gear: a spiral bevel gear on a shaft that does not intersect the shaft of its mate (offset <see cref="OffsetMm"/>).
    /// <para>
    /// The pair is described at the mean point (see <see cref="HypoidGeometry"/>). <see cref="Module"/> is the mean transverse module of the
    /// gear (the large wheel), the same on both components of a pair, so the gear has the mean pitch radius <c>m z2 / 2</c>.
    /// <see cref="MeanSpiralAngleDeg"/> is the mean spiral angle of the pinion, also the same on both components; the gear has the
    /// spiral angle <c>beta1 - eps</c>. The pinion pitch radius, both pitch cone angles and the apex positions follow from the pair solution,
    /// so the pinion is larger than its tooth count suggests and the cone apexes do not coincide. The tooth depth is that of the gear for
    /// both gears. The cone distance is the distance from the gear's own apex, the face is centred on the mean point and the face width is
    /// measured along the cone. Only the standard tooth proportions are used.
    /// </para>
    /// </summary>
    public sealed class HypoidGearComponent : SpiralBevelGearComponent
    {
        private HypoidPair _pair;
        private string _pairError;
        private (int, int, double, double, double, double) _pairKey;
        private bool _pairKeyValid;

        public HypoidGearComponent(string id) : base(id)
        {
            MeanSpiralAngleDeg = 45.0;
        }

        /// <summary>
        /// Offset of the pinion axis from the gear axis (mm), positive or negative for the two sides; set by the mesh solver from the
        /// relation, and the same on both components of a pair.
        /// </summary>
        public double OffsetMm { get; set; }

        /// <summary>True for the small gear of the pair (the pinion).</summary>
        public bool IsPinion { get; set; }

        public override string TypeName => IsPinion ? "Hypoid pinion" : "Hypoid gear";

        private int PinionTeeth => IsPinion ? Teeth : EffectiveMateTeeth;

        private int GearTeeth => IsPinion ? EffectiveMateTeeth : Teeth;

        /// <summary>The solved pair at the mean point, or null when the data give no solution (see <see cref="PairError"/>).</summary>
        public HypoidPair Pair
        {
            get
            {
                EnsurePair();
                return _pair;
            }
        }

        /// <summary>Why <see cref="Pair"/> is null; null when the pair is solved.</summary>
        public string PairError
        {
            get
            {
                EnsurePair();
                return _pairError;
            }
        }

        private void EnsurePair()
        {
            var key = (PinionTeeth, GearTeeth, Module, OffsetMm, ShaftAngleDeg, MeanSpiralAngleDeg);
            if (_pairKeyValid && _pairKey.Equals(key)) return;

            _pairKey = key;
            _pairKeyValid = true;
            try
            {
                _pair = HypoidGeometry.Solve(PinionTeeth, GearTeeth, Module, OffsetMm, ShaftAngleDeg, MeanSpiralAngleDeg);
                _pairError = null;
            }
            catch (ArgumentException ex)
            {
                _pair = null;
                _pairError = ex.Message;
            }
        }

        protected override double TraceSpiralAngleDeg
        {
            get
            {
                var pair = Pair;
                if (pair == null) return MeanSpiralAngleDeg;
                return IsPinion ? pair.PinionSpiralAngleDeg : pair.GearSpiralAngleDeg;
            }
        }

        /// <summary>Distance from the apex of this gear to the mean point (mm).</summary>
        public double MeanConeDistance
        {
            get
            {
                var pair = Pair;
                if (pair == null) return base.ConeDistance - FaceWidth / 2.0;
                return IsPinion ? pair.PinionMeanConeDistance : pair.GearMeanConeDistance;
            }
        }

        public override double PitchConeAngleRad
        {
            get
            {
                var pair = Pair;
                if (pair == null) return base.PitchConeAngleRad;
                return IsPinion ? pair.PinionConeAngleRad : pair.GearConeAngleRad;
            }
        }

        public override double ConeDistance => Pair == null ? base.ConeDistance : MeanConeDistance + FaceWidth / 2.0;

        /// <summary>Pitch diameter at the large end (mm).</summary>
        public override double PitchDiameter => Pair == null ? base.PitchDiameter : 2.0 * ConeDistance * Math.Sin(PitchConeAngleRad);

        /// <summary>The tooth depth is that of the gear at its large end, for both gears.</summary>
        protected override double DepthModule
        {
            get
            {
                var pair = Pair;
                if (pair == null) return Module;
                return Module * (pair.GearMeanConeDistance + FaceWidth / 2.0) / pair.GearMeanConeDistance;
            }
        }

        public override BevelBlankProfile CreateBlankProfile()
        {
            var pair = Pair;
            if (pair == null) return base.CreateBlankProfile();

            return new BevelBlankProfile(
                Teeth, EffectiveMateTeeth, PitchDiameter / Teeth, FaceWidth, ShaftAngleDeg,
                depthModule: DepthModule, pitchConeAngleOverrideRad: PitchConeAngleRad);
        }

        public override void Validate()
        {
            if (BevelType != BevelGearType.Standard)
            {
                throw new InvalidOperationException($"{TypeName} '{Id}': hypoid gears use the standard tooth proportions only.");
            }

            if (!(Math.Abs(OffsetMm) > 1e-9))
            {
                throw new InvalidOperationException(
                    $"{TypeName} '{Id}': the offset of a hypoid pair must not be zero; use a spiral bevel gear for intersecting shafts.");
            }

            if (Pair == null)
            {
                throw new InvalidOperationException($"{TypeName} '{Id}': {PairError}");
            }

            base.Validate();
        }
    }
}
