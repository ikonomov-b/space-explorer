using System.Security.Cryptography;
using SpaceExplorer.Core.Derivation;
using Xunit;

namespace SpaceExplorer.Core.Tests.Derivation;

/// <summary>
/// `terrain-heightfield/1`, the rule that turns a planet's relief field into the ground of one region
/// ([decision 0063](../../../docs/decisions/0063-relief-by-terrain-heightfield-registry-revision-6-and-grammar-version-7.md)).
/// Four vectors, no two of which rest on the same thing: a hand case an implementation outside this
/// repository produced, a published anchor, a portability digest continuous integration compares on both
/// operating systems, and two properties the arithmetic claims.
/// </summary>
public class TerrainHeightfieldTests
{
    /// <summary>The hand case's record: a minimum-radius body at one octave, so all sixteen samples can be written out.</summary>
    private static readonly ReliefField HandCase = new(
        ReferenceRadiusUnits: 524_288L * 256,
        AmplitudeMetres: 100,
        RoughnessFraction: 32_768,
        WavelengthMetres: ReliefField.FinestWavelengthMetres,
        CompositionSeed: 42,
        InstancePath: "solar-system/orbit/0");

    /// <summary>A maximal region's record, for the digest that makes the landscape portable rather than only repeatable.</summary>
    private static readonly ReliefField Portable = new(
        ReferenceRadiusUnits: 1_383_000L * 256,
        AmplitudeMetres: 240,
        RoughnessFraction: 41_000,
        WavelengthMetres: 2_048,
        CompositionSeed: 14_299_526_199_266_706_610UL,
        InstancePath: "solar-system/orbit/0");

    [Fact]
    public void The_hand_case_is_what_an_implementation_outside_this_repository_computes()
    {
        // Written out in full because sixteen numbers can be checked by hand against clause 6, and
        // produced by a second implementation of that clause rather than by this one: what it pins is the
        // arithmetic below the seed — the frame, the lattice, the smoothstep, the octave sum, and the
        // rounding — where decision 0020's own vectors pin the canonical encoding above it.
        short[] heights = TerrainHeightfield.Sample(HandCase, latitude: 0, longitude: 0, heading: 0, extentMetres: 6);

        Assert.Equal(4, TerrainHeightfield.SamplesAcross(6));
        Assert.Equal(
            [-205, 458, 55, -348, -150, 243, 245, 247, -293, 158, -158, -474, -435, 72, -561, -1194],
            heights);
    }

    [Fact]
    public void The_lattice_is_continuous_where_a_coordinate_is_negative()
    {
        // The same hand case in the far octant, where every planet-fixed coordinate is negative and the
        // wavelength does not divide the position exactly. It is a case of its own because the first one
        // cannot catch a truncating division: at the finest wavelength the position divides exactly, so
        // floor and truncation agree, and over half a sphere they do not. Also produced outside this
        // repository.
        ReliefField field = HandCase with { WavelengthMetres = 2_048 };
        short[] heights = TerrainHeightfield.Sample(field, latitude: -300_000_000, longitude: -1_500_000_000, heading: 40_000_000, extentMetres: 6);

        Assert.Equal(
            [367, 369, 371, 373, 366, 369, 371, 373, 365, 367, 370, 373, 364, 366, 368, 371],
            heights);
    }

    [Fact]
    public void The_lattice_finaliser_is_splitmix64s_published_first_output()
    {
        // The one hash construction this rule contains rests on a value computed outside this repository:
        // SplitMix64's first output for seed 0, which is the finaliser applied to the golden-ratio
        // increment. `derive-surface-raster/1` has no such anchor, which decision 0056 records as its own
        // weakness and this rule does not repeat.
        Assert.Equal(0xE220A8397B1DCDAFUL, SplitMix64Finalise(0x9E3779B97F4A7C15UL));
    }

    [Theory]
    // A turn is 2^32, so an eighth, a sixth, a quarter and a half of one are these.
    [InlineData(1 << 29, 759_250_125L)]
    [InlineData((int)((1L << 32) / 6), 929_887_697L)]
    [InlineData(1 << 30, 1_073_741_824L)]
    [InlineData(int.MinValue, 0L)]
    public void The_integer_sine_holds_against_its_published_values(int turn, long expected)
    {
        // Within a unit at a scale of 2^30, which is what the truncated series and the fixed-point shifts
        // together give, and four orders of magnitude finer than a metre on the largest body drawn.
        Assert.InRange(IntegerTrigonometry.Sin(turn), expected - 1, expected + 1);
    }

    [Fact]
    public void One_maximal_region_is_one_recorded_digest()
    {
        // What makes this evidence of a portable landscape rather than of a repeatable one is continuous
        // integration computing it on `ubuntu-latest` and on `windows-latest`.
        short[] heights = TerrainHeightfield.Sample(Portable, 170_000_000, 650_000_000, 90_000_000, 2_048);

        Assert.Equal(1_025 * 1_025, heights.Length);
        Assert.Equal("b259a742f5494ae8c6eee14112869265dc77b6cf94c368afc6315b1bded725b4", Digest(heights));
    }

