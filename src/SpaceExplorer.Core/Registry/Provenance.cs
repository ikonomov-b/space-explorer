using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Registry;

/// <summary>The kind of a definition; every core-release definition is generated (decision 0002).</summary>
public enum ProvenanceKind : byte
{
    Generated = 1,
}

/// <summary>Where a definition came from: its kind and the exact template revision that produced it (decisions 0002, 0031).</summary>
public sealed record Provenance
{
    public Provenance(ProvenanceKind kind, PackId templatePack, uint templateId, ContentHash templateHash)
    {
        if (!System.Enum.IsDefined(kind))
        {
            throw new ArgumentException($"Unknown provenance kind {(byte)kind}.", nameof(kind));
        }

        if (templatePack.IsUnset || templateId == 0 || templateHash.IsUnset)
        {
            throw new ArgumentException("Provenance must name an exact template revision: pack, non-zero ID, and hash.", nameof(templateId));
        }

        Kind = kind;
        TemplatePack = templatePack;
        TemplateId = templateId;
        TemplateHash = templateHash;
    }

    public ProvenanceKind Kind { get; }
    public PackId TemplatePack { get; }
    public uint TemplateId { get; }
    public ContentHash TemplateHash { get; }

    internal void Encode(CanonicalWriter writer)
    {
        writer.WriteUInt8((byte)Kind);
        writer.WritePackId(TemplatePack);
        writer.WriteUInt32(TemplateId);
        writer.WriteContentHash(TemplateHash);
    }

    internal static Provenance Decode(CanonicalReader reader)
    {
        try
        {
            return new Provenance((ProvenanceKind)reader.ReadUInt8(), reader.ReadPackId(), reader.ReadUInt32(), reader.ReadContentHash());
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message, exception);
        }
    }
}
