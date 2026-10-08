# Save & Load Architecture

**Version:** 2.1  
**Date:** 2026-10-07  
**Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 2.1 | 2026-10-07 | implemented | What resumes from which screen (SPEC-AMB-005, GAP-018/021); compatibility rules incl. 1.0 → 1.1 (SPEC-MAC-009); autosave reloads before writing (GAP-037); mistakes saved immediately (GAP-031); XP log and analytics saved with the run (GAP-038); abandoning a saved run (GAP-032, SPEC-AMB-010). |

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

### What is kept across Save & Quit → Resume (1.1, SPEC-AMB-005, GAP-018)

| Screen when saving | What the resumed run shows |
|--------------------|----------------------------|
| Path map | The same floor graph (`FloorGraphVersion`) and the same puzzle configs for every tile (`NodeConfigs`, saved per floor), so the map preview matches the generated puzzle |
| Inside a puzzle | The same board, placed digits, pencil marks, HP and timers (`PuzzleSaveState`); mistakes are saved immediately (GAP-031) |
| Boss gate | The same modifier options (decision seed per floor/node) and the same boss debuff (seed + floor) |
| Item reward | The same slots; a reroll gives the same new slots as before the save |
| Shop | The same offers; each shop reroll draws the next deterministic set |
| Relic choice | The same relics; accepting the shown relic works even without the rolled list (GAP-021) |
| Event | The same event and the same outcome per option |

Run decisions use a seed derived from (run seed, floor, depth, node, purpose), so nothing is re-rolled on resume. Evidence: `DeterminismAcrossResumeTests`, `NodeConfigPreviewTests`, `ResumeMidPuzzleTests`.

The run's XP log and analytics (`RunState.TileXpLog`, `RunState.Analytics`) are saved too: before 1.1 they were rebuilt empty on resume, so the XP, item counts and run score of the tiles played before a Save & Quit were lost at run end (GAP-038, fixed 1.1).

### Compatibility (SPEC-MAC-009)

- Saves are JSON (`JsonUtility`); a field missing from an older save keeps its initializer value. New fields are therefore added with safe defaults or a version flag.
- **1.0 → 1.1:** 1.0 saves load. A run resumed from 1.0 keeps its old bridge-style floor graph (`FloorGraphVersion = 0`) until the next floor; it has no saved node configs, so they are built at resume; its XP log starts empty (the 1.0 log was never saved). Completion data lost to the 1.0 bugs (GAP-009, GAP-037) cannot be recovered.
- A damaged slot file falls back to its `.bak` copy written before each save.

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

Since 1.1 (GAP-037) the coordinator reloads the slot file before every write and merges only the run fields into it, so profile data written in between (codex discoveries, in-run option changes, prestige) is never overwritten by an older copy. `AutosaveMergeTests` covers this.

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

### Abandoning a saved run (1.1, GAP-032, SPEC-AMB-010)

Starting a new run while a Garden Run is saved shows a warning; "Abandon & Begin" ends the saved run as a defeat first (`RunResumeService.AbandonSavedRun`): its XP, stats and unlock progress are recorded like any other defeat and the saved run is cleared. The in-run Abandon Run button does the same through the normal defeat screen. Evidence: `RunSeedAndAbandonTests`, `InRunUiPlayModeTests.RunMenu_ShowsSeedAndModifiers_AndAbandonEndsTheRun`.

## Local-Only Scope

The checked repo does **not** include active cloud-save or conflict-resolution services. Earlier design references to `SteamCloudSyncService`, `SaveConflictService`, or provider abstractions do not match the current checked files.

This implementation is local-slot based only.

`Assets/Scripts/Platform/PlatformServices.cs` now exposes this explicitly through
`LocalOnlyCloudSaveProvider`, which reports cloud save as `NotConfigured`.
Future Steam Cloud or platform-sync work should hang off that capability surface
only after conflict resolution, offline behavior, and migration tests are
defined.
