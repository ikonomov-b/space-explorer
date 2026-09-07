using System.Text;

namespace SpaceExplorer.Core.Shared;

/// <summary>
/// The canonical form of a random-stream path, such as
/// <c>system/planet/0/region/3/site/1/artifact/2</c>. Pinned under
/// <see cref="GeneratorVersion"/>, since the derived bytes feed <see cref="Mix64.Derive"/>
/// (decision 0008: "path canonicalisation, separators, and encoding are pinned with the generator
/// version").
/// </summary>
/// <remarks>
/// <para>
/// Segments are restricted to ASCII letters, digits, <c>_</c> and <c>-</c>. The restriction is the
/// point: with no non-ASCII byte admitted, no Unicode normalisation form, case-folding rule, or
/// locale can ever change the bytes a path produces. Paths are internal identifiers built by the
/// generator, not player input, so the restriction costs nothing.
/// </para>
/// <para>
/// Relaxing this later is a safe generator version change, because every path valid now stays valid
/// and keeps its bytes. Tightening it would not be, which is why it starts strict.
/// </para>
/// <para>
/// Path strings reach persisted data: artifact identity is the world identity plus the stable artifact
/// path, and decision 0007 records substitutions in the world manifest as an artifact path and a
/// variation index. The canonical form is therefore a storage contract as well as a hashing input.
/// </para>
/// </remarks>
public static class StreamPath
{
    /// <summary>The only segment separator.</summary>
    public const char Separator = '/';

    /// <summary>
    /// Throws unless <paramref name="path"/> is in canonical form: one or more non-empty segments of
    /// permitted characters, joined by single separators, with no leading or trailing separator.
    /// </summary>
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

    /// <summary>
    /// Validates <paramref name="path"/> and returns its canonical UTF-8 bytes. Because every
    /// permitted character is ASCII, each character contributes exactly one byte and no byte-order
    /// mark is emitted.
    /// </summary>
    public static byte[] ToCanonicalBytes(string path)
    {
        Validate(path);
        return Encoding.UTF8.GetBytes(path);
    }

    /// <summary>
    /// The frozen character set: ASCII letters, digits, <c>_</c> and <c>-</c>. Shared with
    /// <see cref="CanonicalWriter.WriteText"/>, so canonical text and path segments admit exactly the
    /// same bytes and neither can drift from the other.
    /// </summary>
    internal static bool IsPermitted(char character) =>
        character is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_' or '-';
}
