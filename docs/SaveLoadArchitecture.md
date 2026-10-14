# Save & Load Architecture

**Version:** 2.4  
**Date:** 2026-10-14  
**Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 2.4 | 2026-10-14 | implemented | Active profile slot without a stored pref: the most recently written profile is used and stored (SAVE-SLOT-004, GAP-043, ActiveSlotRecoveryTests). |
| 2.3 | 2026-10-14 | implemented | Save folder stays LocalLow/Kratossos/sudoku_rouge_like, the folder of the released 1.0 build and the Steam Auto-Cloud root (SAVE-FOLDER-001); the 2.2 move to "Run of the Nine" was withdrawn the same day, before any release. SAVE-MIGRATE-001 copies only from the pre-release folder DefaultCompany/RunOfTheNine. Steam Auto-Cloud recorded as configured (portal check 2026-10-14). |
| 2.2 | 2026-10-14 | implemented | Save folder follows the Steamworks names: LocalLow/Kratossos/Run of the Nine; one-time copy of earlier save folders (SAVE-MIGRATE-001, LegacySaveMigrationTests); optional Steam Auto-Cloud settings recorded. |
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

On Windows `persistentDataPath` is `%USERPROFILE%\AppData\LocalLow\Kratossos\sudoku_rouge_like` (company and product name from the Player Settings, SAVE-FOLDER-001).

Crash-safety support:

- `.tmp` temporary file during write
- `.bak` backup file used as load fallback

### Save folder (SAVE-FOLDER-001)

| ID | Requirement |
|---|---|
| SAVE-FOLDER-001 | The Player Settings company and product names stay `Kratossos` / `sudoku_rouge_like`, so the save folder is `LocalLow\Kratossos\sudoku_rouge_like`: the folder of the released 1.0 build (Steam build 24153478, whose `app.info` holds these names) and the Steam Auto-Cloud root. Changing either name would hide every player's saves from the game and from Steam Cloud. The store name "Run of the Nine" is shown as the window title instead (GameDesignSpec DISPLAY-MODE-007). Test: `LegacySaveMigrationTests.PlayerSettings_KeepTheSaveFolderOfTheReleasedGame`. |

### Active profile slot (SAVE-SLOT-004)

| ID | Requirement |
|---|---|
| SAVE-SLOT-004 | The active slot is stored in PlayerPrefs (`ActiveProfileSlot`, the registry on Windows), the profiles in files. When no slot is stored but profiles exist, the most recently written profile file becomes the active slot and is stored; ties go to the lower slot. Without any profile nothing is stored, so a fresh install still shows profile select. Reason: Steam Cloud restores `save_profile_*.json` on a new PC or after a reinstall, not the registry; before 0.1.5 the game opened an empty slot 0 there (GAP-043). Code: `SaveProfileService.ActiveSlot`, `EnsureActiveSlot`, `NewestExistingSlot`; tests: `ActiveSlotRecoveryTests`. |

### Earlier save folders (SAVE-MIGRATE-001)

| ID | Requirement |
|---|---|
| SAVE-MIGRATE-001 | Before the first scene loads, the Windows/Linux player copies `save_profile_*.json` and `save_profile_*.json.bak` from `LocalLow\DefaultCompany\RunOfTheNine` (pre-release Steam builds, June to early July 2026) into the current folder, but only when the current folder has no profile. Originals are never deleted or overwritten; `.tmp` files are not copied. The most recently written copied profile becomes the active slot (the `ActiveProfileSlot` PlayerPref lived under the old names). The Editor and test runs never migrate. Code: `LegacySaveMigration`; tests: `LegacySaveMigrationTests`. |

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

Steam Auto-Cloud needs no game code and is configured in the Steamworks portal (PRG-CLOUD-SAVE; checked 2026-10-14, unchanged since 1.0): root `WinAppDataLocalLow`, subdirectory `Kratossos/sudoku_rouge_like/`, pattern `save_profile_*.json`, OS Windows, quota 100 MB and 100 files. Steam syncs these files around each session and resolves conflicts with its own dialog; the in-game capability keeps reporting `NotConfigured` until in-game sync is built (FUTURE-006).
Future Steam Cloud or platform-sync work should hang off that capability surface
only after conflict resolution, offline behavior, and migration tests are
defined.
