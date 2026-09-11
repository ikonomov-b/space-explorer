namespace SpaceExplorer.Core.Registry;

/// <summary>
/// The category-registry revisions a build supports. Content pins one revision and its hash; a loader
/// selects that revision from this list and refuses any other with a compatibility error naming the
/// revision, never by substituting a schema (decision 0035).
/// </summary>
public sealed class CategoryRegistries
{
    private readonly CategoryRegistry[] _registries;

    private CategoryRegistries(CategoryRegistry[] registries) => _registries = registries;

    /// <summary>The revisions this build supports. An earlier one is retained so the content published under it still loads (decision 0035).</summary>
    public static CategoryRegistries Supported { get; } = Of(CategoryRegistryRevision1.Registry, CategoryRegistryRevision2.Registry, CategoryRegistryRevision3.Registry, CategoryRegistryRevision4.Registry, CategoryRegistryRevision5.Registry, CategoryRegistryRevision6.Registry, CategoryRegistryRevision7.Registry, CategoryRegistryRevision8.Registry);

    /// <summary>The supported revisions in ascending revision order.</summary>
    public IReadOnlyList<CategoryRegistry> Registries => _registries;

    /// <summary>The supported revision numbers in ascending order, as an error message or a handshake lists them (decision 0035).</summary>
    public IEnumerable<uint> Revisions => _registries.Select(registry => registry.Revision);

    /// <summary>Lists <paramref name="registries"/> as one build's supported set; they need not be given in revision order.</summary>
    /// <exception cref="ArgumentException">No registry is given, or two share a revision.</exception>
    public static CategoryRegistries Of(params CategoryRegistry[] registries)
    {
        ArgumentNullException.ThrowIfNull(registries);

        if (registries.Length == 0)
        {
            throw new ArgumentException("A build supports at least one category-registry revision.", nameof(registries));
        }

        CategoryRegistry[] ordered = [.. registries.OrderBy(registry => registry.Revision)];
        for (int index = 1; index < ordered.Length; index++)
        {
            if (ordered[index].Revision == ordered[index - 1].Revision)
            {
                throw new ArgumentException($"Two registries were offered for revision {ordered[index].Revision}; a revision is immutable and has one record (decision 0035).", nameof(registries));
            }
        }

        return new CategoryRegistries(ordered);
    }

    /// <summary>The supported revision <paramref name="revision"/>, or null when this build does not support it.</summary>
    public CategoryRegistry? TryFind(uint revision) => _registries.FirstOrDefault(registry => registry.Revision == revision);

    /// <summary>The supported revision <paramref name="revision"/>.</summary>
    /// <exception cref="KeyNotFoundException">This build does not support that revision.</exception>
    public CategoryRegistry Find(uint revision) =>
        TryFind(revision) ?? throw new KeyNotFoundException($"This build supports category-registry revisions {string.Join(", ", Revisions)}, not {revision}.");
}
