using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Generation;

/// <summary>
/// Draws one parameter value inside a template's range from a <see cref="Pcg32"/> stream, using only
/// bounded integer sampling by rejection (decision 0008). Draw order per kind is fixed here and is part
/// of what the generator version freezes.
/// </summary>
public static class ParameterSampler
{
    /// <summary>Draws a value inside <paramref name="range"/>; a reference is drawn uniformly from <paramref name="referencePool"/>.</summary>
    /// <exception cref="InvalidOperationException">A reference is needed and the pool is empty.</exception>
    public static ParameterValue Sample(Pcg32 stream, ParameterRange range, ParameterDescriptor descriptor, IReadOnlyList<PrimitiveRevisionRef> referencePool)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(range);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(referencePool);

        switch (range.Kind)
        {
            case ParameterKind.Bool:
                return ParameterValue.Bool(range.BoolMask switch
                {
                    1 => false,
                    2 => true,
                    _ => stream.NextBounded(2) == 1,
                });
            case ParameterKind.Enum:
                return ParameterValue.Enum(range.EnumAllowed[(int)stream.NextBounded((uint)range.EnumAllowed.Count)]);
            case ParameterKind.Integer:
                return ParameterValue.Integer(SampleInclusive(stream, range.Min[0], range.Max[0]));
            case ParameterKind.BinaryTurn:
                return ParameterValue.BinaryTurn((int)SampleInclusive(stream, range.Min[0], range.Max[0]));
            case ParameterKind.Rotation:
                return ParameterValue.Rotation(
                    (int)SampleInclusive(stream, range.Min[0], range.Max[0]),
                    (int)SampleInclusive(stream, range.Min[1], range.Max[1]),
                    (int)SampleInclusive(stream, range.Min[2], range.Max[2]));
            case ParameterKind.Vector3:
                return ParameterValue.Vector3(
                    SampleInclusive(stream, range.Min[0], range.Max[0]),
                    SampleInclusive(stream, range.Min[1], range.Max[1]),
                    SampleInclusive(stream, range.Min[2], range.Max[2]));
            case ParameterKind.Colour:
                return ParameterValue.Colour(
                    (byte)SampleInclusive(stream, range.Min[0], range.Max[0]),
                    (byte)SampleInclusive(stream, range.Min[1], range.Max[1]),
                    (byte)SampleInclusive(stream, range.Min[2], range.Max[2]),
                    (byte)SampleInclusive(stream, range.Min[3], range.Max[3]));
            case ParameterKind.PrimitiveRef:
                if (referencePool.Count == 0)
                {
                    throw new InvalidOperationException($"Parameter '{descriptor.Label}' needs a definition of category {descriptor.RefCategory}, and none has been generated yet.");
                }

                return ParameterValue.Ref(referencePool[(int)stream.NextBounded((uint)referencePool.Count)]);
            default:
                throw new InvalidOperationException($"Unknown parameter kind {(byte)range.Kind}.");
        }
    }

    /// <summary>Draws uniformly in <c>[min, max]</c>, rejecting rather than reducing modulo when the span exceeds 32 bits.</summary>
    internal static long SampleInclusive(Pcg32 stream, long min, long max)
    {
        ulong span = unchecked((ulong)(max - min)) + 1UL;
        if (span == 0UL)
        {
            // The full 64-bit range: two draws make one value and nothing is rejected.
            return unchecked((long)Next64(stream));
        }

        if (span <= uint.MaxValue)
        {
            return min + stream.NextBounded((uint)span);
        }

        // 2^64 mod span, computed in wraparound arithmetic as Pcg32.NextBounded does for 32 bits.
        ulong threshold = unchecked(0UL - span) % span;
        while (true)
        {
            ulong drawn = Next64(stream);
            if (drawn >= threshold)
            {
                return unchecked(min + (long)(drawn % span));
            }
        }
    }

    private static ulong Next64(Pcg32 stream)
    {
        ulong high = stream.NextUInt32();
        ulong low = stream.NextUInt32();
        return (high << 32) | low;
    }
}
