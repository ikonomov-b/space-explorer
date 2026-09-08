namespace SpaceExplorer.Core.Registry;

/// <summary>
/// The composition grammar versions a build supports, the counterpart of <see cref="CategoryRegistries"/>
/// for grammars. A graph pins the version and hash it was composed under, so an earlier version is
/// retained for as long as content composed under it must load, and a graph pinning any other version is
/// refused by name rather than reinterpreted (decisions 0035, 0048).
/// </summary>
public sealed class CompositionGrammars
{
    private readonly CompositionGrammar[] _grammars;

    private CompositionGrammars(CompositionGrammar[] grammars) => _grammars = grammars;

    /// <summary>The versions this build supports.</summary>
    public static CompositionGrammars Supported { get; } = Of(CompositionGrammarVersion1.Grammar, CompositionGrammarVersion2.Grammar, CompositionGrammarVersion3.Grammar);

    /// <summary>The supported grammars in ascending version order.</summary>
    public IReadOnlyList<CompositionGrammar> Grammars => _grammars;

    /// <summary>The supported version numbers in ascending order, as an error message lists them.</summary>
    public IEnumerable<uint> Versions => _grammars.Select(grammar => grammar.Version);

    /// <summary>Lists <paramref name="grammars"/> as one build's supported set; they need not be given in version order.</summary>
    /// <exception cref="ArgumentException">No grammar is given, or two share a version.</exception>
    public static CompositionGrammars Of(params CompositionGrammar[] grammars)
    {
        ArgumentNullException.ThrowIfNull(grammars);

        if (grammars.Length == 0)
        {
            throw new ArgumentException("A build supports at least one composition grammar.", nameof(grammars));
        }

        CompositionGrammar[] ordered = [.. grammars.OrderBy(grammar => grammar.Version)];
        for (int index = 1; index < ordered.Length; index++)
        {
            if (ordered[index].Version == ordered[index - 1].Version)
            {
                throw new ArgumentException($"Two grammars were offered for version {ordered[index].Version}; a version is immutable and has one record.", nameof(grammars));
            }
        }

        return new CompositionGrammars(ordered);
    }

    /// <summary>The supported version <paramref name="version"/>, or null.</summary>
    public CompositionGrammar? TryFind(uint version) => _grammars.FirstOrDefault(grammar => grammar.Version == version);

    /// <summary>The supported version <paramref name="version"/>.</summary>
    /// <exception cref="KeyNotFoundException">This build does not support that version.</exception>
    public CompositionGrammar Find(uint version) =>
        TryFind(version) ?? throw new KeyNotFoundException($"This build supports composition grammar versions {string.Join(", ", Versions)}, not {version}.");

    /// <summary>
    /// The grammar a new graph over category-registry revision <paramref name="registryRevision"/> is
    /// composed under: the newest version laid out over that revision, or null when none is.
    /// </summary>
    public CompositionGrammar? Newest(uint registryRevision) =>
        _grammars.LastOrDefault(grammar => grammar.RegistryRevision == registryRevision);
}
