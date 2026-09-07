using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Registry;

public class PrimitiveRevisionRefTests
{
    private static readonly PackId Pack = PackId.FromSpecificationHash(ContentHash.Of("pack"u8));

    [Fact]
    public void The_default_value_is_unset()
    {
        Assert.True(default(PrimitiveRevisionRef).IsUnset);
    }

    [Fact]
    public void An_identity_without_a_hash_is_unset()
    {
        Assert.True(new PrimitiveRevisionRef(new PrimitiveId(Pack, 1), default).IsUnset);
    }

    [Fact]
    public void A_hash_without_an_identity_is_unset()
    {
        Assert.True(new PrimitiveRevisionRef(default, ContentHash.Of("d"u8)).IsUnset);
    }

    [Fact]
    public void Two_revisions_of_one_identity_are_different_references()
    {
        var id = new PrimitiveId(Pack, 1);
        Assert.NotEqual(
            new PrimitiveRevisionRef(id, ContentHash.Of("a"u8)),
            new PrimitiveRevisionRef(id, ContentHash.Of("b"u8)));
    }
}
