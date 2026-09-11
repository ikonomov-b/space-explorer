namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Composition grammar version 10, over category registry revision 9: version 9's rules over a registry
/// that has grown, and no production of its own
/// ([decision 0070](../../../docs/decisions/0070-a-biome-is-a-derived-set-registry-revision-9-and-grammar-version-10.md)
/// clause 10).
/// </summary>
/// <remarks>
/// A biome element is referenced and never composed: it is a leaf a planet's palette names, as a
/// surface material is, so nothing about the shape of a graph changes. What a region gains at this rung
/// is a derived set, which decision 0038 places outside the grammar's productions entirely.
/// </remarks>
public static class CompositionGrammarVersion10
{
    /// <summary>The version this grammar defines.</summary>
    public const uint Version = 10;

    public static readonly CompositionGrammar Grammar = CompositionGrammar.Create(
        CategoryRegistryRevision9.Registry,
        Version,
        CompositionGrammarVersion3.MaxDepth,
        CompositionGrammarVersion3.MaxNodes,
        CompositionGrammarVersion3.RetryBudget,
        CompositionGrammarVersion9.Grammar.Roots,
        CompositionGrammarVersion9.Grammar.Productions);
}
