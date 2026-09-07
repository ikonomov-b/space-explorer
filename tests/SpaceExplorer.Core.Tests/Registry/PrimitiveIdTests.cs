using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Registry;

public class PrimitiveIdTests
{
    private static readonly PackId RealPack = PackId.FromSpecificationHash(ContentHash.Of("pack"u8));

    [Fact]
    public void The_default_value_is_unset()
    {
        Assert.True(default(PrimitiveId).IsUnset);
    }

    [Fact]
    public void A_reserved_zero_local_id_is_unset_even_with_a_real_pack()
    {
        var id = new PrimitiveId(RealPack, 0);
        Assert.True(id.IsUnset);
    }

    [Fact]
    public void An_unset_pack_is_unset_even_with_a_nonzero_local_id()
    {
        var id = new PrimitiveId(default, 1);
        Assert.True(id.IsUnset);
    }

    [Fact]
    public void A_real_pack_with_a_nonzero_local_id_is_not_unset()
    {
        var id = new PrimitiveId(RealPack, 1);
        Assert.False(id.IsUnset);
    }

    [Fact]
    public void Two_identities_with_the_same_pack_and_local_id_are_equal()
    {
        Assert.Equal(new PrimitiveId(RealPack, 7), new PrimitiveId(RealPack, 7));
    }

    [Fact]
    public void Identities_with_different_local_ids_are_not_equal()
    {
        Assert.NotEqual(new PrimitiveId(RealPack, 7), new PrimitiveId(RealPack, 8));
    }
}
