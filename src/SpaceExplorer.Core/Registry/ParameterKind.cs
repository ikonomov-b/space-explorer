namespace SpaceExplorer.Core.Registry;

/// <summary>
/// The closed authoritative parameter schema of decision 0031 as registry revision 1 admits it. A later
/// kind is a registry revision (decision 0035), never a silent extension; grids and cube-sphere grids
/// arrive with the first category that needs them.
/// </summary>
public enum ParameterKind : byte
{
    /// <summary>One byte, 0 or 1.</summary>
    Bool = 1,

    /// <summary>An index into the descriptor's enum labels.</summary>
    Enum = 2,

    /// <summary>A signed 64-bit integer, or fixed-point when the descriptor declares fraction bits, within the descriptor's range.</summary>
    Integer = 3,

    /// <summary>A signed 32-bit binary turn within the descriptor's range (decision 0036).</summary>
    BinaryTurn = 4,

    /// <summary>Three binary turns: yaw, pitch, roll (decision 0036).</summary>
    Rotation = 5,

    /// <summary>Three integers or fixed-point values, each within the descriptor's range.</summary>
    Vector3 = 6,

    /// <summary>Four 8-bit channels: red, green, blue, alpha.</summary>
    Colour = 7,

    /// <summary>An exact reference to a definition of the descriptor's reference category (decision 0031).</summary>
    PrimitiveRef = 8,

    /// <summary>
    /// An ordered array of exact references to the descriptor's reference category, of a length between
    /// the descriptor's own minimum and maximum, checked before allocation
    /// ([decision 0070](../../../docs/decisions/0070-a-biome-is-a-derived-set-registry-revision-9-and-grammar-version-10.md)
    /// clause 4). The documented schema has admitted ordered arrays since before revision 1; this is the
    /// first category to store one, which is when a kind arrives and not before.
    /// </summary>
    RefList = 9,
}
