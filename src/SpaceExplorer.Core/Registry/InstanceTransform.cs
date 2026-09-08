using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// The attachment transform of one instance, of the single type its connector kind admits: a rigid
/// transform, orbital elements, or a surface anchor (decision 0036). Components are held in the
/// encoding order of <see cref="TransformSchema"/>, so no node carries a transform its frame cannot hold.
/// </summary>
public sealed record InstanceTransform
{
    public InstanceTransform(TransformKind kind, IReadOnlyList<long> components)
    {
        ArgumentNullException.ThrowIfNull(components);
        IReadOnlyList<TransformComponent> schema = TransformSchema.For(kind);

        if (components.Count != schema.Count)
        {
            throw new ArgumentException($"A {kind} transform has {schema.Count} components; received {components.Count}.", nameof(components));
        }

        for (int index = 0; index < components.Count; index++)
        {
            if (components[index] < schema[index].Min || components[index] > schema[index].Max)
            {
                throw new ArgumentException($"Component '{schema[index].Label}' is {components[index]}, outside the range its width admits.", nameof(components));
            }
        }

        Kind = kind;
        Components = [.. components];
    }

    public TransformKind Kind { get; }

    /// <summary>The component values in the encoding order of <see cref="TransformSchema"/>.</summary>
    public IReadOnlyList<long> Components { get; }

    /// <summary>The value of the component labelled <paramref name="label"/>.</summary>
    /// <exception cref="KeyNotFoundException">This transform type has no such component.</exception>
    public long Component(string label)
    {
        IReadOnlyList<TransformComponent> schema = TransformSchema.For(Kind);
        for (int index = 0; index < schema.Count; index++)
        {
            if (schema[index].Label == label)
            {
                return Components[index];
            }
        }

        throw new KeyNotFoundException($"A {Kind} transform has no component '{label}'.");
    }

    internal void Encode(CanonicalWriter writer)
    {
        IReadOnlyList<TransformComponent> schema = TransformSchema.For(Kind);
        for (int index = 0; index < schema.Count; index++)
        {
            long value = Components[index];
            switch (schema[index].Width)
            {
                case TransformComponentWidth.Signed:
                    writer.WriteVarInt(value);
                    break;
                case TransformComponentWidth.Unsigned:
                    writer.WriteVarUInt((ulong)value);
                    break;
                case TransformComponentWidth.BinaryTurn:
                    writer.WriteInt32((int)value);
                    break;
                case TransformComponentWidth.Fraction32:
                    writer.WriteUInt32((uint)value);
                    break;
            }
        }
    }

    internal static InstanceTransform Decode(CanonicalReader reader, TransformKind kind)
    {
        IReadOnlyList<TransformComponent> schema = TransformSchema.For(kind);
        var components = new long[schema.Count];
        for (int index = 0; index < schema.Count; index++)
        {
            components[index] = schema[index].Width switch
            {
                TransformComponentWidth.Signed => reader.ReadVarInt(),
                TransformComponentWidth.Unsigned => ReadUnsigned(reader, schema[index]),
                TransformComponentWidth.BinaryTurn => reader.ReadInt32(),
                _ => reader.ReadUInt32(),
            };
        }

        try
        {
            return new InstanceTransform(kind, components);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }
    }

    private static long ReadUnsigned(CanonicalReader reader, TransformComponent component)
    {
        ulong value = reader.ReadVarUInt();
        return value <= long.MaxValue
            ? (long)value
            : throw new FormatException($"Component '{component.Label}' is {value}, beyond the 2^63-1 this build admits.");
    }
}
