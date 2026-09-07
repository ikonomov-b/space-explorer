namespace SpaceExplorer.Core.Networking;

/// <summary>
/// The core-owned byte transport: send, receive, peer-connected, and peer-disconnected, and nothing
/// else (decision 0003, decision 0028). Every member is peer-addressed, so a transport that later
/// serves more than one simultaneous peer needs no interface change. <see cref="InMemoryTransport"/> is
/// the M0a implementation; a Godot adapter over an ENet peer follows at M1/M2.
/// </summary>
public interface ITransport
{
    /// <summary>Sends a payload to a connected peer.</summary>
    /// <exception cref="InvalidOperationException"><paramref name="to"/> is not a currently connected peer.</exception>
    void Send(PeerId to, ReadOnlyMemory<byte> payload);

    /// <summary>Raised with the sender's identity and payload when a connected peer sends bytes.</summary>
    event Action<PeerId, ReadOnlyMemory<byte>>? Received;

    /// <summary>Raised with the peer's identity when a connection is established.</summary>
    event Action<PeerId>? PeerConnected;

    /// <summary>Raised with the peer's identity when a connection ends.</summary>
    event Action<PeerId>? PeerDisconnected;
}
