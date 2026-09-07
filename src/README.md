# Source layout

This repository is at the documentation-first stage, with no application code. The tree is laid out by assembly as fixed in [decision 0009](../docs/decisions/0009-solution-layout.md); the [technical design](../docs/technical-design.md) and [technology decision](../docs/technology-stack.md) define what each assembly owns.

| Path | Assembly | Contents |
| --- | --- | --- |
| `src/SpaceExplorer.Core/` | class library, no Godot reference | Generation, registry, campaign rules, valuation, random streams, binary formats, and the network contract. Former `generation`, `registry`, `campaign`, and `shared` folders are namespaces here. |
| `src/SpaceExplorer.Persistence/` | class library, no Godot reference | SQLite adapter through `Microsoft.Data.Sqlite` and set/world package I/O. |
| `src/SpaceExplorer.Cli/` | console application | Primitive-set generator, validator, and inspector. |
| `src/SpaceExplorer.Game/` | Godot 4 .NET project | `project.godot`, scenes, the rendering adapter, the ENet transport adapter, and runtime assets under `assets/`. |
| `content/` | data | Authored templates, allocation ledgers, and numeric tables consumed without Godot. |
| `tests/SpaceExplorer.Core.Tests/`, `tests/SpaceExplorer.Persistence.Tests/` | xUnit.net | Correctness suites, including the two-process determinism test and the architecture test that Core never references Godot. |
| `tests/SpaceExplorer.Benchmarks/` | BenchmarkDotNet | Core measurements. |
| `tests/SpaceExplorer.Game.Smoke/` | scripts | Exported-build smoke tests on Linux and Windows. |

The platform policy is in [requirements.md](../docs/requirements.md). The solution file, `global.json`, `Directory.Build.props`, and `Directory.Packages.props` are created at M0a start, when the Godot release, SDK, and package versions are pinned. No executable build commands exist yet.
