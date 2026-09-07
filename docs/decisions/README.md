# Decision records

Each accepted design or technology decision gets one short record in this folder. Records are numbered in the order they are accepted and are never edited after acceptance; a later change adds a new record that supersedes the old one.

Records are created when a finding in the [design review](../review.md) is cleared, and when an entry in the [technology decision register](../technology-stack.md#gap-filling-decisions-for-confirmation) is confirmed. The review's status table links each cleared finding to its record.

File name: `NNNN-short-title.md`. Use [0000-template.md](0000-template.md).

| # | Decision | Status |
| --- | --- | --- |
| [0001](0001-materialize-authoritative-terrain.md) | Materialize authoritative terrain at world acceptance | Accepted |
| [0002](0002-primitive-set-content-gate.md) | Content gate for generated sets; generated-only sets; deferred set algebra | Accepted |
| [0003](0003-network-topology-and-transport.md) | Core-owned replication, loosely coupled hosts, connectivity timing | Accepted |
| [0004](0004-preparation-is-real-generation-time.md) | Preparation is real generation time; effort score is tier base plus recipe complexity | Accepted |
| [0005](0005-discoverer-hosts-every-visit.md) | A discovered system is reachable only through its discoverer | Accepted |
| [0006](0006-pack-identity-and-allocation.md) | 128-bit pack identifiers, one pack per generation run, normative terminology | Accepted |
| [0007](0007-artifact-collision-substitution-and-campaign-salt.md) | Campaign branch in the specification; per-artifact substitution on collision | Accepted |
| [0008](0008-random-stream-derivation.md) | Random stream derivation, rejection sampling, build-time enforcement | Accepted |
| [0009](0009-solution-layout.md) | Solution layout by assembly; authored content outside the Godot project | Accepted |
| [0010](0010-units-coordinates-and-region-bounds.md) | Units, coordinates, region bounds, and simulation tick | Accepted; region extent cap superseded by [0017](0017-region-extent-cap-and-storage-derivation.md) |
| [0011](0011-requirements-single-source.md) | Requirements document as single source for platform policy and provenance | Accepted |
| [0012](0012-core-travel-model.md) | Core travel model: hub and spoke, two tiers, snapping distance control, one guest | Accepted |
| [0013](0013-host-simulates-hazards.md) | The host simulates hazards for every avatar | Accepted |
| [0014](0014-tooling-and-licence.md) | Repository tooling aligned with the stack; licence deferred with notice | Accepted |
| [0015](0015-greybox-prototype.md) | Disposable greybox expedition prototype in parallel with M0a | Accepted |
| [0016](0016-platform-confirmed-and-toolchain-pinned.md) | Platform confirmed: Godot 4.7.2 .NET on .NET 10; toolchain pinned; solution scaffolded | Accepted |
| [0017](0017-region-extent-cap-and-storage-derivation.md) | Region extent cap 2,048 m; per-region storage derived from the constants | Accepted |
| [0018](0018-data-root-region-encoding-and-save-integrity.md) | Data root, package layout, region encoding, and save integrity | Accepted |
