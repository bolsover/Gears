namespace Bolsover.Gears.Model;

/// <summary>
/// Resolves variable symbols to values within a broader scope (e.g., a GearSet),
/// optionally considering the current gear to prioritize lookups.
/// </summary>
public interface IVariableResolver
{
    /// <summary>
    /// Resolve a symbol (e.g., "z", "z1", "m", etc.) to a numeric value.
    /// </summary>
    /// <param name="symbol">The symbol text found in an expression (without brackets).</param>
    /// <param name="current">The current gear performing the calculation; may be null.</param>
    /// <returns>The resolved value if found; otherwise null.</returns>
    double? ResolveByNCalcSymbol(string symbol, Gear current);
}
