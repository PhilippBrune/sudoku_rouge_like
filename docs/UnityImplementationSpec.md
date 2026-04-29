# Unity Implementation Specification

**Version:** 2.0  
**Date:** 2026-04-27  
**Status:** implemented

## Purpose

This document is the implementation-first snapshot for the checked Unity project. It intentionally replaces older prototype-era assumptions that no longer match the current scripts.

## Core Runtime Layout

Primary entry points:

- `Assets/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Scripts/UI/MainMenuBlueprintBuilder.cs`
- `Assets/Scripts/UI/MainMenuController.cs`
- `Assets/Scripts/UI/InRunController.cs`
- `Assets/Scripts/Run/RunDirector.cs`

Main responsibilities:

- `GameBootstrap` owns startup, profile wiring, mode launch, and resume launch
- `MainMenuBlueprintBuilder` builds the menu UI in code
- `MainMenuController` handles menu navigation and launch requests
- `RunDirector` owns run state, level generation, rewards, economy, boss flow, and special modes
- `InRunController` owns in-session UI flow and puzzle interaction

## Game Modes

Current checked-in modes:

- Garden Run
- Endless Zen
- Spirit Trials
- Seasonal Challenge
- Tutorial / Sudoku Basics

### Garden Run

Garden Run uses:

- class selection
- Harmony difficulty selection
- branching floor graph
- floor modifiers
- boss modifier choice gates
- reward/shop/relic/rest/event flow
- resumable local save

### Endless Zen

Endless Zen now launches through its own run-state path:

- direct class selection
- direct puzzle view
- no path graph
- 9x9 only
- depth-based scaling
- no progression rewards
- no resume

### Spirit Trials

Spirit Trials now launches through its own run-state path:

- tier select, then class select
- direct puzzle view
- one puzzle per session
- timer starts on first interaction
- no progression rewards
- no resume

## Harmony Difficulty

Harmony is wired into normal Garden Run launch through `LaunchRequest.HarmonyLevel` and `LaunchRequest.HarmonyPerk`.

Affected systems include:

- run archetype setup
- floor modifier pressure
- relic availability
- shop surcharge
- XP multiplier at run end

Primary references:

- `Assets/Scripts/Run/HarmonyDifficultyService.cs`
- `Assets/Scripts/Run/RunArchetypeService.cs`
- `Assets/Scripts/UI/GameModesPanelController.cs`

## Boss Modifiers

Boss puzzles now stack:

- active floor modifiers
- chosen boss modifiers

This behavior is built in `RunDirector.BuildLevelConfig(...)`.

## Save / Resume

The current repo uses one per-slot JSON envelope:

- `save_profile_0.json`
- `save_profile_1.json`
- `save_profile_2.json`

There is no active cloud-save/conflict-resolution implementation in the checked files.

Resume is restricted to runs where:

- `TutorialMode == false`
- `DisableProgressionRewards == false`

## Input

The runtime input stack is custom:

- keyboard rebinding through `InputRemapService`
- fixed gamepad defaults through direct `Input` / `KeyCode`
- no Unity Input System gameplay action asset
- no Steam Input action-set layer

## Accessibility

Implemented accessibility support includes:

- colorblind mode
- high contrast mode
- reduce motion
- alternative symbols
- font scale
- on-screen accessibility announcement captions

There is no OS-level screen-reader integration in the checked files.

## Tutorial Basics

The first-time Sudoku Basics tutorial is:

- optional
- triggered from a main-menu prompt
- deterministic
- based on a fixed 6x6 board
- non-progressing (`DisableProgressionRewards = true`)

## Known Repository Caveats

These are repository-level issues visible today and not specific to the latest mode fixes:

- the generated `.sln` / `.csproj` set contains stale references to missing source files
- direct `dotnet build` therefore fails before giving a clean gameplay-only compile signal
- several older design docs referenced unimplemented platform services; those docs have been reduced to current behavior in this pass
