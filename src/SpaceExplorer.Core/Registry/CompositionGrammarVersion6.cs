using SpaceExplorer.Core.Derivation;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision5;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Composition grammar version 6, over category registry revision 5: version 3's rules with one addition,
/// the surface anchor that places a region on a planet. It is rung one of the ladder of
/// [decision 0061](../../../docs/decisions/0061-surface-iterations-by-an-escalating-ladder-registry-revision-5-and-grammar-version-6.md)
/// and nothing more — a flat region wearing the material its planet already stores, with no relief, no
/// biome, no scatter, no far field, no sky, and no chunks, each of which arrives with the rung that
/// needs it.
/// </summary>
/// <remarks>
/// The anchor is gated rather than merely optional. Decision 0041 admits a surface anchor only on a body
/// whose reference radius reaches the minimum landable radius, and no grammar could state that before:
/// a band tag asks what a child suits, and this asks what the parent can carry, from a radius that is
/// derived from the mass and the density rather than declared anywhere a tag could read. The gate names
/// its rule, the rule names the region-limits record, and the grammar's own hash covers the name — so
/// what decides a region's existence is pinned by the graph that holds it.
///
/// A region is placed pole to pole on any meridian, facing any direction, and at the reference radius
/// itself: height is pinned to zero because at rung one there is no relief for it to sit above.
/// </remarks>
public static class CompositionGrammarVersion6
{
    /// <summary>The version this grammar defines.</summary>
    public const uint Version = 6;

    /// <summary>A quarter turn: the latitude of a pole, which is as far from the equator as an anchor goes.</summary>
    private const long QuarterTurn = 1L << 30;

    /// <summary>
    /// What a planet offers the level below: exactly one region wherever one fits, and none where the
    /// gate refuses. A body large enough to carry a region always carries one, so a sample's regions
    /// follow from the bodies rather than from a second draw.
    /// </summary>
    public static readonly ConnectorRule SurfaceAnchor = new(1, 1, new TransformRange(TransformKind.SurfaceAnchor,
    [
        (-QuarterTurn, QuarterTurn),
        (int.MinValue, int.MaxValue),
        (0, 0),
        (int.MinValue, int.MaxValue),
    ]),
    [new CategoryChoice(Region, 1)],
    Gate: ConnectorGates.LandableBodyRevision);

    public static readonly CompositionGrammar Grammar = CompositionGrammar.Create(
        CategoryRegistryRevision5.Registry,
        Version,
        CompositionGrammarVersion3.MaxDepth,
        CompositionGrammarVersion3.MaxNodes,
        CompositionGrammarVersion3.RetryBudget,
        CompositionGrammarVersion3.Grammar.Roots,
        [
            .. CompositionGrammarVersion3.Grammar.Productions.Select(Grown),
            new Production(Region, []),
        ]);

    /// <summary>Version 3's production, with the surface anchor appended to the planet's rules in the category's own order.</summary>
    private static Production Grown(Production production) =>
        production.Category != Planet
            ? production
            : new Production(production.Category, [.. production.ConnectorRules, SurfaceAnchor]);
}
