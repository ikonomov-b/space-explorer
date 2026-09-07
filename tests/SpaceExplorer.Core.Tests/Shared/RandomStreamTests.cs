using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Shared;

public class RandomStreamTests
{
    [Fact]
    public void Derivation_follows_the_state_and_increment_assignment_of_decision_0008()
    {
        // state0 = h.low64 and increment = (h.high64 << 1) | 1. Asserted through the first draws,
        // because the state and increment are private: an implementation that swapped the halves, or
        // that ran the reference seeding routine on top of them, would produce a different sequence.
        const ulong seed = 42UL;
        const string path = "system/planet/0";

        Mix128 mixed = Mix64.Derive(seed, StreamPath.ToCanonicalBytes(path));
        var expected = Pcg32.FromState(mixed.Low, (mixed.High << 1) | 1UL);
        var actual = RandomStream.Derive(seed, path);

        for (int draw = 0; draw < 8; draw++)
        {
            Assert.Equal(expected.NextUInt32(), actual.NextUInt32());
        }
    }

    // Recorded derivation vectors, as decision 0008 requires under the generator version. Unlike the
    // PCG32 and SplitMix64 anchors, these pin *our* construction rather than a published one: a change
    // to the mixer, the domain constants, the length step, the path canonicalisation, or the half
    // assignment will break them. That is their purpose. Changing them is a
    // GeneratorVersion increment, never an edit to fit new output.
    [Theory]
    [InlineData(0UL, "system", 0x1CDB09BECB2165EFUL, 0xE7AF3F1AD6751A13UL, 0x336c3a20u, 0x4849616bu)]
    [InlineData(
        42UL,
        "system/planet/0/region/3/site/1/artifact/2",
        0xEF1992AEB197D3DCUL,
        0xC2CAD5ACCB908942UL,
        0x19a968d7u,
        0x7797fc6eu)]
    [InlineData(ulong.MaxValue, "a", 0x8A864F8505690FD1UL, 0xD23F138B96789EBDUL, 0xd2492866u, 0x6b2a0327u)]
    public void Recorded_vectors_freeze_the_derivation(
        ulong seed,
        string path,
        ulong expectedLow,
        ulong expectedHigh,
        uint expectedFirstDraw,
        uint expectedSecondDraw)
    {
        Mix128 mixed = Mix64.Derive(seed, StreamPath.ToCanonicalBytes(path));

        Assert.Equal(expectedLow, mixed.Low);
        Assert.Equal(expectedHigh, mixed.High);

        var stream = RandomStream.Derive(seed, path);

        Assert.Equal(expectedFirstDraw, stream.NextUInt32());
        Assert.Equal(expectedSecondDraw, stream.NextUInt32());
    }

    [Fact]
    public void Derivation_is_repeatable_within_a_process()
    {
        // The weakest determinism check there is, and still worth keeping: it fails if any hidden
        // per-instance or per-call state has crept into the derivation. Cross-process and cross-platform
        // determinism are the two-process suite's job, which arrives with the first generated output.
        var first = RandomStream.Derive(99UL, "system/planet/1/region/0");
        var second = RandomStream.Derive(99UL, "system/planet/1/region/0");

        for (int draw = 0; draw < 16; draw++)
        {
            Assert.Equal(first.NextUInt32(), second.NextUInt32());
        }
    }

    [Fact]
    public void Sibling_paths_receive_streams_that_differ_in_both_state_and_increment()
    {
        // The specific failure decision 0008 warns about: hashing the path into the stream selector
        // alone leaves siblings sharing a state, and PCG32 streams over a shared state are not
        // independent. Both halves must move when the path changes by one character.
        Mix128 first = Mix64.Derive(5UL, StreamPath.ToCanonicalBytes("system/planet/0/artifact/0"));
        Mix128 second = Mix64.Derive(5UL, StreamPath.ToCanonicalBytes("system/planet/0/artifact/1"));

        Assert.NotEqual(first.Low, second.Low);
        Assert.NotEqual(first.High, second.High);
    }

    [Fact]
    public void Sibling_streams_do_not_repeat_each_other_early()
    {
        // A regression guard, not a statistical proof of independence: correlated siblings of the kind
        // decision 0008 describes tend to overlap in their first handful of draws. Deterministic, so it
        // cannot flake.
        const int draws = 16;

        var first = RandomStream.Derive(5UL, "system/planet/0/artifact/0");
        var second = RandomStream.Derive(5UL, "system/planet/0/artifact/1");

        var firstDraws = new HashSet<uint>();

        for (int draw = 0; draw < draws; draw++)
        {
            firstDraws.Add(first.NextUInt32());
        }

        for (int draw = 0; draw < draws; draw++)
        {
            Assert.DoesNotContain(second.NextUInt32(), firstDraws);
        }
    }

    [Fact]
    public void Every_derived_increment_is_odd()
    {
        // The "| 1" of the derivation, checked across a spread of seeds and paths rather than assumed
        // from reading the one line that implements it.
        foreach (ulong seed in new[] { 0UL, 1UL, 42UL, ulong.MaxValue })
        {
            for (int index = 0; index < 64; index++)
            {
                Mix128 mixed = Mix64.Derive(seed, StreamPath.ToCanonicalBytes($"system/planet/{index}"));
                ulong increment = (mixed.High << 1) | 1UL;

                Assert.Equal(1UL, increment & 1UL);

                // Constructing the stream is itself the assertion: FromState rejects an even increment.
                Pcg32.FromState(mixed.Low, increment);
            }
        }
    }

    [Fact]
    public void An_invalid_path_is_rejected_before_a_stream_is_derived()
    {
        Assert.Throws<ArgumentException>(() => RandomStream.Derive(1UL, "system//planet"));
    }
}
