using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>One accepted definition as the manifest lists it: local ID, category, and the one hash it has (decision 0033).</summary>
public sealed record ManifestEntry(uint LocalId, uint Category, ContentHash Hash);

/// <summary>A storage policy pinned per category for a set-owned payload (decision 0034).</summary>
public sealed record PolicyPin(uint Category, StoragePolicies Policy);

/// <summary>The outcome of validation: whether the set was accepted and how many candidates were tried and rejected.</summary>
public sealed record ValidationResult(bool Accepted, uint CandidatesTried, uint CandidatesRejected);

/// <summary>
/// The record identifying a published set: the specification it came from, the pack it publishes, the
/// registry and vocabulary it pins, every accepted definition with its hash, the generator revisions and
/// storage policies used, and the validation result. Exactly one manifest exists per pack identifier
/// (decisions 0031, 0035, 0039).
/// </summary>
public sealed class SetManifest
{
    public const string Domain = "set-manifest/1";
    public const int MaxDefinitions = 1 << 20;

    private readonly byte[] _bytes;

    private SetManifest(ContentHash specificationHash, PackId pack, uint registryRevision, ContentHash registryHash, int generatorVersion, uint grammarVersion, PackId templatePack, ContentHash vocabularyHash, ManifestEntry[] definitions, string[] generatorRevisions, PolicyPin[] storagePolicies, ValidationResult validation, byte[] bytes, ContentHash hash)
    {
        SpecificationHash = specificationHash;
        Pack = pack;
        RegistryRevision = registryRevision;
        RegistryHash = registryHash;
        GeneratorVersion = generatorVersion;
        GrammarVersion = grammarVersion;
        TemplatePack = templatePack;
        VocabularyHash = vocabularyHash;
        Definitions = definitions;
        GeneratorRevisions = generatorRevisions;
        StoragePolicies = storagePolicies;
        Validation = validation;
        _bytes = bytes;
        Hash = hash;
    }

    public ContentHash SpecificationHash { get; }
    public PackId Pack { get; }
    public uint RegistryRevision { get; }
    public ContentHash RegistryHash { get; }
    public int GeneratorVersion { get; }
    public uint GrammarVersion { get; }
    public PackId TemplatePack { get; }
    public ContentHash VocabularyHash { get; }
    public IReadOnlyList<ManifestEntry> Definitions { get; }
    public IReadOnlyList<string> GeneratorRevisions { get; }
    public IReadOnlyList<PolicyPin> StoragePolicies { get; }
    public ValidationResult Validation { get; }
    public byte[] Bytes => [.. _bytes];
    public ContentHash Hash { get; }

    public static SetManifest Create(ContentHash specificationHash, PackId pack, uint registryRevision, ContentHash registryHash, int generatorVersion, uint grammarVersion, PackId templatePack, ContentHash vocabularyHash, IReadOnlyList<ManifestEntry> definitions, IReadOnlyList<string> generatorRevisions, IReadOnlyList<PolicyPin> storagePolicies, ValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(generatorRevisions);
        ArgumentNullException.ThrowIfNull(storagePolicies);
        ArgumentNullException.ThrowIfNull(validation);

        if (specificationHash.IsUnset || pack.IsUnset || registryRevision == 0 || registryHash.IsUnset || templatePack.IsUnset || vocabularyHash.IsUnset)
        {
            throw new ArgumentException("A manifest pins its specification, pack, registry, and vocabulary exactly.", nameof(specificationHash));
        }

        if (pack != PackId.FromSpecificationHash(specificationHash))
        {
            throw new ArgumentException("The pack identifier must be the leading 16 bytes of the specification hash (decision 0020).", nameof(pack));
        }

        if (generatorVersion <= 0 || grammarVersion == 0)
        {
            throw new ArgumentException("Generator and grammar versions start at 1.", nameof(generatorVersion));
        }

        if (definitions.Count > MaxDefinitions)
        {
            throw new ArgumentException($"A manifest lists at most {MaxDefinitions} definitions; received {definitions.Count}.", nameof(definitions));
        }

        for (int index = 0; index < definitions.Count; index++)
        {
            ManifestEntry entry = definitions[index];
            if (entry.LocalId != (uint)index + 1)
            {
                throw new ArgumentException($"Definitions must be listed as IDs 1..n in order; entry {index} has ID {entry.LocalId} (decision 0006).", nameof(definitions));
            }

            if (entry.Category == 0 || entry.Hash.IsUnset)
            {
                throw new ArgumentException($"Definition {entry.LocalId} needs a category and a hash.", nameof(definitions));
            }
        }

        var revisionSeen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string revision in generatorRevisions)
        {
            GeneratorRevisionIdentifier.Validate(revision);
            if (!revisionSeen.Add(revision))
            {
                throw new ArgumentException($"Generator revision '{revision}' is repeated.", nameof(generatorRevisions));
            }
        }

