# 0038. Derived instances: recipe-produced, path-addressed, overlay-targetable, counted apart from nodes

Date: 2026-09-07. Status: Accepted. Supersedes the expected-scale clause of [decision 0006](0006-pack-identity-and-allocation.md). Source: review recommendation taken on the owner's instruction of 2026-09-07 to follow the recommended path.

## Context

[Decision 0006](0006-pack-identity-and-allocation.md) expected "thousands of composition nodes per world". A maximal region is 2,048 m × 2,048 m, 4,194,304 m²; one rock or plant per 100 m² is 41,943 items in one region and one per 10 m² is 419,430. Either the budget was off by two or three orders of magnitude, or a scatter recipe must yield instances that are addressable by path but not stored as nodes, and the documents said neither. Overlays are keyed by instance path and load-time verification requires that "each overlay targets a compatible primitive instance", so collecting one rock from a scatter had no path to record ([review finding 41](../review.md#41-the-node-budget-assumes-no-derived-instances)).

## Decision

**A category may derive instances.** The category registry ([decision 0035](0035-category-registry-record-and-generator-revision-identifiers.md)) declares, per category, whether its recipe derives instances and the maximum count one node may derive. A derived instance is produced by the node's generator revision identifier from the node's parameters and the node's random stream alone, in a deterministic order, and is addressed by extending the node's path with `item/<index>`, as in `system/planet/2/surface/region/7/scatter/41/item/1234`. Derived instances are not stored as nodes, carry no allocated ID, and count against the node's derived budget rather than the graph's node limit. A derived instance may itself carry parameters and a rigid transform in the containing frame but never children or connectors; anything that needs those is an explicit node.

**Storage follows the category's policy** ([decision 0034](0034-storage-policy-pinned-by-manifests.md)): under `regenerate` the derived set exists only when evaluated; under `materialize` or `hybrid` the accepted derived set is the node's primitive-owned payload, a bounded ordered array whose length is checked against the declared maximum before allocation.

**Overlays may target derived paths.** A primitive-state overlay record may name a derived instance path. At load, the integrity check resolves it by evaluating the parent node under its pinned generator revision identifier and the world's pinned policy, or by reading the materialized payload, and refuses a path the parent does not derive. Collection, damage, and depletion of scattered items are therefore ordinary overlay records.

**Expected scale, restated.** Hundreds of definitions per set, thousands of explicit nodes per world, and derived instances bounded per node by the registry and measured per region at M0b. The two counts are reported separately in every resource measurement.

## Consequences

Easier: a forest, a rock field, or a debris scatter is one node with one stream and one payload, and a player can still pick up one of its items; the node limit stays meaningful; the M0b measurement gets a second axis rather than an impossible first one. Harder: two instance kinds in the path grammar and the overlay verifier, and a derived budget that the registry must declare for every deriving category. Verified by the Resource-use and Durability checks in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Technical design: primitive sets and compact references](../technical-design.md#primitive-sets-and-compact-references), [persistence](../technical-design.md#persistence-and-compatibility), and [save integrity](../technical-design.md#save-integrity)
- [Development plan: verification](../development-plan.md#verification-and-performance-targets)
- [Glossary: content identity](../glossary.md#content-identity)
- [Review finding 41](../review.md#41-the-node-budget-assumes-no-derived-instances)
- [Progress: design documentation](../progress.md#design-documentation)
