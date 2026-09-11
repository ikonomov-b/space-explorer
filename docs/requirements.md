# Requirements

Reviewed 2026-09-11. Owner-stated constraints that sit outside the game idea. The concept of record is [concept.md](concept.md); where a refinement was adopted, the concept keeps the original idea inline and [decisions/](decisions/README.md) records what was chosen. Working defaults chosen by the documents, such as the engine, database, renderer, and CPU architecture, are not requirements and live in the [technology decision](technology-stack.md).

## Platform policy

This section is the single source for the platform policy. Other documents state it in one sentence and link here.

- **R1. Linux is the primary development environment.** The full daily loop works on Linux without any Windows-only tool, IDE, asset-conversion step, build script, or service: editing C#, generating and inspecting primitive sets, running tests, profiling, using the editor, and producing a Linux build.
- **R2. Native Linux and Windows are the two required release platforms, with equal status.** Windows builds are validated on native Windows machines or runners at milestone and release gates, and a gate is incomplete until the Windows checks have run. Cross-exporting from Linux does not replace execution on Windows.
- **R3. Linux support does not depend on Wine or Proton.**
- **R4. The 3D engine and development workflow are free to use** and approachable for building a 3D exploration game.
- **R5. Python is not required.** It may be used where it does not become a build or runtime dependency.

## Design constraints stated by the owner

- **R6. Primitive sets are generated and stored before worlds:** M0a precedes M0b. Stated on 2026-09-07 during the platform discussion. The concept as first written did not state it, which the assessment lists as a gap; both statements are correct.
- **R7. Primitive identities are unsigned numeric values.** Width, namespace, and allocation policy are defaults confirmed in [decision 0006](decisions/0006-pack-identity-and-allocation.md).
- **R8. A discovered system is reachable only through its discoverer,** who hosts every visit and may invite guests. Recorded in [decision 0005](decisions/0005-discoverer-hosts-every-visit.md).
- **R9. Multiplayer topology is a loosely coupled network of hosts** coordinated by a simple cloud server, with all compute on the host. Recorded in [decision 0003](decisions/0003-network-topology-and-transport.md).
- **R10. Primitive composition represents the complete generated environment.** Every authoritative property of a solar system, its planets and surfaces, life, sites, and artifacts originates in exact primitive revisions, generated instance parameters, or primitive-based mutable state; there is no parallel environment-data model. Solar systems, planets, surfaces, caves, life forms, sites, and artifacts are nested composition graphs of primitive IDs rather than monolithic types. Recorded in [decision 0031](decisions/0031-primitive-complete-composition-and-storage.md).
- **R11. Existing generated systems reproduce exactly while derived exploration data remains cacheable.** Primitive revisions, generator/grammar versions, and parameters are pinned. Per-category storage may regenerate, materialize, or combine both according to measured determinism and performance, while memory and disk caches are fully evictable. Recorded in [decision 0031](decisions/0031-primitive-complete-composition-and-storage.md).
- **R12. A planet-surface sky is astronomically consistent with its generated system.** From the observer's surface location and saved simulation time, the star direction and lighting, apparent motion caused by planet rotation, and the position, phase, angular size, horizon visibility, eclipses, and occultations of other generated celestial bodies derive from the same pinned system, orbit, rotation, and atmosphere data. Decorative bodies cannot replace this solution. Recorded in [decision 0032](decisions/0032-astronomically-consistent-surface-sky.md).
- **R13. The destination dashboard has two controls: a tiered distance control and a seed lever.** The lever fills the destination's seed with a near-unique value that the player sees only as an abstract cue, never as a number and never typed back; environment preferences and survey focus are not core controls. Recorded in [decision 0043](decisions/0043-destination-controls-distance-tier-and-seed-lever.md).
- **R14. System composition is tiered and explorability is a validation outcome.** The starter tier's destination has exactly one explorable planet and no life; every higher tier has at least two explorable planets and at least one life-bearing planet. A planet is explorable when a region on it passes the suit profile, not because of its kind. Recorded in [decision 0044](decisions/0044-tiered-system-composition-one-star-and-explorability-by-validation.md).
- **R15. The whole environment of a destination is stored locally, for multiple visits and continued exploration.** Generation writes to the database and the view reads from it; storing only explored areas in more detail is a fallback for when storing everything would take too much space, and the measurement of 2026-09-11 says it does not ([inspection: storage](inspection-storage.md#the-open-question)). Recorded in [decision 0066](decisions/0066-generation-writes-to-the-database-and-the-view-only-reads.md), and, for when a destination's ground is generated, in [decision 0067](decisions/0067-a-destinations-ground-is-generated-when-it-is-composed.md).
- **R16. The primitive set is expected to grow substantially.** Storage, loading, and reference formats must scale to a set many times the 117 definitions of the `basic` vocabulary without re-minting existing content ([decision 0033](decisions/0033-generated-definitions-have-one-revision.md), [decision 0039](decisions/0039-one-manifest-per-pack-identifier.md)); a set's size and load cost are measured in the Resource-use row of [progress](progress.md#verification-and-performance-targets) before any format change is proposed.

## Provenance

| Requirement | Source | Date |
| --- | --- | --- |
| R1 to R5 | Platform discussion with the project owner | 2026-09-07 |
| R6 | Platform discussion with the project owner | 2026-09-07 |
| R7 | Owner request recorded in the technology decision register | 2026-09-07 |
| R8, R9 | Owner statements during the design review | 2026-09-07 |
| R10, R11 | Primitive composition and storage clarification from the project owner | 2026-09-07 |
| R12 | Surface-view realism request from the project owner | 2026-09-07 |
| R13, R14 | The owner's note "Environment generation and randomization" and its clarification session | 2026-09-08 |
| R15 | Owner statements during surface cycle three and the storage inspection, and the acceptance of decision 0067 | 2026-09-11 |
| R16 | Owner statement given with the acceptance of decision 0067 | 2026-09-11 |
