# 0030. M0a criterion 1 proceeds before the P0 exit record

Date: 2026-09-07. Status: Accepted. Supersedes the criterion 1 wait of [decision 0022](0022-p0-gates-the-first-frozen-content-format.md). Source: owner decision of 2026-09-07.

## Context

[Decision 0022](0022-p0-gates-the-first-frozen-content-format.md) held M0a criterion 1, the authored template vocabulary and the set specification record, until the P0 exit record, because every generated `pack_id` is the leading half of that record's hash and a change to the record after the first published set changes every identifier downstream. The owner has now asked for a primitive data structure carrying an ID, a material/texture, and a compatibility group, generated from authored templates for basic groups such as solar system, planet, and artifact, ahead of P0's three-to-five-player sessions.

## Decision

Criterion 1 proceeds now. The owner accepts decision 0022's named cost directly: if P0 findings later change what a primitive needs to carry, the specification record changes and every `pack_id` derived under the first version is superseded, exactly as decision 0022 described. Decision 0022's carve-out already proceeding alongside P0 (the canonical-format reader, the exported-build smoke tests, the Windows reference machine) is unaffected. P0 itself is unaffected: decision 0015's isolation rule stands, and the P0 exit record is still due before the greybox prototype is judged.

## Consequences

Easier: the primitive definition, its authored template vocabulary, and the generator that produces constraint-valid sets from it can be built and tested now rather than waiting on scheduling three to five player sessions. Harder: the first frozen `set-specification/1` record and every `pack_id` it derives may need to be redone once the P0 exit record lands, exactly the cost decision 0022 was written to avoid; nothing downstream may treat a `pack_id` minted before that record as permanent until P0's findings are in. Verified by the M0a criterion 1 row in [progress.md](../progress.md#m0a-exit-criteria).

## Applied to

- [Progress: milestones, M0a exit criteria](../progress.md#m0a-exit-criteria)
- [Development plan: milestones](../development-plan.md#milestones)
