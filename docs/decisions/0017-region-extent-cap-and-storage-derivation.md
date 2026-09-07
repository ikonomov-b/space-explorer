# 0017. Region extent cap reduced to 2,048 m; per-region storage derived from the constants

Date: 2026-09-07. Status: Accepted. Supersedes the region extent cap of [decision 0010](0010-units-coordinates-and-region-bounds.md); its other constants stand.

## Context

Decision 0010 fixed a 4,096 m region extent cap with 2 m heightfield cells and quoted "about 2 MB raw for a maximal region". Decision 0001 budgets 0.1 to 1 MB of stored authoritative data per region. Both figures came from the table in [review finding 1](../review.md#1-terrain-authority-is-left-as-an-or), which sized heights alone at 2 km with 2 m cells and at 4 km with 4 m cells. For the combination actually adopted, 4,096 m at 2 m with int16 heights and a one-byte biome index, a maximal region is 2,048 × 2,048 × 3 bytes = 12.0 MiB raw, four times the quoted figure for heights alone and six times with the biome map. The three constants are over-determined by raw bytes = 3 × (extent / cell)², so one had to give way. Resolves [review finding 18](../review.md#18-region-storage-constants-are-over-determined).

## Decision

**The extent cap gives way.** The region extent cap is 2,048 m per axis. The 2 m cell size, int16 heights at 1/16 m, region-local int32 fixed-point positions, and the 20 Hz tick from decision 0010 are unchanged. A maximal region is 1,024 × 1,024 cells: 2 MiB of heights plus 1 MiB of biome indices, 3 MiB raw. Areas larger than 2 km per side are adjacent regions, which the travel model already supports; the cap is never raised to enlarge a single region.

**Budget.** The authoritative data of a maximal region compresses to at most 1 MiB, so the 0.1 to 1 MB range of decision 0001 stands as the measured target. The derivation assumes an overall ratio of 3:1 or better: heights at 4:1 to 6:1 after delta prediction and the biome map at 30:1 or better after run-length coding. If M0b measures worse, the biome map moves to 4 m cells, which brings a maximal region to 2.25 MiB raw, before any other constant is touched.

**Why not the others.** The cell size is the constant players feel underfoot: decision 0001 keeps cosmetic displacement out of collision, so 4 m cells would make walkable terrain 4 m facets in an on-foot game. The budget sizes the resource-use gate, the disk footprint of a campaign, and the region transfer to a joining guest. The extent cap was an upper bound that nothing depended on: the short expedition of the [development plan](../development-plan.md#verification-and-performance-targets) lasts 15 to 30 minutes across three sites, and crossing 2,048 m at a suited walking pace of 2 m/s takes about 17 minutes. Halving the cap also quarters the worst-case mesh and level-of-detail workload noted as an open item in [decision 0016](0016-platform-confirmed-and-toolchain-pinned.md).

Sizes in these documents are binary units: 1 MiB = 1,048,576 bytes.

## Consequences

Easier: a 300-region campaign stays near 300 MiB; a guest receives a region in about a second on a home upload link; terrain meshing works on one million cells at most. Harder: region design must fit 2 km per side, and a destination that wants more ground needs more regions. Verified by the M0b region measurement and the resource-use check in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Technical design: units, coordinates, and bounds](../technical-design.md#units-coordinates-and-bounds) and [persistence](../technical-design.md#persistence-and-compatibility)
- [Development plan: core-release contents, milestones, verification, decisions](../development-plan.md#core-release-contents)
- [Glossary](../glossary.md): region
- [Decision records index](README.md): status of decision 0010
