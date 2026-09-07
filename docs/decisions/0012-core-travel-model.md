# 0012. Core travel model: hub and spoke, two tiers with a snapping distance control, one guest

Date: 2026-09-07. Status: Accepted.

## Context

The plan said "two unlockable distance tiers" while meaning one unlock, the concept said "friends" while the core has one guest, and "travel distance is the main control" implied a continuous value while the plan has two discrete tiers. The sentence "repeated short hops cannot bypass access tiers" implied destination-to-destination travel, which the concept's home, destination, home loop does not include. Resolves [review findings 11 and 12](../review.md#11-concept-wording-that-will-become-requirements).

## Decision

Travel in the core release is hub and spoke. Every expedition departs from and returns to the home anchor; the ship is always at home or at exactly one destination, and a guest joins the host at whichever of those the host occupies. Distance and tier are measured from the anchor. The core has two distance tiers, the second unlocked with credits. The distance control on the ship dashboard snaps to those tiers; continuous distance within a tier is deferred to M3, as is chained travel between discovered systems. The core supports one guest; the concept says "a friend" for the core and larger groups are an expansion.

## Consequences

Easier: the save records one ship location, guest joining has two cases, and tier access needs no path accounting. Harder: the concept's "distance is the main control" is coarse in the core, so destination preferences carry more of the player's expressed intent until M3. Verified by the M1 solo slice in the [development plan](../development-plan.md#milestones).

## Applied to

- [Concept 1](../concept.md#1-the-general-idea)
- [Development plan: scope, core-release contents, milestones](../development-plan.md#scope-and-working-assumptions)
- [Technical design: multiplayer](../technical-design.md#multiplayer-and-trust-boundary)
