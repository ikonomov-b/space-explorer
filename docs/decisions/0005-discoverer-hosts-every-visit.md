# 0005. A discovered system is reachable only through its discoverer

Date: 2026-09-07. Status: Accepted. Source: project owner statement, 2026-09-07.

## Context

The concept said a saved destination "can be shared with an invited player over the internet" without stating whether the world itself travels or only the visit. The owner's story rule is that discovered solar systems are accessible solely through the person who discovered them, who can invite guests to travel there together and explore side by side.

## Decision

A discovered system exists in its discoverer's campaign and is visited only in a session hosted by that discoverer. Guests travel with the host to the system and explore together; they cannot reach it on their own, and a world is never transferred into another campaign. Export and migration serve the owner's backup and change of machine; they do not make a discovered system playable elsewhere. Shared discoveries, the shared collection, and guest progress live in the host's campaign, as the multiplayer contract already states.

## Consequences

Easier: ownership is unambiguous in fiction and in data, matching the host-authoritative model in [decision 0003](0003-network-topology-and-transport.md). Sharing a seed between players is no longer a design goal, so world generation may include campaign-specific salt if a later decision needs it; this reopens the analysis of [review finding 6](../review.md#6-uniqueness-by-rejection). Harder: a guest who wants to return to a system they explored must be invited again by its discoverer, which the game's fiction should present as travelling together rather than as a restriction.

## Applied to

- [Concept 5](../concept.md#5-remembering-destinations-and-exploring-together)
- [Technical design: persistence and multiplayer](../technical-design.md#multiplayer-and-trust-boundary)
- [Glossary](../glossary.md): world
