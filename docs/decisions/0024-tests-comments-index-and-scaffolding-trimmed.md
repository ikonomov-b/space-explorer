# 0024. One test per frozen rule; comments point to decisions; generated index, Benchmarks project, and code-style flag removed

Date: 2026-09-07. Status: Accepted. Supersedes the code-style enforcement of [decision 0016](0016-platform-confirmed-and-toolchain-pinned.md). Supersedes the Benchmarks project of [decision 0009](0009-solution-layout.md). Source: owner decision of 2026-09-07 on four review recommendations.

## Context

The implementation review of 2026-09-07 found four kinds of structure carried ahead of need. Six random-foundation tests caught none of eleven source mutations, and one tested something other than its comment claimed ([finding 24](../review.md#24-tests-that-pin-nothing-the-recorded-vectors-do-not)). Facts were restated in four or five places, `Shared/` held more comment lines than code lines, and the generated document index repeated the README with rows of up to 26 links ([finding 25](../review.md#25-documentation-restates-itself-and-the-generated-index-is-unreadable)). The Benchmarks project built in continuous integration on both operating systems with no benchmark in it ([finding 26](../review.md#26-the-benchmarks-project-is-empty)). `EnforceCodeStyleInBuild` was on with no rule given a severity, so three style violations built with zero warnings ([finding 27](../review.md#27-code-style-enforcement-is-nominal)). The owner accepted the proposed solution of each.

## Decision

**Tests.** The suite for a frozen rule is its recorded vectors, its external anchors, and the guards on its preconditions: one test per rule. Property-style checks that no mutation distinguishes from the vectors are not added, and a mutation run recorded in the decision that freezes the rule, not the test count, is the evidence that the suite is load-bearing. The six tests finding 24 names are deleted: the bound-of-one, in-range, and distribution checks on bounded draws and the repeatability, early-repeat, and increment-oddness checks on derivation. Recorded vectors hold literals only, as [decision 0021](0021-sha256-stream-derivation.md) already states.

**Comments.** A code comment says what a member does and names the decision that fixes any rule it implements. It does not restate the rule's rationale, which lives in the decision record once. The eight files in `src/SpaceExplorer.Core/Shared/` are rewritten to this rule; it applies to all code from here on.

**Documentation index.** `docs/index.md` and its generator are removed. `tools/docs.cs` keeps the link, anchor, and decision-numbering checks and the generated decision table. The README's curated list is the map of the documents; [progress.md](../progress.md) remains the single status source.

**Benchmarks.** `tests/SpaceExplorer.Benchmarks/` is deleted, with its solution entry and the BenchmarkDotNet pin in `Directory.Packages.props`, until the first benchmark exists; the M0a generator measurement recreates it. BenchmarkDotNet remains the chosen measurement tool in the [technology decision](../technology-stack.md#selected-components).

**Code style.** `EnforceCodeStyleInBuild` is removed from `Directory.Build.props`. Warnings as errors on the compiler's own diagnostics is the enforcement. A style rule is adopted, with a warning severity in `.editorconfig`, only when a specific rule earns it.

## Consequences

Easier: each frozen rule has one place that explains it and one test that pins it, so a change touches fewer files; the build has one less analyzer pass and one less project; the tree carries nothing that exists for a later step. Harder: a reader of the code follows a decision number for the rationale, and the first benchmark rebuilds a fifteen-line project.

What is verified, on Debian 13 x86_64: the solution builds with zero warnings; 69 Core tests and 2 Persistence tests pass, six fewer and one more than before; the eight rewritten files hold 142 comment lines against 307 unchanged code lines, down from 326; the docs check reports zero problems with 587 internal links, down from 905 with the index. The `windows-latest` result for this change is recorded in [progress.md](../progress.md) when the continuous integration run completes.

## Applied to

- `src/SpaceExplorer.Core/Shared/*.cs`, `tests/SpaceExplorer.Core.Tests/Shared/{Pcg32,RandomStream}Tests.cs`
- Deleted: `tests/SpaceExplorer.Benchmarks/`, `docs/index.md`; `SpaceExplorer.sln`, `Directory.Build.props`, `Directory.Packages.props`, `tools/docs.cs`
- [README](../../README.md), [src/README.md](../../src/README.md), [progress.md](../progress.md)
- [Review findings 24 to 27](../review.md#24-tests-that-pin-nothing-the-recorded-vectors-do-not)
