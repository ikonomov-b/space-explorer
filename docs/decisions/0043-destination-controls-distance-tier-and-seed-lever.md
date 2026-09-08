# 0043. Destination controls: a tiered distance control labelled in light years and a seed lever with no visible value; preferences leave the core

Date: 2026-09-08. Status: Accepted. Supersedes the preference clause of [decision 0012](0012-core-travel-model.md). Source: the owner's note "Environment generation and randomization" of 2026-09-08 and the owner's answers in the clarification session of the same day.

## Context

The owner's note proposed that the player configures each trip with two levers: a trip distance of 1 to 100 light years, glossed as roughly five minutes to a few hours of generation, and a "randomization salt" lever whose movement yields a very large integer, or a set of integers, unlikely to be used twice. Against the documents: [decision 0012](0012-core-travel-model.md) fixes two snapping distance tiers for the core and defers continuous distance to M3; [decision 0004](0004-preparation-is-real-generation-time.md) fixes preparation as real generation time, with core tiers computing in seconds to minutes and a 60-second starter target; "salt" already names the campaign branch mixed into every world by [decision 0007](0007-artifact-collision-substitution-and-campaign-salt.md); the seed is one unsigned 64-bit field of the destination specification under [decision 0021](0021-sha256-stream-derivation.md); the development plan's navigation row promised recorded seeds and seed re-entry; and [concept 2](../concept.md#2-destination-programming) and the specification carried environment preferences and survey focus, which decision 0012's consequences relied on because tiered distance is coarse. Resolves items 1 to 5 of [review finding 42](../review.md#42-the-generation-note-diverges-from-the-concept-plan-and-accepted-decisions).

## Decision

**Distance.** Decision 0012 stands: two tiers, a control that snaps to them, continuous distance at M3. Each tier displays a light-year value as its fiction label, an entry of the numeric tables with no effect on generation. Decision 0004 stands unchanged: a longer journey is a more complex destination that takes the real time its generation needs; the note's minute and hour figures are estimates, not targets, and the 60-second starter target remains.

**Seed lever.** The second control fills the specification's seed with a fresh value. Its internal name is the seed; the dashboard may name it anything in the fiction, but the documents never call it a salt, which remains the campaign branch of decision 0007. The player sees the lever's state only as an abstract cue, such as a glow level, never as a number, and cannot type a seed. The value serves randomization uniqueness only: it is recorded in the world manifest as it always was, but it has no player-side meaning, and a discovered place is reached again through its recorded destination ([concept 2](../concept.md#2-destination-programming)), never by re-entering a seed. Near-uniqueness is a convenience, not a rule: the same seed in the same campaign selects the existing world and another campaign derives a different world from it (decision 0007), so no uniqueness guarantee is needed and none is made. The field stays one unsigned 64-bit value; "a set of integers" is not adopted.

**Preferences.** Environment preferences and survey focus leave the core release. They are not dashboard controls and not fields of the destination specification, which is now the campaign branch ID, the seed, the distance tier, the fixed content budget, and the pinned versions. They join continuous distance and chained travel as M3 options.

## Consequences

Easier: the dashboard is two controls and a return-visit list; the specification is smaller; no seed-entry interface, seed display, or seed-uniqueness rule exists; the plan's "seed re-entry creates no extra value" clause becomes "re-rolling the seed lever creates no extra value", which the substitution rule of decision 0007 already guarantees. Harder: with distance coarse and preferences gone, the tier is the player's only expressed intent, so the variety the player meets within a tier must come from the generator alone and the Destination validity and Artifact variety checks must show it. Verified by the M1 solo slice and the Gameplay/economy check in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Concept 2](../concept.md#2-destination-programming)
- [Requirements: R13](../requirements.md#design-constraints-stated-by-the-owner)
- [Development plan: scope, navigation row, gameplay/economy check](../development-plan.md#scope-and-working-assumptions)
- [Technical design: destination identity and determinism](../technical-design.md#destination-identity-and-determinism)
- [Glossary: destination specification, distance tier, seed lever](../glossary.md#worlds-and-destinations)
- [Review finding 42](../review.md#42-the-generation-note-diverges-from-the-concept-plan-and-accepted-decisions)
