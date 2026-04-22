# Annotation Gap Report

**Generated:** 2026-04-16  
**Scope:** IDs that exist in `REQUIREMENT_MAP.md` and are marked ✅ implemented, but are **not yet annotated** in the corresponding source file with a `// [REQ: ID]` comment.

**Coverage baseline:** ~50% (122 of 244 IDs have inline annotations after Bucket A pass)

---

## How to Use This Report

Each row names the ID, the file where the annotation belongs, and the exact method or field to annotate.  
Priority = how many dependents break if this is wrong (H=high, M=medium, L=low).

---

## PRESSURE — Pressure Mechanics

| ID | File | Method / Location | Priority |
|----|------|-------------------|----------|
| PRESSURE-TICK-002 | `Assets/Scripts/Run/RunDirector.cs` | `PlaceNumber` — incorrect path after `Mistakes++` (wrong placement also decrements counters) | H |
| PRESSURE-TICK-006 | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — `penaltyPer` calculation (DoublePenalty doubles expiry) | M |
| PRESSURE-TICK-008 | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — `WaveSpawnCount < 3` (wave cap) | M |
| PRESSURE-TICK-011 | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureMistake` — `extraDmg` calc (HauntedCell DoublePenalty stacking) | M |
| PRESSURE-TICK-014 | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureMistake` — `if <= 0` (fragility 0 → AutoFill trigger) | M |
| PRESSURE-TICK-016 | `Assets/Scripts/Run/RunDirector.cs` | `AutoFillCrumbledCell` — threat loop (AutoFill silently resolves threats) | M |
| PRESSURE-TICK-018 | `Assets/Scripts/Run/RunDirector.cs` | `AutoFillCrumbledCell` — `HauntedCell = (-1,-1)` | L |
| PRESSURE-TICK-019 | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty(int damage)` — all HP via this overload | H |
| PRESSURE-UI-003 | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — badge colour calc; needs `InitialPlacements` check | M |
| PRESSURE-UI-006 | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — HauntedCell purple pulse block | M |
| PRESSURE-UI-007 | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — CrumblingCells brown tint block | M |
| PRESSURE-SAVE-001 | `Assets/Scripts/Core/RuntimeModels.cs` | `LevelState` — all pressure fields marked `[NonSerialized]` | L |
| PRESSURE-SAVE-002 | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` call inside `StartLevel` | L |

---

## CURSE — Curse System

| ID | File | Method / Location | Priority |
|----|------|-------------------|----------|
| CURSE-ACQUIRE-001 | `Assets/Scripts/Run/RunEventService.cs` | `ResolveChoice` — event offers curses (Ink Peddler, Tempting Shrine) | H |
| CURSE-ACQUIRE-002 | `Assets/Scripts/Items/ItemService.cs` | `UseItem` — 4 curse-bearing items call `ApplyCurse` | H |
| CURSE-ACQUIRE-003 | `Assets/Scripts/Items/ItemService.cs` | Item catalog / `GetItemDescription` — curse cost in tooltip | M |
| CURSE-ACQUIRE-004 | `Assets/Scripts/Run/RunDirector.cs` | `AcceptRestCurseRemoval` — rest node 4-way when curses active | H |
| CURSE-ACQUIRE-005 | `Assets/Scripts/Run/RunDirector.cs` | Rest handlers — rest node 3-way when no curses active | M |
| CURSE-INT-001 | `Assets/Scripts/Run/RunEventService.cs` | `ResolveChoice` — calls `ApplyCurse` | H |
| CURSE-INT-002 | `Assets/Scripts/Run/RunDirector.cs` | `PlaceNumber` — phantom_pain 2×HP; trembling_hand 1HP first placement | H |
| CURSE-INT-003 | `Assets/Scripts/Run/RunDirector.cs` | `CompleteLevelAndGrantRewards` — misfortune slot−1; restless_inventory drain | H |
| CURSE-INT-004 | `Assets/Scripts/Run/RunDirector.cs` | `AdvanceToNextFloor` — fraying_thread −1 MaxHP | H |
| CURSE-INT-006 | `Assets/Scripts/Run/RunDirector.cs` | Board setup / `StartLevel` — inkblot marks 3 cells as fogged | M |
| CURSE-INT-007 | `Assets/Scripts/Items/ItemService.cs` | `UseItem` — 4 curse-bearing items call `ApplyCurse` | M |
| CURSE-INT-010 | `Assets/Scripts/Run/RunDirector.cs` | `AcceptRestCurseRemoval` — rest cleanse wired | M |
| CURSE-INT-011 | `Assets/Scripts/UI/InRunController.cs` | `CursePanelController` render active curses | L |
| CURSE-INT-012 | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState.ActiveCurseIds` — serialized via RunState | L |
| CURSE-STACK-003 | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState.ActiveCurseIds` — curses persist across all 5 floors | M |

