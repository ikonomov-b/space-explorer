# 0014. Repository tooling aligned with the stack; licence deferred with an explicit notice

Date: 2026-09-07. Status: Accepted.

## Context

The ignore file reflected the removed TypeScript scaffold and lacked .NET, Godot, and database entries; the editorconfig had no C# rule; no licence file existed although the technology decision left the game's licence undetermined; and both-OS gates were mandatory without automation. Resolves [review finding 14](../review.md#14-tooling-predates-the-stack-decision).

## Decision

The ignore file gains `bin/`, `obj/`, `.godot/`, `*.db`, `*.db-journal`, `*.db-wal`, `BenchmarkDotNet.Artifacts/`, `TestResults/`, `.vs/`, and `*.user`. The editorconfig gains four-space indentation for C# and two-space indentation for project, props, and targets files. A `LICENSE` file states that all rights are reserved and no licence is granted yet; the licence is decided before the first external contribution or the first third-party asset enters the repository, whichever comes first. A Linux and Windows test matrix is added the day the first test project exists.

## Consequences

Easier: build output and local databases never enter the history; contributors see the licence status instead of assuming one. Harder: none of substance; the licence decision remains open and is now visibly open. No verification milestone; the CI rule is checked at M0a.

## Applied to

- `.gitignore`, `.editorconfig`, new `LICENSE`, [README](../../README.md)
- [Technology decision: native platform and packaging policy](../technology-stack.md#native-platform-and-packaging-policy)
