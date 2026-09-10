using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Description;

/// <summary>
/// What the two destination levers ask for, with everything that decides the answer: the tier, the lever
/// seed, and the pins that fix which system satisfies that tier — the generator, the category registry,
/// the grammar, the stored set, the description version, and the suit profile whose limits decide whether
/// a body can be landed on. Its content hash names the destination's pack, so the pack identifier follows
/// from the lever values and the build's own pins alone: a stored destination is found by addressing it,
/// not by searching for it ([decision 0053](../../../docs/decisions/0053-a-destination-is-a-stored-record-addressed-by-its-levers.md)).
/// </summary>
public sealed class DestinationSpecification
{
    public const string Domain = "destination-specification/1";

    private readonly byte[] _bytes;

    private DestinationSpecification(
        DistanceTier tier,
        ulong seed,
        int generatorVersion,
        uint registryRevision,
        ContentHash registryHash,
        uint grammarVersion,
        ContentHash grammarHash,
        PackId sourcePack,
        ContentHash sourceManifestHash,
        uint descriptionVersion,
        uint suitVersion,
        ContentHash suitHash,
        byte[] bytes,
        ContentHash hash)
    {
        Tier = tier;
        Seed = seed;
        GeneratorVersion = generatorVersion;
        RegistryRevision = registryRevision;
        RegistryHash = registryHash;
        GrammarVersion = grammarVersion;
        GrammarHash = grammarHash;
        SourcePack = sourcePack;
        SourceManifestHash = sourceManifestHash;
        DescriptionVersion = descriptionVersion;
        SuitVersion = suitVersion;
        SuitHash = suitHash;
        _bytes = bytes;
        Hash = hash;
    }

    /// <summary>The distance tier lever (decision 0043).</summary>
    public DistanceTier Tier { get; }

    /// <summary>The seed lever: the value a player holds, not the seed a system is composed on (decision 0052).</summary>
    public ulong Seed { get; }

    public int GeneratorVersion { get; }
    public uint RegistryRevision { get; }
    public ContentHash RegistryHash { get; }
    public uint GrammarVersion { get; }
    public ContentHash GrammarHash { get; }

    /// <summary>The published set every instance draws its definition from.</summary>
    public PackId SourcePack { get; }

    public ContentHash SourceManifestHash { get; }

    /// <summary>The description version the verdict was read under (decision 0045).</summary>
    public uint DescriptionVersion { get; }

    public uint SuitVersion { get; }

    /// <summary>The suit profile's hash: its limits decide the landing candidates a tier's rules count (decision 0049).</summary>
    public ContentHash SuitHash { get; }

    public byte[] Bytes => [.. _bytes];
    public ContentHash Hash { get; }

    /// <summary>The identifier of the pack this specification publishes: the leading 16 bytes of its hash (decision 0020).</summary>
    public PackId PackId => PackId.FromSpecificationHash(Hash);

    /// <summary>The specification the two levers name over <paramref name="set"/>, <paramref name="grammar"/>, <paramref name="registry"/>, and <paramref name="suit"/>.</summary>
    public static DestinationSpecification For(DistanceTier tier, ulong seed, PrimitiveSet set, CompositionGrammar grammar, CategoryRegistry registry, SuitProfile suit)
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(suit);

