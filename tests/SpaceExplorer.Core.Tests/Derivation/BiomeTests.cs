using SpaceExplorer.Core.Derivation;
using Xunit;

namespace SpaceExplorer.Core.Tests.Derivation;

/// <summary>
/// Rung 3's two rules
/// ([decision 0070](../../../docs/decisions/0070-a-biome-is-a-derived-set-registry-revision-9-and-grammar-version-10.md)):
/// `biome-patches/1`, which draws what is stored, and `derive-biome-index/1`, which turns that plus the
/// heights and the sea datum into what a renderer draws and nothing stores.
/// </summary>
public class BiomeTests
{
    private const string RegionPath = "solar-system/orbit/0/surface-anchor/0";
    private const ulong Seed = 14_299_526_199_266_706_610UL;

    private static readonly ReliefField Field = new(
        ReferenceRadiusUnits: 1_383_000L * 256,
        AmplitudeMetres: 451,
        RoughnessFraction: 31_457,
        WavelengthMetres: 2_262,
        CompositionSeed: Seed,
        InstancePath: "solar-system/orbit/0",
        RidgingFraction: 50_923);

    /// <summary>A palette of four: a sea floor, then three bands of land that overlap deliberately.</summary>
    private static readonly BiomeClaim[] Palette =
    [
        new(Submerged: true, -65_535, 65_535),
        new(Submerged: false, -65_535, 10_000),
        new(Submerged: false, 5_000, 40_000),
        new(Submerged: false, 30_000, 65_535),
    ];

    [Theory]
    [InlineData(256, 1)]
    [InlineData(512, 4)]
    [InlineData(1_024, 16)]
    [InlineData(2_048, 64)]
    public void The_patch_count_follows_from_the_extent_rather_than_being_chosen(long extent, int expected)
    {
        // One patch per 256 m of extent per axis, so the mean spacing is 256 m at every extent — 128 cells,
        // about two minutes for a walker to cross at decision 0017's pace, whatever size the region is.
        Assert.Equal(expected, BiomePatches.CountFor(extent));
        Assert.Equal(expected, BiomePatches.Draw(Seed, RegionPath, extent, 4).Length);
    }

    [Fact]
    public void One_region_draws_one_set_of_patches_and_another_draws_its_own()
    {
        // The patches are a function of the composition seed and the region's own path, so a revisit finds
        // the same ground and a second region of the same planet finds its own.
        BiomePatch[] once = BiomePatches.Draw(Seed, RegionPath, 2_048, 4);
        BiomePatch[] again = BiomePatches.Draw(Seed, RegionPath, 2_048, 4);
        BiomePatch[] elsewhere = BiomePatches.Draw(Seed, "solar-system/orbit/1/surface-anchor/0", 2_048, 4);

        Assert.Equal(once, again);
        Assert.NotEqual(once, elsewhere);
        Assert.NotEqual(once, BiomePatches.Draw(Seed + 1, RegionPath, 2_048, 4));
    }

    [Fact]
    public void Every_patch_lies_in_the_region_and_names_a_biome_of_the_palette()
    {
        BiomePatch[] patches = BiomePatches.Draw(Seed, RegionPath, 2_048, 4);
        long half = 2_048 * 256 / 2;

        Assert.All(patches, patch =>
        {
            Assert.InRange(patch.Ordinal, 0, 3);
            Assert.InRange(patch.CentreX, -half, half);
            Assert.InRange(patch.CentreZ, -half, half);
            Assert.InRange(patch.ReachMetres, BiomePatches.MinimumReachMetres, BiomePatches.MaximumReachMetres);
        });
    }

    [Fact]
    public void The_index_is_total_and_one_byte_a_cell()
    {
        // Totality is the claim that keeps decision 0063 clause 15's retry deferral alive for another rung:
        // no cell and therefore no region can fail, so nothing has to be re-drawn.
        short[] heights = TerrainHeightfield.Sample(Field, 170_000_000, 650_000_000, 90_000_000, 512);
        BiomePatch[] patches = BiomePatches.Draw(Seed, RegionPath, 512, Palette.Length);

        byte[] index = BiomeIndex.Derive(heights, 512, patches, Palette, -1_024, Field.AmplitudeMetres);

        int cells = TerrainHeightfield.SamplesAcross(512) - 1;
        Assert.Equal(cells * cells, index.Length);
        Assert.All(index, at => Assert.InRange(at, 0, Palette.Length - 1));
    }

