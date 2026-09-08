using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Persistence;

/// <summary>
/// Loads a published set without generating anything: the index names the manifest, every record is
/// verified against its hash before it is decoded, the manifest's pinned registry revision must be one
/// the caller supports and must pin that revision's hash, and the definitions must fit the manifest
/// exactly (decisions 0031, 0033, 0035).
/// </summary>
public static class SetLoader
{
    public static PrimitiveSet Load(DataRoot root, PackId pack, CategoryRegistries registries)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(registries);

        ContentHash manifestHash;
        using (PackageIndex index = PackageIndex.Open(root))
        {
            manifestHash = index.FindManifest(pack);
        }

        if (manifestHash.IsUnset)
        {
            throw new PackageNotFoundException(pack);
        }

        SetManifest manifest;
        try
        {
            manifest = SetManifest.Decode(ReadVerified(root.RecordPath(pack, manifestHash), manifestHash));
        }
        catch (FormatException exception)
        {
            throw new PackageIntegrityException($"The manifest of pack {pack} is not a valid record: {exception.Message}", exception);
        }

        if (manifest.Pack != pack)
        {
            throw new PackageIntegrityException($"The manifest indexed under pack {pack} declares pack {manifest.Pack}.");
        }

        CategoryRegistry registry = registries.TryFind(manifest.RegistryRevision)
            ?? throw new CompatibilityException(
                $"Pack {pack} was generated under category-registry revision {manifest.RegistryRevision}; this build supports revision(s) {string.Join(", ", registries.Revisions)}.");

        if (manifest.RegistryHash != registry.Hash)
        {
            throw new CompatibilityException(
                $"Pack {pack} pins category-registry revision {manifest.RegistryRevision} with hash {manifest.RegistryHash}; this build holds {registry.Hash} for that revision.");
        }

        var definitions = new PrimitiveDefinition[manifest.Definitions.Count];
        for (int index = 0; index < definitions.Length; index++)
        {
            ManifestEntry entry = manifest.Definitions[index];
            try
            {
                definitions[index] = PrimitiveDefinition.Decode(ReadVerified(root.RecordPath(pack, entry.Hash), entry.Hash), registry, new PrimitiveId(pack, entry.LocalId));
            }
            catch (FormatException exception)
            {
                throw new PackageIntegrityException($"Definition {entry.LocalId} of pack {pack} is not a valid record: {exception.Message}", exception);
            }
        }

        try
        {
            return PrimitiveSet.Create(manifest, definitions);
        }
        catch (ArgumentException exception)
        {
            throw new PackageIntegrityException($"Pack {pack} does not fit its manifest: {exception.Message}", exception);
        }
    }

    /// <summary>Every published pack with its manifest hash and definition count.</summary>
    public static IReadOnlyList<(PackId Pack, ContentHash Manifest, int DefinitionCount)> List(DataRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        using PackageIndex index = PackageIndex.Open(root);
        return index.ListPackages();
    }

    /// <summary>Reads a record file and refuses it unless it hashes to its name; the first check every load makes (decision 0018).</summary>
    internal static byte[] ReadVerified(string path, ContentHash expected)
    {
        if (!File.Exists(path))
        {
            throw new PackageIntegrityException($"Record {expected} is missing at {path}.");
        }

        byte[] bytes = File.ReadAllBytes(path);
        if (ContentHash.Of(bytes) != expected)
        {
            throw new PackageIntegrityException($"Record at {path} does not hash to {expected}; it is corrupt.");
        }

        return bytes;
    }
}