    [Fact]
    public void No_sample_leaves_the_stored_amplitude()
    {
        // Clause 6's bound is load-bearing rather than a claim: the octave weights are normalised to the
        // stored amplitude, so an int16 at 1/16 m cannot saturate and rungs 3 and 5 keep their headroom.
        foreach (long roughness in (long[])[0, 1, 32_768, 65_534, ReliefField.RoughnessUnit])
        {
            ReliefField field = Portable with { RoughnessFraction = roughness };
            short[] heights = TerrainHeightfield.Sample(field, 12_345_678, -987_654_321, 55_555, 256);

            long bound = field.AmplitudeMetres * ReliefField.HeightUnit;
            Assert.All(heights, height => Assert.InRange((long)height, -bound, bound));
        }
    }

    [Fact]
    public void Two_regions_of_one_planet_agree_on_the_ground_they_share()
    {
        // The field is evaluated in the planet-fixed frame, so it belongs to the planet and not to the
        // region: two extents about one anchor describe the same ground where they overlap, which is what
        // lets rung 5's far field continue a region rather than invent a second landscape beside it.
        short[] small = TerrainHeightfield.Sample(Portable, 170_000_000, 650_000_000, 90_000_000, 256);
        short[] large = TerrainHeightfield.Sample(Portable, 170_000_000, 650_000_000, 90_000_000, 1_024);

        int smallSide = TerrainHeightfield.SamplesAcross(256);
        int largeSide = TerrainHeightfield.SamplesAcross(1_024);
        int offset = (largeSide - smallSide) / 2;

        for (int j = 0; j < smallSide; j++)
        {
            for (int i = 0; i < smallSide; i++)
            {
                Assert.Equal(small[(j * smallSide) + i], large[((j + offset) * largeSide) + i + offset]);
            }
        }
    }

    [Fact]
    public void Turning_a_region_turns_the_frame_and_not_the_ground()
    {
        // The sharpest statement of clause 3 available without a second anchor: under a heading a quarter
        // turn on, clause 4's axes give X̂' = Ẑ and Ẑ' = −X̂, so the sample at local (x, z) is the sample
        // at (−z, x) before — the same planet-fixed ground, indexed differently. A field evaluated in the
        // region's own tangent frame would be unmoved by the heading instead, which is the reading rung 5
        // could not have lived with.
        const int quarterTurn = 1 << 30;
        const long extent = 512;

        short[] straight = TerrainHeightfield.Sample(Portable, 170_000_000, 650_000_000, 90_000_000, extent);
        short[] turned = TerrainHeightfield.Sample(Portable, 170_000_000, 650_000_000, 90_000_000 + quarterTurn, extent);

        int side = TerrainHeightfield.SamplesAcross(extent);
        int half = (side - 1) / 2;
        for (int j = 0; j < side; j++)
        {
            for (int i = 0; i < side; i++)
            {
                Assert.Equal(straight[((half + i - half) * side) + half - (j - half)], turned[(j * side) + i]);
            }
        }

        Assert.NotEqual(straight, TerrainHeightfield.Sample(Portable, 175_000_000, 650_000_000, 90_000_000, extent));
    }

    [Fact]
    public void A_field_belongs_to_an_instance_rather_than_to_a_definition()
    {
        // Two planets of one definition in one system carry the same parameters and must not carry the
        // same landscape, which is what the composition seed and the instance path in the record are for.
        ReliefField elsewhere = Portable with { InstancePath = "solar-system/orbit/1" };
        ReliefField later = Portable with { CompositionSeed = Portable.CompositionSeed + 1 };

        Assert.NotEqual(Portable.Hash, elsewhere.Hash);
        Assert.NotEqual(Portable.Hash, later.Hash);
        Assert.NotEqual(
            TerrainHeightfield.Sample(Portable, 0, 0, 0, 64),
            TerrainHeightfield.Sample(elsewhere, 0, 0, 0, 64));
    }

    [Fact]
    public void The_octave_count_follows_from_the_wavelength_rather_than_being_chosen()
    {
        // The finest octave is always 4 m, twice decision 0010's cell, whatever the coarsest is.
        Assert.Equal(1, (Portable with { WavelengthMetres = 4 }).Octaves);
        Assert.Equal(2, (Portable with { WavelengthMetres = 8 }).Octaves);
        Assert.Equal(10, (Portable with { WavelengthMetres = 2_048 }).Octaves);
        Assert.Equal(12, (Portable with { WavelengthMetres = 8_192 }).Octaves);
    }

