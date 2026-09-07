using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Persistence;

/// <summary>
/// The one root under which player data lives: the local application data folder plus
/// <c>SpaceExplorer</c>, overridable by <c>SPACE_EXPLORER_DATA_DIR</c>, with rebuildable caches under a
/// separate cache root (decision 0018). Packs and the index database hang off it (decisions 0039, 0040).
/// </summary>
public sealed class DataRoot
{
    public const string OverrideVariable = "SPACE_EXPLORER_DATA_DIR";

    private DataRoot(string path, string cachePath)
    {
        Path = path;
        CachePath = cachePath;
    }

    public string Path { get; }
    public string CachePath { get; }

    public string PacksDirectory => System.IO.Path.Combine(Path, "packs");
    public string IndexDatabasePath => System.IO.Path.Combine(Path, "index.db");

    /// <summary>Resolves the root for this user and platform, honouring the override variable.</summary>
    public static DataRoot Resolve()
    {
        string? overridden = Environment.GetEnvironmentVariable(OverrideVariable);
        if (!string.IsNullOrEmpty(overridden))
        {
            return At(overridden);
        }

        if (OperatingSystem.IsWindows())
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string root = System.IO.Path.Combine(local, "SpaceExplorer");
            return new DataRoot(root, System.IO.Path.Combine(root, "cache"));
        }

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME") is { Length: > 0 } data ? data : System.IO.Path.Combine(home, ".local", "share");
        string cacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME") is { Length: > 0 } cache ? cache : System.IO.Path.Combine(home, ".cache");
        return new DataRoot(System.IO.Path.Combine(dataHome, "SpaceExplorer"), System.IO.Path.Combine(cacheHome, "SpaceExplorer"));
    }

    /// <summary>A root at an explicit path, for tests and the override.</summary>
    public static DataRoot At(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        string full = System.IO.Path.GetFullPath(path);
        return new DataRoot(full, System.IO.Path.Combine(full, "cache"));
    }

    public string PackDirectory(PackId pack) => System.IO.Path.Combine(PacksDirectory, pack.ToString());

    public string RecordPath(PackId pack, ContentHash hash) => System.IO.Path.Combine(PackDirectory(pack), hash + ".bin");
}
