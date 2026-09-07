namespace SpaceExplorer.Core.Shared;

/// <summary>
/// Identity of the frozen set of authoritative generation algorithms. World manifests and primitive
/// set manifests pin this value, so a change to any algorithm frozen under it is a version increment,
/// never a silent edit (technical design, destination identity and determinism).
/// </summary>
/// <remarks>
/// <para>
/// Version 2 freezes: the SHA-256 stream derivation of <see cref="RandomStream"/>, the
/// <see cref="Pcg32"/> XSH-RR output function and raw-state initialisation, rejection-based bounded
/// sampling, and the <see cref="StreamPath"/> canonical form (decisions 0008 and 0021).
/// </para>
/// <para>
/// Version 1 differed in one respect: stream derivation ran a SplitMix64-based construction of this
/// project's own instead of SHA-256 (decision 0019). Nothing was ever generated under it, so no stored
/// world pins it.
/// </para>
/// </remarks>
public static class GeneratorVersion
{
    /// <summary>The generator version this build produces and validates.</summary>
    public const int Current = 2;
}
