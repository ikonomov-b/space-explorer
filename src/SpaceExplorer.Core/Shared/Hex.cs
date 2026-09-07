namespace SpaceExplorer.Core.Shared;

/// <summary>
/// The one lower-case hexadecimal rendering used by <see cref="ContentHash"/> and <see cref="PackId"/>,
/// which reach the file system as directory and file names under the data root (decision 0018).
/// </summary>
/// <remarks>
/// Rendering is lower case and parsing rejects upper case, so one identity has exactly one name. A
/// case-insensitive file system would otherwise treat two spellings of the same identity as one file
/// and a case-sensitive one as two, which is the cross-platform path hazard the technical design
/// requires be tested for (architecture and ownership).
/// </remarks>
internal static class Hex
{
    /// <summary>Renders <paramref name="bytes"/> as lower-case hexadecimal.</summary>
    internal static string ToLowerString(ReadOnlySpan<byte> bytes) => Convert.ToHexStringLower(bytes);

    /// <summary>Parses exactly <paramref name="expectedByteCount"/> bytes of lower-case hexadecimal.</summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="expectedByteCount">The exact number of bytes the text must encode.</param>
    /// <param name="what">What is being parsed, for the exception message.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="FormatException"><paramref name="text"/> is the wrong length or is not lower-case hexadecimal.</exception>
    internal static byte[] Parse(string text, int expectedByteCount, string what)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length != expectedByteCount * 2)
        {
            throw new FormatException(
                $"A {what} is {expectedByteCount * 2} hexadecimal characters; '{text}' has {text.Length}.");
        }

        foreach (char character in text)
        {
            if (character is not ((>= '0' and <= '9') or (>= 'a' and <= 'f')))
            {
                throw new FormatException(
                    $"A {what} admits only lower-case hexadecimal characters; found '{character}' in '{text}'.");
            }
        }

        return Convert.FromHexString(text);
    }
}
