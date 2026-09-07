# 0016. Platform confirmed: Godot 4.7.2 .NET with C# on .NET 10; toolchain pinned; solution scaffolded

Date: 2026-09-07. Status: Accepted. Source: owner instruction of 2026-09-07 to prepare the code, platform, and test structures if an assessment confirmed the platform; the assessment did.

## Context

The technology decision selected Godot 4 .NET with C#, an engine-independent core, and SQLite as a working stack to be validated through M0, and left register entries 2, 3, 6, 8, and 10 as recommended defaults. Decisions 0009 and 0014 deferred the solution file, pinned SDK, central package versions, and the test matrix to M0a start.

The assessment checked the stack against [requirements.md](../requirements.md). Godot is the only engine reviewed that is free to use (R4), has a first-class Linux editor with a Linux-native daily loop (R1, R3), ships native Linux and Windows desktop exports (R2), and supports C# officially with no Python dependency (R5). Unity's Linux editor is second-tier, Unreal's languages are C++ and Blueprints with a heavy Linux install, Stride's editor runs only on Windows, MonoGame and FNA have no editor, and Flax takes a revenue share. The accepted design already compensates for Godot's known weak points: single-precision vectors (region-local fixed-point frame, [decision 0010](0010-units-coordinates-and-region-bounds.md)), a thread-unsafe scene tree (workers return plain data), scene-bound replication nodes (core-owned byte transport, [decision 0003](0003-network-topology-and-transport.md)), and no built-in terrain system (materialized heightfields, [decision 0001](0001-materialize-authoritative-terrain.md)).

## Decision

**Platform.** Godot 4.7.2 stable, .NET edition, with Godot.NET.Sdk 4.7.2 and the 4.7.2.stable.mono export templates. Register entries 2, 3, 6, 8, and 10 of the [technology decision](../technology-stack.md#gap-filling-decisions-for-confirmation) are confirmed. The Compatibility renderer remains the baseline whose final adoption is decided from M0b and M1 measurements.

**Toolchain pins.** .NET SDK 10.0.400 in `global.json` with `latestPatch` roll-forward; every assembly targets `net10.0`. Godot 4.7 requires net8.0 or later; .NET 8 and .NET 9 leave support on 2026-11-10, while .NET 10 is the long-term-support release until 2028-11-14. Package versions are central in `Directory.Packages.props`: Microsoft.Data.Sqlite 10.0.11, Microsoft.CodeAnalysis.BannedApiAnalyzers 5.6.0, xunit 2.9.3, xunit.runner.visualstudio 3.1.5, Microsoft.NET.Test.Sdk 18.9.0, BenchmarkDotNet 0.15.8. Moving a pin is an ordinary change and needs no new record.

**Solution.** One `SpaceExplorer.sln` at the root holds all seven projects laid out by [decision 0009](0009-solution-layout.md). The Godot project points at it with `dotnet/project/solution_directory="../.."`; Godot 4.7 selects the solution that contains its project, so no second solution exists. `Directory.Build.props` enables nullable reference types, implicit usings, warnings as errors, and code-style enforcement. The Game project repeats `TargetFramework` because the Godot editor inserts its minimum into any project file that does not state one.

**Enforcement and tests.** The banned-API analyzer on Core rejects `System.Random`, `Guid.NewGuid`, `DateTime.Now`, `Environment.TickCount`, and `string.GetHashCode` ([decision 0008](0008-random-stream-derivation.md)); the rejection was verified by compiling a violation. Architecture tests assert that Core and Persistence reference no Godot assembly. A persistence test loads the native SQLite library. The exported-build smoke scripts in `tests/SpaceExplorer.Game.Smoke/` export with the Linux or Windows Desktop preset and run the build with `-- --smoke`, which opens SQLite through Persistence and exits 0. The GitHub Actions workflow runs restore, build, and test on Ubuntu and Windows for every push to `main` and every pull request ([decision 0014](0014-tooling-and-licence.md)). Exported-build smoke tests join CI at the M0a gate, once Godot and its templates are provisioned on runners.

**Godot project.** `src/SpaceExplorer.Game/` uses the Compatibility renderer, a root `Main` scene that hosts the smoke entry point and later the M0a viewer, and two x86_64 export presets. The Windows preset disables resource modification so the Windows build cross-exports from Linux without Wine; executing it still requires Windows (R2).

## Consequences

Easier: the daily loop of R1 runs today on Linux: edit, `dotnet build`, `dotnet test`, open the editor, export a Linux build, run the smoke script. Verified on 2026-09-07 on a Debian 13 x86_64 workstation: build with zero warnings, three tests passing, the Linux export printing `SMOKE OK` with Godot 4.7.2, .NET 10.0.11, and SQLite 3.53.3, and a Windows export produced from Linux but not executed. Harder: exported builds bundle the self-contained .NET runtime, about 157 MB for Linux before compression, and the Windows smoke script stays unverified until a Windows machine or runner exists.

Open items carried from the assessment, to be resolved as review findings or decisions rather than silently: (1) Godot has no built-in terrain or level-of-detail system, so a 4,096 m region at 2 m cells, about four million cells, needs a chunked mesh with LOD before M0b's region measurement; (2) the P0 greybox prototype should be built with this Godot project's stack so that movement, physics, the Compatibility renderer, and the Windows export are exercised before M1; (3) the native Windows machine or runner that R2 requires is not yet identified; (4) the technology decision's alternatives table omits Unity, Unreal, Stride, Flax, and MonoGame and should record the comparison above with primary sources so the choice survives challenge.

## Applied to

- New files: `SpaceExplorer.sln`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.gitattributes`, `.github/workflows/ci.yml`, the seven projects under `src/` and `tests/`, and `tests/SpaceExplorer.Game.Smoke/`
- [Technology decision: rationale, architecture, packaging policy, register](../technology-stack.md#gap-filling-decisions-for-confirmation)
- [Development plan: milestones and decisions before production commitments](../development-plan.md#decisions-before-production-commitments)
- [README](../../README.md), [src/README.md](../../src/README.md), `.gitignore`
