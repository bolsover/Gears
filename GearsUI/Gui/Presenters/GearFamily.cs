namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>The kind of gear that a new train starts with.</summary>
    public enum GearFamily
    {
        Spur,
        Bevel,
        Worm,

        /// <summary>A spiral bevel gear (35 degree mean spiral angle, right hand).</summary>
        SpiralBevel,

        /// <summary>A zerol bevel gear (curved teeth, zero mean spiral angle, right hand).</summary>
        ZerolBevel,

        /// <summary>The pinion of a hypoid pair, with the offset of a hypoid mate already set.</summary>
        Hypoid,

        /// <summary>A helical spur gear (15 degree helix angle, right hand).</summary>
        Helical
    }
}
