using SpaceExplorer.Core.Derivation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Derivation;

/// <summary>
/// The stored ground of a region
/// ([decision 0066](../../../docs/decisions/0066-generation-writes-to-the-database-and-the-view-only-reads.md)):
/// a canonical record like any other, compacted by arithmetic this repository owns rather than by a
/// compressor whose output could move under it and silently rename every stored surface.
/// </summary>
public class RegionPayloadTests
{
    private static readonly ReliefField Field = new(
        ReferenceRadiusUnits: 1_383_000L * 256,
        AmplitudeMetres: 451,
        RoughnessFraction: 31_457,
        WavelengthMetres: 2_262,
        CompositionSeed: 14_299_526_199_266_706_610UL,
        InstancePath: "solar-system/orbit/0",
        RidgingFraction: 50_923);

    [Theory]
    [InlineData(64)]
    [InlineData(256)]
    [InlineData(2_048)]
    public void Ground_survives_the_round_trip_exactly(long extent)
    {
        // Lossless is the whole requirement: a surface that came back approximately would be a different
        // world from the one the rule made, and the rule's own recorded digest would no longer describe it.
        short[] heights = TerrainHeightfield.Sample(Field, 170_000_000, 650_000_000, 90_000_000, extent);
        RegionPayload payload = RegionPayload.Of("solar-system/orbit/0/surface-anchor/0", TerrainHeightfield.RidgedRule, Field.Hash, extent, heights);

        RegionPayload back = RegionPayload.Decode(payload.Bytes);

        Assert.Equal(heights, back.Heights);
        Assert.Equal(payload.InstancePath, back.InstancePath);
        Assert.Equal(payload.Rule, back.Rule);
        Assert.Equal(payload.FieldHash, back.FieldHash);
        Assert.Equal(extent, back.ExtentMetres);
        Assert.Equal(payload.Hash, back.Hash);
    }

    [Fact]
    public void Real_ground_stores_in_well_under_half_the_bytes_its_samples_take()
    {
        // The figures decision 0066 cites, as a test rather than as a claim in prose: if a change to the
        // predictor or the packing quietly gave most of this back, the record would be wrong about its own
        // cost and the campaign budget it was measured against.
        foreach ((long amplitude, long ridging, double ceiling) in ((long, long, double)[])[
            (124, 1_310, 0.25),      // ocean: flat
            (451, 0, 0.35),          // smooth
            (451, 50_923, 0.45)])    // ridged rock
        {
            ReliefField field = Field with { AmplitudeMetres = amplitude, RidgingFraction = ridging };
            short[] heights = TerrainHeightfield.Sample(field, 170_000_000, 650_000_000, 90_000_000, 2_048);
            RegionPayload payload = RegionPayload.Of("solar-system/orbit/0/surface-anchor/0", TerrainHeightfield.RidgedRule, field.Hash, 2_048, heights);

            double share = payload.Bytes.Length / (double)(heights.Length * 2);
            Assert.True(share < ceiling, $"ground of amplitude {amplitude} and ridging {ridging} stored at {share:P1} of raw, over the {ceiling:P0} this pins");
        }
    }

