using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// One parameter of a category's schema: its label, kind, fixed-point scale, range, enum labels, or
/// reference category (decision 0035). Values are laid out by this descriptor, so a change to it is a
/// registry revision.
/// </summary>
public sealed record ParameterDescriptor
{
    /// <summary>The most fraction bits a fixed-point parameter may declare.</summary>
    public const byte MaxFractionBits = 62;

    /// <summary>The most labels an enum parameter may declare.</summary>
    public const int MaxEnumLabels = 256;

    public ParameterDescriptor(string label, ParameterKind kind, byte fractionBits, long min, long max, IReadOnlyList<string> enumLabels, uint refCategory)
    {
        StreamPath.Validate(label);
        ArgumentNullException.ThrowIfNull(enumLabels);

        if (!System.Enum.IsDefined(kind))
        {
            throw new ArgumentException($"Parameter '{label}' has unknown kind {(byte)kind}.", nameof(kind));
        }

        bool scaled = kind is ParameterKind.Integer or ParameterKind.Vector3;
        if (fractionBits > MaxFractionBits || (!scaled && fractionBits != 0))
        {
            throw new ArgumentException($"Parameter '{label}' of kind {kind} cannot declare {fractionBits} fraction bits.", nameof(fractionBits));
        }

        bool ranged = kind is ParameterKind.Integer or ParameterKind.Vector3 or ParameterKind.BinaryTurn;
        if (ranged && min > max)
        {
            throw new ArgumentException($"Parameter '{label}' has an empty range [{min}, {max}].", nameof(min));
        }

        if (kind == ParameterKind.BinaryTurn && (min < int.MinValue || max > int.MaxValue))
        {
            throw new ArgumentException($"Parameter '{label}' is a binary turn; its range must fit a signed 32-bit value.", nameof(min));
        }

        if (!ranged && (min != 0 || max != 0))
        {
            throw new ArgumentException($"Parameter '{label}' of kind {kind} carries no range.", nameof(min));
        }

        if (kind == ParameterKind.Enum)
        {
            if (enumLabels.Count == 0 || enumLabels.Count > MaxEnumLabels)
            {
                throw new ArgumentException($"Enum parameter '{label}' must declare 1 to {MaxEnumLabels} labels.", nameof(enumLabels));
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string enumLabel in enumLabels)
            {
                StreamPath.Validate(enumLabel);
                if (!seen.Add(enumLabel))
                {
                    throw new ArgumentException($"Enum parameter '{label}' repeats label '{enumLabel}'.", nameof(enumLabels));
                }
            }
        }
        else if (enumLabels.Count != 0)
        {
            throw new ArgumentException($"Parameter '{label}' of kind {kind} carries no enum labels.", nameof(enumLabels));
        }

        if ((kind == ParameterKind.PrimitiveRef) != (refCategory != 0))
        {
            throw new ArgumentException($"Parameter '{label}' must name a reference category exactly when its kind is PrimitiveRef.", nameof(refCategory));
        }

        Label = label;
        Kind = kind;
        FractionBits = fractionBits;
        Min = min;
        Max = max;
        EnumLabels = [.. enumLabels];
        RefCategory = refCategory;
    }

    public string Label { get; }
    public ParameterKind Kind { get; }
    public byte FractionBits { get; }
    public long Min { get; }
    public long Max { get; }
    public IReadOnlyList<string> EnumLabels { get; }
    public uint RefCategory { get; }

    /// <summary>How many scalar components a value of this kind carries in its ranges: one, three, or four; zero for kinds without a range.</summary>
    public int RangeComponents => Kind switch
    {
        ParameterKind.Integer or ParameterKind.BinaryTurn => 1,
        ParameterKind.Rotation or ParameterKind.Vector3 => 3,
        ParameterKind.Colour => 4,
        _ => 0,
    };

    public static ParameterDescriptor Bool(string label) => new(label, ParameterKind.Bool, 0, 0, 0, [], 0);

    public static ParameterDescriptor Choice(string label, params string[] labels) => new(label, ParameterKind.Enum, 0, 0, 0, labels, 0);

    public static ParameterDescriptor Integer(string label, long min, long max, byte fractionBits = 0) => new(label, ParameterKind.Integer, fractionBits, min, max, [], 0);

    public static ParameterDescriptor BinaryTurn(string label, int min = int.MinValue, int max = int.MaxValue) => new(label, ParameterKind.BinaryTurn, 0, min, max, [], 0);

    public static ParameterDescriptor Rotation(string label) => new(label, ParameterKind.Rotation, 0, 0, 0, [], 0);

    public static ParameterDescriptor Vector3(string label, long min, long max, byte fractionBits = 0) => new(label, ParameterKind.Vector3, fractionBits, min, max, [], 0);

    public static ParameterDescriptor Colour(string label) => new(label, ParameterKind.Colour, 0, 0, 0, [], 0);

    public static ParameterDescriptor Ref(string label, uint category) => new(label, ParameterKind.PrimitiveRef, 0, 0, 0, [], category);

    internal void Encode(CanonicalWriter writer)
    {
        writer.WriteText(Label);
        writer.WriteUInt8((byte)Kind);
        writer.WriteUInt8(FractionBits);
        writer.WriteVarInt(Min);
        writer.WriteVarInt(Max);
        writer.WriteCount(EnumLabels.Count);
        foreach (string enumLabel in EnumLabels)
        {
            writer.WriteText(enumLabel);
        }

        writer.WriteUInt32(RefCategory);
    }

    internal static ParameterDescriptor Decode(CanonicalReader reader)
    {
        string label = reader.ReadText();
        var kind = (ParameterKind)reader.ReadUInt8();
        byte fractionBits = reader.ReadUInt8();
        long min = reader.ReadVarInt();
        long max = reader.ReadVarInt();
        int labelCount = reader.ReadCount();
        if (labelCount > MaxEnumLabels)
        {
            throw new FormatException($"Parameter '{label}' declares {labelCount} enum labels; the bound is {MaxEnumLabels}.");
        }

        var labels = new string[labelCount];
        for (int index = 0; index < labelCount; index++)
        {
            labels[index] = reader.ReadText();
        }

        uint refCategory = reader.ReadUInt32();

        try
        {
            return new ParameterDescriptor(label, kind, fractionBits, min, max, labels, refCategory);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }
    }
}
