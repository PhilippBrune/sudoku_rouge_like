# Spirit Trials Mode

**Version:** 2.4  
**Date:** 2026-11-03  
**Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 2.4 | 2026-11-03 | implemented | Owner (2026-11-03): "if the requirement gives a starting item, there must be a slot for it". TRIAL-SESSION-001 promised one starting item while the code gave `ItemSlots = 0`: a trial now starts with one item in one slot, Normal on Apprentice and Adept, Rare on Master and Grandmaster (the 0.1 spec's table), and the bag shows that slot without the relic part (TRIAL-HUD-001, GAP-113). The 0.1 spec's "Solver Start" choice is not built. |
| 2.3 | 2026-11-02 | implemented | 0.3.1 playtest (owner): the trial's puzzle shows no bag and no relic slots (TRIAL-HUD-001, GAP-112). |
| 2.2 | 2026-10-28 | implemented | 0.3.0 playtest (RM-217, owner's pick UI-STYLE-035): the tier select as four steles with the chosen tier's slip; stars and rules per tier come from `SpiritTrialsService.StarsFor` and `RulesFor`. |
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
| Items | **[TRIAL-SESSION-001]** `ItemSlots = 1` with one starting item: Normal on Apprentice and Adept, Rare on Master and Grandmaster. The seed picks it from Solver, Finder, Ink Well, Meditation Stone, Chime of Notes and Koi Reflection (items that help in any one puzzle; no path, gold, fog or rule-zone items). `SpiritTrialsService.StartingItem` |
| Relics | None |
| Puzzle screen | **[TRIAL-HUD-001]** The bag shows the item's one slot and no relic part (0.3.1 playtest, GAP-112: the screen showed empty item and relic slots; owner 2026-11-03, GAP-113: the starting item gets its slot) |
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
