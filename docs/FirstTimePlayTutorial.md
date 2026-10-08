# First-Time Play Tutorial

**Version:** 2.1  
**Date:** 2026-10-07  
**Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 2.1 | 2026-10-07 | implemented | 15 shipped steps written down as acceptance criteria (SPEC-CONF-037, SPEC-MAC-002); TUTO-BASICS-STEP-001/002/006 re-specified; evidence SudokuBasicsStepsTests. |

## Overview

The first-time onboarding flow is a short optional Sudoku Basics tutorial presented from the main menu. It is deterministic, progression-free, and separate from the custom puzzle tutorial builder.

Primary implementation references:

- `Assets/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Scripts/UI/MainMenuController.cs`
- `Assets/Scripts/Tutorial/TutorialModeService.cs`
- `Assets/Scripts/Run/RunDirector.cs`

## Trigger Flow

- After the main menu loads, `GameBootstrap.Start()` checks whether `TutorialProgress.CompletedKeys` contains `sudoku_basics`.
- If not, `MainMenuController.ShowSudokuBasicsPrompt()` shows a modal prompt.
- Choosing **Yes, show me** marks the tutorial as completed for future launches and starts the tutorial immediately.
- Choosing **Skip** marks it as completed without launching it.
- The tutorial can also be launched manually from the menu via `MainMenuController.LaunchSudokuBasicsTutorial()`.

## Tutorial Board

The shipped basics tutorial uses a fixed **6x6** Sudoku board, not 4x4.

Current runtime facts:

- `BoardSize = 6`
- `Seed = 999`
- `RegionVariant = 0`
- deterministic hand-authored givens and solution
- no active modifiers

Board construction lives in:

- `TutorialModeService.BuildSudokuBasicsLevel()`
- `TutorialModeService.BuildBasicsTutorialBoard()`

## Run State Rules

The tutorial run is intentionally non-punishing:

- `HP = 999`
- `Pencil = 999`
- `CurrentGold = 0`
- `ItemSlots = 0`
- `TutorialMode = true`
- `DisableProgressionRewards = true`
- `AllowIrregularPuzzles = false`

This means:

- mistakes do not meaningfully threaten failure
- no run rewards are generated
- no save/resume state is created
- class progression is not advanced

## Scripted Teaching Flow — acceptance criteria

The board is deterministic so `SudokuBasicsTutorialController` (driven by `InRunController`) can point at exact cells and expected actions. The 15 steps below are the shipped lesson and its acceptance criteria (decided 2026-10-07, SPEC-CONF-037; earlier drafts described 7 or 11 steps). Rows and columns are 0-based. Explanation steps block all board input and advance with **Next**; interaction steps advance only when the expected action is done, and a wrong digit there shows a hint instead of a penalty.

| Step | Title | Type | Highlight / target | Expected action |
|:---:|-------|------|--------------------|-----------------|
| 0 | Welcome to Sudoku | Explanation | — | Next |
| 1 | The Row Rule | Explanation | row 0 | Next |
| 2 | The Column Rule | Explanation | column 1 | Next |
| 3 | The Box Rule | Explanation | top-left 2×3 box | Next |
| 4 | Finding the Answer | Explanation | row 3, target (3,1) | Next |
| 5 | Narrow It Down | Explanation | column 1, target (3,1) | Next |
| 6 | Only One Answer! | Explanation | row 3 + column 1, target (3,1) | Next |
| 7 | Your Turn! | Interaction | cell (3,1) | place **6** |
| 8 | Well Done! | Explanation | row 3, target (3,2) | Next |
| 9 | One More! | Interaction | cell (3,2) | place **1** |
| 10 | Pencil Marks | Explanation | cell (0,1) | Next |
| 11 | Mark Your Candidates | Pencil interaction | row 0, target (0,1) | pencil-mark **2 and 6** |
| 12 | Eliminate With Logic | Explanation (removes the 6 mark) | column 1, target (0,1) | Next |
| 13 | Commit the Answer | Interaction | cell (0,1) | place **2** |
| 14 | You've Got It! | Explanation | — | finish |

Evidence: `SudokuBasicsStepsTests` (step count, interactive targets and digits match the board's solution). TUTO-BASICS-STEP-006 ("a step blocks input until the correct digit is placed") is step 7.

## Persistence

Completion is stored by writing `sudoku_basics` into `TutorialProgress.CompletedKeys`.

Implementation reference:

- `MainMenuController.MarkSudokuBasicsComplete()`

## Scope Boundary

This document only covers the first-time Sudoku Basics flow. The separate free-form tutorial/custom puzzle setup uses:

- `TutorialMenuController`
- `TutorialSetupConfig`
- `TutorialModeService.BuildTutorialLevel()`
