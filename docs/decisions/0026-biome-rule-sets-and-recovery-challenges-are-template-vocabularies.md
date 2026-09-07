# 0026. Biome rule sets and recovery challenges are scoped template vocabularies

Date: 2026-09-07. Status: Accepted.

## Context

The development plan's core-release contents table requires "at least three biome rule sets" and "two recovery challenges" with the same weight as artifact families and regions ([core-release contents](../development-plan.md#core-release-contents)). Artifact families are defined one paragraph later in the plan and backed by valuation and dealer-spread machinery; regions have a full contract in the glossary, the registry, and persistence. Neither biome rule set nor recovery challenge appears anywhere else in the documents: the primitive registry's `category` field has `biome element` (a generated primitive, not an authored rule set) and `site component`, but no template vocabulary, persistence record, or content-gate coverage is defined for either as a countable content type, and `content/README.md`'s description of authored content names neither. The line has read this way since the plan's first commit, through twenty-five decisions, so nothing built so far was designed with these two counts in mind. Resolves [review finding 28](../review.md#28-biome-rule-sets-and-recovery-challenges-are-undefined-content).

## Decision

A **biome rule set** is a template vocabulary ([glossary](../glossary.md)) scoped to the `biome element` category already in the primitive registry's `category` field: an authored set of parametric rules and constraints from which biome-element primitives are generated, giving a region's environmental profile a recognizably distinct character from every other rule set. The core release requires at least three.

A **recovery challenge** is a template vocabulary scoped to the `site component` category, constrained to the return-path site type and covering the "route and challenge" that [artifact valuation](../technical-design.md#artifact-valuation-and-transactions) already scores as recovery effort: an authored obstacle and route combination a player must actively navigate between an artifact site and the ship. The core release requires at least two, distinct in the hazard/route combination they demand, not only in cosmetic dressing.

Neither term introduces a new primitive category, persistence record, or pipeline stage. Both use the registry's existing `category`, `parameters`, `connectors`/`constraints`, and `provenance` fields, and both pass through the same generation, validation, and content-gate pipeline ([decision 0002](0002-primitive-set-content-gate.md)) as every other template vocabulary.

## Consequences

Easier: the Content row's counts are now implementable against the same machinery as everything else in that table, with no new format to design. Harder: `biome element` and `site component` templates now carry a minimum-distinct-count obligation the generic content gate does not itself check; M0a's test plan must add it. Verified by the artifact-variety check and the M0a content gate in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Glossary](../glossary.md): biome rule set, recovery challenge
- [Technical design: primitive registry and composition](../technical-design.md#primitive-registry-and-composition)
- [Development plan: core-release contents](../development-plan.md#core-release-contents)
- [Technology decision: architecture and first implementation step](../technology-stack.md#architecture-and-first-implementation-step)
- [Review finding 28](../review.md#28-biome-rule-sets-and-recovery-challenges-are-undefined-content)
