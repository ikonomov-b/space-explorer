# 0036. Four frames; the connector kind decides the transform type; a units row per frame

Date: 2026-09-07. Status: Accepted. Supersedes the single position type and coordinate-frame row of [decision 0010](0010-units-coordinates-and-region-bounds.md) as the only ones; its region-local constants stand. Source: review recommendation taken on the owner's instruction of 2026-09-07 to follow the recommended path.

## Context

[Decision 0031](0031-primitive-complete-composition-and-storage.md) gives every node a transform and every connector an attachment transform, and the only position type fixed so far is int32 fixed-point at 1/256 m, whose range is ±8,388,608 m. That holds a region and, barely, an Earth-sized planet-fixed frame; it cannot place a planet in a system where 1 AU is 1.496 × 10^11 m, nor a point on a body larger than 8,389 km in radius. The units table deferred "planetary values" to numeric tables. Had the first `PrimitiveDefinition` record frozen one transform type, the system and planet domains would have inherited a type that cannot hold their values ([review finding 37](../review.md#37-one-transform-type-cannot-span-the-frame-hierarchy)). Nothing has been generated, so these constants are frozen now with the first content; changing any of them later is a generator or format version increment, as the units table already says of its rows.

## Decision

**Scalar types.** An *angle* is a signed 32-bit *binary turn*: the value divided by 2^32 is the angle in turns, so the range is ±half a turn and the resolution is 2^-32 turn, about 1.46 × 10^-9 rad, which at a 6,371 km radius is 9 mm of arc. A *rotation* is an ordered triple of binary turns applied intrinsically as yaw about +Y, then pitch about the rotated +X, then roll about the rotated +Z; it is stored, never integrated. *Simulation time* is a signed 64-bit count of 20 Hz ticks ([decision 0010](0010-units-coordinates-and-region-bounds.md)) since the world's epoch. Physical quantities such as mass, luminosity, pressure, and temperature take the width, unit, and scale declared per parameter in the category registry ([decision 0035](0035-category-registry-record-and-generator-revision-identifiers.md)); no single scale serves them all.

**Four frames**, each right-handed:

| Frame | Orientation | Stored positions |
| --- | --- | --- |
| System inertial | Origin at the system root, a star or barycentre; +Z along the root's spin axis at the epoch; +X toward the root's prime meridian at the epoch, fixed thereafter | None. Body positions are derived from orbital elements at the requested time, never stored. Distances are unsigned 64-bit metres. |
| Planet-fixed | Rotates with the body; +Z along its spin axis; +X through its prime meridian | Latitude and longitude as binary turns; radial lengths, body reference radius and height above it, as signed 64-bit fixed-point at 1/256 m. |
| Region-local | Tangent frame at the region's surface anchor: +X east, +Y up along the local vertical, +Z south, origin at the region centre | Signed 32-bit fixed-point at 1/256 m per axis, range ±8,388,608 m, as decision 0010 fixed. |
| Artifact-local | The artifact root's own frame | Signed 32-bit fixed-point at 1/65,536 m per axis, range ±32,768 m, resolution about 15 µm. |

**The connector kind decides the transform type.** An *orbit connector* attaches a body to its parent body or barycentre with orbital elements: semi-major axis as unsigned 64-bit metres, eccentricity as an unsigned 32-bit fraction of 2^32, inclination, longitude of the ascending node, argument of periapsis, and mean anomaly at the epoch as binary turns, and the epoch as simulation time. A body carries its spin as two binary turns for the pole's direction in the system frame, an unsigned 64-bit sidereal period in ticks, and a binary-turn prime-meridian phase at the epoch. A *surface anchor connector* attaches a region to a planet with latitude, longitude, height above the reference radius, and a heading as a binary turn clockwise from north, which defines the region-local frame. Every other connector attaches a child with a *rigid transform*: a translation in the containing frame's position type and a rotation triple. A connector kind admits exactly one of these three types, declared in the category registry, so no node carries a transform its frame cannot hold.

## Consequences

Easier: the system and planet domains have types that hold their values; the category registry has one place to declare which transform a connector carries; the region-local and artifact-local frames keep floats small in the Godot adapter, as decision 0010 intended. Harder: three transform types instead of one in the definition record and its reader; the tangent-plane projection of the surface anchor and the minimum body radius it implies are fixed separately ([review finding 38](../review.md#38-planet-level-fields-have-no-spherical-form-and-the-flat-region-bounds-the-explorable-body-radius)). Verified by the Determinism and Surface-sky consistency checks in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Technical design: units, coordinates, and bounds](../technical-design.md#units-coordinates-and-bounds), [astronomical state](../technical-design.md#astronomical-state-and-surface-sky), and [primitive registry and composition](../technical-design.md#primitive-registry-and-composition)
- [Glossary: worlds and destinations](../glossary.md#worlds-and-destinations)
- [Review finding 37](../review.md#37-one-transform-type-cannot-span-the-frame-hierarchy)
- [Progress: design documentation](../progress.md#design-documentation)
