# First-Time Play Tutorial

**Version:** 2.0  
**Date:** 2026-04-27  
**Status:** implemented

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

## Scripted Teaching Flow

The board is deterministic so the in-run tutorial controller can point at exact cells and expected actions. The tutorial scripting lives in `InRunController` and its associated basics tutorial logic.

Verified design intent from code/comments:

- the lesson is step-driven
- specific cells are referenced directly
- later steps expect concrete placements on the fixed 6x6 board

## Persistence

Completion is stored by writing `sudoku_basics` into `TutorialProgress.CompletedKeys`.

Implementation reference:

- `MainMenuController.MarkSudokuBasicsComplete()`

## Scope Boundary

This document only covers the first-time Sudoku Basics flow. The separate free-form tutorial/custom puzzle setup uses:

- `TutorialMenuController`
- `TutorialSetupConfig`
- `TutorialModeService.BuildTutorialLevel()`
