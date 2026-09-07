using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Core.Networking;

/// <summary>
/// Evaluates a <see cref="JoinRequest"/> against what the host knows, rejecting unsupported
/// combinations explicitly rather than silently, one check at a time (decision 0028).
/// </summary>
public static class JoinHandshake
{
    /// <summary>
    /// Accepts <paramref name="request"/> with <paramref name="hostRevision"/> when its protocol
    /// version and campaign ID match the host's and every requested manifest is one the host has;
    /// otherwise rejects with the first mismatch found, in that order.
    /// </summary>
    public static JoinResponse Evaluate(
        JoinRequest request,
        uint hostProtocolVersion,
        ContentHash hostCampaignId,
        IReadOnlySet<PackId> hostManifests,
        ulong hostRevision)
    {
        if (request.ProtocolVersion != hostProtocolVersion)
        {
            return new JoinResponse.Rejected(JoinRejectionReason.ProtocolVersionMismatch);
        }

        if (request.CampaignId != hostCampaignId)
        {
            return new JoinResponse.Rejected(JoinRejectionReason.CampaignMismatch);
        }

        foreach (PackId manifest in request.Manifests)
        {
            if (!hostManifests.Contains(manifest))
            {
                return new JoinResponse.Rejected(JoinRejectionReason.ManifestMismatch);
            }
        }

        return new JoinResponse.Accepted(hostRevision);
    }
}
