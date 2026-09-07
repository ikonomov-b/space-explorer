# Source layout

This repository is still at the documentation-first stage, with no application code. The source tree is laid out to match the [technical design](../docs/technical-design.md). The [technology decision](../docs/technology-stack.md) selects Godot 4 .NET/C# with SQLite as the working stack.

Linux is the primary development environment. Keep the edit/test/generate/profile/build loop Linux-native; validate distributed builds on both Linux and Windows. Routine development must not depend on Windows-only tools.

- `src/generation/` for deterministic primitive-set generation first, then world and artifact generation.
- `src/registry/` for primitive and content registry data.
- `src/persistence/` for save, world, and transaction storage.
- `src/campaign/` for campaign state and ownership rules.
- `src/multiplayer/` for host, guest, and trust-boundary logic.
- `src/rendering/` for presentation and cache layers.
- `src/shared/` for common types and utilities.

Keep domain types, registry rules, and generators independent of Godot. The command-line primitive tool, tests, and Godot game will share those C# libraries; persistence, rendering, and transport provide adapters. The existing folders are responsibility boundaries, not yet .NET projects. Project files, the CLI entry point, and the Godot project will be added during implementation; no executable build commands exist yet.
