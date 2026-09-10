namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Composition grammar version 5: version 3's rules, unchanged, over category registry revision 4. A
/// grammar binds to a registry revision, so content generated under revision 4 needs a grammar laid out
/// over it, and this is that grammar and nothing more
/// ([decision 0060](../../../docs/decisions/0060-registry-revision-4-units-defaults-and-validation-limits.md)).
/// </summary>
public static class CompositionGrammarVersion5
{
    /// <summary>The version this grammar defines.</summary>
    public const uint Version = 5;

    public static readonly CompositionGrammar Grammar = CompositionGrammar.Create(
        CategoryRegistryRevision4.Registry,
        Version,
        CompositionGrammarVersion3.MaxDepth,
        CompositionGrammarVersion3.MaxNodes,
        CompositionGrammarVersion3.RetryBudget,
        CompositionGrammarVersion3.Grammar.Roots,
        CompositionGrammarVersion3.Grammar.Productions);
}
