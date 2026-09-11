using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// The bounded variation a template allows for one parameter: a sub-range per component, an allowed
/// subset of enum labels, an allowed set of booleans, or, for a reference, any earlier definition of the
/// descriptor's category. Templates are generation inputs, not runtime primitives (decision 0031).
/// </summary>
public sealed record ParameterRange
{
    private ParameterRange(ParameterKind kind, long[] min, long[] max, uint[] enumAllowed, byte boolMask, string[]? requiredTags = null)
    {
        Kind = kind;
        Min = min;
        Max = max;
        EnumAllowed = enumAllowed;
        BoolMask = boolMask;
        RequiredTags = requiredTags ?? [];
    }

    public ParameterKind Kind { get; }

    /// <summary>Per-component lower bounds, inclusive; one, three, or four entries by kind, none for other kinds.</summary>
    public IReadOnlyList<long> Min { get; }

    /// <summary>Per-component upper bounds, inclusive.</summary>
    public IReadOnlyList<long> Max { get; }

    /// <summary>The allowed enum indices, ascending.</summary>
    public IReadOnlyList<uint> EnumAllowed { get; }

    /// <summary>Bit 0 allows false, bit 1 allows true.</summary>
    public byte BoolMask { get; }

    /// <summary>
    /// For a reference, the tags a candidate definition must carry: the generator draws only from
    /// definitions whose own tags satisfy these, so a body takes a material that suits it rather than any
    /// material of the category (decision 0055). Empty for every other kind, and empty for a reference
    /// that accepts any definition of its category.
    /// </summary>
    public IReadOnlyList<string> RequiredTags { get; }

    public static ParameterRange Bool(bool allowFalse, bool allowTrue)
    {
        if (!allowFalse && !allowTrue)
        {
            throw new ArgumentException("A boolean range must allow at least one value.");
        }

        return new(ParameterKind.Bool, [], [], [], (byte)((allowFalse ? 1 : 0) | (allowTrue ? 2 : 0)));
    }

    public static ParameterRange Enum(params uint[] allowed)
    {
        if (allowed.Length == 0)
        {
            throw new ArgumentException("An enum range must allow at least one label.", nameof(allowed));
        }

        for (int index = 1; index < allowed.Length; index++)
        {
            if (allowed[index] <= allowed[index - 1])
            {
                throw new ArgumentException("Allowed enum indices must be ascending and distinct.", nameof(allowed));
            }
        }

        return new(ParameterKind.Enum, [], [], [.. allowed], 0);
    }

    public static ParameterRange Integer(long min, long max) => Components(ParameterKind.Integer, [min], [max]);
    public static ParameterRange BinaryTurn(int min, int max) => Components(ParameterKind.BinaryTurn, [min], [max]);
    public static ParameterRange Rotation((int Min, int Max) yaw, (int Min, int Max) pitch, (int Min, int Max) roll) =>
        Components(ParameterKind.Rotation, [yaw.Min, pitch.Min, roll.Min], [yaw.Max, pitch.Max, roll.Max]);
    public static ParameterRange Vector3((long Min, long Max) x, (long Min, long Max) y, (long Min, long Max) z) =>
        Components(ParameterKind.Vector3, [x.Min, y.Min, z.Min], [x.Max, y.Max, z.Max]);
    public static ParameterRange Colour((byte Min, byte Max) red, (byte Min, byte Max) green, (byte Min, byte Max) blue, (byte Min, byte Max) alpha) =>
        Components(ParameterKind.Colour, [red.Min, green.Min, blue.Min, alpha.Min], [red.Max, green.Max, blue.Max, alpha.Max]);
    /// <summary>A reference to any earlier definition of the descriptor's category carrying every tag in <paramref name="requiredTags"/>.</summary>
    public static ParameterRange Ref(params string[] requiredTags) => new(ParameterKind.PrimitiveRef, [], [], [], 0, TagList.Validate(requiredTags, nameof(requiredTags)));

