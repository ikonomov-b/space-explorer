using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Shared;

public class Mix64Tests
{
    [Fact]
    public void Finalize_matches_the_published_SplitMix64_finalizer()
    {
        // External anchor: SplitMix64 seeded with 0 advances its state by the golden-ratio gamma and
        // finalizes it, and its published first output is 0xE220A8397B1DCDAF. This pins the two
        // multipliers and three shift distances against a source outside this repository.
        Assert.Equal(0xE220A8397B1DCDAFUL, Mix64.Finalize(0x9E3779B97F4A7C15UL));
    }

    [Fact]
    public void Finalize_maps_zero_to_zero()
    {
        // A property of the finalizer, recorded so that it stays a known consequence rather than a
        // surprise: a mix that collapses to zero is not a special case anywhere.
        Assert.Equal(0UL, Mix64.Finalize(0UL));
    }

    [Fact]
    public void Derive_separates_inputs_that_differ_only_in_trailing_zero_bytes()
    {
        // Both inputs pad to the same single little-endian chunk, so without the length absorbed into
        // the mix they would produce identical output. This is the collision the length step removes.
        Mix128 oneByte = Mix64.Derive(0UL, new byte[] { 0x61 });
        Mix128 paddedToTwo = Mix64.Derive(0UL, new byte[] { 0x61, 0x00 });

        Assert.NotEqual(oneByte, paddedToTwo);
    }

    [Fact]
    public void Derive_separates_inputs_at_the_chunk_boundary()
    {
        // Eight bytes fill one chunk exactly and nine begin a second, the point where an off-by-one in
        // the absorb loop would drop the ninth byte entirely.
        Mix128 eight = Mix64.Derive(0UL, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        Mix128 nine = Mix64.Derive(0UL, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });

        Assert.NotEqual(eight, nine);
    }

    [Fact]
    public void Derive_produces_distinct_halves()
    {
        // The two halves must come from different domain constants. If the construction lost that
        // separation, every derived stream would have an increment mechanically tied to its state.
        foreach (string path in new[] { "system", "system/planet/0", "a", "artifact/2" })
        {
            Mix128 mixed = Mix64.Derive(42UL, StreamPath.ToCanonicalBytes(path));

            Assert.NotEqual(mixed.Low, mixed.High);
        }
    }

    [Fact]
    public void Derive_responds_to_the_seed_as_well_as_the_data()
    {
        Mix128 seedZero = Mix64.Derive(0UL, StreamPath.ToCanonicalBytes("system"));
        Mix128 seedOne = Mix64.Derive(1UL, StreamPath.ToCanonicalBytes("system"));

        Assert.NotEqual(seedZero, seedOne);
    }
}
