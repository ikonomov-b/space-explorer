# Source layout and build

The tree is laid out by assembly as fixed in [decision 0009](../docs/decisions/0009-solution-layout.md); the platform confirmation and toolchain pins are in [decision 0016](../docs/decisions/0016-platform-confirmed-and-toolchain-pinned.md). The [technical design](../docs/technical-design.md) and [technology decision](../docs/technology-stack.md) define what each assembly owns. The platform policy is in [requirements.md](../docs/requirements.md), and implementation status is tracked in [progress.md](../docs/progress.md).

| Path | Assembly | Contents |
| --- | --- | --- |
| `src/SpaceExplorer.Core/` | class library, no Godot reference | Generation, registry, complete primitive-composition graphs, campaign rules, valuation, random streams, binary formats, and the network contract. `Shared/` holds the frozen determinism primitives `Pcg32`, `StreamPath`, `RandomStream`, and `GeneratorVersion`, whose stream derivation is SHA-256 over a canonical record ([decision 0021](../docs/decisions/0021-sha256-stream-derivation.md)), and the frozen identity primitives `CanonicalWriter`, `CanonicalReader`, `ContentHash`, and `PackId` ([decision 0020](../docs/decisions/0020-canonical-encoding-and-content-hash.md)). `Networking/` holds the core-owned transport contract, `ITransport` and its `InMemoryTransport` implementation, the `CommandJournal`, and the canonically encoded join handshake ([decision 0028](../docs/decisions/0028-in-memory-transport-command-and-handshake-contract.md)). `Registry/` holds identity and exact references (`PrimitiveId`, `PrimitiveRevisionRef`, `Handle`, `ReferenceTable`, the unimplemented `ICrossPackageSetOperations`), the category registry model with `CategoryRegistryRevision1` and the `CategoryRegistries` list of revisions this build supports ([decision 0035](../docs/decisions/0035-category-registry-record-and-generator-revision-identifiers.md)), and the canonical records `PrimitiveDefinition`, `PrimitiveTemplate`, `TemplateVocabulary`, `SetSpecification`, `SetManifest`, and `PrimitiveSet` ([decision 0042](../docs/decisions/0042-category-registry-revision-1-first-content-records-generator-and-publish-protocol.md)). `Generation/` holds `SetGenerator` and `ParameterSampler`, which draw a set from a vocabulary on per-instance streams. `BannedSymbols.txt` enforces [decision 0008](../docs/decisions/0008-random-stream-derivation.md) at build time. |
| `src/SpaceExplorer.Persistence/` | class library, no Godot reference | `DataRoot` resolves the data root of [decision 0018](../docs/decisions/0018-data-root-region-encoding-and-save-integrity.md); `SetPublisher` writes content-addressed records and commits `index.db` last; `SetLoader` reloads and verifies a pack; `PackageIndex` holds the package, record, and dependant tables of decisions 0039 and 0040 ([decision 0042](../docs/decisions/0042-category-registry-revision-1-first-content-records-generator-and-publish-protocol.md)). World packages, overlays, and caches are later work. |
| `src/SpaceExplorer.Cli/` | console application | Primitive-set generator, validator, and inspector: `registry`, `generate-set`, `list`, `inspect`, `validate`, and `diagnostics`; `VocabularyFile` reads the JSON source form of an authored vocabulary into its canonical record. Commands are in [docs/tools.md](../docs/tools.md#dotnet). |
| `src/SpaceExplorer.Game/` | Godot 4.7.2 .NET project | `project.godot`, the `Main` scene with the exported-build smoke entry point, export presets for Linux and Windows Desktop, the rendering and ENet adapters, and runtime assets under `assets/`. |
| `content/` | data | Versioned authored templates, allocation ledgers, numeric tables, and source mesh/texture dependencies consumed without Godot. `templates/basic/vocabulary.json` is the first vocabulary. |
| `tests/SpaceExplorer.Core.Tests/`, `tests/SpaceExplorer.Persistence.Tests/`, `tests/SpaceExplorer.Cli.Tests/` | xUnit.net | Architecture tests that Core and Persistence never reference Godot, the native SQLite load test, the `Shared/` suite pinning the frozen random foundation and the canonical encoding against their published reference vectors, the `Networking/` suite exercising the in-memory transport, command journal, and join handshake, the `Registry/` and `Generation/` suites exercising identity, the registry and record round trips with their frozen hashes, and deterministic generation, the Persistence suite exercising publication, reload, refusal, and corruption cases against a temporary data root and two registry revisions loaded side by side, and the Cli suite, which launches the tool as a child process for the two-process determinism comparison. |
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

Every command this project runs — build, test, the command-line tool, the Godot editor, the exported-build smoke scripts on both platforms, the documentation tool, and continuous integration — is in [docs/tools.md](../docs/tools.md), with the directory it runs from, what it needs on `PATH`, and what success looks like.
