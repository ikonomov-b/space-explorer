using SpaceExplorer.Core.Description;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Persistence;

/// <summary>What one destination publication did.</summary>
public sealed record DestinationPublishResult(PackId Pack, ContentHash RecordHash, bool AlreadyPublished, int RecordsWritten);

/// <summary>A published destination as the index lists it, without reading the record.</summary>
public sealed record DestinationEntry(PackId Pack, ContentHash RecordHash, PackId GraphPack, DistanceTier Tier, ulong Seed, uint Attempt);

/// <summary>
/// Publishes and loads the destination record of
/// [decision 0053](../../../docs/decisions/0053-a-destination-is-a-stored-record-addressed-by-its-levers.md)
/// through the protocol sets and graphs already use: the graph it names must already be published with
/// the hash the record pins, the record is written and verified first, and the index row and its
/// reference-index edges are committed last (decisions 0018, 0040, 0042). A differing record under an
/// existing destination pack identifier is a reproduction failure, as a differing manifest is
/// (decision 0039).
/// </summary>
public static class DestinationStore
{
    public static DestinationPublishResult Publish(DataRoot root, DestinationRecord record)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(record);

        using PackageIndex index = PackageIndex.Open(root);

        GraphRow graph = index.FindGraph(record.GraphPack) ?? throw new PackageNotFoundException(record.GraphPack);
        if (graph.GraphHash != record.GraphHash)
        {
            throw new CompatibilityException(
                $"The destination names graph {record.GraphPack} at {record.GraphHash}; this data root holds {graph.GraphHash} for it.");
        }

        if (index.FindDestination(record.Pack) is { } existing)
        {
            if (existing.RecordHash == record.Hash)
            {
                return new DestinationPublishResult(record.Pack, record.Hash, AlreadyPublished: true, RecordsWritten: 0);
            }

            throw new ReproductionFailureException(record.Pack, existing.RecordHash, record.Hash);
        }

        RecordFile.PrepareDirectory(root.PackDirectory(record.Pack));
        int written = RecordFile.Write(root.RecordPath(record.Pack, record.Hash), record.Bytes, record.Hash);

        index.RegisterDestination(record);
        return new DestinationPublishResult(record.Pack, record.Hash, AlreadyPublished: false, written);
    }

    /// <summary>
    /// The stored destination the two levers name, or null when none is stored. The pack identifier follows
    /// from the specification alone, so this costs one index lookup and no composition.
    /// </summary>
    public static DestinationRecord? Find(DataRoot root, DestinationSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(specification);

        DestinationRow? row;
        using (PackageIndex index = PackageIndex.Open(root))
        {
            row = index.FindDestination(specification.PackId);
        }

        return row is null ? null : Read(root, specification.PackId, row.RecordHash);
    }

    /// <summary>The stored destination under <paramref name="pack"/>.</summary>
    /// <exception cref="PackageNotFoundException">No destination is indexed under that pack identifier.</exception>
    public static DestinationRecord Load(DataRoot root, PackId pack)
    {
        ArgumentNullException.ThrowIfNull(root);

        DestinationRow row;
        using (PackageIndex index = PackageIndex.Open(root))
        {
            row = index.FindDestination(pack) ?? throw new PackageNotFoundException(pack);
        }

        return Read(root, pack, row.RecordHash);
    }

    /// <summary>Every published destination with what the index knows of it.</summary>
    public static IReadOnlyList<DestinationEntry> List(DataRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        using PackageIndex index = PackageIndex.Open(root);
        return [.. index.ListDestinations().Select(row => new DestinationEntry(row.Pack, row.RecordHash, row.GraphPack, row.Tier, row.Seed, row.Attempt))];
    }

    private static DestinationRecord Read(DataRoot root, PackId pack, ContentHash hash)
    {
        DestinationRecord record;
        try
        {
            record = DestinationRecord.Decode(SetLoader.ReadVerified(root.RecordPath(pack, hash), hash));
        }
        catch (FormatException exception)
        {
            throw new PackageIntegrityException($"The record of destination {pack} is not a valid destination: {exception.Message}", exception);
        }

        if (record.Pack != pack)
        {
            throw new PackageIntegrityException($"The destination indexed under {pack} derives pack identifier {record.Pack} from its own specification.");
        }

        return record;
    }
}
