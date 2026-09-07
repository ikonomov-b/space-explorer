using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Networking;

/// <summary>
/// What a joining peer sends first: protocol version, its own identity, the campaign it expects to
/// join, the manifests it already has, and the revision it is caught up to. Canonically encoded under
/// its own domain so a mismatched record can never be mistaken for a <see cref="JoinResponse"/>
/// (decision 0028). Campaign identity and manifests are placeholders: no decision fixes either shape yet.
/// </summary>
public sealed record JoinRequest
{
    /// <summary>The domain this record is canonically encoded under.</summary>
    public const string Domain = "join-request/1";

    /// <summary>The protocol version the sender speaks.</summary>
    public required uint ProtocolVersion { get; init; }

    /// <summary>The sender's transport identity.</summary>
    public required PeerId PeerId { get; init; }

    /// <summary>A placeholder for the campaign the sender expects to join.</summary>
    public required ContentHash CampaignId { get; init; }

    /// <summary>A placeholder for the set manifests the sender already has.</summary>
    public required IReadOnlyList<PackId> Manifests { get; init; }

    /// <summary>The state revision the sender is caught up to.</summary>
    public required ulong StateRevision { get; init; }

    /// <summary>Encodes this request as canonical bytes.</summary>
    public byte[] ToCanonicalBytes()
    {
        var writer = new CanonicalWriter(Domain);
        writer.WriteUInt32(ProtocolVersion);
        writer.WriteUInt32(PeerId.Value);
        writer.WriteContentHash(CampaignId);
        writer.WriteCount(Manifests.Count);

        foreach (PackId manifest in Manifests)
        {
            writer.WritePackId(manifest);
        }

        writer.WriteUInt64(StateRevision);
        return writer.ToArray();
    }

    /// <summary>Decodes a request written by <see cref="ToCanonicalBytes"/>.</summary>
    public static JoinRequest FromCanonicalBytes(byte[] bytes)
    {
        var reader = new CanonicalReader(bytes, Domain);
        uint protocolVersion = reader.ReadUInt32();
        var peerId = new PeerId(reader.ReadUInt32());
        ContentHash campaignId = reader.ReadContentHash();

        int manifestCount = reader.ReadCount();
        var manifests = new PackId[manifestCount];
        for (int index = 0; index < manifestCount; index++)
        {
            manifests[index] = reader.ReadPackId();
        }

        ulong stateRevision = reader.ReadUInt64();

        return new JoinRequest
        {
            ProtocolVersion = protocolVersion,
            PeerId = peerId,
            CampaignId = campaignId,
            Manifests = manifests,
            StateRevision = stateRevision,
        };
    }
}
