# 0065. `terrain-heightfield/2` ridges, and registry revision 7 lets a body say how much

Date: 2026-09-11. Status: Accepted. Source: the owner's verdict on surface cycle two at the window on 2026-09-11 — the iteration accepted, with "the surface is nothing like rocky, looks more like smooth curvy surface" named as its open fault — and his instruction of the same day to fix it before the ladder builds on it. Adds a version to the rule [decision 0063](0063-relief-by-terrain-heightfield-registry-revision-6-and-grammar-version-7.md) froze and supersedes no part of it: version 1 is retained and unchanged, and the sample published under it reproduces.

## Context

Cycle two's ground is the right height and the wrong shape. Its amplitudes, wavelengths and octave counts all do what the record says, and its verdict turned on none of them: `terrain-heightfield/1` sums smoothstep-interpolated value noise, and that construction makes dunes. Every feature it can produce is rounded, nothing fractures, and no slope is steeper than its neighbours prepare it to be. Rock is the opposite — ridges, scarps, and talus, where the sharp places are sharp and the flat places between them are flat.

This is a fault of the construction and not of the content, so no vocabulary edit reaches it: raising the amplitude makes bigger dunes and raising the roughness makes bumpier ones. It is also the fault most expensive to leave, because every rung above this one stands on the ground's shape — a biome layer at rung 3 paints it, scatter and sites at rung 4 sit on it, the far field at rung 5 continues it, and chunks at rung 7 divide it. Fixing it now costs a rule version and a sample; fixing it later costs those plus everything built on the old shape.

**What must not follow from the fix.** An icy body flows and flattens, and an ocean body's solid ground is drowned smooth; neither should grow ridges because a rocky one needed them. So the construction cannot simply become ridged — how ridged a body's ground is has to be something the body says, which makes it content and not code, and puts it where [finding 54](../review.md#54-the-registry-the-grammars-and-the-numeric-tables-are-code-where-the-vocabulary-is-content)'s cost is already partly paid: [decision 0063](0063-relief-by-terrain-heightfield-registry-revision-6-and-grammar-version-7.md)'s Consequences note that relief's tunable parameters are the half of it that is already data.

## Decision

**1. `relief-ridging`, a fourth relief parameter, in registry revision 7 (open; recommended and taken).** A fraction of 65,535 on the `planet` category, carrying its unit, its default and the decoder's bounds as [decision 0060](0060-registry-revision-4-units-defaults-and-validation-limits.md) requires of every parameter. Zero is version 1's smooth ground exactly; 65,535 is fully ridged. Its default is zero, so a template that ranges nothing keeps the shape it had.

The alternative, deriving ridging from `body-type` in the rule, was rejected: it puts a content judgement in C# for the sake of avoiding a revision, which is precisely the divergence [finding 54](../review.md#54-the-registry-the-grammars-and-the-numeric-tables-are-code-where-the-vocabulary-is-content) records, and it forecloses a rocky body that happens to be smooth.

**2. `terrain-heightfield/2` folds each octave and blends by that fraction (open as to the construction; recommended and taken).** Per octave, over the signed lattice value `v` of [decision 0063](0063-relief-by-terrain-heightfield-registry-revision-6-and-grammar-version-7.md) clause 6, unchanged:

- `folded = 32,767 − |v|`, so the crease where the smooth field crosses zero becomes the high place: this is what turns a rounded hill into a ridge line.
- `sharpened = folded × folded / 32,767`, which narrows the ridges and broadens the valleys between them, because a squared fold spends most of its range near the bottom.
- `ridged = 2 × sharpened − 32,767`, recentred so it spans the same signed range the smooth value does and the amplitude bound of clause 6 holds unchanged.
- `value = v + ((ridged − v) × ridging / 65,535)`, so a body at zero draws exactly version 1's number and a body at the maximum draws the fold.

Everything else is version 1's, unchanged and shared: the frame, the lattice, the seed, the octave count and weights, the normalisation, and the rounding. What a reader compares between the two cycles is therefore one construction and not a new rule.

**3. Grammar version 8 (follows).** Version 7's rules over registry revision 7, with no production of its own, exactly as version 7 stood to version 6. Revisions 1 to 6 and versions 1 to 7 stay supported, so cycle two and every solar-system iteration reproduce from their records.

**4. The vocabulary says which bodies ridge (open; recommended and taken).** Rocky bodies ridge strongly, icy bodies moderately, ocean bodies barely, and a gas giant not at all — it carries no region. The ranges are authored content, so the next reading tunes them with a file edit and a rerun rather than a rebuild.

**5. What proves it (follows, on decision 0063's own pattern).** A hand case computed outside this repository, as both of version 1's were; the identity that `relief-ridging` of zero reproduces version 1 sample for sample, which is what makes this a blend rather than a replacement; the amplitude bound holding at every ridging as it holds at every roughness; and a recorded digest of a maximal ridged region for both operating systems to compare. Version 1 keeps its two hand cases and its digest.

**6. It is read as its own cycle (follows).** Cycle three is this and nothing else, on decision 0061's rule that a rung adds one capability so a verdict has one cause — the ladder's rung 2 revisited rather than a rung of its own, since it changes the shape of the same capability rather than adding another. The sample is the same twenty-four starter seeds, so cycle three compares with cycle two picture for picture.

## Consequences

Easier: a rocky body can look like rock, and an icy one need not; the shape every later rung stands on is settled before scatter, sites, the far field and chunks are built on it; and how ridged a world is becomes a thing an author tunes rather than a thing a programmer decides.

Harder: a seventh registry revision and an eighth grammar version join those retained, and cycle three re-mints its set and its destinations again; `terrain-heightfield/1` is retained for as long as cycle two's row cites it, which is for good; and this is the third of the four rungs [decision 0061](0061-surface-iterations-by-an-escalating-ladder-registry-revision-5-and-grammar-version-6.md) said would each fire [finding 54](../review.md#54-the-registry-the-grammars-and-the-numeric-tables-are-code-where-the-vocabulary-is-content)'s trigger, and it fires it.

**What must be verified**, on the evidence [progress](../progress.md#surface-iterations) records: registry revision 7's and grammar version 8's frozen hashes beside the earlier ones, with every earlier hash unmoved; version 1 reproducing its two hand cases and its digest; ridging of zero reproducing version 1 exactly; the amplitude bound at every ridging; a recorded digest of a ridged maximal region identical on both operating systems; and the exported-build smoke check still printing `SMOKE OK`.

## Applied to

- [Progress: surface iterations](../progress.md#surface-iterations)
- [Decision 0063](0063-relief-by-terrain-heightfield-registry-revision-6-and-grammar-version-7.md), whose rule this versions and whose version 1 it leaves as it stands
- [Review finding 54](../review.md#54-the-registry-the-grammars-and-the-numeric-tables-are-code-where-the-vocabulary-is-content), which stays open at a recorded cost
