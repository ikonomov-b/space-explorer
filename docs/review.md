# Design review: planted basics

Reviewed 2026-09-07. This review inspects the documentation-stage repository for foundational decisions that would be costly to reverse once implementation starts, plus smaller gaps and inconsistencies. Each finding states the failure mechanism, a proposed solution, and a status. Clearing a finding means recording the decision in [decisions/](decisions/README.md) and applying the change to the affected documents; the status row then links to both.

The nineteen findings of the documentation stage were cleared on 2026-09-07; the status table links each to its decision record or to the document it changed. An implementation review the same day, after the first two M0a steps landed, raised [findings 20 to 27](#findings-raised-by-the-implementation-review), of which all eight are cleared. A scan of the [development plan](development-plan.md) for gaps and misorders, also 2026-09-07, raised [findings 28 and 29](#findings-raised-by-the-development-plan-scan), both cleared. A review of surface-view realism raised [finding 30](#30-surface-sky-is-decorative-rather-than-astronomically-derived), cleared by decision 0032. An inspection of the primitive storage and handling plan against the design, also 2026-09-07, raised [findings 31 to 36](#findings-raised-by-the-primitive-storage-inspection), all open with proposed solutions. A check of the same structures against the realistic planetary model of decision 0032 raised [findings 37 to 41](#findings-raised-by-the-planetary-model-sufficiency-check), all open with proposed solutions. Checks performed for this review, repeatable with `dotnet run tools/docs.cs -- --check` (links, anchors, decision numbering, the generated decision table; run in continuous integration) and `-- --external` (cited URLs): every internal link and anchor in the documents resolves; all 54 cited external URLs respond, except Epic's Unreal Engine licence page, which answers automated requests with HTTP 403 and must be opened in a browser; the worked valuation and byte-size examples are arithmetically correct. The scaffold of [decision 0016](decisions/0016-platform-confirmed-and-toolchain-pinned.md) builds and its tests pass on both operating systems; only the random foundation and the canonical encoding are tested against code, and the remaining design statements are not. Terms used below are defined in the [glossary](glossary.md).

## Status

| # | Finding | Class | Status |
| --- | --- | --- | --- |
| 1 | [Terrain authority is left as an "or"](#1-terrain-authority-is-left-as-an-or) | Foundational | Cleared by [decision 0001](decisions/0001-materialize-authoritative-terrain.md); its blanket materialization was later replaced by the explicit per-primitive policy of [decision 0031](decisions/0031-primitive-complete-composition-and-storage.md) |
| 2 | [Random primitive sets are untested as content](#2-random-primitive-sets-are-untested-as-content) | Foundational | Cleared, [decision 0002](decisions/0002-primitive-set-content-gate.md) |
| 3 | [Transport selected before connectivity; replication coupling](#3-transport-selected-before-connectivity-replication-coupling) | Foundational | Cleared, [decision 0003](decisions/0003-network-topology-and-transport.md) |
| 4 | [Long journeys lost their rationale](#4-long-journeys-lost-their-rationale) | Foundational | Cleared, [decision 0004](decisions/0004-preparation-is-real-generation-time.md) |
| 5 | [Identity scheme details](#5-identity-scheme-details) | Foundational | Cleared, [decision 0006](decisions/0006-pack-identity-and-allocation.md) |
| 6 | [Uniqueness by rejection](#6-uniqueness-by-rejection) | Foundational | Cleared, [decision 0007](decisions/0007-artifact-collision-substitution-and-campaign-salt.md) |
| 7 | [Random stream derivation](#7-random-stream-derivation) | Foundational | Cleared, [decision 0008](decisions/0008-random-stream-derivation.md) |
| 8 | [Folder layout versus assembly boundaries](#8-folder-layout-versus-assembly-boundaries) | Foundational | Cleared, [decision 0009](decisions/0009-solution-layout.md) |
| 9 | [Units, coordinates, and region bounds](#9-units-coordinates-and-region-bounds) | Foundational | Cleared, [decision 0010](decisions/0010-units-coordinates-and-region-bounds.md); extent cap superseded by [decision 0017](decisions/0017-region-extent-cap-and-storage-derivation.md) |
| 10 | [Requirements provenance and repeated policy text](#10-requirements-provenance-and-repeated-policy-text) | Gap | Cleared, [decision 0011](decisions/0011-requirements-single-source.md) |
| 11 | [Concept wording that will become requirements](#11-concept-wording-that-will-become-requirements) | Inconsistency | Cleared, [decision 0012](decisions/0012-core-travel-model.md) |
| 12 | [Travel topology](#12-travel-topology) | Gap | Cleared, [decision 0012](decisions/0012-core-travel-model.md) |
| 13 | [Guest hazard authority](#13-guest-hazard-authority) | Gap | Cleared, [decision 0013](decisions/0013-host-simulates-hazards.md) |
| 14 | [Tooling predates the stack decision](#14-tooling-predates-the-stack-decision) | Gap | Cleared, [decision 0014](decisions/0014-tooling-and-licence.md) |
| 15 | [Infrastructure-first plan versus fun-first assessment](#15-infrastructure-first-plan-versus-fun-first-assessment) | Process | Cleared, [decision 0015](decisions/0015-greybox-prototype.md) |
| 16 | [Missing comparables](#16-missing-comparables) | Relevance | Cleared, applied to the [assessment](assessment.md#similar-projects) |
| 17 | [Alternatives table omits the mainstream engines](#17-alternatives-table-omits-the-mainstream-engines) | Relevance | Cleared, applied to the [technology decision](technology-stack.md#alternatives-considered) |
| 18 | [Region storage constants are over-determined](#18-region-storage-constants-are-over-determined) | Inconsistency | Cleared, [decision 0017](decisions/0017-region-extent-cap-and-storage-derivation.md); its height/biome representation is now a measured materialized/hybrid candidate under [decision 0031](decisions/0031-primitive-complete-composition-and-storage.md) |
| 19 | [Data location, encoding, and save integrity unspecified](#19-data-location-encoding-and-save-integrity-unspecified) | Gap | Cleared by [decision 0018](decisions/0018-data-root-region-encoding-and-save-integrity.md); fixed region encoding and world-package layout later superseded by [decision 0031](decisions/0031-primitive-complete-composition-and-storage.md) |
| 20 | [Two hashing primitives where one would do](#20-two-hashing-primitives-where-one-would-do) | Foundational | Cleared, [decision 0021](decisions/0021-sha256-stream-derivation.md) |
| 21 | [Pcg32 is a mutable struct](#21-pcg32-is-a-mutable-struct) | Foundational | Cleared, [decision 0023](decisions/0023-pcg32-is-a-sealed-class.md) |
| 22 | [The canonical format has no reader and its reader model was unstated](#22-the-canonical-format-has-no-reader-and-its-reader-model-was-unstated) | Gap | Cleared, applied to the [technical design](technical-design.md#primitive-sets-and-compact-references) |
| 23 | [P0 has not started while M0a proceeds](#23-p0-has-not-started-while-m0a-proceeds) | Process | Cleared by [decision 0022](decisions/0022-p0-gates-the-first-frozen-content-format.md); its criterion-1 wait later superseded by the owner's [decision 0030](decisions/0030-m0a-criterion-1-proceeds-before-the-p0-exit-record.md) |
| 24 | [Tests that pin nothing the recorded vectors do not](#24-tests-that-pin-nothing-the-recorded-vectors-do-not) | Process | Cleared, [decision 0024](decisions/0024-tests-comments-index-and-scaffolding-trimmed.md) |
| 25 | [Documentation restates itself and the generated index is unreadable](#25-documentation-restates-itself-and-the-generated-index-is-unreadable) | Process | Cleared, [decision 0024](decisions/0024-tests-comments-index-and-scaffolding-trimmed.md) |
| 26 | [The Benchmarks project is empty](#26-the-benchmarks-project-is-empty) | Process | Cleared, [decision 0024](decisions/0024-tests-comments-index-and-scaffolding-trimmed.md) |
| 27 | [Code-style enforcement is nominal](#27-code-style-enforcement-is-nominal) | Process | Cleared, [decision 0024](decisions/0024-tests-comments-index-and-scaffolding-trimmed.md) |
| 28 | [Biome rule sets and recovery challenges are undefined content](#28-biome-rule-sets-and-recovery-challenges-are-undefined-content) | Gap | Cleared, [decision 0026](decisions/0026-biome-rule-sets-and-recovery-challenges-are-template-vocabularies.md) |
| 29 | [The in-memory transport, commands, and handshake are untimed for M0a](#29-the-in-memory-transport-commands-and-handshake-are-untimed-for-m0a) | Gap | Cleared, [decision 0027](decisions/0027-in-memory-transport-commands-and-handshake-at-m0a.md) |
| 30 | [Surface sky is decorative rather than astronomically derived](#30-surface-sky-is-decorative-rather-than-astronomically-derived) | Gap | Cleared, [decision 0032](decisions/0032-astronomically-consistent-surface-sky.md) |
| 31 | [A generated definition can never receive a second revision](#31-a-generated-definition-can-never-receive-a-second-revision) | Inconsistency | Open |
| 32 | [The storage policy sits inside the hashed definition that M0b's measurement will change](#32-the-storage-policy-sits-inside-the-hashed-definition-that-m0bs-measurement-will-change) | Foundational | Open |
| 33 | [The generator implementation a regenerate primitive pins has no identity](#33-the-generator-implementation-a-regenerate-primitive-pins-has-no-identity) | Gap | Open |
| 34 | [The category registry has no home, no revision identity, and no stated role in decoding](#34-the-category-registry-has-no-home-no-revision-identity-and-no-stated-role-in-decoding) | Gap | Open |
| 35 | [Two set manifests can install under one pack identifier](#35-two-set-manifests-can-install-under-one-pack-identifier) | Gap | Open |
| 36 | [The shared content-addressed stores have no reference index and no deletion rule](#36-the-shared-content-addressed-stores-have-no-reference-index-and-no-deletion-rule) | Gap | Open |
| 37 | [One transform type cannot span the frame hierarchy](#37-one-transform-type-cannot-span-the-frame-hierarchy) | Foundational | Open |
| 38 | [Planet-level fields have no spherical form and the flat region bounds the explorable body radius](#38-planet-level-fields-have-no-spherical-form-and-the-flat-region-bounds-the-explorable-body-radius) | Foundational | Open |
| 39 | [Nothing is defined beyond the region edge](#39-nothing-is-defined-beyond-the-region-edge) | Gap | Open |
| 40 | [Physical derivation rules are missing](#40-physical-derivation-rules-are-missing) | Gap | Open |
| 41 | [The node budget assumes no derived instances](#41-the-node-budget-assumes-no-derived-instances) | Foundational | Open |

## Foundational findings

### 1. Terrain authority is left as an "or"

Affects: [technical design](technical-design.md#destination-identity-and-determinism), [persistence](technical-design.md#persistence-and-compatibility), [resource-use check](development-plan.md#verification-and-performance-targets).

**Why it can go wrong.** The design requires that old worlds stay frozen, that unvisited regions never regenerate differently, and that terrain is reconstructed from pinned algorithms. Any change to terrain code, including a bug fix or a noise implementation swap, changes reconstructed terrain in old worlds. The only ways out are retaining every historical terrain algorithm in the shipped game or storing authoritative geometry at acceptance. The persistence section names both and chooses neither. Cross-platform floating point adds a second trap: basic arithmetic matches on x86_64, but transcendental functions call the platform C runtime and can differ in the last bits between Linux and Windows.

**Proposed solution.** Materialize the authoritative layer at world acceptance and regenerate only cosmetics. Store per region a quantized heightfield, a biome/material index map, site anchors, the traversal graph, hazard volumes, and artifact placements. Collision, routes, reachability, and placement checks read only stored data. Cosmetic detail is regenerated at runtime under a hard rule: displacement amplitude stays below a fixed epsilon and is never applied to collision meshes. The host always sends stored regions to a guest, so no runtime "reconstruction differs" path is needed. Cross-OS matching becomes a hard requirement for descriptors, recipes, and placements, and a measured target for terrain generation, reachable later with fixed-point noise.

| Region extent | Cell size | Raw int16 heights | Compressed estimate |
| --- | --- | --- | --- |
| 1 km | 4 m | 125 KB | 30 to 60 KB |
| 2 km | 2 m | 2 MB | 0.5 to 1 MB |
| 4 km | 4 m | 2 MB | 0.5 to 1 MB |

Apply by extending the Region record's minimum contents and making "bytes per region" the gate that confirms the cost.

**Status:** Cleared 2026-09-07 by [decision 0001](decisions/0001-materialize-authoritative-terrain.md), then refined by the owner's [decision 0031](decisions/0031-primitive-complete-composition-and-storage.md). Terrain remains exact, but its primitive category now pins `regenerate`, `materialize`, or `hybrid` from cross-platform determinism and performance evidence; inability to prove exact regeneration selects materialization. Applied to the technical design and development plan. The size table above counts heights only and never the adopted 4 km at 2 m combination; the corrected derivation is in [finding 18](#18-region-storage-constants-are-over-determined).

### 2. Random primitive sets are untested as content

Affects: [concept 8](concept.md#8-randomness-from-expandable-primitives), [M0a](development-plan.md#milestones), [registry](technical-design.md#primitive-registry-and-composition).

**Why it can go wrong.** Comparable games with procedural creatures or objects use authored parts with procedural assembly and parameters. This design generates the parts themselves from a small template vocabulary. Generated parts from a small vocabulary tend to converge on a recognizable noise look, which contradicts the concept's demand for visibly distinct, memorable finds. M0a's exit criteria are hashes, IDs, storage, reload, and set operations. A set can pass all of them and still be unusable as content, and by then the registry, manifests, and world format are built around it.

**Proposed solution.** Three changes. Add a content gate to M0a: render a fixed sample of generated primitives in the viewer, have two or three people sort them into distinct versus indistinct, and set a pass threshold in advance; add an automated silhouette comparison from fixed viewpoints as an early duplicate detector. Make the set format origin-agnostic: define `provenance.kind` as authored, generated, or derived, and require that authored meshes and rules can be published into a set through the same manifest machinery, so a failed content gate pivots to authored parts without touching registry, persistence, or world formats. Defer set algebra: specify the identity-resolution rule but implement union, intersection, and difference when a feature needs them. Time-box M0a.

**Status:** Cleared 2026-09-07 by [decision 0002](decisions/0002-primitive-set-content-gate.md). Content gate adopted; sets stay generated-only in the core release, so a later pivot to authored members is a format revision; set algebra specified but deferred. Applied to the development plan, technical design, and technology decision.

### 3. Transport selected before connectivity; replication coupling

Affects: [multiplayer](technical-design.md#multiplayer-and-trust-boundary), [selected components](technology-stack.md#selected-components), [internet hosting decision](development-plan.md#decisions-before-production-commitments).

**Why it can go wrong.** ENet needs a reachable UDP endpoint. Home routers, carrier-grade NAT, and disabled UPnP make "invite a friend" fail for a large share of players. Every realistic fix is a relay, and each relay option replaces the peer rather than sitting on it: Steam networking, Epic Online Services, and WebRTC all supply their own transport. Separately, Godot's spawner and synchronizer nodes replicate scene-tree properties; using them for inventory, credits, or artifact existence puts authoritative state in nodes, which the design forbids and which cannot be tested headlessly.

**Proposed solution.** Define a transport interface in the core with send, receive, peer-connected, and peer-disconnected as its whole surface, carrying bytes serialized with the same versioned binary encoding as saves. Implement it over Godot's raw packet path on an ENet peer and in memory for tests. Commands, state deltas, and the join handshake become core code covered by unit tests. Use Godot's synchronizer only for cosmetic presence data such as avatar transforms. Move the connectivity decision from "before M2" to "before M0b's mixed-OS demo". Evaluate a self-hosted relay first because it keeps the ENet peer, needs no store account, costs one small server, and is reversible; choose Steam networking only if shipping on Steam anyway. Reword the stack table to "ENet peer as the initial transport; the core owns replication".

**Status:** Cleared 2026-09-07 by [decision 0003](decisions/0003-network-topology-and-transport.md). Core-owned replication adopted. Topology set as loosely coupled hosts with all compute on the host, coordinated by a simple cloud server; one guest first with a capacity-scalable session model. Differs from the proposal on timing: connectivity is decided at M1 start rather than before M0b, and the mixed-OS demo moves from M0b to M2 start. Self-hosted relay evaluated first.

### 4. Long journeys lost their rationale

Affects: [concept 4](concept.md#4-travel-and-the-randomness-of-the-destination), [generation lifecycle](technical-design.md#generation-lifecycle), [valuation](technical-design.md#artifact-valuation-and-transactions), [long-wait decision](development-plan.md#decisions-before-production-commitments).

**Why it can go wrong.** The original rule tied value to generation time plus exploration time and tied richness to distance through more computation. The refined rule fixes value from tier and recipe, fixes content by a tier budget, and determines all recipes before acceptance. Compute duration now affects nothing, so a days-long preparation is a pure timer gate. Yet the requirement for days-long resumable computation drives the most complex parts of the lifecycle: multi-day checkpoints, low-disk pauses mid-job, eviction during generation, and the opt-in background worker.

**Proposed solution.** Decide that preparation time is a design parameter, not a computation artifact. Define preparation as in-fiction time that advances while the player plays other expeditions; a destination becomes ready when the generation job finishes and the fictional clock has elapsed. Keep pause, resume, and checkpoints for crash safety, sized for minutes. Make the effort score explicit: an authored tier base plus a recipe complexity score computed from the composition graph, stored under a valuation version. Update concept 4 to retire the compute-time rationale and move the wall-clock long mode to M3 as an explicit alternative.

**Status:** Cleared 2026-09-07 by [decision 0004](decisions/0004-preparation-is-real-generation-time.md). Differs from the proposal: preparation is real generation time rather than in-fiction time, with no artificial padding; larger tiers compute longer, and days-long budgets are the M3 experiment. Effort score adopted as tier base plus recipe complexity. Applied to concept, plan, technical design, and assessment.

### 5. Identity scheme details

Affects: [registry](technical-design.md#primitive-registry-and-composition), [decision 5](technology-stack.md#gap-filling-decisions-for-confirmation).

**Why it can go wrong.** The format and allocator of `pack_id` are unspecified, so human-chosen names from independent authors collide. The pack that receives IDs allocated by a generated set is unspecified. Reproducing a set requires "the same starting allocation state", so two machines cannot independently reproduce a set without sharing a ledger. The terms pack, pack revision, set, manifest, package, and handle overlap, which is how a wrong schema gets written on day one.

**Proposed solution.** Make `pack_id` a 128-bit identifier with a human-readable name as metadata only; generate it randomly once for authored packs and derive it from the canonical set specification hash for generated packs. Adopt one allocation rule: each set-generation run publishes exactly one new pack, its definitions receive IDs 1..n in stable generation order, and its manifest pins the base packs it drew templates from. Only authored packs keep a persisted ledger, an append-only text file validated for monotonicity. Store `pack_id` as a 16-byte BLOB and `primitive_id` as INTEGER with a range check. Keep 4-byte handles in packed recipes but state expected scale, hundreds of definitions per set and thousands of nodes per world, instead of presenting the byte saving as a design driver. Adopt the [glossary](glossary.md).

**Status:** Cleared 2026-09-07 by [decision 0006](decisions/0006-pack-identity-and-allocation.md). 128-bit pack identifiers, one new pack per generation run, ledger only for authored packs, glossary normative. Applied to technical design, technology decision, development plan, and glossary.

### 6. Uniqueness by rejection

Affects: [composition validation](technical-design.md#composition-validation), [destination validity check](development-plan.md#verification-and-performance-targets).

**Why it can go wrong.** At commit, every new artifact's canonical appearance recipe is compared with the whole campaign, and one collision rejects the whole destination. Collision probability grows with campaign size times artifacts per world divided by a recipe space that is finite by design. Late in a campaign the player sees "destination unavailable" errors caused by invisible state, on destinations they may have chosen by seed.

**Proposed solution.** Replace whole-world rejection with per-artifact substitution. On collision, re-sample only the colliding artifact's variation stream with the campaign branch as salt, choosing among equal-effort alternatives so value is unaffected. Geography, sites, and every other artifact remain determined by the specification, so seeds stay shareable and world identity stays the specification hash. Record the substitution as a variation overlay in the campaign's mutable state, keyed by artifact path. Fall back to rejection only when variation attempts are exhausted. Define recipe canonicalization to bucket continuous parameters so near-identical recipes collide. Add a simulated 50-world campaign to the seed suite and record collision rates with and without the overlay.

**Update 2026-09-07.** [decision 0005](decisions/0005-discoverer-hosts-every-visit.md) makes a discovered system reachable only through its discoverer, so sharing a seed between players is no longer a design goal. The original reason for rejecting campaign-salted variation no longer applies, and salting world generation by campaign branch becomes a candidate alongside per-artifact substitution.

**Status:** Cleared 2026-09-07 by [decision 0007](decisions/0007-artifact-collision-substitution-and-campaign-salt.md). Per-artifact substitution adopted, with the substitution list recorded in the world manifest rather than in mutable state; the campaign branch is part of the specification. Applied to technical design, development plan, and glossary.

### 7. Random stream derivation

Affects: [determinism](technical-design.md#destination-identity-and-determinism), [reproducible randomness component](technology-stack.md#selected-components).

**Why it can go wrong.** PCG32 keeps a 64-bit state and a stream selector. Different streams over the same state are not independent, and seeding sibling streams from related values is the classic correlation pitfall. Deriving per-path streams by hashing only the path into the stream selector can correlate neighbouring regions or sibling artifacts. Fixing this later requires a generator version bump and a retained old generator.

**Proposed solution.** Derive both state and stream from a pinned 64-bit mixer over the world seed and the canonical path bytes, and pin the path encoding and bounded sampling:

```text
h = mix64(world_seed, utf8(path))        # SplitMix64 finalizer or murmur3 fmix64, pinned
state0    = h.low64
increment = (h.high64 << 1) | 1
NextBelow(bound) uses rejection sampling; no floating-point sampling in authoritative code
```

Use the reference implementation's demo output for seed 42 and stream 54 as the first test vector. Enforce the "never use" list with a banned-API analyzer in the core project: `System.Random`, `Guid.NewGuid`, `DateTime.Now`, `Environment.TickCount`, `string.GetHashCode`. Run the determinism test in two separate processes to catch reliance on dictionary iteration order, since .NET randomizes string hashing per process.

**Status:** Cleared 2026-09-07 by [decision 0008](decisions/0008-random-stream-derivation.md). Mixed state/stream derivation, rejection sampling, banned-API analyzer, and two-process determinism test adopted. Applied to technical design, technology decision, and development plan.

### 8. Folder layout versus assembly boundaries

Affects: [src/README.md](../src/README.md), [architecture](technology-stack.md#architecture-and-first-implementation-step).

**Why it can go wrong.** The architecture has four consumers, core library, CLI, Godot game, and tests, but the folders are seven responsibilities. Godot needs its own project folder with a project file and a resource root, and the root `assets/` directory sits outside it. Once project files reference each other, moving folders touches every project, the Godot resource paths, and CI.

**Proposed solution.** Lay out the solution by assembly before the first project file and map the current folders into namespaces:

```text
SpaceExplorer.sln
global.json                    pinned SDK
Directory.Build.props          target framework, nullable, warnings as errors, analyzers
Directory.Packages.props       central package versions
src/
  SpaceExplorer.Core/          generation, registry, campaign, valuation, PRNG, binary formats
  SpaceExplorer.Persistence/   SQLite adapter and package I/O
  SpaceExplorer.Cli/           set generator, validator, inspector
  SpaceExplorer.Game/          Godot project: project.godot, scenes, rendering and network adapters, assets/
content/                       authored pack sources: templates, ledgers, numeric tables
tests/
  SpaceExplorer.Core.Tests/
  SpaceExplorer.Persistence.Tests/
  SpaceExplorer.Benchmarks/
  SpaceExplorer.Game.Smoke/    exported-build smoke scripts
```

Core and Persistence are plain class libraries with no Godot reference; one architecture test asserts the Core assembly never references the Godot assembly. Pin the target framework to the one the chosen Godot release supports. Authored content lives outside the Godot project because the CLI consumes it.

**Status:** Cleared 2026-09-07 by [decision 0009](decisions/0009-solution-layout.md). Assembly layout adopted and the placeholder folders restructured; authored content in `content/` at the root; root `assets/` retired; pinning files deferred to M0a start. Applied to the repository tree, src/README.md, technology decision, and technical design.

### 9. Units, coordinates, and region bounds

Affects: [technical design](technical-design.md#destination-identity-and-determinism), [core-release contents](development-plan.md#core-release-contents).

**Why it can go wrong.** Godot's default vectors are 32-bit floats, and physics jitter appears tens of kilometres from the origin. Bounded regions mitigate this only if a maximum region size exists. The same numbers drive terrain storage, network position encoding, and every authoritative distance check. Choosing a double-precision engine build later changes export templates and the C# bindings.

**Proposed solution.** Fix these values in the technical design as versioned constants:

| Quantity | Proposal |
| --- | --- |
| Length unit | metre; authoritative positions as int32 fixed-point at 1/256 m |
| Region frame | local origin at region centre; region-local coordinates everywhere |
| Region extent cap | 4,096 m per axis |
| Heightfield | 2 m or 4 m cells; heights as int16 at 1/16 m |
| Simulation tick | fixed host step of 20 Hz; durations in ticks; reserves in integer units |
| Planetary values | integers in documented units such as metres and millimetres per second squared |

Authoritative checks run on fixed-point values in the core; the Godot adapter converts to floats for rendering and physics. Rule out double-precision builds unless measurements force them.

**Status:** Cleared 2026-09-07 by [decision 0010](decisions/0010-units-coordinates-and-region-bounds.md). All proposed constants adopted: int32 fixed-point at 1/256 m, region-local frame, 4,096 m cap, 2 m cells with int16 heights at 1/16 m, 20 Hz tick. Applied to technical design, development plan, and glossary. Later the same day the cap was found inconsistent with the per-region storage budget and reduced to 2,048 m by [decision 0017](decisions/0017-region-extent-cap-and-storage-derivation.md); see [finding 18](#18-region-storage-constants-are-over-determined).

## Smaller findings

### 10. Requirements provenance and repeated policy text

Affects: [assessment](assessment.md), [decision register](technology-stack.md#gap-filling-decisions-for-confirmation), [concept](concept.md), all documents restating the Linux-first policy.

**Why it can go wrong.** The assessment audits "the original nine-point concept" and the stack decision cites "the platform discussion", but neither exists in the repository. The gaps table cannot be checked, the "user requirement" labels cannot be verified, and the register and the assessment disagree about whether the M0a ordering was a requirement or a filled gap. The Linux-first policy is restated in seven files. The concept carries a platform sentence although the assessment says it contains only the game idea.

**Proposed solution.** Add `docs/requirements.md` holding the original nine points verbatim, marked as such, plus the platform requirements with the date stated. Record decisions in [decisions/](decisions/README.md). Replace repeated Linux-first paragraphs with one sentence and a link. Move the platform sentence from the concept into requirements. Record that the M0a ordering was requested during the platform discussion and absent from the written concept.

**Status:** Cleared 2026-09-07 by [decision 0011](decisions/0011-requirements-single-source.md). Differs from the proposal: no separate original text exists, so concept.md is declared the concept of record and the gaps table is labelled as quoting its first draft. requirements.md created as the single source for platform policy and owner constraints; restatements reduced to one sentence plus link; platform sentence moved out of the concept.

### 11. Concept wording that will become requirements

Affects: [core-release contents](development-plan.md#core-release-contents), [concept 1, 2, 5](concept.md).

**Why it can go wrong.** "Two unlockable distance tiers" reads as two unlocks while the rest of the plan means one. The concept says "friends" and "travel distance is the main control" while the core has one guest and two discrete tiers. Such mismatches are read literally when acceptance tests are written.

**Proposed solution.** Write "two distance tiers, the second unlocked with credits". Make the core distance control a slider that snaps to tiers and defer continuous distance to M3. Write "a friend" for the core and keep larger groups under M3.

**Status:** Cleared 2026-09-07 by [decision 0012](decisions/0012-core-travel-model.md). All three wording fixes applied.

### 12. Travel topology

Affects: [scope](development-plan.md#scope-and-working-assumptions), [concept 7](concept.md#7-the-players-goal).

**Why it can go wrong.** "Repeated short hops cannot bypass access tiers" only makes sense if destination-to-destination travel exists, yet the concept's loop is home, destination, home. Chained hops change where the ship can be, what a save must record, and where a guest joins.

**Proposed solution.** Decide hub-and-spoke for the core: every expedition starts and ends at the home anchor, and the ship is always at home or at exactly one destination. Delete the hop sentence and note chained travel as an M3 option with tier defined by distance from home.

**Status:** Cleared 2026-09-07 by [decision 0012](decisions/0012-core-travel-model.md). Hub and spoke adopted; hop sentence removed; chained travel listed under M3.

### 13. Guest hazard authority

Affects: [multiplayer](technical-design.md#multiplayer-and-trust-boundary).

**Why it can go wrong.** Nothing says whether the host or the guest computes a guest's suit depletion and emergency recovery. Guest computation lets impossible states reach the host save; host computation needs guest positions at tick rate and shapes the message design.

**Proposed solution.** The host simulates hazards for every avatar from client-reported positions and inputs at the fixed tick. Clients predict locally for display only. The host is authoritative on reserve exhaustion, emergency recovery, and the field cache.

**Status:** Cleared 2026-09-07, [decision 0013](decisions/0013-host-simulates-hazards.md). Host simulates all avatars; clients never report reserves.

### 14. Tooling predates the stack decision

Affects: `.gitignore`, `.editorconfig`, missing `LICENSE`, missing CI.

**Why it can go wrong.** The ignore file reflects the removed TypeScript scaffold and lacks .NET, Godot, and database entries. The editorconfig has no C# rule. No licence exists although the stack document leaves the game's licence undecided. Both-OS gates are mandatory with no automation.

**Proposed solution.** Add ignore entries for `bin/`, `obj/`, `.godot/`, `*.db`, `*.db-journal`, `*.db-wal`, `BenchmarkDotNet.Artifacts/`, `TestResults/`, `.vs/`, `*.user`. Add `[*.cs]` with four-space indentation and `[*.{csproj,props,targets}]` with two-space indentation to the editorconfig. Choose a licence before third-party code or assets enter the repository. Add a Linux and Windows test matrix the day the first test project exists.

**Status:** Cleared 2026-09-07, [decision 0014](decisions/0014-tooling-and-licence.md). Ignore and editorconfig additions applied; LICENSE added with all rights reserved until decided; CI matrix rule recorded.

## Process and relevance

### 15. Infrastructure-first plan versus fun-first assessment

Affects: [assessment recommendation](assessment.md#relevance-and-recommendation), [milestones](development-plan.md#milestones).

**Why it can go wrong.** The assessment says to test whether destination and artifact choices are meaningful before committing to whole planets or long computation. The first playable is M1, after two milestones with cross-OS hash gates, a CLI, a viewer, and packaged smoke tests. If the expedition loop is not fun, that infrastructure was built for a game that needs redesign.

**Proposed solution.** Run a disposable greybox prototype in parallel with M0a, in a `prototypes/` folder that nothing in `src/` may reference. Hard-code one landing region with three sites and four artifacts, a cargo limit that forces choosing two, a return, a sale, and a second destination choice; no generation and no persistence. Put it in front of three to five players and record whether they can explain a choice that changed their outcome. Budget one to two weeks.

**Status:** Cleared 2026-09-07, [decision 0015](decisions/0015-greybox-prototype.md). P0 greybox prototype added to the milestones in parallel with M0a; prototypes/ folder created with its isolation rule.

### 16. Missing comparables

Affects: [similar projects](assessment.md#similar-projects).

**Why it can go wrong.** The comparison omits the closest recent big-budget precedent and several small-scope references, so positioning claims rest on an incomplete field.

**Proposed solution.** Add four rows with primary sources and a specific lesson each: Starfield, for procedural planets with artifacts as the central objective and the reception of many similar generated surfaces; The Long Journey Home, for procedural systems with alien artifact trading at small-team scope; Journey to the Savage Planet, for bounded-planet exploration with cataloguing as the core motivation; Subnautica, for hazard-driven preparation and survival envelopes on a single world.

**Status:** Cleared 2026-09-07, applied to the [assessment](assessment.md#similar-projects). Four rows added with verified sources: Starfield, The Long Journey Home, Journey to the Savage Planet, Subnautica.

### 17. Alternatives table omits the mainstream engines

Affects: [alternatives considered](technology-stack.md#alternatives-considered), open item 4 of [decision 0016](decisions/0016-platform-confirmed-and-toolchain-pinned.md).

**Why it can go wrong.** The table compared Godot .NET only with GDScript and two Python engines. A reader could not see whether Unity, Unreal, Stride, Flax, or MonoGame satisfy R1 to R5 as well or better, so the platform confirmation in decision 0016 rested on a comparison that was never written down and could not be challenged.

**Proposed solution.** Add one row per engine stating licence terms, Linux editor status, and language fit, each taken from the vendor's own pages on the day of the check, and give the rejection reason in terms of R1 to R5. Name the runner-up so a failed M0 validation has a documented fallback.

**Status:** Cleared 2026-09-07, applied to the [technology decision](technology-stack.md#alternatives-considered). Five rows added with official sources; Flax is recorded as the runner-up. Epic's licence page is not machine-verifiable (HTTP 403) and its royalty figures need a browser check.

## Findings raised after the platform confirmation

### 18. Region storage constants are over-determined

Affects: [decision 0010](decisions/0010-units-coordinates-and-region-bounds.md), [decision 0001](decisions/0001-materialize-authoritative-terrain.md), [units table](technical-design.md#units-coordinates-and-bounds), [resource-use check](development-plan.md#verification-and-performance-targets).

**Why it can go wrong.** Decision 0010 fixed a 4,096 m extent cap and 2 m cells and quoted about 2 MB raw per maximal region; decision 0001 budgets 0.1 to 1 MB per region. Both numbers came from the table in [finding 1](#1-terrain-authority-is-left-as-an-or), which sized heights alone at 2 km with 2 m cells and 4 km with 4 m cells, never 4 km with 2 m cells, and never with the biome byte. For the adopted combination a maximal region is 2,048 × 2,048 × 3 bytes = 12.0 MiB raw. Raw bytes = 3 × (extent / cell)², so the cap, the cell size, and the budget could not all hold, and the resource-use gate would have failed on arithmetic rather than on engineering.

**Proposed solution.** Keep two constants and derive the third. Keeping the cap and the cells raises the budget to about 2 MiB compressed; keeping the cap and the budget coarsens cells to 4 m, which decision 0001 would turn into 4 m collision facets; keeping the cells and the budget lowers the cap to 2,048 m, 3 MiB raw. Lower the cap: it is an upper bound nothing depends on, a 2 km crossing already takes about 17 minutes on foot against a 15 to 30 minute expedition, and it quarters the terrain meshing workload. Restate the raw figure correctly and keep the biome map at 4 m cells as the fallback if compression disappoints.

**Status:** Cleared 2026-09-07 by [decision 0017](decisions/0017-region-extent-cap-and-storage-derivation.md). Cap reduced to 2,048 m; 3 MiB raw, at most 1 MiB compressed; other constants unchanged. Applied to the technical design, development plan, glossary, and the decisions index.

### 19. Data location, encoding, and save integrity unspecified

Affects: [architecture](technical-design.md#architecture-and-ownership), [persistence](technical-design.md#persistence-and-compatibility), [multiplayer trust boundary](technical-design.md#multiplayer-and-trust-boundary).

**Why it can go wrong.** The design asked for platform-appropriate user-data locations, content-hash deduplication, and compressed region data without naming a directory, a layout, an encoding, or a compressor, so the game, the command-line tool, and the tests could each pick their own and disagree, and Godot's default `user://` would put gigabytes of world data in a roaming profile on Windows. It also noted that checksums cannot detect an edited save and stopped there, leaving credits and artifact values as plain numbers in a database anyone can open.

**Proposed solution.** One data root owned by Persistence in the local application data folder with an environment override, caches in a separate deletable root, packages named by content hash, a fixed region encoding of delta-predicted heights and run-length-coded biome rows under Brotli from the .NET base library, and a deterrent against casual editing: derive credits and custody from a tag-chained ledger, tag every record with a keyed hash, verify invariants at load, and disclose a modified campaign to guests instead of locking the owner out.

**Status:** Cleared 2026-09-07 by [decision 0018](decisions/0018-data-root-region-encoding-and-save-integrity.md). Its data root and save-integrity decisions stand; [decision 0031](decisions/0031-primitive-complete-composition-and-storage.md) later replaced the fixed region codec and world layout with authoritative primitive graphs, content-addressed blobs, adaptive primitive-owned payloads, and evictable caches. Applied to the technical design, development plan, technology decision, and glossary.
## Findings raised by the implementation review

Raised 2026-09-07 after the first two M0a steps landed ([decision 0019](decisions/0019-generator-version-1-frozen.md) and [decision 0020](decisions/0020-canonical-encoding-and-content-hash.md)), from a read of every tracked file and the checks named in each finding. Measurements are from the Debian 13 x86_64 workstation in release configuration. Findings are ordered by class, as above, not by importance; 23 is the one that decides whether the rest matter.

### 20. Two hashing primitives where one would do

Affects: [decision 0008](decisions/0008-random-stream-derivation.md), [decision 0019](decisions/0019-generator-version-1-frozen.md), [determinism](technical-design.md#destination-identity-and-determinism).

**Why it can go wrong.** Stream derivation runs a SplitMix64-based construction that decision 0019 itself describes as "this project's own, not an external standard", with two external anchors and three self-produced vectors as its whole evidence. Decision 0020 then brought SHA-256 into the core for the content hash. The core therefore carries two hashing primitives, and the one without an external specification is the one every artifact identity depends on. Cost does not justify the custom construction: a derivation measures 29 ns with the mixer and 477 ns with SHA-256, so 10,000 paths cost 0.3 ms against 4.8 ms, and derivation happens once per path, never per draw. Replacing it is a generator-version increment, free while no set or world exists and expensive after the first one.

**Proposed solution.** Derive both halves from the SHA-256 content hash of a domain-labelled canonical record holding the seed and the path bytes: the first eight bytes become the state and the next eight, shifted left and forced odd, the increment. Delete the mixer and its tests. This supersedes the mixer clause of decision 0008 and the construction of decision 0019, and increments the generator version to 2 with no stored data to migrate.

**Status:** Cleared by [decision 0021](decisions/0021-sha256-stream-derivation.md), implemented as generator version 2 in `acdc063`, which derives both halves from the SHA-256 content hash of a `random-stream/2` record and deletes the mixer and its tests. The technical design, glossary, technology decision, and `src/README.md` no longer describe a pinned 64-bit mixer.

### 21. Pcg32 is a mutable struct

Affects: `src/SpaceExplorer.Core/Shared/Pcg32.cs` and every future generation call site.

**Why it can go wrong.** Drawing a value mutates the struct. Held in a readonly field, read through a list indexer, or bound by a foreach, the struct is copied, the copy advances, and the original does not, with no compiler diagnostic. Demonstrated: a readonly field and a list element both returned `67f3cd9b` on two successive draws, while a local variable returned `67f3cd9b` then `553d86dc`. The type's remark warns about this, but a comment does not reach the hundreds of call sites generation code will have, and a silently repeated sequence in authoritative generation is a determinism bug that reproduces perfectly and therefore looks like intended output.

**Proposed solution.** Make it a sealed class. One allocation per derived stream is negligible beside the derivation itself, and the copy hazard disappears. Decide before generation code uses the type widely; afterwards the change touches every call site.

**Status:** Cleared 2026-09-07 by [decision 0023](decisions/0023-pcg32-is-a-sealed-class.md). `Pcg32` is a sealed class; the generator version is unchanged because no frozen algorithm moved, and a structural test guards the shape.

### 22. The canonical format has no reader and its reader model was unstated

Affects: [decision 0020](decisions/0020-canonical-encoding-and-content-hash.md), [primitive sets and compact references](technical-design.md#primitive-sets-and-compact-references).

**Why it can go wrong.** The format is written and hashed but never parsed back, which [progress.md](progress.md#m0a-exit-criteria) records. What no document said is that the bytes carry no field tags or types, so a reader cannot be generic: it must dispatch on the domain label and know that record's field order for the declared format version. Left unstated, the first package reader could be attempted as a self-describing parser, or the format could drift toward tagged fields, which changes every hash.

**Proposed solution.** State in the technical design that the format is schema-driven, not self-describing, and build the reader per record kind alongside the first stored record.

**Status:** Cleared 2026-09-07, applied to the [technical design](technical-design.md#primitive-sets-and-compact-references). The reader itself arrives with M0a criterion 3.

### 23. P0 has not started while M0a proceeds

Affects: [decision 0015](decisions/0015-greybox-prototype.md), [milestones](development-plan.md#milestones), [progress](progress.md#milestones).

**Why it can go wrong.** Decision 0015 scheduled the greybox prototype in parallel with M0a for one to two weeks, because [finding 15](#15-infrastructure-first-plan-versus-fun-first-assessment) warned that infrastructure would otherwise be built for a game whose loop is untested. M0a is two steps in, twenty decision records and about 2,100 lines of documentation and tooling exist, and `prototypes/` holds only its README. Nothing built so far shows whether choosing two artifacts out of four under a cargo limit is a decision a player can explain. Each further M0a step raises the cost of learning that it is not.

**Proposed solution.** Start P0 now, in the Godot project as open item 2 of [decision 0016](decisions/0016-platform-confirmed-and-toolchain-pinned.md) suggests, so movement, physics, the Compatibility renderer, and the Windows export are exercised at the same time. Treat the P0 exit record as due before the next M0a step that freezes a new format.

**Status:** Cleared 2026-09-07 by [decision 0022](decisions/0022-p0-gates-the-first-frozen-content-format.md). The owner's later [decision 0030](decisions/0030-m0a-criterion-1-proceeds-before-the-p0-exit-record.md) accepts the compatibility cost and lets the set specification proceed before the still-required P0 exit record. P0's location, scope, and players are fixed by decision 0025.

### 24. Tests that pin nothing the recorded vectors do not

Affects: `tests/SpaceExplorer.Core.Tests/Shared/`, [decision 0019](decisions/0019-generator-version-1-frozen.md).

**Why it can go wrong.** At `cc21bbb`, `Shared/` had 79 tests for 332 executable lines, 41 of them on the random foundation; after the mixer removal in `acdc063` it has 47 tests for 307 lines. Eleven source mutations were run against the suite: bare modulo, a zero threshold, an off-by-one in the rejection rule, an unshifted increment, swapped domain constants, a dropped length step, a dropped trailing chunk, a perturbed LCG multiplier, an accepted even increment, big-endian fixed-width fields, and a dropped zig-zag step. Ten tests caught none of them: the bound-of-one, in-range, and distribution checks on bounded draws; the zero-maps-to-zero, distinct-halves, seed-response, and chunk-boundary checks on the mixer; and the repeatability, early-repeat, and increment-oddness checks on derivation. The chunk-boundary test does not test what its comment claims, because the length step separates its two inputs even when the ninth byte is dropped; the recorded vectors caught that mutation instead. The increment-oddness test asserts that a value with its low bit forced to one is odd. The suite runs in under 50 ms, so the cost is maintenance and false confidence, not time. The one surviving mutant, the off-by-one, differs from the reference rule on one draw in 2^32 and is not worth a test. A related coupling surfaced when the generator version moved to 2: the recorded encoding vector wrote `GeneratorVersion.Current` as a field, so a generator-version increment broke a vector that freezes the format of decision 0020. It now writes a literal; a recorded vector must contain only literals.

**Proposed solution.** Keep the recorded vectors, the two external anchors, the rejection-replay test, the assignment test, the even-increment and empty-range guards, the trailing-zero test, and the sibling-difference test; delete the ten. The four mixer tests went with finding 20 in `acdc063`; the six others remain: the three bounded-draw checks and the repeatability, early-repeat, and increment-oddness checks. Going forward, one test per frozen rule, with a mutation run rather than a test count as the evidence that the suite is load-bearing.

**Status:** Cleared 2026-09-07 by [decision 0024](decisions/0024-tests-comments-index-and-scaffolding-trimmed.md). The six tests are deleted and one structural test for decision 0023 is added, 69 Core tests in all; one test per frozen rule with a mutation run as evidence is the rule going forward.

### 25. Documentation restates itself and the generated index is unreadable

Affects: all documents, `tools/docs.cs`, `docs/index.md` (since deleted).

**Why it can go wrong.** A fact lives in the technical design, a decision record, the glossary, the XML comment of the code that implements it, and the progress document: PCG32 is named in 14 files, the 2,048 m cap in 8, the 20 Hz tick in 6, the PCG32 reference vector in 5. In `Shared/`, 343 of 779 lines are comments, more than the 332 lines of code. Every change to a frozen rule therefore touches four or five files, and the 366-line docs tool exists largely to catch the copies that were missed. The generated index repeats the README's curated list and adds a "referenced from" column that reaches 26 links in one row. About 2,100 lines of documentation and tooling govern about 450 lines of executable code.

**Proposed solution.** Keep the link, anchor, numbering, and generated-table checks. Drop the generated document index, or reduce it to the decision table without the "referenced from" column. In code, replace restated rules with a one-line pointer to the decision that fixes them, so the record is the single copy and the comment cannot go stale. Leave the progress document as the single status source it was designed to be.

**Status:** Cleared 2026-09-07 by [decision 0024](decisions/0024-tests-comments-index-and-scaffolding-trimmed.md). `docs/index.md` and its generator are removed; the eight `Shared/` files now carry 142 comment lines against 307 of code, down from 326; comments point to decisions rather than restating them.

### 26. The Benchmarks project is empty

Affects: `tests/SpaceExplorer.Benchmarks/`, [decision 0009](decisions/0009-solution-layout.md).

**Why it can go wrong.** The project restores BenchmarkDotNet and builds in continuous integration on both operating systems, and contains no benchmark. It is a placeholder for the M0a generator measurement. Small, but it is the pattern of finding 25 in code: structure ahead of the thing that needs it.

**Proposed solution.** Delete the project until the first benchmark exists; recreating it is fifteen lines and a solution entry. Or keep it and accept the cost knowingly. The point is to decide rather than carry it by default.

**Status:** Cleared 2026-09-07 by [decision 0024](decisions/0024-tests-comments-index-and-scaffolding-trimmed.md). The project, its solution entry, and the BenchmarkDotNet pin are deleted until the first benchmark exists.

### 27. Code-style enforcement is nominal

Affects: `Directory.Build.props`, `.editorconfig`, [decision 0014](decisions/0014-tooling-and-licence.md).

**Why it can go wrong.** `EnforceCodeStyleInBuild` is on and warnings are errors, which reads as "style is enforced". The editorconfig sets indentation only, so every IDE rule keeps its default severity of suggestion or silent and none reaches the build. Verified: a file with a non-readonly field assigned only in its constructor, a public field in the wrong case, and an unnecessary assignment builds with zero warnings; setting `dotnet_diagnostic.IDE0044.severity = warning` and the same for IDE0059 turns both into build errors. The flag gives assurance it does not deliver.

**Proposed solution.** Either set the handful of rules the project wants to warning in the editorconfig, so the flag does work, or remove the flag and rely on the compiler warnings that are already errors. A `dotnet format --verify-no-changes` step in continuous integration is the cheaper alternative for whitespace and usings.

**Status:** Cleared 2026-09-07 by [decision 0024](decisions/0024-tests-comments-index-and-scaffolding-trimmed.md). The flag is removed; compiler warnings as errors is the enforcement, and a style rule is adopted only when one earns it.

## Findings raised by the development-plan scan

Raised 2026-09-07 from a scan of the [development plan](development-plan.md) for gaps and misorders against the rest of the documents. Both are gaps, not foundational risks or process problems.

### 28. Biome rule sets and recovery challenges are undefined content

Affects: [core-release contents](development-plan.md#core-release-contents), [primitive registry](technical-design.md#primitive-registry-and-composition), [glossary](glossary.md).

**Why it can go wrong.** The core-release contents table requires "at least three biome rule sets" and "two recovery challenges" with the same weight as artifact families and regions. Artifact families are defined one paragraph later in the plan and backed by valuation and dealer-spread machinery; regions have a full contract in the glossary, the registry, and persistence. Neither biome rule set nor recovery challenge appears anywhere else: the registry's `category` field has `biome element` (a generated primitive, not an authored rule set) and `site component`, but no template vocabulary, persistence record, or content-gate coverage is defined for either as a countable content type, and `content/README.md`'s description of authored content names neither. The line has read this way since the plan's first commit, through twenty-five decisions and twenty-seven review findings, so nothing built so far — the template vocabulary, the registry, or the content gate — was designed with these two counts in mind. Whoever picks up content authoring for M0a or M0b has two required counts and nothing to implement them against.

**Proposed solution.** Define both as template vocabularies scoped to categories the registry already has, rather than inventing new content machinery: biome rule sets under `biome element`, recovery challenges under `site component` and constrained to the return-path type, tied to the "route and challenge" that artifact valuation already scores as recovery effort. Both then pass through the same generation, validation, and content-gate pipeline as every other template vocabulary, with no new persistence record or pipeline stage.

**Status:** Cleared 2026-09-07 by [decision 0026](decisions/0026-biome-rule-sets-and-recovery-challenges-are-template-vocabularies.md). Both terms added to the glossary and tied to the registry's `category` field.

### 29. The in-memory transport, commands, and handshake are untimed for M0a

Affects: [decision 0003](decisions/0003-network-topology-and-transport.md), [M0a](development-plan.md#milestones), [Multiplayer check](development-plan.md#verification-and-performance-targets), [M0a exit criteria](progress.md#m0a-exit-criteria).

**Why it can go wrong.** Decision 0003 mitigates the risk of discovering multiplayer problems only at M2 with "the in-memory transport tests from M0a onward," and the plan's own Multiplayer check requires running "the command and handshake suite over the in-memory transport from M0a onward." But every document that describes M0a's actual scope in detail — the plan's M0a row, its fifteen-criterion decomposition in progress.md, decision 0022's list of M0a work that may proceed alongside P0 without freezing a content format, and the technology decision's own walkthrough of "M0a first generates a random, constraint-valid primitive set..." — covers primitive-set work only and never schedules building the transport, commands, or handshake. No such code exists in the repository. The mitigation decision 0003 names has nothing to schedule it, so it does not happen unless someone notices the gap independently.

**Proposed solution.** Add the transport contract, core commands, and join handshake, tested over an in-memory implementation in continuous integration, as an M0a exit criterion. It freezes no content format, so it joins decision 0022's carve-out of work that proceeds alongside P0. The Godot/ENet adapter over this contract stays out of scope until M1/M2, per decision 0003.

**Status:** Cleared 2026-09-07 by [decision 0027](decisions/0027-in-memory-transport-commands-and-handshake-at-m0a.md). M0a gains a sixteenth exit criterion; progress.md's decomposition and the Multiplayer check now cite it.

## Finding raised by the surface-view review

Raised 2026-09-07 by checking the current Godot preview and the planned world model against the owner's requirement for a realistic view from a planet's surface.

### 30. Surface sky is decorative rather than astronomically derived

Affects: [destination planetary system](concept.md#3-the-destination-planetary-system), [technical design](technical-design.md), [milestones and verification](development-plan.md#milestones), `src/SpaceExplorer.Game/Main.tscn`, and `src/SpaceExplorer.Game/assets/environment/airless_sky.gdshader`.

**Why it can go wrong.** The design generates stars, orbits, planet properties, and atmospheres but never transforms that data into the sky seen by an observer. The current preview has one fixed directional light, a procedural decorative star field, and two hard-coded coloured planet discs explicitly labelled as non-simulated. It has no epoch, saved astronomical time, planetary spin, surface latitude/longitude, orbit propagation, horizon test, phase, angular-size calculation, eclipse/occultation test, or atmospheric visibility model. Completing the existing milestones could therefore leave generated orbits in data while showing an unrelated sky.

**Proposed solution.** Make an observer-centred celestial solution part of M0b and its Godot integration part of M1. Generate a pinned epoch, saved simulation time, bounded orbital parameters, body dimensions, planet spin state, background-star data, atmosphere model, and region-to-planet mapping. At a requested time, deterministically transform body positions through the planet-fixed and local horizon frames and derive directions, distances, angular sizes, phases, visibility, eclipses, occultations, and lighting from one result. Use bounded analytic propagation rather than full n-body simulation, and add cross-platform fixed reference cases.

**Status:** Cleared 2026-09-07 by [decision 0032](decisions/0032-astronomically-consistent-surface-sky.md). Requirement R12, the observer-centred technical contract, M0b/M1 work, a dedicated verification check, progress status, and glossary terms now make the feature explicit. The existing preview remains a placeholder and no implementation is claimed.

## Findings raised by the primitive storage inspection

Raised 2026-09-07 by inspecting the primitive storage and handling plan, [decision 0031](decisions/0031-primitive-complete-composition-and-storage.md) with the [registry](technical-design.md#primitive-registry-and-composition), [sets](technical-design.md#primitive-sets-and-compact-references), and [persistence](technical-design.md#persistence-and-compatibility) sections it was applied to, the M0a and M0b rows of the [development plan](development-plan.md#milestones), and the immediate step in [progress.md](progress.md#m0a-exit-criteria), against the decisions it builds on and the code under `src/SpaceExplorer.Core/Registry/`. Findings 31 to 34 each fix a field of the `PrimitiveDefinition`, `SetSpecification`, or `SetManifest` record that step proposes to freeze, so they are due before that record rather than after it; 35 is due with the first publisher and 36 with the first world package.

### 31. A generated definition can never receive a second revision

Affects: [decision 0031](decisions/0031-primitive-complete-composition-and-storage.md) (definitions and instances), the `content_hash` row of the [registry](technical-design.md#primitive-registry-and-composition), the Pack, Pack revision, and Allocation ledger entries of the [glossary](glossary.md#content-identity), the Compatibility check in the [development plan](development-plan.md#verification-and-performance-targets), and `ReferenceTable.Create`.

**Why it can go wrong.** Decision 0031 rests the exact-reference contract on one event, "a compatible revision of the same conceptual definition retains its ID and receives a new hash", so a `PrimitiveId` alone cannot name usable content and every table entry and dependency carries a 32-byte hash. But every runtime definition in the core release is generated ([decision 0002](decisions/0002-primitive-set-content-gate.md)), a generated pack is published exactly once per run with IDs 1..n and no ledger ([decision 0006](decisions/0006-pack-identity-and-allocation.md)), and worlds draw only from saved sets. A generated definition therefore lives in a pack that cannot gain a second revision; a changed template or asset flows through a new specification, a new `pack_id`, and new IDs. The event the contract guards against cannot occur, the Compatibility check's "revise a primitive without changing its stable ID" cannot be run, and the verification row "two revisions of one ID" has nothing to exercise. Whether one reference table may hold two revisions of one `PrimitiveId`, which [decision 0029](decisions/0029-primitive-identity-handle-and-cross-package-set-operations-contract.md) rejected as a duplicate identity, is undecided for the table the immediate step must rewrite.

**Proposed solution.** Choose one reading and record it. Either revisions are a property of authored template packs only: a generated definition's hash is fixed for life, exact references keep the hash as a cross-machine check that reproduction matched rather than as a disambiguator, a reference table rejects two entries with one `PrimitiveId`, and the Compatibility check becomes "publish a new set from revised templates; the old world keeps its old pack". Or generated packs gain revisions: a re-publication keeps its `pack_id`, records the manifest it supersedes, and needs the ledger-like record decision 0006 says generated packs do not have. The first trims rather than scaffolds ([decision 0024](decisions/0024-tests-comments-index-and-scaffolding-trimmed.md)) and is recommended.

**Status:** Open.

### 32. The storage policy sits inside the hashed definition that M0b's measurement will change

Affects: [decision 0031](decisions/0031-primitive-complete-composition-and-storage.md) (storage and caching), the `storage_policy` row of the [registry](technical-design.md#primitive-registry-and-composition), the World manifest row under [persistence](technical-design.md#persistence-and-compatibility), the Storage policy entry of the [glossary](glossary.md#content-identity), and the M0a and M0b rows and Resource-use check of the [development plan](development-plan.md#milestones).

**Why it can go wrong.** Each definition "pins one of three policies selected by cross-platform determinism and performance measurements", the measurements are M0b work, and M0a publishes the definitions first. A definition is a canonical record, so every field enters its content hash ([decision 0020](decisions/0020-canonical-encoding-and-content-hash.md)); the measured policy would give every M0a definition a new hash, which under finding 31 means a new pack, so M0b's measurement re-mints every identity M0a published. The pin is also stated twice, in the definition and in the world manifest's "storage policies", and the Resource-use check pins "per category" while the registry table pins "by the definition". The policy is a deployment choice that depends on the machine and the measurement, not a property of the content.

**Proposed solution.** The category registry lists the permitted policies, as the glossary already says; the world manifest pins the policy chosen per category at generation time from the versioned measured configuration, and the set manifest does the same for any set-owned payload; the definition record carries no policy field. Measurement then changes no definition hash, and an old world keeps the policy under which it was accepted. This supersedes the "each definition pins" clause of decision 0031 and needs a record.

**Status:** Open.

### 33. The generator implementation a regenerate primitive pins has no identity

Affects: [decision 0031](decisions/0031-primitive-complete-composition-and-storage.md) (storage and caching; consequences), the `dependencies` row of the [registry](technical-design.md#primitive-registry-and-composition), the compatibility paragraph and World manifest row under [persistence](technical-design.md#persistence-and-compatibility), the Generator version entry of the [glossary](glossary.md#content-identity), the join handshake under [multiplayer](technical-design.md#multiplayer-and-trust-boundary), and [decision 0028](decisions/0028-in-memory-transport-command-and-handshake-contract.md).

**Why it can go wrong.** A `regenerate` or `hybrid` primitive "pins its generator implementation, which remains available while anything references it", exports carry the "generator dependency closure", and a missing retained generator revision must fail explicitly. Nothing names that implementation. The registry's `dependencies` admits exact primitive revisions and asset hashes only; the only generator identity in the project is `GeneratorVersion`, one integer for the whole core that covers the random foundation alone ([decision 0021](decisions/0021-sha256-stream-derivation.md)). If that integer is the pin, any change to any regeneration algorithm bumps it and the shipped game must retain every complete generator side by side; if the pin is per category, its identifier and where it is recorded are unspecified. Packages are data-only, so an export can only name an implementation the binary must contain, and the join handshake compares protocol versions only, so a guest whose build lacks a named implementation finds out after transfer rather than before.

**Proposed solution.** Per-category generator revision identifiers in the category registry, in stream-path form such as `terrain-heightfield/1`, pinned by the set and world manifests and admitted by the `dependencies` row; `GeneratorVersion` stays the random foundation's version; retention is per revision identifier, not per whole generator; the join handshake advertises the retained identifiers so a mismatch fails before any package moves. The M0a record that fixes the category vocabulary can carry this.

**Status:** Open.

### 34. The category registry has no home, no revision identity, and no stated role in decoding

Affects: the `category` and `parameters` rows of the [registry](technical-design.md#primitive-registry-and-composition), the schema-driven reader model under [sets](technical-design.md#primitive-sets-and-compact-references), the Primitive set row under [persistence](technical-design.md#persistence-and-compatibility), the Primitive-set foundation check in the [development plan](development-plan.md#verification-and-performance-targets), and the immediate step in [progress.md](progress.md#m0a-exit-criteria).

**Why it can go wrong.** The canonical format is schema-driven, not self-describing: a reader must know a record's field order for the version the bytes declare. A definition's parameter payload is laid out by its category's parameter schema, so no reader can decode a definition without the category-registry revision that defined it. No document says whether the registry is code or a data file under `content/`, how its revision is named, or which record carries that revision: the definition's domain label, the pack manifest, or both. "Unsupported category registries fail explicitly" cannot be tested until this is fixed, and the immediate step proposes to write `PrimitiveDefinition` first.

**Proposed solution.** The category registry is a canonical data record with an integer revision in its domain label, `category-registry/N`, following the `set-specification/1` pattern of [decision 0020](decisions/0020-canonical-encoding-and-content-hash.md); each pack manifest pins the revision and hash it was generated under; each definition record's domain label carries the registry revision so the reader selects the parameter schema before allocating; the game lists the revisions it supports and refuses others with a compatibility error. The M0a record that fixes the category vocabulary can carry this.

**Status:** Open.

### 35. Two set manifests can install under one pack identifier

Affects: [decision 0006](decisions/0006-pack-identity-and-allocation.md), the pack identifier clause of [decision 0020](decisions/0020-canonical-encoding-and-content-hash.md), the publication paragraph under [storage location and layout](technical-design.md#storage-location-and-layout), the Primitive set entry of the [glossary](glossary.md#content-identity), and the Primitive-set foundation and Durability checks in the [development plan](development-plan.md#verification-and-performance-targets).

**Why it can go wrong.** A generated `pack_id` is the leading 16 bytes of its specification hash so that reproducing a specification reproduces its pack. A set is also identified by its manifest hash, which covers the accepted definitions and the validation result. Publication installs `packs/<pack-id>/<content-hash>.bin` "without overwriting different content" and says nothing about a second manifest with a different hash under the same `pack_id` directory. A non-deterministic generator, a platform difference, or a retained old build therefore installs two sets under one `pack_id` silently; `PrimitiveId` (pack, 7) then names two definitions, and a lookup by `pack_id` has no rule for which manifest it means. The hash in every exact reference catches the mismatch at use, far from the publication where the evidence is.

**Proposed solution.** Publishing a manifest whose `pack_id` directory already holds a manifest with a different hash is a reproduction failure: reject it, report both hashes, and enforce one manifest per `pack_id` in the SQLite index. Add the case to the Primitive-set foundation check under "ID and revision boundaries/collisions" and to the Durability check.

**Status:** Open.

### 36. The shared content-addressed stores have no reference index and no deletion rule

Affects: the `packs/` and `blobs/` entries under [storage location and layout](technical-design.md#storage-location-and-layout), the record table and archive-removal paragraph under [persistence](technical-design.md#persistence-and-compatibility), the packages paragraph of [decision 0031](decisions/0031-primitive-complete-composition-and-storage.md), and the Durability and Resource-use checks in the [development plan](development-plan.md#verification-and-performance-targets).

**Why it can go wrong.** Decision 0031 moved materialized payloads and assets into `blobs/`, shared by every campaign alongside `packs/`, and allows deletion "only when no campaign, world, export, or retained backup depends on it". Nothing records the dependants: the persistence table has no reference index, `campaign.db` is per campaign so no single database sees every campaign, and `backups/<branch-id>/` copies are dependants too. Either nothing is ever deleted, so an abandoned campaign's world payloads persist and low-disk recovery can evict only caches, or removing a campaign deletes blobs another campaign still references. Neither check exercises the case.

**Proposed solution.** A data-root-level reference index, maintained inside the publish-last step of the same protocol that installs a package, recording every campaign, world, export, and backup that depends on each package or blob. Deletion is an explicit user-confirmed sweep of zero-reference content and never runs implicitly. Durability gains "remove a campaign that shares packs with another; the survivor still loads", and Resource-use gains "unreferenced content is reported, not deleted, until confirmed".

**Status:** Open.

## Findings raised by the planetary-model sufficiency check

Raised 2026-09-07 by asking whether the structures of [decision 0031](decisions/0031-primitive-complete-composition-and-storage.md), which must carry the solar system, every planet surface, and every artifact as one composition graph, are sufficient for the realistic planetary model that [decision 0032](decisions/0032-astronomically-consistent-surface-sky.md) and requirement R12 demand. Identity, exact revisions, instance paths, typed connectors, grammar, aggregation rules, the closed parameter schema, storage policies, chunks, overlays, and the material, texture, collision, and behaviour split were found sufficient for artifacts, sites, life, caves, and atmosphere selection, subject to findings 31 to 36. The five findings below are about the shape of the graph, not its storage. Every number derives from constants fixed in [decision 0010](decisions/0010-units-coordinates-and-region-bounds.md), [decision 0017](decisions/0017-region-extent-cap-and-storage-derivation.md), and [decision 0032](decisions/0032-astronomically-consistent-surface-sky.md), plus a 2 m eye height, which is an assumption named where it is used.

### 37. One transform type cannot span the frame hierarchy

Affects: the transform and attachment-transform clauses of [decision 0031](decisions/0031-primitive-complete-composition-and-storage.md), the `connectors` and `parameters` rows of the [registry](technical-design.md#primitive-registry-and-composition), the [units table](technical-design.md#units-coordinates-and-bounds), and [astronomical state](technical-design.md#astronomical-state-and-surface-sky).

**Why it can go wrong.** Every node carries a "transform" and every connector an "attachment transform or spatial area", and the only position type fixed so far is int32 fixed-point at 1/256 m, whose range is ±8,388,608 m. That covers a region and, barely, an Earth-sized planet-fixed frame; it cannot place a planet in the system frame, where 1 AU is 1.496 × 10^11 m, nor a point on the surface of a body larger than 8,389 km in radius, which excludes every gas giant. The units table's "Planetary values: integers in documented units defined with the numeric tables" defers the question rather than answering it. If the first `PrimitiveDefinition` record freezes one transform type, the system and planet domains inherit a type that cannot hold their values.

**Proposed solution.** The connector kind decides the transform type: orbital elements plus epoch for an orbit connector, latitude, longitude, height, and heading for a surface anchor, and a rigid fixed-point transform only inside a region or an artifact. The units table gains a row per frame, system, planet-fixed, region-local, and artifact-local, each with its integer width, unit, and scale, so that no frame reuses another's position type by default. The category registry record of the M0a immediate step is where this lands.

**Status:** Open.

### 38. Planet-level fields have no spherical form and the flat region bounds the explorable body radius

Affects: the `parameters` row of the [registry](technical-design.md#primitive-registry-and-composition) (bounded grids), [astronomical state](technical-design.md#astronomical-state-and-surface-sky) (region-to-planet mapping), [region encoding](technical-design.md#region-encoding-and-compression), the Region entry of the [glossary](glossary.md#worlds-and-destinations), and the Worlds row of the [core-release contents](development-plan.md#core-release-contents).

**Why it can go wrong.** Bounded grids are rectangular. Global relief, climate, insolation by latitude, and the ocean datum that every region of one planet must agree on need a spherical parameterization, and none is named. Decision 0032 requires "a region's stable mapping to the planet-fixed frame" without fixing its form. A region is a flat patch of at most 2,048 m per axis, and that flatness bounds which bodies it can sit on:

| Body radius | Horizon at 2 m eye height | Curvature drop over the 1,024 m half-region | Gravity tilt over the half-region |
| --- | --- | --- | --- |
| 6,371 km | 5.0 km | 8 cm | 0.009° |
| 1,737 km | 2.6 km | 30 cm | 0.034° |
| 262 km | 1.02 km | 2.0 m | 0.22° |
| 100 km | 0.63 km | 5.2 m | 0.59° |

Horizon distance is `sqrt(2 R h)`, curvature drop is `x² / 2R`, and gravity tilt is `x / R` with `x` the half-extent. Below about 262 km the true horizon lies inside a maximal region, so a flat tangent plane contradicts requirement R12's horizon visibility, and the gravity direction the host simulates ([decision 0013](decisions/0013-host-simulates-hazards.md)) is visibly wrong at the region edge. Nothing states a minimum explorable radius or a curvature rule.

**Proposed solution.** Name one spherical parameterization for planet-level bounded grids, such as a cube-sphere or equirectangular grid with a declared resolution, as a parameter type of the planet domain. Fix the region-to-planet mapping as a tangent plane at a latitude, longitude, height, and heading. Derive the minimum explorable body radius from the region cap and the horizon rule and put it in the stellar grammar's constraints, so the generator never lands a region on a body the flat model cannot represent; curved regions stay outside the core release.

**Status:** Open.

### 39. Nothing is defined beyond the region edge

Affects: requirement R12 in [requirements](requirements.md#design-constraints-stated-by-the-owner), [astronomical state](technical-design.md#astronomical-state-and-surface-sky), the Region entry of the [glossary](glossary.md#worlds-and-destinations), the Exploration row of the [core-release contents](development-plan.md#core-release-contents), and the Surface-sky consistency check in the [development plan](development-plan.md#verification-and-performance-targets).

**Why it can go wrong.** On an Earth-sized body the horizon at 2 m eye height is 5.0 km away and a maximal region ends 1,024 m from its centre. Requirement R12 asks that "standing on a planet must reveal the generated system coherently", yet no structure supplies terrain between the region edge and the horizon, and no rule says what bounds traversal there: a wall, a fog, a cliff, or distant ground. The sky is derived from pinned data precisely so that it cannot be decorative; the ground beyond the region has no such source and would be authored ad hoc.

**Proposed solution.** The planet-level relief field of finding 38 is the non-authoritative source of far-field terrain, sampled by the Godot adapter as a cache the same way the sky is derived from the celestial solution, so the far field agrees with the region's placement, the planet's radius, and its horizon. The region boundary is a defined rule of the region primitive's traversal primitive, not of the renderer. The Surface-sky consistency check gains a case that the far-field horizon and the celestial horizon agree.

**Status:** Open.

### 40. Physical derivation rules are missing

Affects: the aggregation-rule clause of [decision 0031](decisions/0031-primitive-complete-composition-and-storage.md), [sets and compact references](technical-design.md#primitive-sets-and-compact-references), [astronomical state](technical-design.md#astronomical-state-and-surface-sky), the Generation lifecycle's step 2 under [technical design](technical-design.md#generation-lifecycle), and [finding 33](#33-the-generator-implementation-a-regenerate-primitive-pins-has-no-identity).

**Why it can go wrong.** Aggregation rules are sum, weighted average, minimum, maximum, bounded composition, and override. A realistic system needs surface gravity from mass and radius, orbital period from semi-major axis and central mass, equilibrium temperature from luminosity, distance, and albedo, pressure against altitude, atmosphere retention against escape velocity, tidal locking, and orbit-spacing stability. These are neither parameters nor aggregation rules nor named generator revisions, so a generator would either store independent values that can disagree, gravity that does not follow from mass and radius, or compute them in code that finding 33 shows has no identity. Authoritative floating point is forbidden, so each rule needs an integer or fixed-point form with roots and powers, and periodic motion expressed as an angular rate accumulates drift over years of simulation time at any fixed-point resolution.

**Proposed solution.** A class of versioned derivation-rule primitives beside the aggregation rules, each an integer or fixed-point implementation with a named revision in the category registry, so finding 33's identifiers cover them. Decide per rule whether the derived value is stored and validated against the rule at load or computed on demand. Periodic motion is stored as an integer period and a phase at the epoch and evaluated by integer modulo, never by integrating a rate. Binary stars and moons use the same hierarchy: an orbit connector's parent is a body or an explicit non-rendered barycentre node, which the explicit-absence rule already permits.

**Status:** Open.

### 41. The node budget assumes no derived instances

Affects: the scale clause of [decision 0006](decisions/0006-pack-identity-and-allocation.md), the "expected scale" sentence under [sets and compact references](technical-design.md#primitive-sets-and-compact-references), the Composition graph/chunk and Mutable state rows under [persistence](technical-design.md#persistence-and-compatibility), the Primitive instance and Primitive state overlay entries of the [glossary](glossary.md#content-identity), and the overlay-target invariant under [save integrity](technical-design.md#save-integrity).

**Why it can go wrong.** Decision 0006 expects "thousands of composition nodes per world". A maximal region is 2,048 m × 2,048 m, 4,194,304 m². One rock or plant per 100 m² is 41,943 items in one region; one per 10 m² is 419,430. Either the budget is off by two or three orders of magnitude, or a scatter recipe must yield instances that are addressable by path but not stored as nodes, and the documents say neither. Overlays are keyed by instance path and load-time verification requires that "each overlay targets a compatible primitive instance"; collecting one rock from a scatter needs a path no explicit node carries, so today that pickup either cannot be recorded or fails verification.

**Proposed solution.** Define derived instances: a node whose policy is `regenerate` or `hybrid` may produce instances addressed by extending its path, such as `.../scatter/41/item/1234`, that are not stored as nodes, draw from the parent's stream only, and count against a per-node derived budget rather than the graph's node limit. Overlays may target a derived path, and the load-time check resolves it by regenerating the parent under its pinned revision. Restate the expected scale as explicit nodes plus derived instances, measured separately at M0b.

**Status:** Open.
