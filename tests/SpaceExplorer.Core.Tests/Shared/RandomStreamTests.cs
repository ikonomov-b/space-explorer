using System.Buffers.Binary;
using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Shared;

public class RandomStreamTests
{
    [Fact]
    public void The_halves_are_the_first_sixteen_bytes_of_the_digest_in_order()
    {
        // The assignment of decision 0008, now over a SHA-256 digest (decision 0021): the first eight
        // bytes seed the state and the next eight, shifted odd, select the stream. Rebuilding the
        // hashed record here also documents its preimage: domain, seed, then the path as a
        // length-prefixed byte string.
        const ulong seed = 42UL;
        const string path = "system/planet/0";

        var record = new CanonicalWriter("random-stream/2");
        record.WriteUInt64(seed);
        record.WriteBytes(StreamPath.ToCanonicalBytes(path));
        ReadOnlySpan<byte> digest = record.ToContentHash().Bytes;

        (ulong state, ulong increment) = RandomStream.DeriveParts(seed, path);

        Assert.Equal(BinaryPrimitives.ReadUInt64LittleEndian(digest[..8]), state);
        Assert.Equal((BinaryPrimitives.ReadUInt64LittleEndian(digest[8..16]) << 1) | 1UL, increment);

        // And the stream really is initialised from those two, rather than run through the reference
        // seeding routine on top of them, which would change every draw.
        var expected = Pcg32.FromState(state, increment);
        var actual = RandomStream.Derive(seed, path);

        for (int draw = 0; draw < 8; draw++)
        {
            Assert.Equal(expected.NextUInt32(), actual.NextUInt32());
        }
    }

    // Recorded derivation vectors, as decision 0008 requires under the generator version. The digests
    // were computed outside this repository from the byte string the preimage test documents, so these
    // check the implementation rather than merely record it. A change to the domain label, the seed
    // encoding, the path canonicalisation, the framing, or the half assignment breaks them. That is
    // their purpose. Changing them is a GeneratorVersion increment, never an edit to fit new output.
    [Theory]
    [InlineData(
        0UL,
        "system",
        "f1e2f37f6c987c1f5717dd691d90ee6a1a67438519d85100b30d232de4af90b0",
        0x1F7C986C7FF3E2F1UL,
        0xD5DD203AD3BA2EAFUL,
        0x7df27ecdu,
        0x0dc53df9u)]
    [InlineData(
        42UL,
        "system/planet/0/region/3/site/1/artifact/2",
        "1c4db2454834f46e1f8da4683744238d39c99cfe43c9aa02e37340eee74aaabd",
        0x6EF4344845B24D1CUL,
        0x1A46886ED1491A3FUL,
        0xf54ef42fu,
        0x5d9ea18eu)]
    [InlineData(
        ulong.MaxValue,
        "a",
        "aee118053ad9298b5323aeb2ac80ef5b10c7060b81bea45fdf8d71ca22e96ad1",
        0x8B29D93A0518E1AEUL,
        0xB7DF0159655C46A7UL,
        0xbf07329fu,
        0x57d5cdccu)]
    public void Recorded_vectors_freeze_the_derivation(
        ulong seed,
        string path,
        string expectedDigest,
        ulong expectedState,
        ulong expectedIncrement,
        uint expectedFirstDraw,
        uint expectedSecondDraw)
    {
        var record = new CanonicalWriter("random-stream/2");
        record.WriteUInt64(seed);
        record.WriteBytes(StreamPath.ToCanonicalBytes(path));

        Assert.Equal(expectedDigest, record.ToContentHash().ToString());

        (ulong state, ulong increment) = RandomStream.DeriveParts(seed, path);

        Assert.Equal(expectedState, state);
        Assert.Equal(expectedIncrement, increment);

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
        (ulong firstState, ulong firstIncrement) = RandomStream.DeriveParts(5UL, "system/planet/0/artifact/0");
        (ulong secondState, ulong secondIncrement) = RandomStream.DeriveParts(5UL, "system/planet/0/artifact/1");

        Assert.NotEqual(firstState, secondState);
        Assert.NotEqual(firstIncrement, secondIncrement);
    }

    [Fact]
    public void A_seed_and_a_path_cannot_trade_bytes_with_each_other()
    {
        // The framing the canonical record supplies: the path is length-prefixed, so no seed and path
        // pair can hash the same bytes as a different pair. Without the prefix these two would differ
        // only in where the reader believed the seed ended.
        Assert.NotEqual(
            RandomStream.DeriveParts(0UL, "ab"),
            RandomStream.DeriveParts(0UL, "a"));
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
                (ulong state, ulong increment) = RandomStream.DeriveParts(seed, $"system/planet/{index}");

                Assert.Equal(1UL, increment & 1UL);

                // Constructing the stream is itself the assertion: FromState rejects an even increment.
                Pcg32.FromState(state, increment);
            }
        }
    }

    [Fact]
    public void An_invalid_path_is_rejected_before_a_stream_is_derived()
    {
        Assert.Throws<ArgumentException>(() => RandomStream.Derive(1UL, "system//planet"));
    }
}
