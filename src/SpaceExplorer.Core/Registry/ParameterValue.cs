using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// One authoritative parameter value, laid out by a <see cref="ParameterDescriptor"/> when encoded
/// (decisions 0031, 0035). Integers and fixed-point values share a representation; the descriptor's
/// fraction bits give the scale.
/// </summary>
public readonly record struct ParameterValue
{
    private readonly long _a;
    private readonly long _b;
    private readonly long _c;
    private readonly long _d;
    private readonly PrimitiveRevisionRef _reference;

    private ParameterValue(ParameterKind kind, long a, long b, long c, long d, PrimitiveRevisionRef reference)
    {
        Kind = kind;
        _a = a;
        _b = b;
        _c = c;
        _d = d;
        _reference = reference;
    }

    public ParameterKind Kind { get; }

    public static ParameterValue Bool(bool value) => new(ParameterKind.Bool, value ? 1 : 0, 0, 0, 0, default);
    public static ParameterValue Enum(uint index) => new(ParameterKind.Enum, index, 0, 0, 0, default);
    public static ParameterValue Integer(long value) => new(ParameterKind.Integer, value, 0, 0, 0, default);
    public static ParameterValue BinaryTurn(int value) => new(ParameterKind.BinaryTurn, value, 0, 0, 0, default);
    public static ParameterValue Rotation(int yaw, int pitch, int roll) => new(ParameterKind.Rotation, yaw, pitch, roll, 0, default);
    public static ParameterValue Vector3(long x, long y, long z) => new(ParameterKind.Vector3, x, y, z, 0, default);
    public static ParameterValue Colour(byte red, byte green, byte blue, byte alpha) => new(ParameterKind.Colour, red, green, blue, alpha, default);
    public static ParameterValue Ref(PrimitiveRevisionRef reference) => new(ParameterKind.PrimitiveRef, 0, 0, 0, 0, reference);

    public bool AsBool => Expect(ParameterKind.Bool)._a != 0;
    public uint AsEnumIndex => (uint)Expect(ParameterKind.Enum)._a;
    public long AsInteger => Expect(ParameterKind.Integer)._a;
    public int AsBinaryTurn => (int)Expect(ParameterKind.BinaryTurn)._a;
    public (int Yaw, int Pitch, int Roll) AsRotation { get { ParameterValue v = Expect(ParameterKind.Rotation); return ((int)v._a, (int)v._b, (int)v._c); } }
    public (long X, long Y, long Z) AsVector3 { get { ParameterValue v = Expect(ParameterKind.Vector3); return (v._a, v._b, v._c); } }
    public (byte Red, byte Green, byte Blue, byte Alpha) AsColour { get { ParameterValue v = Expect(ParameterKind.Colour); return ((byte)v._a, (byte)v._b, (byte)v._c, (byte)v._d); } }
    public PrimitiveRevisionRef AsRef => Expect(ParameterKind.PrimitiveRef)._reference;

    /// <summary>The <paramref name="index"/>th scalar component, for range checks: one for integers and turns, three for rotations and vectors, four for colours.</summary>
    public long Component(int index) => index switch
    {
        0 => _a,
        1 => _b,
        2 => _c,
        3 => _d,
        _ => throw new ArgumentOutOfRangeException(nameof(index)),
    };

    /// <summary>Throws unless this value fits <paramref name="descriptor"/>: same kind, within range, a known enum index, a set reference.</summary>
    /// <exception cref="ArgumentException">The value does not fit.</exception>
    public void Validate(ParameterDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (Kind != descriptor.Kind)
        {
            throw new ArgumentException($"Parameter '{descriptor.Label}' expects {descriptor.Kind}; the value is {Kind}.");
        }

        switch (Kind)
        {
            case ParameterKind.Enum:
                if (_a >= descriptor.EnumLabels.Count)
                {
                    throw new ArgumentException($"Parameter '{descriptor.Label}' has {descriptor.EnumLabels.Count} labels; index {_a} is out of range.");
                }

                break;
            case ParameterKind.Integer:
            case ParameterKind.BinaryTurn:
            case ParameterKind.Vector3:
                for (int index = 0; index < descriptor.RangeComponents; index++)
                {
                    long component = Component(index);
                    if (component < descriptor.Min || component > descriptor.Max)
                    {
                        throw new ArgumentException($"Parameter '{descriptor.Label}' component {index} is {component}, outside [{descriptor.Min}, {descriptor.Max}].");
                    }
                }

                break;
            case ParameterKind.PrimitiveRef:
                if (_reference.IsUnset)
                {
                    throw new ArgumentException($"Parameter '{descriptor.Label}' holds an unset reference.");
                }

                break;
        }
    }

    internal void Encode(CanonicalWriter writer, ParameterDescriptor descriptor)
    {
        Validate(descriptor);

        switch (Kind)
        {
            case ParameterKind.Bool:
                writer.WriteUInt8((byte)_a);
                break;
            case ParameterKind.Enum:
                writer.WriteVarUInt((ulong)_a);
                break;
            case ParameterKind.Integer:
                writer.WriteVarInt(_a);
                break;
            case ParameterKind.BinaryTurn:
                writer.WriteInt32((int)_a);
                break;
            case ParameterKind.Rotation:
                writer.WriteInt32((int)_a);
                writer.WriteInt32((int)_b);
                writer.WriteInt32((int)_c);
                break;
            case ParameterKind.Vector3:
                writer.WriteVarInt(_a);
                writer.WriteVarInt(_b);
                writer.WriteVarInt(_c);
                break;
            case ParameterKind.Colour:
                writer.WriteUInt8((byte)_a);
                writer.WriteUInt8((byte)_b);
                writer.WriteUInt8((byte)_c);
                writer.WriteUInt8((byte)_d);
                break;
            case ParameterKind.PrimitiveRef:
                writer.WritePackId(_reference.Id.Pack);
                writer.WriteUInt32(_reference.Id.LocalId);
                writer.WriteContentHash(_reference.Hash);
                break;
        }
    }

    internal static ParameterValue Decode(CanonicalReader reader, ParameterDescriptor descriptor)
    {
        ParameterValue value = descriptor.Kind switch
        {
            ParameterKind.Bool => DecodeBool(reader, descriptor),
            ParameterKind.Enum => DecodeEnum(reader, descriptor),
            ParameterKind.Integer => Integer(reader.ReadVarInt()),
            ParameterKind.BinaryTurn => BinaryTurn(reader.ReadInt32()),
            ParameterKind.Rotation => Rotation(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
            ParameterKind.Vector3 => Vector3(reader.ReadVarInt(), reader.ReadVarInt(), reader.ReadVarInt()),
            ParameterKind.Colour => Colour(reader.ReadUInt8(), reader.ReadUInt8(), reader.ReadUInt8(), reader.ReadUInt8()),
            ParameterKind.PrimitiveRef => Ref(new PrimitiveRevisionRef(new PrimitiveId(reader.ReadPackId(), reader.ReadUInt32()), reader.ReadContentHash())),
            _ => throw new FormatException($"Parameter '{descriptor.Label}' has unknown kind {(byte)descriptor.Kind}."),
        };

        try
        {
            value.Validate(descriptor);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }

        return value;
    }

    private static ParameterValue DecodeBool(CanonicalReader reader, ParameterDescriptor descriptor)
    {
        byte raw = reader.ReadUInt8();
        if (raw > 1)
        {
            throw new FormatException($"Parameter '{descriptor.Label}' is a boolean; byte {raw} is neither 0 nor 1.");
        }

        return Bool(raw == 1);
    }

    private static ParameterValue DecodeEnum(CanonicalReader reader, ParameterDescriptor descriptor)
    {
        ulong index = reader.ReadVarUInt();
        if (index > uint.MaxValue)
        {
            throw new FormatException($"Parameter '{descriptor.Label}' has an enum index beyond 32 bits.");
        }

        return Enum((uint)index);
    }

    private ParameterValue Expect(ParameterKind kind) =>
        Kind == kind ? this : throw new InvalidOperationException($"The value is a {Kind}, not a {kind}.");
}
