# 0023. Pcg32 is a sealed class

Date: 2026-09-07. Status: Accepted. Source: owner decision of 2026-09-07 on a review recommendation.

## Context

`Pcg32` was a mutable struct: drawing a value advanced the copy it was called on. Held in a readonly field, read through a list indexer, or bound by a foreach, the struct was copied, the copy advanced, and the original did not, with no compiler diagnostic. A demonstration on 2026-09-07 drew the same value twice from a readonly field and from a list element while a local variable advanced. A silently repeated sequence in authoritative generation is a determinism bug that reproduces perfectly and therefore looks like intended output. Resolves [review finding 21](../review.md#21-pcg32-is-a-mutable-struct).

## Decision

`Pcg32` is a sealed class. Its constants, initialisation, output function, and bounded sampling are unchanged, so every recorded vector holds and the generator version stays at 2: the shape of the type is not one of the algorithms [decision 0021](0021-sha256-stream-derivation.md) freezes. One structural test asserts that the type is not a value type and that a stream read twice through a collection advances.

## Consequences

Easier: no call site can repeat a sequence by copying, so generation code needs no by-reference discipline. Harder: one allocation per derived stream, which is negligible beside the SHA-256 derivation that creates it and is per stream, never per draw. Verified by the structural test and the unchanged recorded vectors in `tests/SpaceExplorer.Core.Tests/Shared/`.

## Applied to

- `src/SpaceExplorer.Core/Shared/Pcg32.cs`, `tests/SpaceExplorer.Core.Tests/Shared/Pcg32Tests.cs`
- [Review finding 21](../review.md#21-pcg32-is-a-mutable-struct)
