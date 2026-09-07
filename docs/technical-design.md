# Space Explorer: technical design draft

Reviewed 2026-09-07. This is a proposed implementation contract for the [concept](concept.md). No engine, database, transport service, or library is selected, and no benchmark result is claimed. Requirements below apply to a future implementation.

## Architecture and ownership

Separate the deterministic generator, content registry, world store, campaign simulation, renderer, and network transport. Generation returns data independent of rendered frames or player input. Solo play uses the same command validation as hosted play.

The campaign owns credits, artifact custody, progression, and destination references. World packages own immutable specifications and pinned dependencies. Presentation caches can be rebuilt without changing discovery locations or tradeable objects.

## Destination identity and determinism

A destination specification contains a seed, distance tier, environment preferences, survey focus, fixed content budget, and versions of the generator, registry, grammar, suit-validation profiles, and valuation rules. Define field ordering, numeric encoding, and units. A content hash identifies the base specification; a separate campaign/branch ID identifies the player's history.

Re-entering an existing specification in the same campaign selects that world's current state and never resets loot. The game's fork function assigns a new branch ID; independent saves cannot export items into each other's economy.

Derive random streams from the seed and stable paths such as `system/planet/region/site/artifact`. Pin the random algorithm and derivation method. Never use current time, frame count, thread scheduling, unordered iteration, or an unspecified language hash as random input. Adding optional details must not consume randomness from an unrelated artifact stream.

Save accepted descriptors and composition graphs explicitly. Reconstruct terrain using pinned algorithms and specified authoritative coordinates. Cosmetic GPU differences are acceptable; collision surfaces, routes, and artifact placements must not depend on them. Cross-platform reproduction must be verified on supported builds. A guest receives authoritative descriptors or chunks if local reconstruction differs.

In the core release, determine all site and artifact recipes before accepting a destination; meshes and textures may load later. This makes reachability, predefined values, and uniqueness verifiable before play. Later region expansion must preserve these guarantees.

## Generation lifecycle

The main sequence is `draft -> reserved -> generating -> validating -> ready`. Generation and validation may pause, fail, or be cancelled; paused work resumes its saved stage. A ready world can be revisited and never returns to draft.

1. Validate settings, equipment access, resource allowance, and content dependencies. Fix the seed and budget, then reserve travel payment atomically.
2. Generate stellar and planetary descriptors, including orbits, radius, gravity, atmosphere, temperature envelope, radiation, and life flags in documented units.
3. Generate landing regions, traversal graphs, hazards, observable life, artifact sites, and return paths.
4. Validate the required planets and reachable recovery routes against the declared suit profile. It supplies numeric gravity, pressure, temperature, radiation-dose, chemical-hazard, and mission-duration limits. A safe average planet temperature is insufficient.
5. Retry invalid structures deterministically up to a versioned attempt budget. Try a prevalidated fallback that retains all required constraints. If neither succeeds, fail explicitly and refund once.
6. Checkpoint completed work and stable task IDs or random-stream positions. Make the final package durable before publishing its manifest, settling payment, and exposing the destination.

The core cancellation rule refunds uncompleted reservations and retains the same specification for retries, so cancelling does not reroll a destination. Artifact locations and values are not revealed before readiness. Successfully prepared expeditions commit the charge even if the player later abandons exploration.

Pause at safe boundaries when resource limits are reached. Report completed work, remaining estimates, and validation failures. Every task needs a work or duration limit even inside a days-long expedition. Stop controls must remain responsive.

Closing the game saves progress and stops computation; restarting resumes durable work. A future optional worker must be explicitly enabled, resource limited, and coordinated with the campaign writer. Preparation can coexist with visits to ready worlds. Only one job may commit a particular destination.

## Primitive registry and composition

| Field | Contract |
| --- | --- |
| `pack_id`, `primitive_id` | Stable pack namespace and immutable numeric ID within the pack; never recycle identities. |
| `revision`, `content_hash` | Identify exact definition and asset contents. A changed mesh or rule is a new revision even if its filename is unchanged. |
| `category` | Stellar body, terrain, biome element, site component, artifact component, or another supported kind. |
| `parameters` | Typed ranges, units, defaults, and bounded variation. |
| `connectors`, `constraints` | Compatible attachment types, adjacency rules, exclusions, spatial bounds, and environmental preconditions. |
| `dependencies` | Exact required primitive revisions and assets; missing dependencies invalidate a pack. |
| `provenance` | Author/source and recorded content reuse terms. |

Each world pins a complete registry manifest and grammar revision. Rules define weights, maximum depth, component count, and work bounds. Geometric compatibility and semantic validity are separate: attached pieces can still form an unreachable site or an artifact too heavy for every supported loadout.

Before admitting packs, check unique IDs, valid dependencies, compatible parameter ranges, supported formats, finite recursion, and valid productions for required structures. Never resolve an old world against "latest" content.

Artifact identity uses the world identity and stable artifact path, with collision detection. For appearance comparisons, canonicalize the composition recipe, excluding instance IDs, names, invisible metadata, and irrelevant ordering. Resolve duplicates within a new world using deterministic variation attempts in a fixed order.

Before committing a world, compare its appearance recipes with the campaign's existing artifacts. A collision with an existing world rejects admission and requests a distinct specification; do not silently vary the same specification based on campaign history. Validate uniqueness again at commit if jobs run concurrently. Exhaustion is an explicit content-capacity error. Changing only a label does not create a distinct appearance.

