# 0051. Grammar version 3: orbits scaled to the star, and a body suited to the zone it takes

Date: 2026-09-08. Status: Accepted. Source: the owner's instruction of 2026-09-08 to proceed to solar-system iteration 3, acting on the observations iteration 2 recorded in [progress](../progress.md#solar-system-iterations). Extends [decision 0050](0050-registry-revision-2-grammar-version-2-and-versioned-record-growth.md) with a fourth derivation rule and three grammar fields; it needs no registry revision.

## Context

Iteration 2 fixed what contradicted itself *inside* a body. Three of the five things it left are relationships *between* bodies, and the sample showed each of them plainly. Fifty-three of its 103 moons outweighed the planet they orbited, one of 519 Earth masses about a planet of 0.21, because a moon is a planet on a planet's orbit connector and nothing related the two masses. Ice appeared at 282 K and equilibrium temperatures reached 1,459 K, because a body's type was unrelated to where it sat and because every star handed its planets the same absolute distances however bright it was: the bands were measured from a fixed base, so a system around a star of a thousandth of the Sun's output was as cold as one around a star of a thousand times it was hot.

## Decision

**A star's orbit bands scale with its luminosity.** `derive-orbit-scale/1` gives the square root of a star's luminosity in solar units, from the luminosity `derive-luminosity/1` already derives, and a connector rule may name it. Every band of that connector is multiplied by it. A body at `sqrt(L)` times the distance receives the same flux and so reaches the same temperature, so one set of bands means one sequence of temperatures around any star: a faint dwarf gets a compact system and a luminous star a wide one, and neither is uniformly frozen or uniformly scorched.

**A band's base is stated apart from its range.** Once a scale moves a progression, the range a band must stay inside is far wider than the progression itself, so a rule states where its innermost band begins separately from the envelope every scaled band is clamped to. Conflating the two, as version 2 did by taking the range's minimum as the base, collapses every band onto the minimum the moment the envelope is widened.

**A band may demand a tag.** A connector rule may state one tag per band, and the child taking that band must provide it as well as whatever the parent's own declaration requires. A body therefore takes only an orbit it suits: the vocabulary gives each body the zones it can occupy, and ice can no longer form where it would sublimate. This is the existing connector-tag rule of [decision 0048](0048-composition-grammar-version-1-graph-records-and-graph-publication.md) with the requirement coming from the grammar as well as from the parent definition, which is what [decision 0031](0031-primitive-complete-composition-and-storage.md) means by a versioned grammar carrying semantic constraints.

**A connector may pass its own zone down.** A body that shares its parent's distance from the star shares its temperature, so a rule may declare that its children inherit the band tag their parent was chosen for rather than taking one of their own. A moon and the pair of a barycentre inherit; without it a band's demand stops at the first generation and ice reappears at 414 K on a moon, which is what the sample showed before the rule existed.

**Grammar version 3** carries all four over category registry revision 2, unchanged: planets spaced at 1.55 from three tenths of an astronomical unit at unit scale, inside an envelope of 0.005 to 400 astronomical units that holds every scaled band from a cool dwarf's innermost to a hot star's outermost; the first two bands demand `zone-hot`, the next three `zone-temperate`, the last three `zone-cold`, which at one solar luminosity are 464 K down to 80 K; and the moon and barycentre connectors inherit. The `basic` vocabulary states each body's zones and a mass class on its orbit connector, and requires of its own moons a class below its own, so a moon cannot outweigh its planet. Registry revision 1 and 2 and grammar versions 1 and 2 are all still supported, so both earlier iterations reproduce.

## Consequences

Easier: a system reads as a system, with an inner and an outer part whose temperatures follow from the star rather than from a constant; a body's type, its mass relative to its planet, and its place in the system agree; and the mechanism is data in the grammar and tags in the vocabulary, so the next tuning is a grammar version and a vocabulary, not code. Harder: the grammar carries four more things to get right, and two of them interact, since a band tag no body provides fails the run rather than filling the band with something unsuitable; the vocabulary must now provide every zone a band can demand, which is a coupling between two authored files that only generation reveals; and a body's zones are stated by hand, so a template whose parameters change may need its tags revisited.

Iteration 3's measurements are in [progress](../progress.md#solar-system-iterations). Its remaining observations stand for the owner's verdict, chiefly that an atmosphere is still unrelated to the gravity that would have to hold it, and that the starter tier's rule of exactly one explorable planet is met by 5 systems of 24 because making systems habitable made more of them habitable than the rule allows.

Verified by the Destination validity and Determinism checks in the [development plan](../development-plan.md#verification-and-performance-targets): the vector of `derive-orbit-scale/1`, the frozen hashes of all three grammar versions, and the composition tests that a body takes only the band whose zone it provides and that an inheriting connector hands its zone down, on Linux and Windows.

## Applied to

- `src/SpaceExplorer.Core/Registry/`, `src/SpaceExplorer.Core/Derivation/`, `src/SpaceExplorer.Core/Generation/`, `content/templates/basic/vocabulary.json`, and the matching tests
- [Progress: completed steps, solar-system iterations](../progress.md#solar-system-iterations)
- [src/README.md](../../src/README.md)
