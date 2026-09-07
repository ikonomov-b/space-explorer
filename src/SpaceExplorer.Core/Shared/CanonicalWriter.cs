namespace SpaceExplorer.Core.Shared;

/// <summary>
/// Builds the canonical byte string of a record, which is what <see cref="ContentHash"/> hashes and
/// what identity-bearing storage holds. The technical design requires "canonical binary encodings and
/// a specified content hash such as SHA-256, not runtime object hashes" (destination identity and
/// determinism), and "explicit little-endian fields, lengths, and integrity checks" for packed vectors
/// (primitive sets and compact references).
/// </summary>
/// <remarks>
/// <para>
/// The encoding is frozen under <see cref="FormatVersion"/>, which is a separate axis from
/// <see cref="GeneratorVersion"/>: save format, generator, and content versions are independent
/// (technical design, persistence and compatibility). A change to any rule below is a format-version
/// increment, because it changes the hash of records whose bytes have not changed.
/// </para>
/// <para>The frozen rules:</para>
/// <list type="number">
///   <item>Every byte string opens with <see cref="FormatVersion"/> as one byte, then the domain label
///   as length-prefixed bytes. The domain separates roles, so the same field values encoded as two
///   different kinds of record can never produce the same hash.</item>
///   <item>Fixed-width integers are little-endian two's complement, assembled bytewise. Host byte order
///   is unreachable from this code, as it is from <see cref="Mix64"/>.</item>
///   <item>Variable-width integers are LEB128, and signed ones are zig-zag encoded first, so the
///   encoding of a small negative number is short.</item>
///   <item>Byte strings and text carry a variable-width length prefix, so concatenated fields cannot be
///   reinterpreted: <c>("a", "b")</c> and <c>("ab", "")</c> encode differently.</item>
///   <item>Text admits only the <see cref="StreamPath"/> character set. No admitted byte has a second
///   Unicode normalisation form, a case-folding rule, or a locale-dependent reading, so no hashed text
///   can vary between machines. Human-readable names are metadata and are never encoded here
///   (decision 0006).</item>
///   <item>There is no floating-point method, as on <see cref="Pcg32"/>. Authoritative quantities are
///   integers or fixed-point (decision 0010), and a float would make a hash depend on rounding.</item>
/// </list>
/// <para>
/// Two rules the caller owns, because no encoder can enforce them: fields are written in a fixed
/// declared order, and a collection is sorted into a defined order before it is written. Hashing an
/// unordered iteration would make identity depend on runtime layout, which decision 0008 forbids.
/// </para>
/// </remarks>
public sealed class CanonicalWriter
{
    /// <summary>The encoding version this build produces and validates.</summary>
    public const byte FormatVersion = 1;

    private readonly List<byte> _bytes = [];

    /// <summary>Opens a byte string for the record kind named by <paramref name="domain"/>.</summary>
    /// <param name="domain">
    /// A stable label for the kind of record being encoded, such as <c>set-specification</c>. It is in
    /// <see cref="StreamPath"/> canonical form, so a versioned label such as <c>set-specification/1</c>
    /// is admitted.
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

    /// <summary>
    /// Writes a signed value as a zig-zag encoded LEB128 value, mapping 0, -1, 1, -2 to 0, 1, 2, 3, so
    /// that a small magnitude of either sign costs one byte.
    /// </summary>
    public void WriteVarInt(long value) => WriteVarUInt(unchecked((ulong)((value << 1) ^ (value >> 63))));

    /// <summary>
    /// Writes the length of a sequence, before its elements. Distinct from
    /// <see cref="WriteVarUInt(ulong)"/> so that a count is range-checked at the point it is written,
    /// as the technical design requires of lengths before allocation.
    /// </summary>
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

    /// <summary>
    /// Writes a length prefix followed by the bytes of <paramref name="value"/>, which admits only the
    /// <see cref="StreamPath"/> character set and may be empty.
    /// </summary>
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
