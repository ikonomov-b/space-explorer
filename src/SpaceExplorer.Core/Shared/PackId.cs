using System.Buffers.Binary;

namespace SpaceExplorer.Core.Shared;

/// <summary>
/// The 128-bit identity of a pack, stored as a 16-byte BLOB (decision 0006). A generated pack derives
/// it from the content hash of its canonical set specification, so independent reproduction of the
/// same specification on either operating system yields the same identifier.
/// </summary>
/// <remarks>
/// <para>
/// Derivation takes the leading 16 bytes of the specification's <see cref="ContentHash"/>. The pack
/// directory name is therefore a prefix of the specification hash, which makes the relationship
/// checkable by eye. Truncating to 128 bits is what decision 0006 specifies; at the expected scale of
/// packs a collision is not a practical concern, and the full definition identity remains
/// <c>(pack_id, primitive_id)</c> with the exact content given by the revision hash.
/// </para>
/// <para>
/// An authored pack instead receives a random identifier once at creation. That identifier cannot
/// originate here: <see cref="System.Guid.NewGuid"/> and <see cref="System.Random"/> are rejected at
/// build time in this assembly (decision 0008), and drawing an identity from a seeded generation
/// stream would not be random across authors. The command-line tool supplies the 16 bytes and
/// <see cref="FromBytes"/> accepts them, so the core keeps no source of non-deterministic randomness.
/// </para>
/// <para>
/// The default value is unset, as with <see cref="ContentHash"/>. A human-readable pack name is
/// metadata and is never an identity (decision 0006).
/// </para>
/// </remarks>
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
