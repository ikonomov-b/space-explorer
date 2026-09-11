using SpaceExplorer.Core.Description;
using SpaceExplorer.Core.Registry;

namespace SpaceExplorer.Core.Derivation;

/// <summary>
/// The rules a composition grammar may name to decide whether a connector is offered on a given parent
/// at all. A grammar carries the identifier, this resolves it, and the rule itself knows which of the
/// parent's quantities it reads, so the composition generator stays free of any knowledge of planets or
/// regions — the same division <see cref="BandScaleRules"/> keeps for orbit bands (decisions 0031, 0035).
/// </summary>
/// <remarks>
/// A gate is not a tag demand, though it may read a tag. A tag demand asks what a child suits; a gate asks
/// what a parent can carry, and part of what decides that is derived rather than declared — a body's
/// radius comes from its mass and its density and appears in no template. That is why
/// [decision 0041](../../../docs/decisions/0041-planet-fields-tangent-regions-minimum-radius-and-far-field.md)'s
/// clause that a surface anchor belongs only on a body of at least the minimum landable radius could not
/// be stated by any grammar before version 6
/// ([decision 0061](../../../docs/decisions/0061-surface-iterations-by-an-escalating-ladder-registry-revision-5-and-grammar-version-6.md)).
///
/// Size is not the whole of it, which the first surface cycle showed at once: every body the `basic`
/// vocabulary draws clears the minimum radius, gas giants most of all, so a radius-only gate gave a
/// 96,000 km ball of hydrogen a patch of walkable ground. What a body is made of decides that, and it is
/// a content judgement, so it is declared where the content is — a definition tag the vocabulary grants
/// to solid bodies and withholds from gas giants — rather than written into this rule as a list of body
/// types. The rule reads the tag; the vocabulary decides who carries it.
///
/// The rule names the record it reads rather than restating its number: <see cref="RegionLimits.Version1"/>
/// is the pinned, versioned, hashed record of [decision 0059](../../../docs/decisions/0059-region-limits-are-a-pinned-record.md),
/// and a change to the limits it reads is a new rule revision, which is a new grammar version, which is
/// a new graph — the chain [decision 0049](../../../docs/decisions/0049-suit-profile-and-system-description-version-1-and-derivation-rules-as-named-implementations.md)
/// established for derivation rules as named implementations.
/// </remarks>
public static class ConnectorGates
{
    /// <summary>
    /// Admits the connector only on a body that is large enough to carry a region and made of something
    /// to stand on: the 524,288 m of decision 0041, read from the region-limits record and not from a
    /// constant, and the <see cref="SolidSurfaceTag"/> its own definition declares.
    /// </summary>
    public const string LandableBodyRevision = "admit-landable-body/1";

    /// <summary>
    /// The tag a body's definition declares to say it has ground: what the vocabulary grants a rocky, icy
    /// or ocean body and withholds from a gas giant. Definition tags arrive with registry revision 3
    /// ([decision 0055](../../../docs/decisions/0055-definition-level-tags-and-tagged-references.md)), so
    /// a body published under an earlier revision carries none and no gate admits it.
    /// </summary>
    public const string SolidSurfaceTag = "solid-surface";

    /// <summary>Whether <paramref name="revision"/> names a connector gate this build retains (decision 0035).</summary>
    public static bool IsKnown(string revision) => revision == LandableBodyRevision;

    /// <summary>Whether the gate <paramref name="revision"/> names offers its connector on <paramref name="parent"/>.</summary>
    /// <exception cref="ArgumentException">The identifier names no gate this build retains, or the parent lacks what the gate reads.</exception>
    public static bool Admits(string revision, PrimitiveDefinition parent, CategoryRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(registry);

        if (revision != LandableBodyRevision)
        {
            throw new ArgumentException($"'{revision}' names no connector gate this build retains.", nameof(revision));
        }

        if (!parent.Tags.Contains(SolidSurfaceTag, StringComparer.Ordinal))
        {
            return false;
        }

        long mass = Required(parent, registry, "mass");
        long density = Required(parent, registry, "density");
        return RegionLimits.Version1.AdmitsRegion(DerivationRules.Radius(mass, density));
    }

    private static long Required(PrimitiveDefinition parent, CategoryRegistry registry, string label) =>
        parent.TryParameter(registry, label)?.Value.AsInteger
            ?? throw new ArgumentException($"Category '{registry.Find(parent.Category).Label}' has no parameter '{label}', which {LandableBodyRevision} reads.", nameof(parent));
}
