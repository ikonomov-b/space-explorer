using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Persistence;

/// <summary>What one publication did.</summary>
public sealed record PublishResult(PackId Pack, ContentHash ManifestHash, bool AlreadyPublished, int RecordsWritten);

/// <summary>
/// Publishes a generated set: writes each record to a temporary file, flushes and re-reads it, verifies
/// its hash, installs it under its hash without overwriting different content, and commits the index
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

        string directory = root.PackDirectory(manifest.Pack);
        Directory.CreateDirectory(directory);
        foreach (string leftover in Directory.EnumerateFiles(directory, "*.tmp"))
        {
            File.Delete(leftover);
        }

        int written = 0;
        foreach (PrimitiveDefinition definition in set.Definitions)
        {
            written += WriteRecord(root.RecordPath(manifest.Pack, definition.Hash), definition.Bytes, definition.Hash);
        }

        written += WriteRecord(root.RecordPath(manifest.Pack, manifest.Hash), manifest.Bytes, manifest.Hash);

        index.RegisterPackage(manifest);
        return new PublishResult(manifest.Pack, manifest.Hash, AlreadyPublished: false, written);
    }

    private static int WriteRecord(string path, byte[] bytes, ContentHash hash)
    {
        if (File.Exists(path))
        {
            VerifyExisting(path, hash);
            return 0;
        }

        string temporary = path + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes);
            stream.Flush(flushToDisk: true);
        }

        byte[] reread = File.ReadAllBytes(temporary);
        if (ContentHash.Of(reread) != hash)
        {
            File.Delete(temporary);
            throw new PackageIntegrityException($"Record {hash} did not read back with its own hash after writing; the filesystem is not trustworthy here.");
        }

        try
        {
            File.Move(temporary, path, overwrite: false);
        }
        catch (IOException) when (File.Exists(path))
        {
            // Another writer installed the same content-addressed record first; verify and keep theirs.
            VerifyExisting(path, hash);
            File.Delete(temporary);
            return 0;
        }

        return 1;
    }

    private static void VerifyExisting(string path, ContentHash hash)
    {
        if (ContentHash.Of(File.ReadAllBytes(path)) != hash)
        {
            throw new PackageIntegrityException($"The file at {path} does not hash to its name; it is corrupt and is never overwritten.");
        }
    }
}
