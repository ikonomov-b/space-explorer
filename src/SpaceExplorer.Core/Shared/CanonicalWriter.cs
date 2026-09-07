namespace SpaceExplorer.Core.Shared;

/// <summary>
/// Builds the canonical byte string of a record: the format version, the domain label, then the fields in
/// their declared order. The encoding rules are frozen under <see cref="FormatVersion"/> by decision 0020,
/// and <see cref="ContentHash"/> hashes exactly these bytes. Field order and collection ordering are the
/// caller's responsibility; the writer exposes no floating-point operation.
/// </summary>
public sealed class CanonicalWriter
{
    /// <summary>The encoding version this build produces and validates (decision 0020).</summary>
    public const byte FormatVersion = 1;

    private readonly List<byte> _bytes = [];

    /// <summary>Opens a byte string for the record kind named by <paramref name="domain"/>.</summary>
    /// <param name="domain">
    /// A stable label for the kind of record being encoded, in <see cref="StreamPath"/> canonical form, such
    /// as <c>set-specification/1</c>.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="domain"/> is not in canonical form.</exception>
    public CanonicalWriter(string domain)
    {
        _bytes.Add(FormatVersion);
        WriteBytes(StreamPath.ToCanonicalBytes(domain));
    }

    /// <summary>Writes one byte.</summary>
    public void WriteUInt8(byte value) => _bytes.Add(value);

    /// <summary>Writes an unsigned 16-bit value as two little-endian bytes.</summary>
    public void WriteUInt16(ushort value) => WriteLittleEndian(value, byteCount: 2);

    /// <summary>Writes an unsigned 32-bit value as four little-endian bytes.</summary>
    public void WriteUInt32(uint value) => WriteLittleEndian(value, byteCount: 4);

    /// <summary>Writes an unsigned 64-bit value as eight little-endian bytes.</summary>
    public void WriteUInt64(ulong value) => WriteLittleEndian(value, byteCount: 8);

    /// <summary>Writes a signed 16-bit value as two little-endian two's complement bytes.</summary>
    public void WriteInt16(short value) => WriteLittleEndian(unchecked((ushort)value), byteCount: 2);

    /// <summary>Writes a signed 32-bit value as four little-endian two's complement bytes.</summary>
    public void WriteInt32(int value) => WriteLittleEndian(unchecked((uint)value), byteCount: 4);

    /// <summary>Writes a signed 64-bit value as eight little-endian two's complement bytes.</summary>
    public void WriteInt64(long value) => WriteLittleEndian(unchecked((ulong)value), byteCount: 8);

    /// <summary>Writes an unsigned value as LEB128: seven bits per byte, low group first, high bit set on every byte but the last.</summary>
    public void WriteVarUInt(ulong value)
    {
        ulong remaining = value;

        while (remaining >= 0x80UL)
        {
            _bytes.Add((byte)((remaining & 0x7FUL) | 0x80UL));
            remaining >>= 7;
        }

        _bytes.Add((byte)remaining);
    }

    /// <summary>Writes a signed value as zig-zag encoded LEB128, mapping 0, -1, 1, -2 to 0, 1, 2, 3.</summary>
    public void WriteVarInt(long value) => WriteVarUInt(unchecked((ulong)((value << 1) ^ (value >> 63))));

    /// <summary>Writes the length of a sequence before its elements, rejecting a negative count.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    public void WriteCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        WriteVarUInt((ulong)count);
    }

    /// <summary>Writes a length prefix followed by <paramref name="value"/>.</summary>
    public void WriteBytes(ReadOnlySpan<byte> value)
    {
        WriteCount(value.Length);
        _bytes.AddRange(value);
    }

    /// <summary>Writes a length prefix followed by the bytes of <paramref name="value"/>, which admits only the <see cref="StreamPath"/> character set and may be empty.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> holds a character outside the permitted set.</exception>
    public void WriteText(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        foreach (char character in value)
        {
            if (!StreamPath.IsPermitted(character))
            {
                throw new ArgumentException(
                    $"Canonical text admits only ASCII letters, digits, '_' and '-'; " +
                    $"found '{character}' in '{value}'.",
                    nameof(value));
            }
        }

        WriteCount(value.Length);

        foreach (char character in value)
        {
            // Every permitted character is ASCII, so each contributes exactly one byte and the
            // UTF-8 encoder is not needed to establish that.
            _bytes.Add((byte)character);
        }
    }

    /// <summary>Writes the 32 bytes of <paramref name="value"/>, which is a fixed width and needs no length prefix.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is unset.</exception>
    public void WriteContentHash(ContentHash value)
    {
        if (value.IsUnset)
        {
            throw new ArgumentException("An unset content hash has no canonical bytes.", nameof(value));
        }

        _bytes.AddRange(value.Bytes);
    }

    /// <summary>Writes the 16 bytes of <paramref name="value"/>, which is a fixed width and needs no length prefix.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is unset.</exception>
    public void WritePackId(PackId value)
    {
        if (value.IsUnset)
        {
            throw new ArgumentException("An unset pack identifier has no canonical bytes.", nameof(value));
        }

        _bytes.AddRange(value.Bytes);
    }

    /// <summary>Returns the canonical bytes written so far.</summary>
    public byte[] ToArray() => [.. _bytes];

    /// <summary>Returns the content hash of the canonical bytes written so far.</summary>
    public ContentHash ToContentHash() => ContentHash.Of(_bytes.ToArray());

    /// <summary>Appends the low <paramref name="byteCount"/> bytes of <paramref name="value"/>, least significant first.</summary>
    private void WriteLittleEndian(ulong value, int byteCount)
    {
        for (int index = 0; index < byteCount; index++)
        {
            _bytes.Add((byte)(value >> (8 * index)));
        }
    }
}
