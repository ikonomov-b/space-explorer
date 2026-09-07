namespace SpaceExplorer.Core.Networking;

/// <summary>
/// The M0a <see cref="ITransport"/> implementation: a 1:1 in-process pair, matching "the core release
/// supports one guest" (decision 0003). Build a side with <see cref="Create"/>, subscribe to its events,
/// then join two sides with <see cref="ConnectTo"/>; connecting is a separate step from creation so a
/// caller can never miss <see cref="ITransport.PeerConnected"/> by subscribing too late (decision 0028).
/// </summary>
public sealed class InMemoryTransport : ITransport
{
    private InMemoryTransport? _peer;

    private InMemoryTransport(PeerId localId) => LocalId = localId;

    /// <summary>This side's identifier.</summary>
    public PeerId LocalId { get; }

    /// <summary>The connected peer's identifier, or null when not connected.</summary>
    public PeerId? RemoteId => _peer?.LocalId;

    /// <inheritdoc/>
    public event Action<PeerId, ReadOnlyMemory<byte>>? Received;

    /// <inheritdoc/>
    public event Action<PeerId>? PeerConnected;

    /// <inheritdoc/>
    public event Action<PeerId>? PeerDisconnected;

    /// <summary>Creates one unconnected side of a future pair.</summary>
    public static InMemoryTransport Create(PeerId localId) => new(localId);

    /// <summary>
    /// Connects this side to <paramref name="other"/>, raising <see cref="PeerConnected"/> on both sides.
    /// </summary>
    /// <exception cref="InvalidOperationException">Either side is already connected.</exception>
    public void ConnectTo(InMemoryTransport other)
    {
        if (_peer is not null || other._peer is not null)
        {
            throw new InvalidOperationException("A side of an in-memory transport pair may connect once.");
        }

        _peer = other;
        other._peer = this;

        PeerConnected?.Invoke(other.LocalId);
        other.PeerConnected?.Invoke(LocalId);
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Not connected, or <paramref name="to"/> is not the connected peer.</exception>
    public void Send(PeerId to, ReadOnlyMemory<byte> payload)
    {
        if (_peer is null)
        {
            throw new InvalidOperationException($"{LocalId} is not connected.");
        }

        if (to != _peer.LocalId)
        {
            throw new InvalidOperationException($"{LocalId} is connected to {_peer.LocalId}, not {to}.");
        }

        _peer.Received?.Invoke(LocalId, payload);
    }

    /// <summary>Ends the connection, raising <see cref="PeerDisconnected"/> on both sides. A no-op if not connected.</summary>
    public void Disconnect()
    {
        if (_peer is null)
        {
            return;
        }

        InMemoryTransport other = _peer;
        _peer = null;
        other._peer = null;

        PeerDisconnected?.Invoke(other.LocalId);
        other.PeerDisconnected?.Invoke(LocalId);
    }
}
