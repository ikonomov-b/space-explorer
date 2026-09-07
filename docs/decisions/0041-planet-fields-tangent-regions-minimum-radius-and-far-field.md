# 0041. Cube-sphere planet fields, tangent-plane regions, a minimum landable radius, and a derived far field

Date: 2026-09-07. Status: Accepted. Source: review recommendation taken on the owner's instruction of 2026-09-07 to follow the recommended path.

## Context

Bounded grids were rectangular, so global relief, climate, insolation by latitude, and the ocean datum that every region of one planet must agree on had no representation, and the "region's stable mapping to the planet-fixed frame" of [decision 0032](0032-astronomically-consistent-surface-sky.md) had no form ([review finding 38](../review.md#38-planet-level-fields-have-no-spherical-form-and-the-flat-region-bounds-the-explorable-body-radius)). A region is a flat patch of at most 2,048 m per axis ([decision 0017](0017-region-extent-cap-and-storage-derivation.md)), which bounds the bodies it can sit on, and nothing stated the bound. On an Earth-sized body the horizon at a 2 m eye height is 5.0 km away while the region ends 1,024 m from its centre, and no structure supplied the ground between, nor a rule for the region boundary, although requirement R12 asks for a coherent view ([review finding 39](../review.md#39-nothing-is-defined-beyond-the-region-edge)).

## Decision

**Cube-sphere grids for the planet domain.** The closed parameter schema gains a cube-sphere bounded grid: six square faces at a declared power-of-two resolution per face, a cell addressed by face, `u`, and `v`, values in the parameter's declared type, with the face layout and its orientation in the planet-fixed frame fixed by the category registry ([decision 0035](0035-category-registry-record-and-generator-revision-identifiers.md)). Global relief, climate, insolation, and the ocean datum are such grids, or recipes that produce them, owned by planet instances; regions sample them, which is parent-to-child data flow and consumes no other chunk's stream.

**The surface anchor is a tangent plane.** The anchor of [decision 0036](0036-frames-and-transforms-typed-by-connector-kind.md) places the region-local origin at its latitude, longitude, and height on the planet-fixed sphere. +Y is the outward local vertical; +X and +Z are east and south rotated about +Y by the heading so that −Z points along it. A region-local point maps to planet-fixed coordinates by that affine frame; the region is flat, not projected onto the sphere, and the observer's local horizon frame for the celestial solution is the region-local frame itself.

**Minimum landable radius, derived.** The half-diagonal of a maximal region is 1,024 × √2, about 1,448 m. With the assumed 2 m eye height the true horizon lies beyond every corner of a region seen from its centre when `R ≥ d² / 2h`, which is 524,288 m, or 2^19 m. At that radius the corner sits 2.0 m below the tangent plane and the gravity direction tilts 0.16° across the half-diagonal; both are accepted as the flat model's error. The stellar grammar admits a surface anchor only on bodies whose reference radius is at least 524,288 m. Smaller bodies exist in the graph, are propagated, and appear in the sky, but are not landable in the core release; curved regions are outside it.

**The far field is derived, like the sky.** The Godot adapter renders the ground between the region edge and the geometric horizon from the planet's relief grid sampled in the region's tangent frame, as an evictable cache. Inside the region the authoritative heights win and the far field begins at the edge. Its horizon is the geometric horizon of decision 0032's solution, so a body the solution hides is also occluded by the far field. The far field has no collision, no placements, and no state.

**The region edge is the traversal primitive's rule.** The region's traversal primitive declares its boundary. In the core release the boundary is impassable, enforced by the host at the tick ([decision 0013](0013-host-simulates-hazards.md)), and the adapter shows a boundary cue that does not rely on colour alone, as the usability row requires. The Surface-sky consistency check gains two cases: the far-field horizon agrees with the celestial horizon at the region edge, and the boundary cue is displayed.

## Consequences

Easier: every region of one planet shares one relief, one climate, and one sea level; the view to the horizon comes from pinned data rather than authored dressing; the generator cannot land a region where the flat model would contradict the sky. Harder: a sixth grid kind in the schema and its reader, a planet-level field the generator must produce before any region, and a hard lower bound on landable bodies that the stellar template family must respect. Verified by the Surface-sky consistency and Destination validity checks in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Technical design: primitive registry and composition](../technical-design.md#primitive-registry-and-composition), [astronomical state](../technical-design.md#astronomical-state-and-surface-sky), and [region encoding](../technical-design.md#region-encoding-and-compression)
- [Development plan: core-release contents](../development-plan.md#core-release-contents) and [verification](../development-plan.md#verification-and-performance-targets)
- [Glossary: worlds and destinations](../glossary.md#worlds-and-destinations)
- [Review findings 38](../review.md#38-planet-level-fields-have-no-spherical-form-and-the-flat-region-bounds-the-explorable-body-radius) and [39](../review.md#39-nothing-is-defined-beyond-the-region-edge)
- [Progress: milestones and design documentation](../progress.md#milestones)
