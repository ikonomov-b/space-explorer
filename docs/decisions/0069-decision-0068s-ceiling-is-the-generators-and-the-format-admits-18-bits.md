# 0069. Decision 0068's ceiling is the generator's, not the format's: the format admits 18 bits a residual

Date: 2026-09-11. Status: Accepted. Source: the iteration session's measurement of 2026-09-11 against the `region-payload/1` packer, made within the hour of [decision 0068](0068-campaign-storage-budget-re-derived-from-the-payload-ceiling.md)'s acceptance; accepted by the owner the same day, on the words "accept the recommended". Corrects how decision 0068 clause 1 characterises its ceiling and changes none of its figures and nothing of the owner's ruling. Supersedes clause 1's claims that the ceiling holds "whatever the region looks like" and that "nothing in it is assumed", and nothing else of that record.

## Context

Decision 0068 clause 1 derived its 2.13 MiB ceiling from the statement that an int16 residual needs at most 17 bits, taken from the payload class's own docstring. Both were wrong about the format. The predictor is left plus up less the corner, so a residual is one int16 less two plus one, spans ±131,070, and zig-zagged reaches 262,140, which is 18 bits; under the relief amplitude cap of ±1,024 m, 16,384 units, it still spans ±65,536 and 131,072 zig-zagged, 18 bits at the corner where four neighbouring samples sit at opposite extremes.

Measured against the packer of `2b83d81` on a maximal region: the sample's content reaches a widest row of 13 bits, 796,025 B, 37.9% of raw; the worst the vocabulary can draw, maximum amplitude at the finest wavelength with full ridging, reaches 17 bits, 2,234,490 B, 106.3%, which is decision 0068's figure; the format at 18 bits is 2,308 B a row, 2,365,760 B, 2.26 MiB, 112.6% of raw, and 300 such regions are 677 MiB, over the 640 MiB decision 0068 names. As written, that record's campaign figure is a bound over content this generator produces, and the format can exceed it.

## Decision

**1. 640 MiB stands, as the ceiling over any content the generator can draw (follows).** Seventeen bits a residual is the worst the smoothstep-interpolated lattice of `terrain-heightfield` reaches at 2 m spacing with a 4 m finest wavelength under the amplitude cap, measured at the vocabulary's extremes rather than derived from the format. The figure is checkable by repeating that measurement, and it moves when the rule, the cap, or the sample format does.

**2. The format's own bound is named beside it (follows).** Eighteen bits a residual, 2.26 MiB a region, 677 MiB per 300 regions, reachable only by neighbouring samples at opposite amplitude extremes, which no rule of the surface ladder produces. A payload wider than 17 bits is therefore a finding against the generator or the cap and not a rounding, which is what decision 0068's verification clause meant and now says.

**3. What decision 0068 assumes (follows).** Its derivation assumes the generator's bound and not a compression ratio. The difference that matters is that the first is measured at the extremes and can be re-measured, where the second was guessed; the sentence that said nothing was assumed overstated it.

## Consequences

Easier: a reader deciding on the margin knows which of two numbers to rely on, and why they differ by 37 MiB over a campaign. Harder: nothing; the budget, the ruling, and every applied document keep their figures, and the Resource-use row of [progress](../progress.md#verification-and-performance-targets) records both numbers with the 17-bit figure as the practical ceiling and the 18-bit one named so nobody re-derives it in surprise.

**What must be verified**: the two figures in that row, and the measurement at the vocabulary's extremes made a test rather than a repeated reading: one case in the payload tests packs the pathological field and asserts the widest row is 17 bits, so the day the rule, the amplitude cap, or the sample format moves the ceiling, the suite goes red and this record and decision 0068 are revisited by name.

## Applied to

- [Technical design: region encoding and compression](../technical-design.md#region-encoding-and-compression)
- [Primitives part 5: the arithmetic that decides it](../primitives.md#the-arithmetic-that-decides-it)
- [Storage inspection: since the reading](../inspection-storage.md#since-the-reading)
- [Decision 0068](0068-campaign-storage-budget-re-derived-from-the-payload-ceiling.md), whose clause 1 characterisation this corrects
