namespace SpaceExplorer.Core.Registry;

/// <summary>
/// Composition grammar version 4: version 3's rules, unchanged, over category registry revision 3. A
/// grammar binds to a registry revision — <see cref="CompositionGrammars.Newest"/> selects by it and a
/// graph pins the version and hash it was composed under — so content generated under revision 3 needs a
/// grammar laid out over revision 3, and this is that grammar and nothing more
/// ([decision 0055](../../../docs/decisions/0055-definition-level-tags-and-tagged-references.md)).
/// </summary>
public static class CompositionGrammarVersion4
{
    /// <summary>The version this grammar defines.</summary>
    public const uint Version = 4;

    public static readonly CompositionGrammar Grammar = CompositionGrammar.Create(
        CategoryRegistryRevision3.Registry,
        Version,
        CompositionGrammarVersion3.MaxDepth,
        CompositionGrammarVersion3.MaxNodes,
        CompositionGrammarVersion3.RetryBudget,
        CompositionGrammarVersion3.Grammar.Roots,
        CompositionGrammarVersion3.Grammar.Productions);
}