    [Fact]
    public void The_worst_ground_this_generator_draws_packs_within_the_budgeted_ceiling()
    {
        // What [decision 0069](../../../docs/decisions/0069-decision-0068s-ceiling-is-the-generators-and-the-format-admits-18-bits.md)
        // asks to be a test rather than a reading, because the number was got wrong twice in prose before
        // it was got right: decision 0068 budgets a campaign on a per-region ceiling of 17 bits a residual,
        // which is what the vocabulary's extremes reach and not what the format admits. A residual is one
        // int16 less the planar prediction of three others, so it spans ±131,070 and eighteen bits
        // zig-zagged — reachable only where four neighbouring samples sit at opposite amplitude extremes,
        // which smoothstep interpolation over a 4 m finest wavelength cannot produce at 2 m spacing.
        //
        // If this goes red, the ceiling has moved and decisions 0068 and 0069 are to be revisited by name:
        // the generator, the amplitude cap, or the sample format has changed under them.
        ReliefField worst = Field with
        {
            AmplitudeMetres = CategoryRegistryRevision6.MaximumAmplitudeMetres,
            RoughnessFraction = ReliefField.RoughnessUnit,
            WavelengthMetres = ReliefField.FinestWavelengthMetres,
            RidgingFraction = ReliefField.RoughnessUnit,
        };

        short[] heights = TerrainHeightfield.Sample(worst, 170_000_000, 650_000_000, 90_000_000, 2_048);
        int across = TerrainHeightfield.SamplesAcross(2_048);

        int widest = 0;
        for (int row = 0; row < across; row++)
        {
            for (int column = 0; column < across; column++)
            {
                int index = (row * across) + column;
                long prediction = row == 0
                    ? (column == 0 ? 0 : heights[index - 1])
                    : column == 0
                        ? heights[index - across]
                        : heights[index - 1] + heights[index - across] - heights[index - across - 1];

                long zigZagged = ((heights[index] - prediction) << 1) ^ ((heights[index] - prediction) >> 63);
                int bits = 0;
                for (long value = zigZagged; value > 0; value >>= 1)
                {
                    bits++;
                }

                widest = Math.Max(widest, bits);
            }
        }

        Assert.Equal(17, widest);
        Assert.InRange(RegionPayload.Of("solar-system/orbit/0/surface-anchor/0", TerrainHeightfield.RidgedRule, worst.Hash, 2_048, heights).Bytes.Length, 1, 2_234_560);
    }

    [Fact]
    public void One_ground_is_one_record_whoever_writes_it()
    {
        // Two runs of one generation must publish one file rather than two, which is what makes a revisit
        // find what the first visit wrote.
        short[] first = TerrainHeightfield.Sample(Field, 12_345, -6_789, 1_000, 256);
        short[] second = TerrainHeightfield.Sample(Field, 12_345, -6_789, 1_000, 256);

        RegionPayload one = RegionPayload.Of("solar-system/orbit/2/surface-anchor/0", TerrainHeightfield.RidgedRule, Field.Hash, 256, first);
        RegionPayload other = RegionPayload.Of("solar-system/orbit/2/surface-anchor/0", TerrainHeightfield.RidgedRule, Field.Hash, 256, second);

        Assert.Equal(one.Hash, other.Hash);
        Assert.Equal(one.Bytes, other.Bytes);
    }

    [Fact]
    public void A_payload_of_another_world_is_a_different_record()
    {
        // The field hash is in the record precisely so a stored surface cannot be passed off as the ground
        // of content whose relief parameters have since moved.
        short[] heights = TerrainHeightfield.Sample(Field, 0, 0, 0, 64);
        ReliefField moved = Field with { AmplitudeMetres = Field.AmplitudeMetres + 1 };

        Assert.NotEqual(
            RegionPayload.Of("solar-system/orbit/0/surface-anchor/0", TerrainHeightfield.RidgedRule, Field.Hash, 64, heights).Hash,
            RegionPayload.Of("solar-system/orbit/0/surface-anchor/0", TerrainHeightfield.RidgedRule, moved.Hash, 64, heights).Hash);
    }

    [Fact]
    public void A_record_that_is_not_a_payload_is_refused_by_name()
    {
        short[] heights = TerrainHeightfield.Sample(Field, 0, 0, 0, 64);
        byte[] bytes = RegionPayload.Of("solar-system/orbit/0/surface-anchor/0", TerrainHeightfield.RidgedRule, Field.Hash, 64, heights).Bytes;

        Assert.Throws<FormatException>(() => RegionPayload.Decode([.. bytes, 0]));
        Assert.Throws<FormatException>(() => RegionPayload.Decode(bytes[..^1]));

        // Padding bits are part of the record: a decoder that ignored them would accept two spellings of
        // one surface, and a content-addressed store cannot have two names for one thing.
        byte[] padded = [.. bytes];
        padded[^1] ^= 0x80;
        Assert.Throws<FormatException>(() => RegionPayload.Decode(padded));
    }

    [Fact]
    public void Heights_that_are_not_the_square_the_extent_calls_for_are_refused()
    {
        Assert.Throws<ArgumentException>(() =>
            RegionPayload.Of("solar-system/orbit/0/surface-anchor/0", TerrainHeightfield.RidgedRule, ContentHash.Of([1]), 64, new short[10]));
    }
}
