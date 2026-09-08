# 0052. The two levers compose a destination by bounded retry, and a review view shows it beside its description

Date: 2026-09-09. Status: Accepted. Source: the owner's instruction of 2026-09-09 to make a test that takes the two lever values, generates the system accordingly, and launches a three-dimensional view beside the description text for review. Answers, for the solar-system level only, part of [review finding 48](../review.md#48-every-randomization-level-is-to-be-judged-graphically-and-nothing-says-which-view-in-what-order-or-against-what-vector).

## Context

[Decision 0043](0043-destination-controls-distance-tier-and-seed-lever.md) gives the player two destination controls, a distance tier and a seed. Nothing composed a destination from them: `compose` takes a graph seed and `iterate` takes a range of them, and both leave the tier as something the description is judged against afterwards. Three iterations have shown why that matters — iteration 3 met the starter tier's rule in 5 systems of 24 — so a lever that names a tier but hands back a system breaking it is not a lever.

Separately, the three iterations have been judged from text alone. [Finding 48](../review.md#48-every-randomization-level-is-to-be-judged-graphically-and-nothing-says-which-view-in-what-order-or-against-what-vector) records the owner's intent that every randomization level be judged graphically, and it is open because no document says by which view. [Decision 0045](0045-close-views-only-in-the-surface-sky-and-a-derived-system-description.md) says the core release adds no orbital view, which a view of a whole system would appear to contradict.

## Decision

**A destination is composed from the two levers by bounded retry.** Given a tier and a seed, the composer draws a system on a seed derived from the two by the frozen stream derivation, checks it against that tier's rules, and draws again on the next derived seed until one satisfies them or sixty-four attempts are spent, when it rejects the destination with an explicit content-capacity error naming what the last attempt broke. This is the bounded retry and explicit rejection the development plan's [Destination validity](../development-plan.md#verification-and-performance-targets) check asks for, and the attempt sequence follows from the lever values alone, so one pair names one destination on any machine without recording which attempt won. Constraining generation by tier, rather than searching for a system that happens to satisfy it, remains open: it needs a grammar that can be gated on a tier, which is the question decision 0044 left and no decision has taken.

**The review view is a harness, not a feature.** A three-dimensional view of a composed system, launched from the game project with `--system`, shows the bodies beside the description text they are judged by. It is a tool for reading a generator's output, like the `iterate` command, and it is not reachable from anything a player does; decision 0045's rule that other bodies are seen only in the surface sky is about the core release and stands unchanged. Should the owner want a system view *in* the game, that is a change to decision 0045 and needs its own record.

**What the view must show.** Every element carries the description of itself: each body wears its own line of the same text the panel lists, so the two cannot say different things, and the star wears the star's line. A caption keeps one size on screen however near the camera is, stands on a leader line rising from its body, takes that body's own colour, and is drawn apart from its neighbours' both above and across the screen, so which text belongs to which object is never in doubt. A body far from the camera shows the short form of its line and a near one the whole of it, which is the only way eight of them are legible at once.

**What the view may not pretend.** Nothing is to scale and the view says so on screen: orbit radii and body radii are each placed logarithmically between the system's own smallest and largest, so order, ratio, and kind are faithful and size is not, and a moon is offset beside its planet rather than on its own orbit. Bodies stand at their epoch mean anomaly on a circle of their semi-major axis; the two-body propagation of [decision 0037](0037-derivation-rules-integer-periods-and-orbit-hierarchy.md) is not implemented and this view does not stand in for it.

**Moving through the system.** The view can be taken to a readable distance from the star and from every body: a body is approached to a few times its own radius, so a moon is met closely and the star from far enough back to see it whole. Bodies are cycled with the tab key or chosen by number, the whole system is framed with zero, the view turns by dragging and closes by distance with the wheel, and it moves freely with the usual keys at a speed that follows the distance being viewed from, so one set of keys crosses a system and creeps up on a moon.

**It is reviewable without a window.** The same two levers drive the `destination` command, which prints what the view shows, and the view itself saves a picture and exits when given `--screenshot`, so a system can be reviewed from a terminal, in a session with no display, or as an image in a record.

## Consequences

Easier: the two levers do what a player's controls will do, so what is reviewed is what will be played; a destination is reproducible from the two values a record can hold; and a generated system can be judged by eye, which is what the owner asked of every randomization level. Harder: the tier is satisfied by searching rather than by construction, so a tier whose rules the content cannot meet costs sixty-four compositions before it says so, and the frequency of that is a number the Destination validity check must record; the view's conventions are chosen for reading and would mislead anyone who took them for a simulation, so they are stated on screen; and the game project now carries a review path beside the smoke path, which the exported-build smoke test must keep passing.

Verified by the tests that one pair of lever values names one destination and that what comes back satisfies the tier that asked for it, by the command-line test that runs the levers in two processes, and by the exported-build smoke check, which is unchanged.

## Applied to

- `src/SpaceExplorer.Core/Description/DestinationComposer.cs`, `src/SpaceExplorer.Game/`, `src/SpaceExplorer.Cli/`, and the matching tests
- [Progress: completed steps, verification](../progress.md#verification-and-performance-targets)
- [Review finding 48](../review.md#48-every-randomization-level-is-to-be-judged-graphically-and-nothing-says-which-view-in-what-order-or-against-what-vector)
- [src/README.md](../../src/README.md), [docs/tools.md](../tools.md#godot)
