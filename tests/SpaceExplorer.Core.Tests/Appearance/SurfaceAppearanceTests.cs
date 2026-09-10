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
        byte[] pixels = Banded.Raster(64);

        Assert.Equal(64 * 64 * 4, pixels.Length);
        Assert.Equal("4a9c2dd16acc0ed321b19433bfdb4cd13e413371eeb11494ba0990e8c05bf2e8", Digest(pixels));
        Assert.Equal(Digest(pixels), Digest(Banded.Raster(64)));
    }

    [Fact]
    public void A_different_appearance_is_a_different_image_and_a_different_hash()
    {
        SurfaceAppearance other = Banded with { ColourB = new Rgba(80, 90, 210, 255) };

        Assert.NotEqual(Banded.Hash, other.Hash);
        Assert.NotEqual(Digest(Banded.Raster(64)), Digest(other.Raster(64)));

        // The seed follows the appearance's own bytes, so two equal appearances cannot differ.
        Assert.Equal(Banded.Hash, (Banded with { }).Hash);
    }

    [Fact]
    public void A_raster_side_outside_the_bounds_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Banded.Raster(4));
        Assert.Throws<ArgumentOutOfRangeException>(() => Banded.Raster(1_024));
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
