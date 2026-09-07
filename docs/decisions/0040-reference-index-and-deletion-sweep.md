# 0040. A data-root reference index for shared content; deletion is an explicit confirmed sweep

Date: 2026-09-07. Status: Accepted. Source: review recommendation taken on the owner's instruction of 2026-09-07 to follow the recommended path.

## Context

[Decision 0031](0031-primitive-complete-composition-and-storage.md) moved materialized payloads and assets into `blobs/`, shared by every campaign alongside `packs/`, and allowed deletion "only when no campaign, world, export, or retained backup depends on it". Nothing recorded the dependants: the persistence table had no reference index, `campaign.db` is per campaign so no single database saw every campaign, and `backups/<branch-id>/` copies are dependants too. Either nothing would ever be deleted, so an abandoned campaign's payloads persist and low-disk recovery could evict only caches, or removing a campaign would delete blobs another campaign still referenced ([review finding 36](../review.md#36-the-shared-content-addressed-stores-have-no-reference-index-and-no-deletion-rule)).

## Decision

**`<data root>/index.db`.** One SQLite database at the data root holds the index of verified immutable packages and blobs that the [technical design](../technical-design.md#persistence-and-compatibility) already required, and the *reference index*: one row per edge from a dependant, a campaign, a world, an export, or a backup, to each package or blob it depends on. It is the database whose uniqueness constraint [decision 0039](0039-one-manifest-per-pack-identifier.md) names. Every row is derivable by scanning the manifests under the data root, so the index is rebuildable and its loss is not a loss of content.

**Maintained in the publish-last step.** The publish protocol of [decision 0018](0018-data-root-region-encoding-and-save-integrity.md) writes and verifies files first and commits index references last; the reference rows for the new manifest's dependency closure are inserted in that same last transaction, so a package is never installed without its edges. Creating a backup, an export, or a campaign fork inserts its edges the same way; removing a campaign or a backup removes its edges and nothing else.

**Deletion is a sweep, explicit and confirmed.** Content with zero references is never removed implicitly. A sweep is a user-initiated operation that lists the zero-reference packages and blobs with their sizes, deletes only what the user confirms, and re-verifies the reference count of each item inside the deleting transaction. Low disk space pauses generation and evicts caches ([decision 0018](0018-data-root-region-encoding-and-save-integrity.md)) and then reports what a sweep would free; it does not sweep.

## Consequences

Easier: two campaigns share a pack safely, an abandoned campaign's content becomes reclaimable without becoming fragile, and the "deleted only when nothing depends on it" rule of decision 0031 has a mechanism. Harder: one more database and one more transaction participant in publication; the index must be rebuilt from manifests when it is missing or fails verification, which the command-line tool provides. Verified by the Durability and Resource-use checks in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Technical design: persistence](../technical-design.md#persistence-and-compatibility) and [storage location and layout](../technical-design.md#storage-location-and-layout)
- [Development plan: verification](../development-plan.md#verification-and-performance-targets)
- [Glossary: campaign and play](../glossary.md#campaign-and-play)
- [Review finding 36](../review.md#36-the-shared-content-addressed-stores-have-no-reference-index-and-no-deletion-rule)
- [Progress: design documentation](../progress.md#design-documentation)