    /// <summary>
    /// An ordered array: how many entries it may hold, and the tags each must carry
    /// ([decision 0070](../../../docs/decisions/0070-a-biome-is-a-derived-set-registry-revision-9-and-grammar-version-10.md)
    /// clause 4). The length is the range; the tags are the demand, exactly as a single reference states it.
    /// </summary>
    public static ParameterRange RefList(long minimum, long maximum, params string[] requiredTags) =>
        new(ParameterKind.RefList, [minimum], [maximum], [], 0, TagList.Validate(requiredTags, nameof(requiredTags)));

    private static ParameterRange Components(ParameterKind kind, long[] min, long[] max)
    {
        for (int index = 0; index < min.Length; index++)
        {
            if (min[index] > max[index])
            {
                throw new ArgumentException($"Component {index} has an empty range [{min[index]}, {max[index]}].");
            }
        }

        return new(kind, min, max, [], 0);
    }

    /// <summary>Throws unless this range fits inside <paramref name="descriptor"/>.</summary>
    /// <exception cref="ArgumentException">The range is of another kind or exceeds the descriptor's bounds.</exception>
    public void Validate(ParameterDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (Kind != descriptor.Kind)
        {
            throw new ArgumentException($"Parameter '{descriptor.Label}' expects {descriptor.Kind}; the range is {Kind}.");
        }

        if (Min.Count != descriptor.RangeComponents)
        {
            throw new ArgumentException($"Parameter '{descriptor.Label}' has {descriptor.RangeComponents} range components; the range has {Min.Count}.");
        }

        switch (Kind)
        {
            case ParameterKind.Enum:
                if (EnumAllowed[^1] >= descriptor.EnumLabels.Count)
                {
                    throw new ArgumentException($"Parameter '{descriptor.Label}' has {descriptor.EnumLabels.Count} labels; the range allows index {EnumAllowed[^1]}.");
                }

                break;
            case ParameterKind.Integer:
            case ParameterKind.BinaryTurn:
            case ParameterKind.Vector3:
                for (int index = 0; index < Min.Count; index++)
                {
                    if (Min[index] < descriptor.Min || Max[index] > descriptor.Max)
                    {
                        throw new ArgumentException($"Parameter '{descriptor.Label}' component {index} range [{Min[index]}, {Max[index]}] exceeds [{descriptor.Min}, {descriptor.Max}].");
                    }
                }

                break;
            case ParameterKind.Rotation:
                for (int index = 0; index < Min.Count; index++)
                {
                    if (Min[index] < int.MinValue || Max[index] > int.MaxValue)
                    {
                        throw new ArgumentException($"Parameter '{descriptor.Label}' rotation component {index} exceeds a binary turn.");
                    }
                }

                break;
            case ParameterKind.Colour:
                for (int index = 0; index < Min.Count; index++)
                {
                    if (Min[index] < 0 || Max[index] > 255)
                    {
                        throw new ArgumentException($"Parameter '{descriptor.Label}' colour channel {index} exceeds a byte.");
                    }
                }

                break;
        }
    }

    /// <summary>Whether <paramref name="value"/> lies within this range; references always do, since the pool is decided at generation.</summary>
    public bool Contains(ParameterValue value)
    {
        if (value.Kind != Kind)
        {
            return false;
        }

        return Kind switch
        {
            ParameterKind.Bool => (BoolMask & (value.AsBool ? 2 : 1)) != 0,
            ParameterKind.Enum => EnumAllowed.Contains(value.AsEnumIndex),
            ParameterKind.PrimitiveRef => true,
            _ => Enumerable.Range(0, Min.Count).All(index => value.Component(index) >= Min[index] && value.Component(index) <= Max[index]),
        };
    }

