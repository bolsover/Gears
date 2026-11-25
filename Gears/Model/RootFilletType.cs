namespace Bolsover.Gears.Model
{
    /// <summary>The method used to form the root transition of an involute gear tooth.</summary>
    public enum RootFilletType
    {
        /// <summary>The standard trochoidal root transition.</summary>
        Standard,

        /// <summary>A circular dedendum fillet with a radius of 0.38 times the module.</summary>
        Radius
    }
}
