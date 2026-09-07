# Agent instructions

[README.md](README.md) lists every document. [docs/progress.md](docs/progress.md) is the single, current-status source ([decision 0011](docs/decisions/0011-requirements-single-source.md)) — read it before the development plan or technical design if you need what is actually true right now, not what was planned. Every exact command this project runs is single-sourced in [docs/tools.md](docs/tools.md); do not re-derive an invocation.

## Before running any command

`dotnet` is not on `PATH` in non-interactive shells (`bash -c`, hooks, agent shells), only in interactive terminals:

```sh
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$HOME/.dotnet:$HOME/.dotnet/tools"
```

Full detail, every tool, and what success looks like: [docs/tools.md](docs/tools.md).

## Documentation-driven process

Every accepted decision gets one immutable record in `docs/decisions/`, numbered in acceptance order and never edited after acceptance ([docs/decisions/README.md](docs/decisions/README.md)). Design gaps and inconsistencies are tracked as numbered findings in [docs/review.md](docs/review.md), each cleared by recording a decision and applying it to every affected document. A change that isn't reflected in the matching doc (decision record, review-finding status, `progress.md`) breaks the single-source rules of decisions [0011](docs/decisions/0011-requirements-single-source.md) and [0016](docs/decisions/0016-platform-confirmed-and-toolchain-pinned.md). Before committing any doc change, run:

```sh
dotnet run tools/docs.cs -- --check
```

It verifies every internal link and anchor, and that decision-record numbering is contiguous — CI runs it on every push.

## This repo is edited by concurrent sessions

Boyan and other Claude Code sessions can be working the same checkout at the same time, coordinating over `SendMessage` when visible as peers. Decision numbers are contiguous only among *git-tracked* files, so a peer's in-flight, uncommitted decision is invisible until it lands — claiming its number produces a collision. Re-check `git status` and `ls docs/decisions/` immediately before writing, not just at the start of a turn; prefer `git add <specific paths>` over `git add -A`; announce what you're about to touch before you start, not only when asked.

## House style

Trim rather than scaffold ahead of need — see [decision 0002](docs/decisions/0002-primitive-set-content-gate.md) (set algebra specified, not implemented, until a feature needs it) and [decision 0024](docs/decisions/0024-tests-comments-index-and-scaffolding-trimmed.md) (an empty Benchmarks project, a stale code-style flag, and restated comments removed). Derive a value from existing constants rather than restating an estimate ([decision 0017](docs/decisions/0017-region-extent-cap-and-storage-derivation.md)). Cite every claim to a decision, requirement, or section anchor instead of restating it in prose.
