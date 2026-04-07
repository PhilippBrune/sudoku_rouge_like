# Save & Load Architecture

**Version:** 1.0 | **Date:** 2026-03-21 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.1 | 2026-03-23 | implemented | TimestampUtc added to SaveFileEnvelope, SteamCloudSyncService recreated as ICloudSaveProvider stub |
| 1.0 | 2026-03-21 | implemented | Initial specification |

---

## Overview

Run of the Nine uses a **dual-file save system**: one file for the persistent player profile and one for the active run state. Saves are JSON-serialized, versioned, and backed up with rotation. Only **Garden Run mode** supports save & resume — Tutorial, Endless Zen, and Spirit Trials do not persist mid-session state.

---

## Save Files

| File | Path | Contents | Lifecycle |
|------|------|----------|-----------|
| Profile | `{persistentDataPath}/profile_save.json` | Player options, meta progression, stats, mastery, completion, tutorial progress, codex | Created on first launch; updated after every run and settings change |
| Run | `{persistentDataPath}/run_save.json` | Active run state + current puzzle state | Created on first autosave of a run; deleted on run completion or abandonment |

### Save Version

Current version: `"1.0.0"` (stored in `SaveFileEnvelope.SaveVersion`).

Version is checked on load. Future migrations will convert older formats forward.

---

## Save File Envelope

The `SaveFileEnvelope` is the root serialization container for both profile and run saves:

```
SaveFileEnvelope
├── SaveVersion          (string: "1.0.0")
├── PlayerProfile        (ProfileSaveData)
│   └── Options          (OptionsState: audio, graphics, gameplay, accessibility, language)
├── MetaProgress         (MetaProgressionState)
│   ├── UnlockedClasses  (List<ClassId>)
│   ├── DiscoveredRelics (List<RelicId>)
│   ├── GardenProgression (per-class TotalXp + PrestigeTier)
│   ├── ClassUnlocks     (cumulative boss defeats, items used, relics collected, gold collected)
│   ├── ItemCodex        (discovery + mastery tracking per item/relic)
│   ├── AscensionLevel, PrestigeCount, MaxStarCap
│   ├── PurchasedPermanentUpgrades
│   └── UnlockedAchievements
├── ActiveRunState       (RunState — only in run save)
├── ActivePuzzle         (PuzzleSaveState — only in run save)
├── TutorialProgress     (TutorialProgressState)
├── Statistics           (ProfileStats)
├── Mastery              (MasteryAchievementState)
└── Completion           (CompletionTrackerState)
```

---

## Autosave System

### When Autosave Triggers

`RunAutoSaveCoordinator` listens to `RunDirector.SaveRequested` events. Autosave fires:

1. **After each puzzle completion** — before the item reward phase
2. **After item selection** — once the player picks a slot or Nothing
3. **After shop transaction** — each purchase or exit
4. **After boss modifier selection** — once locked in
5. **After floor transition** — when entering a new floor
6. **On Save & Quit** — when the player chooses Save & Quit from the garden overview pause menu

### What Gets Saved

Each autosave captures:
- Full `RunState` (HP, Pencil, Gold, XP, inventory, relic, path history, boss modifier state, floor, node index)
- Current `PuzzleSaveState` (board, solution, moves, pencil marks, fog state, modifier overlay)
- All profile data (options, meta, stats, mastery, completion, codex)

### Pre-Save Sync

Before building the envelope, `RunAutoSaveCoordinator.Save()` calls:
- `RunState.SyncSeenModifiersToList()` — converts `SeenBossModifiers` HashSet to serializable `SeenBossModifierList`

---

## Resume Flow

### Entry Point

`RunResumeService.TryResumeFromSave(RunDirector, SaveFileEnvelope)` restores a saved run.

### Resume Steps

1. Load `SaveFileEnvelope` via `SaveFileService.TryLoadRun()`
2. Validate envelope integrity (`ValidateEnvelope()`)
3. Sanitize all fields (`SanitizeRunState()`, `SanitizeProfileAndMeta()`, `ValidateAndSanitizePuzzleState()`)
4. Start run via `RunDirector.StartRun()` with saved ClassId, Mode, Depth
5. Copy all RunState fields:
   - Resources: HP, Pencil, Gold, XP, RerollTokens, ItemSlots
   - Inventory: ItemInstance list (Id, Type, Rarity, Charges, IsInfinite)
   - Relic: HasRelic + HeldRelic (RelicInstance)
   - Path: RouteHistory, NodePath, CurrentFloor, TotalFloors, CurrentNodeIndex
   - Boss: HasChosenBossModifier, ChosenBossModifierId, ChosenBossModifiers list, SeenBossModifierList → HashSet sync
   - Settings: AllowIrregularPuzzles
