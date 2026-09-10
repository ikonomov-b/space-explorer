# 0057. The review view draws what the content stores: one size scale, the stored pole, the stored orbit, the stored air, and a star's colour from its temperature

Date: 2026-09-10. Status: Accepted. Source: the owner's observation of 2026-09-10 that the star's size is unrealistic against the planets', his choice of the power-law rule among three, and his instruction to take the star's colour, the eccentric orbits and the atmosphere into the same pass. Supersedes the body-radius clause and the circular-orbit clause of [decision 0052](0052-the-two-levers-compose-a-destination-and-a-review-view-shows-it.md); its caption, movement, and harness clauses stand, as does the panel rule of [decision 0054](0054-the-review-panel-carries-the-summary-while-the-system-is-framed.md). Adds the derivation rule `derive-star-colour/1` under [decision 0037](0037-derivation-rules-integer-periods-and-orbit-hierarchy.md).

## Context

[Decision 0055](0055-definition-level-tags-and-tagged-references.md) put a body's material in the database and had the view resolve it, on the principle that no rendered property may be invented where a record holds one ([concept 8](../concept.md#8-randomness-from-expandable-primitives), and the primitive-completeness invariant of the [technical design](../technical-design.md#primitive-registry-and-composition)). An audit of the review view against that principle, asked for by the owner on 2026-09-10, found the principle held for structure, placement, material and nothing else. Five properties the content stores were either invented in the viewer or ignored:

- **The star's size was a constant.** A dwarf of 169,077 km and a giant of 1,058,024 km drew the same disc, though their records differ sixfold.
- **The size scale flattened the rest.** Logarithmic placement between the system's smallest and largest body drew a star nine times a gas giant at a third larger, and on seed 13 a star eighty-four times its one body at twice its size. The legend claimed ratios were faithful, which was not true of what a reader saw.
- **Every body stood upright.** The pole right ascension, pole declination and prime-meridian phase that registry revision 2 stores on each body were unused, so iteration 4's banded gas giants were seen down their own axes and read as bullseyes.
- **The star's colour came from a palette in the viewer**, keyed on a spectral class the content derives, when the content also stores the effective temperature that class comes from.
- **Three of the seven stored orbital elements were ignored** — eccentricity, ascending node, argument of periapsis — so every orbit was drawn as a circle; and **the atmosphere was not drawn at all**, though a body stores its model, surface pressure, composition and scattering tint, and pressure is the commonest reason a body is refused.

## Decision

**One size scale over the star and every body.** The largest stored radius in the system takes three units, and everything else is drawn at that times the ratio of its own stored radius to the largest, raised to the power `0.4`. A barycentre keeps a fixed small mark, which is also the floor. The exponent is the owner's choice among three: it draws the sample's seed 8 star two and a half times its gas giant where the truth is nine, and fourteen times a small moon where the truth is eight hundred and twenty, while leaving that moon some ten pixels across in the framed view. A square root would read closer to the truth and take the smallest moons to about five pixels; the logarithm it replaces kept every moon large and every star small.

**A body stands on its own pole.** Its mesh is turned by the pole right ascension and declination its definition stores and spun to its stored prime-meridian phase, so a banded surface reads as latitude. A category storing no such parameter is drawn upright.

**A star's colour is derived from its effective temperature** by `derive-star-colour/1`, a named rule in the core beside the others, interpolating in integers between ten anchor colours that approximate the black-body locus and clamping outside them. The anchors are chosen to look right rather than taken from a published table, and the record says so; what matters is that the property is derived from what the star stores rather than chosen in a viewer.

**An orbit is the ellipse its elements describe.** The stored eccentricity, inclination, ascending node and argument of periapsis shape and orient it, with the star at a focus, and the body stands where its stored mean anomaly puts it, by Kepler's equation solved from the mean anomaly. Only the semi-major axis is remapped, logarithmically between the system's innermost and outermost, as decision 0052 already had it. This is presentation in floating point over a stored record, and nothing here moves with time: the two-body propagation of [decision 0037](0037-derivation-rules-integer-periods-and-orbit-hierarchy.md) is still unimplemented and this does not stand in for it.

**An atmosphere is drawn as the shell its record describes**, in the scattering tint it stores, standing off the surface and hiding the body by amounts that follow the logarithm of its stored surface pressure. An airless atmosphere, which is one explicit primitive rather than an absence ([decision 0031](0031-primitive-complete-composition-and-storage.md)), draws nothing.

**The legend states what the picture is**: orbit radii logarithmic, the star and every body on one scale by the stored radius to the power 0.4, order and rank faithful and true proportion not, and a body standing on its own pole.

## Consequences

Easier: every visible property of a body in this view — where it is, how large, what colour, what texture, which way up, what it is wrapped in, and the shape of the path it sits on — is now read from a record, so a verdict on a picture is a verdict on the generator; a star is a star; and the three orbital elements a grammar draws and a record keeps are visible for the first time, which is also the first check that they are drawn sensibly.

Harder: the picture is busier and less even — small moons are small, orbits no longer nest tidily, and an eccentric orbit's ring is offset from the star, which is correct and unfamiliar; the exponent, the anchor colours, and the shell's thickness and opacity are presentation constants chosen by eye, stated on screen or here, and a later reading may move any of them; and iteration 4's captured sample predates all of it, so the sample its verdict is read from is re-rendered, the pins unchanged because no content moved.

Verified by `derive-star-colour/1`'s tests — the anchors, monotone blue against temperature, the clamps, and a refusal at zero — by the re-rendered sample of [progress](../progress.md#solar-system-iterations), and by the exported-build smoke check, which the review path must not disturb. No record, hash, or content changes, so no test vector moves.

## Applied to

- `src/SpaceExplorer.Core/Derivation/DerivationRules.cs`, `src/SpaceExplorer.Game/SystemView.cs`, and the matching tests
- [Progress: M0a exit criteria, solar-system iterations](../progress.md#solar-system-iterations)
- [docs/tools.md](../tools.md#godot)