---

## GEN — Sudoku Generation Pipeline

| ID | File | Method / Location | Priority |
|----|------|-------------------|----------|
| GEN-REGION-001 | `Assets/Scripts/Core/SudokuGenerator.cs` | `BuildRegionMap` — 4 variants | H |
| GEN-REGION-002 | `Assets/Scripts/Core/SudokuGenerator.cs` | Template methods (each region = `size` cells, contiguous) | M |
| GEN-REGION-003 | `Assets/Scripts/Run/RunDirector.cs` | `BuildLevelConfig` — `AllowIrregularPuzzles=false` restricts to variants 0–1 | H |
| GEN-SOLVE-001 | `Assets/Scripts/Core/SudokuGenerator.cs` | `GenerateSolvedBoard` / `FillBoard` — backtracking + MRV + seeded shuffle | H |
| GEN-REMOVE-001 | `Assets/Scripts/Core/SudokuGenerator.cs` | `RemoveCells` | H |
| GEN-REMOVE-002 | `Assets/Scripts/Core/StarDensityService.cs` | `MissingPercentForStars` formula | M |
| GEN-REMOVE-003 | `Assets/Scripts/Core/StarDensityService.cs` | 7★ case (removes all cells, requires ≥1 modifier) | M |
| GEN-REMOVE-004 | `Assets/Scripts/Core/SudokuGenerator.cs` | `RemoveCells` — 7★ clamp bypass | M |
| GEN-MOD-001 | `Assets/Scripts/Core/ModifierGeometryGenerator.cs` | `Generate` entry point | H |
| GEN-VALID-001 | `Assets/Scripts/Core/SudokuBoard.cs` | `IsCorrectAt`, `IsComplete` — placement valid if satisfies all constraints | H |
| GEN-VALID-002 | `Assets/Scripts/Run/RunDirector.cs` | `PlaceNumber` → `IsMoveValid` → `SudokuConstraintEngine.ValidateAll` | H |

---

## MAP — Path System & Garden Overview

| ID | File | Method / Location | Priority |
|----|------|-------------------|----------|
| MAP-STRUCT-001 | `Assets/Scripts/Run/RunDirector.cs` | Floor management (5 floors per run) | H |
| MAP-STRUCT-002 | `Assets/Scripts/Run/RunGraphService.cs` | Graph generation — Shared Start → Calm/Risk → Boss Gate | H |
| MAP-ROUTE-001 | `Assets/Scripts/Run/RunGraphService.cs` | `BuildCalmRoute` | M |
| MAP-ROUTE-002 | `Assets/Scripts/Run/RunGraphService.cs` | `BuildRiskRoute` | M |
| MAP-CROSS-001 | `Assets/Scripts/Run/RunGraphService.cs` | Cross-link logic (exactly one per floor) | M |

---

## META — Meta Progression

