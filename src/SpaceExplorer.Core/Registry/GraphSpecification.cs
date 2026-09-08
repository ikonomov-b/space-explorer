using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// The complete input of one composition run: the registry revision and hash, the generator version, the
/// grammar version and hash, the seed, the exact stored set to compose from, and the composition domain.
/// Its content hash identifies the run, and the graph's pack identifier is the leading 16 bytes of that
/// hash, exactly as a generated set's is (decisions 0006, 0020, 0031, 0035).
/// </summary>
public sealed class GraphSpecification
{
    public const string Domain = "composition-graph-specification/1";

    private readonly byte[] _bytes;

    private GraphSpecification(uint registryRevision, ContentHash registryHash, int generatorVersion, uint grammarVersion, ContentHash grammarHash, ulong seed, PackId sourcePack, ContentHash sourceManifestHash, CompositionDomain domain, byte[] bytes, ContentHash hash)
    {
        RegistryRevision = registryRevision;
        RegistryHash = registryHash;
        GeneratorVersion = generatorVersion;
        GrammarVersion = grammarVersion;
        GrammarHash = grammarHash;
        Seed = seed;
        SourcePack = sourcePack;
        SourceManifestHash = sourceManifestHash;
        CompositionDomain = domain;
        _bytes = bytes;
        Hash = hash;
    }

    public uint RegistryRevision { get; }
    public ContentHash RegistryHash { get; }
    public int GeneratorVersion { get; }
    public uint GrammarVersion { get; }
    public ContentHash GrammarHash { get; }
    public ulong Seed { get; }

    /// <summary>The published set every instance draws its definition from.</summary>
    public PackId SourcePack { get; }

    /// <summary>The manifest hash of that set, so the composition pins an exact catalogue, not a pack identifier alone.</summary>
    public ContentHash SourceManifestHash { get; }

    public CompositionDomain CompositionDomain { get; }
    public byte[] Bytes => [.. _bytes];
    public ContentHash Hash { get; }

    /// <summary>The identifier of the pack this specification publishes: the leading 16 bytes of its hash (decision 0020).</summary>
    public PackId PackId => PackId.FromSpecificationHash(Hash);

    /// <summary>The first segment of every instance path in the graph (decision 0031).</summary>
    public string RootPath => CompositionDomains.Label(CompositionDomain);

    public static GraphSpecification Create(uint registryRevision, ContentHash registryHash, int generatorVersion, uint grammarVersion, ContentHash grammarHash, ulong seed, PackId sourcePack, ContentHash sourceManifestHash, CompositionDomain domain)
    {
        if (registryRevision == 0 || registryHash.IsUnset)
        {
            throw new ArgumentException("A specification pins a registry revision and its hash.", nameof(registryRevision));
        }

        if (generatorVersion <= 0 || grammarVersion == 0 || grammarHash.IsUnset)
        {
            throw new ArgumentException("A specification pins a generator version and an exact grammar.", nameof(generatorVersion));
        }

        if (sourcePack.IsUnset || sourceManifestHash.IsUnset)
        {
            throw new ArgumentException("A specification pins an exact stored set: pack and manifest hash.", nameof(sourcePack));
        }

        if (!Enum.IsDefined(domain))
        {
            throw new ArgumentException($"Unknown composition domain {(byte)domain}.", nameof(domain));
        }

        var writer = new CanonicalWriter(Domain);
        writer.WriteUInt32(registryRevision);
        writer.WriteContentHash(registryHash);
        writer.WriteVarInt(generatorVersion);
        writer.WriteUInt32(grammarVersion);
        writer.WriteContentHash(grammarHash);
        writer.WriteUInt64(seed);
        writer.WritePackId(sourcePack);
        writer.WriteContentHash(sourceManifestHash);
        writer.WriteUInt8((byte)domain);

        return new GraphSpecification(registryRevision, registryHash, generatorVersion, grammarVersion, grammarHash, seed, sourcePack, sourceManifestHash, domain, writer.ToArray(), writer.ToContentHash());
    }

    public static GraphSpecification Decode(byte[] bytes)
    {
        var reader = new CanonicalReader(bytes, Domain);
        GraphSpecification specification = Read(reader);

        if (!reader.IsAtEnd)
        {
            throw new FormatException("The graph specification record has trailing bytes.");
        }

        if (!specification._bytes.AsSpan().SequenceEqual(bytes))
        {
            throw new FormatException("The graph specification record is not in canonical form: re-encoding it yields different bytes.");
        }

        return specification;
    }

    /// <summary>Writes the fields without a header, into an enclosing graph record.</summary>
    internal void EncodeInline(CanonicalWriter writer)
    {
        writer.WriteUInt32(RegistryRevision);
        writer.WriteContentHash(RegistryHash);
        writer.WriteVarInt(GeneratorVersion);
        writer.WriteUInt32(GrammarVersion);
        writer.WriteContentHash(GrammarHash);
        writer.WriteUInt64(Seed);
        writer.WritePackId(SourcePack);
        writer.WriteContentHash(SourceManifestHash);
        writer.WriteUInt8((byte)CompositionDomain);
    }

    /// <summary>Reads the fields <see cref="EncodeInline"/> wrote and rebuilds the specification, recomputing its hash.</summary>
    internal static GraphSpecification Read(CanonicalReader reader)
    {
        uint registryRevision = reader.ReadUInt32();
        ContentHash registryHash = reader.ReadContentHash();
        long generatorVersion = reader.ReadVarInt();
        uint grammarVersion = reader.ReadUInt32();
        ContentHash grammarHash = reader.ReadContentHash();
        ulong seed = reader.ReadUInt64();
        PackId sourcePack = reader.ReadPackId();
        ContentHash sourceManifestHash = reader.ReadContentHash();
        var domain = (CompositionDomain)reader.ReadUInt8();

        if (generatorVersion is <= 0 or > int.MaxValue)
        {
            throw new FormatException($"Generator version {generatorVersion} is out of range.");
        }

        try
        {
            return Create(registryRevision, registryHash, (int)generatorVersion, grammarVersion, grammarHash, seed, sourcePack, sourceManifestHash, domain);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }
    }
}
