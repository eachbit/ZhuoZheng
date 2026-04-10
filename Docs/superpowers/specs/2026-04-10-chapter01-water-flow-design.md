# Chapter01 Water Flow Design

## Goal

Improve the Chapter01 garden water so it has believable motion across the full connected water mesh while still making the puzzle's center route feel visually special after the player solves the flow direction step.

This design must work with the current scene setup where the visible water is authored as one irregular mesh rather than many isolated flat planes.

## Problem Summary

The current project already contains:

- a lightweight center-flow shader and material for a generated overlay surface
- scene logic that can show or hide solved-flow visuals through `Chapter01EnvironmentController`
- direction selection feedback effects for correct and incorrect flow choices

What it does not yet have is a stable way to make the entire authored water mesh feel alive while also highlighting the puzzle-critical center channel without trying to replace or split the original mesh.

Because the existing water mesh is one continuous irregular model, a single generated quad is not sufficient as the whole solution. A full-scene overlay would either cover too much of the garden or float in visually incorrect ways on curved sections.

## Design Summary

Use a two-layer water solution:

1. A persistent global-flow layer on the original water mesh.
2. A stronger center-channel overlay layer for the puzzle route.

The global-flow layer gives the whole garden water a subtle sense of motion at all times.

The center-channel overlay layer creates a more directional, brighter, faster-moving current only along the puzzle's intended main route. This layer remains weak or hidden before the puzzle is solved and becomes clearly visible after success.

## Scene Structure

### Existing Water Mesh

Keep the current authored water mesh object as the base visible water.

- Target object: the current water mesh such as `Mesh3825`
- Keep the existing mesh intact
- Do not split the mesh into smaller pieces
- Do not remove or replace the source FBX geometry

### Global Flow Material

Assign the water mesh a Chapter01-specific material instance rather than editing the shared imported material directly.

Proposed asset:

- `Assets/Materials/Chapters/Chapter01/Water/Chapter01GlobalFlow.mat`

This material will be visually close to the current imported water but will add controlled UV scrolling and subtle highlight variation so the whole water system looks alive.

### Center Flow Root

Continue using the scene root already referenced by Chapter01 logic:

- `FlowCenterVisuals`

This root remains the place where solved-flow visuals live so the existing environment controller can continue enabling or disabling them by state.

### Center Flow Segments

Replace the idea of one large center quad with several smaller authored overlays:

- `FlowCenterVisuals/CenterFlowSegment_01`
- `FlowCenterVisuals/CenterFlowSegment_02`
- `FlowCenterVisuals/CenterFlowSegment_03`

These are narrow quads or simple planes placed by hand over the main route from the selector side toward the page pickup side.

Optional future children under the same root may include foam or splash accents, but those are not required for the first pass.

## Visual Behavior

### Global Flow Layer

This layer is always active.

Visual intent:

- subtle motion
- slow speed
- low contrast directional feeling
- close to the current water color and readability

The goal is not for the player to read an exact flow direction from the entire pond network. The goal is to prevent the water from feeling static.

### Center Flow Layer

This layer communicates the main route and puzzle outcome.

Visual intent:

- stronger directional movement
- slightly brighter and more readable highlights
- longer and cleaner flow streaking than the global layer
- soft transparent falloff at the sides so the overlays blend into the base mesh

This should read like a focused current moving through the main channel instead of a flat decal sitting on top of the water.

## State-Driven Presentation

### Initial State

- Global flow is active.
- Center flow segments are hidden or nearly imperceptible.

### Flow Selection Phase

- Global flow remains active.
- Existing selection feedback continues to communicate immediate choice results.
- Center flow segments stay subdued so they do not reveal the answer too early.

### Wrong Direction Chosen

- Global flow remains active.
- Existing pulse, trail, flare, and impact feedback stays responsible for the error response.
- Center flow segments do not enter a sustained strong state.

### Correct Direction Solved

- Global flow remains active.
- Center flow segments become clearly visible.
- Existing Chapter01 solved-flow state continues to drive page reveal and progression.

