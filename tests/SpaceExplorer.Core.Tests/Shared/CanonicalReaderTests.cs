using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Shared;

public class CanonicalReaderTests
{
    [Fact]
    public void The_header_is_checked_against_the_format_version_and_expected_domain()
    {
        byte[] bytes = new CanonicalWriter("t").ToArray();
        var reader = new CanonicalReader(bytes, "t");

        Assert.True(reader.IsAtEnd);
    }

    [Fact]
    public void A_mismatched_domain_fails_explicitly()
    {
        byte[] bytes = new CanonicalWriter("set-specification").ToArray();
        Assert.Throws<FormatException>(() => new CanonicalReader(bytes, "world-manifest"));
    }

    [Fact]
    public void An_unrecognised_format_version_fails_explicitly()
    {
        byte[] bytes = new CanonicalWriter("t").ToArray();
        bytes[0] = 2;

        Assert.Throws<FormatException>(() => new CanonicalReader(bytes, "t"));
    }

    [Fact]
    public void Truncated_bytes_fail_explicitly_instead_of_reading_past_the_end()
    {
        var writer = new CanonicalWriter("t");
        writer.WriteUInt32(0x01020304U);
        byte[] truncated = writer.ToArray()[..^1];

        var reader = new CanonicalReader(truncated, "t");
        Assert.Throws<FormatException>(() => reader.ReadUInt32());
    }

    [Fact]
    public void Every_fixed_width_reader_round_trips_its_writer()
    {
        Assert.Equal((byte)42, RoundTrip(w => w.WriteUInt8(42), r => r.ReadUInt8()));
        Assert.Equal((ushort)0x0102, RoundTrip(w => w.WriteUInt16(0x0102), r => r.ReadUInt16()));
        Assert.Equal(0x0123456789ABCDEFUL, RoundTrip(w => w.WriteUInt64(0x0123456789ABCDEFUL), r => r.ReadUInt64()));
        Assert.Equal((short)-1, RoundTrip(w => w.WriteInt16(-1), r => r.ReadInt16()));
        Assert.Equal(-2, RoundTrip(w => w.WriteInt32(-2), r => r.ReadInt32()));
        Assert.Equal(int.MinValue, RoundTrip(w => w.WriteInt32(int.MinValue), r => r.ReadInt32()));
        Assert.Equal(-0x800000000000L, RoundTrip(w => w.WriteInt64(-0x800000000000L), r => r.ReadInt64()));
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(127UL)]
    [InlineData(128UL)]
    [InlineData(300UL)]
    [InlineData(ulong.MaxValue)]
    public void Variable_width_unsigned_values_round_trip(ulong value) =>
        Assert.Equal(value, RoundTrip(w => w.WriteVarUInt(value), r => r.ReadVarUInt()));

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(1L)]
    [InlineData(-2L)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void Variable_width_signed_values_round_trip(long value) =>
        Assert.Equal(value, RoundTrip(w => w.WriteVarInt(value), r => r.ReadVarInt()));

    [Fact]
    public void An_overlong_variable_width_encoding_fails_explicitly()
    {
        // Ten continuation bytes followed by a terminator: a structurally complete encoding a 64-bit
        // value never needs an eleventh group for, so this must fail on the shift bound, not on running
        // out of bytes — a buffer with no terminator at all would fail either way and prove nothing.
        byte[] header = new CanonicalWriter("t").ToArray();
        byte[] bytes = [.. header, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01];

        var reader = new CanonicalReader(bytes, "t");
        Assert.Throws<FormatException>(() => reader.ReadVarUInt());
    }

    [Fact]
    public void Adjacent_length_prefixed_fields_read_back_distinctly()
    {
        // The framing case CanonicalWriterTests exercises for writing: two encodings that would collide
        // without a length prefix must decode back to their own field values, not each other's.
        (string, string) twoFields = RoundTrip(
            w => { w.WriteText("a"); w.WriteText("b"); },
            r => (r.ReadText(), r.ReadText()));
        (string, string) oneFieldAndAnEmptyOne = RoundTrip(
            w => { w.WriteText("ab"); w.WriteText(string.Empty); },
            r => (r.ReadText(), r.ReadText()));

        Assert.Equal(("a", "b"), twoFields);
        Assert.Equal(("ab", ""), oneFieldAndAnEmptyOne);
    }

    [Fact]
    public void Text_rejects_a_decoded_byte_outside_the_frozen_character_set()
    {
        // Written through WriteBytes, which admits any byte, since WriteText itself would refuse "café".
        var writer = new CanonicalWriter("t");
        writer.WriteBytes("café"u8);
        var reader = new CanonicalReader(writer.ToArray(), "t");

        Assert.Throws<FormatException>(() => reader.ReadText());
    }

    [Fact]
    public void Identities_round_trip_at_their_fixed_width()
    {
        ContentHash hash = ContentHash.Of("payload"u8);
        PackId pack = PackId.FromSpecificationHash(hash);

        Assert.Equal(hash, RoundTrip(w => w.WriteContentHash(hash), r => r.ReadContentHash()));
        Assert.Equal(pack, RoundTrip(w => w.WritePackId(pack), r => r.ReadPackId()));
    }

    [Fact]
    public void The_recorded_vector_reads_back_to_its_original_field_values()
    {
        // The exact vector CanonicalWriterTests.The_recorded_vector_holds froze. Reading it back closes
        // the gap decision 0020 named: "the format is written and hashed but never parsed back."
        byte[] bytes = Convert.FromHexString(
            "01137365742d73706563696669636174696f6e2f3101000000efcdab896745230108746965722d74776f050201ac02");

        var reader = new CanonicalReader(bytes, "set-specification/1");

        Assert.Equal(1U, reader.ReadUInt32());
        Assert.Equal(0x0123456789ABCDEFUL, reader.ReadUInt64());
        Assert.Equal("tier-two", reader.ReadText());
        Assert.Equal(-3, reader.ReadVarInt());
        Assert.Equal(2, reader.ReadCount());
        Assert.Equal(1UL, reader.ReadVarUInt());
        Assert.Equal(300UL, reader.ReadVarUInt());
        Assert.True(reader.IsAtEnd);
    }

    /// <summary>Writes with a fresh "t"-domain writer, reads the same bytes back, and returns the result.</summary>
    private static T RoundTrip<T>(Action<CanonicalWriter> write, Func<CanonicalReader, T> read)
    {
        var writer = new CanonicalWriter("t");
        write(writer);

        var reader = new CanonicalReader(writer.ToArray(), "t");
        T result = read(reader);

        Assert.True(reader.IsAtEnd);
        return result;
    }
}
