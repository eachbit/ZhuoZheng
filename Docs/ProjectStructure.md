# ZhuoZhengYuan Project Structure

## Goal

This project uses a low-risk shared structure so multiple teammates can develop different chapters without breaking the common first-person and third-person traversal foundation.

## Shared Public Layers

These folders are common systems. They should stay reusable across every chapter.

- `Assets/Scripts/Core`
  - global game flow
  - save data
  - shared chapter state entry points
- `Assets/Scripts/Player`
  - first-person traversal
  - third-person traversal
  - camera mode switching
  - player interaction entry
- `Assets/Scripts/Interaction`
  - common interaction helpers
- `Assets/Scripts/UI`
  - shared runtime UI and chapter-agnostic UI systems
- `Assets/Scripts/Environment`
  - shared environment helpers that are not chapter-private
- `Assets/Scripts/Intro`
  - shared intro flow systems if reused later

## Chapter-Private Layers

Each chapter keeps its own gameplay and editor tools inside a chapter folder.

- `Assets/Scripts/Chapters/Chapter01`
  - Chapter01 gameplay logic only
- `Assets/Scripts/Editor/Chapters/Chapter01`
  - Chapter01 editor helpers only
- `Assets/Figure/Chapters/Chapter01`
  - Chapter01 placeholder/test models
- `Assets/Materials/Chapters/Chapter01`
  - Chapter01 materials
- `Assets/Shaders/Chapters/Chapter01`
  - Chapter01 shaders

Future chapters should follow the same pattern:

- `Assets/Scripts/Chapters/Chapter02`
- `Assets/Scripts/Editor/Chapters/Chapter02`
- `Assets/Figure/Chapters/Chapter02`
- `Assets/Materials/Chapters/Chapter02`
- `Assets/Shaders/Chapters/Chapter02`

## Scene Hierarchy Standard

`Garden_Main` uses these top-level groups:

- `_00_Core`
- `_10_World`
- `_20_Story`
- `_30_Chapters`

Inside `_30_Chapters`, Chapter01 is split into:

- `_31_Chapter01_Gameplay`
- `_32_Chapter01_Visuals`

Recommended ownership:

- `_00_Core`
  - common systems only
- `_10_World`
  - public garden base, blockers, walkable support
- `_20_Story`
  - intro actors, dialogue anchors, story markers
- `_31_Chapter01_Gameplay`
  - triggers, interactables, puzzle objects, pickups
- `_32_Chapter01_Visuals`
  - effects, reveal visuals, chapter-only presentation helpers

## Team Rules

- Do not duplicate the player controller into chapter folders.
- Do not move shared traversal code out of `Assets/Scripts/Player`.
- Prefer adding new chapter logic under `Assets/Scripts/Chapters/<ChapterName>`.
- Prefer adding chapter-only editor tools under `Assets/Scripts/Editor/Chapters/<ChapterName>`.
- Keep temporary captures and validation output out of Git. `Artifacts/` is ignored.
- Avoid editing shared core files from multiple branches at the same time.

## GitHub Workflow Recommendation

- One feature branch per teammate.
- Keep `main` stable.
- Merge chapter work through review instead of pushing directly to `main`.
- When possible, one teammate owns each shared system file at a time.
