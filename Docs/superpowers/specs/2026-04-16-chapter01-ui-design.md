# Chapter01 Formal UI Design

Date: 2026-04-16
Project: ZhuoZheng / Garden_Main
Scope: Replace the current temporary Chapter01 runtime UI with a formal, art-directed UI that fits the selected visual direction.

## Goal

Build the full Chapter01 UI in the existing `Garden_Main` scene without changing Chapter01 gameplay rules. The result should:

- replace the current temporary `OnGUI` presentation used by `PrototypeRuntimeUI`
- preserve the existing Chapter01 flow, save behavior, and interaction rules
- follow the approved visual direction: `园亭匾额 + 深墨金边`
- be easy for a teammate to reskin later without rewriting gameplay logic

This work is UI replacement and UI structure work, not a gameplay redesign. Chapter01 gate calibration, flow direction choice, page collection, dialogue, and toast logic stay functionally the same.

## Current State

The current project uses `PrototypeRuntimeUI` to draw temporary Chapter01 and Chapter02 UI through `OnGUI`. That temporary layer already exposes the gameplay-facing methods needed by `GardenGameManager` and `Chapter01Director`, including:

- page counter
- objective text
- toast/result banner
- interaction prompt
- dialogue box
- direction choice panel
- Chapter02 quiz panel

Chapter01 gameplay flow is owned by `Chapter01Director`. That script already decides:

- when objectives change
- when the player can calibrate gates
- when the flow selection UI opens
- what choice labels the player sees
- when the result banner appears
- when the page becomes available

Because those rules are already stable, the formal UI should be introduced as a presentation layer that keeps the same external hooks where practical.

## Approved Visual Direction

Primary direction: `园亭匾额 + 深墨金边`

Visual language:

- deep ink-green panels instead of pure black
- thin gold borders and restrained gold accents
- plaque-like title bars inspired by pavilion signboards
- cream-white text for readability
- elegant title typography with clean body typography
- no heavy western fantasy bevels
- no bright neon or oversized decorative flourishes

Font system:

- title / panel title: `SourceHanSerifSC-SemiBold.otf`
- body / option / prompt: `SourceHanSansSC-Medium.otf`
- count / highlight / key numbers: `SourceHanSansSC-Bold.otf`

## UI Modules To Build

All of the following belong to Chapter01 delivery:

1. Page counter
2. Objective bar
3. Toast / result banner
4. Interaction prompt
5. Dialogue box
6. Water direction choice panel
7. Gate calibration panel

Chapter02 quiz UI is explicitly out of scope for this pass.

## Module Design

### 1. Page Counter

Purpose:
Show current page progress in the upper-left corner.

Visual treatment:

- compact dark panel
- gold border
- bold numeric emphasis
- low visual noise

Data:

- current collected pages
- total pages

Source:

- `PrototypeRuntimeUI.SetPageCount`

### 2. Objective Bar

Purpose:
Show the current Chapter01 goal in the upper-left area beneath the page counter.

Visual treatment:

- narrow plaque-like label or compact task board
- serif title styling for the heading if used
- sans body text
- stable width so the layout does not jump excessively

Data:

- current objective string from `GardenGameManager.SetObjective`

### 3. Toast / Result Banner

Purpose:
Show short-lived feedback such as correct or incorrect water-flow outcomes.

Visual treatment:

- top-center banner
- dark ink panel with thin gold border
- accent color support for state changes
- stronger hierarchy than the temporary implementation

Data:

- toast text
- direction result title
- direction result body
- optional accent color

Source:

- `ShowToast`
- `ShowDirectionResult`

### 4. Interaction Prompt

Purpose:
Show a small prompt when the player can interact and no modal UI is open.

Visual treatment:

- bottom-center compact panel
- smaller than the dialogue box
- subtle, readable, not dominant

Data:

- current interaction prompt string

Rule:

- hidden while dialogue, direction choice, gate puzzle panel, or Chapter02 quiz is open

### 5. Dialogue Box

Purpose:
Show guided story dialogue and narration.

Visual treatment:

- full-width lower panel
- small name plaque above or integrated into the frame
- serif speaker label
- sans body copy
- visible continue hint and continue button

Data:

- speaker name
- dialogue body
- continue action

Source:

- `ShowDialogue`
- `AdvanceDialogue`

### 6. Water Direction Choice Panel

Purpose:
Serve as the main Chapter01 modal decision panel when the player chooses the water route.

Visual treatment:

