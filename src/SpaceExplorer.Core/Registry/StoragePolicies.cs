namespace SpaceExplorer.Core.Registry;

/// <summary>The storage policies a category permits; the world or set manifest pins one per category (decision 0034).</summary>
[Flags]
public enum StoragePolicies : byte
{
    None = 0,
    Regenerate = 1,
    Materialize = 2,
    Hybrid = 4,
}
