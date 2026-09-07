namespace SpaceExplorer.Core.Shared;

/// <summary>
/// Identity of the frozen set of authoritative generation algorithms. Every manifest pins it, and a
/// change to any frozen algorithm increments it rather than editing it (decision 0008).
/// </summary>
/// <remarks>Version 2 is decision 0021; version 1, superseded before anything was generated under it, is decision 0019.</remarks>
public static class GeneratorVersion
{
    /// <summary>The generator version this build produces and validates.</summary>
    public const int Current = 2;
}
