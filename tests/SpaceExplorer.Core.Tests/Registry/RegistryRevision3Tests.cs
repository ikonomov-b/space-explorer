using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Tests.Generation;
using Xunit;
using static SpaceExplorer.Core.Registry.CategoryRegistryRevision1;

namespace SpaceExplorer.Core.Tests.Registry;

/// <summary>
/// Category registry revision 3: its records carry definition tags, and a planet references the material
/// its surface is made of. What the tags are for is that a reference draws only from definitions that
/// suit it, so an icy body cannot take a material made for rock (decision 0055).
/// </summary>
public class RegistryRevision3Tests
{
    private static readonly CategoryRegistry Revision3 = CategoryRegistryRevision3.Registry;

    [Fact]
    public void Revision_3_round_trips_and_leaves_the_earlier_revisions_byte_identical()
    {
        Assert.Equal(Revision3.Hash, CategoryRegistry.Decode(Revision3.Bytes, 3).Hash);
        Assert.Throws<FormatException>(() => CategoryRegistry.Decode(Revision3.Bytes, 2));
        Assert.NotEqual(CategoryRegistryRevision2.Registry.Hash, Revision3.Hash);

        // The earlier revisions carry no tags, which is why every pack published under them keeps loading
        // and their recorded hashes still hold.
        Assert.False(CategoryRegistryRevision1.Registry.CarriesTags);
        Assert.False(CategoryRegistryRevision2.Registry.CarriesTags);
        Assert.True(Revision3.CarriesTags);

        // The one addition: a planet says what its surface is made of, and revision 2's does not.
        Assert.Equal(8, CategoryRegistryRevision2.Registry.Find(Planet).Parameters.Count);
        ParameterDescriptor surface = Assert.Single(Revision3.Find(Planet).Parameters, parameter => parameter.Label == CategoryRegistryRevision3.SurfaceParameter);
        Assert.Equal(ParameterKind.PrimitiveRef, surface.Kind);
        Assert.Equal(SurfaceMaterial, surface.RefCategory);
    }

    [Fact]
    public void A_reference_draws_only_from_definitions_that_suit_it()
    {
        PrimitiveSet set = TaggedFixture.Set;
        PrimitiveDefinition icy = Material(set, "for-icy");
        PrimitiveDefinition rocky = Material(set, "for-rocky");
        Assert.NotEqual(icy.Id, rocky.Id);

        foreach (PrimitiveDefinition body in set.Definitions.Where(definition => definition.Category == Planet))
        {
            string bodyType = Label(body, "body-type");
            PrimitiveRevisionRef reference = body.TryParameter(Revision3, CategoryRegistryRevision3.SurfaceParameter)!.Value.Value.AsRef;

            Assert.Equal(bodyType == "icy" ? icy.Id : rocky.Id, reference.Id);
            Assert.Contains(bodyType == "icy" ? "for-icy" : "for-rocky", set.TryFind(reference.Id)!.Tags);
        }

        // A material's own texture is drawn the same way, so an icy surface cannot take rock's pattern.
        Assert.Contains("for-icy", set.TryFind(icy.TryParameter(Revision3, "texture")!.Value.Value.AsRef.Id)!.Tags);
    }

    [Fact]
    public void A_definition_carries_the_tags_its_template_declared_and_the_record_round_trips()
    {
        PrimitiveDefinition material = Material(TaggedFixture.Set, "for-icy");
        PrimitiveDefinition decoded = PrimitiveDefinition.Decode(material.Bytes, Revision3, material.Id);

        Assert.Equal(["for-icy"], material.Tags);
        Assert.Equal(material.Hash, decoded.Hash);
        Assert.Equal(material.Tags, decoded.Tags);
    }

    [Fact]
    public void A_reference_no_definition_suits_fails_by_name_rather_than_drawing_something_else()
    {
        // Nothing carries "for-ocean", so the body that asks for it cannot be generated, and the failure
        // names the template and what it wanted rather than substituting a material that does not suit.
        GenerationException failure = Assert.Throws<GenerationException>(
            () => TaggedFixture.Generate(TaggedFixture.Compose(icyBodyRequires: "for-ocean"), TaggedFixture.Requests));

        Assert.Contains("icy-body", failure.Message, StringComparison.Ordinal);
        Assert.Contains("for-ocean", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_revision_that_carries_no_tags_refuses_them()
    {
        CategoryRegistry revision2 = CategoryRegistryRevision2.Registry;
        ParameterRange[] material =
        [
            ParameterRange.Enum(0),
            ParameterRange.Colour((10, 250), (10, 250), (10, 250), (255, 255)),
            ParameterRange.Integer(0, 65_535),
            ParameterRange.Integer(0, 65_535),
            ParameterRange.Ref(),
        ];

        Assert.Throws<ArgumentException>(() => PrimitiveTemplate.Create(revision2, 1, "tagged", SurfaceMaterial, material, [], ["for-icy"]));

        // And a reference cannot require what no definition of that revision can carry.
        Assert.Throws<ArgumentException>(() => PrimitiveTemplate.Create(
            revision2, 1, "tagged-reference", SurfaceMaterial, [.. material[..4], ParameterRange.Ref("for-icy")], []));
    }

    private static PrimitiveDefinition Material(PrimitiveSet set, string tag) =>
        set.Definitions.Single(definition => definition.Category == SurfaceMaterial && definition.Tags.Contains(tag));

    private static string Label(PrimitiveDefinition definition, string parameter)
    {
        (ParameterDescriptor descriptor, ParameterValue value) = definition.TryParameter(Revision3, parameter)!.Value;
        return descriptor.EnumLabels[(int)value.AsEnumIndex];
    }
}
