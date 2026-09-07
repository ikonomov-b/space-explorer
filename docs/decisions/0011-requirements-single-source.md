# 0011. Requirements document as the single source for platform policy and provenance

Date: 2026-09-07. Status: Accepted.

## Context

The assessment audited "the original nine-point concept" and the technology decision cited "the platform discussion", but neither existed in the repository, so the gaps table and the "user requirement" labels could not be checked. The Linux-first policy was restated in seven files, and the concept carried a platform sentence although the assessment said it contained only the game idea. Resolves [review finding 10](../review.md#10-requirements-provenance-and-repeated-policy-text).

## Decision

[concept.md](../concept.md) is the concept of record; there is no separate original text. Where a refinement was adopted, the concept keeps the original idea inline. Quoted phrases in the assessment's gaps table come from the concept's first draft, which the current concept superseded, and the table is labelled accordingly.

[requirements.md](../requirements.md) is the single source for owner-stated constraints: the platform policy and the design constraints the owner has stated, each with source and date. Other documents state the platform policy in one sentence and link there. The concept contains only the game idea, so its platform sentence moves to requirements.

## Consequences

Easier: one place to change policy; provenance of every "user requirement" label is checkable. Harder: readers of a single document follow one link for the full policy. No verification milestone; this is a documentation rule.

## Applied to

- New [requirements.md](../requirements.md)
- [README](../../README.md), [concept](../concept.md), [assessment](../assessment.md), [development plan](../development-plan.md), [technical design](../technical-design.md), [technology decision](../technology-stack.md), [src/README.md](../../src/README.md)