This keeps the puzzle readable: the whole garden feels alive, but the player still receives a distinct reward when the main route is correctly established.

## Center Segment Placement

Begin with three authored segments.

### Segment 01

Place near the source side of the route around the area the player associates with initial flow selection.

Purpose:

- establish where the guided current begins

### Segment 02

Place along the most visually dominant middle bend of the central water route.

Purpose:

- carry the strongest visual sense of forward current

### Segment 03

Place near the destination side leading toward the page pickup area.

Purpose:

- visually complete the route and reinforce successful delivery

### Placement Rules

- Each segment should cover only the center portion of the channel, roughly 60% to 75% of the local width.
- Each segment should sit slightly above the water surface, starting around 0.02 to 0.05 units above the base mesh.
- Adjacent segments may overlap slightly to avoid visible seams.
- Each segment should be rotated individually to match the local direction of the river bend.
- Segment edges should fade softly instead of ending in a hard border.

## Technical Strategy

### Global Water Implementation

Implement a Chapter01-specific global-flow shader or shader variant for the full water mesh material.

Expected capabilities:

- base texture sampling compatible with the current water look
- low-strength UV scrolling
- optional secondary sampling offset for richer motion
- transparency compatible with the current scene

This should be stable on the existing irregular water mesh and should not depend on a perfectly flat surface.

### Center Overlay Implementation

Continue using a dedicated flow shader for center overlays, based on the existing lightweight flow approach already in the project.

The center overlay implementation should support:

- stronger directional UV motion than the global layer
- gentle edge fade
- material tuning for brightness, tint, alpha, and flow speed

The overlays should remain cheap and reliable. This design does not require simulated fluid motion, vertex displacement, or complex screen-space refraction.

## Integration With Existing Code

Reuse the current chapter architecture instead of introducing a separate flow state system.

Primary integration points:

- `Chapter01EnvironmentController`
- `Chapter01Director`
- the existing `FlowCenterVisuals` root and `flowingObjects` binding behavior

Implementation intent:

- the global flow layer is present regardless of puzzle state because it lives on the base water mesh material
- the center flow layer is controlled through the existing environment flow state hooks
- selection feedback remains handled by the current runtime feedback coroutines

## Non-Goals

The first pass does not include:

- splitting the source water mesh into many authored sections
- physically accurate fluid simulation
- advanced vertex wave deformation
- screen-space refraction or reflection systems
- broad scene-wide foam generation

These may be explored later, but they are outside the current scope because they increase risk without being necessary to achieve the target gameplay read.

## Risks And Mitigations

### Risk: Shared Material Side Effects

If the imported water material is edited directly, other water objects may change unexpectedly.

Mitigation:

- create and assign a Chapter01-specific material instance for the scene water mesh

### Risk: Overlay Floating Or Z-Fighting

If center overlays sit too high they will look detached; if too low they may flicker.

Mitigation:

- keep segment height offsets very small and tune per segment
- use multiple smaller segments instead of one oversized plane

### Risk: Puzzle Readability Becomes Too Obvious

If the center route is too strong before success, it may spoil the puzzle.

Mitigation:

- keep pre-solve center flow weak or hidden
- continue relying on explicit interaction feedback for wrong choices

## Acceptance Criteria

The design is successful when all of the following are true:

1. The garden's full connected water mesh shows subtle continuous movement during normal play.
2. The puzzle's center route is more visually active than the rest of the water after success.
3. The center route enhancement does not obviously float, clip through banks, or shimmer from depth fighting.
4. Existing Chapter01 puzzle progression and page reveal logic still behaves correctly.
5. The base water mesh remains intact and does not require manual geometric splitting.
6. Further polish such as foam or accents can be added later without replacing this structure.

## Implementation Notes For Next Step

The next planning phase should cover:

- exact assets to create for the global and center materials
- whether to extend the current center-flow tool or author center segments directly in scene
- how to bind the center segment root safely to `Chapter01EnvironmentController`
- how to assign a Chapter01-only material to the full water mesh without affecting shared imported assets
- how to verify the visual result in both edit mode and runtime puzzle states
