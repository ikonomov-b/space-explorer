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
    public void NextBounded_of_one_always_yields_zero_without_consuming_a_draw_pattern()
    {
        var stream = RandomStream.Derive(1UL, "test");

        for (int index = 0; index < 32; index++)
        {
            Assert.Equal(0U, stream.NextBounded(1U));
        }
    }

    [Fact]
    public void NextBounded_never_returns_a_value_at_or_above_the_bound()
    {
        var stream = RandomStream.Derive(7UL, "bounds");

        foreach (uint bound in new[] { 2U, 3U, 7U, 256U, 1000U, uint.MaxValue })
        {
            for (int draw = 0; draw < 500; draw++)
            {
                Assert.InRange(stream.NextBounded(bound), 0U, bound - 1U);
            }
        }
    }

    [Fact]
    public void NextBounded_distributes_a_small_range_evenly()
    {
        // Deterministic, not statistical: a fixed seed means this either passes or fails identically on
        // every run and platform. It catches gross errors in the modulo step; the rejection rule itself
        // is checked by the test below, because at bound 7 only four of 2^32 values are ever rejected.
        const uint bound = 7U;
        const int draws = 70_000;

        var stream = RandomStream.Derive(11UL, "distribution");
        int[] counts = new int[bound];

        for (int draw = 0; draw < draws; draw++)
        {
            counts[stream.NextBounded(bound)]++;
        }

        int expected = draws / (int)bound;

        foreach (int count in counts)
        {
            Assert.InRange(count, (int)(expected * 0.95), (int)(expected * 1.05));
        }
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
}
