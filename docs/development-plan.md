# Space Explorer: proposed development plan

Reviewed 2026-09-07. The project contains documentation and source-directory placeholders, with no application code. This plan translates the [concept](concept.md) into a proposed prototype and release scope. Added defaults are recommendations, not implemented features or confirmed production commitments. The [assessment](assessment.md) explains the reasoning; the [technical design](technical-design.md) specifies data and behavior contracts; the [technology decision](technology-stack.md) selects the working implementation stack.

## Scope and working assumptions

Linux is the primary development environment; Linux and Windows are the two required native release platforms. Use Godot 4 .NET with C#, an engine-independent generator, and SQLite persistence as the working stack. The 3D platform must be free to use and provide an accessible development workflow. Routine coding, generation, tests, profiling, and editor use must work on Linux without Windows-only tooling. Validate Windows builds on native Windows machines/runners at milestone and release gates. Linux support must not depend on Wine or Proton. Python is not required. Initial validation targets x86_64 and the Compatibility renderer, subject to measured hardware and presentation requirements; exact versions are pinned at implementation start.

Start with artifact expeditions, manageable environmental hazards, and private cooperation. Use 3D surface movement in bounded landing regions, with map-based or transitional travel between regions. Full planetary surfaces, seamless space-to-ground flight, orbital simulation, terrain deformation, combat, construction, industrial crafting, and public markets are outside the core release.

Use a home anchor for trading and progression. Distance is measured from that anchor so repeated short hops cannot bypass access tiers. Destination controls include distance, environment preference, survey focus, recorded seed, and return-visit selection. Preview cost, equipment requirements, hazards, cargo capacity, and preparation/resource estimates without revealing artifact locations or prices.

Proposed timing is short trips first, with optional days-long preparation evaluated later. Proposed pricing uses predefined reference generation/recovery effort rather than elapsed wall-clock time. Both refine the original concept and remain explicit validation questions.

## Core-release contents

| Area | Minimum usable result |
| --- | --- |
| Platforms | Linux-primary development with portable tooling; native Linux and Windows builds tested on their respective OSes. Confirm initial CPU architectures, supported OS versions/distributions, and minimum graphics requirements during the prototype. |
| Primitive foundation | Random, validated primitive sets generated and durably stored before worlds; permanent pack-local unsigned 32-bit IDs, compact references, pinned dependencies, and expansion without rewriting saved sets. |
| Navigation | One ship, one home anchor, two unlockable distance tiers, destination preferences, recorded seeds, and return visits. |
| Worlds | One stellar template family; every accepted destination has at least two explorable planets and one observable life-bearing environment, which may overlap. At least one bounded region per explorable planet. |
| Exploration | Surface movement, orbital/region map, scanner clues, suit reserves, cargo limits, and emergency recovery. |
| Content | At least three biome rule sets, three artifact families, two recovery challenges, and enough compatible compositions for uniqueness and variety checks. |
| Economy | Catalogue, collection storage, NPC buying/selling, supplies, an equipment improvement, and second-tier travel access. |
| Persistence | Save/load, interrupted-generation resume, atomic transactions, recoverable backups, and an update-compatibility demonstration. |
| Multiplayer | A host and one invited guest on separate internet connections, with shared discoveries and no duplicate pickups or sales. |
| Usability | Remappable keyboard/mouse controls, scalable text, hazard cues beyond color alone, and skippable or reduced-motion wormhole effects. |

Artifact families begin with minerals, biological specimens, and manufactured relics. NPC dealers at the home anchor buy and sell individual instances with fixed spreads that prevent profitable immediate resale. Supplies, travel, and equipment are credit sinks; access upgrades are permanent. Higher tiers increase reward ranges and recovery demands, without making every common find outperform every lower-tier artifact.

The proposed emergency rule returns a player with exhausted suit reserves to the ship and leaves carried artifacts in a recoverable field cache. Banked finds remain safe. Provide a basic suit, a return route, and a zero-cost starter expedition or recovery contract so failure cannot strand a campaign. Generation failures and cancellations refund uncompleted reservations once; cancellation preserves the specification rather than rerolling it.

The host owns the campaign; guest inventory, shared collection, and credit balance stay in that save. Spending and selling require host permission. Private campaign branches do not exchange inventories. The [technical design](technical-design.md) defines persistence and reconnect details.

## Milestones

| Milestone | Deliverable and exit criterion |
| --- | --- |
| M0a: primitive-set foundation | First implementation step: a C# core and CLI generate random, constraint-valid primitive sets from authored templates; allocate IDs; validate, store, reload, query, and combine sets without rerolling. Verify canonical hashes on Linux and Windows, missing/corrupt dependencies, ID boundaries/collisions, bounded failures, interrupted publication, and expansion compatibility. Add a minimal Godot viewer and native packaged smoke tests; pin tool versions and record reference machines. |
| M0b: world generation and save prototype | Compose worlds and artifacts from stored M0a sets. Reproduce fixed-seed data; validate required planet/life and route constraints; bound failures; resume interrupted generation; preserve old worlds when sets expand. Measure one representative region and persistence on both platforms; demonstrate a minimal mixed-OS host/guest connection. |
| M1: solo playable expedition | Complete destination selection, travel, landing, recovery, return, sale, second-tier unlocking, and revisit. Removed items remain removed after reload. Testers can explain a recovery or collection choice that changed their outcome. |
| M2: connected core release | Two PCs on different networks share a saved destination; simultaneous pickups and retried sales commit once; disconnect/rejoin restores consistent state. Meet the verification criteria below. |
| M3: expansion experiments | Evaluate days-long preparation, more regions/primitives, authoring tools, and larger groups after the core loop and compatibility contract work. |

