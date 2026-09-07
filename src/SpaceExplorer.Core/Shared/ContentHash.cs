using System.Buffers.Binary;
using System.Security.Cryptography;

namespace SpaceExplorer.Core.Shared;

/// <summary>
/// The SHA-256 hash of a record's canonical bytes, which is how this project identifies exact content
/// (decision 0020). Rendered as lower-case hexadecimal, which is its file name under the data root
/// (decision 0018). The default value is unset rather than the hash of anything.
/// </summary>
public readonly struct ContentHash : IEquatable<ContentHash>
{
    /// <summary>The width of a SHA-256 hash.</summary>
    public const int ByteCount = 32;

    private readonly byte[]? _bytes;

    private ContentHash(byte[] bytes) => _bytes = bytes;

    /// <summary>The hash bytes, or an empty span when this value is unset.</summary>
    public ReadOnlySpan<byte> Bytes => _bytes;

    /// <summary>Whether this is the default value rather than the hash of some content.</summary>
    public bool IsUnset => _bytes is null;

    /// <summary>Hashes the canonical bytes of a record, normally <see cref="CanonicalWriter.ToArray"/>.</summary>
    public static ContentHash Of(ReadOnlySpan<byte> canonicalBytes) => new(SHA256.HashData(canonicalBytes));

    /// <summary>Wraps 32 stored or received bytes as a hash, without hashing them.</summary>
    /// <exception cref="ArgumentException"><paramref name="bytes"/> is not exactly 32 bytes long.</exception>
    public static ContentHash FromBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != ByteCount)
        {
            throw new ArgumentException(
                $"A content hash is exactly {ByteCount} bytes; received {bytes.Length}.",
                nameof(bytes));
        }

        return new ContentHash(bytes.ToArray());
    }

    /// <summary>Parses the lower-case hexadecimal form produced by <see cref="ToString"/>.</summary>
    /// <exception cref="FormatException"><paramref name="text"/> is not 64 lower-case hexadecimal characters.</exception>
    public static ContentHash Parse(string text) => new(Hex.Parse(text, ByteCount, "content hash"));

    /// <summary>Renders the hash as 64 lower-case hexadecimal characters.</summary>
    public override string ToString() => _bytes is null ? "(unset)" : Hex.ToLowerString(_bytes);

    /// <inheritdoc/>
    public bool Equals(ContentHash other) => Bytes.SequenceEqual(other.Bytes);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ContentHash other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        _bytes is null ? 0 : BinaryPrimitives.ReadInt32LittleEndian(_bytes);

    /// <summary>Whether two hashes are equal.</summary>
    public static bool operator ==(ContentHash left, ContentHash right) => left.Equals(right);

    /// <summary>Whether two hashes differ.</summary>
    public static bool operator !=(ContentHash left, ContentHash right) => !left.Equals(right);
}
