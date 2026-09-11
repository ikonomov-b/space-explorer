using System.Security.Cryptography;
using SpaceExplorer.Core.Appearance;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Tests.Generation;
using Xunit;

namespace SpaceExplorer.Core.Tests.Appearance;

/// <summary>
/// What a body looks like is read out of the stored material and the texture recipe it references, and
/// the pixels are computed in the core by integer arithmetic, so the same material is the same image on
/// every operating system and in every renderer (decisions 0031, 0055).
/// </summary>
public class SurfaceAppearanceTests
{
    /// <summary>A fixed appearance, standing for one a generator produced.</summary>
    private static readonly SurfaceAppearance Banded = new(
        "standard-opaque",
        new Rgba(198, 176, 132, 255),
        RoughnessFraction: 58_000,
        MetallicFraction: 1_024,
        "stripes",
        ScaleUnits: 32_768,
        new Rgba(170, 140, 100, 255),
        new Rgba(232, 212, 176, 255),
        ContrastFraction: 40_000);

    [Fact]
    public void One_appearance_rasters_to_one_recorded_image()
    {
        // The digest of the raster bytes, computed from this implementation rather than derived
        // independently: what it pins is that the image does not drift, and what makes it evidence of a
        // portable image is continuous integration comparing it on Linux and on Windows.
        byte[] pixels = Banded.Raster(64, 1);

        Assert.Equal(64 * 64 * 4, pixels.Length);
        Assert.Equal("4a9c2dd16acc0ed321b19433bfdb4cd13e413371eeb11494ba0990e8c05bf2e8", Digest(pixels));
        Assert.Equal(Digest(pixels), Digest(Banded.Raster(64, 1)));
    }

    [Fact]
    public void Version_2_tiles_without_a_seam_where_version_1_does_not()
    {
        // The fault decision 0061 clause 3 predicted and decision 0064 fixes: a region lays a tile at its
        // true metre length, so the tile abuts its own copy every 35 cm and version 1's unwrapped lattice
        // draws a line there. Seamlessness is checked as the eye checks it — across the join — rather than
        // by trusting the modulo: the last column against the first, and the last row against the first.
        SurfaceAppearance ground = Banded with { Pattern = "noise" };

        Assert.True(EdgeDifference(ground.Raster(64, 2)) < EdgeDifference(ground.Raster(64, 1)) / 2);
    }

    [Fact]
    public void Version_1_is_unmoved_by_version_2_existing()
    {
        // A rule version is frozen at its first vectored result, so the digest above is version 1's for
        // good; what a second version may not do is quietly become the first.
        Assert.Equal("4a9c2dd16acc0ed321b19433bfdb4cd13e413371eeb11494ba0990e8c05bf2e8", Digest(Banded.Raster(64, 1)));
        Assert.NotEqual(Digest(Banded.Raster(64, 1)), Digest(Banded.Raster(64, 2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Banded.Raster(64, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Banded.Raster(64, 3));
    }

    /// <summary>The mean channel difference across the tile's two joins, which is what a seam is.</summary>
    private static double EdgeDifference(byte[] pixels)
    {
        const int side = 64;
        long total = 0;
        for (int at = 0; at < side; at++)
        {
            for (int channel = 0; channel < 3; channel++)
            {
                int leftEdge = ((at * side) + side - 1) * 4;
                int rightEdge = (at * side) * 4;
                int bottomEdge = (((side - 1) * side) + at) * 4;
                int topEdge = at * 4;
                total += Math.Abs(pixels[leftEdge + channel] - pixels[rightEdge + channel]);
                total += Math.Abs(pixels[bottomEdge + channel] - pixels[topEdge + channel]);
            }
        }

        return total / (double)(side * 6);
    }

    [Fact]
    public void A_different_appearance_is_a_different_image_and_a_different_hash()
    {
        SurfaceAppearance other = Banded with { ColourB = new Rgba(80, 90, 210, 255) };

        Assert.NotEqual(Banded.Hash, other.Hash);
        Assert.NotEqual(Digest(Banded.Raster(64, 1)), Digest(other.Raster(64, 1)));

        // The seed follows the appearance's own bytes, so two equal appearances cannot differ.
        Assert.Equal(Banded.Hash, (Banded with { }).Hash);
    }

    [Fact]
    public void A_raster_side_outside_the_bounds_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Banded.Raster(4, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Banded.Raster(1_024, 1));
    }

    [Fact]
    public void An_appearance_is_read_out_of_the_material_and_the_recipe_it_references()
    {
        PrimitiveSet set = TaggedFixture.Set;
        CategoryRegistry registry = CategoryRegistryRevision3.Registry;
        PrimitiveDefinition material = set.Definitions.First(definition => definition.Category == CategoryRegistryRevision1.SurfaceMaterial);

        SurfaceAppearance appearance = SurfaceAppearance.From(material, set, registry);
        PrimitiveDefinition recipe = set.TryFind(material.TryParameter(registry, "texture")!.Value.Value.AsRef.Id)!;

        Assert.Equal(Colour(material, registry, "base-colour"), appearance.BaseColour);
        Assert.Equal(material.TryParameter(registry, "roughness")!.Value.Value.AsInteger, appearance.RoughnessFraction);
        Assert.Equal(Label(recipe, registry, "pattern"), appearance.Pattern);
        Assert.Equal(Colour(recipe, registry, "colour-a"), appearance.ColourA);
        Assert.Equal(recipe.TryParameter(registry, "contrast")!.Value.Value.AsInteger, appearance.ContrastFraction);
    }

    private static string Digest(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static Rgba Colour(PrimitiveDefinition definition, CategoryRegistry registry, string label)
    {
        (byte red, byte green, byte blue, byte alpha) = definition.TryParameter(registry, label)!.Value.Value.AsColour;
        return new Rgba(red, green, blue, alpha);
    }

    private static string Label(PrimitiveDefinition definition, CategoryRegistry registry, string label)
    {
        (ParameterDescriptor descriptor, ParameterValue value) = definition.TryParameter(registry, label)!.Value;
        return descriptor.EnumLabels[(int)value.AsEnumIndex];
    }
}