        return Create(
            tier,
            seed,
            Shared.GeneratorVersion.Current,
            registry.Revision,
            registry.Hash,
            grammar.Version,
            grammar.Hash,
            set.Manifest.Pack,
            set.Manifest.Hash,
            SystemDescription.Version,
            suit.Version,
            suit.Hash);
    }

    public static DestinationSpecification Create(
        DistanceTier tier,
        ulong seed,
        int generatorVersion,
        uint registryRevision,
        ContentHash registryHash,
        uint grammarVersion,
        ContentHash grammarHash,
        PackId sourcePack,
        ContentHash sourceManifestHash,
        uint descriptionVersion,
        uint suitVersion,
        ContentHash suitHash)
    {
        if (!Enum.IsDefined(tier))
        {
            throw new ArgumentException($"Unknown distance tier {(byte)tier}.", nameof(tier));
        }

        if (generatorVersion <= 0 || registryRevision == 0 || registryHash.IsUnset || grammarVersion == 0 || grammarHash.IsUnset)
        {
            throw new ArgumentException("A destination pins a generator version, a category registry, and an exact grammar.", nameof(generatorVersion));
        }

        if (sourcePack.IsUnset || sourceManifestHash.IsUnset)
        {
            throw new ArgumentException("A destination pins an exact stored set: pack and manifest hash.", nameof(sourcePack));
        }

        if (descriptionVersion == 0 || suitVersion == 0 || suitHash.IsUnset)
        {
            throw new ArgumentException("A destination pins the description version and the suit profile its verdict was read under.", nameof(descriptionVersion));
        }

        var writer = new CanonicalWriter(Domain);
        Encode(writer, tier, seed, generatorVersion, registryRevision, registryHash, grammarVersion, grammarHash, sourcePack, sourceManifestHash, descriptionVersion, suitVersion, suitHash);

        return new DestinationSpecification(
            tier, seed, generatorVersion, registryRevision, registryHash, grammarVersion, grammarHash, sourcePack, sourceManifestHash, descriptionVersion, suitVersion, suitHash, writer.ToArray(), writer.ToContentHash());
    }

    /// <summary>Writes the fields without a header, into an enclosing destination record.</summary>
    internal void EncodeInline(CanonicalWriter writer) =>
        Encode(writer, Tier, Seed, GeneratorVersion, RegistryRevision, RegistryHash, GrammarVersion, GrammarHash, SourcePack, SourceManifestHash, DescriptionVersion, SuitVersion, SuitHash);

    /// <summary>Reads the fields <see cref="EncodeInline"/> wrote and rebuilds the specification, recomputing its hash.</summary>
    internal static DestinationSpecification Read(CanonicalReader reader)
    {
        var tier = (DistanceTier)reader.ReadUInt8();
        ulong seed = reader.ReadUInt64();
        long generatorVersion = reader.ReadVarInt();
        uint registryRevision = reader.ReadUInt32();
        ContentHash registryHash = reader.ReadContentHash();
        uint grammarVersion = reader.ReadUInt32();
        ContentHash grammarHash = reader.ReadContentHash();
        PackId sourcePack = reader.ReadPackId();
        ContentHash sourceManifestHash = reader.ReadContentHash();
        uint descriptionVersion = reader.ReadUInt32();
        uint suitVersion = reader.ReadUInt32();
        ContentHash suitHash = reader.ReadContentHash();

        if (generatorVersion is <= 0 or > int.MaxValue)
        {
            throw new FormatException($"Generator version {generatorVersion} is out of range.");
        }

        try
        {
            return Create(tier, seed, (int)generatorVersion, registryRevision, registryHash, grammarVersion, grammarHash, sourcePack, sourceManifestHash, descriptionVersion, suitVersion, suitHash);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }
    }

    private static void Encode(
        CanonicalWriter writer,
        DistanceTier tier,
        ulong seed,
        int generatorVersion,
        uint registryRevision,
        ContentHash registryHash,
        uint grammarVersion,
        ContentHash grammarHash,
        PackId sourcePack,
        ContentHash sourceManifestHash,
        uint descriptionVersion,
        uint suitVersion,
        ContentHash suitHash)
    {
        writer.WriteUInt8((byte)tier);
        writer.WriteUInt64(seed);
        writer.WriteVarInt(generatorVersion);
        writer.WriteUInt32(registryRevision);
        writer.WriteContentHash(registryHash);
        writer.WriteUInt32(grammarVersion);
        writer.WriteContentHash(grammarHash);
        writer.WritePackId(sourcePack);
        writer.WriteContentHash(sourceManifestHash);
        writer.WriteUInt32(descriptionVersion);
        writer.WriteUInt32(suitVersion);
        writer.WriteContentHash(suitHash);
    }
}

/// <summary>
/// One stored destination: the specification the levers name, laid out inline so the record reproduces
/// from its own bytes, and the result of the bounded retry beside it — the attempt that satisfied the
/// tier, the seed that attempt composed on, and the graph it produced. The graph is named rather than
/// repeated: a system is stored once, as the graph record of
/// [decision 0048](../../../docs/decisions/0048-composition-grammar-version-1-graph-records-and-graph-publication.md),
/// and two lever pairs that draw the same system name one graph.
/// </summary>
public sealed class DestinationRecord
{
    public const string Domain = "destination/1";

