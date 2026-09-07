# 0008. Random stream derivation, rejection sampling, and build-time enforcement

Date: 2026-09-07. Status: Accepted.

## Context

The design pinned PCG32 XSH-RR and required per-path streams derived from the seed and stable paths, but did not say how. Hashing the path into the stream selector alone leaves sibling streams correlated, because PCG32 streams over a shared state are not independent. The "never use" list for randomness and hashing had no enforcement. Fixing either later requires a generator version bump. Resolves [review finding 7](../review.md#7-random-stream-derivation).

## Decision

**Derivation.** Each stream is derived by applying a pinned 64-bit mixer, such as the SplitMix64 finalizer or the murmur3 64-bit finalizer, to the world seed and the canonical UTF-8 bytes of the path. The mixer produces 128 bits: the low half seeds the PCG32 state and the high half, shifted left one bit and forced odd, selects the stream. Path canonicalization, separators, and encoding are pinned with the generator version.

```text
h = mix64(world_seed, utf8(path))
state0    = h.low64
increment = (h.high64 << 1) | 1
```

**Sampling.** Bounded integer sampling uses rejection. Authoritative code never samples floating-point values. The first reference test vector is the PCG32 reference implementation's demo output for seed 42 and stream 54; derivation vectors for fixed seed and path pairs are added under the generator version.

**Enforcement.** The core project uses a banned-API analyzer that rejects `System.Random`, `Guid.NewGuid`, `DateTime.Now`, `Environment.TickCount`, and `string.GetHashCode` at build time. The determinism suite runs generation in two separate processes and compares canonical output, because .NET randomizes string hashing per process and this exposes reliance on dictionary iteration order.

## Consequences

Easier: sibling streams are independent by construction; violations of the never-use list fail the build instead of surfacing as cross-platform mismatches. Harder: one more pinned algorithm and test-vector set to maintain. Verified by the determinism check in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Technical design: destination identity and determinism](../technical-design.md#destination-identity-and-determinism)
- [Technology decision: selected components and register entry 7](../technology-stack.md#selected-components)
- [Development plan: verification](../development-plan.md#verification-and-performance-targets)
