# 0022. The P0 exit record precedes the first frozen content format

Date: 2026-09-07. Status: Accepted. Supersedes the no-gate clause of [decision 0015](0015-greybox-prototype.md). Source: owner decision of 2026-09-07 on a review recommendation.

## Context

[Decision 0015](0015-greybox-prototype.md) scheduled the greybox prototype P0 in parallel with M0a for one to two weeks and said it does not gate M0a. Two M0a steps have since landed, the random foundation and the canonical encoding, while `prototypes/` still holds only its README. The next M0a step, the authored template vocabulary and the set specification record of criterion 1, is the first that freezes a content format: every generated `pack_id` is the leading half of that record's hash ([decision 0020](0020-canonical-encoding-and-content-hash.md)), so a change to the record after the first published set changes every identifier downstream. Nothing built so far shows whether choosing two artifacts out of four under a cargo limit is a decision a player can explain, which is the question [review finding 15](../review.md#15-infrastructure-first-plan-versus-fun-first-assessment) said to answer before infrastructure. Resolves [review finding 23](../review.md#23-p0-has-not-started-while-m0a-proceeds).

## Decision

The P0 exit record defined by decision 0015 is due before the first M0a step that freezes a content format. The authored template vocabulary and the set specification record of M0a criterion 1 wait for it. M0a work that freezes no content format may proceed alongside P0: the reader for the canonical format that criterion 3 needs, the exported-build smoke tests joining continuous integration, and the Windows reference machine.

P0 is built on the pinned Godot 4.7.2 .NET stack, as open item 2 of [decision 0016](0016-platform-confirmed-and-toolchain-pinned.md) asks, so that movement, physics, the Compatibility renderer, and the Windows export are exercised before M1. Its location relative to the solution, its scope beyond decision 0015, and who plays it are the subject of the P0 assessment the owner requested on 2026-09-07; if they need a record they take the next number. Decision 0015's isolation rule stands: nothing in `src/` references P0 and P0 references nothing in `src/`. The rest of decision 0015 stands: one to two weeks, three to five players, the exit record states whether each can explain a choice that changed their outcome, and the code is discarded.

## Consequences

Easier: the set specification record is designed knowing what the expedition loop needs, and a failed P0 changes the design before any identifier depends on it. Harder: M0a criterion 1 waits up to two weeks, and the exit record becomes a gate rather than a parallel finding. Verified by the P0 exit record in the [development plan](../development-plan.md#milestones); [progress.md](../progress.md#milestones) tracks it.

## Applied to

- [Development plan: milestones](../development-plan.md#milestones)
- [Progress: milestones, M0a exit criteria, open items](../progress.md#milestones)
- [Review finding 23](../review.md#23-p0-has-not-started-while-m0a-proceeds)
