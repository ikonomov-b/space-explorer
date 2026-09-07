# 0018. Data root, package layout, region encoding, and save integrity

Date: 2026-09-07. Status: Accepted. Source: owner request of 2026-09-07 for space-saving storage outside the repository and basic protection against manually edited saves.

## Context

The technical design required platform-appropriate writable user-data locations, content-hash deduplication, compressed region data, and stated that checksums detect corruption but cannot prove a save was not edited. It fixed no location, directory layout, encoding, compressor, or deterrent. Resolves [review finding 19](../review.md#19-data-location-encoding-and-save-integrity-unspecified).

## Decision

**Data root.** Persistence owns one root path: the operating system's local application data folder plus `SpaceExplorer`. On Linux that is `$XDG_DATA_HOME/SpaceExplorer`, by default `~/.local/share/SpaceExplorer`; on Windows it is `%LOCALAPPDATA%\SpaceExplorer`. The environment variable `SPACE_EXPLORER_DATA_DIR` overrides it, and tests use temporary directories. Rebuildable caches live under a cache root, `$XDG_CACHE_HOME/SpaceExplorer` (default `~/.cache/SpaceExplorer`) on Linux and `%LOCALAPPDATA%\SpaceExplorer\cache` on Windows, and deleting them never loses a discovery. Roaming or cloud-synced profile folders, the repository, and the install directory are never used. Godot's `user://` directory holds engine settings and logs only, so the command-line tool reaches campaign data without Godot.

**Layout.** Under the data root: `campaigns/<branch-id>/campaign.db` (SQLite: campaign state, transaction ledger, destination index, generation jobs), `campaigns/<branch-id>/worlds/<specification-hash>/` (world package: manifest, artifacts, `regions/<region-id>.bin`), `packs/<pack-id>/<content-hash>.bin` (immutable set packages shared by every campaign), and `backups/<branch-id>/` (rolling recoverable copies). Package files are named by content hash: identical data is stored once and every load verifies it before use. The existing publish protocol stands: write and verify files first, then reference them from the database.

**Region encoding.** Heights are an int16 grid at 2 m cells; each height is predicted as left plus up minus upper-left and the residual is zig-zag encoded as a variable-length integer. The biome/material index is a byte grid run-length coded per row. Anchors, the traversal graph, hazard volumes, and placements are variable-length-integer records. Each layer is compressed with Brotli from `System.IO.Compression`, which ships with .NET on both platforms; the quality level is fixed at M0b from measured size and time, since regions are written once and read many times. The content hash covers the decoded canonical payload, never the compressed bytes, so compressor differences across versions or platforms cannot change identity. Meshes and textures are never stored authoritatively.

**Save integrity.** Nothing stored locally can prove the owner has not edited it; these measures deter casual editing with a database browser or hex editor and no more. Credits and artifact custody are derived from the append-only transaction ledger and cached, never stored as the only copy. An artifact's value is recomputed from its stored inputs under the versioned tables at load and at sale. Every ledger row, world manifest, and artifact record carries an HMAC-SHA256 tag over its canonical bytes, keyed by HMAC-SHA256 of the campaign branch ID under an application constant; every ledger row includes the tag of the previous row. At load the campaign verifies the chain, the tags, and three invariants: every artifact exists at a placement in its world, each world's value total respects its tier bounds, and credits equal the ledger. Any failure sets the campaign's modified flag, itself tagged, which is shown to the host and to every guest at join and never locks the owner out. Extracting the key from the binary defeats the scheme; that is accepted, and a public market remains a server-side problem.

## Consequences

Easier: one path rule serves the game, the tool, and the tests on both operating systems; caches are safe to delete; edited saves are visible to a friend who joins. Harder: 32 bytes and one verification per record, a load-time pass over the ledger, and an application constant that is a known weak point. Verified by the native platform support and durability checks in the [development plan](../development-plan.md#verification-and-performance-targets), which gain edited-save cases, and by the M0b region measurement for the compression assumption of [decision 0017](0017-region-extent-cap-and-storage-derivation.md).

## Applied to

- [Technical design: architecture, persistence, multiplayer trust](../technical-design.md#persistence-and-compatibility)
- [Development plan: verification](../development-plan.md#verification-and-performance-targets)
- [Technology decision: selected components and register entry 12](../technology-stack.md#selected-components)
- [Glossary](../glossary.md): data root, transaction ledger, modified campaign
