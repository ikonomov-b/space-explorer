# 0034. Storage policy is pinned per category by the world and set manifests, not by the definition

Date: 2026-09-07. Status: Accepted. Supersedes the per-definition storage-policy pin of [decision 0031](0031-primitive-complete-composition-and-storage.md). Source: review recommendation taken on the owner's instruction of 2026-09-07 to follow the recommended path.

## Context

[Decision 0031](0031-primitive-complete-composition-and-storage.md) had "each definition pin one of three policies selected by cross-platform determinism and performance measurements". The measurements are M0b work and M0a publishes the definitions first. A definition is a canonical record whose every field enters its content hash ([decision 0020](0020-canonical-encoding-and-content-hash.md)), and under [decision 0033](0033-generated-definitions-have-one-revision.md) a generated definition has one hash for life, so M0b's measured policy would have re-published every M0a pack under new identities. The pin was also stated twice, in the definition and in the world manifest's "storage policies", and the Resource-use check pinned "per category" while the registry table pinned "by the definition". Resolves [review finding 32](../review.md#32-the-storage-policy-sits-inside-the-hashed-definition-that-m0bs-measurement-will-change).

## Decision

**The definition carries no policy field.** A definition's content is what it is, independent of how a given world chooses to store what it produces. The category registry lists, per category, the permitted policies among `regenerate`, `materialize`, and `hybrid`, as the glossary already had it.

**The world manifest pins the policy per category** at generation time, chosen from the versioned measured configuration of decision 0031 by the rule of decision 0031: regeneration only where the retained generator revision reproduces exactly across supported platforms and measured background work stays ahead of exploration, otherwise `hybrid` or `materialize`. The set manifest pins the same for any set-owned payload. An old world keeps the policy under which it was accepted; a re-measurement changes the configuration future worlds are generated under and nothing about an existing world or pack.

**Consequently** the M0a row's "storage policies" means the permitted options per category and the initial measured configuration, both in versioned data, not a field of any definition record.

## Consequences

Easier: M0b's measurement re-mints no identity; the pin is stated in one place per world; the Resource-use check's "per category" wording is now the rule rather than a loose paraphrase. Harder: loading a chunk needs the world manifest's policy table before it can interpret a node's payload, so the manifest is read first, which the load order already requires for its dependency closure. Verified by the Resource-use and Compatibility checks in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Technical design: destination identity and determinism](../technical-design.md#destination-identity-and-determinism), [primitive registry and composition](../technical-design.md#primitive-registry-and-composition), and [persistence](../technical-design.md#persistence-and-compatibility)
- [Development plan: milestones](../development-plan.md#milestones) and [verification](../development-plan.md#verification-and-performance-targets)
- [Glossary: content identity](../glossary.md#content-identity)
- [Review finding 32](../review.md#32-the-storage-policy-sits-inside-the-hashed-definition-that-m0bs-measurement-will-change)
- [Progress: design documentation](../progress.md#design-documentation)
