namespace SpaceExplorer.Core.Networking;

/// <summary>
/// The network protocol version, a separate axis from <see cref="Shared.CanonicalWriter.FormatVersion"/>
/// and <see cref="Shared.GeneratorVersion"/>: save format, generator, and protocol compatibility change
/// independently (decision 0028).
/// </summary>
public static class ProtocolVersion
{
    /// <summary>The protocol version this build speaks and requires of a peer at join.</summary>
    public const uint Current = 1;
}
