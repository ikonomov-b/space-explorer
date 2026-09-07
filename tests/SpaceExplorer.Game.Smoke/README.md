# Exported-build smoke tests

Each script exports the Godot project with the preset for its operating system and launches the exported build with `-- --smoke`. The build opens an in-memory SQLite database through `SpaceExplorer.Persistence`, prints `SMOKE OK` with the Godot, .NET, and SQLite versions, and exits with code 0. This is the native packaged smoke test required at the M0a gate; it proves the .NET runtime and the native SQLite library load from an exported build without an installed SDK or editor.

| Script | Runs on | Preset | Exported binary |
| --- | --- | --- | --- |
| `smoke.sh` | Linux | `Linux` | `build/smoke/linux/SpaceExplorer.Game.x86_64` |
| `smoke.ps1` | Windows (Windows PowerShell 5.1 or PowerShell 7) | `Windows Desktop` | `build/smoke/windows/SpaceExplorer.Game.console.exe` |

Requirements: the Godot 4.7.2 .NET editor on `PATH` as `godot`, or its path in the `GODOT` environment variable; the matching 4.7.2 .NET export templates; the .NET SDK from `global.json`. The Windows preset disables resource modification, so exporting the Windows build from Linux needs no Wine; running it still requires Windows ([requirement R2](../../docs/requirements.md)).
