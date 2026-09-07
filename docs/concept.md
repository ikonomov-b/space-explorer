# Space Explorer: game concept

A PC game about travelling through wormholes, exploring unfamiliar planetary systems, and collecting distinctive artifacts.

This document describes the game concept. The [assessment](assessment.md) discusses its relevance and similar projects. The [development plan](development-plan.md) and [technical design](technical-design.md) cover proposed implementation and validation.

## 1. The general idea

The player commands an imaginary spacecraft capable of travelling between stars through wormholes. Each journey leads to a generated planetary system that can be explored, remembered, and visited again.

The intended experience combines anticipation before a journey, discovery in unfamiliar environments, and the satisfaction of assembling a personal collection. It is aimed at players who enjoy exploration, collecting, and planning expeditions. Friends can join the player to explore previously discovered destinations.

The central identity is a sequence of purposeful artifact expeditions. The player chooses where to go, investigates what makes a place interesting, and decides which discoveries to bring home.

## 2. Destination programming

A simple ship dashboard lets the player influence the next destination without knowing its exact contents. Travel distance is the main control. Additional environment and survey preferences can bias the kinds of planets and artifacts encountered while preserving surprise.

These settings express an explorer's intentions rather than requiring the player to write code. The player understands the broad cost, demands, and possibilities of a journey before committing, but exact landscapes, sites, and finds remain unknown.

Known destinations remain selectable for return visits. A recorded destination represents a particular place with its own exploration history, rather than another roll of the same settings.

## 3. The destination planetary system

Every destination is a planetary system inspired by current astronomical knowledge. It contains at least two planets with regions that a person could explore using a plausible fictional space suit, and at least one planet inhabited by some form of life. The inhabited planet may also be one of the two explorable planets.

Suit-survivable does not mean breathable, harmless everywhere, or safe indefinitely. Different combinations of gravity, pressure, temperature, radiation, and chemical hazards give planets their character and make preparation matter.

Habitable means capable of supporting life; inhabited means life is actually present. The concept requires the latter on at least one planet, which may support simple or unfamiliar organisms rather than an intelligent civilization.

Wormholes and alien life belong to the fiction. The navigator selects destinations suitable for expeditions, explaining why the player consistently encounters explorable systems. This guarantee is a gameplay premise, not a claim about how common such systems are in reality.

## 4. Travel and the randomness of the destination

Travel represents the creation and gradual revelation of a destination. The original idea allows more distant journeys to produce more complex, detailed worlds, with preparation potentially lasting days and no universal ceiling on journey duration.

Greater distance should mean richer places to investigate: more varied environments, relationships between sites, and more intricate artifacts. Distance acts as a progression choice within the fiction rather than a scientific explanation for planetary complexity.

A proposed refinement is to offer short expeditions alongside optional long journeys. Long preparation can build anticipation while the player reviews a collection or returns to earlier discoveries. Whether long waits should be central to progression remains a design question discussed in the assessment.

The experience must justify the wait through discoveries the player can recognize. Once a place is discovered, further preparation must not change its identity or erase the player's knowledge of it.

## 5. Remembering destinations and exploring together

Generated systems are kept on the player's local computer as a lasting archive of discoveries. A saved destination supports multiple expeditions and can be shared with an invited player over the internet.

The identity of the world endures while its history develops. Landscapes and artifact origins remain recognizable; collected items stay collected, and discoveries remain recorded. Returning with better equipment or a friend creates another opportunity to investigate the same place.

The proposed initial social experience is cooperation in the host's campaign, contributing to a shared expedition history and collection. A persistent global universe or universal market is not essential to this concept.

## 6. The artifacts

Artifacts are the main motivation for exploration. They occur throughout explorable regions and have varied physical characteristics, distinctive appearances, and a predefined relative value.

Possible families include unusual mineral formations, preserved biological specimens, and manufactured relics. Their shapes, materials, arrangements, and environmental context make them recognizable. Their origin, discoverer, and recovery history give a collection meaning beyond its sale price.

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

Planetary systems, explorable environments, and artifacts are composed from predefined primitives that can combine into larger structures. Compatible combinations create variety while retaining coherence.

Each primitive has a permanent numeric identity. The library can expand with new primitives, while existing worlds keep the content and composition that defined them. New possibilities enrich future destinations without rewriting old discoveries.

The same broad principle of constrained composition applies at different scales, from a system's arrangement to a planet's environments and an artifact's shape. Each scale has its own compatibility rules.

Randomness serves variety and surprise. Successful combinations should produce places and objects the player can understand, distinguish, and remember.

## 9. The core experience

The core game contains the basic structures and a minimal usable set of primitives sufficient for a complete expedition: choose a destination, explore its planets, recover artifacts, trade or collect them, and return to a remembered world.

Its value comes from this connected experience and the promise of further discoveries as the primitive library grows.