    [Fact]
    public void A_sea_floor_claims_what_is_under_the_sea_and_nothing_above_it()
    {
        // The fault this caught when it was measured rather than assumed: a submerged biome whose band is
        // the widest one wins land cells on distance alone, and the sea becomes invisible — one took 62% of
        // the cells at every datum from none to sixty metres before the land rule excluded it.
        short[] heights = TerrainHeightfield.Sample(Field, 170_000_000, 650_000_000, 90_000_000, 512);
        BiomePatch[] patches = BiomePatches.Draw(Seed, RegionPath, 512, Palette.Length);

        byte[] dry = BiomeIndex.Derive(heights, 512, patches, Palette, -1_024, Field.AmplitudeMetres);
        Assert.DoesNotContain((byte)0, dry);

        // Every cell of this region is under a datum above its highest sample, and none under one below its
        // lowest, so the datum decides the shoreline rather than merely tinting it.
        long highest = heights.Max() / ReliefField.HeightUnit;
        long lowest = heights.Min() / ReliefField.HeightUnit;
        Assert.All(BiomeIndex.Derive(heights, 512, patches, Palette, highest + 1, Field.AmplitudeMetres), at => Assert.Equal(0, at));
        Assert.DoesNotContain((byte)0, BiomeIndex.Derive(heights, 512, patches, Palette, lowest - 1, Field.AmplitudeMetres));
    }

    [Fact]
    public void A_palette_of_one_land_biome_paints_every_land_cell_with_it()
    {
        // The other half of totality: where no biome admits a cell's elevation the nearest patch of any
        // kind claims it, so a palette whose bands leave a gap still covers the ground.
        short[] heights = TerrainHeightfield.Sample(Field, 0, 0, 0, 256);
        BiomeClaim[] narrow = [new(Submerged: false, 60_000, 65_535)];
        BiomePatch[] patches = BiomePatches.Draw(Seed, RegionPath, 256, 1);

        Assert.All(BiomeIndex.Derive(heights, 256, patches, narrow, -1_024, Field.AmplitudeMetres), at => Assert.Equal(0, at));
    }

    [Fact]
    public void A_patch_naming_a_biome_the_palette_does_not_hold_is_refused_by_name()
    {
        short[] heights = TerrainHeightfield.Sample(Field, 0, 0, 0, 256);
        BiomePatch[] beyond = [new BiomePatch(9, 0, 0, 256)];

        Assert.Throws<ArgumentException>(() => BiomeIndex.Derive(heights, 256, beyond, Palette, -1_024, Field.AmplitudeMetres));
        Assert.Throws<ArgumentException>(() => BiomeIndex.Derive(heights, 256, [], [], -1_024, Field.AmplitudeMetres));
    }

    [Fact]
    public void The_payload_grows_a_version_and_leaves_version_one_where_it_was()
    {
        // Decision 0050's rule, which is what lets surface cycles one to three reproduce from their records
        // after this rung moved the format: a version's added field is written only from that version on.
        short[] heights = TerrainHeightfield.Sample(Field, 0, 0, 0, 256);
        BiomePatch[] patches = BiomePatches.Draw(Seed, RegionPath, 256, 4);

        RegionPayload one = RegionPayload.Of(RegionPath, TerrainHeightfield.RidgedRule, Field.Hash, 256, heights);
        RegionPayload two = RegionPayload.Of(RegionPath, TerrainHeightfield.RidgedRule, Field.Hash, 256, heights, BiomePatches.Rule, patches);

        Assert.Equal(1, one.Version);
        Assert.Equal(2, two.Version);
        Assert.NotEqual(one.Hash, two.Hash);

        RegionPayload back = RegionPayload.Decode(two.Bytes);
        Assert.Equal(2, back.Version);
        Assert.Equal(BiomePatches.Rule, back.PatchRule);
        Assert.Equal(patches, back.Patches);
        Assert.Equal(heights, back.Heights);

        // A version 1 record still decodes as one, which is the claim that matters for the earlier cycles.
        Assert.Equal(1, RegionPayload.Decode(one.Bytes).Version);
        Assert.Equal(one.Hash, RegionPayload.Decode(one.Bytes).Hash);
    }

    [Fact]
    public void The_patches_cost_what_decision_0070_says_they_cost()
    {
        // Clause 8 states under 640 B a region permanent, against decision 0068's 2.13 MiB ceiling. If the
        // patch record grew, the budget that record was written against would be wrong.
        short[] heights = TerrainHeightfield.Sample(Field, 170_000_000, 650_000_000, 90_000_000, 2_048);
        BiomePatch[] patches = BiomePatches.Draw(Seed, RegionPath, 2_048, 6);

        int withPatches = RegionPayload.Of(RegionPath, TerrainHeightfield.RidgedRule, Field.Hash, 2_048, heights, BiomePatches.Rule, patches).Bytes.Length;
        int without = RegionPayload.Of(RegionPath, TerrainHeightfield.RidgedRule, Field.Hash, 2_048, heights).Bytes.Length;

        Assert.InRange(withPatches - without, 1, 640);
    }

    [Fact]
    public void A_payload_carrying_more_patches_than_the_derived_budget_is_refused()
    {
        short[] heights = TerrainHeightfield.Sample(Field, 0, 0, 0, 256);
        BiomePatch[] tooMany = [.. Enumerable.Range(0, RegionPayload.MaxPatches + 1).Select(_ => new BiomePatch(0, 0, 0, 256))];

        Assert.Throws<ArgumentException>(() =>
            RegionPayload.Of(RegionPath, TerrainHeightfield.RidgedRule, Field.Hash, 256, heights, BiomePatches.Rule, tooMany));
    }
}
