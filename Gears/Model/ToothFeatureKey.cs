namespace Bolsover.Gears.Model
{
    /// <summary>
    /// Enumerates well-known keys for features inside a ToothProfile.
    /// Using enums avoids stringly-typed keys and enables compile-time safety.
    /// </summary>
    public enum ToothFeatureKey
    {
        // Reference circles
        RootCircle,
        BaseCircle,
        PitchCircle,
        OutsideCircle,

        // Placeholder flanks
        ToothFlank,
        ToothFlankMirror,

        // Additional keys that may be used elsewhere/tests (keep strings support too)
        CentreToRootRhs,
        CentreToRootLhs,
        RootRhsToRelief,
        RootLhsToRelief,
        RhsRootRelief,
        LhsRootRelief,
        RhsReliefToBase,
        LhsReliefToBase,
        RhsInvolute,
        LhsInvolute,
        RhsTipRelief,
        LhsTipRelief,
        Tip
    }
}
