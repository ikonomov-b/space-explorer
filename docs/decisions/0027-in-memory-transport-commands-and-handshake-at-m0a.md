# 0027. The in-memory transport, commands, and join handshake are M0a scope

Date: 2026-09-07. Status: Accepted.

## Context

[Decision 0003](0003-network-topology-and-transport.md) mitigates the risk of discovering multiplayer problems only at M2 with "the in-memory transport tests from M0a onward," and the development plan's own Multiplayer check requires running "the command and handshake suite over the in-memory transport from M0a onward" ([verification and performance targets](../development-plan.md#verification-and-performance-targets)). But every document that describes M0a's actual scope in detail covers primitive-set work only: the plan's M0a row, its fifteen-criterion decomposition in [progress.md](../progress.md#m0a-exit-criteria), [decision 0022](0022-p0-gates-the-first-frozen-content-format.md)'s list of M0a work that may proceed alongside P0 without freezing a content format, and the technology decision's own walkthrough of "M0a first generates a random, constraint-valid primitive set..." ([architecture and first implementation step](../technology-stack.md#architecture-and-first-implementation-step)). None schedules building the transport, commands, or handshake, and no such code exists in the repository. The mitigation decision 0003 names has nothing to schedule it. Resolves [review finding 29](../review.md#29-the-in-memory-transport-commands-and-handshake-are-untimed-for-m0a).

## Decision

M0a's exit criteria gain a sixteenth line: define the core-owned byte transport (send, receive, peer-connected, peer-disconnected), core commands with unique IDs and state-revision validation, and the join handshake, serialized with the same versioned binary encoding as saves ([multiplayer and trust boundary](../technical-design.md#multiplayer-and-trust-boundary)), and cover them with a test suite run over an in-memory transport in continuous integration. This freezes no content format, so it joins decision 0022's carve-out of work that may proceed alongside P0 without waiting on P0's exit record. The Godot/ENet adapter over this contract stays out of scope until M1 start (the connectivity approach) and M2 start (the first mixed-OS connection), per decision 0003; only the interface, the in-memory implementation, and its tests land at M0a.

## Consequences

Easier: the mitigation decision 0003 describes actually happens on the schedule it names, and command validation exists before campaign and valuation code needs it. Harder: M0a grows a sixteenth exit criterion and a test surface unrelated to content generation, in a milestone the plan already flags for time-boxing. Verified by the Multiplayer check and the M0a exit criteria in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Development plan: milestones, verification](../development-plan.md#milestones)
- [Progress: milestones, M0a exit criteria, verification](../progress.md#m0a-exit-criteria)
- [Technical design: multiplayer and trust boundary](../technical-design.md#multiplayer-and-trust-boundary)
- [Technology decision: architecture and first implementation step](../technology-stack.md#architecture-and-first-implementation-step)
- [Review finding 29](../review.md#29-the-in-memory-transport-commands-and-handshake-are-untimed-for-m0a)
