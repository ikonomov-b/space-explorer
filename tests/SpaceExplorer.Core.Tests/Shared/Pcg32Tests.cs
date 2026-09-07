using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Shared;

public class Pcg32Tests
{
    [Fact]
    public void Output_matches_the_PCG32_reference_demo_for_seed_42_and_stream_54()
    {
        // The external anchor decision 0008 names: the PCG reference implementation's demo output for
        // pcg32_srandom_r(&rng, 42u, 54u). Every other vector in this suite pins our own construction,
        // so this is the one assertion that ties the generator to a published source. If it fails, the
        // XSH-RR output function or the LCG constants are wrong, not merely changed.
        uint[] expected =
        [
            0xa15c02b7u, 0x7b47f409u, 0xba1d3330u, 0x83d2f293u, 0xbfa4784bu, 0xcbed606eu,
        ];

        var stream = Pcg32.FromReferenceSeed(42UL, 54UL);
        uint[] actual = new uint[expected.Length];

        for (int index = 0; index < actual.Length; index++)
        {
            actual[index] = stream.NextUInt32();
        }

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FromState_rejects_an_even_increment()
    {
        // An even increment gives the LCG a shorter period and correlates the stream with others
        // sharing its state, so the derivation's "| 1" is a precondition rather than a convention.
        var thrown = Assert.Throws<ArgumentException>(() => Pcg32.FromState(1UL, 2UL));

        Assert.Equal("increment", thrown.ParamName);
    }

    [Fact]
    public void NextBounded_rejects_an_empty_range()
    {
        var stream = RandomStream.Derive(1UL, "test");

        Assert.Throws<ArgumentOutOfRangeException>(() => stream.NextBounded(0U));
    }

    [Fact]
    public void NextBounded_rejects_the_biased_prefix_rather_than_taking_a_bare_modulo()
    {
        // Bound 2^31 + 1 has threshold 2^32 mod bound = 2^31 - 1, so very nearly half of all raw draws
        // must be discarded. Replaying the rejection rule against the raw stream therefore diverges from
        // a bare-modulo implementation within the first few results, which is the bias decision 0008
        // requires rejection to remove.
        const uint bound = 0x80000001U;
        const uint threshold = 0x7FFFFFFFU;

        var bounded = RandomStream.Derive(3UL, "rejection");
        var raw = RandomStream.Derive(3UL, "rejection");
        int rejected = 0;

        for (int result = 0; result < 200; result++)
        {
            uint drawn;

            while ((drawn = raw.NextUInt32()) < threshold)
            {
                rejected++;
            }

            Assert.Equal(drawn % bound, bounded.NextBounded(bound));
        }

        Assert.True(rejected > 0, "Expected the near-half rejection rate of this bound to reject draws.");
    }

    [Fact]
    public void A_stream_is_a_reference_so_a_copy_cannot_silently_repeat_the_sequence()
    {
        // Structural, like the architecture tests (decision 0023): as a mutable struct, a stream read
        // through a collection or a readonly field would be a copy that never advanced.
        Assert.False(typeof(Pcg32).IsValueType);

        var streams = new List<Pcg32> { RandomStream.Derive(1UL, "system/planet/0") };

        Assert.NotEqual(streams[0].NextUInt32(), streams[0].NextUInt32());
    }
}
