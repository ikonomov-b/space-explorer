# Space Explorer: game concept

A PC game about travelling through wormholes, exploring unfamiliar planetary systems, and collecting distinctive artifacts. Platform requirements are recorded separately in [requirements.md](requirements.md).

This document describes the game concept. The [assessment](assessment.md) discusses its relevance and similar projects. The [development plan](development-plan.md) and [technical design](technical-design.md) cover proposed implementation and validation.

## 1. The general idea

The player commands an imaginary spacecraft capable of travelling between stars through wormholes. Each journey leads to a generated planetary system that can be explored, remembered, and visited again.

The intended experience combines anticipation before a journey, discovery in unfamiliar environments, and the satisfaction of assembling a personal collection. It is aimed at players who enjoy exploration, collecting, and planning expeditions. A friend can join the player to explore previously discovered destinations; larger groups are a later expansion.

The central identity is a sequence of purposeful artifact expeditions. The player chooses where to go, investigates what makes a place interesting, and decides which discoveries to bring home.

## 2. Destination programming

A simple ship dashboard lets the player influence the next destination without knowing its exact contents. Travel distance is the main control. The original idea added environment and survey preferences that bias the kinds of planets and artifacts encountered while preserving surprise.

The adopted refinement gives the dashboard two controls. The distance control selects a tier and shows it as a light-year value. A second lever seeds the destination in a near-unique way; the player sees it only as an abstract cue such as a glow level, never as a number, and has no reason to remember it, because a discovered place is reached again through its record rather than through its seed. Environment and survey preferences are an expansion option ([decision 0043](decisions/0043-destination-controls-distance-tier-and-seed-lever.md)).

These settings express an explorer's intentions rather than requiring the player to write code. The player understands the broad cost, demands, and possibilities of a journey before committing, but exact landscapes, sites, and finds remain unknown.

Known destinations remain selectable for return visits. A recorded destination represents a particular place with its own exploration history, rather than another roll of the same settings.

## 3. The destination planetary system

Every destination is a planetary system inspired by current astronomical knowledge, with one star. The original idea has every system contain at least two planets with regions that a person could explore using a plausible fictional space suit, and at least one planet inhabited by some form of life; the inhabited planet may also be one of the two explorable planets.

The adopted refinement makes this depend on the journey. The shortest journeys lead to a small system with exactly one explorable planet and no life. Every longer journey keeps the original rule, at least two explorable planets and at least one planet with life, and brings more planets and more landing regions the further it goes. Whether a planet is explorable is decided by testing its surface against the suit, not by its kind: a world of gas has no surface to stand on, while an ocean world with land or a volcanic world with a calm region can qualify ([decision 0044](decisions/0044-tiered-system-composition-one-star-and-explorability-by-validation.md)). The other planets of the system are seen from the surface at their true size and phase; the core release has no closer view of them ([decision 0045](decisions/0045-close-views-only-in-the-surface-sky-and-a-derived-system-description.md)).

Suit-survivable does not mean breathable, harmless everywhere, or safe indefinitely. Different combinations of gravity, pressure, temperature, radiation, and chemical hazards give planets their character and make preparation matter.

Habitable means capable of supporting life; inhabited means life is actually present. The concept requires the latter on at least one planet of every journey beyond the shortest, which may support simple or unfamiliar organisms rather than an intelligent civilization.

Wormholes and alien life belong to the fiction. The navigator selects destinations suitable for expeditions, explaining why the player consistently encounters explorable systems. This guarantee is a gameplay premise, not a claim about how common such systems are in reality.

On arrival the traveller reads a description of the system: the kind of star, and each planet with its type, whether it can be explored, and whether it bears life. The same text, produced from the generated data and never stored separately, is the first check that generated systems vary as intended ([decision 0045](decisions/0045-close-views-only-in-the-surface-sky-and-a-derived-system-description.md)).

Standing on a planet must reveal the generated system coherently. The star's position and illumination, the motion of the sky as the planet rotates, and the position, phase, apparent size, horizon visibility, eclipses, and occultations of other generated celestial bodies follow the observer's surface location and the saved astronomical time. An atmosphere changes scattering, twilight, extinction, and the horizon treatment; an airless world does not invent them. The core release uses bounded deterministic analytic propagation rather than full n-body simulation, but fixed decorative planets or an unrelated light direction are not an acceptable substitute ([decision 0032](decisions/0032-astronomically-consistent-surface-sky.md)).

## 4. Travel and the randomness of the destination

Travel represents the creation and gradual revelation of a destination. The original idea allows more distant journeys to produce more complex, detailed worlds, with preparation potentially lasting days and no universal ceiling on journey duration.

Greater distance should mean richer places to investigate: more varied environments, relationships between sites, and more intricate artifacts. Distance acts as a progression choice within the fiction rather than a scientific explanation for planetary complexity.

The adopted refinement keeps preparation as the real time the host's machine needs to generate the destination, never an artificial timer. Short tiers come first and compute in seconds to minutes; more distant tiers carry larger content budgets and therefore take longer. Days-long journeys with very large budgets are an expansion experiment. Long preparation can build anticipation while the player reviews a collection or returns to earlier discoveries ([decision 0004](decisions/0004-preparation-is-real-generation-time.md)).

