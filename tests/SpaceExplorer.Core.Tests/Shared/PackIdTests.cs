using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Shared;

public class PackIdTests
{
    [Fact]
    public void A_generated_pack_identifier_is_the_leading_half_of_its_specification_hash()
    {
        ContentHash specification = ContentHash.Of("specification"u8);
        PackId pack = PackId.FromSpecificationHash(specification);

        Assert.Equal(specification.ToString()[..(PackId.ByteCount * 2)], pack.ToString());
        Assert.Equal(PackId.ByteCount, pack.Bytes.Length);
    }

    [Fact]
    public void The_same_specification_derives_the_same_identifier_and_a_different_one_does_not()
    {
        // Decision 0006: independent reproduction of the same specification yields the same identifier,
        // so two machines never coordinate. The suite runs on both operating systems, which is where
        // that claim is actually tested.
        Assert.Equal(
            PackId.FromSpecificationHash(ContentHash.Of("specification"u8)),
            PackId.FromSpecificationHash(ContentHash.Of("specification"u8)));

        Assert.NotEqual(
            PackId.FromSpecificationHash(ContentHash.Of("specification"u8)),
            PackId.FromSpecificationHash(ContentHash.Of("specificatioo"u8)));
    }

    [Fact]
    public void An_unset_hash_identifies_no_specification() =>
        Assert.Throws<ArgumentException>(() => PackId.FromSpecificationHash(default));

    [Fact]
    public void An_authored_identifier_is_accepted_as_bytes_from_outside_the_core()
    {
        // The core draws no random identity of its own: Guid.NewGuid and System.Random are banned here,
        // and a seeded stream is not random across authors (decision 0008).
        byte[] drawn = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
        PackId pack = PackId.FromBytes(drawn);

        Assert.Equal("0102030405060708090a0b0c0d0e0f10", pack.ToString());
        Assert.Equal(pack, PackId.Parse(pack.ToString()));
        Assert.Throws<ArgumentException>(() => PackId.FromBytes(drawn.AsSpan(1)));
    }

    [Fact]
    public void Parsing_rejects_anything_but_the_one_rendered_form()
    {
        Assert.Throws<FormatException>(() => PackId.Parse("0102030405060708090A0B0C0D0E0F10"));
        Assert.Throws<FormatException>(() => PackId.Parse("0102030405060708090a0b0c0d0e0f"));
    }

    [Fact]
    public void The_default_value_is_unset_rather_than_a_zero_identifier()
    {
        PackId unset = default;

        Assert.True(unset.IsUnset);
        Assert.Equal("(unset)", unset.ToString());
        Assert.NotEqual(PackId.FromBytes(new byte[PackId.ByteCount]), unset);
    }
}
