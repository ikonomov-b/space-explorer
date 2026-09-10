# Inspection: documents and structure

Inspected 2026-09-10 at commit `ef14630`, "Decision 0055: a definition carries tags, and a body its surface". This inspection reads the documents against each other and against the code, and reports what the two no longer agree on. It records no decision and clears nothing: each finding below states whether clearing it applies a decision already accepted or needs a new record, and acceptance stays with the owner ([decisions](decisions/README.md)).

Findings are numbered within this document. They are not [design review](review.md) findings, whose sequence stands at 49 and whose status table remains that review's own.

The work this inspection set out to read as uncommitted was committed and pushed during the reading, so the tree is clean and the state below is the state of `ef14630`.

## What was run

Commands are the ones single-sourced in [external tools](tools.md); none was re-derived.

| Check | Result |
| --- | --- |
| `dotnet run tools/docs.cs -- --check` | 79 documents, 1776 internal links, 55 decision records; 0 problems, exit 0 |
| `dotnet build --no-incremental` | Succeeded, 0 warnings, 0 errors |
| `dotnet test --no-build` | 284 pass: 253 Core, 25 Persistence, 6 Cli |
| Continuous integration, run `34491083126` | Green on `ubuntu-latest` and `windows-latest` |
| Frozen hashes | Registry `1419038d` (revision 1) and `ae7474a8` (revision 2), grammars `611829f1`, `61a218fd`, `d0d5635f` all unmoved; new registry revision 3 `81883cec`, grammar version 4 `53314489` |
| `generate-set`, then `iterate --seeds 1-24 --tier starter` | Runs clean under revision 3 and grammar 4, and returns iteration 3's figures exactly: 5 of 24; 114 planets, 68 moons, 72 candidates; refused 62 by pressure, 54 by temperature, 42 by gravity |
| Review view | Renders; the panel reads `pinned registry 3, grammar 4`, and the gas giant wears a banded generated material |

The unmoved hashes are the substantive result: [decision 0050](decisions/0050-registry-revision-2-grammar-version-2-and-versioned-record-growth.md)'s versioned-record-growth rule is held in fact and not only in prose, and the unchanged iteration figures are what a revision that alters no physical value should produce.

## Findings

