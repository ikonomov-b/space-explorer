using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// What a definition or template declares for one of its category's connector kinds: the tags it
/// provides and the tags a child must provide (decision 0031). Tags are canonical text.
/// </summary>
public sealed record ConnectorDeclaration
{
    public const int MaxTags = 16;

    public ConnectorDeclaration(IReadOnlyList<string> providedTags, IReadOnlyList<string> requiredTags)
    {
        ProvidedTags = ValidateTags(providedTags, nameof(providedTags));
        RequiredTags = ValidateTags(requiredTags, nameof(requiredTags));
    }

    public static ConnectorDeclaration Empty { get; } = new([], []);

    public IReadOnlyList<string> ProvidedTags { get; }
    public IReadOnlyList<string> RequiredTags { get; }

    /// <summary>Whether every tag <paramref name="required"/> by a parent appears among this declaration's provided tags.</summary>
    public bool Satisfies(IReadOnlyList<string> required) => required.All(ProvidedTags.Contains);

    private static string[] ValidateTags(IReadOnlyList<string> tags, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(tags, parameterName);
        if (tags.Count > MaxTags)
        {
            throw new ArgumentException($"A connector declares at most {MaxTags} tags; received {tags.Count}.", parameterName);
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

    internal void Encode(CanonicalWriter writer)
    {
        writer.WriteCount(ProvidedTags.Count);
        foreach (string tag in ProvidedTags)
        {
            writer.WriteText(tag);
        }

        writer.WriteCount(RequiredTags.Count);
        foreach (string tag in RequiredTags)
        {
            writer.WriteText(tag);
        }
    }

    internal static ConnectorDeclaration Decode(CanonicalReader reader)
    {
        try
        {
            return new ConnectorDeclaration(ReadTags(reader), ReadTags(reader));
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }
    }

    private static string[] ReadTags(CanonicalReader reader)
    {
        int count = reader.ReadCount();
        if (count > MaxTags)
        {
            throw new FormatException($"A connector declares at most {MaxTags} tags; found {count}.");
        }

        var tags = new string[count];
        for (int index = 0; index < count; index++)
        {
            tags[index] = reader.ReadText();
        }

        return tags;
    }
}
