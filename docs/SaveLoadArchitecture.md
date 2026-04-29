# Save & Load Architecture

**Version:** 2.0  
**Date:** 2026-04-27  
**Status:** implemented

## Overview

The current repo uses one JSON save envelope per profile slot. Profile data and any resumable run state live inside the same file.

Primary implementation references:

- `Assets/Scripts/Core/SaveModels.cs`
- `Assets/Scripts/Save/SaveFileService.cs`
- `Assets/Scripts/Save/SaveProfileService.cs`
- `Assets/Scripts/Save/RunAutoSaveCoordinator.cs`
- `Assets/Scripts/Save/RunResumeService.cs`
- `Assets/Scripts/Bootstrap/GameBootstrap.cs`

## Slot Files

Each profile slot writes to:

```text
{Application.persistentDataPath}/save_profile_0.json
{Application.persistentDataPath}/save_profile_1.json
{Application.persistentDataPath}/save_profile_2.json
```

Crash-safety support:

- `.tmp` temporary file during write
- `.bak` backup file used as load fallback

## Save Envelope

`SaveFileEnvelope` currently contains:

- `PlayerProfile`
- `MetaProgress`
- `ActiveRunState`
- `ActivePuzzle`
- `ActiveSeasonalRunState`
- `ActiveSeasonalPuzzle`
- `TutorialProgress`
- `Statistics`
- `Mastery`
- `Completion`
- `DailyGoals`
- `SeasonalChallenge`

## What Can Resume

Resume is currently allowed only when all of the following are true:

- `ActiveRunState` exists
- `TutorialMode == false`
- `DisableProgressionRewards == false`

That means normal Garden Run progress can resume, while these do not:

- Sudoku Basics tutorial
- custom tutorial runs
- Endless Zen
- Spirit Trials
- other no-progression sessions

## Seasonal Challenge Handling

Seasonal Challenge uses dedicated save fields:

- `ActiveSeasonalRunState`
- `ActiveSeasonalPuzzle`

This avoids overwriting a normal in-progress run.

## Autosave Behavior

`RunAutoSaveCoordinator` writes the active envelope after important run transitions. It stores:

- full `RunState`
- current `PuzzleSaveState`
- updated daily-goal state
- discovered boss modifiers merged into meta progression

Tutorial and no-progression modes are skipped deliberately.

## Resume Flow

Current resume flow:

1. `RunResumeService` loads the envelope
2. resumability is validated
3. transient runtime state is rebuilt
4. `GameBootstrap.LaunchResume()` or `RunResumeService.TryResumeFromSave(...)` reinitializes `RunDirector`
5. saved `RunState` is restored
6. floor graph is rebuilt
7. puzzle state is restored if present

Important restore detail:

- `HarmonyConfig` is rebuilt from `HarmonyLevel`

## Local-Only Scope

The checked repo does **not** include active cloud-save or conflict-resolution services. Earlier design references to `SteamCloudSyncService`, `SaveConflictService`, or provider abstractions do not match the current checked files.

This implementation is local-slot based only.
