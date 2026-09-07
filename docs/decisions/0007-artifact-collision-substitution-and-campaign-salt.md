# 0007. Campaign branch in the world specification; per-artifact substitution on appearance collision

Date: 2026-09-07. Status: Accepted.

## Context

Composition validation compared every new artifact's canonical appearance recipe with the whole campaign and rejected the entire destination on one collision, asking the player for a different specification. Collision probability grows with campaign size over a finite recipe space, so late-campaign players would meet "destination unavailable" errors driven by invisible state. The rule against varying a world by campaign history existed to keep seeds shareable between players, a goal that [decision 0005](0005-discoverer-hosts-every-visit.md) removed. Resolves [review finding 6](../review.md#6-uniqueness-by-rejection).

## Decision

**Campaign salt.** The campaign branch ID is a field of the destination specification and therefore part of its content hash. The same seed produces a different system in another campaign, and a system cannot be reproduced outside its discoverer's campaign. Re-entering a specification within the same campaign still selects the existing world. A forked campaign copies existing worlds unchanged and derives new destinations from its own branch ID.

**Substitution.** Before committing a world, compare its appearance recipes with the campaign's existing artifacts. On a collision, re-sample only the colliding artifact from alternatives of the same family and tier budget, in a fixed order, from a variation stream salted by the campaign branch. Geography, sites, and every other artifact are unchanged. Record each substitution as an entry of artifact path and variation index in the world manifest, so the world reproduces from its specification plus its manifest without replaying campaign history. Substitution precedes valuation, so the committed value reflects the final recipe. Only when the variation attempts are exhausted is the destination rejected with an explicit content-capacity error.

**Canonicalization.** Appearance comparison buckets continuous parameters so near-identical recipes compare equal, and it excludes instance IDs, names, invisible metadata, and irrelevant ordering as before.

## Consequences

Easier: players stop seeing rejections caused by their own collection; the fiction that a system belongs to its discoverer holds in data; the world package stays self-describing. Harder: the generation function takes the set of existing appearance hashes as an input, so determinism tests fix that set alongside the specification; bug reports need the branch ID as one more input. Verified by the destination-validity check in the [development plan](../development-plan.md#verification-and-performance-targets), which gains a simulated 50-world campaign with recorded collision, substitution, and residual-rejection rates.

## Applied to

- [Technical design: destination identity and determinism](../technical-design.md#destination-identity-and-determinism)
- [Technical design: composition validation](../technical-design.md#composition-validation) and [persistence](../technical-design.md#persistence-and-compatibility)
- [Development plan: verification](../development-plan.md#verification-and-performance-targets)
- [Glossary](../glossary.md): destination specification, variation overlay
