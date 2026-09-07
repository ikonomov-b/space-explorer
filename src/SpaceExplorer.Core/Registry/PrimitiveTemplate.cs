using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// An authored template: a category, a bounded range per parameter, and a declaration per connector
/// kind, from which the generator draws definitions. Its content hash is its exact revision; a
/// compatible change keeps its ID and takes a new hash (decisions 0031, 0033).
/// </summary>
public sealed class PrimitiveTemplate
{
    private readonly byte[] _bytes;

    private PrimitiveTemplate(uint id, string label, uint category, ParameterRange[] ranges, ConnectorDeclaration[] connectors, byte[] bytes, ContentHash hash)
    {
        Id = id;
        Label = label;
        Category = category;
        Ranges = ranges;
        Connectors = connectors;
        _bytes = bytes;
        Hash = hash;
    }

    public uint Id { get; }
    public string Label { get; }
    public uint Category { get; }
    public IReadOnlyList<ParameterRange> Ranges { get; }
    public IReadOnlyList<ConnectorDeclaration> Connectors { get; }
    public ContentHash Hash { get; }

    /// <summary>Builds and validates a template against <paramref name="registry"/>.</summary>
    /// <exception cref="ArgumentException">A range does not fit its category's schema.</exception>
    public static PrimitiveTemplate Create(CategoryRegistry registry, uint id, string label, uint category, IReadOnlyList<ParameterRange> ranges, IReadOnlyList<ConnectorDeclaration> connectors)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(ranges);
        ArgumentNullException.ThrowIfNull(connectors);
        StreamPath.Validate(label);

        if (id == 0)
        {
            throw new ArgumentException("Template IDs start at 1; zero is reserved.", nameof(id));
        }

        CategoryDefinition schema = registry.TryFind(category)
            ?? throw new ArgumentException($"Registry revision {registry.Revision} has no category {category}.", nameof(category));

        if (ranges.Count != schema.Parameters.Count)
        {
            throw new ArgumentException($"Category '{schema.Label}' has {schema.Parameters.Count} parameters; template '{label}' ranges {ranges.Count}.", nameof(ranges));
        }

        for (int index = 0; index < ranges.Count; index++)
        {
            ranges[index].Validate(schema.Parameters[index]);
        }

        if (connectors.Count != schema.Connectors.Count)
        {
            throw new ArgumentException($"Category '{schema.Label}' has {schema.Connectors.Count} connector kinds; template '{label}' declares {connectors.Count}.", nameof(connectors));
        }

        var writer = new CanonicalWriter(registry.TemplateDomain);
        Encode(writer, id, label, category, ranges, connectors, schema);
        return new PrimitiveTemplate(id, label, category, [.. ranges], [.. connectors], writer.ToArray(), writer.ToContentHash());
    }

    private static void Encode(CanonicalWriter writer, uint id, string label, uint category, IReadOnlyList<ParameterRange> ranges, IReadOnlyList<ConnectorDeclaration> connectors, CategoryDefinition schema)
    {
        writer.WriteUInt32(id);
        writer.WriteText(label);
        writer.WriteUInt32(category);
        for (int index = 0; index < ranges.Count; index++)
        {
            ranges[index].Encode(writer, schema.Parameters[index]);
        }

        foreach (ConnectorDeclaration connector in connectors)
        {
            connector.Encode(writer);
        }
    }

    /// <summary>Writes this template's fields, without a header, into an enclosing vocabulary record.</summary>
    internal void EncodeInline(CanonicalWriter writer, CategoryRegistry registry) =>
        Encode(writer, Id, Label, Category, Ranges, Connectors, registry.Find(Category));

    /// <summary>Reads the fields <see cref="EncodeInline"/> wrote and rebuilds the template, recomputing its hash.</summary>
    internal static PrimitiveTemplate DecodeInline(CanonicalReader reader, CategoryRegistry registry)
    {
        uint id = reader.ReadUInt32();
        string label = reader.ReadText();
        uint category = reader.ReadUInt32();
        CategoryDefinition schema = registry.TryFind(category)
            ?? throw new FormatException($"Registry revision {registry.Revision} has no category {category}.");

        var ranges = new ParameterRange[schema.Parameters.Count];
        for (int index = 0; index < ranges.Length; index++)
        {
            ranges[index] = ParameterRange.Decode(reader, schema.Parameters[index]);
        }

        var connectors = new ConnectorDeclaration[schema.Connectors.Count];
        for (int index = 0; index < connectors.Length; index++)
        {
            connectors[index] = ConnectorDeclaration.Decode(reader);
        }

        try
        {
            return Create(registry, id, label, category, ranges, connectors);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }
    }
}
