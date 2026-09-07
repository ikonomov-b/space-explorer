# 0025. P0 assessment: location, scope, and players

Date: 2026-09-07. Status: Accepted. Source: resolves the P0 assessment [decision 0022](0022-p0-gates-the-first-frozen-content-format.md) reserved to the owner, applying decision 0022's own constraints rather than opening new ones.

## Context

[Decision 0022](0022-p0-gates-the-first-frozen-content-format.md) left P0's location relative to the solution, its scope beyond [decision 0015](0015-greybox-prototype.md), and who plays it to a separate owner assessment, taking the next decision number if one was needed. `prototypes/` still held only its README, so this record settles the three questions and unblocks the build.

## Decision

**Location.** P0 is a second, fully self-contained Godot 4.7.2 .NET project at `prototypes/p0-expedition/`: its own `project.godot`, `.csproj`, and `.sln`, gl_compatibility renderer to match `src/SpaceExplorer.Game`. It is not added to `SpaceExplorer.sln` and no `ProjectReference` connects it to anything under `src/`, so it builds, and fails, independently of the main solution. This is the only shape decision 0015's isolation rule and decision 0022's build-on-the-pinned-stack requirement leave open; there was no second reasonable location to weigh it against.

**Scope.** Exactly decision 0015, nothing added: one hard-coded landing region, three sites, four artifacts, a cargo limit forcing a choice of two, a return to a fixed departure point, a sale, and a second destination choice. No generation, no persistence. The prototype uses this project's existing vocabulary (site, region, artifact, home anchor, credits) from [glossary.md](../glossary.md) rather than inventing new terms, and stays under the 2,048 m region extent cap of [decision 0017](0017-region-extent-cap-and-storage-derivation.md) without needing to approach it. Perspective is first-person with mouse-look and WASD movement on a physics-driven character body, the simplest choice that still exercises the movement and physics decision 0022 asks P0 to exercise; camera and art direction remain open by design for M1 regardless of what P0 uses.

**Players.** Unresolved by this record. Three to five players, the exit record stating whether each can explain a choice that changed their outcome and whether they choose another trip, is decision 0015's process; recruiting and running those sessions is the owner's action, not a build task, and no automated substitute is attempted. The prototype ships with a run book at `prototypes/p0-expedition/README.md` for whoever runs the sessions, and the exit record itself is added to that file once sessions are run.

## Consequences

Easier: the location and scope questions decision 0022 opened are closed before any code is written, so the build proceeds without a second pass to relocate or rescope it. Harder: the exit record, the part decision 0015 actually verifies, still waits on the owner scheduling real players; this record does not close [review finding 23](../review.md#23-p0-has-not-started-while-m0a-proceeds) by itself, only decision 0022 did that for the gating question.

## Applied to

- [Progress: milestones](../progress.md#milestones)
- New `prototypes/p0-expedition/`
