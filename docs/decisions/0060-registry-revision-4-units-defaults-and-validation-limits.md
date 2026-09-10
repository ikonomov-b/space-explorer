# 0060. Category registry revision 4: a unit and a default on every parameter, the validation limits in the record, and a barycentre's derived mass

Date: 2026-09-10. Status: Accepted. Source: the owner's choice of 2026-09-10, on [review finding 50](../review.md#50-a-stored-quantity-carries-no-unit-no-default-and-no-validation-limit), of the fuller option — revision 4 carries unit, default, *and* the per-category validation limits, rather than striking validation limits from [decision 0035](0035-category-registry-record-and-generator-revision-identifiers.md) and the [glossary](../glossary.md#content-identity). Clears [finding 50](../review.md#50-a-stored-quantity-carries-no-unit-no-default-and-no-validation-limit) when applied, and takes [finding 53](../review.md#53-a-barycentre-holds-nothing-so-a-published-pair-contradicts-its-own-masses)'s registry half in the same revision. This record specifies the format; it implements nothing.

## Context

[Decision 0035](0035-category-registry-record-and-generator-revision-identifiers.md) says the registry records, per category, "the ordered parameter schema with each parameter's type, width, unit and scale, range, and default, permitted connector kinds, permitted storage policies, validation limits, and the generator revision identifiers that may produce it". The [glossary](../glossary.md#content-identity) and the [technical design](../technical-design.md#primitive-registry-and-composition) repeat "validation limits" in their own lists, and the design's `parameters` row reads "Typed ranges, units, defaults, and bounded variation".

Three of those are absent from the bytes. `ParameterDescriptor.Encode` writes a label, a kind, fraction bits, a minimum, a maximum, enum labels, and a reference category. Fraction bits are a *scale*, not a unit: they say a radius is fixed-point with eight fraction bits and never that the whole is metres. So what a stored quantity means lives only in C#, mostly in comments, and the two places it is written as a declaration — `MassUnitExponentKilograms` and `LuminosityUnitExponentWatts` — are referenced by nothing. Decision 0035's own promise, that "a definition's bytes are decodable from its label alone plus a shipped data file", holds for decoding and fails for interpretation.

The sharpest proof is inside revision 3 itself: `mass` appears on three categories in two different units. A `star`'s and a `planet`'s are in 10^20 kg, so Earth reads 59,720; an `artifact-part`'s ranges 1 to 200,000 and is in something else entirely. One label, two meanings, and nothing in the record — or, for the artifact part, in any document — distinguishes them.

## Decision

Category registry revision 4 makes five changes. Revisions 1 to 3 keep their bytes and therefore their hashes, and every pack published under them keeps loading, exactly as revision 3 grew from revision 2 ([decision 0050](0050-registry-revision-2-grammar-version-2-and-versioned-record-growth.md)); a record's domain label carries its revision, so a reader dispatches before it reads a field.

### 1. Every parameter carries a unit

**A unit is a base symbol and a decimal exponent**, and it fixes the meaning of a stored value by one equation:

> quantity = `stored` ÷ 2^`fractionBits` × 10^`exponent`, in the base symbol

The exponent is what carries a magnitude too large for the integer itself — a mass in 10^20 kg — and the fraction bits keep doing exactly what they do now, carrying sub-unit resolution. Neither duplicates the other, which is why the unit is added beside the fraction bits rather than replacing them.

The base symbol is one of a **closed vocabulary this revision fixes**; a symbol not on the list is refused, and a later symbol is a later revision. It is written with `WritePath`, the writer that already carries a generator revision identifier, because a compound symbol needs the separator and `WriteText`'s character set has none.

| Symbol | Meaning |
| --- | --- |
| `kg` | kilogram |
| `m` | metre |
| `kg/m3` | kilogram per cubic metre |
| `K` | kelvin |
| `Pa` | pascal |
| `W` | watt |
| `tick` | one simulation tick, the 20 Hz step of [decision 0010](0010-units-coordinates-and-region-bounds.md) |
| `turn` | one binary turn, the angle unit of [decision 0036](0036-frames-and-transforms-typed-by-connector-kind.md) |
| `ratio` | dimensionless; the quantity is `stored` ÷ `Max`, so the declared maximum is the denominator |
| `1` | dimensionless; a count, an index, a channel, or a parameter that is not a quantity |

`ratio` and `1` are the two that do not follow the equation above, and they are there because two real kinds of dimensionless parameter must be told apart: a fraction of 65,535 is a ratio whose denominator the record already holds as its maximum, and `segments` is a count. Both take exponent zero.

**Every parameter of registry revision 3, restated in revision 4.** A category not listed keeps its parameters' units from this table where the labels match.

| Category | Parameter | Unit | Exponent | Fraction bits |
| --- | --- | --- | --- | --- |
| `star` | `mass` | `kg` | 20 | 0 |
| `star` | `radius` | `m` | 0 | 8 |
| `star` | `effective-temperature` | `K` | 0 | 0 |
| `planet` | `mass` | `kg` | 20 | 0 |
| `planet` | `density` | `kg/m3` | 0 | 0 |
| `planet` | `albedo` | `ratio` | 0 | 0 |
| `planet` | `pole-right-ascension`, `pole-declination`, `prime-meridian-phase` | `turn` | 0 | 0 |
| `planet` | `sidereal-period` | `tick` | 0 | 0 |
| `planet` | `body-type`, `surface` | `1` | 0 | 0 |
| `atmosphere` | `surface-pressure` | `Pa` | 0 | 0 |
| `atmosphere` | `model`, `composition`, `scattering-tint` | `1` | 0 | 0 |
| `surface-material` | `roughness`, `metallic` | `ratio` | 0 | 0 |
| `surface-material` | `shader`, `base-colour`, `texture` | `1` | 0 | 0 |
| `texture-recipe` | `scale` | `m` | 0 | 16 |
| `texture-recipe` | `contrast` | `ratio` | 0 | 0 |
| `texture-recipe` | `pattern`, `colour-a`, `colour-b` | `1` | 0 | 0 |
| `geometry-recipe` | `size`, `displacement` | `m` | 0 | 16 |
| `geometry-recipe` | `segments` | `1` | 0 | 0 |
| `geometry-recipe` | `shape` | `1` | 0 | 0 |
| `artifact-part` | `mass` | `kg` | **−3** | 0 |
| `artifact-part` | `geometry`, `material` | `1` | 0 | 0 |
| `artifact` | `family`, `root`, `tier` | `1` | 0 | 0 |

Every row above records a unit some code already assumes, with one exception, marked in bold. **`artifact-part.mass` is decided here, not recorded**: no document states it, and its range of 1 to 200,000 is read as grams, so a part spans one gram to two hundred kilograms and an assembled artifact under [decision 0046](0046-artifact-families-human-height-cap-cargo-by-mass-and-material-rarity.md)'s 2 m cap and `sum` aggregation stays something a person recovers. If that is not what was meant, this is the row to change before the revision is built, and it is the clearest possible evidence for the finding: a quantity whose unit no document holds cannot be checked by reading, only by deciding.

### 2. Every parameter carries a default, where its kind admits one

**A default is the value a parameter takes when nothing else supplies one, and its job is to say what a template's silence means.** Today a template that states no range for a parameter is widened to the category's full range by `FullRange` in `src/SpaceExplorer.Cli/VocabularyFile.cs` — a policy in a command-line tool, behind no record, that happens to be right for `pole-right-ascension` and would be silently wrong for `albedo`. From revision 4 a template that states no range for a parameter **pins it to the category's default**, and a vocabulary that wants a parameter varied says so.

Presence follows from the kind and is never flagged, exactly as enum labels are present when the kind is `Enum` and the reference category is non-zero when the kind is `PrimitiveRef`. That keeps the encoding canonical by construction, with no optional field and no presence byte to be written two ways.

| Kind | Default | Validated against |
| --- | --- | --- |
| `Bool` | one byte, 0 or 1 | — |
| `Enum` | an index | fewer than the declared label count |
| `Integer` | one value | inside `[Min, Max]` |
| `BinaryTurn` | one value | inside `[Min, Max]` |
| `Rotation` | three binary turns | each a signed 32-bit value |
| `Vector3` | three values | each inside `[Min, Max]` |
| `Colour` | four channel bytes | — |
| `PrimitiveRef` | **none; the field is absent** | — |

A reference has no default because a default is written when the registry is written and a reference names an exact revision of a definition that does not exist then. A template that states no range for a reference therefore keeps today's behaviour of drawing from every definition of the category, or fails by name where the demand admits none ([decision 0055](0055-definition-level-tags-and-tagged-references.md)) — it does not default.

### 3. The validation limits are the decoder's bounds, moved into the record

The three documents list "validation limits" in one phrase and none expands it. The only place any document says what is validated is [composition validation](../technical-design.md#composition-validation), whose admission list is "unique IDs and exact revisions, complete hash-verified dependencies, supported category and parameter schemas, connector cardinality and compatibility, parameter ranges, graph and decoded-size bounds, finite recursion, data-only content restrictions, storage-policy support, and valid productions for required structures", followed by the reader rule: readers "reject truncated, overflowing, non-minimal, trailing, or over-budget canonical encodings **before allocation**".

Every item on that list resolves to something a record already holds, except the decoded-size bounds — and those are the seven C# constants `CategoryRegistry.MaxCategories`, `CategoryDefinition.MaxParameters`, `MaxConnectors`, `MaxDomains`, `MaxGeneratorRevisions`, `ParameterDescriptor.MaxEnumLabels`, and `TagList.MaxTags`. A reader that must refuse an over-budget encoding before allocating has to know the budget from the bytes it is opening, not from the build opening them; today it knows it only from the build. That is what the phrase means, and satisfying it is a relocation rather than an invention.

**Per revision, in the record's header, written once before the categories:** the seven bounds above, as varints, in that order. A decoder reads them first and applies them to everything after, so an over-budget count is refused against the record's own declared bound.

**Per category, beside the derived-instance budget it already carries:** `max-children-total`, the greatest number of children a node of this category may hold across all its connectors. The per-connector maxima already bound each connector; their sum is the implicit fan-out bound a decoder must allocate against, and making it explicit is the same relocation one level down. `maxDerivedInstances` is unchanged and is the one validation limit the record already held, under its own name ([decision 0038](0038-derived-instances.md)).

### 4. A derived parameter carries a full descriptor

`DerivedParameter` is a label and a rule identifier, so a computed quantity has no unit, no scale, no range, and, for `spectral-class`, no labels — its seven classes live only in `DerivationRules.SpectralClasses`. A derived value is read by the description and by other rules and has exactly the interpretability problem a stored one has. From revision 4 a derived parameter carries **the same descriptor a stored parameter carries, minus the default**, which nothing supplies, plus its rule identifier. `star.luminosity` then reads `W` at exponent 20, `planet.radius` reads `m` at eight fraction bits, and `star.spectral-class` carries its labels in the record.

The stored-or-computed declaration of [decision 0037](0037-derivation-rules-integer-periods-and-orbit-hierarchy.md) is **not** added: [decision 0049](0049-suit-profile-and-system-description-version-1-and-derivation-rules-as-named-implementations.md) defers it until a graph must reference a rule rather than a tool applying one, nothing here changes that, and every derived parameter this revision declares is computed on demand.

### 5. A barycentre carries the mass decision 0037 already gives it

[Decision 0037](0037-derivation-rules-integer-periods-and-orbit-hierarchy.md) says an orbit connector's parent may be "an explicit `barycentre` primitive, which is non-rendered and whose mass is the aggregated sum of its children's". No registry revision declares it, which is half of [finding 53](../review.md#53-a-barycentre-holds-nothing-so-a-published-pair-contradicts-its-own-masses). Revision 4 declares it: `barycentre` gains a derived parameter `mass`, unit `kg` at exponent 20, matching a `star`'s and a `planet`'s, named by an aggregation rule that sums its children's `mass`.

This is the first aggregation-rule primitive [decision 0031](0031-primitive-complete-composition-and-storage.md) specifies and nothing implements, so one thing must be settled with it: an aggregation rule needs an identifier form. The recommendation is `aggregate-<name>/<k>` — `aggregate-sum/1` here — alongside `derive-<name>/<k>`, validated by the same `GeneratorRevisionIdentifier` rule and retained under the same rule of [decision 0035](0035-category-registry-record-and-generator-revision-identifiers.md), with the derived-parameter field admitting either form. The technical design already speaks of the `sum` aggregation rule twice, for an artifact's mass and for a barycentre's, so one identifier serves both.

Taking only the registry half here is deliberate. **Placing the pair by the mass ratio is a grammar version, not this revision**: it changes how the two companions' semi-major axes, eccentricities, and mean anomalies are drawn, which is grammar version 5's work over registry revision 4, and finding 53 stays open until both land.

### Encoding

Revision 4's record is revision 3's with fields added, in this order and no other:

- **Header, before the category count:** the seven decoder bounds as varints, in the order given above.
- **Per parameter descriptor, after the reference category:** the unit's base symbol as a path, its decimal exponent as a varint, then the default in its kind's form — absent for `PrimitiveRef`.
- **Per category, after `maxDerivedInstances`:** `max-children-total` as a varint.
- **Per derived parameter:** the full descriptor in the order above, minus the default, then the rule identifier as a path.

A revision earlier than 4 writes none of it, so revisions 1 to 3 re-encode to the bytes they already have.

## Consequences

**What must move with it.** A new registry revision needs a grammar version bound to it, as revision 3 needed version 4 ([decision 0055](0055-definition-level-tags-and-tagged-references.md)), so grammar version 5 arrives with it and is the natural home for finding 53's placement rule. The `basic` vocabulary is authored for one revision and must be re-authored for this one; under the new default rule it must also state ranges for `pole-right-ascension` and `prime-meridian-phase` on its six body templates, which it omits today and which would otherwise pin to a default. That moves the vocabulary hash and the set pack, and therefore reissues the iteration rows that pin them — the same cost [inspection finding 3](../inspection.md#3-artifact-partmaterial-is-untagged-while-its-pool-widened) is already waiting to spend, so the two belong in one pass.

**Two constants move into the record and one out of the registry.** `MassUnitExponentKilograms` and `LuminosityUnitExponentWatts` become the exponents of the `kg` and `W` units and are trimmed under [decision 0024](0024-tests-comments-index-and-scaffolding-trimmed.md). `EarthMass`, `SolarMass`, and `SolarLuminosity` stay: they are reference values the description renders against and `derive-orbit-scale/1` divides by, not units. `MinimumLandableRadius` leaves the registry class for the region-limits record under [decision 0059](0059-region-limits-are-a-pinned-record.md).

Easier: a definition record becomes interpretable and not merely decodable, which is what decision 0035 promised and is the condition for anything outside this codebase reading published content; the two `mass` units stop colliding under one label; a template's silence becomes a stated value instead of a silent widening; a decoder's budget travels with the bytes it bounds; and a barycentre stops being the one composed thing with no property at all. Harder: one more registry revision and grammar version to support and to retain readers for; a re-authored vocabulary and a reissued set; a closed unit vocabulary that a genuinely new quantity extends only by a revision; and one more field per parameter in a record that four revisions now share a decoder for.

**Out of scope, deliberately.** The transform schema of [decision 0036](0036-frames-and-transforms-typed-by-connector-kind.md) has the same problem and is not fixed here: a semi-major axis in metres, an eccentricity as a fraction of 2^32, an epoch in ticks, and none of it in a record, because `TransformSchema` is code and not a record at all. When it becomes one it should take this unit vocabulary rather than a second one. A **life category is not added**: making [finding 52](../review.md#52-life-is-a-structural-absence-so-the-second-tier-can-never-be-composed)'s flag countable by declaring an empty category is scaffolding ahead of need ([decision 0002](0002-primitive-set-content-gate.md), [decision 0024](0024-tests-comments-index-and-scaffolding-trimmed.md)); life arrives with the vocabulary and grammar of its own domain.

**What must be verified.** The Primitive-set foundation and Compatibility checks of the [development plan](../development-plan.md#verification-and-performance-targets): that revisions 1 to 3 re-encode to their recorded hashes unchanged, which is [decision 0050](0050-registry-revision-2-grammar-version-2-and-versioned-record-growth.md)'s growth rule held in fact; that a pack published under an earlier revision still loads beside one published under revision 4; that every unit symbol outside the closed vocabulary, every default outside its parameter's range, and every count above a declared header bound is refused by name; and that a barycentre's derived mass equals the sum of its children's on a composed pair.

## The open question

One clause above is a judgement rather than a reading, and it is the owner's. **Should a category also carry an instance budget — the most instances of it one composition graph may hold?**

The documents' position: nothing asks for one. The grammar bounds a graph's total node count and depth ([decision 0048](0048-composition-grammar-version-1-graph-records-and-graph-publication.md)), each connector bounds its own children, and the admission list's "graph and decoded-size bounds" is satisfied by the header bounds and `max-children-total` above.

The recommendation is **no**. Nothing consumes a per-category instance budget, the grammar already stops a graph growing without limit, and house style is to trim rather than scaffold ahead of need. If it is wanted anyway — as a guard against one category exhausting a graph's node budget before a required child is placed — it is one varint per category and belongs in this revision rather than a later one, because adding it afterwards is another registry revision.

## Applied to

Nothing yet: this record specifies a format and is `Proposed`. On acceptance it applies to `src/SpaceExplorer.Core/Registry/`, a re-authored `content/templates/basic/vocabulary.json`, [review finding 50](../review.md#50-a-stored-quantity-carries-no-unit-no-default-and-no-validation-limit) and the registry half of [finding 53](../review.md#53-a-barycentre-holds-nothing-so-a-published-pair-contradicts-its-own-masses), the [technical design](../technical-design.md#primitive-registry-and-composition), the [glossary](../glossary.md#content-identity), and [progress](../progress.md#solar-system-iterations).
