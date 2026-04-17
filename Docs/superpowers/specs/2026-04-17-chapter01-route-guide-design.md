# Chapter01 Route Guide Beautification Design

Date: 2026-04-17
Project: ZhuoZheng / Garden_Main
Scope: Beautify the Chapter01 route guidance so it clearly guides the player to the next playable location while preserving scene tone and avoiding any gameplay logic changes.

## 1. Goal

Upgrade the existing Chapter01 route guidance into a polished in-world visual guide that:

- fits the 3D classical garden scene
- reads clearly from a third-person camera
- does not replace existing objective, trigger, or chapter progression logic
- fades out naturally when the player reaches the intended target

The guidance should feel like part of the garden atmosphere rather than a generic mission arrow.

## 2. Design Principles

Two constraints are mandatory for this work:

1. Beauty first:
   the guide must feel elegant, restrained, and integrated with the garden. It should resemble water glow, mist, or faint gold dust instead of neon game navigation.

2. No gameplay regression:
   the guide is presentation-only. It must not change how objectives advance, how gates solve, how interaction prompts appear, how saves work, or how chapter triggers fire.

Supporting principles:

- readable without dominating the frame
- visible on stone path, bridge, and garden ground
- easy to tune by artists or level designers later
- deactivates cleanly when not needed

## 3. Recommended Approach

Use a hybrid presentation:

- Primary guide:
  a ground-hugging flowing ribbon that follows authored route points
- Secondary guide:
  a subtle destination marker hovering above the current target

Do not use a full-time HUD arrow as the main guidance layer.

Reasoning:

- the project is a 3D garden scene, so ground guidance communicates direction more naturally than screen-space arrows
- the project already contains `Chapter01AuthoredRouteGuide`, `GuidePoint` markers, and authored path support, so visual upgrade is lower risk than replacing the system
- a soft destination marker solves the last 10 percent of clarity without making the scene look gamey

## 4. Visual Direction

### 4.1 Ground ribbon

The route should look like a narrow animated band resting slightly above the ground:

- base hue: desaturated teal-green
- highlight hue: warm pale gold
- brightness: moderate, never emissive enough to wash out the scene
- width: readable from gameplay camera, but narrower than a road marking
- edge treatment: soft falloff, not hard-edged painted stripes
- motion: slow directional shimmer or pulse toward the objective

The intended feeling is "guided flow through the garden", not "quest line on the floor".

### 4.2 Destination marker

At the current target, add one subtle marker:

- a soft glow ring
- a suspended mote cluster
- or a small elegant crest-like shimmer

It should sit above the target and help players identify the endpoint once the route approaches it.

It must remain visually lighter than a standard action-game waypoint arrow.

### 4.3 Visibility rules

The guide should:

- be visible enough in daylight scenes
- avoid overpowering dialogue or chapter UI
- avoid clipping harshly through uneven ground
- avoid becoming a bright streak across the whole environment

## 5. Behavior

### 5.1 When the guide appears

The guide appears only when guidance is needed, such as:

- chapter start before the player reaches the left gate
- after route rebuilds when the target is still unresolved by the player

### 5.2 When the guide hides

The guide fades out when:

- the player reaches the target radius
- the relevant chapter step is already solved
- the system decides the route should no longer be shown

### 5.3 What the guide must not do

The guide must not:

- move the player
- alter target selection logic
- modify objective text rules
- unlock or complete chapter state
- interfere with interaction prompts, dialogue, gate calibration, or water direction selection

## 6. Technical Design

### 6.1 Existing system to reuse

Keep `Assets/Scripts/Chapters/Chapter01/Chapter01AuthoredRouteGuide.cs` as the routing and lifecycle entry point.

The following parts are already useful and should remain the foundation:

- authored route root lookup
- route point collection
- smoothed display path generation
- segment reveal / fade timing
- solved-target hiding logic

### 6.2 What changes

Refine only the presentation layer:

- segment mesh look
- segment material setup
- reveal timing polish
- destination marker creation and fade logic
- tuning values for width, alpha, glow, and offsets

### 6.3 What stays unchanged

Do not rewrite:

- `Chapter01Director` objective flow
- `GardenGameManager` chapter state progression
- interactable trigger rules
- save data fields or serialization
- authored path point ownership in scene

### 6.4 Rendering strategy

Preferred implementation order:

1. improve the existing ribbon strips and shader/material response
2. add a lightweight endpoint marker object
3. tune visibility, fade, and obstruction trimming

If the current custom shader is insufficient, update or replace the route material implementation, but keep the route generation code path stable.

External assets or packages may be referenced for inspiration, but the first implementation should prefer the existing local system unless a package clearly reduces risk and fits the project style.

## 7. Options Considered

### Option A: Ground flowing ribbon

Pros:

- best fit for 3D garden traversal
- strongest atmosphere
- reuses existing authored route system

Cons:

- needs careful tuning to avoid looking too bright or too wide

Recommendation:

This is the chosen primary approach.

### Option B: Ground decals like ripples, leaves, or footprints

Pros:

- elegant and environmental
- can look very natural in URP

Cons:

- weaker long-distance readability
- more content-authoring overhead for decal assets

Recommendation:

Use only as a future enhancement or secondary treatment, not the first pass.

### Option C: Floating arrow and edge indicator

Pros:

- highest immediate clarity
- easy for players to understand

Cons:

- weakest thematic fit
- feels too system-heavy for this project

Recommendation:

Keep only as an optional fallback, not the main route guide.

## 8. Risks and Mitigations

### Risk 1: The guide becomes visually noisy

Mitigation:

- clamp brightness and alpha
- use narrow width
- use soft motion rather than aggressive pulsing

### Risk 2: The guide intersects uneven terrain badly

Mitigation:

- keep using ground resolution and obstacle trimming
- slightly increase ground offset only as much as needed to prevent z-fighting

### Risk 3: The guide accidentally changes chapter behavior

Mitigation:

- keep all changes inside route guide presentation code
- do not edit objective progression rules except for read-only integration
- verify the guide still hides using existing solved-state checks

## 9. Verification Plan

Before calling the work complete, verify:

- the route appears at chapter start when expected
- the route still points to the intended gate target
- the route fades after reaching the target
- UI prompts, gate interaction, water direction choice, and objective progression still behave exactly as before
- no new save/load side effects occur
- the guide remains readable but not overpowering in gameplay camera view

## 10. Deliverable

The final deliverable for this design is:

- a beautified Chapter01 in-world route guide based on the existing authored route system
- a subtle destination marker
- no chapter logic regressions
- tunable visual parameters suitable for future polish
