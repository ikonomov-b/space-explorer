using System.Numerics;

namespace SpaceExplorer.Core.Shared;

/// <summary>
/// PCG32 XSH-RR: a 64-bit linear congruential state with a 32-bit permuted output, matching the
/// <c>pcg32</c> generator of the PCG reference implementation. Frozen under
/// <see cref="GeneratorVersion"/> (decision 0008).
/// </summary>
/// <remarks>
/// <para>
/// This is the only source of authoritative randomness in the core. <see cref="System.Random"/>,
/// <see cref="System.Guid.NewGuid"/>, wall-clock time, and randomized string hashing are rejected at
/// build time by the banned-API analyzer, because each varies between processes or platforms.
/// </para>
/// <para>
/// There is deliberately no floating-point sampling method. Authoritative code never samples
/// floating-point values, since results must compare equal as integers across operating systems
/// (decision 0008).
/// </para>
/// <para>
/// The struct is mutable: drawing a value advances the state. Copy it and both copies continue the
/// same sequence independently, so pass it by reference when a single sequence must be shared.
/// </para>
/// </remarks>
public struct Pcg32
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

    /// <summary>
    /// Creates a stream from a state and increment directly, which is how
    /// <see cref="RandomStream.Derive"/> initialises every authoritative stream: the mixer supplies
    /// both halves, so the reference seeding routine is not applied on top of them (decision 0008).
    /// </summary>
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
    /// Creates a stream using the reference implementation's <c>pcg32_srandom_r</c> seeding routine.
    /// </summary>
    /// <remarks>
    /// This exists to check <see cref="NextUInt32"/> against the published reference test vector, which
    /// is stated in terms of that routine. Generation uses <see cref="FromState"/> instead, because
    /// decision 0008 assigns the mixer's output halves to the state and increment directly.
    /// </remarks>
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

    /// <summary>
    /// Draws a value uniformly in <c>[0, <paramref name="exclusiveBound"/>)</c> using rejection, as
    /// decision 0008 requires. Modulo alone would bias the low values of a range that does not divide
    /// 2^32; the rejected prefix makes the accepted range an exact multiple of the bound.
    /// </summary>
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
