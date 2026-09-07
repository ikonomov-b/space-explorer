using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Networking;

/// <summary>Why <see cref="JoinHandshake.Evaluate"/> rejected a <see cref="JoinRequest"/>.</summary>
public enum JoinRejectionReason : byte
{
    /// <summary>The sender's <see cref="JoinRequest.ProtocolVersion"/> does not match the host's.</summary>
    ProtocolVersionMismatch = 0,

    /// <summary>The sender's <see cref="JoinRequest.CampaignId"/> does not match the host's.</summary>
    CampaignMismatch = 1,

    /// <summary>The sender requested a manifest the host does not have.</summary>
    ManifestMismatch = 2,
}

/// <summary>
/// The host's answer to a <see cref="JoinRequest"/>: accepted with the revision to catch up to, or
/// rejected with the specific reason (decision 0028). Canonically encoded under its own domain, distinct
/// from <see cref="JoinRequest"/>'s.
/// </summary>
public abstract record JoinResponse
{
    /// <summary>The domain this record is canonically encoded under.</summary>
    public const string Domain = "join-response/1";

    private const byte AcceptedTag = 0;
    private const byte RejectedTag = 1;

    private JoinResponse()
    {
    }

    /// <summary>The request is accepted; the sender should catch up to <paramref name="HostRevision"/>.</summary>
    public sealed record Accepted(ulong HostRevision) : JoinResponse;

    /// <summary>The request is rejected for <paramref name="Reason"/>.</summary>
    public sealed record Rejected(JoinRejectionReason Reason) : JoinResponse;

    /// <summary>Encodes this response as canonical bytes.</summary>
    public byte[] ToCanonicalBytes()
    {
        var writer = new CanonicalWriter(Domain);

        switch (this)
        {
            case Accepted accepted:
                writer.WriteUInt8(AcceptedTag);
                writer.WriteUInt64(accepted.HostRevision);
                break;
            case Rejected rejected:
                writer.WriteUInt8(RejectedTag);
                writer.WriteUInt8((byte)rejected.Reason);
                break;
            default:
                throw new NotSupportedException($"Unrecognised {nameof(JoinResponse)} case: {GetType()}.");
        }

        return writer.ToArray();
    }

    /// <summary>Decodes a response written by <see cref="ToCanonicalBytes"/>.</summary>
    /// <exception cref="FormatException">The tag byte names neither known case.</exception>
    public static JoinResponse FromCanonicalBytes(byte[] bytes)
    {
        var reader = new CanonicalReader(bytes, Domain);
        byte tag = reader.ReadUInt8();

        return tag switch
        {
            AcceptedTag => new Accepted(reader.ReadUInt64()),
            RejectedTag => new Rejected((JoinRejectionReason)reader.ReadUInt8()),
            _ => throw new FormatException($"Unrecognised {nameof(JoinResponse)} tag {tag}."),
        };
    }
}
