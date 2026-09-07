# 0009. Solution layout by assembly; authored content outside the Godot project

Date: 2026-09-07. Status: Accepted.

## Context

The architecture has four consumers, core library, command-line tool, Godot game, and tests, but the source tree held seven responsibility folders with no project boundaries. Godot requires its own project folder with a resource root, and the root `assets/` placeholder would have sat outside it. Once project files reference each other, moving folders touches every project, the Godot resource paths, and continuous integration. Resolves [review finding 8](../review.md#8-folder-layout-versus-assembly-boundaries).

## Decision

The repository is laid out by assembly:

```text
SpaceExplorer.sln                (created at M0a start)
global.json                      pinned SDK (M0a start)
Directory.Build.props            target framework, nullable, warnings as errors, analyzers (M0a start)
Directory.Packages.props         central package versions (M0a start)
src/
  SpaceExplorer.Core/            generation, registry, campaign rules, valuation, random, binary formats
  SpaceExplorer.Persistence/     SQLite adapter and package I/O
  SpaceExplorer.Cli/             set generator, validator, inspector
  SpaceExplorer.Game/            Godot project: project.godot, scenes, rendering and network adapters, assets/
content/                         authored pack sources: templates, ledgers, numeric tables
tests/
  SpaceExplorer.Core.Tests/
  SpaceExplorer.Persistence.Tests/
  SpaceExplorer.Benchmarks/
  SpaceExplorer.Game.Smoke/      exported-build smoke scripts
```

The former responsibility folders become namespaces: generation, registry, campaign, and shared live in Core; persistence is the Persistence project; rendering and multiplayer are adapters inside Game. Core and Persistence are plain class libraries with no Godot reference, and one architecture test asserts that the Core assembly never references the Godot assembly. The target framework is pinned to the one the chosen Godot release supports. Authored content is consumed by the CLI and tests without Godot, so it lives in `content/` at the repository root; the Godot project keeps only runtime assets such as UI, shaders, and materials. The root `assets/` placeholder is retired.

## Consequences

Easier: project references, resource paths, and CI never move; the no-Godot rule is enforced mechanically. Harder: two content locations, `content/` for authored data and the Godot assets folder for runtime assets, must stay distinct. The solution and pinning files are created at M0a start when versions are chosen. Verified by the architecture test and the native platform support check in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- Repository folders and [src/README.md](../../src/README.md)
- [Technology decision: architecture and first implementation step](../technology-stack.md#architecture-and-first-implementation-step)
- [Technical design: architecture and ownership](../technical-design.md#architecture-and-ownership)
