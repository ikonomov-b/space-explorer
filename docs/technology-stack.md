# Technology decision: native Linux and Windows

Reviewed 2026-09-07. Selected working stack: **Godot 4 .NET with C#**, an engine-independent generation core, and SQLite persistence. This is a documentation decision, not an implemented or benchmarked system. Exact tool versions and shipping hardware requirements must pass the prototype checks below. The [decision register](#gap-filling-decisions-for-confirmation) distinguishes user requirements from recommended implementation defaults.

## Requirements and rationale

The platform requirements in [requirements.md](requirements.md) apply: a free engine and workflow, native Linux and Windows releases, Linux as the daily environment, Python optional. The project also needs random primitive-set generation and storage before world generation, compact unsigned primitive identifiers, durable local worlds, and private hosted cooperation.

Godot provides the integrated editor and desktop targets; its .NET edition supports C# game exports on both required operating systems. C# lets the primitive tools and game share a typed data model and generator without tying those components to scenes or rendering. This combination is the best fit among the options reviewed, not a claim of universal superiority or measured performance. [Godot platform/language support](https://docs.godotengine.org/en/stable/about/faq.html), [C# setup and desktop exports](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html).

Godot is MIT-licensed and permits commercial distribution without engine royalties or a subscription. Include required engine and third-party notices. That does not grant rights to unrelated assets, competitor code, or branding, and does not determine this game's own licence. [Godot licensing](https://godotengine.org/license/).

## Alternatives considered

| Option | Fit and trade-off | Decision |
| --- | --- | --- |
| Godot .NET + C# | Native Linux/Windows; shared engine-independent libraries; explicit fixed-width numeric types. Requires the .NET editor edition, SDK, and a separate code editor for a comfortable workflow. | Selected working stack. |
| Godot + GDScript | Simpler initial scripting setup and close editor integration. GDScript resembles Python but is a different language. Sharing the same generator with a normal .NET command-line tool would require an additional language boundary. | Strong alternative if initial scripting simplicity becomes more important than a single reusable C# core. |
| Python + Ursina/Panda3D | Ursina lists Linux and Windows support and provides convenient 3D primitives. Panda3D has a C++ engine with Python bindings, so Python does not imply a Python software renderer. The underlying workflow is more code-oriented. | Viable if Python becomes a requirement; no need to introduce it for the current requirements. |

Godot recommends GDScript for beginners and officially supports C#, GDScript, and C++; Python integrations are unofficial. Choosing C# here is a project-specific trade-off, not a dismissal of GDScript. C++ remains an option for a measured bottleneck, not a prerequisite for compact identifiers. [Godot language guidance](https://docs.godotengine.org/en/stable/about/faq.html), [Ursina](https://www.ursinaengine.org/), [Panda3D introduction](https://docs.panda3d.org/1.10/python/introduction/index).

## Selected components

| Responsibility | Choice | Boundary |
| --- | --- | --- |
| Editor, 3D presentation, input, UI, audio, physics | Godot 4 .NET | Use built-in facilities before adding plugins. |
| Game logic and offline tools | C# and a Godot-compatible, supported .NET SDK | One main application language; core libraries must not reference Godot. |
| Primitive and world generation | Shared C# library plus a command-line entry point | Generate, validate, save, reload, and inspect without launching a graphical editor. |
| Primitive references | 128-bit `pack_id` plus C# `uint` primitive IDs; packed `uint[]` handle buffers with explicit binary encoding | Four bytes per packed handle is a format detail; full identities include the 128-bit pack namespace ([decision 0006](decisions/0006-pack-identity-and-allocation.md)). |
| Registry metadata and campaign state | SQLite through `Microsoft.Data.Sqlite` | Explicit transactions, constraints, schema versions, and a coordinated campaign writer; no database server or ORM required. |
| Compact stored recipes and sets | Versioned binary records using .NET binary I/O; fixed-width fields explicitly little-endian | Store packed vectors as BLOBs or immutable package data. Do not serialize process memory or use engine resources as the authoritative save format. |
| Reproducible randomness | Versioned PCG32 XSH-RR implementation with reference test vectors; state and stream derived from a pinned 64-bit mixer over seed and path | Stable per-task streams; rejection sampling; no dependency on frame timing or task scheduling ([decision 0008](decisions/0008-random-stream-derivation.md)). This is not a security token generator. |
| Multiplayer transport | ENet peer through Godot, behind a core-defined byte transport | The core owns commands, state replication, and the join handshake; Godot scene replication carries cosmetic presence only. A simple cloud coordination server for directory, invitation, and relay is evaluated at M1 start ([decision 0003](decisions/0003-network-topology-and-transport.md)). |
| Verification | xUnit.net; BenchmarkDotNet for core measurements; a banned-API analyzer on the core project; Godot profiler for frame/render costs | Run the correctness suite on both operating systems and in two separate processes for determinism; engine integration also needs exported-build tests. |

