using System.Text;
using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Shared;

public class StreamPathTests
{
    [Theory]
    [InlineData("system")]
    [InlineData("a")]
    [InlineData("system/planet/0/region/3/site/1/artifact/2")]
    [InlineData("terrain_layer/A-1")]
    [InlineData("0/0/0")]
    public void Canonical_paths_are_accepted(string path)
    {
        StreamPath.Validate(path);
    }

    // Each rejection removes a way for two paths that look alike to derive different bytes, or for one
    // path to derive different bytes under a different locale or Unicode normalisation form.
    [Theory]
    [InlineData("")]                    // no segments
    [InlineData("/system")]             // leading separator
    [InlineData("system/")]             // trailing separator
    [InlineData("system//planet")]      // empty segment
    [InlineData("system planet")]       // space
    [InlineData("system.planet")]       // dot
    [InlineData("system\\planet")]      // backslash, the other platform's separator
    [InlineData("system\0")]            // null byte
    [InlineData("planète")]        // non-ASCII letter
    [InlineData("café")]          // non-ASCII, and a string with more than one normalisation form
    public void Non_canonical_paths_are_rejected(string path)
    {
        var thrown = Assert.Throws<ArgumentException>(() => StreamPath.Validate(path));

        Assert.Equal("path", thrown.ParamName);
    }

    [Fact]
    public void A_null_path_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => StreamPath.Validate(null!));
    }

    [Fact]
    public void Validation_is_case_sensitive_rather_than_case_folding()
    {
        // Both are valid, and they are different paths. Nothing may fold them together, because a
        // case-insensitive host filesystem must never influence a derived stream.
        StreamPath.Validate("Region");
        StreamPath.Validate("region");

        Assert.NotEqual(StreamPath.ToCanonicalBytes("Region"), StreamPath.ToCanonicalBytes("region"));
    }

    [Fact]
    public void Canonical_bytes_are_the_ASCII_bytes_of_the_path_with_no_byte_order_mark()
    {
        const string path = "system/planet/0";

        byte[] canonical = StreamPath.ToCanonicalBytes(path);

        Assert.Equal(Encoding.ASCII.GetBytes(path), canonical);
        Assert.Equal(path.Length, canonical.Length);
    }
}
