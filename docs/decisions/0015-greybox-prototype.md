# 0015. Disposable greybox expedition prototype in parallel with M0a

Date: 2026-09-07. Status: Accepted.

## Context

The assessment recommended testing whether destination and artifact choices are meaningful before committing to infrastructure, yet the first playable in the plan was M1, after two milestones of identity, storage, and determinism work. Resolves [review finding 15](../review.md#15-infrastructure-first-plan-versus-fun-first-assessment).

## Decision

A disposable greybox prototype, P0, runs in parallel with M0a for one to two weeks in a `prototypes/` folder that nothing in `src/` may reference. It hard-codes one landing region with three sites and four artifacts, a cargo limit that forces choosing two, a return, a sale, and a second destination choice. It has no generation and no persistence. Three to five players play it; the record states whether each can explain a choice that changed their outcome and whether they choose another trip. The findings feed M1's design and the code is discarded. P0 does not gate M0a.

## Consequences

Easier: the expedition loop is tested while the foundations are still cheap to change. Harder: one to two weeks of parallel effort. Verified by the P0 exit record in the [development plan](../development-plan.md#milestones).

## Applied to

- [Development plan: milestones](../development-plan.md#milestones)
- [Assessment: relevance and recommendation](../assessment.md#relevance-and-recommendation)
- New `prototypes/README.md`
