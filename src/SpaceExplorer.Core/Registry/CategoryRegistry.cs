using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// The category registry: one canonical record <c>category-registry/N</c> defining every primitive
/// category for revision <c>N</c>, identified by its content hash. A definition record's label carries
/// the revision it is laid out under, and pack manifests pin the revision and hash (decision 0035).
/// </summary>
public sealed class CategoryRegistry
{
    /// <summary>The most categories one revision may define.</summary>
    public const int MaxCategories = 4096;

    private readonly CategoryDefinition[] _categories;
    private readonly Lazy<(byte[] Bytes, ContentHash Hash)> _encoded;

    private CategoryRegistry(uint revision, CategoryDefinition[] categories)
    {
        Revision = revision;
        _categories = categories;
        _encoded = new Lazy<(byte[], ContentHash)>(() =>
        {
            CanonicalWriter writer = Write();
            return (writer.ToArray(), writer.ToContentHash());
        });
    }

    /// <summary>The revision this record defines; part of the domain label.</summary>
    public uint Revision { get; }

    /// <summary>The categories in ascending ID order.</summary>
    public IReadOnlyList<CategoryDefinition> Categories => _categories;

    /// <summary>The domain label of this record.</summary>
    public string Domain => DomainFor(Revision);

    /// <summary>The domain label a definition record laid out under this revision carries.</summary>
    public string DefinitionDomain => $"primitive-definition/1/registry/{Revision}";

    /// <summary>The domain label a template record laid out under this revision carries.</summary>
    public string TemplateDomain => $"primitive-template/1/registry/{Revision}";

    /// <summary>The domain label a template-vocabulary record laid out under this revision carries.</summary>
    public string VocabularyDomain => $"template-vocabulary/1/registry/{Revision}";

    /// <summary>The domain label a composition-grammar record of <paramref name="grammarVersion"/> laid out under this revision carries.</summary>
    public string GrammarDomain(uint grammarVersion) => $"composition-grammar/{grammarVersion}/registry/{Revision}";

    /// <summary>The domain label a composition-graph record laid out under this revision carries.</summary>
    public string GraphDomain => $"composition-graph/1/registry/{Revision}";

    /// <summary>The canonical bytes of this record.</summary>
    public byte[] Bytes => [.. _encoded.Value.Bytes];

    /// <summary>The content hash of this record, which pack manifests pin.</summary>
    public ContentHash Hash => _encoded.Value.Hash;

    /// <summary>The domain label for <paramref name="revision"/>.</summary>
    public static string DomainFor(uint revision) => $"category-registry/{revision}";

    /// <summary>Builds and validates a revision: IDs ascending and distinct, every referenced category present.</summary>
    /// <exception cref="ArgumentException">A rule is broken.</exception>
    public static CategoryRegistry Create(uint revision, IReadOnlyList<CategoryDefinition> categories)
    {
        ArgumentNullException.ThrowIfNull(categories);

        if (revision == 0)
        {
            throw new ArgumentException("Registry revisions start at 1.", nameof(revision));
        }

        if (categories.Count == 0 || categories.Count > MaxCategories)
        {
            throw new ArgumentException($"A registry defines 1 to {MaxCategories} categories; received {categories.Count}.", nameof(categories));
        }

        var ids = new HashSet<uint>();
        var labels = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < categories.Count; index++)
        {
            CategoryDefinition category = categories[index];
            if (index > 0 && category.Id <= categories[index - 1].Id)
            {
                throw new ArgumentException($"Categories must be in ascending ID order; '{category.Label}' ({category.Id}) is out of place.", nameof(categories));
            }

            ids.Add(category.Id);
            if (!labels.Add(category.Label))
            {
                throw new ArgumentException($"Category label '{category.Label}' is repeated.", nameof(categories));
            }
        }

        foreach (CategoryDefinition category in categories)
        {
            foreach (ParameterDescriptor parameter in category.Parameters)
            {
                if (parameter.Kind == ParameterKind.PrimitiveRef && !ids.Contains(parameter.RefCategory))
                {
                    throw new ArgumentException($"Parameter '{category.Label}/{parameter.Label}' references unknown category {parameter.RefCategory}.", nameof(categories));
                }
            }

            foreach (ConnectorKind connector in category.Connectors)
            {
                foreach (uint child in connector.ChildCategories)
                {
                    if (!ids.Contains(child))
                    {
                        throw new ArgumentException($"Connector '{category.Label}/{connector.Label}' admits unknown category {child}.", nameof(categories));
                    }
                }
            }
        }

        return new CategoryRegistry(revision, [.. categories]);
    }

    /// <summary>Decodes a record of <paramref name="expectedRevision"/>, rejecting trailing bytes and a non-canonical encoding.</summary>
    /// <exception cref="FormatException">The bytes are not a valid record of that revision.</exception>
    public static CategoryRegistry Decode(byte[] bytes, uint expectedRevision)
    {
        var reader = new CanonicalReader(bytes, DomainFor(expectedRevision));
        int count = reader.ReadCount();
        if (count > MaxCategories)
        {
            throw new FormatException($"The registry declares {count} categories; the bound is {MaxCategories}.");
        }

        var categories = new CategoryDefinition[count];
        for (int index = 0; index < count; index++)
        {
            categories[index] = CategoryDefinition.Decode(reader, expectedRevision);
        }

        if (!reader.IsAtEnd)
        {
            throw new FormatException("The registry record has trailing bytes.");
        }

        CategoryRegistry registry;
        try
        {
            registry = Create(expectedRevision, categories);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }

        if (!registry._encoded.Value.Bytes.AsSpan().SequenceEqual(bytes))
        {
            throw new FormatException("The registry record is not in canonical form: re-encoding it yields different bytes.");
        }

        return registry;
    }

    /// <summary>Finds a category by ID, or null.</summary>
    public CategoryDefinition? TryFind(uint id)
    {
        int low = 0;
        int high = _categories.Length - 1;
        while (low <= high)
        {
            int middle = low + ((high - low) / 2);
            uint candidate = _categories[middle].Id;
            if (candidate == id)
            {
                return _categories[middle];
            }

            if (candidate < id)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return null;
    }

    /// <summary>Finds a category by ID.</summary>
    /// <exception cref="KeyNotFoundException">No category has <paramref name="id"/>.</exception>
    public CategoryDefinition Find(uint id) =>
        TryFind(id) ?? throw new KeyNotFoundException($"Category registry revision {Revision} has no category {id}.");

    /// <summary>Finds a category by label, or null.</summary>
    public CategoryDefinition? TryFindByLabel(string label) => _categories.FirstOrDefault(category => category.Label == label);

    private CanonicalWriter Write()
    {
        var writer = new CanonicalWriter(Domain);
        writer.WriteCount(_categories.Length);
        foreach (CategoryDefinition category in _categories)
        {
            category.Encode(writer, Revision);
        }

        return writer;
    }
}
