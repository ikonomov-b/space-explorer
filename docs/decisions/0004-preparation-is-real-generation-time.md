# 0004. Preparation time is real generation time; effort score is tier base plus recipe complexity

Date: 2026-09-07. Status: Accepted.

## Context

The original concept tied both artifact value and world richness to the time spent generating a destination. The technical design replaced wall-clock pricing with fixed reference effort and fixed the content budget per tier, which left the role of long preparation undefined: the concept kept days-long journeys alive while nothing in the design gave them a function. Resolves [review finding 4](../review.md#4-long-journeys-lost-their-rationale).

## Decision

**Preparation time is the real time the host's machine needs to generate the destination.** There is no artificial timer. Higher tiers carry larger content budgets and therefore compute longer; a slower computer waits longer for the same destination and receives the same world and the same values. Generation jobs perform only useful work and are never padded to lengthen a wait. A tier that generates too quickly to feel like a journey receives a larger content budget, not a delay.

**Scale.** Core-release tiers compute in seconds to minutes, and the 60-second starter-destination target stands. Days-long journeys arise only from very large content budgets and are the M3 long-mode experiment, which keeps the requirements for resumable work, bounded resources, progress visibility, continued access to prepared destinations, and an opt-in background worker. The core lifecycle stays resumable with checkpoints sized for minute-scale jobs.

**Effort score.** Generation effort in the value formula is an authored per-tier base score plus a recipe complexity score computed from the composition graph, using node count, depth, and rare-connector use. Recovery effort remains the expected active effort from the site's route and challenge. Both are stored with the numeric tables under a valuation version. Wall-clock time is never an input to value.

## Consequences

Easier: the wait needs no separate design, and the pipeline's pause and resume machinery serves crash safety and long mode alike. Harder: the felt length of a journey depends on hardware, so the reference PC and content budgets must be tuned together, and "useful playable content per second of generation" becomes a tracked measurement. Literal elapsed-time pricing is rejected. Verified by the responsiveness and resource-use checks and by the M3 long-mode experiment in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Concept 4](../concept.md#4-travel-and-the-randomness-of-the-destination)
- [Development plan: scope, milestones, decisions](../development-plan.md#scope-and-working-assumptions)
- [Technical design: generation lifecycle and valuation](../technical-design.md#generation-lifecycle)
- [Assessment: decisions and validation still needed](../assessment.md#decisions-and-validation-still-needed)
