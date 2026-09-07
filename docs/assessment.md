# Concept assessment and similar projects

Assessment date: 2026-09-07. Scope: the original nine-point concept, the local workspace, and public primary sources. The workspace contained no application code or other project documentation to compare against. Research used official developer pages, developer documentation, and publisher-controlled store descriptions. No games were installed or playtested.

## Relevance and recommendation

The concept is relevant as a focused exploration-and-collecting game, but procedural systems, space travel, artifact discovery, trading, and cooperative exploration have substantial precedents. No Man's Sky's own description combines generated systems, alien life, ancient artifacts, and trading for equipment; its multiplayer material adds shared exploration. [Official concept](https://www.nomanssky.com/about/), [multiplayer update](https://www.nomanssky.com/en/next-update/).

The strongest proposition to test is: **program an expedition, discover a distinctive collection of artifacts, and keep a dependable local archive of worlds to revisit with friends**. This is a proposed focus, not evidence of exclusive novelty or unmet commercial demand. Immutable saves have a useful player benefit: confidence that places, discoveries, and collections persist.

Recommend a small playable prototype that demonstrates the expedition-and-trade loop and save compatibility. Test whether destination choices and artifact investigation produce meaningful decisions before committing to whole planets or days of computation. Audience demand, commercial viability, team capacity, budget, and delivery dates remain unproven.

The [concept](concept.md) contains only the game idea. Scope, milestones, measurements, and production decisions belong in the separate [development plan](development-plan.md); implementation contracts belong in the [technical design](technical-design.md).

## Similar projects

The overlap column reports supported features. Implications are analysis for Space Explorer, not claims that competitors lack every other feature. Store descriptions establish advertised scope; historical development posts are used only as engineering evidence. This is a targeted comparison, not an exhaustive catalogue.

| Project | Supported overlap | Implication for Space Explorer |
| --- | --- | --- |
| **No Man's Sky** — close gameplay comparison | Procedural galaxy, planetary exploration, life discovery, ancient artifacts, trade, and multiplayer exploration. [About](https://www.nomanssky.com/about/), [NEXT](https://www.nomanssky.com/en/next-update/). | Generated worlds plus collectible finds are established. Test artifact identity, expedition controls, and dependable revisits as a combined experience. |
| **Elite Dangerous** — exploration/progression comparison | Connected galaxy, discovery, planetary surface exploration by vehicle, trading economy, ship customization, and cooperative wings. [Frontier's description](https://store.steampowered.com/app/359320/Elite_Dangerous/). | Space travel and wealth-funded equipment progression are familiar. Bounded surface expeditions and private campaigns give the proposed game a narrower scope. |
| **Starbound** — content/extensibility comparison | Procedural universe, planet exploration, treasure hunting, collectible creatures, multiplayer, and support for adding items, planets, dungeons, and quests. [Chucklefish's description](https://store.steampowered.com/app/211820/Starbound/). | An expandable library is useful infrastructure, but its resulting encounters and collections must justify it to players. |
| **Astroneer** — planetary cooperation comparison | Seven-body system, exploration, terrain manipulation, mysterious structures/artifacts, and 2–4 player online cooperation. Developers describe planets generated when a save is created. [System Era's description](https://store.steampowered.com/app/361420/ASTRONEER/), [generation notes](https://blog.astroneer.space/patch-notes/patch-43/). | A limited collection of explorable worlds is a useful scope precedent. Study generated-site validation and shared state; terrain editing is outside Space Explorer's proposed core. |
| **Empyrion – Galactic Survival** — space survival comparison | Procedurally generated galaxy, travel between systems, planetary landing, hazardous planet types, construction, and solo/cooperative play. [Official FAQ](https://empyriongame.com/faq/), [multiplayer overview](https://empyriongame.com/single-and-multiplayer/). | Its broad mix demonstrates how much the project could expand. Keep construction, combat, and industrial crafting outside the initial artifact loop. |
| **SpaceEngine** — scientific/visual reference | Combines astronomical catalogues with procedural stars, planets, and landscapes for unknown objects. [Celestial objects](https://spaceengine.org/universe/real-celestial-object), [project site](https://spaceengine.org/). | Useful reference for coherent systems and scale. A simulator does not by itself validate an artifact economy or progression design. |
| **Pioneer** — open-source technical reference | Open-ended space travel, planetary landing, exploration, trading, and missions. It publishes source and developer documentation; its FAQ describes reproducible procedural worlds. [Repository](https://github.com/pioneerspacesim/pioneer), [FAQ](https://wiki.pioneerspacesim.net/wiki/FAQ), [developer docs](https://dev.pioneerspacesim.net/). | A candidate reference for generation, simulation, and content authoring. This review has not audited its code or established component suitability or reuse terms. |
| **Wormhole Adventurer** — smaller wormhole comparison | Procedural wormhole network, mining, ship customization, crafting, and turn-based combat to rebuild a station. [Developer's description](https://store.steampowered.com/app/1959940/Wormhole_Adventurer/). | Wormholes already structure generated exploration. Its station-rebuilding goal offers a comparison for giving exploration a concrete progression objective. |

The sources reviewed did not establish an exact equivalent of making artifact value directly proportional to destination-generation time plus exploration time. That does not prove this rule is novel, enjoyable, or commercially useful. Its pacing and fairness problems require explicit treatment.

## Gaps addressed in the documents

These resolutions preserve the expedition-and-artifact objective while making ambiguity explicit. New defaults are proposals; timing, pricing, and release choices are not presented as pre-existing requirements.

| Original area or missing requirement | Gap or risk | Documented resolution |
| --- | --- | --- |
| 1. General idea | No target player, central activity, or distinctive benefit. | [Concept 1](concept.md#1-the-general-idea) defines the audience hypothesis and expedition/collection experience; this assessment tests its relevance against precedents. |
| 2. Destination programming | "Plant randomness" did not explain player agency or return visits. | [Concept 2](concept.md#2-destination-programming) describes intentions, uncertainty, and known destinations. Controls, pricing previews, and distance tiers are specified in the [plan](development-plan.md#scope-and-working-assumptions). |
| 3. Planet requirements | Ambiguous "inhabitable" and no distinction between suit survival, habitat, and life. | [Concept 3](concept.md#3-the-destination-planetary-system) preserves two explorable planets and one inhabited planet, clarifies overlap, and explains fictional destination selection. The [lifecycle](technical-design.md#generation-lifecycle) defines validation. |
| 4. Unlimited generation time | Waiting could block play; extra computation alone does not establish interesting content. | [Concept 4](concept.md#4-travel-and-the-randomness-of-the-destination) retains long travel and proposes short trips alongside optional long preparation. The [plan](development-plan.md) and [lifecycle](technical-design.md#generation-lifecycle) add budgets, checkpoints, cancellation, and bounded failure. |
| 5. Saving and online visits | No distinction between immutable geography, player history, host ownership, or updates. | [Concept 5](concept.md#5-remembering-destinations-and-exploring-together) explains enduring places and evolving history. [Persistence](technical-design.md#persistence-and-compatibility) and [multiplayer](technical-design.md#multiplayer-and-trust-boundary) specify the contracts. |
| 6. Artifacts | Unique IDs do not ensure unique appearances; future exploration duration conflicts with predefined value. | [Concept 6](concept.md#6-the-artifacts) clarifies visible variety and effort/reward intent. The [registry](technical-design.md#primitive-registry-and-composition) defines duplicate handling; [valuation](technical-design.md#artifact-valuation-and-transactions) proposes fixed effort scores. |
| 7. Goal/economy | No meaningful collection choices, buyers, credit sinks, failure recovery, or duplicate-sale rules. | [Concept 7](concept.md#7-the-players-goal) adds retention-versus-sale motivation. The [plan](development-plan.md#core-release-contents) and [transactions](technical-design.md#artifact-valuation-and-transactions) define progression and recovery. |
| 8. Primitives | Immutable IDs do not freeze assets or algorithms; "combinable" cannot validate an entire structure. | [Concept 8](concept.md#8-randomness-from-expandable-primitives) explains coherent combinations and preserved worlds. [Registry](technical-design.md#primitive-registry-and-composition) and [determinism](technical-design.md#destination-identity-and-determinism) pin definitions, grammar, randomness, and dependencies. |
| 9. Core release | "Basic structures" gave no usable completion boundary. | [Concept 9](concept.md#9-the-core-experience) stays conceptual. The separate [development plan](development-plan.md#milestones) defines the solo slice and connected core release. |
| Cross-cutting scope/usability | No surface scale, control baseline, performance criteria, or validation plan. | The [plan](development-plan.md) proposes bounded regions, accessibility requirements, benchmark targets, and experiments, without claiming measured results. |
| Cross-cutting trust | Local editable saves cannot establish globally trusted item ownership. | The [technical design](technical-design.md#multiplayer-and-trust-boundary) scopes the economy to private campaigns and separates future public trade from offline inventories. |

## Evidence behind the highest-risk changes

Astroneer's developers describe mission items lacking valid generated spawn locations, with missing items or indefinite loading as possible outcomes. Their fix relaxed placement conditions and could change points of interest in existing saves. This supports treating valid placement, bounded failure, and update compatibility as concrete concerns. Space Explorer's immutable-world policy is its own proposal, not a policy attributed to Astroneer. [Patch 1.43.2](https://blog.astroneer.space/patch-notes/patch-43/).

Astroneer's early multiplayer write-up describes an authoritative server/client model, limited initial cooperation, and networking complications involving procedural terrain and stateful objects. This is historical engineering evidence, not a claim about every current limitation. The implication here is to define authority and shared state during the solo prototype. [Day-1 Multiplayer and Beyond](https://blog.astroneer.space/p/day-1-multiplayer-and-beyond/).

NASA distinguishes habitable-zone planets from worlds that are habitable or actually inhabited. Preserving guaranteed life therefore requires fictional destination selection and constraints, rather than suggesting all realistic systems satisfy the concept. [NASA explanation](https://science.nasa.gov/blogs/webb/2024/06/05/reconnaissance-of-potentially-habitable-worlds-with-nasas-webb/).

The timing and pricing recommendations are design deductions: identical content can take different time on different machines, and idling increases duration without adding a discovery. Fixed reference effort supports reproducible predefined values. Whether players prefer this system remains a playtest question; competitor sources do not prove that preference.

## Decisions and validation still needed

Documentation cannot supply absent player feedback, production capacity, or benchmarks. The [decision register](development-plan.md#decisions-before-production-commitments) identifies the next evidence needed for travel pacing, valuation, engine/platform choice, numeric budgets, presentation, internet transport, schedule, and costs.

The recommended prototype focuses on short expeditions, private cooperation, and fixed effort-based prices. Mandatory days-long waiting or literal elapsed-time pricing would be material alternative designs. They remain visible in the concept rather than being silently discarded.

Recheck similar projects before public positioning or expansion. This comparison supports informed scope and prototype choices; it cannot establish exclusive novelty or commercial demand.
