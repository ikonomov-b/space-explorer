# 0002. Content gate for generated primitive sets; generated-only sets; deferred set algebra

Date: 2026-09-07. Status: Accepted.

## Context

The primitive-set foundation generates building blocks from a small template vocabulary rather than composing authored parts. M0a's exit criteria covered identity, storage, reload, determinism, and set operations, but nothing checked whether generated primitives read as distinct, memorable objects, which the concept requires. Resolves [review finding 2](../review.md#2-random-primitive-sets-are-untested-as-content).

## Decision

M0a gains a content gate. A fixed sample of generated primitives is rendered in the minimal Godot viewer; two or three reviewers sort each into distinct or indistinct against a pass threshold recorded before the review. An automated silhouette comparison from fixed viewpoints runs on the same sample as a near-duplicate detector and is the first application of the artifact-variety check. A failed gate blocks M0b until the vocabulary is revised or this decision is superseded.

Sets contain generated definitions only in the core release. Authored content enters the system as templates and constraints, not as set members. The `provenance` field records the kind of every definition, which is `generated` for all core-release definitions; admitting authored or derived definitions into sets later is a manifest-format revision, not a silent extension.

Cross-package set operations are specified, not implemented, in M0a. The identity-resolution rule stands: union, intersection, and difference across packages resolve or remap full identities before operating on handles. M0a implements lookup and membership; the operations are built when a feature consumes them.

## Consequences

Easier: content quality is judged before the world format depends on the sets, and M0a is smaller. Harder: a pivot to authored set members is a format change rather than a configuration change, so the content gate carries more weight. The gate's threshold and sample size are fixed in M0a's test plan. Verified at the M0a exit in the [development plan](../development-plan.md#milestones).

## Applied to

- [Development plan: milestones](../development-plan.md#milestones) and [verification](../development-plan.md#verification-and-performance-targets)
- [Technical design: primitive registry and composition](../technical-design.md#primitive-registry-and-composition)
- [Technical design: composition validation](../technical-design.md#composition-validation)
- [Technology decision: architecture and first implementation step](../technology-stack.md#architecture-and-first-implementation-step)
