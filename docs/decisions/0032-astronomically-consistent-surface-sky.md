# 0032. Astronomically consistent surface sky

Date: 2026-09-07. Status: Accepted. Source: owner request of 2026-09-07. Clarifies the exclusion of orbital simulation in the [development plan](../development-plan.md#scope-and-working-assumptions): interactive spaceflight and full n-body dynamics remain outside the core release, while deterministic celestial propagation needed for a planet-surface view is included.

## Context

The concept says destinations are inspired by current astronomical knowledge, and decision 0031 requires stars, planets, orbits, atmospheres, physical properties, and rendering to originate in the primitive graph. Neither statement required the view from an observer on a planet to agree with those properties. The current materials/environment preview demonstrates the gap: its Sun is a fixed directional light, its star field is decorative, and its two other worlds are fixed coloured discs whose shader comment explicitly says their orbits are not simulated.

The owner requires the surface view to be realistic with respect to the star's position, the observed planet's rotation, and the visibility of other planets in the generated system. This needs an observer-centred celestial model and an acceptance test; visual plausibility or independently positioned sky decorations are insufficient.

## Decision

**Astronomical state.** Every generated system has a defined inertial reference frame, a reference epoch, and a saved authoritative simulation time. Stellar and orbit primitives provide the bounded physical and orbital parameters required by a versioned deterministic propagation model. Every explorable planet provides its shape or reference radius, spin-axis orientation, sidereal rotation period, and prime-meridian phase at the epoch. Regions map their local coordinates to a planet-fixed surface location, and the observer position includes latitude, longitude, and height derived from that mapping. The exact gameplay time scale, pause behavior, and whether inactive destinations advance are versioned numeric policy to be fixed before M1; no view may use wall-clock time implicitly.

**One derived celestial solution.** At a requested simulation time, Core derives body positions in the system inertial frame, transforms them through the observed planet's rotating frame into the observer's local horizon frame, and supplies the renderer with directions, distances, angular radii, illumination geometry, and occlusion state. The visible star disc and the directional light use the same derived direction. Other generated celestial bodies are shown only when the solution places them above the geometric or atmosphere-adjusted horizon and they pass versioned apparent-size or brightness thresholds. Their apparent angular size follows body size and distance; their phase and illuminated limb follow the star-body-observer geometry. The planet and intervening bodies can occult or eclipse them. No planet count, sky direction, angular size, phase, or visibility may be independently hard-coded by the renderer.

Background stars occupy the inertial frame and therefore move through the local sky as the planet rotates. The core release may use a generated bounded star field rather than reproduce a real catalogue, but its directions, colours, and brightness are pinned world data and the observer transform is the same one used for system bodies. Atmosphere primitives select a bounded approved visual model for scattering, extinction, twilight, and any refraction used by the visibility test; an airless atmosphere has no scattering or refractive lift. Clouds, weather, rings, and moons affect the sky when their generated primitive graphs declare them; meaningful absence remains explicit under decision 0031.

**Scope and exactness.** A versioned analytic two-body propagation model is sufficient for the core release; full n-body perturbations, long-term scientific ephemeris accuracy, and interactive orbital flight are not required. Authoritative inputs, simulation time, and reference outputs obey the deterministic numeric and storage rules of decision 0031. If cross-platform trigonometric or propagation calculations cannot reproduce the reference outputs exactly, use deterministic fixed-point/table machinery or materialize the bounded celestial samples needed for the affected category before M1. Godot receives derived presentation data and may interpolate visually, but it does not own astronomical state.

## Consequences

Easier: the Sun, shadows, day/night cycle, stars, and neighbouring worlds tell one coherent story from every landing site; revisiting a saved world reproduces the same sky at the same simulation time; generated orbital properties become visible to the player rather than remaining map metadata. Harder: M0b must generate and validate the required orbit, spin, epoch, atmosphere, and region-to-planet mapping data and prove deterministic reference cases; M1 must integrate that solution with lighting and the sky renderer. Atmosphere presentation needs separate airless and atmospheric paths, and artistic exaggeration cannot silently change positions or visibility.

Verification uses fixed configurations for noon and midnight, sunrise and sunset, equatorial and polar observers, rotation across one sidereal day, bodies above and below the horizon, conjunction and opposition, phase and angular-size changes, and eclipses or occultations. It also verifies that the light direction equals the rendered stellar direction, that the same saved time produces the same celestial output across Linux and Windows, and that no decorative planet remains visible when the authoritative solution hides it.

## Applied to

- [Requirements: owner design constraints](../requirements.md#design-constraints-stated-by-the-owner)
- [Concept: destination planetary system](../concept.md#3-the-destination-planetary-system)
- [Technical design: astronomical state and surface sky](../technical-design.md#astronomical-state-and-surface-sky)
- [Development plan: milestones and verification](../development-plan.md#milestones)
- [Glossary: worlds and destinations](../glossary.md#worlds-and-destinations)
- [Review finding 30](../review.md#30-surface-sky-is-decorative-rather-than-astronomically-derived)
- [Progress: verification and performance targets](../progress.md#verification-and-performance-targets)
