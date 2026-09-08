using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Persistence;

/// <summary>What one publication did.</summary>
public sealed record PublishResult(PackId Pack, ContentHash ManifestHash, bool AlreadyPublished, int RecordsWritten);

/// <summary>
/// Publishes a generated set: writes each record through <see cref="RecordFile"/> and commits the index
/// rows last (decision 0018). A differing manifest under an existing pack identifier is refused before
/// anything is written (decision 0039), and the reference-index edge lands in the same last transaction
/// (decision 0040).
/// </summary>
public static class SetPublisher
{
    public static PublishResult Publish(DataRoot root, PrimitiveSet set)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(set);

        SetManifest manifest = set.Manifest;
        using PackageIndex index = PackageIndex.Open(root);

        ContentHash existing = index.FindManifest(manifest.Pack);
        if (!existing.IsUnset)
        {
            if (existing == manifest.Hash)
            {
                return new PublishResult(manifest.Pack, manifest.Hash, AlreadyPublished: true, RecordsWritten: 0);
            }

            throw new ReproductionFailureException(manifest.Pack, existing, manifest.Hash);
        }

        RecordFile.PrepareDirectory(root.PackDirectory(manifest.Pack));

        int written = 0;
        foreach (PrimitiveDefinition definition in set.Definitions)
        {
            written += RecordFile.Write(root.RecordPath(manifest.Pack, definition.Hash), definition.Bytes, definition.Hash);
        }

        written += RecordFile.Write(root.RecordPath(manifest.Pack, manifest.Hash), manifest.Bytes, manifest.Hash);

        index.RegisterPackage(manifest);
        return new PublishResult(manifest.Pack, manifest.Hash, AlreadyPublished: false, written);
    }
}
