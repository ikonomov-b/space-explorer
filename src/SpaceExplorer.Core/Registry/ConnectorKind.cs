using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>
/// A connector kind a category offers: its label, the one transform type it carries (decision 0036),
/// its child cardinality, and the categories a child may belong to (decision 0031).
/// </summary>
public sealed record ConnectorKind
{
    /// <summary>The most child categories one connector kind may list.</summary>
    public const int MaxChildCategories = 64;

    public ConnectorKind(string label, TransformKind transform, uint minCount, uint maxCount, IReadOnlyList<uint> childCategories)
    {
        StreamPath.Validate(label);
        ArgumentNullException.ThrowIfNull(childCategories);

        if (!Enum.IsDefined(transform))
        {
            throw new ArgumentException($"Connector '{label}' has unknown transform kind {(byte)transform}.", nameof(transform));
        }

        if (minCount > maxCount)
        {
            throw new ArgumentException($"Connector '{label}' has an empty cardinality [{minCount}, {maxCount}].", nameof(minCount));
        }

        if (childCategories.Count > MaxChildCategories)
        {
            throw new ArgumentException($"Connector '{label}' lists {childCategories.Count} child categories; the bound is {MaxChildCategories}.", nameof(childCategories));
        }

        var seen = new HashSet<uint>();
        foreach (uint category in childCategories)
        {
            if (category == 0 || !seen.Add(category))
            {
                throw new ArgumentException($"Connector '{label}' lists a reserved or repeated child category {category}.", nameof(childCategories));
            }
        }

        Label = label;
        Transform = transform;
        MinCount = minCount;
        MaxCount = maxCount;
        ChildCategories = [.. childCategories];
    }

    public string Label { get; }
    public TransformKind Transform { get; }
    public uint MinCount { get; }
    public uint MaxCount { get; }
    public IReadOnlyList<uint> ChildCategories { get; }

    internal void Encode(CanonicalWriter writer)
    {
        writer.WriteText(Label);
        writer.WriteUInt8((byte)Transform);
        writer.WriteVarUInt(MinCount);
        writer.WriteVarUInt(MaxCount);
        writer.WriteCount(ChildCategories.Count);
        foreach (uint category in ChildCategories)
        {
            writer.WriteUInt32(category);
        }
    }

    internal static ConnectorKind Decode(CanonicalReader reader)
    {
        string label = reader.ReadText();
        var transform = (TransformKind)reader.ReadUInt8();
        ulong minCount = reader.ReadVarUInt();
        ulong maxCount = reader.ReadVarUInt();
        if (minCount > uint.MaxValue || maxCount > uint.MaxValue)
        {
            throw new FormatException($"Connector '{label}' has a cardinality beyond 32 bits.");
        }

        int childCount = reader.ReadCount();
        if (childCount > MaxChildCategories)
        {
            throw new FormatException($"Connector '{label}' lists {childCount} child categories; the bound is {MaxChildCategories}.");
        }

        var children = new uint[childCount];
        for (int index = 0; index < childCount; index++)
        {
            children[index] = reader.ReadUInt32();
        }

        try
        {
            return new ConnectorKind(label, transform, (uint)minCount, (uint)maxCount, children);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }
    }
}
