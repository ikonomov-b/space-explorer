# Space Explorer

A PC game concept about wormhole expeditions, generated planetary systems, artifact collecting, and locally saved worlds that friends can explore together.

Development is Linux-first with native Linux and Windows releases; the full platform policy is in [docs/requirements.md](docs/requirements.md). The stack is Godot 4.7.2 .NET with C# on .NET 10, an engine-independent generator, and SQLite persistence, confirmed and pinned in [decision 0016](docs/decisions/0016-platform-confirmed-and-toolchain-pinned.md). The engine is free to use; performance and execution on Windows remain to be validated.

The scaffold and the core's deterministic random foundation are in place; there is no world generation, persistence schema, or playable build yet. Implementation status is tracked in [docs/progress.md](docs/progress.md), and build commands are in [src/README.md](src/README.md).

## Documents

- [Game concept](docs/concept.md): the game idea, player experience, worlds, artifacts, and progression.
- [Requirements](docs/requirements.md): owner-stated platform policy and design constraints, with provenance.
- [Assessment and similar projects](docs/assessment.md): relevance, twelve comparable projects with primary sources, gaps addressed, and remaining uncertainties.
- [Development plan](docs/development-plan.md): proposed release scope, milestones, usability requirements, verification targets, and production decisions.
- [Implementation progress](docs/progress.md): what is built, what is verified and how, what is not, and the open items.
- [Technical design](docs/technical-design.md): generation, primitive versions, persistence, artifact valuation, and multiplayer ownership.
- [Technology decision](docs/technology-stack.md): selected platform/libraries, alternatives, native-platform validation, and numbered decisions for confirmation.
- [Design review](docs/review.md): foundational decisions that would be costly to reverse, gaps, inconsistencies, proposed solutions, and their status.
- [Glossary](docs/glossary.md): terms used across the documents.
- [Decision records](docs/decisions/README.md): accepted decisions, one record each.
- [Document index](docs/index.md): generated map of every tracked document with its date, purpose, and the documents that reference it; regenerated with `dotnet run tools/docs.cs`.

Assessment date: 2026-09-07. Added design defaults are proposals for a prototype, not implemented features or validated market demand. Licence: all rights reserved until decided; see [LICENSE](LICENSE).
