# 0010. Units, coordinates, region bounds, and simulation tick

Date: 2026-09-07. Status: Accepted.

## Context

The documents required documented units and bounded landing regions but fixed no length unit, coordinate representation, maximum region size, heightfield resolution, or simulation tick. Godot's default vectors are 32-bit floats, so region size bounds rendering precision, and the same constants drive terrain storage, network position encoding, and every authoritative distance check. Resolves [review finding 9](../review.md#9-units-coordinates-and-region-bounds).

## Decision

| Quantity | Value |
| --- | --- |
| Length unit | Metre. Authoritative positions are int32 fixed-point at 1/256 m per axis. |
| Coordinate frame | Region-local, origin at the region centre; every stored or transmitted position is region ID plus local fixed-point coordinates. |
| Region extent cap | 4,096 m per axis. |
| Heightfield | 2 m cells; heights as int16 at 1/16 m, giving about 2 km of vertical range within a region. About 2 MB raw for a maximal region before compression. |
| Simulation tick | Fixed host step at 20 Hz (50 ms). Durations are counted in ticks; suit reserves and hazard exposure are integer units per tick. |
| Planetary values | Integers in documented units defined with the numeric tables, such as metres and millimetres per second squared. |

Authoritative checks such as pickup range, reachability, and hazard exposure run on fixed-point values in the core. The Godot adapter converts to floats for rendering and physics, and the region-local frame keeps those floats small. Double-precision engine builds are ruled out unless measurements force them. These constants are versioned with the numeric tables; changing one is a generator or valuation version change, never a silent edit.

## Consequences

Easier: terrain storage, network encoding, and authoritative geometry share one representation, and cross-platform equality of authoritative results is an integer comparison. Harder: the Godot adapter carries conversions at every boundary, and region design must fit the cap. Verified by the M0b region measurement and the resource-use check in the [development plan](../development-plan.md#verification-and-performance-targets).

## Applied to

- [Technical design: units, coordinates, and bounds](../technical-design.md#units-coordinates-and-bounds)
- [Development plan: core-release contents and decisions](../development-plan.md#core-release-contents)
- [Glossary](../glossary.md): region, tick
