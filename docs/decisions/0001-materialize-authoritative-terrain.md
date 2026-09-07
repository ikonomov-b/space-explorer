# 0001. Materialize authoritative terrain at world acceptance

Date: 2026-09-07. Status: Accepted.

## Context

The technical design required old worlds to stay frozen, unvisited regions never to regenerate differently, and terrain to be reconstructed from pinned algorithms. Those three requirements can only be met together by either retaining every historical terrain algorithm in the shipped game or by storing authoritative geometry when a world is accepted. The persistence section named both options and chose neither. Cross-platform floating-point differences in transcendental functions made algorithmic reconstruction an additional determinism risk. Resolves [review finding 1](../review.md#1-terrain-authority-is-left-as-an-or).

## Decision

Authoritative region data is materialized when a world is accepted and stored for every region of that world, visited or not. Per region this is a quantized heightfield, a biome/material index map, site anchors, the traversal graph, hazard volumes, and artifact placements, in documented integer units. Collision surfaces, routes, reachability checks, and placement validation read only this stored data.

Cosmetic detail is regenerated at runtime and may use any generator version: fine displacement, scatter of small props, textures, and normal maps. Displacement amplitude is bounded by a fixed epsilon and is never applied to collision meshes. Mesh construction from a recipe is presentation; changes to it apply uniformly to old and new worlds and must preserve a recipe's distinguishing features.

The host sends stored region data to guests; guests never reconstruct authoritative terrain. Old generator versions are not retained, because every authoritative result is materialized.

Cross-operating-system matching is a hard requirement for descriptors, recipes, and placements from M0a. Matching terrain generation output across Linux and Windows is a measured target at M0b; if measurements differ, a fixed-point noise implementation is adopted before M1.

## Consequences

Easier: update compatibility, since no authoritative result depends on code that may change; multiplayer join, since one path ships stored regions; determinism testing, since terrain generation becomes a measured pipeline rather than a runtime guarantee. Harder: save size grows by an estimated 0.1 to 1 MB per region depending on extent and cell size, and world acceptance must finish terrain generation for every region before the destination is ready. Verified by the resource-use check and the M0b region measurement in the [development plan](../development-plan.md#verification-and-performance-targets). Cell size, height quantization, and the region extent cap are fixed by review finding 9.

## Applied to

- [Technical design: destination identity and determinism](../technical-design.md#destination-identity-and-determinism)
- [Technical design: persistence and compatibility](../technical-design.md#persistence-and-compatibility)
- [Technical design: multiplayer and trust boundary](../technical-design.md#multiplayer-and-trust-boundary)
- [Development plan: milestones and verification](../development-plan.md#milestones)
