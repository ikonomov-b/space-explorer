# 0013. The host simulates hazards for every avatar

Date: 2026-09-07. Status: Accepted.

## Context

The multiplayer contract did not say whether the host or the guest computes a guest's suit depletion and emergency recovery. Guest computation would let impossible states reach the host save; host computation needs guest positions at tick rate and shapes the message design. Resolves [review finding 13](../review.md#13-guest-hazard-authority).

## Decision

The host simulates suit reserves, hazard exposure, and emergency recovery for every avatar, including its own, from client-reported positions and inputs at the fixed 20 Hz tick of [decision 0010](0010-units-coordinates-and-region-bounds.md). Clients predict locally for display only and never report reserve values. The host is authoritative on reserve exhaustion, emergency recovery, and the field cache, and applies them through the same command validation used in solo play.

## Consequences

Easier: one hazard implementation, and the private-trust model of the design is preserved because a guest can only report where it is, not what happened to it. Harder: position and input messages run at tick rate, which the bounded message sizes already allow. Verified by the multiplayer checks in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Technical design: multiplayer and trust boundary](../technical-design.md#multiplayer-and-trust-boundary)
- [Development plan: verification](../development-plan.md#verification-and-performance-targets)
