using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// The bounded variation a grammar rule allows for an attachment transform: one inclusive sub-range per
/// component of the connector kind's transform type. Decision 0036 fixes the components and their
/// widths; their values are numeric content, so the range is pinned by the grammar's hash and revised by
/// a grammar version rather than by a definition or a registry revision (decisions 0031, 0034).
/// </summary>
public sealed record TransformRange
{
    public TransformRange(TransformKind kind, IReadOnlyList<(long Min, long Max)> bounds)
    {
        ArgumentNullException.ThrowIfNull(bounds);
        IReadOnlyList<TransformComponent> schema = TransformSchema.For(kind);

        if (bounds.Count != schema.Count)
        {
            throw new ArgumentException($"A {kind} transform has {schema.Count} components; received {bounds.Count} ranges.", nameof(bounds));
        }

        for (int index = 0; index < bounds.Count; index++)
        {
            (long min, long max) = bounds[index];
            TransformComponent component = schema[index];
            if (min > max || min < component.Min || max > component.Max)
            {
                throw new ArgumentException($"Component '{component.Label}' has range [{min}, {max}], which is empty or outside what its width admits.", nameof(bounds));
            }
        }

        Kind = kind;
        Bounds = [.. bounds];
    }

    public TransformKind Kind { get; }

    /// <summary>The inclusive bounds per component, in the encoding order of <see cref="TransformSchema"/>.</summary>
    public IReadOnlyList<(long Min, long Max)> Bounds { get; }

    /// <summary>Every component pinned to zero: the range of a connector that attaches at its parent's own frame.</summary>
    public static TransformRange Origin(TransformKind kind) =>
        new(kind, [.. TransformSchema.For(kind).Select(_ => (0L, 0L))]);

    internal void Encode(CanonicalWriter writer)
    {
        foreach ((long min, long max) in Bounds)
        {
            writer.WriteVarInt(min);
            writer.WriteVarInt(max);
        }
    }

    internal static TransformRange Decode(CanonicalReader reader, TransformKind kind)
    {
        var bounds = new (long, long)[TransformSchema.For(kind).Count];
        for (int index = 0; index < bounds.Length; index++)
        {
            bounds[index] = (reader.ReadVarInt(), reader.ReadVarInt());
        }

        try
        {
            return new TransformRange(kind, bounds);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }
    }
}
