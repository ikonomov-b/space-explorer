using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// One exact primitive definition: category, provenance, parameter values in the category's schema
/// order, a declaration per connector kind, and the generator revision that produces it, if any. Its
/// content hash is the hash of its canonical record, whose label carries the registry revision it is
/// laid out under (decisions 0031, 0033, 0035). The record carries no identity: the manifest binds a
/// local ID to a hash, so identical content is one file however many packs hold it (decision 0018).
/// </summary>
public sealed class PrimitiveDefinition
{
    private PrimitiveDefinition(
        PrimitiveId id,
        uint category,
        Provenance provenance,
        ParameterValue[] parameters,
        ConnectorDeclaration[] connectors,
        string generatorRevision,
        byte[] bytes,
        ContentHash hash)
    {
        Id = id;
        Category = category;
        Provenance = provenance;
        Parameters = parameters;
        Connectors = connectors;
        GeneratorRevision = generatorRevision;
        _bytes = bytes;
        Hash = hash;
    }

    private readonly byte[] _bytes;

    public PrimitiveId Id { get; }
    public uint Category { get; }
    public Provenance Provenance { get; }
    public IReadOnlyList<ParameterValue> Parameters { get; }
    public IReadOnlyList<ConnectorDeclaration> Connectors { get; }

    /// <summary>The generator revision identifier of a regenerate or hybrid recipe, or empty for none (decision 0035).</summary>
    public string GeneratorRevision { get; }

    /// <summary>The canonical record.</summary>
    public byte[] Bytes => [.. _bytes];

    /// <summary>The content hash of the canonical record: this definition's one revision (decision 0033).</summary>
    public ContentHash Hash { get; }

    /// <summary>The exact reference to this definition.</summary>
    public PrimitiveRevisionRef Reference => new(Id, Hash);

    /// <summary>The exact primitive references among the parameters, which are this definition's primitive dependencies.</summary>
    public IEnumerable<PrimitiveRevisionRef> Dependencies =>
        Parameters.Where(parameter => parameter.Kind == ParameterKind.PrimitiveRef).Select(parameter => parameter.AsRef);

    /// <summary>Builds and validates a definition against <paramref name="registry"/>, encoding and hashing it once.</summary>
    /// <exception cref="ArgumentException">A value does not fit its category's schema.</exception>
    public static PrimitiveDefinition Create(
        CategoryRegistry registry,
        PrimitiveId id,
        uint category,
        Provenance provenance,
        IReadOnlyList<ParameterValue> parameters,
        IReadOnlyList<ConnectorDeclaration> connectors,
        string generatorRevision)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(provenance);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(connectors);
        ArgumentNullException.ThrowIfNull(generatorRevision);

        if (id.IsUnset)
        {
            throw new ArgumentException("A definition needs a set pack and a non-zero local ID.", nameof(id));
        }

        CategoryDefinition schema = registry.TryFind(category)
            ?? throw new ArgumentException($"Registry revision {registry.Revision} has no category {category}.", nameof(category));

        if (parameters.Count != schema.Parameters.Count)
        {
            throw new ArgumentException($"Category '{schema.Label}' has {schema.Parameters.Count} parameters; received {parameters.Count}.", nameof(parameters));
        }

        for (int index = 0; index < parameters.Count; index++)
        {
            parameters[index].Validate(schema.Parameters[index]);
        }

        if (connectors.Count != schema.Connectors.Count)
        {
            throw new ArgumentException($"Category '{schema.Label}' has {schema.Connectors.Count} connector kinds; received {connectors.Count} declarations.", nameof(connectors));
        }

        if (generatorRevision.Length != 0 && !schema.GeneratorRevisions.Contains(generatorRevision))
        {
            throw new ArgumentException($"Category '{schema.Label}' does not list generator revision '{generatorRevision}'.", nameof(generatorRevision));
        }

        var writer = new CanonicalWriter(registry.DefinitionDomain);
        writer.WriteUInt32(category);
        provenance.Encode(writer);
        for (int index = 0; index < parameters.Count; index++)
        {
            parameters[index].Encode(writer, schema.Parameters[index]);
        }

        foreach (ConnectorDeclaration connector in connectors)
        {
            connector.Encode(writer);
        }

        writer.WriteText(generatorRevision);

        return new PrimitiveDefinition(id, category, provenance, [.. parameters], [.. connectors], generatorRevision, writer.ToArray(), writer.ToContentHash());
    }

    /// <summary>Decodes a record laid out under <paramref name="registry"/> as the definition the manifest lists under <paramref name="id"/>, rejecting trailing bytes and a non-canonical encoding.</summary>
    /// <exception cref="FormatException">The bytes are not a valid definition record of that registry revision.</exception>
    public static PrimitiveDefinition Decode(byte[] bytes, CategoryRegistry registry, PrimitiveId id)
    {
        ArgumentNullException.ThrowIfNull(registry);
        var reader = new CanonicalReader(bytes, registry.DefinitionDomain);

        uint category = reader.ReadUInt32();
        CategoryDefinition schema = registry.TryFind(category)
            ?? throw new FormatException($"Registry revision {registry.Revision} has no category {category}.");

        Provenance provenance = Provenance.Decode(reader);

        var parameters = new ParameterValue[schema.Parameters.Count];
        for (int index = 0; index < parameters.Length; index++)
        {
            parameters[index] = ParameterValue.Decode(reader, schema.Parameters[index]);
        }

        var connectors = new ConnectorDeclaration[schema.Connectors.Count];
        for (int index = 0; index < connectors.Length; index++)
        {
            connectors[index] = ConnectorDeclaration.Decode(reader);
        }

        string generatorRevision = reader.ReadText();

        if (!reader.IsAtEnd)
        {
            throw new FormatException("The definition record has trailing bytes.");
        }

        PrimitiveDefinition definition;
        try
        {
            definition = Create(registry, id, category, provenance, parameters, connectors, generatorRevision);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }

        if (!definition._bytes.AsSpan().SequenceEqual(bytes))
        {
            throw new FormatException("The definition record is not in canonical form: re-encoding it yields different bytes.");
        }

        return definition;
    }
}
