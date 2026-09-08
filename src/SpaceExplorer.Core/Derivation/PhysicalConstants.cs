namespace SpaceExplorer.Core.Derivation;

/// <summary>
/// Defined physical constants the derivation rules, the composition grammar, and the system description
/// share, so no two of them carry the same number written twice. Measured quantities that belong to one
/// body live with the vector that uses them; these are exact by definition.
/// </summary>
public static class PhysicalConstants
{
    /// <summary>The astronomical unit, 149,597,870,700 m, which is exact by definition.</summary>
    public const long AstronomicalUnitMetres = 149_597_870_700;

    /// <summary>Standard gravity, 9.80665 m/s^2 by definition, to the nearest mm/s^2.</summary>
    public const long StandardGravityMillimetres = 9_807;
}
