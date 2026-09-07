# Space Explorer: implementation progress

Updated 2026-09-07. The single source for where the work stands, so the other documents state status once and link here ([decision 0011](decisions/0011-requirements-single-source.md) applies the same rule to platform policy).

It opens with the record of every step that has passed, then tracks the [development plan](development-plan.md) exactly: the same milestones in the same order, the plan's M0a exit criteria in the order the plan states them, the plan's eleven verification checks under their own names, and the plan's seven open production decisions. Nothing is added to the plan's process and nothing is dropped from it; rows the plan does not contain appear only in the section labelled as such.

A step is recorded as passed only with the evidence that closed it, never on the strength of having been written. Status words mean one thing throughout: **Done** is implemented and checked by the evidence named; **Partial** states exactly which part holds; **Not started** means no code or record exists. "Passes on Linux and Windows" always means both were run, never that one was run and the other inferred.

Local results are from a Debian 13 x86_64 workstation. No performance target has been measured.

## Completed steps

Every step that has passed, in the order it passed, with the evidence that closed it and the commit that carries it. All of this is from 2026-09-07.

| # | Step | Passed on the evidence of | Commit |
| --- | --- | --- | --- |
| 1 | Concept and owner requirements captured; `requirements.md` declared the single source for platform policy and provenance ([decision 0011](decisions/0011-requirements-single-source.md)) | Concept of record fixed; platform restatements elsewhere reduced to one sentence and a link | `3faa59e`, `0a98fb0` |
| 2 | Technology assessment and stack selection: Godot 4 .NET with C#, engine-independent core, SQLite | Checked against requirements R1 to R5, with a five-engine alternatives table sourced from vendor pages | `0a98fb0`, `7d544e1` |
| 3 | Design review, first round: sixteen findings raised and cleared, decisions 0001 to 0015 accepted | Every internal link and anchor resolves; cited external URLs respond; worked valuation examples check out arithmetically | `194e0de` |
| 4 | Platform confirmed and toolchain pinned: Godot 4.7.2 .NET, .NET SDK 10.0.400, `net10.0`, central package versions ([decision 0016](decisions/0016-platform-confirmed-and-toolchain-pinned.md)) | Assessment showed Godot alone is free to use with a first-class Linux editor, native Linux and Windows exports, and official C# | `45d0bef` |
| 5 | Solution scaffolded by assembly: seven projects, nullable reference types, implicit usings, warnings as errors, code-style enforcement ([decision 0009](decisions/0009-solution-layout.md)) | `dotnet build` succeeds with zero warnings across all seven projects | `45d0bef` |
| 6 | Structural test baseline | Three tests pass: Core references no Godot assembly, Persistence references no Godot assembly, and the native SQLite library loads and reports a version | `45d0bef` |
| 7 | Continuous integration on both operating systems | GitHub Actions runs restore, build, and test on `ubuntu-latest` and `windows-latest` for every push to `main` and every pull request | `45d0bef` |
| 8 | Godot project and export presets: Compatibility renderer, root `Main` scene, x86_64 presets for Linux and Windows Desktop | Windows preset cross-exports from Linux without Wine | `45d0bef` |
| 9 | Linux exported-build smoke test | `smoke.sh` exports and the packaged build prints `SMOKE OK` with Godot 4.7.2, .NET 10.0.11, and SQLite 3.53.3; run manually on the reference workstation | `45d0bef` |
| 10 | Design review, second round: findings 17 to 19 cleared. Region storage constants corrected and the extent cap set to 2,048 m ([decision 0017](decisions/0017-region-extent-cap-and-storage-derivation.md)); data root, package layout, region encoding, and save integrity specified ([decision 0018](decisions/0018-data-root-region-encoding-and-save-integrity.md)) | The over-determined region constants were caught by deriving bytes from the constants rather than restating the estimate | `7d544e1`, `f774a1a`, `dbe079e` |
| 11 | M0a step one: deterministic random foundation frozen as generator version 1. `Pcg32`, `Mix64`, `StreamPath`, `RandomStream`, `GeneratorVersion` ([decision 0019](decisions/0019-generator-version-1-frozen.md)) | 41 Core tests pass; both external anchors hold, the PCG32 reference demo for seed 42 and stream 54 and the SplitMix64 first output for seed 0; identical results on `ubuntu-latest` and `windows-latest`; mutation-checked on three frozen constants; `RS0030` re-verified on a `System.Random` use in Core | `f774a1a` (source), `9ab5838`, `1bbd8f9` (record) |
| 12 | M0a step two: canonical encoding format 1 and the SHA-256 content hash frozen. `CanonicalWriter`, `ContentHash`, `PackId` ([decision 0020](decisions/0020-canonical-encoding-and-content-hash.md)) | 79 Core tests pass, 38 of them new; the published SHA-256 vectors for the empty input and for `abc` hold; the recorded 47-byte vector follows by hand from the frozen rules and its hash was computed outside this repository, not by the code under test; mutation-checked on five rules, big-endian fixed-width values, a dropped length prefix, a dropped domain, a pack identifier taken from the tail of the hash, and a dropped zig-zag step, each of which turns the suite red; identical results on `ubuntu-latest` and `windows-latest` | `a103471` (source), `8b0c291` (record) |

