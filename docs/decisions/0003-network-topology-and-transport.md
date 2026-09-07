# 0003. Core-owned replication, loosely coupled hosts, and connectivity timing

Date: 2026-09-07. Status: Accepted.

## Context

The stack selected "Godot high-level multiplayer with ENet" as the transport while deferring the internet connectivity decision to "before M2". Every realistic connectivity option replaces the peer rather than sitting on it, and Godot's scene replication nodes would place authoritative state in scene nodes, which the architecture forbids. The M0b milestone also scheduled a mixed-OS host/guest demo before any connectivity path existed. Resolves [review finding 3](../review.md#3-transport-selected-before-connectivity-replication-coupling).

## Decision

**The core owns replication.** The core defines a byte transport with send, receive, peer-connected, and peer-disconnected as its whole surface. Commands, state deltas, and the join handshake are core code, serialized with the same versioned binary encoding as saves, and unit-tested over an in-memory transport. The Godot adapter implements the transport over an ENet peer using raw packets. Godot's scene replication nodes carry only cosmetic presence such as avatar transforms and animation state, never authoritative state.

**Topology.** The intended network is a loosely coupled set of hosts coordinated by a simple cloud-based server. Each campaign is hosted on its owner's machine, and all simulation and generation compute happens on the host where the exploration happens. The coordination server provides directory, invitation, presence, and relay or NAT-traversal assistance. It never holds campaign state and never computes worlds. Solo play works without it, and direct connection by address remains available when it is unreachable.

**Guest capacity.** The core release supports one guest. The session model, peer identity in commands, and per-peer avatars support additional guests from the start, up to a host-configured capacity limit, so adding guests is a capacity and testing question rather than a protocol change.

**Timing.** The connectivity approach, including account requirements, costs, and service-unavailable behavior, is decided at M1 start, once a generated system can be explored locally from a stored save. A self-hosted relay and coordination server is evaluated first because it keeps the ENet peer, needs no store account, costs one small server, and is reversible; store-provided networking is considered only if a store decision makes it free. The first mixed-OS host/guest connection runs at the start of M2 on the chosen path, in both host directions. The M0b networking demo is removed.

## Consequences

Easier: multiplayer logic is testable without Godot or a network; the transport can be swapped without touching commands or persistence; guest count scales without a protocol change. Harder: the coordination server is an operated service with availability and cost to document; multiplayer risk is discovered later than M0b, mitigated by the in-memory transport tests from M0a onward. Verified by the multiplayer checks in the [development plan](../development-plan.md#verification-and-performance-targets), which gain a coordination-server-unavailable case.

## Applied to

- [Technical design: multiplayer and trust boundary](../technical-design.md#multiplayer-and-trust-boundary)
- [Development plan: core-release contents, milestones, verification, decisions](../development-plan.md#milestones)
- [Technology decision: selected components, architecture, packaging policy, register entry 9](../technology-stack.md#selected-components)
- [Glossary](../glossary.md): host, guest, coordination server
