# 0019. Generator version 1: mix construction, path grammar, and bounded sampling frozen

Date: 2026-09-07. Status: Accepted.

## Context

[Decision 0008](0008-random-stream-derivation.md) pinned PCG32 XSH-RR, the assignment of a mixer's low half to the state and high half to the stream selector, rejection sampling, the banned-API analyzer, and the two-process determinism test. It deliberately left three things to implementation: which 64-bit mixer, named only as "such as the SplitMix64 finalizer or the murmur3 64-bit finalizer"; how a seed and a path's bytes combine into 128 bits; and the path canonicalisation, separators, and encoding, which it said are "pinned with the generator version". No generator version existed to pin them under.

Writing the first authoritative code required choosing all three. Every world manifest and primitive set manifest pins the generator version, and every artifact identity is derived through a stream path, so a later change to any of these is a version increment under which existing worlds cannot be reproduced. Choosing them silently inside an implementation would leave the repository's most reversal-expensive new facts recorded only as source code.

## Decision

**Generator version.** `GeneratorVersion.Current = 1` in `SpaceExplorer.Core.Shared`. Everything below is frozen under it.

**Mixer.** The SplitMix64 finalizer, which decision 0008 names as an accepted choice. Its two multipliers and three shift distances are externally published and independently checkable.

**Construction.** The seed is the running value; the input is absorbed eight bytes at a time as explicit little-endian 64-bit chunks, zero-padding a trailing partial chunk, finalizing after each; the input length is then absorbed; the result is finalized twice more against two domain constants to yield the halves. The state domain is the golden-ratio gamma `0x9E3779B97F4A7C15`, the stream domain is xxHash64 `PRIME64_2` `0xC2B2AE3D27D4EB4F`. Chunk assembly is written out bytewise rather than delegated to `BitConverter`, so host byte order cannot enter a stored or transmitted encoding. Absorbing the length is load-bearing: without it, inputs differing only in trailing zero bytes pad to the same final chunk and collide.

**Path grammar.** Separator `/`. Segments are non-empty runs of `[A-Za-z0-9_-]`, with no leading, trailing, or repeated separator. Comparison is case-sensitive and no Unicode normalisation is applied; the ASCII-only restriction is what makes that safe, since no admitted byte has a second normalisation form, a case-folding rule, or a locale-dependent reading. Encoding is UTF-8, which under this restriction is byte-identical to ASCII. Paths are generator-built internal identifiers, never player input, so the restriction costs nothing. It can be relaxed later without invalidating a single existing path, and cannot be tightened without invalidating some; that asymmetry is why it starts strict.

**Bounded sampling.** The reference implementation's `pcg32_boundedrand_r` rule: reject while the draw is below `2^32 mod bound`, computed in 32-bit wraparound arithmetic, then take the modulo. A zero bound is rejected as an empty range. The type exposes no floating-point sampling method at all, rather than documenting that one should not be used.

**Two initialisations.** `Pcg32.FromState` assigns state and increment directly, as decision 0008 specifies, and is what generation uses. `Pcg32.FromReferenceSeed` reproduces the reference `pcg32_srandom_r` routine and exists only so the output function can be checked against the published vector, which is stated in terms of that routine. Conflating the two would silently change every derived stream.

**Test vectors.** Two are externally anchored: the PCG32 reference demo output for seed 42 and stream 54, beginning `0xa15c02b7, 0x7b47f409`, which decision 0008 names as the first reference vector; and the SplitMix64 first output for seed 0, `0xE220A8397B1DCDAF`, which pins the finalizer constants. Three derivation vectors for fixed seed and path pairs are recorded as decision 0008 requires.

## Consequences

Easier: the specifics decision 0008 delegated are now written down where a reader looks for them, so a future change presents itself as a version increment rather than as an edit. Sibling streams are independent by construction. No authoritative draw can depend on host byte order, locale, Unicode normalisation, case folding, or floating-point rounding, because none of those inputs is reachable from the frozen code path.

Harder: the mix construction is this project's own, not an external standard, so only the two anchors above are outside evidence for it. The three derivation vectors were produced by this implementation and therefore freeze it without independently proving it correct.

What is verified: the 41 tests pass on Debian 13 x86_64 and on `windows-latest` in continuous integration, so the recorded derivation vectors and both external anchors hold bit-for-bit on both operating systems. That is genuine cross-platform evidence, and it is the first of it in this project, but it covers only these primitives: the mixer, the path canonicalisation, the PCG32 output function, and bounded sampling. The enforcement of decision 0008 was re-checked by compiling a `System.Random` use in Core and observing `RS0030`. The suite was mutation-checked: perturbing the LCG multiplier, dropping the length step, and swapping the two domain constants each turn it red.

What is not verified: cross-process determinism, since the two-process suite decision 0008 requires exists to expose reliance on dictionary iteration order, and no code here builds a collection. Nor is the matching-canonical-data comparison of the [development plan](../development-plan.md#verification-and-performance-targets) satisfied, because no canonical output exists to compare. Both arrive with the first generated output.

The PCG32 and SplitMix64 anchor values should be confirmed against their published sources by a reader with those sources to hand; they were reproduced here from an implementation written against the algorithms, which is corroboration rather than an independent check.

## Applied to

- New files: `src/SpaceExplorer.Core/Shared/{GeneratorVersion,Mix64,Pcg32,StreamPath,RandomStream}.cs` and `tests/SpaceExplorer.Core.Tests/Shared/{Pcg32,Mix64,StreamPath,RandomStream}Tests.cs`
- [src/README.md](../../src/README.md), [README](../../README.md)
- [Development plan](../development-plan.md) and [technology decision](../technology-stack.md): status sentences
- [Glossary](../glossary.md): generator version, stream path