Two notes on the record above. An initial TypeScript scaffold was created and then replaced when the Linux-first Godot C# stack was documented, so it is not a passed step. The source of step 11 landed in `f774a1a`, a commit whose message describes step 10, because a parallel session staged the whole tree while those files were being written; the content is the reviewed content, but the message does not describe it.

## Milestones

| Milestone | Status |
| --- | --- |
| P0: greybox expedition prototype | **Not started.** `prototypes/` holds only its README. Does not gate M0a ([decision 0015](decisions/0015-greybox-prototype.md)). |
| M0a: primitive-set foundation | **In progress.** Of the plan's exit criteria below, one is done, three are partial, and eleven are not started. The frozen random foundation is not itself one of the plan's M0a criteria; it appears under Determinism in the verification table. The canonical encoding and content hash appear in criterion 5. |
| M0b: world generation and save prototype | **Not started.** |
| M1: solo playable expedition | **Not started.** |
| M2: connected core release | **Not started.** |
| M3: expansion experiments | **Not started.** |

## M0a exit criteria

The plan's M0a row, decomposed in its own order.

| # | Criterion | Status |
| --- | --- | --- |
| 1 | Core and CLI generate random, constraint-valid primitive sets from authored templates | Not started. `content/` holds no templates. |
| 2 | Allocate IDs | Not started. |
| 3 | Validate, store, reload, and query sets without rerolling | Not started. |
| 4 | Specify cross-package set operations without implementing them | Not started. Specified in prose in the [technical design](technical-design.md#primitive-sets-and-compact-references); no code contract exists. |
| 5 | Verify canonical hashes on Linux and Windows | **Partial.** The canonical encoding and the SHA-256 content hash exist and are frozen as format 1 ([decision 0020](decisions/0020-canonical-encoding-and-content-hash.md)), with a recorded vector and a derived pack identifier that the suite checks. That suite passed on `ubuntu-latest` and on `windows-latest` in continuous integration for `8b0c291` on 2026-09-07, 79 Core tests on each, so the recorded vector, its content hash, and the pack identifier derived from it are byte-identical on both operating systems. No set specification, manifest, or package is hashed yet, because none exists, so what is verified is the encoding and the hash themselves, not the hash of any stored record. |
| 6 | Missing and corrupt dependencies fail explicitly | Not started. |
| 7 | ID boundaries and collisions | Not started. |
| 8 | Bounded failures | Not started. |
| 9 | Interrupted publication | Not started. |
| 10 | Expansion compatibility | Not started. |
| 11 | Minimal Godot viewer | Not started. `Main.tscn` holds the exported-build smoke entry point only. |
| 12 | Native packaged smoke tests | **Partial.** `smoke.sh` and `smoke.ps1` exist. The Linux export printed `SMOKE OK` when run manually on 2026-09-07 with Godot 4.7.2, .NET 10.0.11, and SQLite 3.53.3. The Windows export was produced from Linux and never executed, and neither script runs in continuous integration. |
| 13 | Tool versions pinned | **Done.** .NET SDK 10.0.400 in `global.json`, `net10.0` throughout, Godot.NET.Sdk 4.7.2, central package versions ([decision 0016](decisions/0016-platform-confirmed-and-toolchain-pinned.md)). |
| 14 | Reference machines recorded | **Partial.** The Linux workstation is recorded in decision 0016; no Windows reference machine is identified. |
| 15 | Content gate: reviewer sort against a pre-recorded threshold, and the silhouette comparison finds no near-duplicates | Not started. Blocked on criteria 1 and 11 ([decision 0002](decisions/0002-primitive-set-content-gate.md)). |

Immediate next step: the authored template vocabulary of criterion 1 and the set specification record it determines, which fixes the fields hashed under [decision 0020](decisions/0020-canonical-encoding-and-content-hash.md) and therefore the `pack_id` derived from them ([decision 0006](decisions/0006-pack-identity-and-allocation.md)). A reader for the canonical format is needed by criterion 3; the format is written and hashed today but never parsed back.

## Verification and performance targets

The plan's eleven checks, under the plan's names.

| Check | Status |
| --- | --- |
| Native platform support | **Partial.** `dotnet build` and `dotnet test` pass on `ubuntu-latest` and `windows-latest` in continuous integration for every push and pull request: 81 tests, 79 in Core and 2 in Persistence, with zero build warnings under warnings-as-errors. Native SQLite loads on both. None of the rest holds: the distributed game has never been launched on a clean environment, graphics and input are unverified on Windows, there is no save format to exchange, and no co-op test exists. |
| Primitive-set foundation | **Not started.** No set data, pack identifier, manifest, or ID allocator exists to test. |
| Determinism | **Partial.** Two of the plan's clauses hold. The banned-API analyzer gates the core build: `RS0030` was re-checked on a `System.Random` use compiled into Core after the first real code landed. The random primitives produce bit-identical output on both operating systems, evidenced by the random-foundation tests among the 79 Core tests now passing on `ubuntu-latest` and `windows-latest`, including recorded derivation vectors and the two external anchors, the PCG32 reference demo for seed 42 and stream 54 and the SplitMix64 first output for seed 0. The suite is mutation-checked, so it is load-bearing: perturbing the LCG multiplier, dropping the mixer's length step, and swapping the two domain constants each turn it red. Canonical encoding is no longer missing from this check: identity-bearing bytes have a frozen format and a specified hash ([decision 0020](decisions/0020-canonical-encoding-and-content-hash.md)) whose recorded vector and derived pack identifier come out identical on `ubuntu-latest` and `windows-latest`, and the writer admits no floating-point value, no host byte order, and no text outside the ASCII set that path segments use. Not held: there are no descriptors, recipes, placements, or prices to compare, no thread-count or interrupted-run coverage, and the two-process run is absent because no code written so far builds a collection or a canonical record ([decision 0019](decisions/0019-generator-version-1-frozen.md)). |
| Destination validity | **Not started.** No generator, so no seed suite. |
| Compatibility | **Not started.** |
| Durability | **Not started.** No transactions, ledger, or save format exists. |
| Multiplayer | **Not started.** No transport, command, or handshake code exists; the plan runs this suite over the in-memory transport from M0a onward. |
| Responsiveness | **Not started.** |
| Resource use | **Not started.** [Decision 0017](decisions/0017-region-extent-cap-and-storage-derivation.md) derives 3 MiB raw and at most 1 MiB compressed for a maximal 2,048 m region; that is arithmetic awaiting the M0b measurement, not a result. |
| Artifact variety | **Not started.** |
| Gameplay/economy | **Not started.** |

The external reference vectors under Determinism were reproduced from implementations written against the published algorithms rather than read from the sources on the day, so they are corroboration and remain worth confirming against pcg-random.org and a SplitMix64 reference ([decision 0019](decisions/0019-generator-version-1-frozen.md)).

## Decisions before production commitments

The plan's seven rows, with what has changed since it was written.

| Decision | Status |
| --- | --- |
| Long-wait role | Settled for the core release by [decision 0004](decisions/0004-preparation-is-real-generation-time.md). The M3 question, whether very large budgets produce content players value, is untested. |
| Pricing and progression | Open. Needs the simulation of repeated expeditions the plan describes. |
| Stack validation, OS versions, hardware | **Partial.** The stack is confirmed and pinned ([decision 0016](decisions/0016-platform-confirmed-and-toolchain-pinned.md)) and library builds and tests pass on both operating systems. Unvalidated: native packaging on Windows, one generated region, a saved artifact, the networking proof at M2 start, and the initial x86_64 and Compatibility scope. Supported distributions, Windows versions, graphics requirements, and reference machines are not confirmed, so no minimum requirements can be published. |
| Numeric content and suit limits | **Partial.** Units, position encoding, heightfield resolution, and tick are fixed by [decision 0010](decisions/0010-units-coordinates-and-region-bounds.md) and the region cap by [decision 0017](decisions/0017-region-extent-cap-and-storage-derivation.md). Generation and storage costs are unmeasured, so region counts, artifact counts, effort scores, prices, suit envelopes, and site difficulty are untuned, and no versioned numeric tables exist yet. |
| Camera and art direction | Open by design; settled from M1 feedback. |
| Internet hosting | Open by design; the connectivity approach is decided at M1 start, self-hosted relay and coordination server evaluated first ([decision 0003](decisions/0003-network-topology-and-transport.md)). No service is selected. |
| Schedule and budget | Open. Awaits the owner's team capacity and budget; no milestone estimates exist. |

## Open items outside the plan's process

Carried from [decision 0016](decisions/0016-platform-confirmed-and-toolchain-pinned.md) and later records. These are not rows in the development plan, and are listed here so they are not lost.

| Item | State |
| --- | --- |
| Chunked mesh with level of detail | Open, needed before M0b's region measurement. Godot supplies no terrain or LOD system. [Decision 0017](decisions/0017-region-extent-cap-and-storage-derivation.md) quartered the worst case by halving the extent cap, leaving a maximal region at about a million cells (open item 1). |
| P0 built on the Godot stack | Open. Building it in this Godot project would exercise movement, physics, the Compatibility renderer, and the Windows export before M1 (open item 2). |
| Native Windows machine or runner | Open (open item 3). The `windows-latest` job builds and tests the libraries but never launches the exported game, so requirement R2 is not demonstrated ([requirements](requirements.md)). |
| Exported-build smoke tests in continuous integration | Open. Blocked on provisioning Godot 4.7.2 and its export templates on the runners; scheduled to join at the M0a gate. |
| Licence | Deferred. All rights reserved until decided ([decision 0014](decisions/0014-tooling-and-licence.md)). |

## Design documentation

Complete and self-consistent as of 2026-09-07: all nineteen [review](review.md) findings cleared, twenty [decision records](decisions/README.md) accepted, every internal link and anchor resolving. Apart from the random foundation and the canonical encoding, the design statements are untested against code.
