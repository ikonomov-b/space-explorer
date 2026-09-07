# Source layout

This repository is still at the documentation-first stage. The source tree is laid out to match the documented architecture in `docs/technical-design.md`.

- `src/generation/` for deterministic world and artifact generation.
- `src/registry/` for primitive and content registry data.
- `src/persistence/` for save, world, and transaction storage.
- `src/campaign/` for campaign state and ownership rules.
- `src/multiplayer/` for host, guest, and trust-boundary logic.
- `src/rendering/` for presentation and cache layers.
- `src/shared/` for common types and utilities.

The folders exist now so implementation can grow without committing to an engine or runtime before the design decisions in the docs are settled.