        var categorySeen = new HashSet<uint>();
        foreach (PolicyPin pin in storagePolicies)
        {
            bool single = pin.Policy is Registry.StoragePolicies.Regenerate or Registry.StoragePolicies.Materialize or Registry.StoragePolicies.Hybrid;
            if (pin.Category == 0 || !single || !categorySeen.Add(pin.Category))
            {
                throw new ArgumentException("Each storage-policy pin names one category once with exactly one policy (decision 0034).", nameof(storagePolicies));
            }
        }

        var writer = new CanonicalWriter(Domain);
        writer.WriteContentHash(specificationHash);
        writer.WritePackId(pack);
        writer.WriteUInt32(registryRevision);
        writer.WriteContentHash(registryHash);
        writer.WriteVarInt(generatorVersion);
        writer.WriteUInt32(grammarVersion);
        writer.WritePackId(templatePack);
        writer.WriteContentHash(vocabularyHash);
        writer.WriteCount(definitions.Count);
        foreach (ManifestEntry entry in definitions)
        {
            writer.WriteUInt32(entry.LocalId);
            writer.WriteUInt32(entry.Category);
            writer.WriteContentHash(entry.Hash);
        }

        writer.WriteCount(generatorRevisions.Count);
        foreach (string revision in generatorRevisions)
        {
            writer.WritePath(revision);
        }

        writer.WriteCount(storagePolicies.Count);
        foreach (PolicyPin pin in storagePolicies)
        {
            writer.WriteUInt32(pin.Category);
            writer.WriteUInt8((byte)pin.Policy);
        }

        writer.WriteUInt8(validation.Accepted ? (byte)1 : (byte)0);
        writer.WriteUInt32(validation.CandidatesTried);
        writer.WriteUInt32(validation.CandidatesRejected);

        return new SetManifest(specificationHash, pack, registryRevision, registryHash, generatorVersion, grammarVersion, templatePack, vocabularyHash, [.. definitions], [.. generatorRevisions], [.. storagePolicies], validation, writer.ToArray(), writer.ToContentHash());
    }

    public static SetManifest Decode(byte[] bytes)
    {
        var reader = new CanonicalReader(bytes, Domain);
        ContentHash specificationHash = reader.ReadContentHash();
        PackId pack = reader.ReadPackId();
        uint registryRevision = reader.ReadUInt32();
        ContentHash registryHash = reader.ReadContentHash();
        long generatorVersion = reader.ReadVarInt();
        uint grammarVersion = reader.ReadUInt32();
        PackId templatePack = reader.ReadPackId();
        ContentHash vocabularyHash = reader.ReadContentHash();

        int definitionCount = reader.ReadCount();
        if (definitionCount > MaxDefinitions)
        {
            throw new FormatException($"The manifest lists {definitionCount} definitions; the bound is {MaxDefinitions}.");
        }

        var definitions = new ManifestEntry[definitionCount];
        for (int index = 0; index < definitionCount; index++)
        {
            definitions[index] = new ManifestEntry(reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadContentHash());
        }

        int revisionCount = reader.ReadCount();
        if (revisionCount > CategoryDefinition.MaxGeneratorRevisions * CategoryRegistry.MaxCategories)
        {
            throw new FormatException($"The manifest lists {revisionCount} generator revisions; that exceeds every registry bound.");
        }

        var revisions = new string[revisionCount];
        for (int index = 0; index < revisionCount; index++)
        {
            revisions[index] = reader.ReadPath();
        }

        int pinCount = reader.ReadCount();
        if (pinCount > CategoryRegistry.MaxCategories)
        {
            throw new FormatException($"The manifest pins {pinCount} storage policies; the bound is {CategoryRegistry.MaxCategories}.");
        }

        var pins = new PolicyPin[pinCount];
        for (int index = 0; index < pinCount; index++)
        {
            pins[index] = new PolicyPin(reader.ReadUInt32(), (StoragePolicies)reader.ReadUInt8());
        }

        byte accepted = reader.ReadUInt8();
        if (accepted > 1)
        {
            throw new FormatException($"The validation flag must be 0 or 1; found {accepted}.");
        }

        var validation = new ValidationResult(accepted == 1, reader.ReadUInt32(), reader.ReadUInt32());

        if (!reader.IsAtEnd)
        {
            throw new FormatException("The manifest record has trailing bytes.");
        }

        if (generatorVersion is <= 0 or > int.MaxValue)
        {
            throw new FormatException($"Generator version {generatorVersion} is out of range.");
        }

        SetManifest manifest;
        try
        {
            manifest = Create(specificationHash, pack, registryRevision, registryHash, (int)generatorVersion, grammarVersion, templatePack, vocabularyHash, definitions, revisions, pins, validation);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }

        if (!manifest._bytes.AsSpan().SequenceEqual(bytes))
        {
            throw new FormatException("The manifest record is not in canonical form: re-encoding it yields different bytes.");
        }

        return manifest;
    }
}
