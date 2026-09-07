# Glossary

Reviewed 2026-09-07. Normative terminology for the project documents ([decision 0006](decisions/0006-pack-identity-and-allocation.md)). Entries marked *proposed* come from the [design review](review.md) and become definitive when the corresponding finding is cleared.

## Content identity

| Term | Definition | Source |
| --- | --- | --- |
| Primitive definition | A typed reusable building block. Identity is `(pack_id, primitive_id)`; exact content is identified by its revision and content hash. | [Registry](technical-design.md#primitive-registry-and-composition) |
| Pack | A namespace for primitive definitions identified by a 128-bit `pack_id`. An authored pack has revisions and an append-only allocation ledger; a generated pack is published once per set-generation run with IDs 1..n and its identifier derived from the set specification. | [Registry](technical-design.md#primitive-registry-and-composition), [decision 0006](decisions/0006-pack-identity-and-allocation.md) |
| Pack revision | A snapshot of a pack's definitions and their content hashes. Revisions of one pack share the allocation ledger. | [Registry](technical-design.md#primitive-registry-and-composition) |
| Allocation ledger | The append-only record of IDs allocated within an authored pack. IDs are never recycled; zero is reserved. Generated packs need no ledger. | [Registry](technical-design.md#primitive-registry-and-composition) |
| Template / vocabulary | An authored parametric rule from which primitive definitions are generated. The vocabulary is the set of templates plus their constraints. | [Concept 8](concept.md#8-randomness-from-expandable-primitives) |
| Primitive set | An immutable, validated collection of exact primitive revisions plus composition rules and dependencies. The first stored construction unit; identified by its manifest hash. | [Sets](technical-design.md#primitive-sets-and-compact-references) |
| Set manifest | The record identifying a set: generation specification, accepted definitions, membership or reference table, exact dependencies, provenance, and validation result. | [Persistence](technical-design.md#persistence-and-compatibility) |
| Package | An immutable on-disk bundle with a manifest and data files. Either a set package or a world package. | [Decision 0006](decisions/0006-pack-identity-and-allocation.md) |
| Handle | A package-local unsigned index into that package's reference table. Not an identity; tables cannot be reordered in place. | [Sets](technical-design.md#primitive-sets-and-compact-references) |
| Content hash | A specified hash such as SHA-256 over a canonical binary encoding. Never a runtime object hash. | [Determinism](technical-design.md#destination-identity-and-determinism) |
| Generator version | The integer identifying the frozen set of authoritative generation algorithms: the SHA-256 stream derivation, PCG32 initialisation and output, bounded sampling, and the stream-path grammar. Manifests pin it, and changing any frozen algorithm increments it. Currently 2. | [Decision 0008](decisions/0008-random-stream-derivation.md), [decision 0021](decisions/0021-sha256-stream-derivation.md) |
| Stream path | The stable canonical path, such as `system/planet/0/region/3/artifact/2`, that addresses one random stream within a world and forms part of an artifact's identity. Segments are non-empty runs of `[A-Za-z0-9_-]` joined by `/`, case-sensitive, encoded as UTF-8. | [Decision 0008](decisions/0008-random-stream-derivation.md), [decision 0019](decisions/0019-generator-version-1-frozen.md) |

## Worlds and destinations

| Term | Definition | Source |
| --- | --- | --- |
| Destination specification | Campaign branch ID, seed, distance tier, environment preferences, survey focus, fixed content budget, and pinned versions of generator, registry, grammar, suit profiles, and valuation rules. Its content hash identifies the world within its campaign. | [Determinism](technical-design.md#destination-identity-and-determinism), [decision 0007](decisions/0007-artifact-collision-substitution-and-campaign-salt.md) |
| World | A destination generated from a specification. Pins set manifests and a grammar revision. Lives in its discoverer's campaign and is visited only in a session that discoverer hosts. | [Composition validation](technical-design.md#composition-validation) |
| World package | The immutable specification, accepted descriptors, pinned dependencies, and region data of a world. | [Architecture](technical-design.md#architecture-and-ownership) |
| World manifest | The manifest of a world package: specification identity, seed and inputs, pinned versions, accepted descriptors, dependency hashes, and validation result. | [Persistence](technical-design.md#persistence-and-compatibility) |
| Generation job | The durable record of one destination's preparation: specification, reserved charge, completed tasks, checkpoints, progress, and failure state. Stages are draft, reserved, generating, validating, ready. | [Lifecycle](technical-design.md#generation-lifecycle) |
| Region | A bounded landing area of at most 2,048 m per axis on an explorable planet, with a stable ID, recipe, stored authoritative heightfield and placements, sites, environmental profile, and completion state. Positions inside it are region-local fixed-point integers. | [Persistence](technical-design.md#persistence-and-compatibility), [decision 0010](decisions/0010-units-coordinates-and-region-bounds.md), [decision 0017](decisions/0017-region-extent-cap-and-storage-derivation.md) |
| Site | A located point of interest within a region: an artifact site, hazard, observation point, or return path node. | [Lifecycle](technical-design.md#generation-lifecycle) |
| Composition graph | The ordered graph of primitive instances, transforms, and edges that composes a region, site, or artifact. Not deduplicated set membership. | [Sets](technical-design.md#primitive-sets-and-compact-references) |
| Appearance recipe | The canonicalized composition recipe of an artifact used for uniqueness comparison, excluding instance IDs, names, invisible metadata, and irrelevant ordering. | [Composition validation](technical-design.md#composition-validation) |
| Variation overlay | The list in a world manifest of artifact path and variation index for substitutions applied at generation to avoid appearance collisions with the campaign's existing artifacts. | [Decision 0007](decisions/0007-artifact-collision-substitution-and-campaign-salt.md) |
| Distance tier | A discrete access level from the home anchor that sets content budget, reward range, and recovery demands. The core has two tiers. | [Scope](development-plan.md#scope-and-working-assumptions) |
| Home anchor | The fixed location for trading and progression from which distance is measured and every expedition departs and returns. | [Scope](development-plan.md#scope-and-working-assumptions) |

## Campaign and play

| Term | Definition | Source |
| --- | --- | --- |
| Campaign | The host-owned save holding branch ID, players, permissions, credits, upgrades, inventory, catalogue, and destination index. | [Persistence](technical-design.md#persistence-and-compatibility) |
| Branch | The identity of one campaign's history. Forking assigns a new branch ID; branches never exchange inventories. | [Determinism](technical-design.md#destination-identity-and-determinism) |
| Artifact (instance) | A located, collectable object with a stable ID, origin, appearance graph, physical properties, valuation inputs, and predefined value. Distinct from the primitive definitions it is composed from. | [Persistence](technical-design.md#persistence-and-compatibility) |
| Reference effort | The fixed, versioned effort scores for generation and recovery from which an artifact's base value is computed. Independent of wall-clock time. | [Valuation](technical-design.md#artifact-valuation-and-transactions) |
| Field cache | The recoverable store of carried artifacts left behind when a player's suit reserves are exhausted. | [Core-release contents](development-plan.md#core-release-contents) |
| Command | A uniquely identified request validated by the authority against location, reachability, capacity, ownership, permission, price, and state revision. Retries return the original result. | [Valuation](technical-design.md#artifact-valuation-and-transactions) |
| Host | The player whose machine owns the campaign save and performs all simulation and generation compute for a session. | [Decision 0003](decisions/0003-network-topology-and-transport.md) |
| Guest | An invited player whose avatar, inventory, and progress live in the host's campaign. The core release supports one; the session model supports more up to host capacity. | [Decision 0003](decisions/0003-network-topology-and-transport.md) |
| Coordination server | A simple cloud service providing directory, invitation, presence, and relay or NAT-traversal assistance for hosts. Holds no campaign state and computes no worlds. | [Decision 0003](decisions/0003-network-topology-and-transport.md) |
| Tick | The host's fixed simulation step of 50 ms (20 Hz) in which hazards are applied and commands validated; durations are counted in ticks. | [Decision 0010](decisions/0010-units-coordinates-and-region-bounds.md) |
| State revision | The monotonically increasing version of committed campaign state used for optimistic command validation and resynchronization. | [Multiplayer](technical-design.md#multiplayer-and-trust-boundary) |
| Transaction ledger | The append-only, tag-chained list of committed commands from which credits and custody are derived; cached totals are never the only copy. | [Save integrity](technical-design.md#save-integrity), [decision 0018](decisions/0018-data-root-region-encoding-and-save-integrity.md) |
| Modified campaign | A campaign whose ledger chain, record tags, or load-time invariants failed verification. Disclosed to the host and to guests at join; never a lockout. | [Save integrity](technical-design.md#save-integrity), [decision 0018](decisions/0018-data-root-region-encoding-and-save-integrity.md) |
| Data root | The per-user directory holding campaigns, world packages, and set packages: the local application data folder plus `SpaceExplorer`, overridable by `SPACE_EXPLORER_DATA_DIR`. Caches live under a separate cache root. | [Storage location](technical-design.md#storage-location-and-layout), [decision 0018](decisions/0018-data-root-region-encoding-and-save-integrity.md) |
