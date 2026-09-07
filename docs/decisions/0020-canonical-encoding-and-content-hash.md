# 0020. Canonical encoding format 1 and the SHA-256 content hash

Date: 2026-09-07. Status: Accepted.

## Context

The [technical design](../technical-design.md#destination-identity-and-determinism) requires "canonical binary encodings and a specified content hash such as SHA-256, not runtime object hashes", and requires packed vectors to use "explicit little-endian fields, lengths, and integrity checks". It fixes no encoder, no integer encodings, no separation between record kinds, no rule for text that enters a hash, and no rendered form for a hash on disk. [Decision 0006](0006-pack-identity-and-allocation.md) says a generated pack derives its 128-bit `pack_id` from the canonical set specification hash without saying how a 256-bit hash becomes a 128-bit identifier, and [decision 0018](0018-data-root-region-encoding-and-save-integrity.md) names content hashes as file and directory names without fixing their spelling.

This is the first thing the primitive-set foundation needs: M0a exit criterion 5 is "verify canonical hashes on Linux and Windows", and nothing above it can be built until the bytes being hashed are fixed. As with [decision 0019](0019-generator-version-1-frozen.md), these are reversal-expensive facts — every package file name, every generated `pack_id`, and every world identity is downstream of them — so they are recorded here rather than left in source.

## Decision

**Format version.** `CanonicalWriter.FormatVersion = 1` in `SpaceExplorer.Core.Shared`. This is a separate axis from `GeneratorVersion`, because save format, generator, and content versions are independent (technical design, persistence and compatibility). Every rule below is frozen under it, and a change to any of them is a format-version increment: it changes the hash of records whose field values have not changed.

**Header and domain.** Every byte string opens with the format version as one byte, then a domain label as length-prefixed bytes. The label names the kind of record, is in `StreamPath` canonical form, and may carry its own version, as in `set-specification/1`. Two record kinds that happen to encode the same field values therefore never share a hash, which is what makes a bare hash safe as an identity.

**Integers.** Fixed-width values are little-endian two's complement at their declared width, assembled bytewise rather than through `BitConverter`, so host byte order is unreachable, as it is in `Mix64`. Variable-width values are LEB128, seven bits per byte with the high bit set on all but the last; signed ones are zig-zag encoded first, mapping 0, -1, 1, -2 to 0, 1, 2, 3. A sequence length is written through a distinct count operation that rejects a negative value where it is written, which is the range check the technical design requires before allocation.

**Framing.** Byte strings and text carry a variable-width length prefix, so `("a", "b")` and `("ab", "")` cannot encode alike. This is the framing counterpart of the length step in the `Mix64` construction.

**Text.** Text admits only the `StreamPath` character set: ASCII letters, digits, `_` and `-`, shared with path segments through one predicate so the two cannot drift apart. No admitted byte has a second Unicode normalisation form, a case-folding rule, or a locale-dependent reading, so no hashed text can vary between machines. Human-readable names are metadata and are never encoded ([decision 0006](0006-pack-identity-and-allocation.md)). Relaxing the set later is a format-version change that leaves every existing encoding valid; tightening it would not be, which is why it starts strict.

**No floating point.** The writer exposes no floating-point operation at all, as `Pcg32` exposes no floating-point sampling. Authoritative quantities are integers or fixed-point ([decision 0010](0010-units-coordinates-and-region-bounds.md)), and a hash that depended on rounding would not survive the move to another machine. A structural test asserts that no such operation appears later.

**Two rules the caller owns.** Fields are written in a fixed declared order, and a collection is sorted into a defined order before it is written. No encoder can enforce either; hashing an unordered iteration is the failure [decision 0008](0008-random-stream-derivation.md) forbids.

**Content hash.** SHA-256 over the canonical bytes and nothing else: no salt, no prefix, no encoding step of its own. It never covers compressed bytes, so a compressor version or platform difference cannot change an identity ([decision 0018](0018-data-root-region-encoding-and-save-integrity.md)). The rendered form is lower-case hexadecimal, and parsing rejects upper case, so one identity has exactly one file name; two spellings would be one file on a case-insensitive file system and two on a case-sensitive one, which is the cross-platform path hazard the technical design requires be tested for.

**Pack identifier.** A generated pack's `pack_id` is the leading 16 bytes of its specification's content hash, so the pack directory name is a visible prefix of the specification hash. An authored pack's random identifier cannot originate in the core, because `Guid.NewGuid` and `System.Random` are rejected there at build time and a seeded stream is not random across authors; the command-line tool supplies the 16 bytes and `PackId.FromBytes` accepts them. `ContentHash` and `PackId` are value types whose default is unset rather than a hash or an identifier of zero, and the writer rejects an unset value.

**What is deliberately not decided.** No set specification record is defined. Its fields follow from the authored templates of M0a exit criterion 1, which do not exist; inventing them now would freeze a shape that criterion 1 would immediately change, and a changed shape changes every derived `pack_id`. The recorded test vector uses a specification-shaped record to exercise the encoding, not to define one.

## Consequences

Easier: anything with a canonical encoding now has an identity, so criterion 5 has something to verify and `pack_id` derivation exists; the rules are few enough that a recorded vector can be read off by hand and checked without running the code.

Harder: every record type must declare a domain label and a field order, and adding a field to a record already published is a format-version increment rather than an edit. Truncating to 128 bits is accepted as decision 0006 specifies; at the expected scale of packs a collision is not a practical concern, and exact content is still given by the revision hash, with full definition identity remaining `(pack_id, primitive_id)`.

What is verified, on Debian 13 x86_64: 79 Core tests and 2 Persistence tests pass, 38 of them new. Two external anchors hold, the published SHA-256 vectors for the empty input and for `abc`, which pin that this is SHA-256 of the given bytes alone. The recorded vector's 47 bytes follow by hand from the rules above, and its hash was computed with a tool outside this repository rather than by the code under test, so the vector is not merely a record of what the implementation does. The suite is mutation-checked and load-bearing: writing fixed-width values big-endian, dropping the length prefix, dropping the domain from the header, taking the pack identifier from the tail of the hash, and dropping the zig-zag step each turn it red.

What is not verified here: the cross-platform half of criterion 5, which is the same suite passing on `windows-latest`. That result is recorded in [progress.md](../progress.md) when the continuous integration run for this change completes, as it was for [decision 0019](0019-generator-version-1-frozen.md). No set specification, package file, or stored record uses the encoding yet, so nothing has been round-tripped through the file system, and there is no reader: the format is written and hashed but never parsed back, which the first stored package will require.

## Applied to

- New files: `src/SpaceExplorer.Core/Shared/{CanonicalWriter,ContentHash,PackId,Hex}.cs` and `tests/SpaceExplorer.Core.Tests/Shared/{CanonicalWriter,ContentHash,PackId}Tests.cs`; `StreamPath.IsPermitted` becomes internal so one predicate serves both
- [Technical design: destination identity and determinism, primitive registry and composition](../technical-design.md#primitive-registry-and-composition)
- [src/README.md](../../src/README.md) and [progress.md](../progress.md)