| ID | File | Method / Location | Priority |
|----|------|-------------------|----------|
| META-UNLOCK-001 | `Assets/Scripts/Core/RuntimeModels.cs` | `ClassUnlockProgress` — 8 classes with cumulative conditions | H |
| META-UNLOCK-002 | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` — updates counters + `EvaluateUnlocks` | H |
| META-UNLOCK-003 | `Assets/Scripts/UI/InRunController.cs` | Class select panel — unlocked class display | M |
| META-UNLOCK-004 | `Assets/Scripts/UI/InRunController.cs` | Class select panel — locked class greyed + unlock text | M |

---

## SAVE — Save / Load

| ID | File | Method / Location | Priority |
|----|------|-------------------|----------|
| SAVE-SLOT-002 | `Assets/Scripts/Save/ProfileService.cs` | `SaveProfileService` — cross-slot manager, `ActiveSlot` in `PlayerPrefs` | M |
| SAVE-SLOT-003 | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `ApplySlotChange` — re-wires save services after slot switch | M |
| SAVE-SERIAL-001 | `Assets/Scripts/Core/SaveModels.cs` | Data model classes — all `[Serializable]`, uses `JsonUtility` | M |

---

## INPUT — Input & Profiles

| ID | File | Method / Location | Priority |
|----|------|-------------------|----------|
| INPUT-PROFILE-001 | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `Start` — shows `ProfileSelect` if no files exist | M |
| INPUT-PROFILE-002 | `Assets/Scripts/UI/MainMenuController.cs` | `SelectProfileSlot`, `DeleteProfileSlot`, `RefreshProfileSelectCards` | M |
| INPUT-REMAP-004 | `Assets/Scripts/UI/KeybindingsPanelController.cs` | Class definition — manages listen/capture/save rebind flow | M |

---

## ACCESS — Accessibility

| ID | File | Method / Location | Priority |
|----|------|-------------------|----------|
| ACCESS-COLOR-002 | `Assets/Scripts/UI/InRunController.cs` | `OptionsController.SetColorblindMode/SetHighContrast` → `NotifyAccessibilityChanged` | M |
| ACCESS-MOTION-001 | `Assets/Scripts/UI/InRunController.cs` | Screen shake helper / wrong-answer path — skipped when `ReduceMotion` | M |
| ACCESS-OVERLAY-001 | `Assets/Scripts/UI/BoardViewController.cs` | Overlay render — `GetOverlayAlpha` ×1.4, `GetLineWidthMultiplier` ×1.5 | M |
| ACCESS-PARTICLE-001 | `Assets/Scripts/UI/InRunController.cs` | `AmbientParticleController.RefreshSettings` — particles stopped when `ReduceMotion` | M |

---

## XP — Class XP Progression

| ID | File | Method / Location | Priority |
|----|------|-------------------|----------|
| XP-TILE-002 | `Assets/Scripts/Economy/XpTable.cs` | `BaseXpForBoardSize` — already annotated ✅ |
| XP-TILE-003 | `Assets/Scripts/Economy/XpTable.cs` | `StarMultiplier` — already annotated ✅ |
| XP-TILE-004 | `Assets/Scripts/Economy/XpTable.cs` | `BossMultiplier` const — already annotated ✅ |
| XP-CURVE-001 | `Assets/Scripts/Economy/XpTable.cs` | `XpToNextLevel` — already annotated ✅ |
| XP-CURVE-002 | `Assets/Scripts/Economy/XpTable.cs` | `DeriveLevel` — already annotated ✅ |

> XP annotations are complete (all annotated in XpTable.cs in Bucket A pass).

---

## Summary by System

| System | Total IDs | Annotated | Missing | % Done |
|--------|:---------:|:---------:|:-------:|:------:|
| PRESSURE | 30 | 17 | 13 | 57% |
| CURSE | 22 | 7 | 15 | 32% |
| GEN | 11 | 0 | 11 | 0% |
| MAP | 14 | 0 | 5 (key) | — |
| META | 16 | 10 | 4 | 63% |
| SAVE | 10 | 7 | 3 | 70% |
| INPUT | 6 | 3 | 3 | 50% |
| ACCESS | 5 | 1 | 4 | 20% |
| XP | 8 | 8 | 0 | ✅ 100% |
| BOSS | 8 | 8 | 0 | ✅ 100% |
| DEBUFF | 13 | 13 | 0 | ✅ 100% |
| CURSE POOL | 15 | 15 | 0 | ✅ 100% |
| ITEM/RELIC | 6 | 6 | 0 | ✅ 100% |
| ECON | 4 | 4 | 0 | ✅ 100% |
| TUTO-BASICS | 10 | 10 | 0 | ✅ 100% |

### Recommended Next Bucket (Bucket B)

High-ROI targets — small files, clean methods, high traceability value:

1. **GEN** (11 IDs) — `SudokuGenerator.cs`, `StarDensityService.cs`, `ModifierGeometryGenerator.cs` — zero annotations today
2. **CURSE-ACQUIRE + CURSE-INT** (14 IDs) — `RunEventService.cs`, `RunDirector.cs` — high integration value
3. **MAP** (5 key IDs) — `RunGraphService.cs` — single file, all critical path
4. **ACCESS** (4 IDs) — `InRunController.cs`, `BoardViewController.cs` — already have `ACCESS-COLOR-001`
