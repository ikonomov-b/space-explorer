using System.Reflection;
using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Shared;

public class CanonicalWriterTests
{
    // Format version 1, then the domain "t" as a one-byte length and one ASCII byte.
    private const string Header = "010174";

    [Fact]
    public void Every_byte_string_opens_with_the_format_version_and_domain()
    {
        // The header is the whole of an empty record: nothing else is implicit.
        Assert.Equal(Header, Convert.ToHexStringLower(new CanonicalWriter("t").ToArray()));
    }

    [Fact]
    public void The_domain_separates_records_that_carry_identical_fields()
    {
        // Two record kinds that happen to encode the same values must not share a content hash. This is
        // what makes a hash safe to use as an identity without also naming the kind of thing it identifies.
        var first = new CanonicalWriter("set-specification");
        var second = new CanonicalWriter("world-manifest");
        first.WriteUInt32(7U);
        second.WriteUInt32(7U);

        Assert.NotEqual(first.ToContentHash(), second.ToContentHash());
    }

    [Fact]
    public void A_domain_must_be_in_canonical_path_form()
    {
        Assert.Throws<ArgumentException>(() => new CanonicalWriter("world manifest"));
        Assert.Throws<ArgumentException>(() => new CanonicalWriter(string.Empty));
    }

    [Theory]
    [InlineData(0x01020304U, "04030201")]
    [InlineData(0U, "00000000")]
    [InlineData(uint.MaxValue, "ffffffff")]
    public void Fixed_width_values_are_little_endian(uint value, string expected) =>
        Assert.Equal(expected, Encode(writer => writer.WriteUInt32(value)));

    [Fact]
    public void Fixed_width_values_use_their_declared_width()
    {
        Assert.Equal("2a", Encode(writer => writer.WriteUInt8(42)));
        Assert.Equal("0201", Encode(writer => writer.WriteUInt16(0x0102)));
        Assert.Equal("efcdab8967452301", Encode(writer => writer.WriteUInt64(0x0123456789ABCDEFUL)));
    }

    [Fact]
    public void Signed_values_are_two_s_complement()
    {
        Assert.Equal("ffff", Encode(writer => writer.WriteInt16(-1)));
        Assert.Equal("feffffff", Encode(writer => writer.WriteInt32(-2)));
        Assert.Equal("000000000080ffff", Encode(writer => writer.WriteInt64(-0x800000000000L)));
        Assert.Equal("00000080", Encode(writer => writer.WriteInt32(int.MinValue)));
    }

    [Theory]
    [InlineData(0UL, "00")]
    [InlineData(1UL, "01")]
    [InlineData(127UL, "7f")]
    [InlineData(128UL, "8001")]
    [InlineData(300UL, "ac02")]
    [InlineData(ulong.MaxValue, "ffffffffffffffffff01")]
    public void Variable_width_values_are_LEB128(ulong value, string expected) =>
        Assert.Equal(expected, Encode(writer => writer.WriteVarUInt(value)));

    [Theory]
    [InlineData(0L, "00")]
    [InlineData(-1L, "01")]
    [InlineData(1L, "02")]
    [InlineData(-2L, "03")]
    [InlineData(long.MinValue, "ffffffffffffffffff01")]
    public void Signed_variable_width_values_are_zig_zag_encoded(long value, string expected) =>
        Assert.Equal(expected, Encode(writer => writer.WriteVarInt(value)));

    [Fact]
    public void Length_prefixes_keep_adjacent_fields_from_running_together()
    {
        // Without the prefix both records would be the two bytes 'a' 'b', so two different sets of
        // inputs would share an identity. This is the framing counterpart of the length step in Mix64.
        string twoFields = Encode(writer =>
        {
            writer.WriteText("a");
            writer.WriteText("b");
        });

        string oneFieldAndAnEmptyOne = Encode(writer =>
        {
            writer.WriteText("ab");
            writer.WriteText(string.Empty);
        });

        Assert.Equal("01610162", twoFields);
        Assert.Equal("02616200", oneFieldAndAnEmptyOne);
        Assert.NotEqual(twoFields, oneFieldAndAnEmptyOne);
    }

