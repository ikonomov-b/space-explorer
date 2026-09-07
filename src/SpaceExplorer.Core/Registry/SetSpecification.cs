using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>How many definitions to draw from one template.</summary>
public sealed record SetRequest(uint TemplateId, uint Count);

/// <summary>
/// The complete input of one set-generation run: the registry revision and hash, the generator and
/// grammar versions, the seed, the exact template vocabulary, the requests in order, and the retry
/// budget. Its content hash identifies the run, and the generated pack's identifier is the leading 16
/// bytes of that hash (decisions 0006, 0020, 0035).
/// </summary>
public sealed class SetSpecification
{
    public const string Domain = "set-specification/1";
    public const int MaxRequests = 4096;

    private readonly byte[] _bytes;

    private SetSpecification(uint registryRevision, ContentHash registryHash, int generatorVersion, uint grammarVersion, ulong seed, PackId templatePack, ContentHash vocabularyHash, SetRequest[] requests, uint retryBudget, byte[] bytes, ContentHash hash)
    {
        RegistryRevision = registryRevision;
        RegistryHash = registryHash;
        GeneratorVersion = generatorVersion;
        GrammarVersion = grammarVersion;
        Seed = seed;
        TemplatePack = templatePack;
        VocabularyHash = vocabularyHash;
        Requests = requests;
        RetryBudget = retryBudget;
        _bytes = bytes;
        Hash = hash;
    }

    public uint RegistryRevision { get; }
    public ContentHash RegistryHash { get; }
    public int GeneratorVersion { get; }
    public uint GrammarVersion { get; }
    public ulong Seed { get; }
    public PackId TemplatePack { get; }
    public ContentHash VocabularyHash { get; }
    public IReadOnlyList<SetRequest> Requests { get; }
    public uint RetryBudget { get; }
    public byte[] Bytes => [.. _bytes];
    public ContentHash Hash { get; }

    /// <summary>The identifier of the pack this specification generates: the leading 16 bytes of its hash (decision 0020).</summary>
    public PackId PackId => PackId.FromSpecificationHash(Hash);

    public static SetSpecification Create(uint registryRevision, ContentHash registryHash, int generatorVersion, uint grammarVersion, ulong seed, PackId templatePack, ContentHash vocabularyHash, IReadOnlyList<SetRequest> requests, uint retryBudget)
    {
        ArgumentNullException.ThrowIfNull(requests);
        if (registryRevision == 0 || registryHash.IsUnset)
        {
            throw new ArgumentException("A specification pins a registry revision and its hash.", nameof(registryRevision));
        }

        if (generatorVersion <= 0 || grammarVersion == 0)
        {
            throw new ArgumentException("Generator and grammar versions start at 1.", nameof(generatorVersion));
        }

        if (templatePack.IsUnset || vocabularyHash.IsUnset)
        {
            throw new ArgumentException("A specification pins an exact template vocabulary: pack and hash.", nameof(templatePack));
        }

        if (requests.Count == 0 || requests.Count > MaxRequests)
        {
            throw new ArgumentException($"A specification makes 1 to {MaxRequests} requests; received {requests.Count}.", nameof(requests));
        }

        foreach (SetRequest request in requests)
        {
            if (request.TemplateId == 0 || request.Count == 0)
            {
                throw new ArgumentException("Each request names a non-zero template and asks for at least one definition.", nameof(requests));
            }
        }

        if (retryBudget == 0)
        {
            throw new ArgumentException("The retry budget is at least one attempt.", nameof(retryBudget));
        }

        var writer = new CanonicalWriter(Domain);
        writer.WriteUInt32(registryRevision);
        writer.WriteContentHash(registryHash);
        writer.WriteVarInt(generatorVersion);
        writer.WriteUInt32(grammarVersion);
        writer.WriteUInt64(seed);
        writer.WritePackId(templatePack);
        writer.WriteContentHash(vocabularyHash);
        writer.WriteCount(requests.Count);
        foreach (SetRequest request in requests)
        {
            writer.WriteUInt32(request.TemplateId);
            writer.WriteUInt32(request.Count);
        }

        writer.WriteUInt32(retryBudget);

        return new SetSpecification(registryRevision, registryHash, generatorVersion, grammarVersion, seed, templatePack, vocabularyHash, [.. requests], retryBudget, writer.ToArray(), writer.ToContentHash());
    }

    public static SetSpecification Decode(byte[] bytes)
    {
        var reader = new CanonicalReader(bytes, Domain);
        uint registryRevision = reader.ReadUInt32();
        ContentHash registryHash = reader.ReadContentHash();
        long generatorVersion = reader.ReadVarInt();
        uint grammarVersion = reader.ReadUInt32();
        ulong seed = reader.ReadUInt64();
        PackId templatePack = reader.ReadPackId();
        ContentHash vocabularyHash = reader.ReadContentHash();
        int count = reader.ReadCount();
        if (count > MaxRequests)
        {
            throw new FormatException($"The specification makes {count} requests; the bound is {MaxRequests}.");
        }

        var requests = new SetRequest[count];
        for (int index = 0; index < count; index++)
        {
            requests[index] = new SetRequest(reader.ReadUInt32(), reader.ReadUInt32());
        }

        uint retryBudget = reader.ReadUInt32();
        if (!reader.IsAtEnd)
        {
            throw new FormatException("The specification record has trailing bytes.");
        }

        if (generatorVersion is <= 0 or > int.MaxValue)
        {
            throw new FormatException($"Generator version {generatorVersion} is out of range.");
        }

        SetSpecification specification;
        try
        {
            specification = Create(registryRevision, registryHash, (int)generatorVersion, grammarVersion, seed, templatePack, vocabularyHash, requests, retryBudget);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }

        if (!specification._bytes.AsSpan().SequenceEqual(bytes))
        {
            throw new FormatException("The specification record is not in canonical form: re-encoding it yields different bytes.");
        }

        return specification;
    }
}