The experience must justify the wait through discoveries the player can recognize. A slower computer waits longer for the same destination and the same values. Once a place is discovered, further preparation must not change its identity or erase the player's knowledge of it.

## 5. Remembering destinations and exploring together

Generated systems are kept on the player's local computer as a lasting archive of discoveries. A discovered system is reachable only through the person who discovered it: that player hosts every visit and can invite a guest to travel there together and explore side by side. A saved destination supports multiple expeditions, but it never moves into another player's archive ([decision 0005](decisions/0005-discoverer-hosts-every-visit.md)).

The identity of the world endures while its history develops. Landscapes and artifact origins remain recognizable; collected items stay collected, and discoveries remain recorded. Returning with better equipment or a friend creates another opportunity to investigate the same place.

The proposed initial social experience is cooperation in the host's campaign, contributing to a shared expedition history and collection. A persistent global universe or universal market is not essential to this concept.

## 6. The artifacts

Artifacts are the main motivation for exploration. They occur throughout explorable regions and have varied physical characteristics, distinctive appearances, and a predefined relative value.

Possible families include unusual mineral formations, preserved biological specimens, and manufactured relics; relics include technical pieces and works of art whose makers are never encountered. Artifacts range from bean-sized to human height, and a large find costs more cargo room and a harder recovery than a small one, while a rare material makes a mineral rarer rather than differently priced ([decision 0046](decisions/0046-artifact-families-human-height-cap-cargo-by-mass-and-material-rarity.md)). Their shapes, materials, arrangements, and environmental context make them recognizable. Their origin, discoverer, and recovery history give a collection meaning beyond its sale price.

Uniqueness should be visible as well as recorded: changing only an item's name does not make a discovery feel new. The intended collection avoids duplicate artifacts within a campaign; guaranteeing perceptually different objects forever across every player's worlds is an aspiration that a finite primitive library cannot promise.

Artifacts should be found through investigation: observing a landscape, following scanner clues, reaching an unusual site, and deciding whether a find merits the cargo space and effort needed to recover it.

The original value idea is proportionality to the time spent generating a system plus the time spent exploring it. This expresses the wish for more ambitious journeys and demanding discoveries to yield more valuable finds. It also leaves a tension between a predefined value and a player's future exploration time.

A proposed refinement is to base value on the destination's generation effort and the expected difficulty of recovery, with separate recognition for completed discoveries. This keeps the connection between effort and reward without making idling or a slower computer increase an artifact's worth. The assessment records this as a proposed change to the original rule.

## 7. The player's goal

The player collects artifacts, sells and buys them, and uses accumulated credits to reach more distant destinations with more complex worlds and potentially more valuable discoveries.

The recurring experience is choosing a destination, travelling, surveying a planet, locating and recovering artifacts, returning home, and deciding what to retain or sell. Credits fund future travel and equipment; a growing collection provides a parallel sense of progress.

An artifact can be worth keeping because it is beautiful, unusual, completes a collection, or recalls a memorable expedition. Selling therefore involves a choice between preserving a find and financing another discovery.

Environmental challenges and limited carrying capacity give expeditions stakes. Failure should create a recoverable setback while leaving the player able to continue exploring. Progress remains open-ended: distant travel, collection goals, and returning to unfinished discoveries supply reasons to continue.

## 8. Randomness from expandable primitives

Planetary systems, explorable environments, life, and artifacts are represented completely by primitives that combine into larger structures. A small predefined vocabulary supplies versioned templates and constraints for generating random primitive sets. These sets are saved as reusable construction catalogues before they are used to compose destinations and discoveries. Compatible combinations create variety while retaining coherence; there is no separate environment description that bypasses the primitive composition.

Each primitive definition has a permanent numeric identity and an exact content revision. Its instances carry bounded generated parameters and stable paths. Materials, textures, geometry, collision, physical properties, and behaviour are themselves reusable primitive concerns rather than engine-owned facts. The library can expand with new primitives, while existing worlds pin the definitions, rules, and parameters that preserve them exactly. New possibilities enrich future destinations without rewriting old discoveries.

The same broad principle of constrained composition applies at different scales, from a system's star, planets, and orbits to planetary layers, surfaces, rivers, caves, vegetation and life, and an artifact's shape. A generated object is an ordered, bounded graph of primitive instances, not merely a deduplicated set of IDs. Each scale has composition domains, typed connectors, and explicit compatibility rules.

Exact primitive data is authoritative. Expensive derived meshes, textures, collision products, and spatial chunks may be cached during exploration and discarded at any time. Whether a primitive's accepted output is regenerated, stored, or partly materialized is decided per category from cross-platform determinism and performance measurements ([decision 0031](decisions/0031-primitive-complete-composition-and-storage.md)).

Randomness serves variety and surprise. Successful combinations should produce places and objects the player can understand, distinguish, and remember.

## 9. The core experience

The core game contains the basic structures and a minimal usable set of primitives sufficient for a complete expedition: choose a destination, explore its planets, recover artifacts, trade or collect them, and return to a remembered world.

Its value comes from this connected experience and the promise of further discoveries as the primitive library grows.
