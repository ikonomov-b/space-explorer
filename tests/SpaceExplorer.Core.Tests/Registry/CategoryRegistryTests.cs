using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Registry;

public class CategoryRegistryTests
{
    [Fact]
    public void Revision_1_round_trips_through_its_canonical_record()
    {
        CategoryRegistry registry = CategoryRegistryRevision1.Registry;
        CategoryRegistry decoded = CategoryRegistry.Decode(registry.Bytes, 1);

        Assert.Equal(registry.Hash, decoded.Hash);
        Assert.Equal(9, decoded.Categories.Count);
        Assert.Equal("category-registry/1", decoded.Domain);
        Assert.Equal("primitive-definition/1/registry/1", decoded.DefinitionDomain);
    }

    [Fact]
    public void Revision_1_hash_is_frozen()
    {
        // This freezes the record as decision 0042 records it; it is this implementation's output, not an
        // external anchor, so it detects an unrecorded change rather than proving the encoding.
        Assert.Equal(
            "1419038daa22a9b92629c4a7ebbef17902c8d63ddadf3727e6d40b98cea657fb",
            CategoryRegistryRevision1.Registry.Hash.ToString());
    }

    [Fact]
    public void A_record_of_another_revision_is_refused_by_its_label()
    {
        Assert.Throws<FormatException>(() => CategoryRegistry.Decode(CategoryRegistryRevision1.Registry.Bytes, 2));
    }

    [Fact]
    public void Trailing_bytes_are_refused()
    {
        byte[] bytes = [.. CategoryRegistryRevision1.Registry.Bytes, 0];
        Assert.Throws<FormatException>(() => CategoryRegistry.Decode(bytes, 1));
    }

    [Fact]
    public void The_reserved_zero_id_and_out_of_order_ids_are_refused()
    {
        CategoryDefinition Category(uint id, string label) => new(id, label, [CompositionDomain.Artifact], [], [], StoragePolicies.Materialize, 0, []);

        Assert.Throws<ArgumentException>(() => Category(0, "zero"));
        Assert.Throws<ArgumentException>(() => CategoryRegistry.Create(1, [Category(2, "b"), Category(1, "a")]));
    }

    [Fact]
    public void A_reference_to_an_unknown_category_is_refused()
    {
        var referencing = new CategoryDefinition(1, "a", [CompositionDomain.Artifact], [ParameterDescriptor.Ref("other", 7)], [], StoragePolicies.Materialize, 0, []);
        Assert.Throws<ArgumentException>(() => CategoryRegistry.Create(1, [referencing]));
    }

    [Fact]
    public void Regenerate_needs_a_generator_revision_and_materialize_forbids_one()
    {
        Assert.Throws<ArgumentException>(() => new CategoryDefinition(1, "a", [CompositionDomain.Artifact], [], [], StoragePolicies.Regenerate, 0, []));
        Assert.Throws<ArgumentException>(() => new CategoryDefinition(1, "a", [CompositionDomain.Artifact], [], [], StoragePolicies.Materialize, 0, ["gen/1"]));
        Assert.Throws<ArgumentException>(() => new CategoryDefinition(1, "a", [CompositionDomain.Artifact], [], [], StoragePolicies.Regenerate, 0, ["gen/01"]));
        _ = new CategoryDefinition(1, "a", [CompositionDomain.Artifact], [], [], StoragePolicies.Regenerate, 0, ["gen/1"]);
    }
}
