using System.Buffers.Binary;

namespace SpaceExplorer.Core.Shared;

/// <summary>
/// The 128-bit identity of a pack, stored as a 16-byte BLOB (decision 0006). A generated pack's
/// identifier is the leading 16 bytes of its set specification's <see cref="ContentHash"/> (decision
/// 0020); an authored pack's is drawn once outside the core and accepted through <see cref="FromBytes"/>.
/// The default value is unset rather than an identifier of zero.
/// </summary>
public readonly struct PackId : IEquatable<PackId>
{
    /// <summary>The width of a pack identifier.</summary>
    public const int ByteCount = 16;

    private readonly byte[]? _bytes;

    private PackId(byte[] bytes) => _bytes = bytes;

    /// <summary>The identifier bytes, or an empty span when this value is unset.</summary>
    public ReadOnlySpan<byte> Bytes => _bytes;

    /// <summary>Whether this is the default value rather than an identifier.</summary>
    public bool IsUnset => _bytes is null;

    /// <summary>Derives the identifier of a generated pack from its canonical set specification hash.</summary>
    /// <exception cref="ArgumentException"><paramref name="specificationHash"/> is unset.</exception>
    public static PackId FromSpecificationHash(ContentHash specificationHash)
    {
        if (specificationHash.IsUnset)
        {
            throw new ArgumentException(
                "An unset content hash identifies no specification, so no pack identifier derives from it.",
                nameof(specificationHash));
        }

        return new PackId(specificationHash.Bytes[..ByteCount].ToArray());
    }

    /// <summary>Wraps 16 stored, received, or externally drawn bytes as an identifier.</summary>
    /// <exception cref="ArgumentException"><paramref name="bytes"/> is not exactly 16 bytes long.</exception>
    public static PackId FromBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != ByteCount)
        {
            throw new ArgumentException(
                $"A pack identifier is exactly {ByteCount} bytes; received {bytes.Length}.",
                nameof(bytes));
        }

        return new PackId(bytes.ToArray());
    }

    /// <summary>Parses the lower-case hexadecimal form produced by <see cref="ToString"/>.</summary>
    /// <exception cref="FormatException"><paramref name="text"/> is not 32 lower-case hexadecimal characters.</exception>
    public static PackId Parse(string text) => new(Hex.Parse(text, ByteCount, "pack identifier"));

    /// <summary>Renders the identifier as 32 lower-case hexadecimal characters, which is its directory name.</summary>
    public override string ToString() => _bytes is null ? "(unset)" : Hex.ToLowerString(_bytes);

    /// <inheritdoc/>
    public bool Equals(PackId other) => Bytes.SequenceEqual(other.Bytes);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is PackId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        _bytes is null ? 0 : BinaryPrimitives.ReadInt32LittleEndian(_bytes);

    /// <summary>Whether two identifiers are equal.</summary>
    public static bool operator ==(PackId left, PackId right) => left.Equals(right);

    /// <summary>Whether two identifiers differ.</summary>
    public static bool operator !=(PackId left, PackId right) => !left.Equals(right);
}
