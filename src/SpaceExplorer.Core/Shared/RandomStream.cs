using System.Buffers.Binary;

namespace SpaceExplorer.Core.Shared;

/// <summary>
/// Derives an independent <see cref="Pcg32"/> stream for a stable path within a world: SHA-256 over a
/// canonical <c>random-stream/2</c> record of the seed and the path, the first eight digest bytes as the
/// state and the next eight, shifted and forced odd, as the increment (decision 0021). Every
/// authoritative draw in the core obtains its generator this way (decision 0008).
/// </summary>
public static class RandomStream
{
    /// <summary>The domain label of the hashed record; it carries the generator version that froze this derivation.</summary>
    private const string Domain = "random-stream/2";

    /// <summary>Derives the stream for <paramref name="path"/> within the world identified by <paramref name="worldSeed"/>.</summary>
    /// <param name="worldSeed">The world seed from the destination specification.</param>
    /// <param name="path">A canonical path; see <see cref="StreamPath"/>.</param>
    public static Pcg32 Derive(ulong worldSeed, string path)
    {
        (ulong state, ulong increment) = DeriveParts(worldSeed, path);
        return Pcg32.FromState(state, increment);
    }

    /// <summary>The two halves <see cref="Derive"/> assigns, exposed so the frozen vectors can assert them directly.</summary>
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
