using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using Xunit;

namespace SpaceExplorer.Core.Tests.Registry;

public class PrimitiveDefinitionTests
{
    private static readonly PrimitiveSet Set = SetGenerator.Generate(Fixture.Specification(1, Fixture.StandardRequests), Fixture.Vocabulary, Fixture.Registry);

    [Fact]
    public void A_definition_round_trips_and_keeps_its_hash()
    {
        PrimitiveDefinition artifact = Set.Definitions[^1];
        PrimitiveDefinition decoded = PrimitiveDefinition.Decode(artifact.Bytes, Fixture.Registry, artifact.Id);

        Assert.Equal(artifact.Hash, decoded.Hash);
        Assert.Equal(artifact.Id, decoded.Id);
        Assert.Equal(CategoryRegistryRevision1.Artifact, decoded.Category);
        Assert.Equal(artifact.Parameters, decoded.Parameters);
        Assert.Equal(artifact.Provenance, decoded.Provenance);
    }

    [Fact]
    public void An_out_of_range_value_is_refused_when_building()
    {
        PrimitiveDefinition artifact = Set.Definitions[^1];
        ParameterValue[] values = [.. artifact.Parameters];
        values[2] = ParameterValue.Integer(3);

        Assert.Throws<ArgumentException>(() =>
            PrimitiveDefinition.Create(Fixture.Registry, artifact.Id, artifact.Category, artifact.Provenance, values, artifact.Connectors, string.Empty));
    }

    [Fact]
    public void An_out_of_range_value_is_refused_when_decoding()
    {
        // The artifact record ends with the tier varint (zig-zag 1 -> 0x02) and the empty generator text (0x00).
        byte[] bytes = Set.Definitions[^1].Bytes;
        Assert.Equal(0x02, bytes[^2]);
        bytes[^2] = 0x06;

        Assert.Throws<FormatException>(() => PrimitiveDefinition.Decode(bytes, Fixture.Registry, Set.Definitions[^1].Id));
    }

    [Fact]
    public void A_record_laid_out_under_another_registry_revision_is_refused_by_its_label()
    {
        CategoryRegistry other = CategoryRegistry.Create(2, [.. Fixture.Registry.Categories]);
        Assert.Throws<FormatException>(() => PrimitiveDefinition.Decode(Set.Definitions[0].Bytes, other, Set.Definitions[0].Id));
    }

    [Fact]
    public void Dependencies_are_the_reference_parameters()
    {
        PrimitiveDefinition part = Set.Definitions.First(definition => definition.Category == CategoryRegistryRevision1.ArtifactPart);
        Assert.Equal(2, part.Dependencies.Count());
        Assert.All(part.Dependencies, dependency => Assert.True(Set.Contains(dependency)));
    }
}
