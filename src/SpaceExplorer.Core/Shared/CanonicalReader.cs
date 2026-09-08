using System.Text;

namespace SpaceExplorer.Core.Shared;

/// <summary>
/// Parses the canonical byte string a <see cref="CanonicalWriter"/> produced: the format version and
/// domain are read and checked in the constructor, then the fields in their declared order through the
/// matching <c>Read</c> method for each <c>Write</c> method. Field order is the caller's responsibility,
/// exactly as it is for the writer (decision 0020).
/// </summary>
public sealed class CanonicalReader
{
    private readonly byte[] _bytes;
    private int _position;

    /// <summary>Opens a byte string, checking its format version and domain against what the caller expects.</summary>
    /// <param name="bytes">The canonical bytes, normally a stored or received record.</param>
    /// <param name="expectedDomain">The domain the caller's record type was written under, in <see cref="StreamPath"/> canonical form.</param>
    /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="expectedDomain"/> is not in canonical form.</exception>
    /// <exception cref="FormatException">The bytes are truncated, or their format version or domain does not match.</exception>
    public CanonicalReader(byte[] bytes, string expectedDomain)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        StreamPath.Validate(expectedDomain);

        _bytes = bytes;
        _position = 0;

        byte formatVersion = ReadUInt8();
        if (formatVersion != CanonicalWriter.FormatVersion)
        {
            throw new FormatException(
                $"Expected canonical format version {CanonicalWriter.FormatVersion}; found {formatVersion}.");
        }

        string domain = Encoding.UTF8.GetString(ReadBytes());
        if (domain != expectedDomain)
        {
            throw new FormatException($"Expected domain '{expectedDomain}'; found '{domain}'.");
        }
    }

    /// <summary>Whether every byte has been read.</summary>
    public bool IsAtEnd => _position == _bytes.Length;

    /// <summary>Reads one byte.</summary>
    /// <exception cref="FormatException">No byte remains.</exception>
    public byte ReadUInt8() => NextByte();

    /// <summary>Reads an unsigned 16-bit value from two little-endian bytes.</summary>
    public ushort ReadUInt16() => (ushort)ReadLittleEndian(2);

    /// <summary>Reads an unsigned 32-bit value from four little-endian bytes.</summary>
    public uint ReadUInt32() => (uint)ReadLittleEndian(4);

    /// <summary>Reads an unsigned 64-bit value from eight little-endian bytes.</summary>
    public ulong ReadUInt64() => ReadLittleEndian(8);

    /// <summary>Reads a signed 16-bit value from two little-endian two's complement bytes.</summary>
    public short ReadInt16() => unchecked((short)ReadLittleEndian(2));

    /// <summary>Reads a signed 32-bit value from four little-endian two's complement bytes.</summary>
    public int ReadInt32() => unchecked((int)ReadLittleEndian(4));

    /// <summary>Reads a signed 64-bit value from eight little-endian two's complement bytes.</summary>
    public long ReadInt64() => unchecked((long)ReadLittleEndian(8));

    /// <summary>Reads a LEB128-encoded unsigned value: seven bits per byte, low group first, high bit set on every byte but the last.</summary>
    /// <exception cref="FormatException">The bytes are truncated or the encoding runs past 64 bits.</exception>
    public ulong ReadVarUInt()
    {
        ulong result = 0;
        int shift = 0;

        while (true)
        {
            byte next = NextByte();
            result |= (ulong)(next & 0x7F) << shift;

            if ((next & 0x80) == 0)
            {
                return result;
            }

            shift += 7;
            if (shift >= 64)
            {
                throw new FormatException("A variable-width integer did not terminate within 64 bits.");
            }
        }
    }

    /// <summary>Reads a zig-zag encoded LEB128 value, mapping 0, 1, 2, 3 back to 0, -1, 1, -2.</summary>
    public long ReadVarInt()
    {
        ulong zigzag = ReadVarUInt();
        return unchecked((long)(zigzag >> 1)) ^ -unchecked((long)(zigzag & 1UL));
    }

    /// <summary>Reads a sequence length written by <see cref="CanonicalWriter.WriteCount"/>.</summary>
    /// <exception cref="FormatException">The value does not fit in a non-negative <see cref="int"/>.</exception>
    public int ReadCount()
    {
        ulong count = ReadVarUInt();
        if (count > int.MaxValue)
        {
            throw new FormatException($"A count of {count} does not fit in a non-negative Int32.");
        }

        return (int)count;
    }

    /// <summary>Reads a length prefix followed by that many raw bytes.</summary>
    public byte[] ReadBytes() => NextBytes(ReadCount());

    /// <summary>Reads a length-prefixed run of text written by <see cref="CanonicalWriter.WriteText"/>.</summary>
    /// <exception cref="FormatException">A decoded byte is outside the <see cref="StreamPath"/> character set.</exception>
    public string ReadText()
    {
        byte[] raw = ReadBytes();
        var characters = new char[raw.Length];

        for (int index = 0; index < raw.Length; index++)
        {
            char character = (char)raw[index];
            if (!StreamPath.IsPermitted(character))
            {
                throw new FormatException(
                    $"Canonical text admits only ASCII letters, digits, '_' and '-'; found byte 0x{raw[index]:x2}.");
            }

            characters[index] = character;
        }

        return new string(characters);
    }

    /// <summary>Reads a length-prefixed path written by <see cref="CanonicalWriter.WritePath"/>.</summary>
    /// <exception cref="FormatException">The bytes are not a canonical stream path.</exception>
    public string ReadPath()
    {
        string path = Encoding.UTF8.GetString(ReadBytes());
        try
        {
            StreamPath.Validate(path);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }

        return path;
    }

    /// <summary>Reads the 32 bytes <see cref="CanonicalWriter.WriteContentHash"/> wrote.</summary>
    public ContentHash ReadContentHash() => ContentHash.FromBytes(NextBytes(ContentHash.ByteCount));

    /// <summary>Reads the 16 bytes <see cref="CanonicalWriter.WritePackId"/> wrote.</summary>
    public PackId ReadPackId() => PackId.FromBytes(NextBytes(PackId.ByteCount));

    /// <summary>Reads <paramref name="count"/> bytes and assembles them little-endian, least significant first.</summary>
    private ulong ReadLittleEndian(int count)
    {
        byte[] raw = NextBytes(count);
        ulong result = 0;

        for (int index = 0; index < count; index++)
        {
            result |= (ulong)raw[index] << (8 * index);
        }

        return result;
    }

    /// <summary>Returns the next byte and advances past it.</summary>
    /// <exception cref="FormatException">No byte remains.</exception>
    private byte NextByte()
    {
        if (_position >= _bytes.Length)
        {
            throw new FormatException($"Expected one more byte at position {_position}; none remain.");
        }

        return _bytes[_position++];
    }

    /// <summary>Returns the next <paramref name="count"/> bytes and advances past them.</summary>
    /// <exception cref="FormatException">Fewer than <paramref name="count"/> bytes remain.</exception>
    private byte[] NextBytes(int count)
    {
        if (count > _bytes.Length - _position)
        {
            throw new FormatException(
                $"Expected {count} more byte(s) at position {_position}; only {_bytes.Length - _position} remain.");
        }

        byte[] result = _bytes[_position..(_position + count)];
        _position += count;
        return result;
    }
}