- center modal with strongest “formal game UI” finish
- plaque title area
- body text explaining the decision
- clean numbered choices
- clear keyboard hint support

Data:

- title
- option labels
- current callbacks

Behavior:

- preserves current numeric shortcut support
- preserves close/cancel behavior

### 7. Gate Calibration Panel

Purpose:
Provide formal UI feedback during gate rotation puzzle play.

Visual treatment:

- right-side dedicated puzzle panel
- strong border and stable block layout
- explicit angle readout
- clear instruction row for rotate / confirm / cancel

Data needed from gameplay:

- gate puzzle active state
- current angle / calibration display value
- valid range or “ready to confirm” state
- active gate side if available

Important note:

The current temporary `OnGUI` layer does not yet expose a dedicated gate puzzle panel. This means the formal Chapter01 UI implementation must add a presentation path for gate calibration instead of only reskinning existing panels.

## Technical Approach

Recommended approach:

- introduce a dedicated Canvas-based Chapter01 runtime UI
- keep `PrototypeRuntimeUI` only as a compatibility fallback or retire its Chapter01 rendering paths after the new UI is connected
- preserve the current gameplay method surface as much as possible

Architecture:

1. Create a Chapter01 UI root under Canvas in the existing `Garden_Main` scene
2. Split each UI block into clearly named sub-objects/panels
3. Route existing runtime calls into the new presentation layer
4. Add an explicit gate calibration presenter path for the right-side puzzle panel
5. Keep fonts, colors, and sprite slots centralized for teammate-friendly replacement

## Teammate-Friendly Structure

The UI must be organized so later art replacement is easy.

Rules:

- expose background images / frame sprites as serialized fields
- expose color tokens as serialized fields where practical
- avoid hardcoding visual values in multiple places
- separate data update methods from styling setup
- name hierarchy by function, not by art pass

Recommended hierarchy:

- `Chapter01UIRoot`
- `TopLeft/PageCounterPanel`
- `TopLeft/ObjectivePanel`
- `TopCenter/ToastPanel`
- `BottomCenter/InteractionPromptPanel`
- `Bottom/DialoguePanel`
- `Center/FlowChoicePanel`
- `Right/GateCalibrationPanel`

This structure allows a teammate to replace:

- frame sprite
- background texture
- decorative ornament
- font asset
- spacing and padding

without needing to rewrite the gameplay hookups.

## Interaction and Data Flow

Gameplay ownership stays where it is now:

- `Chapter01Director` owns state transitions and correctness
- `GardenGameManager` owns high-level UI calls
- the new Chapter01 formal UI owns presentation only

Flow:

1. Gameplay changes state
2. `GardenGameManager` or `Chapter01Director` calls the UI-facing methods
3. Formal UI updates the correct panel
4. Modal UI panels notify gameplay through existing callbacks

For gate calibration, the new UI needs an additional update path because that information is not currently rendered as a formal panel.

## Error Handling and Safety

If a panel is missing:

- gameplay should continue
- a missing panel should fail quietly where possible
- button callbacks should not throw if the UI is closed while input is pressed

If a font asset is missing:

- fallback should remain readable
- no panel should become invisible due to font load failure

If teammate art is not ready:

- the layout should still work with placeholder slices/colors

## Testing Expectations

Before calling the UI pass complete, verify:

1. Intro end -> objective appears correctly
2. Gate interaction -> calibration panel appears and closes correctly
3. Two gates solved -> flow direction panel opens correctly
4. Wrong choice -> toast/result appears and objective updates
5. Correct choice -> page becomes available and feedback is readable
6. Dialogue panels remain readable in bright and dark scene areas
7. Input shortcuts still work for direction selection
8. No Chapter02 behavior is regressed by the Chapter01 UI changes

## Out of Scope

These are not part of this Chapter01 UI pass:

- Chapter02 final UI
- new gameplay rules
- scene rebuild
- cinematic polish
- voice presentation
- replacing teammate art pipeline

## Recommended Implementation Order

1. Set up Canvas root and shared style tokens
2. Build top-left persistent HUD blocks
3. Build dialogue box
4. Build toast/result banner
5. Build interaction prompt
6. Build flow choice modal
7. Build gate calibration panel
8. Wire all Chapter01 calls and verify the full loop

## Recommendation

Proceed with a Canvas-based formal Chapter01 UI and keep the gameplay API stable. This gives the best balance of:

- visual quality
- implementation control
- teammate reskin flexibility
- low gameplay regression risk
