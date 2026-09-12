# External tools: how to call them

Written 2026-09-07. Every external program this project drives, with the exact command, the directory it runs from, what it needs on `PATH`, and what success looks like. It exists so no one has to re-derive an invocation or hunt for an install path.

Scope: this document is about *calling* the tools. Installing them and the versions they are pinned to are in [src/README.md](../src/README.md#toolchain) and [decision 0016](decisions/0016-platform-confirmed-and-toolchain-pinned.md); what each command is evidence *for* is in [progress.md](progress.md). Invocations are single-sourced here, so other documents link to this one instead of repeating a command.

Verified on the reference workstation: Debian 13 x86_64, .NET SDK 10.0.400, Godot 4.7.2 stable .NET, git 2.47.3, gh 2.100.0.

## Quick reference

Every command runs from the repository root unless stated otherwise.

| Tool | Canonical call | Detail |
| --- | --- | --- |
| `dotnet` | `dotnet build` / `dotnet test` | [below](#dotnet) |
| `godot` | `godot --path src/SpaceExplorer.Game --editor` | [below](#godot) |
| Smoke scripts | `tests/SpaceExplorer.Game.Smoke/smoke.sh` | [below](#exported-build-smoke-scripts) |
| Docs tool | `dotnet run tools/docs.cs -- --check` | [below](#the-documentation-tool-toolsdocscs) |
| Stats tool | `dotnet run tools/stats.cs` | [below](#the-statistics-tool-toolsstatscs) |
| `git` | `git ls-files -z -- '*.md'` (used by the docs tool) | [below](#git) |
| `gh` | `gh run list --branch main --limit 5` | [below](#gh-continuous-integration-status) |
| CI | `.github/workflows/ci.yml` | [below](#github-actions) |

## Environment first: `PATH` and `DOTNET_ROOT`

The .NET SDK is a user-local install at `~/.dotnet`, put on `PATH` by `~/.bashrc`. Debian's `~/.bashrc` returns early for non-interactive shells, so **`dotnet` is not on `PATH` in scripts, hooks, `bash -c` invocations, or agent shells**, only in interactive terminals. Export it, or call the binary by absolute path:

```sh
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$HOME/.dotnet:$HOME/.dotnet/tools"
# or, without touching the environment:
~/.dotnet/dotnet --version    # 10.0.400
```

This applies to Godot too: `Godot.NET.Sdk` shells out to MSBuild, so a Godot build or export from a shell without `dotnet` on `PATH` fails inside the engine rather than at the prompt.

`godot` needs no such setup here — `~/.local/bin/godot` resolves in non-interactive shells and symlinks to `~/.local/opt/godot-4.7.2-mono/Godot_v4.7.2-stable_mono_linux.x86_64`. Use that absolute path as the fallback, or set `GODOT` (the smoke scripts read it).

## `dotnet`

SDK 10.0.400, pinned in `global.json` with `latestPatch` roll-forward; every assembly targets `net10.0`. Package versions are central in `Directory.Packages.props`. `Directory.Build.props` makes warnings errors, so a warning fails the build.

```sh
dotnet --version                       # 10.0.400
dotnet --list-sdks                     # 10.0.400 [/home/bobi/.dotnet/sdk]

dotnet restore                         # solution-wide; SpaceExplorer.sln is picked up implicitly
dotnet build                           # every project, zero warnings expected
dotnet build --no-restore              # what CI runs after its own restore
dotnet test                            # all test projects
dotnet test --no-build                 # what CI runs after its own build
dotnet test tests/SpaceExplorer.Core.Tests                        # one project
dotnet test tests/SpaceExplorer.Cli.Tests                         # launches the tool as a child process, twice
dotnet test --filter FullyQualifiedName~CanonicalWriter           # one class or method
```

Applications:

```sh
dotnet run --project src/SpaceExplorer.Cli -- diagnostics
# runtime  .NET 10.0.11
# platform linux-x64 (Debian GNU/Linux 13 (trixie))
# sqlite   3.53.3

dotnet run --project src/SpaceExplorer.Cli -- registry           # every supported registry revision, its hash, and its categories
dotnet run --project src/SpaceExplorer.Cli -- grammar            # every supported grammar version, with its bounds and rules
dotnet run --project src/SpaceExplorer.Cli -- generate-set --vocabulary content/templates/basic/vocabulary.json --seed 42
# publishes to the data root (decision 0018); prints pack, manifest hash, definition count
# add --data-root <dir> to publish elsewhere, --retries <n> to override the vocabulary's budget
dotnet run --project src/SpaceExplorer.Cli -- list               # published sets, graphs, destinations
dotnet run --project src/SpaceExplorer.Cli -- inspect <pack-id>  # reload without generating; print every definition
dotnet run --project src/SpaceExplorer.Cli -- validate <pack-id> # exit 0 when every record verifies; 1 with the reason otherwise

dotnet run --project src/SpaceExplorer.Cli -- compose --set <pack-id> --domain solar-system --seed 7
# composes a graph from a published set and publishes it; prints the graph pack, hash, instance count, and depth
# --domain takes solar-system or artifact; the grammar version follows the set's registry revision
dotnet run --project src/SpaceExplorer.Cli -- inspect-graph <pack-id>   # reload without composing; print every instance
dotnet run --project src/SpaceExplorer.Cli -- validate-graph <pack-id>  # exit 0 when the graph and its set verify

dotnet run --project src/SpaceExplorer.Cli -- describe <pack-id> --tier starter
# the derived system description of a published graph, then its verdict against that tier's rules
dotnet run --project src/SpaceExplorer.Cli -- iterate --set <pack-id> --seeds 1-24 --tier starter
# one generation iteration: composes a system per seed without publishing, describes each, and prints
# the pins that reproduce it, every verdict, and the sample's totals and refusal counts
dotnet run --project src/SpaceExplorer.Cli -- destination --tier starter --seed 7
# the two destination levers: loads the destination they name when the data root holds it, otherwise
# draws systems on seeds derived from the tier and the seed until one satisfies that tier's rules,
# publishes it, and describes it either way; the printed pack is the destination to reopen or draw
# (decision 0053). Publishing composes the ground too: every region's region-payload/1 is generated
# and written after the graph and before the destination record, so a destination on disk is complete
# or absent, and a first compose prints a ground line, "ground  11 region(s), 11 generated, 10.70 MiB
# written", that a load of the same levers never prints (decision 0067). --no-publish composes without
# storing and grounds nothing, for a sweep of many seeds; --set names the set when more than one is
# published. A destination pins the tier profile whose demands its verdict met and the region limits
# its landing verdicts used, so its pack identifier moves when either is tuned (decisions 0058, 0059)

dotnet run --project src/SpaceExplorer.Cli -- --help   # usage; exits 0
                                                       # unknown command exits 2; a refused input exits 1
```

The `--` separator matters: everything before it is for `dotnet`, everything after it is for the program.

**`--tier second` cannot be composed today** and is refused by name before the first draw, exit 1, rather than after exhausting the bounded retry of [decision 0052](decisions/0052-the-two-levers-compose-a-destination-and-a-review-view-shows-it.md). The tier profile's second demand is at least one life-bearing body ([decision 0044](decisions/0044-tiered-system-composition-one-star-and-explorability-by-validation.md), [decision 0058](decisions/0058-tier-rules-are-a-pinned-record.md)), and no category registry revision defines a life category, so no system can bear life and the demand fails on every draw. It applies to `destination`, `iterate`, and `describe --tier second`, and to `--system --tier second` in the review view below. Use `starter` until a registry revision carries life ([review finding 52](review.md#52-life-is-a-structural-absence-so-the-second-tier-can-never-be-composed)).

Two build knobs worth knowing rather than rediscovering. `CI=true` turns on `ContinuousIntegrationBuild`, so `CI=true dotnet build` reproduces the CI build locally. And `src/SpaceExplorer.Core/BannedSymbols.txt` is enforced at build time by `Microsoft.CodeAnalysis.BannedApiAnalyzers` ([decision 0008](decisions/0008-random-stream-derivation.md)): `System.Random`, `Guid.NewGuid`, `DateTime.Now`, `Environment.TickCount`, and `string.GetHashCode` in Core fail the build as `RS0030`. That is a feature, not a misconfiguration — authoritative code draws from seeded streams.

## `godot`

Godot 4.7.2 stable, .NET edition. The project is `src/SpaceExplorer.Game`, its renderer is `gl_compatibility`, and its export templates live in `~/.local/share/godot/export_templates/4.7.2.stable.mono/`.

```sh
godot --version                                        # 4.7.2.stable.mono.official.ed1daf0bf
godot --path src/SpaceExplorer.Game --editor           # open the editor
godot --path src/SpaceExplorer.Game                    # run the main scene (Main.tscn)
godot --headless --path src/SpaceExplorer.Game --import   # import assets; run before any export
```

The solar-system review view of [decision 0052](decisions/0052-the-two-levers-compose-a-destination-and-a-review-view-shows-it.md). Its arguments come after `--`, like the smoke flag, and it needs a published set in the data root:

```sh
godot --path src/SpaceExplorer.Game -- --system --tier starter --seed 7
# loads or composes the destination the two levers name and draws it, each body wearing its own
# description; --destination <pack-id> draws a stored destination instead, straight from the data root
# --set <pack-id> when more than one set is published; --data-root <dir> to read another root
# tab and shift-tab cycle bodies, 1-9 choose one, 0 frames the system, w a s d q e move, shift is
# faster, drag turns, the wheel closes in, p hides and shows the panel, escape quits
# the panel carries the summary while the whole system is framed and the whole table at a body
# (decision 0054); --no-panel opens without it, for a picture of the system alone
# what the picture shows is read from the records: a body's size, material, pole, orbit shape and air,
# and the star's colour from its temperature ([decision 0057](decisions/0057-the-view-draws-what-the-content-stores.md))

godot --path src/SpaceExplorer.Game --resolution 1600x900 -- --system --tier starter --seed 7 \
  --focus 2 --screenshot /tmp/system.png
# opens at body 2, saves a picture and exits, so a system is reviewable without sitting at the window
# give --screenshot an absolute path: a relative one resolves against src/SpaceExplorer.Game, not the
# shell's directory, and the save fails where that directory does not exist
```

The surface review view of [decision 0061](decisions/0061-surface-iterations-by-an-escalating-ladder-registry-revision-5-and-grammar-version-6.md),
which draws one region of that same destination. It needs a set published under registry revision 5 or
later, because no earlier revision has a `region` category:

```sh
godot --path src/SpaceExplorer.Game -- --surface --tier starter --seed 1
# lists every region the destination carries, then draws the first at eye height: the ground at its
# true extent, wearing the body's own stored material tiled at the length its recipe's scale claims
# --region <n> draws another of the listed regions; --from-above draws the whole region orthographic
# with a scale bar, since a flat 2,048 m plane is illegible from inside it
# the ground is read from the region-payload/1 record the destination's composition wrote, and the
# legend says "loaded from the data root"; "generated and stored" appears only for a destination
# published before decision 0067 or a field whose parameters have moved, which is the fallback and
# not a contradiction (decisions 0066, 0067)
# rung 3 (decision 0070) is built. --kind <rocky|icy|ocean|gas-giant> draws the first region whose body
# is of that type, falling back to the first region where the destination has none, and says which
# before it draws: "kind          ocean: region 2", or "kind          ocean: absent, falling back to
# region 0". A sweep rotating the kind by seed therefore covers rock, ice and ocean in the same
# forty-eight frames, where drawing each destination's first region drew a rocky body every time
# (clause 12). The ground wears each cell's biome material, one mesh surface per biome over shared
# vertices, derived on load by derive-biome-index/1 from the stored patches, the heights and the
# planet's sea level, and stored nowhere; a flat water plane stands at the sea level where the planet
# declares one. The legend gains two lines:
#   biomes     3 in the planet's palette, 64 patches stored, 3 present here, drawn in 3 surface(s)
#   sea        -12 m above the reference sphere, 79.3% of cells under it
#   sea        none: the planet declares no datum
# the second form where the datum is the floor, which is every rocky and icy body the basic vocabulary
# draws. --list is unchanged and prints only the region lines, so the two counts a row carries —
# distinct biome elements across the sample, and biomes present per region — are read from the legend
# per frame and from the set, not from --list.
# --tier, --seed, --set, --data-root, --no-publish and --screenshot are the system view's, unchanged
# w a s d walk the ground, shift is faster, drag turns, and the eye stays at its 2 m: the walk frame
# is walkable in an interactive run, held inside the region's own extent, because a repeat is a
# property of ground over distance and one pose can only report the pose it was taken at
# ([decision 0062](decisions/0062-the-surface-harness-walks-in-an-interactive-session.md))
# the legend names what the view supplied rather than read: the light direction, the sky colour, the
# eye height and the pace are the harness's own, because no celestial solution exists yet (decision 0032)
# --list prints the regions and quits, which is how a sweep counts a sample's regions and the distinct
# surfaces they wear; it is the only form that works under --headless, since a headless run has no
# viewport to save a screenshot from
# escape quits

godot --path src/SpaceExplorer.Game --resolution 1600x900 -- --surface --tier starter --seed 1 \
  --region 0 --screenshot /tmp/surface.png
# saves the fixed frame of decision 0061 and exits, so a picture is the same frame whether or not a
# reader could have walked in it; the map frame is fixed in every run
```

A surface cycle's whole sample, which is the command a row's frames come from
([decision 0061](decisions/0061-surface-iterations-by-an-escalating-ladder-registry-revision-5-and-grammar-version-6.md)
clauses 8 and 9, [decision 0070](decisions/0070-a-biome-is-a-derived-set-registry-revision-9-and-grammar-version-10.md)
clause 12). `SET` is the published set pack and `N` the cycle number:

```sh
run() { command -v xvfb-run >/dev/null && xvfb-run -a "$@" || "$@"; }

mkdir -p "build/samples/surface$N-starter"
for seed in $(seq 1 24); do
  case $(( (seed - 1) % 3 )) in 0) kind=rocky;; 1) kind=icy;; 2) kind=ocean;; esac
  s=$(printf "%02d" "$seed")
  dotnet run --project src/SpaceExplorer.Cli -- destination --tier starter --seed "$seed" --set "$SET"
  for frame in walk map; do
    [ "$frame" = map ] && above=--from-above || above=
    run godot --path src/SpaceExplorer.Game --resolution 1600x900 -- --surface --tier starter \
      --seed "$seed" --set "$SET" --kind "$kind" $above \
      --screenshot "$PWD/build/samples/surface$N-starter/seed$s-$kind-$frame.png"
  done
done
```

**`xvfb-run` is used where it exists and skipped where it does not**, which is the only way a sweep renders
without taking the screen: a Godot window with the no-focus flag does not run its render loop and never
saves a frame, and one placed off the visible desktop renders the overlapping part and black for the rest —
both measured on this workstation rather than assumed. Without a virtual display the sweep opens
forty-eight windows in front of whatever else is running, which works and is merely antisocial. Install it
with `sudo apt install xvfb`.

The destination is composed before its frames because composing publishes the ground
([decision 0067](decisions/0067-a-destinations-ground-is-generated-when-it-is-composed.md)), and the view
only reads. A seed whose system has no body of its turn's kind falls back to its first region and says so,
so the sweep never skips a seed.

**Two windows on one destination**, the system in the first and its ground in the second, which is the
inspection the owner asked for on 2026-09-12 ([decision 0071](decisions/0071-the-explorable-planet-is-the-subject-the-far-field-before-features-and-a-two-window-inspection.md)
clause 6). It needs no harness: both views are the same Godot project, both take the same
levers, and both load the stored destination those levers name rather than composing a second one
([decision 0053](decisions/0053-a-destination-is-a-stored-record-addressed-by-its-levers.md)), so the two
processes show one destination out of one data root.

```sh
godot --path src/SpaceExplorer.Game -- --system --tier starter --seed 7 &
godot --path src/SpaceExplorer.Game -- --surface --tier starter --seed 7 --region 0
```

Compose the destination once before opening either, with `destination --tier starter --seed 7`, so the
first window to open is not the one paying for the ground
([decision 0067](decisions/0067-a-destinations-ground-is-generated-when-it-is-composed.md)). Which region
the second window draws is still `--region <n>` or `--kind <type>`: `--landable`, which names the
explorable planet's region directly, and `--destination <pack>` on the surface view are authorized by
decision 0071 clause 6 and not yet built. Until they are, read the surface legend, which prints
`landing candidate` or the refusal for the body it drew.

**A destination with no landable body has no region to draw** and the view says so by name, exit 1: a
region is placed only where the body is both large enough for one ([decision 0059](decisions/0059-region-limits-are-a-pinned-record.md))
and declares the `solid-surface` tag, which the `basic` vocabulary withholds from a gas giant.

Exports. The preset names are exactly `Linux` and `Windows Desktop`, and they must be quoted:

```sh
godot --headless --path src/SpaceExplorer.Game --export-release "Linux"
godot --headless --path src/SpaceExplorer.Game --export-release "Windows Desktop"
```

With no output path, each preset writes to its `export_path`: `build/linux/SpaceExplorer.Game.x86_64` and `build/windows/SpaceExplorer.Game.exe`. Pass a path as a final argument to override it, which is what the smoke scripts do. The Windows preset disables resource modification, so **cross-exporting the Windows build from Linux needs no Wine**; running it still requires Windows ([requirement R2](requirements.md)).

Both presets set `embed_pck=false`, so an exported build is a directory, not one file: the binary, its `.pck`, and a `data_SpaceExplorer.Game_*` folder holding the .NET assemblies and `libe_sqlite3.so`. Copy the whole directory when you move a build.

Running an exported build takes engine arguments first, then `--`, then the game's own:

```sh
build/smoke/linux/SpaceExplorer.Game.x86_64 --headless -- --smoke
# SMOKE OK godot=4.7.2.stable.mono dotnet=10.0.11 sqlite=3.53.3
```

`Main.cs` reads `--smoke` through `OS.GetCmdlineUserArgs()`, which returns only what follows `--`. Without the separator the flag reaches the engine, which does not know it, and the check never runs.

## Exported-build smoke scripts

Thin wrappers over the two commands above: import, export with the platform's preset, launch the build, require `SMOKE OK` in the output. They export to `build/smoke/<platform>/`, deleting that directory first, so they never disturb `build/linux` or `build/windows`.

```sh
tests/SpaceExplorer.Game.Smoke/smoke.sh                # Linux
GODOT=~/.local/opt/godot-4.7.2-mono/Godot_v4.7.2-stable_mono_linux.x86_64 \
  tests/SpaceExplorer.Game.Smoke/smoke.sh              # with an explicit editor binary
```

```powershell
powershell -ExecutionPolicy Bypass -File tests\SpaceExplorer.Game.Smoke\smoke.ps1   # Windows only
```

Exit 0 means the marker was printed. Non-zero means either the export failed, the build exited non-zero, or the marker was absent; the script prints which. On Windows the script launches `SpaceExplorer.Game.console.exe`, the console wrapper, because the plain `.exe` detaches from the console and its output and exit code would not reach the script. Both scripts run as [continuous integration](#github-actions) steps, which provision Godot and its export templates per operating system.

## The documentation tool (`tools/docs.cs`)

A file-based C# program: no project file, no build step, just the pinned SDK.

```sh
dotnet run tools/docs.cs                 # regenerate the decision table in docs/decisions/README.md
dotnet run tools/docs.cs -- --check      # verify only; exit 1 on any problem
dotnet run tools/docs.cs -- --external   # also resolve every cited external URL (network)
```

A clean `--check` prints one line — the document, link, and record counts followed by `0 problem(s)` — and exits 0. Any problem is listed one per line and exits 1.

`--check` verifies internal links and anchors, decision-record numbering and headings, that each record is referenced from a document other than an index, and that the generated decision table is current. CI runs it on Ubuntu only. `--external` is never run in CI; it needs the network, and Epic's Unreal Engine licence page answers automated requests with HTTP 403 and has to be opened in a browser.

One rule that surprises people: the tool indexes **only Markdown files tracked by git**. A new document is invisible to the checker and absent from the generated tables until it is at least `git add`ed. If a link to a new page reports "link target not found", stage the page.

## The statistics tool (`tools/stats.cs`)

A file-based C# program like the docs tool, needing only the pinned SDK. It reports what the data root of [decision 0018](decisions/0018-data-root-region-encoding-and-save-integrity.md) currently holds: the store's bytes by record kind, each set pack's definitions by category with their record sizes and the templates they were drawn from, every composed structure with its bodies, instances, depth and the bytes it owns beside the set it shares, and the destinations with the attempts each tier needed.

```sh
dotnet run tools/stats.cs                       # the data root the tool would use
dotnet run tools/stats.cs -- --data-root <dir>   # another one, as the command-line tool takes it
```

It builds `src/SpaceExplorer.Cli` once and then calls that assembly per pack, because a report over every pack makes one call per pack and `dotnet run --project` per call costs minutes. Every count therefore comes from the tool's own verified reload path — [`list`, `inspect`, `inspect-graph`](#dotnet) — and every size from the record file on disk; nothing is decoded a second time and nothing is regenerated. Where a count parsed from that output and the tool's own count disagree, the run fails naming the pack rather than reporting the difference, so a change to the tool's output cannot quietly become a wrong number.

It reads and measures only. It writes nothing, publishes nothing, and needs no network.

## `git`

Beyond ordinary version control, git is a runtime dependency of the docs tool, which shells out to it:

```sh
git ls-files -z -- '*.md'      # the tool's own document discovery
```

So the docs tool must run inside the work tree, and an unstaged document is not seen. Repository practice for this project: work on `main`, and finish a piece of work with the docs check green before committing.

## `gh` (continuous integration status)

Not a build dependency; this is how CI results are read back after a push, without opening a browser.

```sh
gh run list --branch main --limit 5              # recent runs and their conclusions
gh run view <run-id>                             # jobs in one run
gh run view <run-id> --log-failed                # only the failing step's log
gh run watch <run-id>                            # follow a run to completion
```

## GitHub Actions

`.github/workflows/ci.yml` runs on every push to `main` and every pull request, on `ubuntu-latest` and `windows-latest` with `fail-fast: false`. It uses `actions/checkout@v7` and `actions/setup-dotnet@v6` with `global-json-file: global.json`, so the runners install the same 10.0.400 SDK, then runs:

```sh
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet run tools/docs.cs -- --check      # ubuntu-latest only
tests/SpaceExplorer.Game.Smoke/smoke.sh  # smoke.ps1 on windows-latest
```

To reproduce that sequence locally, run those commands with `CI=true` exported. The smoke step exports and launches the packaged build headlessly, so it exercises no graphics or input; requirement R2 is still undemonstrated for those.

## Native SQLite (called indirectly)

There is no `sqlite3` command in the workflow. `Microsoft.Data.Sqlite` 10.0.11 carries the native library through SQLitePCLRaw, and `SpaceExplorer.Persistence.SqliteRuntime.GetLibraryVersion()` reports its version. Two ways to confirm the native library loads:

```sh
dotnet run --project src/SpaceExplorer.Cli -- diagnostics   # from the SDK
tests/SpaceExplorer.Game.Smoke/smoke.sh                     # from an exported build, no SDK involved
```

The second is the one that matters, because it proves the library loads without an installed SDK or editor.

## What is deliberately not in the flow

Saves a search. There is **no** style or formatting gate: `EnforceCodeStyleInBuild` was removed because `.editorconfig` sets indentation only, so no IDE rule ever reached the build, and the flag promised enforcement it did not deliver ([finding 27](review.md#27-code-style-enforcement-is-nominal)). What does gate the build is compiler warnings, which are errors, plus the banned-symbol analyzer in Core. There is no `dotnet format` step.

There is no benchmark command. `tests/SpaceExplorer.Benchmarks` was deleted rather than carried empty ([finding 26](review.md#26-the-benchmarks-project-is-empty)); BenchmarkDotNet returns to `Directory.Packages.props` with the first real measurement, at the M0a generator.

No Wine is needed for the Windows export. PowerShell is not needed on Linux; `smoke.ps1` is for Windows only. No containers, and no package manager beyond NuGet with central versions.
