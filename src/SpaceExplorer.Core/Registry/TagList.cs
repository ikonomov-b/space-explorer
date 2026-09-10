using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// A list of compatibility tags, as a connector declares them and as a definition carries them: canonical
/// text, unique, and bounded. One implementation, because a tag means the same thing wherever it is
/// declared ([decision 0055](../../../docs/decisions/0055-definition-level-tags-and-tagged-references.md)).
/// </summary>
internal static class TagList
{
    /// <summary>How many tags one list may hold.</summary>
    public const int MaxTags = 16;

    /// <summary>Checks that <paramref name="tags"/> are canonical, unique, and within the bound.</summary>
    /// <exception cref="ArgumentException">A tag is not canonical text, repeats, or the list is too long.</exception>
    public static string[] Validate(IReadOnlyList<string> tags, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(tags, parameterName);
        if (tags.Count > MaxTags)
        {
            throw new ArgumentException($"A tag list holds at most {MaxTags} tags; received {tags.Count}.", parameterName);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string tag in tags)
        {
            StreamPath.Validate(tag);
            if (!seen.Add(tag))
            {
                throw new ArgumentException($"Tag '{tag}' is repeated.", parameterName);
            }
        }

        return [.. tags];
    }

    public static void Encode(CanonicalWriter writer, IReadOnlyList<string> tags)
    {
        writer.WriteCount(tags.Count);
        foreach (string tag in tags)
        {
            writer.WriteText(tag);
        }
    }

    /// <exception cref="FormatException">The list is longer than the bound.</exception>
    public static string[] Decode(CanonicalReader reader)
    {
        int count = reader.ReadCount();
        if (count > MaxTags)
        {
            throw new FormatException($"A tag list holds at most {MaxTags} tags; found {count}.");
        }

        var tags = new string[count];
        for (int index = 0; index < count; index++)
        {
            tags[index] = reader.ReadText();
        }

        return tags;
    }

    /// <summary>Whether every tag in <paramref name="required"/> appears in <paramref name="provided"/>.</summary>
    public static bool Satisfies(IReadOnlyList<string> provided, IReadOnlyList<string> required) => required.All(provided.Contains);
}