| # | Finding | Class | Clears by |
| --- | --- | --- | --- |
| 1 | [progress.md describes the state before the commit](#1-progressmd-describes-the-state-before-the-commit) | Inconsistency | Applying [decision 0011](decisions/0011-requirements-single-source.md) |
| 2 | [The core rasterizer and `surface-appearance/1` are unrecorded design](#2-the-core-rasterizer-and-surface-appearance1-are-unrecorded-design) | Foundational | **A new decision record** |
| 3 | [`artifact-part.material` is untagged while its pool widened](#3-artifact-partmaterial-is-untagged-while-its-pool-widened) | Gap | Applying [decision 0055](decisions/0055-definition-level-tags-and-tagged-references.md) in authored content |
| 4 | [src/README.md owes the layout update](#4-srcreadmemd-owes-the-layout-update) | Inconsistency | Applying [decision 0055](decisions/0055-definition-level-tags-and-tagged-references.md) |
| 5 | [The view does not say when it is standing in](#5-the-view-does-not-say-when-it-is-standing-in) | Gap | Applying [decision 0055](decisions/0055-definition-level-tags-and-tagged-references.md) |
| 6 | [The verification table's suite size is stale](#6-the-verification-tables-suite-size-is-stale) | Inconsistency | Applying [decision 0011](decisions/0011-requirements-single-source.md) |
| 7 | [The generated textures are never freed](#7-the-generated-textures-are-never-freed) | Process | Code fix; no record |
| 8 | [A half-applied edit in SystemView.cs](#8-a-half-applied-edit-in-systemviewcs) | Process | Code fix; no record |
| 9 | [`ConnectorDeclaration.MaxTags` is a dead alias](#9-connectordeclarationmaxtags-is-a-dead-alias) | Process | Applying [decision 0024](decisions/0024-tests-comments-index-and-scaffolding-trimmed.md) |
| 10 | [Decision 0055 claims a section that did not change](#10-decision-0055-claims-a-section-that-did-not-change) | Inconsistency | Applying [decision 0055](decisions/0055-definition-level-tags-and-tagged-references.md) |

What has been done about each since, which is not part of what was found, is in [what the documents pass cleared](#what-the-documents-pass-cleared).

### 1. progress.md describes the state before the commit

Affects: [implementation progress](progress.md).

[Decision 0011](decisions/0011-requirements-single-source.md) makes `progress.md` the single current-status source, so a row that describes the state before `ef14630` is not merely stale but wrong at the place the process says to read. Three rows are now false, and one row is missing.

- **M0a criterion 1** still reads "Also not held, and required by [decision 0055]: a definition and its template carrying tags of their own under registry revision 3, a template's reference parameter demanding tags of what it names, and the generator filling such a reference only from definitions that satisfy the demand and failing by name where none does." All four clauses are built and tested; the last is pinned by `RegistryRevision3Tests.A_reference_no_definition_suits_fails_by_name_rather_than_drawing_something_else`.
- **M0a criterion 11** still reads "it draws spheres coloured by body type from a composed graph and no geometry, material, or texture primitive reaches it", and "Not held: none of that is built." A material and a texture primitive now reach the adapter through `src/SpaceExplorer.Game/PrimitiveResources.cs`.
- **The iteration 4 row** still reads "The sample does not exist and the code is unfinished." The code is finished, committed, and green in continuous integration; the sample and the verdict are what remain outstanding.
- **No completed-steps row 25 exists**, and the evidence for one now does: `ef14630`, run `34491083126` on both legs, 284 tests, and the hash immobility recorded above. The table stops at 24.

### 2. The core rasterizer and `surface-appearance/1` are unrecorded design

Affects: [technical design](technical-design.md#architecture-and-ownership), [decision 0021](decisions/0021-sha256-stream-derivation.md), [decision 0047](decisions/0047-usable-storage-and-structure-generation-then-solar-system-iterations.md), [review finding 20](review.md#20-two-hashing-primitives-where-one-would-do).

`src/SpaceExplorer.Core/Appearance/SurfaceAppearance.cs` is new and does four things no accepted decision states. [Decision 0055](decisions/0055-definition-level-tags-and-tagged-references.md) says only that "the Godot adapter builds a body's material from the `surface-material` the body stores and the `texture-recipe` that material references"; it does not say where a texture recipe becomes pixels, under what version, or seeded by what.

- **A version-numbered format that nothing pins.** `new CanonicalWriter("surface-appearance/1")` mints a versioned domain label alongside `category-registry/N`, `composition-grammar/N/registry/M`, and `primitive-definition/1/registry/N`. Every other such label is frozen by a decision and pinned in an iteration row; this one is recorded nowhere, and no iteration row lists it among what reproduces the pictures a verdict is read from ([decision 0047](decisions/0047-usable-storage-and-structure-generation-then-solar-system-iterations.md)).
- **A second mixing construction in Core.** `SurfaceAppearance.Value` is a 64-bit mixer using the SplitMix and Murmur finalizer constants. It is the only such construction in `src/`: `Shared/` holds `Pcg32`, `RandomStream`, and `StreamPath` and nothing else, because [decision 0021](decisions/0021-sha256-stream-derivation.md) deleted `Mix64` to clear [review finding 20](review.md#20-two-hashing-primitives-where-one-would-do), and `progress.md` states the resulting position under Determinism: "the core freezes one published hash rather than a construction of its own." A per-pixel lattice hash plausibly cannot afford SHA-256, but that trade is exactly the shape of decision 0021, and it is unwritten.
- **An unnamed, unversioned rule.** `Raster` turns a `pattern` enum plus five parameters into pixels by a fixed algorithm. The project's mechanism for a named implementation with typed inputs, one typed output, integer arithmetic, and recorded vectors is `derive-<name>/<k>` ([decision 0037](decisions/0037-derivation-rules-integer-periods-and-orbit-hierarchy.md), [decision 0049](decisions/0049-suit-profile-and-system-description-version-1-and-derivation-rules-as-named-implementations.md)). This is that in everything but the name and the record, so changing the noise function would silently change every stored body's face with nothing to say a version moved.
- **A layering position.** [Architecture and ownership](technical-design.md#architecture-and-ownership) separates the deterministic generator, content registry, world store, campaign simulation, renderer, and network transport. Computing pixels in Core is a defensible reading of it, and the class comment argues the case, but no document takes that position and the argument currently survives only in the commit message of `ef14630`.

The design statement itself is sound. The recommendation is to record the code, not to move it.

### 3. `artifact-part.material` is untagged while its pool widened

Affects: `content/templates/basic/vocabulary.json`, [decision 0055](decisions/0055-definition-level-tags-and-tagged-references.md).

The registry gives `artifact-part` a `material` reference, and template 6 in the `basic` vocabulary states no range for it, so it draws from any `surface-material`. Templates 25 to 28, the new body surfaces, are requested before templates 3 and 4, the artifact materials, so they are in the pool. In a pack generated at seed 42, five of six artifact parts drew a body surface:

```
41 artifact-part template 6: geometry=->36, material=->33, mass=6517    (metal-material, correct)
42 artifact-part template 6: geometry=->35, material=->12, mass=15747   (rock-surface, template 25)
43 artifact-part template 6: geometry=->35, material=->18, mass=19284   (ocean-surface, template 27)
44 artifact-part template 6: geometry=->39, material=->10, mass=10804   (rock-surface, template 25)
45 artifact-part template 6: geometry=->36, material=->17, mass=10247   (ocean-surface, template 27)
46 artifact-part template 6: geometry=->37, material=->20, mass=5827    (gas-surface, template 28)
```

This is the fault decision 0055 exists to prevent, arriving from the other side. Its Context worries that "an icy body would draw a metallic-green material as readily as an ice one"; its Consequences name "the vocabulary must provide every tag a reference demands, a coupling between two authored files that only generation reveals". An untagged reference whose pool silently grows is that same coupling, unguarded. The fix is authored content: tag templates 3 and 4 `for-artifact` and give template 6 `"material": ["for-artifact"]`. That moves the vocabulary hash and the set pack, which iteration 4 has not yet pinned, so this is the cheapest moment to do it.

### 4. src/README.md owes the layout update

Affects: [src/README.md](../src/README.md).

It is a document the [top-level README](../README.md) lists, it is not in `ef14630`, and it is not in decision 0055's "Applied to" list. It is now wrong in four places: the Core row names `CategoryRegistryRevision1` and `CategoryRegistryRevision2` but not revision 3, and "`CompositionGrammar` with its three versions" where there are now four; it lists neither `Appearance/` nor `TagList`; the Game row omits `PrimitiveResources`, which is the adapter M0a criterion 11 turns on; and the tests row omits the `Appearance/` suite.

### 5. The view does not say when it is standing in

Affects: `src/SpaceExplorer.Game/SystemView.cs`, [decision 0055](decisions/0055-definition-level-tags-and-tagged-references.md), [primitives part 4](primitives.md#part-4-why-this-is-the-universal-base).

Decision 0055 states that "a `star` and an `atmosphere` declare no appearance under revision 3 either, so a view that stands in for them says so", and primitives part 4 makes it general: "A category that declares no appearance is drawn by a stand-in, and a view that stands in says so." `SystemView.Legend()` says only that the view is not to scale. The star is coloured by `StarColour(type)`, and a body under a pre-revision-3 pack falls back to `BodyColour(body.Type)`; neither is disclosed in the picture. Confirmed in the render.

### 6. The verification table's suite size is stale

Affects: [implementation progress](progress.md).

The Native platform support row reads "the suite is 266 tests, 241 in Core, 20 in Persistence, and 5 in `SpaceExplorer.Cli.Tests`". Steps 23 and 24 of the same document record 275 (244, 25, 6), and the tree now runs 284 (253, 25, 6). This row was already stale before decision 0055: it was not updated when [decision 0053](decisions/0053-a-destination-is-a-stored-record-addressed-by-its-levers.md) landed.

### 7. The generated textures are never freed

Affects: `src/SpaceExplorer.Game/PrimitiveResources.cs`, [decision 0054](decisions/0054-the-review-panel-carries-the-summary-while-the-system-is-framed.md).

Running the review view prints on shutdown:

```
ERROR: Texture with GL ID of 45: leaked 349524 bytes.
   at: ~Utilities (drivers/gles3/storage/utilities.cpp:82)
```

349,524 bytes is a 128×128 RGBA image with mipmaps, that is, `PrimitiveResources.TextureFor`. The material dictionary outlives the GL context because `PrimitiveResources` is a plain class held by the `Node3D` rather than a `Node` in the tree. The screenshot still succeeds and the exported-build smoke path does not touch `SystemView`, so nothing is broken; but the sample-rendering workflow of decision 0054 now emits `ERROR:` lines on every run, which will make a real failure harder to see.

### 8. A half-applied edit in SystemView.cs

Affects: `src/SpaceExplorer.Game/SystemView.cs`.

`Stored` was inserted between `Angle`'s doc comment and `Angle`, so `Stored` carries two `<summary>` tags and `Angle` has none:

```csharp
    /// <summary>The angle of a body's named orbital element, from the binary turn the graph stores (decision 0036).</summary>
    /// <summary>The material the graph node behind <paramref name="body"/> names, or null where it names none.</summary>
    private StandardMaterial3D? Stored(BodyDescription body)
```

It does not warn, because documentation-file generation is off, so nothing will catch it.

### 9. `ConnectorDeclaration.MaxTags` is a dead alias

Affects: `src/SpaceExplorer.Core/Registry/ConnectorDeclaration.cs`, [decision 0024](decisions/0024-tests-comments-index-and-scaffolding-trimmed.md).

After the extraction into `TagList`, `public const int MaxTags = TagList.MaxTags;` has no consumer in `src/` or `tests/`. Trimming it is house style. The extraction itself is right: one tag implementation for one meaning is what the decision asks.

### 10. Decision 0055 claims a section that did not change

Affects: [technical design](technical-design.md#composition-validation), [decision 0055](decisions/0055-definition-level-tags-and-tagged-references.md).

The record's "Applied to" entry reads "Technical design: primitive registry and composition, composition validation". The `parameters` and `connectors` rows of [Primitive registry and composition](technical-design.md#primitive-registry-and-composition) were updated well; [Composition validation](technical-design.md#composition-validation) was not. Its pack-admission list is silent on whether a reference's tag demand is re-checked at load. The loader does not re-check it and no test asks it to, so either the section owes a sentence saying the demand is a generation-time check that the published vocabulary record makes re-verifiable, or the claim in the record is loose.

## State of the design review

[Finding 48](review.md#48-every-randomization-level-is-to-be-judged-graphically-and-nothing-says-which-view-in-what-order-or-against-what-vector) is the one open finding, and it is correctly open. Its body was updated to record that decision 0055 answers its third part, that a body could not be rendered to be judged because no revision admitted a surface material; the status table row still reads only "**Open**; the owner's intent is unrecorded". The row is not wrong, but it does not carry the partial answer the body now does.

Findings 1 to 47 and 49 are cleared, and no cleared finding has returned, with one qualification: [finding 20](review.md#20-two-hashing-primitives-where-one-would-do)'s argument is materially reopened by the new mixer of finding 2 above, without the finding itself being reopened.

## Observations, not findings

- The vocabulary is overwritten in place each iteration, so iteration 3's pinned vocabulary `8f2646fb` no longer matches any file in the tree. The row's commit column makes it recoverable and the published pack is content-addressed, so reproduction holds; it holds by git, not by the row.
- `inspect` prints every definition's parameters and no tags, so a revision 3 field the record carries is invisible to the tool whose purpose is reading records back.
- In the rendered picture the gas giant's generated material reads clearly, while the small inner bodies are near-black specks at their rendered size, even after this commit's fill-light rise. That is a reading for iteration 4's verdict, which is the owner's to give.

## What the documents owe

Four applications of decisions already accepted, and one new record.

Applications: [progress.md](progress.md) owes three corrected rows and a completed-steps row 25 with the evidence that now exists (finding 1), and a corrected suite size (finding 6); [src/README.md](../src/README.md) owes the layout update (finding 4); the review view owes its stand-in disclosure and [review.md](review.md)'s finding 48 row owes the partial answer its body already records (finding 5); and `content/templates/basic/vocabulary.json` owes the `for-artifact` tag on templates 3, 4, and 6 before iteration 4 pins a vocabulary hash (finding 3).

New record: the core rasterizer — where a texture recipe becomes pixels, under what version, seeded by what, and whether a mixing construction other than SHA-256 is permitted in Core after [decision 0021](decisions/0021-sha256-stream-derivation.md) (finding 2). Until it exists, `surface-appearance/1` is a frozen format with no record, no pin, and no place in an iteration row.

## The open question

Decision 0021 left Core with one hashing construction on purpose, and `SurfaceAppearance` has added a second, a SplitMix-style integer mixer, as the source of every generated body's face. The documents' position is [finding 20](review.md#20-two-hashing-primitives-where-one-would-do)'s: two hashing primitives where one would do is a fault, and the core "freezes one published hash rather than a construction of its own".

The recommendation is to keep the mixer and record it rather than force per-pixel SHA-256: a new decision that names the raster a versioned rule in the `derive-<name>/<k>` manner of [decision 0037](decisions/0037-derivation-rules-integer-periods-and-orbit-hierarchy.md), freezes `surface-appearance/1` and the mixer's constants as that rule's implementation, states that this is the one exception to decision 0021 and why, and adds the rule's version to what an iteration row pins. The alternative, deriving the lattice from `RandomStream`, keeps one primitive but costs a SHA-256 per lattice cell per pixel, which the review view is unlikely to afford. The choice is the owner's.

## What the documents pass cleared

Added 2026-09-10, after the documents pass this inspection asked for. Every finding above stands as it was written at `ef14630`, because it records what was found; this section records only what has since been done about it, and no finding is cleared by editing it away ([decisions](decisions/README.md)).

| # | State after the pass |
| --- | --- |
| 1 | **Cleared.** M0a exit criteria 1 and 11 describe the built state, criterion 11 staying **Partial** because no geometry, collision, or parameter primitive reaches the adapter; the iteration 4 row carries its pins, its figures, and its reading; and completed step 25 records `ef14630`, run `34491083126`, and the 284 tests ([progress](progress.md#m0a-exit-criteria)). |
| 2 | **Cleared** by [decision 0056](decisions/0056-surface-raster-rule-and-the-boundary-of-stream-derivation.md), which names the image a versioned rule, `derive-surface-raster/1`, freezes `surface-appearance/1` and the mixer's constants as that rule's implementation, states that the pixels are computed in the core against [architecture and ownership](technical-design.md#architecture-and-ownership), and makes a picture-read iteration row pin the rule's version. It is framed as the boundary of [decision 0021](decisions/0021-sha256-stream-derivation.md) — stream derivation and identity-bearing hashing — and not as the exception [the open question](#the-open-question) below proposed, because an exception is a precedent the next per-pixel construction claims where a boundary is a rule its author applies. |
| 3 | **Open.** Authored content, untouched by a documents pass; the fault is recorded against the pins it reproduces from ([progress](progress.md#solar-system-iterations)). |
| 4 | **Cleared.** [src/README.md](../src/README.md) names `CategoryRegistryRevision3`, the fourth grammar version, `TagList`, `Appearance/`, `PrimitiveResources`, and the `Appearance/` test suite. |
| 5 | **Open.** A change to `SystemView.Legend`, which is code. |
| 6 | **Cleared.** The Native platform support row states 284 tests, 253 in Core, 25 in Persistence, and 6 in the command-line suite, and cites the run that evidences them ([progress](progress.md#verification-and-performance-targets)). |
| 7 | **Open.** Code. |
| 8 | **Open.** Code. |
| 9 | **Open.** Code. |
| 10 | **Open, and a question for the owner rather than a fix.** Either [composition validation](technical-design.md#composition-validation) gains a sentence saying that a reference's tag demand is a generation-time check which the published vocabulary record leaves re-verifiable, and that pack admission does not repeat it, or decision 0055's claim on that section is loose. Decision 0055 is accepted and therefore immutable, so only the first is available as an edit, and it settles a design question that is the owner's. |

Two points of the review state above are carried too: [finding 48](review.md#48-every-randomization-level-is-to-be-judged-graphically-and-nothing-says-which-view-in-what-order-or-against-what-vector)'s status row now carries the partial answer its body records and stays open, and [finding 20](review.md#20-two-hashing-primitives-where-one-would-do)'s status records why the new mixer does not reopen it.
