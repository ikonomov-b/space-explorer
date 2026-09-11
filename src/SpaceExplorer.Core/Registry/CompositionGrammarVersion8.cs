namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Composition grammar version 8, over category registry revision 7: version 7's rules over a registry that
/// has grown by one parameter, and no production of its own
/// ([decision 0065](../../../docs/decisions/0065-terrain-heightfield-version-2-ridges-and-registry-revision-7s-ridging.md)
/// clause 3).
/// </summary>
/// <remarks>
/// The same relation version 7 had to version 6: what changed is what a planet stores, which is a registry
/// revision's business, and the structure a grammar builds is untouched.
/// </remarks>
public static class CompositionGrammarVersion8
{
    /// <summary>The version this grammar defines.</summary>
    public const uint Version = 8;

    public static readonly CompositionGrammar Grammar = CompositionGrammar.Create(
        CategoryRegistryRevision7.Registry,
        Version,
        CompositionGrammarVersion3.MaxDepth,
        CompositionGrammarVersion3.MaxNodes,
        CompositionGrammarVersion3.RetryBudget,
        CompositionGrammarVersion7.Grammar.Roots,
        CompositionGrammarVersion7.Grammar.Productions);
}
