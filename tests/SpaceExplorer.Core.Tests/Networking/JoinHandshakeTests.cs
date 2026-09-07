using SpaceExplorer.Core.Networking;
using SpaceExplorer.Core.Shared;
using Xunit;

namespace SpaceExplorer.Core.Tests.Networking;

public class JoinHandshakeTests
{
    private static readonly ContentHash CampaignId = ContentHash.Of("campaign"u8);
    private static readonly PackId ManifestA = PackId.FromSpecificationHash(ContentHash.Of("pack-a"u8));
    private static readonly PackId ManifestB = PackId.FromSpecificationHash(ContentHash.Of("pack-b"u8));

    private static JoinRequest ValidRequest() => new()
    {
        ProtocolVersion = ProtocolVersion.Current,
        PeerId = new PeerId(7),
        CampaignId = CampaignId,
        Manifests = [ManifestA, ManifestB],
        StateRevision = 42,
    };

    [Fact]
    public void A_join_request_round_trips_through_its_canonical_bytes()
    {
        JoinRequest original = ValidRequest();
        JoinRequest decoded = JoinRequest.FromCanonicalBytes(original.ToCanonicalBytes());

        Assert.Equal(original.ProtocolVersion, decoded.ProtocolVersion);
        Assert.Equal(original.PeerId, decoded.PeerId);
        Assert.Equal(original.CampaignId, decoded.CampaignId);
        Assert.Equal(original.Manifests, decoded.Manifests);
        Assert.Equal(original.StateRevision, decoded.StateRevision);
    }

    [Fact]
    public void An_accepted_join_response_round_trips_through_its_canonical_bytes()
    {
        JoinResponse.Accepted original = new(HostRevision: 5);
        var decoded = Assert.IsType<JoinResponse.Accepted>(JoinResponse.FromCanonicalBytes(original.ToCanonicalBytes()));

        Assert.Equal(original.HostRevision, decoded.HostRevision);
    }

    [Fact]
    public void A_rejected_join_response_round_trips_through_its_canonical_bytes()
    {
        JoinResponse.Rejected original = new(JoinRejectionReason.ManifestMismatch);
        var decoded = Assert.IsType<JoinResponse.Rejected>(JoinResponse.FromCanonicalBytes(original.ToCanonicalBytes()));

        Assert.Equal(original.Reason, decoded.Reason);
    }

    [Fact]
    public void A_matching_request_is_accepted_with_the_hosts_revision()
    {
        JoinResponse response = JoinHandshake.Evaluate(
            ValidRequest(),
            hostProtocolVersion: ProtocolVersion.Current,
            hostCampaignId: CampaignId,
            hostManifests: new HashSet<PackId> { ManifestA, ManifestB },
            hostRevision: 42);

        var accepted = Assert.IsType<JoinResponse.Accepted>(response);
        Assert.Equal(42UL, accepted.HostRevision);
    }

    [Fact]
    public void A_mismatched_protocol_version_is_rejected_before_anything_else_is_checked()
    {
        JoinRequest request = ValidRequest() with { ProtocolVersion = ProtocolVersion.Current + 1 };

        JoinResponse response = JoinHandshake.Evaluate(
            request,
            hostProtocolVersion: ProtocolVersion.Current,
            hostCampaignId: ContentHash.Of("a different campaign"u8), // also mismatched
            hostManifests: new HashSet<PackId>(), // also missing every manifest
            hostRevision: 0);

        var rejected = Assert.IsType<JoinResponse.Rejected>(response);
        Assert.Equal(JoinRejectionReason.ProtocolVersionMismatch, rejected.Reason);
    }

    [Fact]
    public void A_mismatched_campaign_is_rejected()
    {
        JoinResponse response = JoinHandshake.Evaluate(
            ValidRequest(),
            hostProtocolVersion: ProtocolVersion.Current,
            hostCampaignId: ContentHash.Of("a different campaign"u8),
            hostManifests: new HashSet<PackId> { ManifestA, ManifestB },
            hostRevision: 0);

        var rejected = Assert.IsType<JoinResponse.Rejected>(response);
        Assert.Equal(JoinRejectionReason.CampaignMismatch, rejected.Reason);
    }

    [Fact]
    public void A_missing_manifest_is_rejected()
    {
        JoinResponse response = JoinHandshake.Evaluate(
            ValidRequest(),
            hostProtocolVersion: ProtocolVersion.Current,
            hostCampaignId: CampaignId,
            hostManifests: new HashSet<PackId> { ManifestA }, // ManifestB is missing
            hostRevision: 0);

        var rejected = Assert.IsType<JoinResponse.Rejected>(response);
        Assert.Equal(JoinRejectionReason.ManifestMismatch, rejected.Reason);
    }

    [Fact]
    public void The_handshake_round_trips_over_a_connected_in_memory_transport_pair()
    {
        var hostPeer = new PeerId(1);
        var guestPeer = new PeerId(2);
        InMemoryTransport host = InMemoryTransport.Create(hostPeer);
        InMemoryTransport guest = InMemoryTransport.Create(guestPeer);

        JoinResponse? responseSeenByGuest = null;
        host.Received += (from, payload) =>
        {
            JoinRequest request = JoinRequest.FromCanonicalBytes(payload.ToArray());
            JoinResponse response = JoinHandshake.Evaluate(
                request,
                hostProtocolVersion: ProtocolVersion.Current,
                hostCampaignId: CampaignId,
                hostManifests: new HashSet<PackId> { ManifestA, ManifestB },
                hostRevision: 100);
            host.Send(from, response.ToCanonicalBytes());
        };
        guest.Received += (_, payload) => responseSeenByGuest = JoinResponse.FromCanonicalBytes(payload.ToArray());

        host.ConnectTo(guest);
        guest.Send(hostPeer, ValidRequest().ToCanonicalBytes());

        var accepted = Assert.IsType<JoinResponse.Accepted>(responseSeenByGuest);
        Assert.Equal(100UL, accepted.HostRevision);
    }
}