    /// <param name="carriesTags">Whether the enclosing record's registry revision carries tags (decision 0055).</param>
    internal void Encode(CanonicalWriter writer, ParameterDescriptor descriptor, bool carriesTags)
    {
        Validate(descriptor);

        if (Kind == ParameterKind.PrimitiveRef && RequiredTags.Count > 0 && !carriesTags)
        {
            throw new ArgumentException($"Parameter '{descriptor.Label}' requires tags of what it references, which a registry revision before 3 does not carry (decision 0055).", nameof(descriptor));
        }

        switch (Kind)
        {
            case ParameterKind.Bool:
                writer.WriteUInt8(BoolMask);
                break;
            case ParameterKind.Enum:
                writer.WriteCount(EnumAllowed.Count);
                foreach (uint index in EnumAllowed)
                {
                    writer.WriteVarUInt(index);
                }

                break;
            case ParameterKind.BinaryTurn:
            case ParameterKind.Rotation:
                for (int index = 0; index < Min.Count; index++)
                {
                    writer.WriteInt32((int)Min[index]);
                    writer.WriteInt32((int)Max[index]);
                }

                break;
            case ParameterKind.Colour:
                for (int index = 0; index < Min.Count; index++)
                {
                    writer.WriteUInt8((byte)Min[index]);
                    writer.WriteUInt8((byte)Max[index]);
                }

                break;
            case ParameterKind.Integer:
            case ParameterKind.Vector3:
                for (int index = 0; index < Min.Count; index++)
                {
                    writer.WriteVarInt(Min[index]);
                    writer.WriteVarInt(Max[index]);
                }

                break;
            case ParameterKind.PrimitiveRef:
                if (carriesTags)
                {
                    TagList.Encode(writer, RequiredTags);
                }

                break;
        }
    }

    /// <param name="carriesTags">Whether the enclosing record's registry revision carries tags (decision 0055).</param>
    internal static ParameterRange Decode(CanonicalReader reader, ParameterDescriptor descriptor, bool carriesTags)
    {
        try
        {
            ParameterRange range = descriptor.Kind switch
            {
                ParameterKind.Bool => DecodeBool(reader),
                ParameterKind.Enum => DecodeEnum(reader, descriptor),
                ParameterKind.BinaryTurn or ParameterKind.Rotation => DecodeComponents(reader, descriptor, r => r.ReadInt32()),
                ParameterKind.Colour => DecodeComponents(reader, descriptor, r => r.ReadUInt8()),
                ParameterKind.Integer or ParameterKind.Vector3 => DecodeComponents(reader, descriptor, r => r.ReadVarInt()),
                ParameterKind.PrimitiveRef => Ref(carriesTags ? TagList.Decode(reader) : []),
                _ => throw new FormatException($"Parameter '{descriptor.Label}' has unknown kind {(byte)descriptor.Kind}."),
            };
            range.Validate(descriptor);
            return range;
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }
    }

    private static ParameterRange DecodeBool(CanonicalReader reader)
    {
        byte mask = reader.ReadUInt8();
        if (mask is 0 or > 3)
        {
            throw new FormatException($"A boolean range mask must be 1, 2, or 3; found {mask}.");
        }

        return Bool((mask & 1) != 0, (mask & 2) != 0);
    }

    private static ParameterRange DecodeEnum(CanonicalReader reader, ParameterDescriptor descriptor)
    {
        int count = reader.ReadCount();
        if (count > ParameterDescriptor.MaxEnumLabels)
        {
            throw new FormatException($"Parameter '{descriptor.Label}' range allows {count} labels; the bound is {ParameterDescriptor.MaxEnumLabels}.");
        }

        var allowed = new uint[count];
        for (int index = 0; index < count; index++)
        {
            ulong value = reader.ReadVarUInt();
            if (value > uint.MaxValue)
            {
                throw new FormatException($"Parameter '{descriptor.Label}' range has an enum index beyond 32 bits.");
            }

            allowed[index] = (uint)value;
        }

        return Enum(allowed);
    }

    private static ParameterRange DecodeComponents(CanonicalReader reader, ParameterDescriptor descriptor, Func<CanonicalReader, long> read)
    {
        int components = descriptor.RangeComponents;
        var min = new long[components];
        var max = new long[components];
        for (int index = 0; index < components; index++)
        {
            min[index] = read(reader);
            max[index] = read(reader);
        }

        return Components(descriptor.Kind, min, max);
    }
}
