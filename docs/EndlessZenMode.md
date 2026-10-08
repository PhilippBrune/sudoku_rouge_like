# Endless Zen Mode

**Version:** 2.1  
**Date:** 2026-10-07  
**Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 2.1 | 2026-10-07 | implemented | Declared the source for Endless Zen rules over GDS §13 (SPEC-CONF-006); unlock metric "runs started" confirmed; modifier pool = generatable player modifiers, not the full enum (SPEC-AMB-009, GAP-007). |

## Overview

Endless Zen is an infinite single-session puzzle chain. It now launches as a dedicated mode instead of reusing Garden Run map flow.

Primary implementation references:

- `Assets/Scripts/UI/MainMenuController.cs`
- `Assets/Scripts/Run/EndlessZenService.cs`
- `Assets/Scripts/Run/RunDirector.cs`
- `Assets/Scripts/UI/InRunController.cs`
- `Assets/Scripts/UI/EndScreenViewController.cs`

## Unlock Condition

`MetaProgressionState.EndlessZenUnlocked` is set once `ProfileStats.TotalRunsStarted >= 10`. Despite its name, `TotalRunsStarted` is counted when a run's result is recorded (`ProfileService.RecordRunAndGetNewUnlocks`): victories, defeats and, since 1.1, abandoned runs; a run that is saved and never finished does not count yet (documented 2026-10-07, SPEC-CONF-006). This document is the source for Endless Zen; GameDesignSpec §13 links here.

Implementation reference:

- `Assets/Scripts/Save/ProfileService.cs`

## Launch Flow

Current menu flow:

1. Open **Game Modes**
2. Choose **Endless Zen**
3. Pick a class
4. Launch `GameMode.EndlessZen`

The mode no longer opens the Garden Run path map. `InRunController.ShowPath()` detects Endless Zen and goes directly to the puzzle view.

## Session Rules

| Parameter | Current Behavior |
|---|---|
| Board size | Always `9x9` |
| Floors / map | No floor graph, no route selection |
| Gold | Starts at `0`; no progression reward flow |
| Items | `ItemSlots = 0` |
| Relics | None are granted during the session |
| XP / meta rewards | Disabled via `DisableProgressionRewards = true` |
| Save & Resume | Disabled |
| Harmony | Forced to base config (`HarmonyLevel = 0`) |
| Irregular toggle | Respected for region variant generation |

## Depth and Recovery Loop

Depth starts at `0`. Each solved puzzle:

- increments `State.Depth`
- restores `+1 HP` up to max
- restores `+3 Pencil` up to max
- generates the next board immediately

Implementation reference:

- `RunDirector.AdvanceEndlessZenLevel()`

## Difficulty Scaling

### Stars

Star rating is computed as:

```text
Clamp(1 + depth / 4, 1, 5)
```

### Modifier Count

Modifier cap is depth-based:

- Depth `0-9`: 1 modifier
- Depth `10-19`: 2 modifiers
- Depth `20+`: 3 modifiers

Modifiers are drawn from the generatable player modifiers for the board size (`BossService.GetGeneratablePlayerModifiers`) and combined with `DrawCombinable`, so blocked pairs never meet (SPEC-AMB-009, GAP-007; the full-enum wording is retired). Region variant uses:

- `0-1` when irregular boards are off
- `0-3` when irregular boards are on

Implementation reference:

- `EndlessZenService.BuildNextLevel()`

## HUD and Results

- The HUD hides puzzle and run timers during Endless Zen.
- Failure still uses HP reaching 0.
- End results are shown through `EndScreenViewController.ShowEndlessZenResult()`.
- Profile stats update `HighestEndlessDepth` and `TotalZenSessions`.

## Explicit Non-Features

The current checked-in implementation does **not** include:

- save/resume
- shops
- reward screens
- relic choices
- Garden Run progression rewards
