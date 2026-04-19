# Chapter06 Quiz Finale Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build Chapter 6 as a Chapter 2-style trigger quiz about Xuexiang Yunwei Pavilion, then end the project through a short pavilion revisit trigger.

**Architecture:** Add an independent Chapter06Director, Chapter06State, and SaveData fields while reusing Chapter02Question, Chapter02QuizSession, and the existing quiz UI presenter. GardenGameManager initializes Chapter06Director and keeps the existing quiz UI/player lock pathway.

**Tech Stack:** Unity C#, NUnit EditMode tests, existing Chapter01CanvasUI quiz presenter.

---

### Task 1: Chapter06 Behavior Tests

**Files:**
- Create: `Assets/Tests/EditMode/UI/Chapter06DirectorObjectiveTests.cs`
- Create: `Assets/Tests/EditMode/UI/Chapter06DirectorObjectiveTests.cs.meta`

- [ ] Write tests that require six answers, award final completion once, and hide the finale prompt before Chapter 6 is complete.
- [ ] Run the tests and confirm they fail because Chapter06Director is not implemented yet.

### Task 2: Chapter06 Runtime Code

**Files:**
- Create: `Assets/Scripts/Chapters/Chapter06/Chapter06State.cs`
- Create: `Assets/Scripts/Chapters/Chapter06/Chapter06Director.cs`
- Create matching `.meta` files.
- Modify: `Assets/Scripts/Core/SaveData.cs`
- Modify: `Assets/Scripts/Core/GardenGameManager.cs`

- [ ] Add Chapter06State values: NotStarted, InProgress, AwaitingFinalView, Completed.
- [ ] Add Chapter06 save fields for state, answered count, question order, and finale viewed flag.
- [ ] Implement Chapter06Director using the Chapter02 quiz session model and six default Xuexiang Yunwei Pavilion questions.
- [ ] Add a second trigger mode for the final pavilion view.
- [ ] Initialize Chapter06Director from GardenGameManager and expose a SetChapter06Objective wrapper.

### Task 3: Editor Helper And Verification

**Files:**
- Create: `Assets/Scripts/Player/Editor/Chapters/Chapter06/Chapter06DirectorAttachmentTool.cs`
- Create matching `.meta` files and folder metas.

- [ ] Add a menu item to attach Chapter06Director to a selected trigger.
- [ ] Run targeted EditMode tests for Chapter06 and existing Chapter02 coverage.
- [ ] Run a broader Unity EditMode test pass if the editor command is available.
