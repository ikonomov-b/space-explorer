using SpaceExplorer.Core.Derivation;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Persistence;

/// <summary>What one payload publication did.</summary>
public sealed record PayloadPublishResult(ContentHash Hash, bool AlreadyPublished, int BytesWritten);

/// <summary>
/// Stores and reloads the ground of a region
/// ([decision 0066](../../../docs/decisions/0066-generation-writes-to-the-database-and-the-view-only-reads.md)),
/// through the protocol every other record uses: the bytes are written, flushed, re-read and verified
/// against the content hash that names them before anything points at them, and a record that does not
/// hash to its own name is refused rather than drawn (decisions 0018, 0042).
/// </summary>
/// <remarks>
/// A payload lives in its graph's pack, because a region is a node of that graph and its ground belongs to
/// the same world. Two runs of one generation therefore publish one file: the bytes are a function of the
/// field and the anchor, so the hash is the same and the second publication is a no-op.
/// </remarks>
public static class PayloadStore
{
    /// <summary>Writes <paramref name="payload"/> into <paramref name="graphPack"/>, or finds it already there.</summary>
    public static PayloadPublishResult Publish(DataRoot root, PackId graphPack, RegionPayload payload)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(payload);

        byte[] bytes = payload.Bytes;
        ContentHash hash = ContentHash.Of(bytes);
        string path = root.RecordPath(graphPack, hash);

        if (File.Exists(path))
        {
            return new PayloadPublishResult(hash, AlreadyPublished: true, BytesWritten: 0);
        }

        RecordFile.PrepareDirectory(root.PackDirectory(graphPack));
        RecordFile.Write(path, bytes, hash);
        return new PayloadPublishResult(hash, AlreadyPublished: false, bytes.Length);
    }

    /// <summary>The stored ground of one region, or null where none has been generated yet.</summary>
    /// <exception cref="PackageIntegrityException">The record does not hash to its name, or is not a payload.</exception>
    public static RegionPayload? TryLoad(DataRoot root, PackId graphPack, ContentHash hash)
    {
        ArgumentNullException.ThrowIfNull(root);

        string path = root.RecordPath(graphPack, hash);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return RegionPayload.Decode(SetLoader.ReadVerified(path, hash));
        }
        catch (FormatException exception)
        {
            throw new PackageIntegrityException($"The record {hash} of graph {graphPack} is not a valid region payload: {exception.Message}", exception);
        }
    }

    /// <summary>
    /// The ground of one region, loaded where it has been stored and generated, stored and then loaded
    /// where it has not: whoever calls this draws bytes that are on the disk either way, which is
    /// decision 0066 clause 1's whole claim.
    /// </summary>
    /// <remarks>
    /// The index is what makes a revisit a load. A payload is named by its own content, so nothing could
    /// ask for one by name without first producing the content it is named for; the `payloads` row answers
    /// "has this region's ground been generated" from the region's path instead, and the hash it gives back
    /// is then verified against the bytes as every other record is.
    ///
    /// A row whose field hash is not this field's is ground of another world — the planet's relief
    /// parameters have moved since it was written — and is regenerated rather than drawn, because what a
    /// payload promises is the surface of the content that names it.
    /// </remarks>
    public static RegionPayload Resolve(
        DataRoot root,
        PackId graphPack,
        string instancePath,
        ReliefField field,
        int latitude,
        int longitude,
        int heading,
        long extentMetres,
        out bool generated)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(field);

        string rule = field.RidgingFraction is null ? TerrainHeightfield.Rule : TerrainHeightfield.RidgedRule;
        ContentHash fieldHash = field.Hash;

        using (PackageIndex index = PackageIndex.Open(root))
        {
            if (index.FindPayload(graphPack, instancePath) is { } row
                && row.FieldHash == fieldHash
                && row.Rule == rule
                && TryLoad(root, graphPack, row.RecordHash) is { } stored)
            {
                generated = false;
                return stored;
            }
        }

        RegionPayload payload = RegionPayload.Of(
            instancePath,
            rule,
            fieldHash,
            extentMetres,
            TerrainHeightfield.Sample(field, latitude, longitude, heading, extentMetres));

        PayloadPublishResult published = Publish(root, graphPack, payload);
        using (PackageIndex index = PackageIndex.Open(root))
        {
            index.RegisterPayload(graphPack, instancePath, published.Hash, fieldHash, rule, payload.Bytes.Length);
        }

        generated = true;
        return payload;
    }
}
