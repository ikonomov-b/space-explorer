using System.Text;

namespace SpaceExplorer.Core.Shared;

/// <summary>
/// The canonical form of a random-stream path such as <c>system/planet/0/region/3/site/1/artifact/2</c>:
/// non-empty segments of ASCII letters, digits, <c>_</c> and <c>-</c>, joined by single separators,
/// case-sensitive. Frozen under <see cref="GeneratorVersion"/> (decisions 0008 and 0019); the same
/// character set bounds <see cref="CanonicalWriter.WriteText"/> (decision 0020).
/// </summary>
public static class StreamPath
{
    /// <summary>The only segment separator.</summary>
    public const char Separator = '/';

    /// <summary>Throws unless <paramref name="path"/> is in canonical form.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is not in canonical form.</exception>
    public static void Validate(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (path.Length == 0)
        {
            throw new ArgumentException("A stream path must have at least one segment.", nameof(path));
        }

        if (path[0] == Separator || path[^1] == Separator)
        {
            throw new ArgumentException(
                $"A stream path must not begin or end with '{Separator}': '{path}'.",
                nameof(path));
        }

        bool previousWasSeparator = false;

        foreach (char character in path)
        {
            if (character == Separator)
            {
                if (previousWasSeparator)
                {
                    throw new ArgumentException(
                        $"A stream path must not contain an empty segment: '{path}'.",
                        nameof(path));
                }

                previousWasSeparator = true;
                continue;
            }

            if (!IsPermitted(character))
            {
                throw new ArgumentException(
                    $"A stream path segment admits only ASCII letters, digits, '_' and '-'; " +
                    $"found '{character}' in '{path}'.",
                    nameof(path));
            }

            previousWasSeparator = false;
        }
    }

    /// <summary>Validates <paramref name="path"/> and returns its canonical UTF-8 bytes, one per character since every permitted character is ASCII.</summary>
    public static byte[] ToCanonicalBytes(string path)
    {
        Validate(path);
        return Encoding.UTF8.GetBytes(path);
    }

    /// <summary>The frozen character set, shared with <see cref="CanonicalWriter.WriteText"/> so the two cannot drift apart.</summary>
    internal static bool IsPermitted(char character) =>
        character is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_' or '-';
}