## Persistence and compatibility

| Record | Minimum contents |
| --- | --- |
| Campaign | Branch ID, players, permissions, credits, upgrades, inventory/cargo, catalogue, and destination index. |
| World manifest | Specification identity, seed/inputs, pinned versions, accepted descriptors, dependency hashes, and validation result. |
| Region | Stable ID, recipe, authoritative geometry where required, sites, environmental profile, and completion state. |
| Artifact | Stable ID, origin, appearance graph, physical properties, valuation inputs/version, and predefined value. |
| Mutable state | Discoveries, pickups, current custody, field caches, annotations, and claimed survey rewards. |
| Transaction | Unique command ID, state revision, validated inventory/credit changes, and committed result. |
| Generation job | Specification, reserved charge, completed tasks, checkpoints, progress, and failure state. |

Store descriptors, recipes, and changes instead of every rendered polygon. Deduplicate immutable assets by content hash and compress region data. Mesh/texture caches have a size limit and can be evicted. Never evict the only copy of a recipe, required asset, unreconstructible geometry, or player action.

Inventory, artifact custody, and credits change in one atomic transaction. A crash leaves either the old state or the committed new state, never a paid sale with an artifact still in inventory. Use a transactional store or equivalent durable journal; choose the implementation in M0. World packages need an equivalent commit protocol: write and verify data first, publish references last, and recover incomplete work on restart.

Keep rolling recoverable saves and verify checksums on load. Low disk space pauses generation before endangering the active campaign. Archiving preserves both the world package and mutable state. Removing an archive is an explicit user operation with its loss of access explained.

Save format, generator, and content versions are independent. Retain old generators and dependencies, or materialize every dependent result before retiring those versions. Keeping only visited terrain is insufficient if unvisited regions would later regenerate differently. Export includes dependencies or a verified means of obtaining them; a seed alone is not a portable save.

Migrate a copy, verify world identities and item/credit totals, and retain the original backup. Missing content causes a specific compatibility error; never substitute a current primitive or silently reroll a planet. Old worlds stay frozen by default. Applying new content requires a separately identified branch.

## Artifact valuation and transactions

Recommended prototype rule, replacing literal elapsed-time pricing:

```text
base_value = round(k * (generation_effort + recovery_effort) * rarity_factor)
```

Generation effort is a reference-time score from the committed tier and recipe. Recovery effort is expected active effort from the site's route and challenge. Both are fixed before discovery in common reference-time units. The versioned coefficient `k` converts effort to credits; rarity is bounded. Persist inputs and rounded value so later tuning does not reprice existing finds.

Illustrative values, not balance results: with `k = 10`, generation effort 4, recovery effort 6, and rarity 1.5, the base value is 150 credits. Taking twice as long to find it, pausing generation, or using a slower PC leaves that value unchanged.

Each tier also defines total artifact-value and artifact-count bounds. Validate the sum of prices so a full system-generation reward is not multiplied across arbitrarily many trivial items. Player preferences cannot set monetary coefficients.

The core uses fixed versioned dealer spreads; variable regional demand is deferred. For the example artifact, a dealer purchase offer of 120 and sale price of 180 prevents profitable immediate cycling. Buying it back restores custody of the same instance without resetting discoveries or rewards.

Optional capped survey bonuses pay once per newly completed objective and have a campaign claim ledger. Idling, retracing explored paths, and repeating scans do not earn them. They are separate from artifact prices. No real-money redemption is part of the concept.

Commands carry unique IDs. The authority checks location, reachability, capacity, ownership, permission, price, and expected state revision as relevant. Retried commands return their original results. Two players seeking one artifact yield one successful pickup; repeated sale requests yield one credit change. NPC trading changes custody and credits in the same transaction.

## Multiplayer and trust boundary

The core supports a host and one guest in a private campaign. The host is authoritative while also playing. Guest inventory and all progress live in the host's save. Credits and collection storage are shared; spending and selling require host permission. Guests can move, scan, and collect into their campaign inventory subject to validation. Transfers retain artifact identity and history.

Joining exchanges protocol versions, campaign identity, manifests, and state revision. Obtain matching content or authoritative data before play; reject unsupported combinations explicitly. Initial synchronization includes removed artifacts and committed state, not just the seed. Later updates carry ordered revisions and acknowledgments. State-hash mismatches trigger resynchronization.

Disconnecting never undoes hazards already applied: committed emergency recovery retains its field cache. Otherwise persist the guest's location and cargo and suspend that avatar; on rejoin restore it, keeping the ship return route available. Disconnect/rejoin must not duplicate items or provide free extraction. When the host exits, the session ends and resumes from its last durable state. Independent hosts cannot merge campaigns automatically.

Internet access needs a selected platform invitation/relay service or a documented directly reachable host. A LAN demonstration is insufficient. Before M2, choose the transport, account requirements, costs, and service-unavailable behavior. Solo play remains usable offline.

Bound message and package sizes, entity counts, nesting, decompressed data, and content types. Guests and imported worlds cannot execute supplied scripts. Checksums detect corruption but do not prove that an offline owner has not edited a save. Private host trust is sufficient here. A future public market requires a separate server-controlled ownership model and cannot trust arbitrary offline inventories.

## Verification

Implementation milestones, proposed benchmark targets, and the test matrix are maintained in the [development plan](development-plan.md#verification-and-performance-targets).
