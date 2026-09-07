using SpaceExplorer.Core.Generation;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Registry;

public class SetRecordsTests
{
    [Fact]
    public void A_specification_round_trips_and_its_pack_is_the_leading_half_of_its_hash()
    {
        SetSpecification specification = Fixture.Specification(42, Fixture.StandardRequests);
        SetSpecification decoded = SetSpecification.Decode(specification.Bytes);

        Assert.Equal(specification.Hash, decoded.Hash);
        Assert.Equal(specification.Requests, decoded.Requests);
        Assert.Equal(PackId.FromSpecificationHash(specification.Hash), decoded.PackId);
    }

    [Fact]
    public void A_manifest_round_trips()
    {
        PrimitiveSet set = SetGenerator.Generate(Fixture.Specification(42, Fixture.StandardRequests), Fixture.Vocabulary, Fixture.Registry);
        SetManifest decoded = SetManifest.Decode(set.Manifest.Bytes);

        Assert.Equal(set.Manifest.Hash, decoded.Hash);
        Assert.Equal(set.Manifest.Definitions, decoded.Definitions);
        Assert.True(decoded.Validation.Accepted);
    }

    [Fact]
    public void A_manifest_must_list_ids_1_to_n_in_order_under_its_own_pack()
    {
        SetSpecification specification = Fixture.Specification(1, Fixture.StandardRequests);
        ContentHash hash = ContentHash.Of("d"u8);
        var validation = new ValidationResult(true, 1, 0);

        Assert.Throws<ArgumentException>(() => SetManifest.Create(specification.Hash, specification.PackId, 1, Fixture.Registry.Hash, GeneratorVersion.Current, SetGenerator.GrammarVersion, Fixture.TemplatePack, Fixture.Vocabulary.Hash,
            [new ManifestEntry(2, 1, hash)], [], [], validation));
        Assert.Throws<ArgumentException>(() => SetManifest.Create(specification.Hash, Fixture.TemplatePack, 1, Fixture.Registry.Hash, GeneratorVersion.Current, SetGenerator.GrammarVersion, Fixture.TemplatePack, Fixture.Vocabulary.Hash,
            [new ManifestEntry(1, 1, hash)], [], [], validation));
    }

    [Fact]
    public void A_set_refuses_a_definition_whose_hash_differs_from_the_manifest()
    {
        PrimitiveSet set = SetGenerator.Generate(Fixture.Specification(1, Fixture.StandardRequests), Fixture.Vocabulary, Fixture.Registry);
        PrimitiveDefinition[] swapped = [.. set.Definitions];
        (swapped[0], swapped[1]) = (swapped[1], swapped[0]);

        Assert.Throws<ArgumentException>(() => PrimitiveSet.Create(set.Manifest, swapped));
    }
}
