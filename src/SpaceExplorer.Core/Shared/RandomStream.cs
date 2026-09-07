using System.Buffers.Binary;

namespace SpaceExplorer.Core.Shared;

/// <summary>
/// Derives an independent <see cref="Pcg32"/> stream for a stable path within a world, which is how
/// every authoritative random draw in the core obtains its generator (decision 0008).
/// </summary>
/// <remarks>
/// <para>The derivation, frozen under <see cref="GeneratorVersion"/> (decision 0021):</para>
/// <code>
/// bytes     = canonical(domain "random-stream/2", world_seed as u64, utf8(path) as a byte string)
/// h         = sha256(bytes)
/// state0    = little-endian u64 of h[0..8]
/// increment = (little-endian u64 of h[8..16] &lt;&lt; 1) | 1
/// </code>
/// <para>
/// The mixing function is SHA-256, the same primitive <see cref="ContentHash"/> uses, so the core
/// freezes one externally published hash rather than a published finalizer wrapped in a construction
/// of this project's own. The bytes it consumes come from <see cref="CanonicalWriter"/>, so the seed's
/// byte order and the framing between the seed and the path are the frozen ones and nothing here
/// re-decides them: the length prefix on the path is what keeps a seed and path pair from colliding
/// with a different pair whose bytes concatenate the same way.
/// </para>
/// <para>
/// Both halves come from the digest. Deriving only the increment from the path and sharing one state
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
    /// <summary>
    /// The domain label of the hashed record. It carries the generator version that froze this
    /// derivation, so a later construction takes its own label and cannot collide with this one.
    /// </summary>
    private const string Domain = "random-stream/2";

    /// <summary>Derives the stream for <paramref name="path"/> within the world identified by <paramref name="worldSeed"/>.</summary>
    /// <param name="worldSeed">The world seed from the destination specification.</param>
    /// <param name="path">A canonical path; see <see cref="StreamPath"/>.</param>
    public static Pcg32 Derive(ulong worldSeed, string path)
    {
        (ulong state, ulong increment) = DeriveParts(worldSeed, path);
        return Pcg32.FromState(state, increment);
    }

    /// <summary>
    /// The two halves <see cref="Derive"/> assigns, exposed so the frozen vectors can assert them
    /// directly rather than only through the draws they produce.
    /// </summary>
    internal static (ulong State, ulong Increment) DeriveParts(ulong worldSeed, string path)
    {
        var writer = new CanonicalWriter(Domain);
        writer.WriteUInt64(worldSeed);
        writer.WriteBytes(StreamPath.ToCanonicalBytes(path));

        ReadOnlySpan<byte> digest = writer.ToContentHash().Bytes;

        // The low bit of the second half is displaced by the shift that forces the increment odd, so
        // the stream selector carries 63 bits of entropy and the state carries 64.
        return (
            BinaryPrimitives.ReadUInt64LittleEndian(digest[..8]),
            (BinaryPrimitives.ReadUInt64LittleEndian(digest[8..16]) << 1) | 1UL);
    }
}
