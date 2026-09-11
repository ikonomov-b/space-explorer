namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Composition grammar version 9, over category registry revision 8: version 8's rules over a registry
/// whose region category now stores its ground, and no production of its own
/// ([decision 0066](../../../docs/decisions/0066-generation-writes-to-the-database-and-the-view-only-reads.md)
/// clause 4).
/// </summary>
public static class CompositionGrammarVersion9
{
    /// <summary>The version this grammar defines.</summary>
    public const uint Version = 9;

    public static readonly CompositionGrammar Grammar = CompositionGrammar.Create(
        CategoryRegistryRevision8.Registry,
        Version,
        CompositionGrammarVersion3.MaxDepth,
        CompositionGrammarVersion3.MaxNodes,
        CompositionGrammarVersion3.RetryBudget,
        CompositionGrammarVersion8.Grammar.Roots,
        CompositionGrammarVersion8.Grammar.Productions);
}