6. Rebuild floor graph if `CurrentFloor > 0` via `RebuildCurrentFloorGraph()`
7. Attempt puzzle state restoration via `TryRestorePuzzleSaveState()`
8. Delete the run save file on successful resume (run continues in memory)

### Resume Button Visibility

The main menu **Resume Game** button is only visible when `SaveFileService.TryLoadRun()` returns true.

---

## Modes and Save Support

| Mode | Profile Save | Run Save & Resume | Reason |
|------|:---:|:---:|--------|
| Garden Run | Yes | Yes | Core mode with multi-floor progression |
| Tutorial | Yes (progress tracking) | No | Single isolated puzzles, no run state |
| Endless Zen | Yes (depth records) | No | Session-based, no mid-session save |
| Spirit Trials | Yes (high scores) | No | Timed mode, no mid-session save |

---

## Validation & Sanitization

On every load, all data passes through validation and sanitization to prevent corrupted saves from crashing the game:

### Profile Sanitization (`SanitizeProfileAndMeta`)

| Field | Clamp Range | Default |
|-------|-------------|---------|
| Audio volumes (Master/Music/SFX/UI) | 0.0–1.0 | 1.0 |
| Board sizes | 5–9 | 9 |
| MaxStarCap | 1–10 | 5 |
| Garden Level | 1–40 | 1 |
| PrestigeTier | 0–9 | 0 |

### Run State Sanitization (`SanitizeRunState`)

| Field | Clamp Range | Default |
|-------|-------------|---------|
| HP (Current/Max) | 1–99 | Class default |
| Pencil (Current/Max) | 1–99 | Class default |
| ClassId | Valid enum | NumberFreak |
| GameMode | Valid enum | GardenRun |

### Puzzle State Validation (`ValidateAndSanitizePuzzleState`)

| Field | Valid Values | Action on Invalid |
|-------|-------------|-------------------|
| BoardSize | 4, 5, 6, 8, 9 | Reject puzzle state |
| Stars | 1–6 | Clamp |
| Difficulty | Diff1–Diff5 | Clamp |

---

## Backup System

### Rotation Policy

- Maximum **5 backup snapshots** per save file (profile and run independently)
- Backup filename pattern: `{path}.backup_{yyyyMMddHHmmss}`
- On each save, the current file is copied to a timestamped backup before overwriting
- When backup count exceeds 5, the oldest backup is deleted

### Atomic Writes

All saves use an atomic write pattern:
1. Write to temporary file (`{path}.tmp`)
2. Use `File.Replace(temp, target, backup)` to swap atomically
3. If the write fails, the previous save file remains intact

### Restore

`TryRestoreLatestRunBackup()` / `TryRestoreLatestProfileBackup()` find the newest backup file and copy it to the primary save path. Used as a fallback when the primary save is corrupted.

---

## Save & Quit Flow (Garden Run Only)

1. Player opens pause menu from the **garden overview** (path screen between puzzles)
2. Player selects **Save & Quit**
3. `RunAutoSaveCoordinator.Save()` captures full state
4. Game returns to main menu
5. On next launch, **Resume Game** button appears
6. Selecting Resume loads the envelope and calls `TryResumeFromSave()`

Save & Quit is **not available mid-puzzle** — only from the garden overview pause menu. If the player force-quits mid-puzzle, the last autosave (from puzzle completion) is used.

---

## Data Lifecycle

```
First Launch
    │
    ▼
Create default ProfileSave (NumberFreak unlocked, default options)
    │
    ▼
Start Garden Run → first autosave creates RunSave
    │
    ▼
Play through floor (autosave after each puzzle/shop/event)
    │
    ▼
┌─────────────────┐     ┌──────────────┐
│ Run Complete     │     │ Save & Quit  │
│ or Game Over     │     │              │
└────────┬────────┘     └──────┬───────┘
         │                      │
         ▼                      ▼
  Delete RunSave          RunSave persists
  Update ProfileSave      (Resume available)
  (XP, unlocks, stats)
```

---

## Source Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Save/SaveFileService.cs` | File I/O, validation, sanitization, backup rotation, atomic writes |
| `Assets/Scripts/Save/RunAutoSaveCoordinator.cs` | Event-driven autosave coordinator |
| `Assets/Scripts/Save/RunResumeService.cs` | Run state restoration from save envelope |
| `Assets/Scripts/Save/ProfileService.cs` | Profile management, post-run updates, class unlocks |
| `Assets/Scripts/Core/RuntimeModels.cs` | `SaveFileEnvelope`, `RunState`, `MetaProgressionState`, all nested data models |

---

## Steam Cloud Sync

### Architecture

Steam Cloud integration uses the Steamworks `RemoteStorage` API to sync save files across devices.

### Sync Strategy — Newer Wins with Conflict Dialog

```
On Game Launch:
    1. Load local save timestamps (profile + run)
    2. Check Steam Cloud for remote copies
    3. Compare timestamps:
       a. If local == remote → no action
       b. If local > remote → upload local to cloud
       c. If remote > local → download remote, replace local
       d. If both modified (conflict) → show Conflict Dialog
```

