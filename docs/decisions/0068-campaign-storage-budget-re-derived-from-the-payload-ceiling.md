# 0068. The campaign storage budget is re-derived from the payload format's ceiling, not from an assumed ratio

Date: 2026-09-11. Status: Accepted. Source: the owner's ruling of 2026-09-11, "re-derive the budget upward, the derived set is the way", given on the [storage inspection](../inspection-storage.md#since-the-reading)'s finding that measured heights alone sit at the heights-plus-biome target of [decision 0017](0017-region-extent-cap-and-storage-derivation.md). Supersedes the budget clause of decision 0017 — 1 MiB compressed per maximal region, a 300 MiB campaign, the assumed 3:1 ratio, and the 4 m biome fallback — and nothing else of it: the extent cap, the cell, the sample format, and the reasoning for keeping them stand. The 0.1 to 1 MB range of [decision 0001](0001-materialize-authoritative-terrain.md), which decision 0017 kept as the measured target, is replaced as a target by the figures below.

## Context

Decision 0017 derived its budget from a ratio it had not measured: heights at 4:1 to 6:1 after delta prediction and a biome map at 30:1 after run-length coding, 3:1 overall, so 3 MiB raw became 1 MiB compressed and 300 regions became 300 MiB. It said so, and named the M0b measurement that would test it.

The measurement exists. [Decision 0066](0066-generation-writes-to-the-database-and-the-view-only-reads.md)'s `region-payload/1` packs heights by planar prediction at a per-row bit width with no compressor, and [decision 0067](0067-a-destinations-ground-is-generated-when-it-is-composed.md) writes one for every region when a destination is composed. Composing the largest destination of the sample wrote 11 payloads of 369,784 to 1,315,585 B, 17.6% to 62.6% of the 2,101,250 B raw, mean 1,019,551 B, which is 0.97 MiB, and median 1.0 MiB ([inspection](../inspection-storage.md#since-the-reading)). Heights alone therefore sit at decision 0017's target for heights and biome together, and 300 regions at the mean are 292 MiB of a 300 MiB budget before a biome byte. The spread is content: a flat region packs to a sixth of raw and a ridged one to two thirds, so any figure taken from a mean is a figure some region will exceed.

The owner chose the biome as a derived set under [decision 0038](0038-derived-instances.md), to be recorded by the rung 3 record of the surface ladder, so no per-cell biome map is stored and decision 0017's 1 MiB biome map and its 4 m fallback have nothing to refer to.

## Decision

**1. The per-region ceiling is derived from the format and needs no ratio (follows).** A maximal region's heights are 1,025 by 1,025 int16 samples, 2,101,250 B ([decision 0063](0063-relief-by-terrain-heightfield-registry-revision-6-and-grammar-version-7.md) clause 6). `region-payload/1` writes each row as one width byte and that many bits a sample, and an int16 residual needs at most 17 bits, so the format's worst case for any ground is 1,025 rows of 2,180 B plus a header of some sixty bytes: **2,234,560 B, 2.13 MiB, 106% of raw**, whatever the region looks like. That is the ceiling a stored region's ground is budgeted at. Nothing in it is assumed, and it moves only when the sample format or the payload format does.

**2. The expected figure is the measured mean, and a reader deciding on the margin uses the ceiling (follows).** 0.97 MiB a region today, restated in the Resource-use row as the sample grows. The two are kept apart on purpose: the mean sizes a disk, the ceiling sizes a promise.

**3. The campaign budget is 640 MiB (open; the owner's ruling to re-derive upward).** Decision 0017's 300 maximal regions at the ceiling are 639 MiB, rounded up to the next 64 MiB; at the measured mean the same campaign is 292 MiB. Regions per destination measured 0 to 11, mean 4.6, so a destination's ground is 4.5 MiB expected and 23.4 MiB at the ceiling, and 300 regions are some 65 destinations. The alternatives the owner was offered, cheaper heights or fewer regions per campaign, are not taken: heights at 1/16 m are the resolution players stand on, and regions per destination are the grammar's to set for the game's reasons and not the disk's.

**4. The biome adds a derived set and no map (the owner's ruling).** Its stored bytes are the bounded ordered array of accepted derived instances the [persistence table](../technical-design.md#persistence-and-compatibility) already lists for a materialized node, bounded by the per-node derived budget the rung 3 record declares; that record states the per-region figure it adds, under this ceiling, and this record reserves nothing for it because a reservation with no declared bound is a guess. Decision 0017's clause that a worse measurement moves the biome map to 4 m cells is void, there being no map.

**5. What is re-derived and what is not (follows).** The budget is re-derived; no content constant moves: the 2,048 m cap, the 2 m cell, int16 heights at 1/16 m, and decision 0017's walk arithmetic stand. This derivation is repeated, not adjusted, when a rung adds a stored layer to a region or a grammar raises regions per landable body, which is the threshold decision 0067 names.

## Consequences

Easier: the budget rests on a format bound and a measurement rather than an assumed ratio, so no region can exceed it by looking different from the sample; the measured mean is 46% of the ceiling, so the expected campaign has room for the layers the ladder adds; and the guest transfer of decision 0017 keeps its order, a maximal region in about one second at the mean and two at the ceiling on the home upload link that record assumed.

Harder: a campaign may hold 640 MiB of ground where 300 MiB was budgeted, which is the cost the owner chose over cheaper heights or fewer regions; and the deletion sweep of [decision 0040](0040-reference-index-and-deletion-sweep.md) has that much more to reclaim per superseded destination, which the [storage inspection](../inspection-storage.md#3-two-of-the-five-index-tables-are-written-and-never-read) already names as necessary.

**What must be verified**: the Resource-use row of [progress](../progress.md#verification-and-performance-targets) carries the ceiling, the budget, the measured mean and range, and the per-destination figures, and restates the mean as the sample grows; the first payload that exceeds 2.13 MiB, should one ever be written, is a finding against this record's derivation and not a rounding.

## Applied to

- [Technical design: region encoding and compression](../technical-design.md#region-encoding-and-compression)
- [Primitives part 5: the arithmetic that decides it](../primitives.md#the-arithmetic-that-decides-it)
- [Development plan: verification](../development-plan.md#verification-and-performance-targets)
- [Storage inspection: since the reading](../inspection-storage.md#since-the-reading)
- [Decision 0017](0017-region-extent-cap-and-storage-derivation.md), whose budget clause this supersedes
