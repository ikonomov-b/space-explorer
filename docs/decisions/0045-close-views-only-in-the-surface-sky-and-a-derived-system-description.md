# 0045. Other bodies are seen only in the surface sky; a system description derived from the graph at M0b and M1

Date: 2026-09-08. Status: Accepted. Source: the owner's note "Environment generation and randomization" of 2026-09-08 and the owner's answers in the clarification session of the same day.

## Context

The owner's note asked that every non-explorable planet "have a realistic look when observed from a distance from which it can fill the screen", and that on arrival the traveller "see a text description" of the system, the type of the sun and the number and type of each planet, "used as a first test of the solar system randomization". Against the documents: the [development plan](../development-plan.md#scope-and-working-assumptions) excludes seamless space-to-ground flight and interactive orbital flight from the core, so the only views of a system are the surface sky of [decision 0032](0032-astronomically-consistent-surface-sky.md) and the orbital/region map; registry revision 1 has no surface material admitted to the planet domain; and no document mentions a text description. Resolves items 11 and 12 of [review finding 42](../review.md#42-the-generation-note-diverges-from-the-concept-plan-and-accepted-decisions).

## Decision

**Close views.** Non-explorable bodies are seen only in the surface sky, at the angular size, phase, and visibility the celestial solution of decision 0032 derives; a large moon or a near planet can loom while a distant one is a point. The core release adds no approach scene, telescope or scanner zoom, or orbital view. A body's disc appearance derives from its planet and atmosphere primitives, so the surface-material primitives the planet domain needs for that rendering are added at M0b, when the first world package needs them; they are not part of registry revision 1.

**System description.** A system description is text derived on demand from a world's accepted composition graph: the star's spectral class, then each planet in orbit order with its body type, whether it is explorable, and whether it bears life, and the counts of each. It is never stored, so it introduces no environment data outside the graph (requirement R10), and it names no artifact, site, or value, so the lifecycle's rule that finds are not revealed before readiness is untouched. At M0b the command-line tool prints it from a world package; at M1 the arrival screen shows the same text to the player. Its test role is twofold: the description of one fixed-seed world is a recorded vector compared across Linux and Windows, and the Destination validity check reviews a sample of descriptions from the seed suite for variety and plausibility, the way the content gate of [decision 0002](0002-primitive-set-content-gate.md) reviews rendered primitives.

## Consequences

Easier: no new presentation path for M1; a human-readable window into the generator exists before any world is rendered, so the owner can judge system variety from a terminal; the arrival screen is one string. Harder: planet-domain materials become M0b work; the description format is versioned text whose fixed-seed vector freezes it, so a change to the wording is a version change of the description, not of the world. Verified by the M0b exit criteria and the Destination validity and Surface-sky consistency checks in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Concept 3](../concept.md#3-the-destination-planetary-system)
- [Development plan: exploration row, milestones M0b and M1, destination validity](../development-plan.md#milestones)
- [Technical design: generation lifecycle and astronomical state](../technical-design.md#generation-lifecycle)
- [Glossary: system description](../glossary.md#worlds-and-destinations)
- [Review finding 42](../review.md#42-the-generation-note-diverges-from-the-concept-plan-and-accepted-decisions)
