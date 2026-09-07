namespace SpaceExplorer.Core.Networking;

/// <summary>
/// A caller-assigned, opaque command identifier. <see cref="System.Guid.NewGuid"/> and
/// <see cref="System.Random"/> are rejected in Core at build time (decision 0008), so issuing one, for
/// instance from a per-peer sequence number, is a caller's responsibility, not this type's.
/// </summary>
public readonly record struct CommandId(ulong Value);