C# defines `uint` as an unsigned 32-bit type. `Microsoft.Data.Sqlite` is a lightweight provider; SQLite supports transactional updates. Its INTEGER storage is signed and variable-width, not a four-byte unsigned column: bind IDs as signed 64-bit values with range checks, and use packed binary vectors where fixed-width storage matters. [C# integer types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types), [provider](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/), [SQLite transactions](https://www.sqlite.org/transactional.html), [SQLite storage types](https://www.sqlite.org/datatype3.html).

The PRNG selection needs a reviewed implementation, checked reuse terms, and fixed test vectors before use. A matching random stream alone does not guarantee matching geometry: ordering, arithmetic, noise, and accepted descriptors also require versioning and tests. [PCG32 reference](https://www.pcg-random.org/using-pcg-c-basic.html).

Optional later dependencies are the standalone C# version of FastNoiseLite for terrain and FlatBuffers if larger schemas justify generated binary accessors. Neither is needed to prove the first primitive-set milestone. Keep noise out of authoritative cross-platform decisions until its results are verified or materialized. Do not add a second renderer, physics library, Python runtime, or native C++ extension without a demonstrated need. [FastNoiseLite](https://github.com/Auburn/FastNoiseLite), [FlatBuffers C#](https://flatbuffers.dev/languages/c_sharp/), [xUnit.net](https://xunit.net/), [BenchmarkDotNet](https://benchmarkdotnet.org/articles/overview.html).

## Architecture and first implementation step

The same core serves three consumers:

```text
SpaceExplorer.Core: domain, registry, generator, campaign rules (no Godot dependency)
  +-- SpaceExplorer.Cli: command-line primitive-set generator and validator
  +-- tests/: automated tests and benchmarks
  +-- SpaceExplorer.Game: Godot C# game adapter and primitive viewer

SpaceExplorer.Persistence (SQLite/binary)  --> core-defined data contracts
ENet transport adapter inside Game         --> core-owned commands and replication
```

The source tree is laid out by assembly as described in [src/README.md](../src/README.md); the solution file and pinned SDK and package versions are created at M0a start ([decision 0009](decisions/0009-solution-layout.md)). Keep Godot types out of stored/core contracts. Worker tasks produce plain data; the game adapter applies results to the active scene on the appropriate thread. Godot's active scene tree is not generally thread-safe. [Thread-safety guidance](https://docs.godotengine.org/en/stable/tutorials/performance/thread_safe_apis.html).

M0a first generates a random, constraint-valid primitive set from a small authored template vocabulary, assigns permanent IDs, and stores its accepted definitions and manifest. Reload it without rerolling; compare canonical hashes across Linux and Windows; exercise lookup and membership; then display a fixed sample in a minimal Godot viewer for the content gate defined in [decision 0002](decisions/0002-primitive-set-content-gate.md). A set is a reusable stored construction unit, not just a transient list sampled while making a planet. M0b composes worlds and artifacts from these saved sets. The exact identity/storage contract belongs in the [technical design](technical-design.md#primitive-registry-and-composition).

Use Godot's procedural mesh APIs for the viewer and later regions. Batch repeated geometry where appropriate; do not create a scene node for every stored primitive reference. [Procedural geometry](https://docs.godotengine.org/en/stable/tutorials/3d/procedural_geometry/index.html).

## Native platform and packaging policy

The main development cycle runs entirely on Linux: edit C#, run core tests and the primitive CLI, inspect results in Godot, profile, and produce a Linux build. Use the Linux Godot .NET editor, .NET CLI, and a Linux-native C# code editor. No Windows-only IDE, asset-conversion tool, build script, or service may be required for this loop. Keep build logic in portable .NET tooling or document equivalent Linux/Windows entry points when scripts are introduced.

Windows validation runs on a native Windows machine or runner at M0a/M0b gates and before releases; it is not replaced by Linux-only tests or Wine. A platform-validation gate remains incomplete until the required Windows checks run. A Linux and Windows test matrix is added the day the first test project exists ([decision 0014](decisions/0014-tooling-and-licence.md)); from then on, run the core regression suite for changes on both OSes and exported-build smoke tests for release candidates. Cross-exporting from Linux may be convenient after it is verified for the pinned toolchain, but does not remove the need to execute the Windows build on Windows. Hardware-dependent graphics/input checks and mixed-OS co-op need appropriate test environments, not just a headless build job.

Start validation on Linux x86_64 and Windows x86_64. This architecture choice is a proposed initial scope, not an exclusion imposed by the engine. Choose supported Linux distributions, Windows versions, minimum GPU/drivers, and reference machines from actual tests; do not claim every Linux distribution or PC will work.

Start the small 3D viewer with Godot's Compatibility renderer to evaluate broad hardware support. It uses OpenGL and has fewer advanced rendering features than Forward+. Before M1, test a representative landing region and decide whether this baseline meets the art and performance needs. Moving to Forward+ requires an explicit graphics requirement update; renderer switching is not a promise of identical visuals. [Renderer comparison](https://docs.godotengine.org/en/stable/tutorials/rendering/renderers.html).

At M0a implementation start, record an exact stable Godot .NET release, matching export templates, a supported compatible SDK/target framework, and locked package versions. Do not float dependencies or blindly choose the newest SDK. Document installation/build commands for both OSes once tested. The development SDK is a developer requirement, not a player requirement: package the game and CLI with their required redistributable runtime/native dependencies and test without an installed SDK or editor.

Build and run tests natively on both platforms. Verify case-sensitive asset paths, writable user-data locations, SQLite native-library loading, save exchange, interrupted writes, and identical canonical primitive/world data. Compare canonical payloads, not entire SQLite files whose physical layouts may differ. Test Linux-host/Windows-guest and Windows-host/Linux-guest combinations. Exporting successfully on one OS does not establish compatibility on the other.

ENet provides networking, not an invitation directory or automatic relay service. A directly reachable UDP host can prove internet play; some networks need port forwarding, and carrier-grade NAT may prevent that approach. Evaluate user-friendly connectivity at M1 start, beginning with a self-hosted relay and coordination server; the intended topology is a loosely coupled network of hosts with all compute on the host. No paid relay, storefront account, or hosting service is selected here; service choice and ongoing costs remain open. Offline solo play must work without them. [Godot multiplayer and internet hosting](https://docs.godotengine.org/en/stable/tutorials/networking/high_level_multiplayer.html).

## Gap-filling decisions for confirmation

Requirements supplied by the user are recorded as such; the other entries are the working defaults added by this decision, offered for confirmation rather than attributed to earlier approval.

| # | Decision | Status |
| --- | --- | --- |
| 1 | Linux-primary daily development, native Linux and Windows releases, free approachable 3D tooling, and no Python requirement. | User requirements R1 to R5 in [requirements.md](requirements.md). |
| 2 | Godot 4 .NET + C# as the main platform and language. | Selected recommendation; validate through M0. |
| 3 | Share engine-independent C# code between the game, generator CLI, and tests. | Recommended default. |
| 4 | M0a generates and stores random primitive sets before M0b generates worlds. | User requirement R6 in [requirements.md](requirements.md). |
| 5 | 128-bit pack identifiers; pack-local unsigned 32-bit primitive IDs, zero reserved, never recycled; one new pack per generation run; cross-pack references through pinned tables. | Unsigned identity requested; namespace and allocation policy confirmed in [decision 0006](decisions/0006-pack-identity-and-allocation.md). |
| 6 | SQLite with `Microsoft.Data.Sqlite` for state/metadata; versioned packed binary for compact vectors and recipes. | Recommended default; exact schema defined in M0a. |
| 7 | Pin PCG32 with mixed state/stream derivation, rejection sampling, a banned-API analyzer, and a two-process determinism test; verify identical canonical data across both OSes. | Confirmed in [decision 0008](decisions/0008-random-stream-derivation.md); not a determinism claim. |
| 8 | Begin with x86_64 and the Compatibility renderer; decide final OS/GPU support from prototype measurements. | Recommended validation baseline. |
| 9 | ENet peer behind a core-owned transport with a host-authoritative campaign; settle invitation/relay needs at M1 start, self-hosted coordination server first. | Confirmed in [decision 0003](decisions/0003-network-topology-and-transport.md); service and costs unresolved. |
| 10 | Use xUnit.net, BenchmarkDotNet, exported-build smoke tests, and mixed-OS co-op checks. | Recommended verification baseline. |
| 11 | Keep Python, C++ extensions, FlatBuffers, and terrain-noise dependencies optional until justified. | Recommended dependency scope. |

No compiler, engine, package, service, or application code was installed or created by this documentation decision. The [development plan](development-plan.md) defines the implementation and validation gates; [concept.md](concept.md) remains a concept document.
