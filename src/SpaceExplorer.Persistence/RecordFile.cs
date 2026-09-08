using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Persistence;

/// <summary>
/// The one way a content-addressed record reaches disk: written to a temporary file, flushed, re-read,
/// verified against its own hash, then moved into place without overwriting different content, with the
/// index committed afterwards by the caller (decision 0018). Sets and composition graphs publish through
/// this same path.
/// </summary>
internal static class RecordFile
{
    /// <summary>Installs <paramref name="bytes"/> at <paramref name="path"/>; returns 1 when it wrote the file and 0 when the verified content was already there.</summary>
    public static int Write(string path, byte[] bytes, ContentHash hash)
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

    /// <summary>Prepares a pack directory: creates it and removes any temporary file an interrupted publication left.</summary>
    public static void PrepareDirectory(string directory)
    {
        Directory.CreateDirectory(directory);
        foreach (string leftover in Directory.EnumerateFiles(directory, "*.tmp"))
        {
            File.Delete(leftover);
        }
    }

    private static void VerifyExisting(string path, ContentHash hash)
    {
        if (ContentHash.Of(File.ReadAllBytes(path)) != hash)
        {
            throw new PackageIntegrityException($"The file at {path} does not hash to its name; it is corrupt and is never overwritten.");
        }
    }
}
