# 0044. Tiered system composition: one star, a single lifeless explorable planet at the starter tier, explorability as a validation outcome, and per-tier planet and region counts

Date: 2026-09-08. Status: Accepted. Refines the planet and life rule of [concept 3](../concept.md#3-the-destination-planetary-system) by tier. Source: the owner's note "Environment generation and randomization" of 2026-09-08 and the owner's answers in the clarification session of the same day.

## Context

The owner's note described a destination as one sun of any known star type, at least one explorable planet, exactly one at the shortest trip, and 1 to 30 non-explorable planets, more with distance, which are non-explorable because they are made of gas, fully covered by liquid, or extremely volcanic; it asked that "the principles of the known types of solar systems" be followed and did not mention life. Against the documents: [concept 3](../concept.md#3-the-destination-planetary-system) and the plan's Worlds row require at least two explorable planets and one life-bearing planet in every destination, which the Destination validity check enforces; the plan names one stellar template family; [decision 0037](0037-derivation-rules-integer-periods-and-orbit-hierarchy.md) admits binary stars through a barycentre root; registry revision 1 ([decision 0042](0042-category-registry-revision-1-first-content-records-generator-and-publish-protocol.md)) has spectral classes O to M, a star orbit connector of 0 to 16 children, a planet orbit connector of 0 to 8 moons, and `ocean` as a body type; the generation lifecycle decides explorability by validating the suit profile numerically; and [decision 0041](0041-planet-fields-tangent-regions-minimum-radius-and-far-field.md) forbids landing below a reference radius of 524,288 m. Resolves items 6 to 10 and 15 of [review finding 42](../review.md#42-the-generation-note-diverges-from-the-concept-plan-and-accepted-decisions).

## Decision

**One star.** Every core-release system has exactly one star at its root. The `barycentre` category stays in the registry; binary systems are an expansion.

**Tiers.** The starter tier's destination has exactly one explorable planet and no life: the grammar admits no life primitive anywhere in a starter-tier system, so life is absent there rather than merely unvalidated. Every higher tier keeps the original rule of concept 3: at least two explorable planets and at least one life-bearing planet, which may overlap. The starter tier may draw the star from any class O to M; a tier that requires life draws it from F, G, K, or M.

**Explorability is a validation outcome.** A planet is explorable when at least one region on it passes step 4 of the [generation lifecycle](../technical-design.md#generation-lifecycle), the suit profile's numeric limits on gravity, pressure, temperature, radiation dose, chemical hazard, and mission duration, on a body whose reference radius is at least 524,288 m (decision 0041). The body type `rocky`, `icy`, `ocean`, or `gas-giant` shifts the odds through template ranges and never decides: a gas giant fails for want of a surface, while an ocean body with land or a volcanic body with a calm region can pass.

**The principles are physical consistency.** The derivation rules of decision 0037 are what "the principles of the known types of solar systems" means here. No vocabulary of system architecture classes is added; the core keeps one stellar template family whose parameters vary.

**Planet count.** Each tier states a minimum and maximum count of non-explorable planets in the numeric tables, rising with the tier. The star orbit connector's cardinality of 16 in registry revision 1 caps every planet of a system until a measured reason raises it in a registry revision; the note's 30 is not adopted now. Moons are separate, 0 to 8 per planet, and are not counted as planets.

**Regions.** The starter tier's explorable planet has one landing region. At higher tiers each explorable planet has a small count of regions stated per tier in the numeric tables, placed by the generator where the planet fields of decision 0041 and the suit profile allow; the orbital/region map shows the regions a world has, and choosing among more is not a core feature.

## Consequences

Easier: a starter destination is one region on one planet with no life, which is the smallest world the pipeline can make and the one the 60-second target of [decision 0004](0004-preparation-is-real-generation-time.md) is measured on; the Destination validity check gets an explicit rule per tier; the sky and the far field are unchanged. Harder: the biological artifact family, biome vegetation, and every life primitive appear only from the second tier, so demonstrating three families needs the second tier unlocked, which M1 includes; the grammar needs a tier gate on life and on the star class; the validity seed suite runs per tier with different rules; the planet count and region count per tier are two more numeric-table entries to tune. Verified by the Destination validity check, at least 1,000 seeds per tier, in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Concept 3](../concept.md#3-the-destination-planetary-system)
- [Requirements: R14](../requirements.md#design-constraints-stated-by-the-owner)
- [Development plan: worlds row, destination validity, numeric content](../development-plan.md#core-release-contents)
- [Technical design: generation lifecycle](../technical-design.md#generation-lifecycle)
- [Glossary: distance tier, explorable planet](../glossary.md#worlds-and-destinations)
- [Review finding 42](../review.md#42-the-generation-note-diverges-from-the-concept-plan-and-accepted-decisions)
