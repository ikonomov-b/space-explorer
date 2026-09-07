# 0006. 128-bit pack identifiers, one pack per generation run, normative terminology

Date: 2026-09-07. Status: Accepted.

## Context

The registry contract fixed `primitive_id` as a pack-local unsigned 32-bit value but left the format and allocator of `pack_id` open, did not say which pack receives the IDs of a generated set, and required "the same starting allocation state" for independent reproduction, which tied set identity to allocation history. The terms pack, pack revision, set, manifest, package, and handle were used inconsistently. Resolves [review finding 5](../review.md#5-identity-scheme-details).

## Decision

**Pack identifier.** `pack_id` is a 128-bit identifier stored as a 16-byte BLOB. An authored pack receives a random identifier once at creation. A generated pack derives its identifier from the canonical set specification hash, so independent reproduction of the same specification yields the same identifier. A human-readable pack name is metadata and never an identity.

**Allocation.** Each set-generation run publishes exactly one new pack. Its definitions receive `primitive_id` values 1..n in stable generation order, and the set manifest pins the exact revisions of the base packs whose templates were used. Reproduction therefore needs no shared allocation state. Only authored packs keep a persisted allocation ledger, an append-only text file validated for monotonic, non-repeating allocation. Zero stays reserved, identities are never recycled, and overflow is rejected.

**Scale and format.** Four-byte handles in packed recipes are a format detail. The expected scale is hundreds of definitions per set and thousands of composition nodes per world; the byte saving is not a design driver.

**Terminology.** The [glossary](../glossary.md) is normative. Pack, pack revision, allocation ledger, set, set manifest, package, handle, and world are used as defined there in every document.

## Consequences

Easier: independent authors and independent machines never coordinate identifiers; generated sets are reproducible from their specification alone; schema and prose use one vocabulary. Harder: `pack_id` costs 16 bytes wherever a full identity is stored, which reference tables keep out of packed recipes; a set that must add definitions after publication becomes a new pack, which the immutable-set rule already required. Verified by the primitive-set foundation checks in the [development plan](../development-plan.md#verification-and-performance-targets), including matching derived pack identifiers across both operating systems.

## Applied to

- [Technical design: primitive registry and composition](../technical-design.md#primitive-registry-and-composition) and [persistence](../technical-design.md#persistence-and-compatibility)
- [Technology decision: selected components and register entry 5](../technology-stack.md#selected-components)
- [Development plan: core-release contents and verification](../development-plan.md#core-release-contents)
- [Glossary](../glossary.md)
