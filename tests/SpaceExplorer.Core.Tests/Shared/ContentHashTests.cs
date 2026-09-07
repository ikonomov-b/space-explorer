using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Shared;

public class ContentHashTests
{
    [Fact]
    public void Of_matches_the_published_SHA_256_vectors()
    {
        // External anchors, from the SHA-256 examples published with the standard. They pin that this is
        // SHA-256 of the bytes given and nothing else: no salt, no prefix, no encoding step of its own.
        Assert.Equal(
            "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            ContentHash.Of(ReadOnlySpan<byte>.Empty).ToString());

        Assert.Equal(
            "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
            ContentHash.Of("abc"u8).ToString());
    }

    [Fact]
    public void Of_hashes_the_canonical_bytes_and_nothing_else()
    {
        var writer = new CanonicalWriter("set-specification");
        writer.WriteUInt32(7U);

        Assert.Equal(ContentHash.Of(writer.ToArray()), writer.ToContentHash());
    }

    [Fact]
    public void A_hash_round_trips_through_its_rendered_form()
    {
        ContentHash hash = ContentHash.Of("payload"u8);

        Assert.Equal(hash, ContentHash.Parse(hash.ToString()));
        Assert.Equal(hash, ContentHash.FromBytes(hash.Bytes));
        Assert.Equal(ContentHash.ByteCount, hash.Bytes.Length);
    }

    [Fact]
    public void Parsing_rejects_anything_but_the_one_rendered_form()
    {
        string valid = ContentHash.Of("payload"u8).ToString();

        // Upper case is rejected so that one identity has exactly one file name, whether or not the
        // file system distinguishes case.
        Assert.Throws<FormatException>(() => ContentHash.Parse(valid.ToUpperInvariant()));
        Assert.Throws<FormatException>(() => ContentHash.Parse(valid[..63]));
        Assert.Throws<FormatException>(() => ContentHash.Parse(valid[..63] + "z"));
    }

    [Fact]
    public void Bytes_of_the_wrong_length_are_not_a_hash() =>
        Assert.Throws<ArgumentException>(() => ContentHash.FromBytes(new byte[ContentHash.ByteCount - 1]));

    [Fact]
    public void The_default_value_is_unset_rather_than_a_hash_of_nothing()
    {
        ContentHash unset = default;

        Assert.True(unset.IsUnset);
        Assert.Equal("(unset)", unset.ToString());
        Assert.Equal(default, unset);
        Assert.NotEqual(ContentHash.Of(ReadOnlySpan<byte>.Empty), unset);
        Assert.False(ContentHash.Of(ReadOnlySpan<byte>.Empty).IsUnset);
    }

    [Fact]
    public void Equal_content_hashes_equal_and_serve_as_dictionary_keys()
    {
        ContentHash first = ContentHash.Of("payload"u8);
        ContentHash again = ContentHash.Of("payload"u8);
        ContentHash other = ContentHash.Of("payloaf"u8);

        Assert.True(first == again);
        Assert.True(first != other);
        Assert.Equal(first.GetHashCode(), again.GetHashCode());
        Assert.Single(new HashSet<ContentHash> { first, again });
    }
}
