# Tutorial Mode System (Implemented)

## Purpose

Tutorial mode is an isolated practice environment using the same Sudoku engine as normal play, but with progression disabled.

No rewards in tutorial:
- No XP
- No Gold
- No Essence
- No relic unlocks
- No class leveling
- No meta stat tracking

## Main Menu Integration

The runtime menu flow includes tutorial entry points:
- Main menu -> Tutorial
- Tutorial setup screen
- Tutorial progress screen

## Setup Configuration

Tutorial setup supports:
- Board size: 5x5..9x9
- Stars: 1..5
- Modifiers: 0..2 from boss modifier pool
- Resource mode:
  - Free: infinite HP and infinite Pencil
  - Simulation: 10 HP, 10 Pencil, no Gold

Balancing constraint implemented:
- On boards < 7x7, German Whispers and Killer Cages are disabled.

## Session Rules

In tutorial sessions:
- Item roll phase is disabled
- Rerolls are disabled
- Route branching is disabled
- Gold spending is disabled
- Progression rewards are disabled
- Class passives/leveling progression logic is disabled

Mistake behavior:
- Free mode: no HP penalty
- Simulation mode: normal HP penalty

## Completion Tracking

Completion key format:
- [BoardSize]|[Stars]|[SortedModifierCombo]

Example:
- `5|1|None`
- `9|5|GermanWhispers`
- `9|5|FogOfWar+RenbanLines`

Tracked in profile tutorial state:
- Completed configurations
- Completed single-modifier trainings

Progress views supported by service:
- Board-size x star grid completion
- Modifier training completion list
- Aggregate completion percent

## UI Indicator

Use this label during tutorial play:
- `TUTORIAL MODE | No Progression Rewards`

## Code Locations

- Tutorial setup + validation + descriptions: `Assets/Scripts/Tutorial/TutorialModeService.cs`
- Tutorial completion tracking: `Assets/Scripts/Tutorial/TutorialProgressService.cs`
- Tutorial flow/menu hooks: `Assets/Scripts/UI/MenuFlowService.cs`
- Tutorial run isolation logic: `Assets/Scripts/Run/RunDirector.cs`
- Tutorial profile state: `Assets/Scripts/Save/ProfileService.cs`
