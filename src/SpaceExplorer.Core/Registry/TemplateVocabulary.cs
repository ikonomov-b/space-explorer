using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// An authored pack's templates as one canonical record: the pack identifier and the templates in
/// ascending ID order. Its content hash is what a set specification pins (decisions 0006, 0031).
/// </summary>
public sealed class TemplateVocabulary
{
    public const int MaxTemplates = 4096;

    private readonly PrimitiveTemplate[] _templates;
    private readonly byte[] _bytes;

    private TemplateVocabulary(PackId pack, PrimitiveTemplate[] templates, byte[] bytes, ContentHash hash)
    {
        Pack = pack;
        _templates = templates;
        _bytes = bytes;
        Hash = hash;
    }

    public PackId Pack { get; }
    public IReadOnlyList<PrimitiveTemplate> Templates => _templates;
    public byte[] Bytes => [.. _bytes];
    public ContentHash Hash { get; }

    /// <summary>Builds and validates a vocabulary: a set pack, ascending distinct template IDs, distinct labels.</summary>
    /// <exception cref="ArgumentException">A rule is broken.</exception>
    public static TemplateVocabulary Create(CategoryRegistry registry, PackId pack, IReadOnlyList<PrimitiveTemplate> templates)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(templates);

        if (pack.IsUnset)
        {
            throw new ArgumentException("A vocabulary belongs to an authored pack.", nameof(pack));
        }

        if (templates.Count == 0 || templates.Count > MaxTemplates)
        {
            throw new ArgumentException($"A vocabulary holds 1 to {MaxTemplates} templates; received {templates.Count}.", nameof(templates));
        }

        var labels = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < templates.Count; index++)
        {
            PrimitiveTemplate template = templates[index];
            if (index > 0 && template.Id <= templates[index - 1].Id)
            {
                throw new ArgumentException($"Templates must be in ascending ID order; '{template.Label}' ({template.Id}) is out of place.", nameof(templates));
            }

            if (!labels.Add(template.Label))
            {
                throw new ArgumentException($"Template label '{template.Label}' is repeated.", nameof(templates));
            }
        }

        var writer = new CanonicalWriter(registry.VocabularyDomain);
        writer.WritePackId(pack);
        writer.WriteCount(templates.Count);
        foreach (PrimitiveTemplate template in templates)
        {
            template.EncodeInline(writer, registry);
        }

        return new TemplateVocabulary(pack, [.. templates], writer.ToArray(), writer.ToContentHash());
    }

    /// <summary>Decodes a record laid out under <paramref name="registry"/>, rejecting trailing bytes and a non-canonical encoding.</summary>
    /// <exception cref="FormatException">The bytes are not a valid vocabulary record of that registry revision.</exception>
    public static TemplateVocabulary Decode(byte[] bytes, CategoryRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        var reader = new CanonicalReader(bytes, registry.VocabularyDomain);
        PackId pack = reader.ReadPackId();
        int count = reader.ReadCount();
        if (count > MaxTemplates)
        {
            throw new FormatException($"The vocabulary declares {count} templates; the bound is {MaxTemplates}.");
        }

        var templates = new PrimitiveTemplate[count];
        for (int index = 0; index < count; index++)
        {
            templates[index] = PrimitiveTemplate.DecodeInline(reader, registry);
        }

        if (!reader.IsAtEnd)
        {
            throw new FormatException("The vocabulary record has trailing bytes.");
        }

        TemplateVocabulary vocabulary;
        try
        {
            vocabulary = Create(registry, pack, templates);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }

        if (!vocabulary._bytes.AsSpan().SequenceEqual(bytes))
        {
            throw new FormatException("The vocabulary record is not in canonical form: re-encoding it yields different bytes.");
        }

        return vocabulary;
    }

    /// <summary>Finds a template by ID, or null.</summary>
    public PrimitiveTemplate? TryFind(uint id) => _templates.FirstOrDefault(template => template.Id == id);
}
