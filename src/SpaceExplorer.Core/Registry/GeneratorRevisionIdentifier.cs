using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// A generator revision identifier is a stream path of the form <c>&lt;generator&gt;/&lt;k&gt;</c> with
/// <c>k</c> a positive integer, such as <c>terrain-heightfield/1</c> (decision 0035).
/// </summary>
public static class GeneratorRevisionIdentifier
{
    /// <summary>Throws unless <paramref name="identifier"/> has the required form.</summary>
    /// <exception cref="ArgumentException">The identifier is not a canonical path ending in a positive integer revision.</exception>
    public static void Validate(string identifier)
    {
        StreamPath.Validate(identifier);

        int separator = identifier.LastIndexOf(StreamPath.Separator);
        if (separator < 0)
        {
            throw new ArgumentException($"Generator revision identifier '{identifier}' has no '/<k>' revision segment.", nameof(identifier));
        }

        string revision = identifier[(separator + 1)..];
        bool positiveInteger = revision.Length > 0 && revision[0] != '0' && revision.All(character => character is >= '0' and <= '9');
        if (!positiveInteger)
        {
            throw new ArgumentException($"Generator revision identifier '{identifier}' must end in a positive integer revision.", nameof(identifier));
        }
    }
}
