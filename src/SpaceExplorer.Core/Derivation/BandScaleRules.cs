using SpaceExplorer.Core.Registry;

namespace SpaceExplorer.Core.Derivation;

/// <summary>
/// The derivation rules a composition grammar may name to scale a connector's orbit bands by something
/// about the parent instance. A grammar carries the identifier, this resolves it, and the rule itself
/// knows which of the parent's parameters it reads, so the composition generator stays free of any
/// knowledge of stars or planets (decisions 0031, 0037).
/// </summary>
public static class BandScaleRules
{
    /// <summary>Whether <paramref name="revision"/> names a band-scale rule this build retains (decision 0035).</summary>
    public static bool IsKnown(string revision) => revision == DerivationRules.OrbitScaleRevision;

    /// <summary>
    /// The scale <paramref name="revision"/> gives the bands of a connector on <paramref name="parent"/>,
    /// in units of <see cref="DerivationRules.OrbitScaleUnit"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The identifier names no rule this build retains, or the parent lacks what the rule reads.</exception>
    public static long Scale(string revision, PrimitiveDefinition parent, CategoryRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(registry);

        if (revision != DerivationRules.OrbitScaleRevision)
        {
            throw new ArgumentException($"'{revision}' names no band-scale rule this build retains.", nameof(revision));
        }

        long radius = Required(parent, registry, "radius");
        long temperature = Required(parent, registry, "effective-temperature");
        return DerivationRules.OrbitScale(radius, temperature);
    }

    private static long Required(PrimitiveDefinition parent, CategoryRegistry registry, string label) =>
        parent.TryParameter(registry, label)?.Value.AsInteger
            ?? throw new ArgumentException($"Category '{registry.Find(parent.Category).Label}' has no parameter '{label}', which {DerivationRules.OrbitScaleRevision} reads.", nameof(parent));
}
