namespace SpaceExplorer.Core.Shared;

/// <summary>
/// Identity of the frozen set of authoritative generation algorithms. World manifests and primitive
/// set manifests pin this value, so a change to any algorithm frozen under it is a version increment,
/// never a silent edit (technical design, destination identity and determinism).
/// </summary>
/// <remarks>
/// Version 1 freezes: the <see cref="Mix64"/> construction and its domain constants, the
/// <see cref="Pcg32"/> XSH-RR output function and raw-state initialisation, rejection-based bounded
/// sampling, and the <see cref="StreamPath"/> canonical form (decision 0008).
/// </remarks>
public static class GeneratorVersion
{
    /// <summary>The generator version this build produces and validates.</summary>
    public const int Current = 1;
}
