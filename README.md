# Space Explorer

A PC game concept about wormhole expeditions, generated planetary systems, artifact collecting, and locally saved worlds that friends can explore together.

Development is Linux-first with native Linux and Windows releases; the full platform policy is in [docs/requirements.md](docs/requirements.md). The stack is Godot 4.7.2 .NET with C# on .NET 10, an engine-independent generator, and SQLite persistence, confirmed and pinned in [decision 0016](docs/decisions/0016-platform-confirmed-and-toolchain-pinned.md). The engine is free to use; performance and execution on Windows remain to be validated.

The scaffold, the core's deterministic random foundation, the primitive-set foundation, and the first structure generation are in place: the command-line tool generates a set from an authored vocabulary, publishes it as content-addressed records, reloads it without generating, composes a bounded solar-system or artifact graph from it under a versioned grammar, and describes a composed system in text against the tier rules. There is no world generation or playable build yet. Implementation status is tracked in [docs/progress.md](docs/progress.md), the source layout and toolchain setup are in [src/README.md](src/README.md), and every command is in [docs/tools.md](docs/tools.md).

## Documents

- [Game concept](docs/concept.md): the game idea, player experience, worlds, artifacts, and progression.
- [Requirements](docs/requirements.md): owner-stated platform policy and design constraints, with provenance.
- [Assessment and similar projects](docs/assessment.md): relevance, twelve comparable projects with primary sources, gaps addressed, and remaining uncertainties.
- [Development plan](docs/development-plan.md): proposed release scope, milestones, usability requirements, verification targets, and production decisions.
- [Implementation progress](docs/progress.md): what is built, what is verified and how, what is not, and the open items.
- [External tools](docs/tools.md): the exact call for every external program the project drives, where it runs from, and what success looks like.
- [Primitives and primitive-based structures](docs/primitives.md): the universal base — what a primitive is, what a structure is, how both are stored and read back, and why every generated level rests on the same two ideas.
- [Primitives and storage, drawn](docs/diagrams/primitives-and-storage.html): the same mechanism as a diagram page — the chain from an authored template to a stored destination, one stored definition and everything it derives, what pins what, and the record sizes as measured in the data root.
- [Technical design](docs/technical-design.md): generation, primitive versions, persistence, artifact valuation, and multiplayer ownership.
- [Technology decision](docs/technology-stack.md): selected platform/libraries, alternatives, native-platform validation, and numbered decisions for confirmation.
- [Design review](docs/review.md): foundational decisions that would be costly to reverse, gaps, inconsistencies, proposed solutions, and their status.
- [Inspection: documents and structure](docs/inspection.md): the documents read against each other and against the code, what the two no longer agree on, and what each finding needs to clear it.
- [Inspection: storage structure and effectiveness](docs/inspection-storage.md): the data root measured as it stands, what a stored system holds and what is derived on read, the cost of each, and what storing every detail of an environment would mean.
- [Glossary](docs/glossary.md): terms used across the documents.
- [Decision records](docs/decisions/README.md): accepted decisions, one record each.

Assessment date: 2026-09-07. Added design defaults are proposals for a prototype, not implemented features or validated market demand. Licence: all rights reserved until decided; see [LICENSE](LICENSE).
