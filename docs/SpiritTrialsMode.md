# Spirit Trials Mode

**Version:** 2.1  
**Date:** 2026-10-07  
**Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 2.1 | 2026-10-07 | implemented | Unlock rule confirmed as the single source: first boss defeat (SPEC-CONF-007); modifier pool limited to generatable modifiers (GAP-007). |

## Overview

Spirit Trials is a one-puzzle challenge mode with tier-based resources, random modifiers, and a score result screen. It now launches through its own run-state builder instead of falling through Garden Run setup.

Primary implementation references:

- `Assets/Scripts/UI/MainMenuController.cs`
- `Assets/Scripts/UI/SpiritTrialsTierSelectController.cs`
- `Assets/Scripts/Run/SpiritTrialsService.cs`
- `Assets/Scripts/Run/RunDirector.cs`
- `Assets/Scripts/UI/InRunController.cs`
- `Assets/Scripts/UI/EndScreenViewController.cs`

## Unlock Condition

`MetaProgressionState.SpiritTrialsUnlocked` is set after the player has defeated at least one boss across runs (`ProfileService.RecordRunAndGetNewUnlocks`, `stats.TotalBossesDefeated >= 1`). This is the single unlock rule (decided 2026-10-07, SPEC-CONF-007); TRIAL-UNLOCK-001 and MetaProgressionSystem were aligned, and the Ascension alternative was dropped (Ascension is deferred).

Implementation reference:

- `Assets/Scripts/Save/ProfileService.cs`

## Launch Flow

Current menu flow:

1. Open **Game Modes**
2. Open **Spirit Trials**
3. Choose a tier
4. Pick a class
5. Launch `GameMode.SpiritTrials`

Like Endless Zen, Spirit Trials goes straight to the puzzle view rather than the Garden Run path map.

## Session Rules

| Parameter | Current Behavior |
|---|---|
| Board size | Always `9x9` |
| Puzzle count | Exactly 1 |
| Gold | `0` |
| Items | `ItemSlots = 0` |
| Relics | None |
| XP / meta rewards | Disabled via `DisableProgressionRewards = true` |
| Save & Resume | Disabled |
| Harmony | Forced to base config (`HarmonyLevel = 0`) |
| Irregular toggle | Respected for region variant generation |

## Tier Table

| Tier | Stars | Modifiers | HP | Pencil |
|---|---:|---:|---:|---:|
| Apprentice | 3 | 0 | 5 | 15 |
| Adept | 3 | 1 | 4 | 12 |
| Master | 4 | 1 | 3 | 10 |
| Grandmaster | 5 | 2 | 2 | 8 |

Implementation reference:

- `SpiritTrialsService.BuildTrialLevel()`
- `SpiritTrialsService.CreateTrialRunState()`

## Timer Behavior

The timer no longer starts when the board appears. It starts on the first real interaction through `EnsureSpiritTrialsTimerStarted()`, which is called from:

- cell click
- number entry
- clear cell
- pencil toggle

Implementation reference:

- `Assets/Scripts/UI/InRunController.cs`

## Score Calculation

The checked-in score formula is implemented in `SpiritTrialsService.CalculateScore()`:

```text
FinalScore =
floor(BasePoints * SpeedMultiplier)
+ ConstraintBonus
+ PencilBonus
- MistakePenalty
```

Current runtime pieces:

- base points are `cellsToFill * pointsPerCell`
- speed multiplier is based on tier par time
- constraint bonus depends on modifier count
- pencil bonus rewards low pencil usage
- mistake penalty scales by tier

Per-tier best scores are stored in `ProfileStats`.

## Results

Both success and failure route to `EndScreenViewController.ShowSpiritTrialsResult()`.

The result uses:

- tier played
- score
- elapsed time
- modifier count
- mistakes made

## Explicit Non-Features

The current checked-in implementation does **not** include:

- item starts
- relic starts
- run rewards
- save/resume
- multi-puzzle sessions