    [Fact]
    public void Text_admits_only_the_frozen_character_set()
    {
        // Anything outside it could carry a second Unicode normalisation form or a case-folding rule,
        // and the hash of a record must not depend on which spelling of a character reached it.
        Assert.Throws<ArgumentException>(() => Encode(writer => writer.WriteText("tier two")));
        Assert.Throws<ArgumentException>(() => Encode(writer => writer.WriteText("v1.2")));
        Assert.Throws<ArgumentException>(() => Encode(writer => writer.WriteText("café")));
        Assert.Equal("00", Encode(writer => writer.WriteText(string.Empty)));
    }

    [Fact]
    public void A_count_is_range_checked_where_it_is_written()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Encode(writer => writer.WriteCount(-1)));
        Assert.Equal("00", Encode(writer => writer.WriteCount(0)));
    }

    [Fact]
    public void Identities_are_written_at_their_fixed_width_and_reject_the_unset_value()
    {
        ContentHash hash = ContentHash.Of("payload"u8);
        PackId pack = PackId.FromSpecificationHash(hash);

        Assert.Equal(ContentHash.ByteCount * 2, Encode(writer => writer.WriteContentHash(hash)).Length);
        Assert.Equal(PackId.ByteCount * 2, Encode(writer => writer.WritePackId(pack)).Length);
        Assert.Throws<ArgumentException>(() => Encode(writer => writer.WriteContentHash(default)));
        Assert.Throws<ArgumentException>(() => Encode(writer => writer.WritePackId(default)));
    }

    [Fact]
    public void No_method_accepts_a_floating_point_value()
    {
        // Structural, like the architecture tests: a float parameter cannot appear by accident later.
        // A hash that depended on rounding would not survive the move to another machine.
        MethodInfo[] floatingPoint = [.. typeof(CanonicalWriter)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.GetParameters()
                .Any(parameter => parameter.ParameterType == typeof(float)
                    || parameter.ParameterType == typeof(double)
                    || parameter.ParameterType == typeof(decimal)))];

        Assert.Empty(floatingPoint);
    }

    [Fact]
    public void The_recorded_vector_holds()
    {
        // Frozen under CanonicalWriter.FormatVersion. Every byte below follows from the rules and can be
        // read off by hand: 01 format version; 13 and the 19 bytes of the domain; the generator version
        // as four little-endian bytes; the seed as eight; 08 and the eight bytes of "tier-two"; 05, the
        // zig-zag encoding of -3; 02 elements; then 01 and ac 02, the LEB128 forms of 1 and 300.
        var writer = new CanonicalWriter("set-specification/1");
        writer.WriteUInt32(GeneratorVersion.Current);
        writer.WriteUInt64(0x0123456789ABCDEFUL);
        writer.WriteText("tier-two");
        writer.WriteVarInt(-3);
        writer.WriteCount(2);
        writer.WriteVarUInt(1UL);
        writer.WriteVarUInt(300UL);

        Assert.Equal(
            "01137365742d73706563696669636174696f6e2f3101000000efcdab896745230108746965722d74776f050201ac02",
            Convert.ToHexStringLower(writer.ToArray()));

        // The hash of those bytes, and the pack identifier that derives from it. Both are compared
        // across operating systems by running this suite on each (M0a exit criterion 5).
        Assert.Equal(
            "6b43029b7f1ddaa46305fd06750ab400725992cbdff27f48649366aa19dc14b2",
            writer.ToContentHash().ToString());

        Assert.Equal(
            "6b43029b7f1ddaa46305fd06750ab400",
            PackId.FromSpecificationHash(writer.ToContentHash()).ToString());
    }

    /// <summary>Encodes into a writer with the one-character domain "t" and returns the bytes after the header.</summary>
    private static string Encode(Action<CanonicalWriter> write)
    {
        var writer = new CanonicalWriter("t");
        write(writer);
        string hex = Convert.ToHexStringLower(writer.ToArray());

        Assert.StartsWith(Header, hex, StringComparison.Ordinal);
        return hex[Header.Length..];
    }
}