    private readonly byte[] _bytes;

    private DestinationRecord(DestinationSpecification specification, uint attempt, ulong compositionSeed, PackId graphPack, ContentHash graphHash, byte[] bytes, ContentHash hash)
    {
        Specification = specification;
        Attempt = attempt;
        CompositionSeed = compositionSeed;
        GraphPack = graphPack;
        GraphHash = graphHash;
        _bytes = bytes;
        Hash = hash;
    }

    public DestinationSpecification Specification { get; }

    /// <summary>Which attempt of the bounded retry satisfied the tier, counted from zero (decision 0052).</summary>
    public uint Attempt { get; }

    /// <summary>The seed that attempt composed on, derived from the levers by the frozen derivation.</summary>
    public ulong CompositionSeed { get; }

    public PackId GraphPack { get; }
    public ContentHash GraphHash { get; }

    /// <summary>The pack this record publishes, which the levers name without composing anything.</summary>
    public PackId Pack => Specification.PackId;

    public byte[] Bytes => [.. _bytes];
    public ContentHash Hash { get; }

    public static DestinationRecord Create(DestinationSpecification specification, uint attempt, ulong compositionSeed, PackId graphPack, ContentHash graphHash)
    {
        ArgumentNullException.ThrowIfNull(specification);

        if (attempt >= DestinationComposer.MaxAttempts)
        {
            throw new ArgumentException($"Attempt {attempt} is beyond the {DestinationComposer.MaxAttempts} a destination may draw.", nameof(attempt));
        }

        if (compositionSeed != DestinationComposer.CompositionSeed(specification.Tier, specification.Seed, attempt))
        {
            throw new ArgumentException("The composition seed is not the one the levers derive for that attempt (decisions 0021, 0052).", nameof(compositionSeed));
        }

        if (graphPack.IsUnset || graphHash.IsUnset)
        {
            throw new ArgumentException("A destination names the graph it composed: pack and graph hash.", nameof(graphPack));
        }

        var writer = new CanonicalWriter(Domain);
        specification.EncodeInline(writer);
        writer.WriteUInt32(attempt);
        writer.WriteUInt64(compositionSeed);
        writer.WritePackId(graphPack);
        writer.WriteContentHash(graphHash);

        return new DestinationRecord(specification, attempt, compositionSeed, graphPack, graphHash, writer.ToArray(), writer.ToContentHash());
    }

    /// <summary>The record for a destination just composed, over the set, grammar, registry, and suit it was composed under.</summary>
    public static DestinationRecord Of(Destination destination, PrimitiveSet set, CompositionGrammar grammar, CategoryRegistry registry, SuitProfile suit)
    {
        ArgumentNullException.ThrowIfNull(destination);

        return Create(
            DestinationSpecification.For(destination.Tier, destination.Seed, set, grammar, registry, suit),
            destination.Attempt,
            destination.CompositionSeed,
            destination.Graph.Pack,
            destination.Graph.Hash);
    }

    /// <summary>Decodes a record, rejecting trailing bytes and a non-canonical encoding.</summary>
    /// <exception cref="FormatException">The bytes are not a valid destination record.</exception>
    public static DestinationRecord Decode(byte[] bytes)
    {
        var reader = new CanonicalReader(bytes, Domain);
        DestinationSpecification specification = DestinationSpecification.Read(reader);
        uint attempt = reader.ReadUInt32();
        ulong compositionSeed = reader.ReadUInt64();
        PackId graphPack = reader.ReadPackId();
        ContentHash graphHash = reader.ReadContentHash();

        if (!reader.IsAtEnd)
        {
            throw new FormatException("The destination record has trailing bytes.");
        }

        DestinationRecord record;
        try
        {
            record = Create(specification, attempt, compositionSeed, graphPack, graphHash);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }

        if (!record._bytes.AsSpan().SequenceEqual(bytes))
        {
            throw new FormatException("The destination record is not in canonical form: re-encoding it yields different bytes.");
        }

        return record;
    }
}
