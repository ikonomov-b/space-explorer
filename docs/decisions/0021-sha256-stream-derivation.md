# 0021. Generator version 2: SHA-256 stream derivation

Date: 2026-09-07. Status: Accepted. Supersedes the mix construction of [decision 0019](0019-generator-version-1-frozen.md). Supersedes the mixer clause of [decision 0008](0008-random-stream-derivation.md). Source: owner decision of 2026-09-07 on a review recommendation.

## Context

[Decision 0019](0019-generator-version-1-frozen.md) froze a stream derivation built from the SplitMix64 finalizer: absorb the path eight bytes at a time, absorb the length, then finalize twice against two domain constants for the two halves. Its own consequences section records the weakness: "the mix construction is this project's own, not an external standard, so only the two anchors above are outside evidence for it", and the three derivation vectors "were produced by this implementation and therefore freeze it without independently proving it correct".

[Decision 0020](0020-canonical-encoding-and-content-hash.md) then brought SHA-256 into the core for the content hash, so the core held two hashing primitives: one published and externally checkable, one this project's own. A design review recommended collapsing them, and the owner decided to do so.

The window matters. Nothing has been generated: no world, no set, no stored stream. A change to the derivation after the first stored pack would make existing worlds unreproducible, which is why this is settled before M0a criterion 1 rather than after it. `pack_id` is unaffected either way, since it derives from SHA-256 already.

## Decision

**Generator version.** `GeneratorVersion.Current = 2`. Version 1 is superseded and nothing was ever generated under it, so no stored world pins it.

**Derivation.** A stream is derived by hashing a canonical record with SHA-256:

```
bytes     = canonical(domain "random-stream/2", world_seed as u64, utf8(path) as a byte string)
h         = sha256(bytes)
state0    = little-endian u64 of h[0..8]
increment = (little-endian u64 of h[8..16] << 1) | 1
```

The bytes come from `CanonicalWriter` under [decision 0020](0020-canonical-encoding-and-content-hash.md), so the seed's byte order, the length prefix on the path, and the header are the ones already frozen there and nothing in the derivation re-decides them. The length prefix is what does the work the absorbed length did in version 1: it keeps a seed and path pair from hashing the same bytes as a different pair. The domain label carries the generator version, so a later construction takes its own label and cannot collide with this one.

**What is unchanged.** Everything else [decision 0008](0008-random-stream-derivation.md) fixed stands: both halves come from the hash, the low half seeds the state and the high half is forced odd to select the stream, PCG32 XSH-RR is the working generator with the reference implementation's raw-state initialisation, bounded sampling uses rejection, no floating-point sampling exists, and the banned-API analyzer gates the core build. The `StreamPath` canonical form is unchanged, so every path valid under version 1 is valid and canonicalises identically under version 2.

**Mix64 is deleted**, with its tests, rather than left in the tree as dead frozen code that a later use site could reach for.

**Recorded vectors hold literals only.** Bumping the version exposed a recorded canonical-encoding vector that wrote `GeneratorVersion.Current` as one of its fields, so a vector whose purpose is to freeze the encoding of [decision 0020](0020-canonical-encoding-and-content-hash.md) moved when this decision changed an unrelated constant. A vector names its values outright ([review finding 24](../review.md#24-tests-that-pin-nothing-the-recorded-vectors-do-not)).

## Consequences

Easier: the core now freezes one hash function instead of two, and it is a published standard with published test vectors, so a reader checking this project's determinism has one fewer construction to take on trust. The derivation reuses the canonical encoder, so the framing of its inputs is specified in one place rather than twice.

Harder: SHA-256 is slower than a SplitMix64 finalizer. That cost is per derived stream, not per draw, and at the expected scale of thousands of stream paths per world it does not appear in any budget; if a future measurement contradicts that, the answer is fewer derivations, not a faster mixer. The published SplitMix64 anchor leaves the suite along with `Mix64`, so the surviving external anchors are the PCG32 reference demo and the two published SHA-256 vectors.

What is verified, on `ubuntu-latest` and `windows-latest` in continuous integration for `acdc063`: 74 Core tests and 2 Persistence tests pass, so the three new derivation vectors are bit-identical on both operating systems. Their digests were computed outside this repository from the preimage a test documents, so unlike the version 1 vectors they check the implementation rather than only record it. The suite is mutation-checked and load-bearing: swapping the two halves, reading the digest big-endian, changing the domain label, and dropping the shift that forces the increment odd each turn it red.

What is not verified: the same gaps [decision 0019](0019-generator-version-1-frozen.md) left. No cross-process determinism suite exists, because no code yet builds a collection or a canonical record, and no descriptors, recipes, placements, or prices exist to compare.

## Applied to

- `src/SpaceExplorer.Core/Shared/{RandomStream,GeneratorVersion}.cs`; `Mix64.cs` and its tests deleted; new derivation vectors in `tests/SpaceExplorer.Core.Tests/Shared/RandomStreamTests.cs`
- [Technical design: destination identity and determinism](../technical-design.md#destination-identity-and-determinism)
- [Technology decision: selected components](../technology-stack.md#selected-components), [glossary](../glossary.md), [src/README.md](../../src/README.md), [progress.md](../progress.md)
