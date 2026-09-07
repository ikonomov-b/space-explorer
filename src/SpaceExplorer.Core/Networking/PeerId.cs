namespace SpaceExplorer.Core.Networking;

/// <summary>
/// A transport-local peer identifier. Nothing here issues one; <see cref="ITransport"/> callers assign
/// them when they create a connection (decision 0028).
/// </summary>
public readonly record struct PeerId(uint Value)
{
    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