M0 means M0a followed by M0b; world generation must not substitute for the primitive-set foundation. M1 is an internal playable slice. It does not satisfy the original internet-sharing requirement alone; M2 is the proposed core release.

Long-mode experiments require resumable work, bounded resources, progress visibility, and continued access to prepared destinations. Closing the game pauses computation. A worker that runs while the game is closed is a separate opt-in feature; computation cannot continue on a powered-off PC.

## Verification and performance targets

These are proposed targets, not benchmark results. M0 records the reference PC, OS, build, content pack, and seed suite. Measure useful playable content separately from total generation cost.

| Check | Acceptance target |
| --- | --- |
| Native platform support | Build and smoke-test on Linux and Windows. On clean target environments, launch the distributed game and verify graphics/input, primitive-set generation and loading, local saves, and hosting/joining without requiring an editor or development SDK. Include a mixed Linux/Windows co-op test and cross-platform save compatibility. |
| Primitive-set foundation | Fixed seed/template/algorithm versions and base pack/allocation state produce matching canonical set data across both OSes and worker counts. Saved accepted definitions reload without regeneration; corrupt/missing dependencies fail explicitly. Test zero/reserved IDs, values above signed-32 range, `uint.MaxValue`, overflow rejection, duplicate allocation, and mixed-pack lookup. Measure packed-ID bytes separately from manifests/assets, plus lookup and set-operation costs. |
| Determinism | Matching authoritative descriptors, recipes, and prices for identical specifications across supported builds, thread counts, and interrupted/resumed runs. |
| Destination validity | At least 1,000 seeds per initial tier plus invalid/boundary inputs. Every accepted world satisfies the two-planet/life, reachable-site, and return-route rules. Retries terminate; record fallback frequency. |
| Compatibility | Save a world, expand the registry, and revisit visited and unvisited regions; identities, geography, artifacts, and prices match the old world. |
| Durability | Interrupt generation, pickup, recovery, and sale around commit boundaries; reload gives consistent state without duplicate items, lost committed credit, or double refunds. |
| Multiplayer | Test separate internet connections, late join, simultaneous pickup, repeated commands, permissions, version mismatch, packet delay, disconnect, and rejoin. A LAN-only demonstration is insufficient. |
| Responsiveness | Target a ready starter destination within 60 seconds, surface exploration at 30 FPS or better at 1080p, and a visible pause/cancel response within one second on the reference PC. |
| Resource use | Over 20 expeditions, record peak RAM, preparation time, package/cache size, and bytes per region/artifact. Demonstrate safe eviction and low-disk recovery; derive shipping limits from measurements. |
| Artifact variety | Review generated samples for duplicate recipes, near-identical silhouettes, unusable shapes, and biome repetition. Players distinguish families and explain collection choices. |
| Gameplay/economy | Observe short expeditions, second-tier unlocking, and zero-credit recovery. Faster hardware, idling, seed re-entry, reloads, reconnects, and immediate resale create no extra value. |

A short expedition should offer a full recovery-and-sale loop in approximately 15–30 minutes once controls are familiar. Before expanding scope, observe at least five relevant players completing the solo slice; record their choices, what they found distinctive, and whether they choose another trip. This is formative feedback, not a market-size or retention estimate.

Seed suites provide regression coverage rather than proof for every possible world. Runtime constraints and bounded fallback remain required. Add regression cases for discovered generator and transaction failures.

## Decisions before production commitments

| Decision | How to resolve it |
| --- | --- |
| Long-wait role | Compare short preparation and a resumable long-mode mockup. Mandatory waits would be a material alternative to the proposed optional mode. |
| Pricing and progression | Simulate repeated expeditions, collection-versus-sale choices, reward budgets, and unlock pacing. If actual elapsed-time pricing is retained, define caps, active-time measurement, and attribution of shared effort. |
| Stack validation, OS versions, hardware | Godot .NET/C# and SQLite are selected working defaults, not measured results. M0a/M0b validate native packaging, one generated region, a saved artifact, and a networking proof on both OSes. Confirm initial x86_64/Compatibility scope, supported distributions/Windows versions, graphics requirements, and reference machines before publishing minimum requirements. Revisit the stack only if evidence exposes a material mismatch. |
| Numeric content and suit limits | Measure generation/storage costs, then tune region counts, artifact counts, effort scores, prices, suit envelopes, and site difficulty. Store numeric tables with explicit versions. |
| Camera and art direction | Use M1 navigation and artifact-distinguishability feedback to settle presentation. |
| Internet hosting | Godot ENet is the initial transport. Before M2, demonstrate a relay/platform or directly reachable host across separate networks, including mixed-OS play in both host directions. Decide whether direct hosting meets usability needs; document account/service dependencies, costs, and unavailable-service behavior. No relay is selected yet; solo play remains offline. |
| Schedule and budget | The project owner supplies team capacity and budget; estimate milestones from prototype results. No delivery date is implied by this document. |

The [assessment](assessment.md) records the evidence, assumptions, and positioning questions that should inform these decisions. The [technology decision register](technology-stack.md#gap-filling-decisions-for-confirmation) lists the added platform and library defaults for user confirmation.
