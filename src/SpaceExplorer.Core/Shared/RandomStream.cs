namespace SpaceExplorer.Core.Shared;

/// <summary>
/// Derives an independent <see cref="Pcg32"/> stream for a stable path within a world, which is how
/// every authoritative random draw in the core obtains its generator (decision 0008).
/// </summary>
/// <remarks>
/// <para>The derivation, frozen under <see cref="GeneratorVersion"/>:</para>
/// <code>
/// h         = mix64(world_seed, utf8(path))
/// state0    = h.low64
/// increment = (h.high64 &lt;&lt; 1) | 1
/// </code>
/// <para>
/// Both halves come from the mixer. Deriving only the increment from the path and sharing one state
/// across siblings would leave those streams correlated, because PCG32 streams over a shared state are
/// not independent; that failure is the reason this type exists rather than a bare
/// <see cref="Pcg32.FromState"/> call at each use site.
/// </para>
/// <para>
/// Because a stream is addressed by its path, adding an optional detail to one artifact cannot consume
/// randomness that another artifact would have drawn: the second artifact's stream is a function of its
/// own path, not of how much of the first was consumed.
/// </para>
/// </remarks>
public static class RandomStream
{
    /// <summary>Derives the stream for <paramref name="path"/> within the world identified by <paramref name="worldSeed"/>.</summary>
    /// <param name="worldSeed">The world seed from the destination specification.</param>
    /// <param name="path">A canonical path; see <see cref="StreamPath"/>.</param>
    public static Pcg32 Derive(ulong worldSeed, string path)
    {
        byte[] canonical = StreamPath.ToCanonicalBytes(path);
        Mix128 mixed = Mix64.Derive(worldSeed, canonical);

        // The low bit of the high half is displaced by the shift that forces the increment odd, so the
        // stream selector carries 63 bits of entropy and the state carries 64.
        return Pcg32.FromState(mixed.Low, (mixed.High << 1) | 1UL);
    }
}
