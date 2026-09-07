# Design review: planted basics

Reviewed 2026-09-07. This review inspects the documentation-stage repository for foundational decisions that would be costly to reverse once implementation starts, plus smaller gaps and inconsistencies. Each finding states the failure mechanism, a proposed solution, and a status. Clearing a finding means recording the decision in [decisions/](decisions/README.md) and applying the change to the affected documents; the status row then links to both.

All seventeen findings were cleared on 2026-09-07; the status table links each to its decision record or to the document it changed. Checks performed for this review: every internal link and anchor in the documents resolves; all 54 cited external URLs respond, except Epic's Unreal Engine licence page, which answers automated requests with HTTP 403 and must be opened in a browser; the worked valuation and byte-size examples are arithmetically correct. The scaffold of [decision 0016](decisions/0016-platform-confirmed-and-toolchain-pinned.md) builds and its tests pass; the design statements themselves are not yet tested against generator code. Terms used below are defined in the [glossary](glossary.md).

## Status

| # | Finding | Class | Status |
| --- | --- | --- | --- |
| 1 | [Terrain authority is left as an "or"](#1-terrain-authority-is-left-as-an-or) | Foundational | Cleared, [decision 0001](decisions/0001-materialize-authoritative-terrain.md) |
| 2 | [Random primitive sets are untested as content](#2-random-primitive-sets-are-untested-as-content) | Foundational | Cleared, [decision 0002](decisions/0002-primitive-set-content-gate.md) |
| 3 | [Transport selected before connectivity; replication coupling](#3-transport-selected-before-connectivity-replication-coupling) | Foundational | Cleared, [decision 0003](decisions/0003-network-topology-and-transport.md) |
| 4 | [Long journeys lost their rationale](#4-long-journeys-lost-their-rationale) | Foundational | Cleared, [decision 0004](decisions/0004-preparation-is-real-generation-time.md) |
| 5 | [Identity scheme details](#5-identity-scheme-details) | Foundational | Cleared, [decision 0006](decisions/0006-pack-identity-and-allocation.md) |
| 6 | [Uniqueness by rejection](#6-uniqueness-by-rejection) | Foundational | Cleared, [decision 0007](decisions/0007-artifact-collision-substitution-and-campaign-salt.md) |
| 7 | [Random stream derivation](#7-random-stream-derivation) | Foundational | Cleared, [decision 0008](decisions/0008-random-stream-derivation.md) |
| 8 | [Folder layout versus assembly boundaries](#8-folder-layout-versus-assembly-boundaries) | Foundational | Cleared, [decision 0009](decisions/0009-solution-layout.md) |
| 9 | [Units, coordinates, and region bounds](#9-units-coordinates-and-region-bounds) | Foundational | Cleared, [decision 0010](decisions/0010-units-coordinates-and-region-bounds.md) |
| 10 | [Requirements provenance and repeated policy text](#10-requirements-provenance-and-repeated-policy-text) | Gap | Cleared, [decision 0011](decisions/0011-requirements-single-source.md) |
| 11 | [Concept wording that will become requirements](#11-concept-wording-that-will-become-requirements) | Inconsistency | Cleared, [decision 0012](decisions/0012-core-travel-model.md) |
| 12 | [Travel topology](#12-travel-topology) | Gap | Cleared, [decision 0012](decisions/0012-core-travel-model.md) |
| 13 | [Guest hazard authority](#13-guest-hazard-authority) | Gap | Cleared, [decision 0013](decisions/0013-host-simulates-hazards.md) |
| 14 | [Tooling predates the stack decision](#14-tooling-predates-the-stack-decision) | Gap | Cleared, [decision 0014](decisions/0014-tooling-and-licence.md) |
| 15 | [Infrastructure-first plan versus fun-first assessment](#15-infrastructure-first-plan-versus-fun-first-assessment) | Process | Cleared, [decision 0015](decisions/0015-greybox-prototype.md) |
| 16 | [Missing comparables](#16-missing-comparables) | Relevance | Cleared, applied to the [assessment](assessment.md#similar-projects) |
| 17 | [Alternatives table omits the mainstream engines](#17-alternatives-table-omits-the-mainstream-engines) | Relevance | Cleared, applied to the [technology decision](technology-stack.md#alternatives-considered) |

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

**Status:** Cleared 2026-09-07 by [decision 0001](decisions/0001-materialize-authoritative-terrain.md). Materialize at acceptance; cross-OS terrain match is a measured target at M0b. Applied to the technical design and development plan.

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

**Status:** Cleared 2026-09-07 by [decision 0010](decisions/0010-units-coordinates-and-region-bounds.md). All proposed constants adopted: int32 fixed-point at 1/256 m, region-local frame, 4,096 m cap, 2 m cells with int16 heights at 1/16 m, 20 Hz tick. Applied to technical design, development plan, and glossary.

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
