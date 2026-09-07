using SpaceExplorer.Core.Networking;
using Xunit;

namespace SpaceExplorer.Core.Tests.Networking;

public class InMemoryTransportTests
{
    private static readonly PeerId Host = new(1);
    private static readonly PeerId Guest = new(2);

    [Fact]
    public void Connecting_raises_PeerConnected_on_both_sides_with_the_other_sides_identity()
    {
        InMemoryTransport host = InMemoryTransport.Create(Host);
        InMemoryTransport guest = InMemoryTransport.Create(Guest);

        // Subscribed before connecting, which is the ordering ConnectTo's contract depends on.
        PeerId? hostSawConnect = null;
        PeerId? guestSawConnect = null;
        host.PeerConnected += peer => hostSawConnect = peer;
        guest.PeerConnected += peer => guestSawConnect = peer;

        host.ConnectTo(guest);

        Assert.Equal(Guest, hostSawConnect);
        Assert.Equal(Host, guestSawConnect);
        Assert.Equal(Guest, host.RemoteId);
        Assert.Equal(Host, guest.RemoteId);
    }

    [Fact]
    public void Connecting_twice_fails_explicitly()
    {
        InMemoryTransport host = InMemoryTransport.Create(Host);
        InMemoryTransport guest = InMemoryTransport.Create(Guest);
        host.ConnectTo(guest);

        InMemoryTransport other = InMemoryTransport.Create(new PeerId(3));
        Assert.Throws<InvalidOperationException>(() => host.ConnectTo(other));
    }

    [Fact]
    public void Send_delivers_the_exact_bytes_to_Received_with_the_senders_identity()
    {
        InMemoryTransport host = InMemoryTransport.Create(Host);
        InMemoryTransport guest = InMemoryTransport.Create(Guest);
        host.ConnectTo(guest);

        PeerId? sender = null;
        byte[]? received = null;
        guest.Received += (from, payload) =>
        {
            sender = from;
            received = payload.ToArray();
        };

        byte[] sent = [1, 2, 3, 4];
        host.Send(Guest, sent);

        Assert.Equal(Host, sender);
        Assert.Equal(sent, received);
    }

    [Fact]
    public void Send_before_connecting_fails_explicitly()
    {
        InMemoryTransport host = InMemoryTransport.Create(Host);
        Assert.Throws<InvalidOperationException>(() => host.Send(Guest, ReadOnlyMemory<byte>.Empty));
    }

    [Fact]
    public void Send_to_a_peer_other_than_the_connected_one_fails_explicitly()
    {
        InMemoryTransport host = InMemoryTransport.Create(Host);
        InMemoryTransport guest = InMemoryTransport.Create(Guest);
        host.ConnectTo(guest);

        Assert.Throws<InvalidOperationException>(() => host.Send(new PeerId(99), ReadOnlyMemory<byte>.Empty));
    }

    [Fact]
    public void Disconnect_raises_PeerDisconnected_on_both_sides_and_blocks_further_sends()
    {
        InMemoryTransport host = InMemoryTransport.Create(Host);
        InMemoryTransport guest = InMemoryTransport.Create(Guest);
        host.ConnectTo(guest);

        PeerId? hostSawDisconnect = null;
        PeerId? guestSawDisconnect = null;
        host.PeerDisconnected += peer => hostSawDisconnect = peer;
        guest.PeerDisconnected += peer => guestSawDisconnect = peer;

        host.Disconnect();

        Assert.Equal(Guest, hostSawDisconnect);
        Assert.Equal(Host, guestSawDisconnect);
        Assert.Null(host.RemoteId);
        Assert.Null(guest.RemoteId);
        Assert.Throws<InvalidOperationException>(() => host.Send(Guest, ReadOnlyMemory<byte>.Empty));
        Assert.Throws<InvalidOperationException>(() => guest.Send(Host, ReadOnlyMemory<byte>.Empty));
    }

    [Fact]
    public void Disconnecting_an_unconnected_transport_is_a_no_op()
    {
        InMemoryTransport host = InMemoryTransport.Create(Host);
        host.Disconnect();
        Assert.Null(host.RemoteId);
    }
}
