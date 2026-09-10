using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// What a definition or template declares for one of its category's connector kinds: the tags it
/// provides and the tags a child must provide (decision 0031). Tags are canonical text.
/// </summary>
public sealed record ConnectorDeclaration
{
    public const int MaxTags = TagList.MaxTags;

    public ConnectorDeclaration(IReadOnlyList<string> providedTags, IReadOnlyList<string> requiredTags)
    {
        ProvidedTags = TagList.Validate(providedTags, nameof(providedTags));
        RequiredTags = TagList.Validate(requiredTags, nameof(requiredTags));
    }

    public static ConnectorDeclaration Empty { get; } = new([], []);

    public IReadOnlyList<string> ProvidedTags { get; }
    public IReadOnlyList<string> RequiredTags { get; }

    /// <summary>Whether every tag <paramref name="required"/> by a parent appears among this declaration's provided tags.</summary>
    public bool Satisfies(IReadOnlyList<string> required) => TagList.Satisfies(ProvidedTags, required);

    internal void Encode(CanonicalWriter writer)
    {
        TagList.Encode(writer, ProvidedTags);
        TagList.Encode(writer, RequiredTags);
    }

    internal static ConnectorDeclaration Decode(CanonicalReader reader)
    {
        try
        {
            return new ConnectorDeclaration(TagList.Decode(reader), TagList.Decode(reader));
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }
    }
}
