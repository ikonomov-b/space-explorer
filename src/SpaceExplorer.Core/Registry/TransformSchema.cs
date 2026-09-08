namespace SpaceExplorer.Core.Registry;

/// <summary>How one transform component is encoded and what values it admits (decisions 0020, 0036).</summary>
public enum TransformComponentWidth : byte
{
    /// <summary>A signed 64-bit value as zig-zag LEB128; a fixed-point length in the containing frame's scale, or a tick count.</summary>
    Signed = 1,

    /// <summary>A non-negative value as LEB128. A value beyond 2^63-1 is refused, which for a length in metres is 975 light years and so outside any system this composes.</summary>
    Unsigned = 2,

    /// <summary>A binary turn: four little-endian two's complement bytes, the value divided by 2^32 being the angle in turns.</summary>
    BinaryTurn = 3,

    /// <summary>An unsigned 32-bit fraction of 2^32: four little-endian bytes.</summary>
    Fraction32 = 4,
}

/// <summary>One named component of a transform, with the width that fixes its range.</summary>
public sealed record TransformComponent(string Label, TransformComponentWidth Width)
{
    /// <summary>The lowest value the width admits.</summary>
    public long Min => Width switch
    {
        TransformComponentWidth.Signed => long.MinValue,
        TransformComponentWidth.BinaryTurn => int.MinValue,
        _ => 0L,
    };

    /// <summary>The highest value the width admits.</summary>
    public long Max => Width switch
    {
        TransformComponentWidth.BinaryTurn => int.MaxValue,
        TransformComponentWidth.Fraction32 => uint.MaxValue,
        _ => long.MaxValue,
    };
}

/// <summary>
/// The components of each transform type, in encoding order: the one table of what decision 0036 fixed,
/// so a connector kind's <see cref="TransformKind"/> alone gives a reader every field and its width.
/// </summary>
public static class TransformSchema
{
    private static readonly TransformComponent[] RigidComponents =
    [
        new("x", TransformComponentWidth.Signed),
        new("y", TransformComponentWidth.Signed),
        new("z", TransformComponentWidth.Signed),
        new("yaw", TransformComponentWidth.BinaryTurn),
        new("pitch", TransformComponentWidth.BinaryTurn),
        new("roll", TransformComponentWidth.BinaryTurn),
    ];

    private static readonly TransformComponent[] OrbitalComponents =
    [
        new("semi-major-axis", TransformComponentWidth.Unsigned),
        new("eccentricity", TransformComponentWidth.Fraction32),
        new("inclination", TransformComponentWidth.BinaryTurn),
        new("ascending-node", TransformComponentWidth.BinaryTurn),
        new("argument-of-periapsis", TransformComponentWidth.BinaryTurn),
        new("mean-anomaly", TransformComponentWidth.BinaryTurn),
        new("epoch", TransformComponentWidth.Signed),
    ];

    private static readonly TransformComponent[] SurfaceAnchorComponents =
    [
        new("latitude", TransformComponentWidth.BinaryTurn),
        new("longitude", TransformComponentWidth.BinaryTurn),
        new("height", TransformComponentWidth.Signed),
        new("heading", TransformComponentWidth.BinaryTurn),
    ];

    /// <summary>The components of <paramref name="kind"/> in encoding order.</summary>
    /// <exception cref="ArgumentException"><paramref name="kind"/> is not a known transform type.</exception>
    public static IReadOnlyList<TransformComponent> For(TransformKind kind) => kind switch
    {
        TransformKind.Rigid => RigidComponents,
        TransformKind.OrbitalElements => OrbitalComponents,
        TransformKind.SurfaceAnchor => SurfaceAnchorComponents,
        _ => throw new ArgumentException($"Unknown transform kind {(byte)kind}.", nameof(kind)),
    };
}
