# Requirements

Reviewed 2026-09-07. Owner-stated constraints that sit outside the game idea. The concept of record is [concept.md](concept.md); where a refinement was adopted, the concept keeps the original idea inline and [decisions/](decisions/README.md) records what was chosen. Working defaults chosen by the documents, such as the engine, database, renderer, and CPU architecture, are not requirements and live in the [technology decision](technology-stack.md).

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

## Provenance

| Requirement | Source | Date |
| --- | --- | --- |
| R1 to R5 | Platform discussion with the project owner | 2026-09-07 |
| R6 | Platform discussion with the project owner | 2026-09-07 |
| R7 | Owner request recorded in the technology decision register | 2026-09-07 |
| R8, R9 | Owner statements during the design review | 2026-09-07 |
| R10, R11 | Primitive composition and storage clarification from the project owner | 2026-09-07 |
