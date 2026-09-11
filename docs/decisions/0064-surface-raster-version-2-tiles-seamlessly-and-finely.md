# 0064. `derive-surface-raster/2` tiles seamlessly and finely, because a region lays a tile at its true length

Date: 2026-09-11. Status: Accepted. Source: the owner's reading of surface cycle two at the window on 2026-09-11 — "the distant texture looks good, but the near pattern is unrealistic" — and his choice, from three options put to him, of a rule version over a vocabulary change or a deferral to rung 3. Adds a version to the rule [decision 0056](0056-surface-raster-rule-and-the-boundary-of-stream-derivation.md) froze and supersedes no part of it: version 1 is retained and unchanged, and the content published under it draws as it drew.

## Context

[Decision 0061](0061-surface-iterations-by-an-escalating-ladder-registry-revision-5-and-grammar-version-6.md) clause 3 named this fault before a frame was drawn, and named where it would send the work: "Resolution is therefore not the fault to look for; **periodicity is** … a failure there sends the recipe's scale range or `derive-surface-raster/2` back before relief is built on them." Rung 1 found it and the owner accepted that rung with it open; rung 2 put the same material on slopes, where it is worse, because a walker now sees the near ground at a slant and across a longer run.

The arithmetic of the complaint. A `texture-recipe.scale` of the range registry revision 4 gives it puts a tile at 0.348 m on the sample's body, so a 128-pixel image repeats 5,891 times across a maximal region and every 35 cm under a walker's eye. Two things then read as a grid rather than as ground: the **seam**, because version 1's lattice does not wrap, so the pixel column at the tile's edge is unrelated to the one it abuts; and the **motif**, because version 1's coarsest noise octave is an eighth of the tile and its cells a sixth of it, which is a feature large enough to be recognised when it comes round again.

Neither is a fault of version 1 where version 1 was used. A body drawn far from its own size clamps its repeat to between one and eight ([decision 0057](0057-the-view-draws-what-the-content-stores.md)), so a motif an eighth of a tile is a motif an eighth of a planet, which is what made a gas giant legible as a gas giant in solar-system iteration 4. The fault appears only where a tile is laid at the true metre length its recipe claims, which is what a region does and nothing before rung 1 did.

## Decision

**1. Version 2 wraps the lattice (open; recommended and taken).** The noise and cell lattices are indexed modulo the tile, so a tile abuts its own copy without a seam. This is the half of the fix that removes the grid, the seam being the stronger cue of the two.

**2. Version 2 carries only fine detail (open; recommended and taken).** The coarsest noise octave is an eighth of version 1's, the cells a quarter of the size, and the stripes half the period. No feature is then large enough to be recognised across a repeat, and what a reader sees close up is grain rather than a motif.

**3. Version 1 is retained, unchanged, and still the one the tests pin (follows).** A rule version is frozen at its first stored or vectored result ([decision 0056](0056-surface-raster-rule-and-the-boundary-of-stream-derivation.md)), so version 1 keeps its recorded 64-pixel digest and every call site that wants it asks for it by number. `Raster` therefore takes the version explicitly rather than defaulting to one, so no call site draws a version it did not name.

**4. Which version draws is the adapter's choice and not the content's (open; recommended and taken).** `PrimitiveResources` names version 2, and no stored record moves: a texture recipe stores a pattern, a scale and two colours, and which rule turns those into pixels is a property of the build, which is why [decision 0061](0061-surface-iterations-by-an-escalating-ladder-registry-revision-5-and-grammar-version-6.md) clause 10 has a row pin the raster version beside the registry and the grammar. No vocabulary, set, graph or destination hash moves for this record, and the solar-system rows, which were read under version 1, keep their pins and their verdicts.

The alternative — storing the version in the recipe — would make every existing pack unreadable without a registry revision, for a choice no author has an opinion about.

**5. What it does not fix (follows).** The tile is still 128 pixels at 0.27 cm a texel, so the limit on how close a walker may usefully stand is unchanged; and the material still says nothing about where on the region it lies, which is rung 3's biome layer and not this record's business.

## Consequences

Easier: the near ground reads as ground; the same material is unchanged at the distance the owner said already looked right, because a minified tile is its own average whichever version drew it; and the fault decision 0061 clause 3 predicted is closed at the rung it was predicted for, by the remedy that clause named.

Harder: a second raster version joins the first as a retained implementation for as long as any row cites it ([decision 0035](0035-category-registry-record-and-generator-revision-identifiers.md)); every call site must now name a version, which is one more thing to get wrong in a new call site and one less thing to get wrong silently; and a body in the solar-system view would change appearance if it were pointed at version 2, so the two views draw the same stored material by two rules until someone decides they should not.

**What must be verified**, on the evidence [progress](../progress.md#surface-iterations) records: version 1's recorded 64-pixel digest unmoved, which is clause 3's whole claim; version 2 producing a tile whose opposite edges meet, so the seam is gone by construction rather than by inspection; a side outside 8 to 512 and a version outside 1 to 2 each refused by name; and no set, vocabulary, graph or destination hash moving, since this record changes no stored byte.

## Applied to

- [Progress: surface iterations](../progress.md#surface-iterations)
- [Decision 0056](0056-surface-raster-rule-and-the-boundary-of-stream-derivation.md), whose rule this versions and whose version 1 it leaves exactly as it stands
