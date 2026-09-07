# 0033. A generated definition has one revision for life; revisions belong to authored template packs

Date: 2026-09-07. Status: Accepted. Supersedes the definition-revision clause of [decision 0031](0031-primitive-complete-composition-and-storage.md) for generated definitions. Source: review recommendation taken on the owner's instruction of 2026-09-07 to follow the recommended path.

## Context

[Decision 0031](0031-primitive-complete-composition-and-storage.md) made every reference an exact one, `PrimitiveId` plus definition content hash, because "a compatible revision of the same conceptual definition retains its ID and receives a new hash". [Review finding 31](../review.md#31-a-generated-definition-can-never-receive-a-second-revision) showed that this event has no producer: every core-release definition is generated ([decision 0002](0002-primitive-set-content-gate.md)), a generated pack is published exactly once per run with IDs 1..n and no ledger ([decision 0006](0006-pack-identity-and-allocation.md)), and worlds draw only from saved sets. A changed template or asset flows through a new specification, a new `pack_id`, and new IDs. The Compatibility check's "revise a primitive without changing its stable ID" therefore described a test that could not run, and whether one reference table may hold two revisions of one `PrimitiveId`, which [decision 0029](0029-primitive-identity-handle-and-cross-package-set-operations-contract.md) rejected, was undecided for the table the M0a immediate step must rewrite.

## Decision

**One revision per generated definition.** `(pack_id, primitive_id)` of a generated pack names exactly one content hash, ever. A change to what a generated definition contains is a new specification, a new pack, and new IDs; there is no re-publication of a generated pack.

**Revisions are an authored-template concern.** Authored template packs keep the rule of decision 0031: a compatible change to the same template retains its template ID and receives a new hash; a different concept receives a new ID; the append-only ledger of decision 0006 records allocation. Templates remain generation inputs, not runtime primitives.

**The hash stays in every exact reference.** Its role for a generated definition is verification, not disambiguation: a loaded definition whose hash differs from the referenced one is a reproduction failure or corruption and is refused, never substituted. The same reference format serves template revisions, where the hash does distinguish revisions, so one format covers both.

**A reference table rejects two entries with one `PrimitiveId`.** The rule of decision 0029 stands under exact references, because one ID has one hash.

**Verification is reworded.** "Two revisions of one ID" is tested on authored template packs and on a reference table offered two entries for one `PrimitiveId`. The Compatibility check becomes: save a world, expand the registry, publish a new set from revised templates, revisit visited and unvisited chunks with cold and warm caches, and confirm the old world still resolves its old packs and exact references unchanged.

## Consequences

Easier: the immediate step's `ReferenceTable` rewrite keeps its duplicate rule, the exact-reference format needs no revision-ordering field, and old worlds cannot acquire a "newer revision" of anything by construction. Harder: the 32-byte hash per table entry and dependency is retained for a verification role; if M0b measurement shows it dominates package size, the single-pack membership form of the [technical design](../technical-design.md#primitive-sets-and-compact-references), a sorted `uint` vector under a manifest that supplies every hash, is the remedy rather than dropping the hash from mixed tables. Verified by the Primitive-set foundation and Compatibility checks in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Technical design: primitive registry and composition](../technical-design.md#primitive-registry-and-composition) and [primitive sets and compact references](../technical-design.md#primitive-sets-and-compact-references)
- [Development plan: verification](../development-plan.md#verification-and-performance-targets)
- [Glossary: content identity](../glossary.md#content-identity)
- [Review finding 31](../review.md#31-a-generated-definition-can-never-receive-a-second-revision)
- [Progress: design documentation](../progress.md#design-documentation)
