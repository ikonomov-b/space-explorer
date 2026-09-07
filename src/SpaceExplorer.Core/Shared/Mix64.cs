namespace SpaceExplorer.Core.Shared;

/// <summary>The 128 bits produced by <see cref="Mix64.Derive"/>.</summary>
/// <param name="Low">Low 64 bits; seeds the PCG32 state.</param>
/// <param name="High">High 64 bits; selects the PCG32 stream.</param>
public readonly record struct Mix128(ulong Low, ulong High);

/// <summary>
/// The pinned 64-bit mixer over a seed and a byte string, producing the 128 bits that
/// <see cref="RandomStream"/> splits into a PCG32 state and stream (decision 0008).
/// </summary>
/// <remarks>
/// <para>
/// The mixing primitive is the SplitMix64 finalizer, which decision 0008 names as an accepted choice.
/// It is an externally published function: the two multipliers and three shift distances below can be
/// checked against any SplitMix64 implementation.
/// </para>
/// <para>
/// The construction <em>around</em> that primitive is pinned here rather than inherited from an
/// external source, and is therefore frozen under <see cref="GeneratorVersion"/>:
/// </para>
/// <list type="number">
///   <item>Start with the seed as the running value.</item>
///   <item>Absorb the input eight bytes at a time as explicit little-endian 64-bit chunks, finalizing
///   after each. A trailing partial chunk is zero-padded.</item>
///   <item>Absorb the input length, so that inputs differing only by trailing zero bytes, which pad to
///   the same final chunk, do not collide.</item>
///   <item>Finalize twice more against two distinct domain constants to obtain independent halves.</item>
/// </list>
/// <para>
/// Little-endian chunk assembly is explicit rather than delegated to <c>BitConverter</c>, because
/// stored and transmitted encodings must not depend on host byte order (technical design, architecture
/// and ownership).
/// </para>
/// </remarks>
public static class Mix64
{
    // SplitMix64 finalizer constants; externally published.
    private const ulong FinalizerMultiplier1 = 0xBF58476D1CE4E5B9UL;
    private const ulong FinalizerMultiplier2 = 0x94D049BB133111EBUL;

    // Domain separation for the two output halves. Both are published odd 64-bit constants:
    // the golden-ratio gamma used by SplitMix64, and xxHash64 PRIME64_2.
    private const ulong StateDomain = 0x9E3779B97F4A7C15UL;
    private const ulong StreamDomain = 0xC2B2AE3D27D4EB4FUL;

    /// <summary>Applies the SplitMix64 finalizer, a bijective 64-bit avalanche function.</summary>
    public static ulong Finalize(ulong value)
    {
        ulong z = value;
        z = unchecked((z ^ (z >> 30)) * FinalizerMultiplier1);
        z = unchecked((z ^ (z >> 27)) * FinalizerMultiplier2);
        return z ^ (z >> 31);
    }

    /// <summary>
    /// Mixes <paramref name="seed"/> and <paramref name="data"/> into 128 bits.
    /// </summary>
    /// <param name="seed">The world seed, or any other 64-bit domain value.</param>
    /// <param name="data">Canonical bytes of the path, from <see cref="StreamPath.ToCanonicalBytes"/>.</param>
    public static Mix128 Derive(ulong seed, ReadOnlySpan<byte> data)
    {
        ulong running = seed;

        for (int offset = 0; offset < data.Length; offset += 8)
        {
            running = Finalize(running ^ ReadLittleEndianChunk(data, offset));
        }

        running = Finalize(running ^ (ulong)data.Length);

        return new Mix128(Finalize(running ^ StateDomain), Finalize(running ^ StreamDomain));
    }

    /// <summary>Reads up to eight bytes at <paramref name="offset"/> as a little-endian 64-bit value, zero-padded.</summary>
    private static ulong ReadLittleEndianChunk(ReadOnlySpan<byte> data, int offset)
    {
        int available = Math.Min(8, data.Length - offset);
        ulong chunk = 0UL;

        for (int index = 0; index < available; index++)
        {
            chunk |= (ulong)data[offset + index] << (8 * index);
        }

        return chunk;
    }
}
