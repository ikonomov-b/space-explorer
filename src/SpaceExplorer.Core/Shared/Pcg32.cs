using System.Numerics;

namespace SpaceExplorer.Core.Shared;

/// <summary>
/// PCG32 XSH-RR, matching the <c>pcg32</c> generator of the PCG reference implementation, and the only
/// source of authoritative randomness in the core; frozen under <see cref="GeneratorVersion"/>
/// (decisions 0008 and 0021). A sealed class rather than a struct, so that a copy can never silently
/// repeat a sequence (decision 0023). There is no floating-point sampling method by design.
/// </summary>
public sealed class Pcg32
{
    /// <summary>The LCG multiplier fixed by the PCG reference implementation.</summary>
    private const ulong Multiplier = 6364136223846793005UL;

    private ulong _state;
    private readonly ulong _increment;

    private Pcg32(ulong state, ulong increment)
    {
        _state = state;
        _increment = increment;
    }

    /// <summary>Creates a stream from a state and increment directly, as <see cref="RandomStream.Derive"/> does.</summary>
    /// <param name="state">Initial state; any 64-bit value.</param>
    /// <param name="increment">Stream selector; must be odd, as a PCG32 increment always is.</param>
    /// <exception cref="ArgumentException"><paramref name="increment"/> is even.</exception>
    public static Pcg32 FromState(ulong state, ulong increment)
    {
        if ((increment & 1UL) == 0UL)
        {
            throw new ArgumentException(
                "A PCG32 increment must be odd; an even increment halves the period and correlates streams.",
                nameof(increment));
        }

        return new Pcg32(state, increment);
    }

    /// <summary>
    /// Creates a stream with the reference implementation's <c>pcg32_srandom_r</c> routine. Exists only to
    /// check <see cref="NextUInt32"/> against the published vector; generation uses <see cref="FromState"/>.
    /// </summary>
    /// <param name="initialState">The reference routine's <c>initstate</c> argument.</param>
    /// <param name="initialSequence">The reference routine's <c>initseq</c> argument.</param>
    public static Pcg32 FromReferenceSeed(ulong initialState, ulong initialSequence)
    {
        var stream = new Pcg32(0UL, (initialSequence << 1) | 1UL);
        stream.NextUInt32();
        stream._state = unchecked(stream._state + initialState);
        stream.NextUInt32();
        return stream;
    }

    /// <summary>Draws the next 32-bit value and advances the state.</summary>
    public uint NextUInt32()
    {
        ulong previous = _state;
        _state = unchecked((previous * Multiplier) + _increment);

        // XSH-RR: xorshift high bits down, then rotate right by the top five bits of the old state.
        uint xorshifted = (uint)(((previous >> 18) ^ previous) >> 27);
        int rotation = (int)(previous >> 59);
        return BitOperations.RotateRight(xorshifted, rotation);
    }

    /// <summary>Draws a value uniformly in <c>[0, <paramref name="exclusiveBound"/>)</c> by rejection, never by bare modulo (decision 0008).</summary>
    /// <param name="exclusiveBound">Exclusive upper bound; must be at least one.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="exclusiveBound"/> is zero.</exception>
    public uint NextBounded(uint exclusiveBound)
    {
        if (exclusiveBound == 0U)
        {
            throw new ArgumentOutOfRangeException(
                nameof(exclusiveBound),
                "An empty range has no values to draw; the bound must be at least one.");
        }

        // 2^32 mod bound, computed in 32-bit wraparound arithmetic as the reference implementation does.
        uint threshold = unchecked(0U - exclusiveBound) % exclusiveBound;

        while (true)
        {
            uint drawn = NextUInt32();

            if (drawn >= threshold)
            {
                return drawn % exclusiveBound;
            }
        }
    }
}