    [Fact]
    public void An_extent_that_is_not_whole_cells_is_refused_by_name()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TerrainHeightfield.Sample(Portable, 0, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => TerrainHeightfield.Sample(Portable, 0, 0, 0, 2_047));
    }

    [Fact]
    public void SampleFar_at_the_regions_own_cell_is_Sample_exactly()
    {
        // Rung 5's far field is the same construction at a coarser stride, not a second rule: asked for the
        // region's own 2 m cell, it is byte-identical to Sample rather than merely close to it.
        short[] region = TerrainHeightfield.Sample(Portable, 170_000_000, 650_000_000, 90_000_000, 1_024);
        short[] far = TerrainHeightfield.SampleFar(Portable, 170_000_000, 650_000_000, 90_000_000, 1_024, TerrainHeightfield.CellMetres);

        Assert.Equal(region, far);
    }

    [Fact]
    public void Two_far_fields_of_one_planet_agree_on_the_ground_they_share()
    {
        // The same property the region's own test pins, at the far field's own stride: two far extents about
        // one anchor describe the same ground where their samples coincide, so the generalization is a
        // property of the planet and not of the patch — which is what lets the far field be re-cut at any
        // extent without the landscape moving under it.
        short[] small = TerrainHeightfield.SampleFar(Portable, 170_000_000, 650_000_000, 90_000_000, 2_048, 64);
        short[] large = TerrainHeightfield.SampleFar(Portable, 170_000_000, 650_000_000, 90_000_000, 8_192, 64);

        int smallSide = (2_048 / 64) + 1;
        int largeSide = (8_192 / 64) + 1;
        int offset = (largeSide - smallSide) / 2;

        for (int j = 0; j < smallSide; j++)
        {
            for (int i = 0; i < smallSide; i++)
            {
                Assert.Equal(small[(j * smallSide) + i], large[((j + offset) * largeSide) + i + offset]);
            }
        }
    }

    [Fact]
    public void A_coarser_stride_drops_the_detail_it_cannot_carry_rather_than_folding_it()
    {
        // Generalizing is a band limit, not a sparse read of the same octaves. At the anchor — the one point
        // every stride passes through — a coarse far field differs from the region's own ground by no more
        // than the amplitude of the octaves it declines to carry, and differs at all, because detail really
        // was dropped. Point-sampling instead would fold those octaves into false coarse relief, which is
        // the landscape a reader would then be asked to judge.
        short[] region = TerrainHeightfield.Sample(Portable, 170_000_000, 650_000_000, 90_000_000, 256);
        short[] far = TerrainHeightfield.SampleFar(Portable, 170_000_000, 650_000_000, 90_000_000, 8_192, 64);

        int regionAcross = TerrainHeightfield.SamplesAcross(256);
        int regionCentre = (((regionAcross - 1) / 2) * regionAcross) + ((regionAcross - 1) / 2);
        int farAcross = (8_192 / 64) + 1;
        int farCentre = (((farAcross - 1) / 2) * farAcross) + ((farAcross - 1) / 2);

        // Portable's coarsest wavelength is 2,048 m over 10 octaves, so a 64 m stride carries the wavelengths
        // down to 128 m — five octaves — and drops the five below it.
        long dropped = DroppedAmplitudeUnits(Portable, carried: 5);

        Assert.NotEqual(region[regionCentre], far[farCentre]);
        Assert.InRange(Math.Abs(region[regionCentre] - far[farCentre]), 1, dropped);
    }

    [Fact]
    public void A_far_field_stays_inside_the_stored_amplitude()
    {
        short[] far = TerrainHeightfield.SampleFar(Portable, 12_345_678, -987_654_321, 55_555, 8_192, 64);
        long bound = Portable.AmplitudeMetres * ReliefField.HeightUnit;

        Assert.All(far, height => Assert.InRange((long)height, -bound, bound));
    }

    /// <summary>The height, in 1/16 m, that the octaves past <paramref name="carried"/> could have contributed.</summary>
    private static long DroppedAmplitudeUnits(ReliefField field, int carried)
    {
        var weights = new List<long>();
        long weight = 1L << 16;
        for (int octave = 0; octave < field.Octaves; octave++)
        {
            weights.Add(weight);
            weight = weight * field.RoughnessFraction / ReliefField.RoughnessUnit;
        }

        long total = weights.Sum();
        long shed = weights.Skip(carried).Sum();
        return (field.AmplitudeMetres * ReliefField.HeightUnit * shed / total) + 1;
    }

    [Fact]
    public void SampleFars_shape_follows_its_own_cell_rather_than_the_regions()
    {
        short[] far = TerrainHeightfield.SampleFar(Portable, 0, 0, 0, 800, 40);
        Assert.Equal(21 * 21, far.Length);
    }

    [Theory]
    [InlineData(100, 0)]
    [InlineData(101, 10)]
    public void A_far_extent_that_is_not_whole_cells_is_refused_by_name(long extentMetres, long cellMetres)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TerrainHeightfield.SampleFar(Portable, 0, 0, 0, extentMetres, cellMetres));
    }

    private static ulong SplitMix64Finalise(ulong state)
    {
        state ^= state >> 30;
        state = unchecked(state * 0xBF58476D1CE4E5B9UL);
        state ^= state >> 27;
        state = unchecked(state * 0x94D049BB133111EBUL);
        return state ^ (state >> 31);
    }

    private static string Digest(short[] heights)
    {
        var bytes = new byte[heights.Length * sizeof(short)];
        Buffer.BlockCopy(heights, 0, bytes, 0, bytes.Length);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
}
