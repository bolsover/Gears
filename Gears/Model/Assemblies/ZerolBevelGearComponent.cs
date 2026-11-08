namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// A zerol bevel gear: curved teeth (the circular arc of a face-mill cutter) with a mean spiral angle of zero. The tooth line is a cone
    /// element at the middle of the face, so the pitch cones, the cone angles and the thrust are those of a straight bevel pair; at the
    /// heel and the toe the spiral angle is small and not zero. The mate has the opposite hand.
    /// </summary>
    public sealed class ZerolBevelGearComponent : SpiralBevelGearComponent
    {
        public ZerolBevelGearComponent(string id) : base(id)
        {
        }

        /// <summary>Always zero; a value set is ignored.</summary>
        public override double MeanSpiralAngleDeg
        {
            get => 0.0;
            set { }
        }

        public override string TypeName => "Zerol bevel gear";
    }
}
