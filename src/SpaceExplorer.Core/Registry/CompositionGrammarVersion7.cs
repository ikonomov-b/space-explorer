namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Composition grammar version 7, over category registry revision 6: version 6's rules over a registry
/// that has grown, and no production of its own
/// ([decision 0063](../../../docs/decisions/0063-relief-by-terrain-heightfield-registry-revision-6-and-grammar-version-7.md)
/// clause 8).
/// </summary>
/// <remarks>
/// Rung 2 adds relief and not structure: a planet still carries one region wherever
/// `admit-landable-body/1` admits one, and the anchor's height component stays pinned at zero, because a
/// heightfield is measured from the tangent plane at the reference radius and needs no second datum. What
/// changed is underneath — a planet now draws the three parameters its relief field is built from, which
/// is a registry revision's business and not a grammar's.
/// </remarks>
public static class CompositionGrammarVersion7
{
    /// <summary>The version this grammar defines.</summary>
    public const uint Version = 7;

    public static readonly CompositionGrammar Grammar = CompositionGrammar.Create(
        CategoryRegistryRevision6.Registry,
        Version,
        CompositionGrammarVersion3.MaxDepth,
        CompositionGrammarVersion3.MaxNodes,
        CompositionGrammarVersion3.RetryBudget,
        CompositionGrammarVersion6.Grammar.Roots,
        CompositionGrammarVersion6.Grammar.Productions);
}
