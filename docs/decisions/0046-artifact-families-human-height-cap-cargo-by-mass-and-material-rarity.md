# 0046. Artifacts: three families with relics covering technical pieces and art, a human-height cap, cargo by mass and size class, and material rarity through the rarity factor

Date: 2026-09-08. Status: Accepted. Source: the owner's note "Environment generation and randomization" of 2026-09-08 and the owner's answers in the clarification session of the same day.

## Context

The owner's note sized artifacts "from a bean to up to a human height" and listed four kinds: valuable materials such as diamonds and rare metals, life-form fossils, technical relics, and art made by an intelligent form. Against the documents: the [development plan](../development-plan.md#core-release-contents) and registry revision 1 ([decision 0042](0042-category-registry-revision-1-first-content-records-generator-and-publish-protocol.md)) have three families, `mineral`, `biological`, and `relic`; [concept 3](../concept.md#3-the-destination-planetary-system) says life may be simple organisms rather than a civilization; a geometry part spans 1 cm to 4 m and an artifact attaches up to 8 parts, so an assembled artifact could stand far taller than a person; the plan's cargo limit forces choices and P0 uses two slots; and under [decision 0004](0004-preparation-is-real-generation-time.md) value is effort times a bounded rarity factor with no material term. Resolves items 13 and 14 of [review finding 42](../review.md#42-the-generation-note-diverges-from-the-concept-plan-and-accepted-decisions).

## Decision

**Three families.** `mineral`, `biological`, and `relic` stand. The relic family covers both technical pieces and works of art; their makers are a past presence the player never encounters, so concept 3's statement about simple life holds. Fossils are one kind of biological specimen; preserved and living specimens remain admissible. No registry change.

**Size.** An assembled artifact's bounding box is at most 2 m in every axis, a composition-grammar constraint checked at generation; the lower bound is the 1 cm minimum of the geometry-part schema. The part range of registry revision 1 is unchanged.

**Mass, size class, and cargo.** An artifact's mass is the `sum` aggregation rule over its parts' masses and its size class is a bucket of its bounds fixed in the numeric tables. Cargo capacity is a mass budget and a volume budget checked by the authority, not a fixed slot count; P0 keeps its two slots because P0 is disposable. Recovery challenges may weigh a large artifact's route, which valuation already scores as recovery effort.

**Material and value.** Each mineral material carries a rarity weight in the numeric tables that feeds the bounded rarity factor of decision 0004. The formula is unchanged and effort still dominates: a diamond is rarer, not differently priced.

## Consequences

Easier: no registry revision; the fiction stays coherent with concept 3; valuation is untouched. Harder: the grammar gains a bound check and a size-class derivation; cargo becomes two budgets that the command authority checks and the interface shows; the numeric tables gain size classes and rarity weights; the Gameplay/economy check observes whether mass and size make cargo choices legible. Verified by the Artifact variety and Gameplay/economy checks in the [development plan](../development-plan.md#verification-and-performance-targets) and by the M1 solo slice.

## Applied to

- [Concept 6](../concept.md#6-the-artifacts)
- [Development plan: exploration row and artifact families](../development-plan.md#core-release-contents)
- [Technical design: composition validation and valuation](../technical-design.md#composition-validation)
- [Glossary: artifact (instance)](../glossary.md#campaign-and-play)
- [Review finding 42](../review.md#42-the-generation-note-diverges-from-the-concept-plan-and-accepted-decisions)
