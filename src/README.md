# Source layout and build

The tree is laid out by assembly as fixed in [decision 0009](../docs/decisions/0009-solution-layout.md); the platform confirmation and toolchain pins are in [decision 0016](../docs/decisions/0016-platform-confirmed-and-toolchain-pinned.md). The [technical design](../docs/technical-design.md) and [technology decision](../docs/technology-stack.md) define what each assembly owns. The platform policy is in [requirements.md](../docs/requirements.md), and implementation status is tracked in [progress.md](../docs/progress.md).

| Path | Assembly | Contents |
| --- | --- | --- |
| `src/SpaceExplorer.Core/` | class library, no Godot reference | Generation, registry, campaign rules, valuation, random streams, binary formats, and the network contract. `Shared/` holds the frozen determinism primitives `Pcg32`, `Mix64`, `StreamPath`, `RandomStream`, and `GeneratorVersion` ([decision 0019](../docs/decisions/0019-generator-version-1-frozen.md)). `BannedSymbols.txt` enforces [decision 0008](../docs/decisions/0008-random-stream-derivation.md) at build time. |
| `src/SpaceExplorer.Persistence/` | class library, no Godot reference | SQLite adapter through `Microsoft.Data.Sqlite` and set/world package I/O. `SqliteRuntime` reports the native library version for diagnostics and smoke tests. |
| `src/SpaceExplorer.Cli/` | console application | Primitive-set generator, validator, and inspector. Only `diagnostics` exists so far. |
| `src/SpaceExplorer.Game/` | Godot 4.7.2 .NET project | `project.godot`, the `Main` scene with the exported-build smoke entry point, export presets for Linux and Windows Desktop, the rendering and ENet adapters, and runtime assets under `assets/`. |
| `content/` | data | Authored templates, allocation ledgers, and numeric tables consumed without Godot. |
| `tests/SpaceExplorer.Core.Tests/`, `tests/SpaceExplorer.Persistence.Tests/` | xUnit.net | Architecture tests that Core and Persistence never reference Godot, the native SQLite load test, the `Shared/` suite pinning the frozen random foundation against its published reference vectors, and later the two-process determinism test. |
| `tests/SpaceExplorer.Benchmarks/` | BenchmarkDotNet | Core measurements; none yet. |
| `tests/SpaceExplorer.Game.Smoke/` | scripts | `smoke.sh` and `smoke.ps1` export the game and run the exported build's smoke check on Linux and Windows. |

## Toolchain

| Component | Pinned to | Recorded in |
| --- | --- | --- |
| .NET SDK | 10.0.400 with `latestPatch` roll-forward; all assemblies target `net10.0` | `global.json`, `Directory.Build.props` |
| Godot | 4.7.2 stable, .NET edition, plus the 4.7.2.stable.mono export templates | `Godot.NET.Sdk/4.7.2` in `SpaceExplorer.Game.csproj` |
| NuGet packages | Central versions | `Directory.Packages.props` |

### Linux setup

Verified 2026-09-07 on Debian 13 x86_64. All steps are user-local and need no root.

1. .NET SDK: `curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version 10.0.400 --install-dir ~/.dotnet`, then export `DOTNET_ROOT=$HOME/.dotnet` and add `~/.dotnet` to `PATH` in your shell profile.
2. Godot editor: download `Godot_v4.7.2-stable_mono_linux_x86_64.zip` from the 4.7.2-stable GitHub release, unzip it, keep the `GodotSharp` folder next to the binary, and put the binary on `PATH` as `godot`.
3. Export templates: download `Godot_v4.7.2-stable_mono_export_templates.tpz`, unzip it, and move the contents of `templates/` to `~/.local/share/godot/export_templates/4.7.2.stable.mono/`.
4. C# editor: any Linux-native one; set it as Godot's external editor under Editor Settings, Dotnet, Editor.

### Windows setup

Same versions: .NET SDK 10.0.400, `Godot_v4.7.2-stable_mono_win64.exe`, and the templates under `%APPDATA%\Godot\export_templates\4.7.2.stable.mono\`. These steps are recorded as tested once the first Windows gate runs ([requirement R2](../docs/requirements.md)).

## Runtime data

The game and the command-line tool write player data to the data root of [decision 0018](../docs/decisions/0018-data-root-region-encoding-and-save-integrity.md): `~/.local/share/SpaceExplorer` on Linux (honouring `XDG_DATA_HOME`) and `%LOCALAPPDATA%\SpaceExplorer` on Windows, with rebuildable caches under `~/.cache/SpaceExplorer` or `%LOCALAPPDATA%\SpaceExplorer\cache`. Set `SPACE_EXPLORER_DATA_DIR` to redirect the data root; tests use temporary directories. Nothing is written inside the repository.

## Commands

From the repository root:

```sh
dotnet build                                          # all seven projects; warnings are errors
dotnet test                                           # architecture and native-library tests
dotnet run --project src/SpaceExplorer.Cli -- diagnostics
godot --path src/SpaceExplorer.Game --editor          # open the game project in the editor
tests/SpaceExplorer.Game.Smoke/smoke.sh               # export the Linux build and run its smoke check
dotnet run --project tests/SpaceExplorer.Benchmarks -c Release -- --filter '*'
```

On Windows, `powershell -ExecutionPolicy Bypass -File tests\SpaceExplorer.Game.Smoke\smoke.ps1` replaces the shell script. Continuous integration (`.github/workflows/ci.yml`) runs restore, build, and test on Ubuntu and Windows for every push to `main` and every pull request.