### Conflict Dialog

When both local and remote have been modified since last sync:

```
┌──────────────────────────────────────┐
│  Save Conflict Detected              │
│                                      │
│  Local:  Floor 3, L12 Monk, 2h ago   │
│  Cloud:  Floor 2, L11 Monk, 5h ago   │
│                                      │
│  [ Use Local ]    [ Use Cloud ]      │
└──────────────────────────────────────┘
```

The dialog shows:
- Current floor / depth for run saves
- Class and level for profile saves
- Relative timestamp ("2 hours ago")
- Player explicitly chooses which version to keep

### Files Synced

| File | Sync | Reason |
|------|:---:|--------|
| `profile_save.json` | Yes | Persistent progression, options, stats |
| `run_save.json` | Yes | Active run state for cross-device resume |
| Backup files | No | Local-only safety net; cloud has its own versioning |

### Implementation Notes

- `SteamRemoteStorage.FileWrite()` / `FileRead()` for upload/download
- Store a `sync_metadata.json` locally with last sync timestamp per file
- On save: write local first (atomic), then upload to cloud asynchronously
- On load: check cloud first, then fall back to local if cloud unavailable
- Offline play: local saves work normally; sync on next online launch
- Steam Cloud quota: profile (~50KB) + run (~200KB) well within Steam's 1GB default

---

## Open Issues

No open issues remaining.


---

---

## Requirements Traceability

<!-- AUTO-GENERATED by SPICE pipeline. Do not edit manually. -->

| REQ-ID | Title | Code File | Verified Classes | Status |
|--------|-------|-----------|-----------------|--------|
| REQ-SAVE-018 | TITLE | `—` | — | ✗ missing |
| REQ-SAVE-019 | TITLE | `Assets\Scripts\Sudoku\ConstraintRules.cs` | — | ✓ verified |
| REQ-SAVE-020 | TITLE | `Assets\Scripts\Save\RunAutoSaveCoordinator.cs` | RunAutoSaveCoordinator | ✓ verified |
| REQ-SAVE-021 | TITLE | `—` | — | ✗ missing |
| REQ-SAVE-022 | TITLE | `Assets\Scripts\Bootstrap\GameBootstrap.cs` | — | ✓ verified |
| REQ-SAVE-023 | TITLE | `Assets\Scripts\Save\RunAutoSaveCoordinator.cs` | RunAutoSaveCoordinator | ✓ verified |
| REQ-SAVE-024 | TITLE | `Assets\Scripts\Bootstrap\GameBootstrap.cs` | — | ✓ verified |
| REQ-SAVE-025 | TITLE | `Assets\Scripts\Save\RunAutoSaveCoordinator.cs` | RunAutoSaveCoordinator | ✓ verified |
| REQ-SAVE-026 | TITLE | `Assets\Scripts\Sudoku\ConstraintRules.cs` | — | ✓ verified |
| REQ-SAVE-027 | TITLE | `Assets\Scripts\Core\HardeningModels.cs` | SaveFileEnvelope | ✓ verified |
| REQ-SAVE-028 | TITLE | `Assets\Scripts\Run\RunDirector.cs` | RunDirector | ✓ verified |
| REQ-SAVE-029 | TITLE | `—` | — | ✗ missing |
| REQ-SAVE-030 | SaveSanitationService | `Assets\Scripts\Sudoku\ConstraintRules.cs` | — | ✓ verified |
| REQ-SAVE-031 | TITLE | `Assets\Scripts\Save\RunAutoSaveCoordinator.cs` | RunAutoSaveCoordinator | ✓ verified |
| REQ-SAVE-032 | TITLE | `—` | — | ✗ missing |
| REQ-SAVE-033 | TITLE | `Assets\Scripts\Bootstrap\GameBootstrap.cs` | — | ✓ verified |
| REQ-SAVE-034 | TITLE | `Assets\Scripts\Save\RunAutoSaveCoordinator.cs` | RunAutoSaveCoordinator | ✓ verified |
| REQ-SAVE-035 | TITLE | `Assets\Scripts\Bootstrap\GameBootstrap.cs` | — | ✓ verified |
| REQ-SAVE-036 | TITLE | `Assets\Scripts\Save\RunAutoSaveCoordinator.cs` | RunAutoSaveCoordinator | ✓ verified |
| REQ-SAVE-037 | TITLE | `Assets\Scripts\Sudoku\ConstraintRules.cs` | — | ✓ verified |
| REQ-SAVE-038 | TITLE | `Assets\Scripts\Core\HardeningModels.cs` | SaveFileEnvelope | ✓ verified |
| REQ-SAVE-039 | TITLE | `Assets\Scripts\Run\RunDirector.cs` | RunDirector | ✓ verified |
