# 0029. Primitive identity, handle, and cross-package set operations contract

Date: 2026-09-07. Status: Accepted.

## Context

M0a exit criterion 4 is "specify cross-package set operations without implementing them." The technical design already fixes the shape in prose: the full definition identity is `(pack_id, primitive_id)`; a handle is "a package-local unsigned index into that package's reference table... not an identity" (glossary); "for mixed packs/revisions, store an immutable reference table mapping package-local uint handles to full exact identities"; and "union/intersection/difference across packages must resolve or remap full identities first; this rule is specified in M0a and the operations are implemented when a feature consumes them" (technical design, primitive sets and compact references). No code contract exists yet. Unlike M0a criterion 1, none of this depends on the authored template vocabulary P0 gates ([decision 0022](0022-p0-gates-the-first-frozen-content-format.md)): `pack_id` is already frozen ([decision 0006](0006-pack-identity-and-allocation.md), [decision 0020](0020-canonical-encoding-and-content-hash.md)), and a pack-local `primitive_id` range is already fixed independently of what any template generates.

## Decision

**`PrimitiveId`.** A `readonly record struct` pairing a `PackId` with a pack-local `uint` local ID, in `SpaceExplorer.Core.Registry`. `IsUnset` is true when the pack is unset or the local ID is the registry's reserved-invalid zero ([decision 0006](0006-pack-identity-and-allocation.md)), matching this project's convention of an unset default rather than an identity of zero.

**`Handle`.** A `readonly record struct` wrapping a `uint`, nothing more: "not an identity," so it carries no other behavior.

**`ReferenceTable`.** Immutable once built. `Create` takes an ordered list of `PrimitiveId`s; entry `i` resolves from `new Handle((uint)i)`, so the handle is the position, not a separately assigned value — the simplest reading of "index" the prose leaves open. `Create` rejects an unset entry and rejects the same identity appearing twice: nothing in the prose requires this, but two handles naming one identity would make "the canonical handle for X" ambiguous for no benefit, so it is rejected here rather than left to accidentally work. `Resolve` rejects a handle past the end explicitly rather than allowing an out-of-range read.

**`ICrossPackageSetOperations`.** `Union`, `Intersect`, and `Except`, each `(IReadOnlySet<PrimitiveId>, IReadOnlySet<PrimitiveId>) -> IReadOnlySet<PrimitiveId>`, operating on full identities per the prose's "must resolve or remap full identities first." An interface only: no type in Core implements it, matching "the operations are implemented when a feature consumes them" literally. A structural test asserts both the exact signature and that nothing implements it yet, so the day a real feature does, that test is expected to fail and should be deleted, not fixed.

## Consequences

Easier: criterion 4's "no code contract exists" gap closes without waiting on criterion 1; a real set-operations implementation, once a feature needs one, has an already-agreed shape to implement against rather than inventing one under feature pressure. Harder: three judgment calls not explicit in the prose are now load-bearing — handle-as-position, rejecting duplicate identities in a table, and the exact three-method split of "union/intersection/difference" — each is a decision this record makes, not one the technical design already made, so a future design that wants a different handle-assignment scheme changes this record, not just adds to it.

Verified: `dotnet build` with zero warnings; the full suite passes locally, 129 Core and 2 Persistence tests. The `Registry` suite is mutation-checked on two rules: `ReferenceTable.Create` accepting a repeated identity, and `Resolve`'s bounds check being off by one, each of which turns it red.

## Applied to

- New files: `src/SpaceExplorer.Core/Registry/{PrimitiveId,Handle,ReferenceTable,ICrossPackageSetOperations}.cs` and `tests/SpaceExplorer.Core.Tests/Registry/{PrimitiveId,ReferenceTable,ICrossPackageSetOperations}Tests.cs`
- [src/README.md](../../src/README.md) and [progress.md: M0a exit criteria](../progress.md#m0a-exit-criteria)
