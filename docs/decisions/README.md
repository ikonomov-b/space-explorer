# Decision records

Each accepted design or technology decision gets one short record in this folder. Records are numbered in the order they are accepted and are never edited after acceptance; a later change adds a new record that supersedes the old one.

Records are created when a finding in the [design review](../review.md) is cleared, and when an entry in the [technology decision register](../technology-stack.md#gap-filling-decisions-for-confirmation) is confirmed. The review's status table links each cleared finding to its record.

File name: `NNNN-short-title.md`. Use [0000-template.md](0000-template.md). A record that replaces part of an earlier one says so in its header line, in the form "Supersedes the region extent cap of decision 0010" with the record number linked. The table below is generated from the records' headings and header lines by `tools/docs.cs` (`dotnet run tools/docs.cs`), which is how a partial supersession is shown against the earlier record without editing it; continuous integration fails when the table is stale.

<!-- generated:begin decisions -->
| # | Decision | Status |
| --- | --- | --- |
| [0001](0001-materialize-authoritative-terrain.md) | Materialize authoritative terrain at world acceptance | Accepted; blanket terrain materialization superseded by [0031](0031-primitive-complete-composition-and-storage.md) |
| [0002](0002-primitive-set-content-gate.md) | Content gate for generated primitive sets; generated-only sets; deferred set algebra | Accepted |
| [0003](0003-network-topology-and-transport.md) | Core-owned replication, loosely coupled hosts, and connectivity timing | Accepted |
| [0004](0004-preparation-is-real-generation-time.md) | Preparation time is real generation time; effort score is tier base plus recipe complexity | Accepted |
| [0005](0005-discoverer-hosts-every-visit.md) | A discovered system is reachable only through its discoverer | Accepted |
| [0006](0006-pack-identity-and-allocation.md) | 128-bit pack identifiers, one pack per generation run, normative terminology | Accepted; expected-scale clause superseded by [0038](0038-derived-instances.md) |
| [0007](0007-artifact-collision-substitution-and-campaign-salt.md) | Campaign branch in the world specification; per-artifact substitution on appearance collision | Accepted |
| [0008](0008-random-stream-derivation.md) | Random stream derivation, rejection sampling, and build-time enforcement | Accepted; mixer clause superseded by [0021](0021-sha256-stream-derivation.md) |
| [0009](0009-solution-layout.md) | Solution layout by assembly; authored content outside the Godot project | Accepted; Benchmarks project superseded by [0024](0024-tests-comments-index-and-scaffolding-trimmed.md) |
| [0010](0010-units-coordinates-and-region-bounds.md) | Units, coordinates, region bounds, and simulation tick | Accepted; region extent cap superseded by [0017](0017-region-extent-cap-and-storage-derivation.md), single position type and coordinate-frame row superseded by [0036](0036-frames-and-transforms-typed-by-connector-kind.md) |
| [0011](0011-requirements-single-source.md) | Requirements document as the single source for platform policy and provenance | Accepted |
| [0012](0012-core-travel-model.md) | Core travel model: hub and spoke, two tiers with a snapping distance control, one guest | Accepted |
| [0013](0013-host-simulates-hazards.md) | The host simulates hazards for every avatar | Accepted |
| [0014](0014-tooling-and-licence.md) | Repository tooling aligned with the stack; licence deferred with an explicit notice | Accepted |
| [0015](0015-greybox-prototype.md) | Disposable greybox expedition prototype in parallel with M0a | Accepted; no-gate clause superseded by [0022](0022-p0-gates-the-first-frozen-content-format.md) |
| [0016](0016-platform-confirmed-and-toolchain-pinned.md) | Platform confirmed: Godot 4.7.2 .NET with C# on .NET 10; toolchain pinned; solution scaffolded | Accepted; code-style enforcement superseded by [0024](0024-tests-comments-index-and-scaffolding-trimmed.md) |
| [0017](0017-region-extent-cap-and-storage-derivation.md) | Region extent cap reduced to 2,048 m; per-region storage derived from the constants | Accepted |
| [0018](0018-data-root-region-encoding-and-save-integrity.md) | Data root, package layout, region encoding, and save integrity | Accepted; fixed region encoding and world-package layout superseded by [0031](0031-primitive-complete-composition-and-storage.md) |
| [0019](0019-generator-version-1-frozen.md) | Generator version 1: mix construction, path grammar, and bounded sampling frozen | Accepted; mix construction superseded by [0021](0021-sha256-stream-derivation.md) |
| [0020](0020-canonical-encoding-and-content-hash.md) | Canonical encoding format 1 and the SHA-256 content hash | Accepted |
| [0021](0021-sha256-stream-derivation.md) | Generator version 2: SHA-256 stream derivation | Accepted |
| [0022](0022-p0-gates-the-first-frozen-content-format.md) | The P0 exit record precedes the first frozen content format | Accepted; criterion 1 wait superseded by [0030](0030-m0a-criterion-1-proceeds-before-the-p0-exit-record.md) |
| [0023](0023-pcg32-is-a-sealed-class.md) | Pcg32 is a sealed class | Accepted |
| [0024](0024-tests-comments-index-and-scaffolding-trimmed.md) | One test per frozen rule; comments point to decisions; generated index, Benchmarks project, and code-style flag removed | Accepted |
| [0025](0025-p0-assessment-location-scope-and-players.md) | P0 assessment: location, scope, and players | Accepted |
| [0026](0026-biome-rule-sets-and-recovery-challenges-are-template-vocabularies.md) | Biome rule sets and recovery challenges are scoped template vocabularies | Accepted |
| [0027](0027-in-memory-transport-commands-and-handshake-at-m0a.md) | The in-memory transport, commands, and join handshake are M0a scope | Accepted |
| [0028](0028-in-memory-transport-command-and-handshake-contract.md) | The in-memory transport, command journal, and join handshake contract | Accepted |
| [0029](0029-primitive-identity-handle-and-cross-package-set-operations-contract.md) | Primitive identity, handle, and cross-package set operations contract | Accepted; revision-blind exact-reference contract superseded by [0031](0031-primitive-complete-composition-and-storage.md) |
| [0030](0030-m0a-criterion-1-proceeds-before-the-p0-exit-record.md) | M0a criterion 1 proceeds before the P0 exit record | Accepted |
| [0031](0031-primitive-complete-composition-and-storage.md) | Primitive-complete composition and adaptive storage | Accepted; definition-revision clause superseded by [0033](0033-generated-definitions-have-one-revision.md), per-definition storage-policy pin superseded by [0034](0034-storage-policy-pinned-by-manifests.md) |
| [0032](0032-astronomically-consistent-surface-sky.md) | Astronomically consistent surface sky | Accepted |
| [0033](0033-generated-definitions-have-one-revision.md) | A generated definition has one revision for life; revisions belong to authored template packs | Accepted |
| [0034](0034-storage-policy-pinned-by-manifests.md) | Storage policy is pinned per category by the world and set manifests, not by the definition | Accepted |
| [0035](0035-category-registry-record-and-generator-revision-identifiers.md) | The category registry is a versioned canonical record that definition labels carry; generator revisions are identifiers it lists | Accepted |
| [0036](0036-frames-and-transforms-typed-by-connector-kind.md) | Four frames; the connector kind decides the transform type; a units row per frame | Accepted |
| [0037](0037-derivation-rules-integer-periods-and-orbit-hierarchy.md) | Derivation-rule primitives, integer periods with phase at epoch, and the two-body hierarchy | Accepted |
| [0038](0038-derived-instances.md) | Derived instances: recipe-produced, path-addressed, overlay-targetable, counted apart from nodes | Accepted |
<!-- generated:end decisions -->
