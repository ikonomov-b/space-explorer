# 0039. One set manifest per pack identifier; a differing second manifest is a reproduction failure

Date: 2026-09-07. Status: Accepted. Source: review recommendation taken on the owner's instruction of 2026-09-07 to follow the recommended path.

## Context

A generated `pack_id` is the leading 16 bytes of its specification hash ([decision 0006](0006-pack-identity-and-allocation.md), [decision 0020](0020-canonical-encoding-and-content-hash.md)) so that reproducing a specification reproduces its pack. A set is also identified by its manifest hash, which covers the accepted definitions and the validation result. Publication installed `packs/<pack-id>/<content-hash>.bin` "without overwriting different content" and said nothing about a second manifest with a different hash under the same `pack_id` directory, so a non-deterministic generator, a platform difference, or a retained old build would have installed two sets under one `pack_id` silently, leaving `PrimitiveId` (pack, 7) naming two definitions and a lookup by `pack_id` with no rule for which manifest it meant ([review finding 35](../review.md#35-two-set-manifests-can-install-under-one-pack-identifier)).

## Decision

**One `pack_id`, one manifest.** Publishing a set manifest whose `pack_id` directory already holds a manifest with a different content hash is refused as a reproduction failure. The refusal reports both hashes and the specification hash they share, installs nothing, and leaves the existing manifest untouched. Publishing a manifest whose hash already exists under that `pack_id` is an idempotent no-op, as content-addressed installation already made it.

**The index enforces it.** The SQLite index of verified packages carries a uniqueness constraint on `pack_id` for set manifests, so a bypass of the file-level check fails at the commit-last step of the publish protocol ([decision 0018](0018-data-root-region-encoding-and-save-integrity.md)).

**Lookup is therefore total.** A `pack_id` resolves to exactly one manifest, and every definition the manifest lists has exactly one hash ([decision 0033](0033-generated-definitions-have-one-revision.md)), so `PrimitiveId` resolves to one definition without consulting a reference's hash; the hash remains the verification it was made for.

## Consequences

Easier: a determinism failure is caught at publication, where both manifests are at hand, rather than at some later use of a reference; the Primitive-set foundation check gains a direct test. Harder: a deliberately changed generator that a developer re-runs against an unchanged specification is refused until the specification pins the new generator revision, which is the behaviour decision 0035 requires anyway. Verified by the Primitive-set foundation and Durability checks in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Technical design: primitive sets and compact references](../technical-design.md#primitive-sets-and-compact-references) and [storage location and layout](../technical-design.md#storage-location-and-layout)
- [Development plan: verification](../development-plan.md#verification-and-performance-targets)
- [Glossary: content identity](../glossary.md#content-identity)
- [Review finding 35](../review.md#35-two-set-manifests-can-install-under-one-pack-identifier)
- [Progress: design documentation](../progress.md#design-documentation)
