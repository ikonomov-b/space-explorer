# 0037. Derivation-rule primitives, integer periods with phase at epoch, and the two-body hierarchy

Date: 2026-09-07. Status: Accepted. Source: review recommendation taken on the owner's instruction of 2026-09-07 to follow the recommended path.

## Context

[Decision 0031](0031-primitive-complete-composition-and-storage.md) resolves composed values through explicit aggregation-rule primitives: sum, weighted average, minimum, maximum, bounded composition, and override. A realistic system also needs surface gravity from mass and radius, orbital period from semi-major axis and central mass, equilibrium temperature from luminosity, distance, and albedo, pressure against altitude, atmosphere retention against escape velocity, tidal locking, and orbit-spacing stability. These were neither parameters nor aggregation rules nor named generator revisions, so a generator would have stored independent values that can disagree or computed them in code with no identity. Authoritative floating point is forbidden, and a periodic quantity expressed as an angular rate drifts over years of simulation time at any fixed-point resolution ([review finding 40](../review.md#40-physical-derivation-rules-are-missing)). [Decision 0035](0035-category-registry-record-and-generator-revision-identifiers.md) supplies the identifier form these rules need.

## Decision

**Derivation rules are primitives.** A derivation rule is a primitive of the `derivation rule` category, named by a generator revision identifier of the form `derive-<name>/<k>`, with typed inputs that are parameter references by instance path, one typed output, an integer or fixed-point implementation, and recorded reference vectors that the suite checks on both operating systems. The initial rules are `derive-surface-gravity`, `derive-orbital-period`, `derive-equilibrium-temperature`, `derive-pressure-at-altitude`, `derive-escape-velocity`, `derive-atmosphere-retention`, `derive-tidal-lock`, and `derive-orbit-spacing`, each at revision 1; the numeric tables hold their constants. A rule is referenced where an aggregation rule would be, so the graph has one mechanism for every composed or derived value.

**Stored or computed, per parameter.** The category registry declares a derived parameter either *computed*, the default, in which case it is never stored and is evaluated on demand under the pinned rule revision, or *stored and validated*, in which case the generator writes it, the record carries it, and the validator recomputes it under the pinned rule at load and refuses a mismatch as corruption. A parameter is stored only when a consumer needs it in the record, such as an orbital period that phase evaluation reads.

**Periodic motion is a period and a phase.** Every periodic quantity is stored as an unsigned 64-bit period in ticks and a binary-turn phase at the epoch ([decision 0036](0036-frames-and-transforms-typed-by-connector-kind.md)). Its phase at simulation time `t` is `phase0 + floor(((t - t0) mod P) * 2^32 / P)` in wide integer arithmetic, so the result is exact for every `t` and the same on every machine. Nothing authoritative integrates a rate per tick. The mean anomaly of an orbit and the prime-meridian angle of a spinning body are both evaluated this way.

**Propagation is a named revision.** The analytic two-body model of [decision 0032](0032-astronomically-consistent-surface-sky.md) is the generator revision identifier `propagate-two-body/1`: mean anomaly from the rule above, Kepler's equation solved by a fixed iteration count in fixed-point with a table-driven sine and cosine, and position composed along the ancestry in the system inertial frame. Its recorded vectors are part of the Surface-sky consistency check.

**The hierarchy is the composition tree.** An orbit connector's parent is a body or an explicit `barycentre` primitive, which is non-rendered and whose mass is the aggregated sum of its children's. The system root is the star or a barycentre; moons orbit planets; each orbit is two-body about its parent. Binary stars and moons therefore need no second mechanism.

## Consequences

Easier: gravity, period, and temperature cannot disagree with the mass, radius, and orbit they follow from; every physical relationship has a revision the manifests pin and the retained-implementation rule of decision 0035 covers; rotation and orbital phase are exact at any time. Harder: each rule needs an integer or fixed-point implementation with roots, powers, and a table-driven sine, each with vectors computed outside this repository, and a rule change is a new identifier rather than an edit. Verified by the Determinism and Surface-sky consistency checks in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Technical design: primitive sets and compact references](../technical-design.md#primitive-sets-and-compact-references), [astronomical state](../technical-design.md#astronomical-state-and-surface-sky), and [generation lifecycle](../technical-design.md#generation-lifecycle)
- [Development plan: verification](../development-plan.md#verification-and-performance-targets)
- [Glossary: worlds and destinations](../glossary.md#worlds-and-destinations)
- [Review finding 40](../review.md#40-physical-derivation-rules-are-missing)
- [Progress: design documentation](../progress.md#design-documentation)
