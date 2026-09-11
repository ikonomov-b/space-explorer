namespace SpaceExplorer.Core.Derivation;

/// <summary>
/// Sine and cosine of a binary turn, in fixed point and without a floating-point value
/// ([decision 0063](../../../docs/decisions/0063-relief-by-terrain-heightfield-registry-revision-6-and-grammar-version-7.md)
/// clause 5). <see cref="TerrainHeightfield"/> is its first consumer, for the six evaluations a region's
/// frame costs, and the two-body propagation of
/// [decision 0037](../../../docs/decisions/0037-derivation-rules-integer-periods-and-orbit-hierarchy.md)
/// is to be its second: one implementation serves both, because a second would be a second set of
/// answers to the same question.
/// </summary>
/// <remarks>
/// Frozen from the first stored or vectored result that rests on it: changing a coefficient, the range
/// reduction, or the scale is a version change of every rule that calls it.
///
/// The coefficients are derived rather than tabulated, so a reader can check them by arithmetic instead
/// of trusting 1,025 literals, and the series is truncated where the argument never exceeds an eighth
/// turn — beyond the x^11 term the error is under a unit at the 2^30 scale the answer is given at.
/// </remarks>
public static class IntegerTrigonometry
{
    /// <summary>The scale a sine or cosine is returned at: one is 2^30, so the answer fits an int32.</summary>
    public const long One = 1L << 30;

    /// <summary>A quarter turn, as a binary turn of decision 0036: 2^32 to the turn.</summary>
    private const long QuarterTurn = 1L << 30;

    /// <summary>Two pi at scale 2^32, which is the angle in radians a whole binary turn stands for.</summary>
    private const ulong Tau = 26_986_075_409UL;

    /// <summary>The sine of <paramref name="turn"/>, a binary turn of decision 0036, at scale <see cref="One"/>.</summary>
    public static long Sin(int turn)
    {
        uint value = (uint)turn;
        uint quadrant = value >> 30;
        uint within = value & 0x3FFF_FFFFU;

        return quadrant switch
        {
            0 => Eighth(within),
            1 => Eighth((uint)(QuarterTurn - within)),
            2 => -Eighth(within),
            _ => -Eighth((uint)(QuarterTurn - within)),
        };
    }

    /// <summary>The cosine of <paramref name="turn"/>, at scale <see cref="One"/>: the sine a quarter turn on.</summary>
    public static long Cos(int turn) => Sin(unchecked(turn + (int)QuarterTurn));

    /// <summary>
    /// The sine of a quarter turn's worth of angle, folded to an eighth so neither series is evaluated
    /// beyond where its truncation is accurate.
    /// </summary>
    private static long Eighth(uint within) =>
        within <= QuarterTurn / 2
            ? SinPoly(within)
            : CosPoly((uint)(QuarterTurn - within));

    /// <summary>The odd series, in x at scale 2^32 with the accumulator at 2^62, returning scale 2^30.</summary>
    private static long SinPoly(uint turn)
    {
        UInt128 x = Radians(turn);
        UInt128 square = (x * x) >> 2;

        Int128 accumulator = Coefficient(11);
        for (int term = 9; term >= 1; term -= 2)
        {
            accumulator = Coefficient(term) - (Int128)(((UInt128)accumulator * square) >> 62);
        }

        return (long)(((Int128)x * accumulator) >> 64);
    }

    /// <summary>The even series, over the same scales, returning scale 2^30.</summary>
    private static long CosPoly(uint turn)
    {
        UInt128 x = Radians(turn);
        UInt128 square = (x * x) >> 2;

        Int128 accumulator = Coefficient(12);
        for (int term = 10; term >= 0; term -= 2)
        {
            accumulator = Coefficient(term) - (Int128)(((UInt128)accumulator * square) >> 62);
        }

        return (long)(accumulator >> 32);
    }

    /// <summary>The angle of a binary turn in radians, at scale 2^32.</summary>
    private static UInt128 Radians(uint turn) => ((UInt128)turn * Tau) >> 32;

    /// <summary>The Taylor coefficient <c>1/k!</c> at scale 2^62, computed rather than tabulated.</summary>
    private static Int128 Coefficient(int order)
    {
        Int128 factorial = 1;
        for (int step = 2; step <= order; step++)
        {
            factorial *= step;
        }

        Int128 scale = (Int128)1 << 62;
        return (scale + (factorial / 2)) / factorial;
    }
}
