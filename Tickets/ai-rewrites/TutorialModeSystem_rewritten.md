# Tutorial Mode System

## Purpose

Tutorial mode is an isolated environment that uses the same Sudoku engine as normal play, but with certain features disabled for practice.

## Main Menu Integration

The user interface includes a tutorial entry point in the main menu:
- Main menu -> Tutorial
- Tutorial setup screen
- Tutorial progress screen

## Setup Configuration

Tutorial setup supports various configurations:
- Board size: 5x5 to 9x9
- Stars: 1 to 5
- Modifiers: 0 to 2 from the boss modifier pool
- Resource mode:
   - Free: infinite health and infinite pencils
   - Simulation: 10 health, 10 pencils, no gold

A balancing constraint is implemented:
- On boards smaller than 7x7, German Whispers and Killer Cages are disabled.

## Session Rules

In tutorial sessions, certain features are disabled:
- Item roll phase is disabled
- Rerolls are disabled
- Route branching is disabled
- Gold spending is disabled
- Progression rewards are disabled
- Class passives/leveling progression logic is disabled

Mistake behavior in both free and simulation modes:
- Free mode: no health penalty
- Simulation mode: normal health penalty

## Completion Tracking

A completion key is used to track tutorial progress:
- Format: [BoardSize]|[Stars]|[SortedModifierCombo]
- Example: 5|1|None
- Example: 9|5|GermanWhispers
- Example: 9|5|FogOfWar+RenbanLines

Progress is tracked in profile tutorial state:
- Completed configurations
- Completed single-modifier trainings

Progress views are supported by the service:
- Board-size x star grid completion
- Modifier training completion list
- Aggregate completion percentage

## UI Indicator

During tutorial play, use this label:
- `TUTORIAL MODE | No Progression Rewards`

## Code Locations

The tutorial setup, validation, descriptions, completion tracking, and other functionalities are located in the following scripts:
- Tutorial setup + validation + descriptions: `Assets/Scripts/Tutorial/TutorialModeService.cs`
- Tutorial completion tracking: `Assets/Scripts/Tutorial/TutorialProgressService.cs`
- Tutorial flow/menu hooks: `Assets/Scripts/UI/MenuFlowService.cs`
- Tutorial run isolation logic: `Assets/Scripts/Run/RunDirector.cs`
- Tutorial profile state: `Assets/Scripts/Save/ProfileService.cs`


