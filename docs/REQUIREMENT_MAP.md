# Requirement Traceability Map — All Systems

**Updated:** 2026-10-14 (Steam build v0.1.5; release 1.1 in the review documents)

Legend: ✅ Implemented and verified | ⚠️ Partial / conflicting | ❌ Not implemented | ◻️ Code present, not verified (no tagged test or E2E check yet) | 🗄️ Deprecated / descoped | 🆕 New (spec only, no code yet)

**Evidence rule (SPEC-PROC-003, 1.1):** ✅ only when the Evidence column names a passing test that carries the ID (`[Requirement("ID")]` attribute or `[REQ: ID]` tag in `Assets/Scripts/Tests`) or an E2E/diagnostic check from `docs/review/E2E_TEST_REPORT.md` (`D##`, `E##`, `F##`, `B##`). Statuses are synced from `docs/review/REQUIREMENT_TRACEABILITY.md`; `RequirementTraceabilityTests` rejects test tags whose ID is not documented.

---

## Index

| System Prefix | Spec File | Status |
|--------------|-----------|--------|
| [PRESSURE](#pressure-mechanics) | `docs/PressureMechanicsSpec.md` | ✅ 19 · ◻️ 35 |
| [DEBUFF](#boss-debuffs) | `docs/BossDebuffEffects.md` | ✅ 33 · ◻️ 7 · 🗄️ 4 |
| [CURSE](#curse-system) | `docs/CurseSystem.md` | ✅ 10 · ◻️ 28 |
| [META](#meta-progression) | `docs/MetaProgressionSystem.md` | ✅ 15 · ⚠️ 1 · ❌ 3 |
| [ZENMODE](#endless-zen-mode) | `docs/EndlessZenMode.md` | ✅ 5 · ◻️ 4 |
| [TRIAL](#spirit-trials-mode) | `docs/SpiritTrialsMode.md` | ✅ 4 · ◻️ 9 |
| [TUTO-BASICS](#sudoku-basics-tutorial) | `docs/FirstTimePlayTutorial.md` | ✅ 5 · ⚠️ 1 · ◻️ 4 |
| [GEN](#sudoku-generation-pipeline) | `docs/SudokuGenerationPipeline.md` | ✅ 12 |
| [MAP](#path-system--garden-overview) | `docs/PathSystem_GardenOverview.md` | ✅ 10 · ◻️ 4 |
| [BOSS](#boss-mechanics) | `docs/BossMechanicsSystem.md` | ✅ 8 |
| [XP](#class-xp-progression) | `docs/ClassXpProgressionSystem.md` | ✅ 8 |
| [ITEM](#items--relics) | `docs/ItemsAndRelicsSystem.md` | ✅ 4 · ◻️ 1 · 🗄️ 1 |
| [SAVE](#save--load) | `docs/SaveLoadArchitecture.md` | ✅ 8 · ◻️ 2 · 🗄️ 1 |
| [INPUT](#input--profiles) | `docs/InputAndProfilesSpec.md` | ✅ 3 · ◻️ 3 |
| [ACCESS](#accessibility) | `docs/AccessibilitySpec.md` | ◻️ 5 |
| [ECON](#gold-economy) | `docs/GoldEconomySystem.md` | ✅ 3 |
| [SEASON](#seasonal-challenge) | `docs/SeasonalChallengeSpec.md` | ✅ 5 · ◻️ 1 |
| [DAILY](#daily-goal-system) | `docs/DailyGoalSystem.md` | ✅ 2 · ◻️ 3 |
| [CLASS](#class-progression-unlocks) | `docs/ClassProgressionUnlocks.md` | ✅ 1 · ◻️ 3 |
| [DISPLAY](#display-modes) | `docs/GameDesignSpec.md` | ✅ 7 |
| [UI-OPT](#options-panels) | `docs/GameDesignSpec.md` | ✅ 3 |
| [RUN-WEEK](#shared-challenges) | `docs/GameDesignSpec.md` | ✅ 2 |
| [CREDITS](#credits) | `docs/GameDesignSpec.md` | ✅ 1 |
| [UI-STYLE](#in-run-visual-style) | `docs/design/layout-proposals.md` | ✅ 4 · ◻️ 2 |
| [LOC](#localization) | `docs/localization-catalog.md` | ✅ 3 |
| [AUDIO](#audio) | `docs/AudioVisualDirection.md` | ✅ 3 · ◻️ 4 |
| [REVIEW](#review-created-requirements) | `docs/review/REQUIREMENT_TRACEABILITY.md` | ✅ 53 · ⚠️ 6 · ◻️ 6 |
| [HARMONY](#harmony-difficulty) | `docs/HarmonyDifficultySystem.md` | ✅ 14 · ◻️ 18 |

---

## Pressure Mechanics

**Spec:** `docs/PressureMechanicsSpec.md` v0.8

### Rolling & Activation

| ID | Description | File | Method / Location | Evidence | Status |
| ---- | ------------- | ------ | ------------------- | ---------- | -------- |
| PRESSURE-ROLL-001 | No floor 1 or tutorial mode | `Assets/Scripts/Run/RunDirector.cs` | `RollPressureMechanic` — first two guards | PressureRollTests | ✅ |
| PRESSURE-ROLL-002 | Elite/pre-boss/boss nodes only | `Assets/Scripts/Run/RunDirector.cs` | `RollPressureMechanic` — node type guard | PressureRollTests | ✅ |
| PRESSURE-ROLL-002 | `LevelConfig` node flags | `Assets/Scripts/Core/RuntimeModels.cs` | `LevelConfig.IsElite`, `LevelConfig.IsPreBoss` | PressureRollTests | ✅ |
| PRESSURE-ROLL-003 | Floor probability table | `Assets/Scripts/Run/RunDirector.cs` | `RollPressureMechanic` — `chance` switch | PressureRollTests | ✅ |
| PRESSURE-ROLL-004 | One mechanic per puzzle max | `Assets/Scripts/Run/RunDirector.cs` | `RollPressureMechanic` — `Contains` dedup guard | E2E/diagnostic D05, D18 | ✅ |
| PRESSURE-ROLL-004 | Modifier data registration | `Assets/Scripts/Boss/BossService.cs` | `ModifierData` — IDs 103–106 | E2E/diagnostic D05, D18 | ✅ |
| PRESSURE-ROLL-005 | PressureWave from floor 3+ | `Assets/Scripts/Run/RunDirector.cs` | `RollPressureMechanic` — `if (floor >= 3)` | PressureRollTests | ✅ |

### Initialization

| ID | Description | File | Method / Location | Evidence | Status |
| ---- | ------------- | ------ | ------------------- | ---------- | -------- |
| PRESSURE-INIT-001 | CountdownFill scope/cells/counter init | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` — CountdownFill case | — | ◻️ |
| PRESSURE-INIT-002 | HauntedCell intensity-keyed selection | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` — HauntedCell case; `SelectHauntedCell` | — | ◻️ |
| PRESSURE-INIT-003 | CrumblingRegion one-region fragility init | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` — CrumblingRegion case; `SelectCrumblingRegion` | — | ◻️ |
| PRESSURE-INIT-004 | PressureWave interval/counter init | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` — PressureWave case | — | ◻️ |
| PRESSURE-INIT-005 | Chain length 3–4; fallback to SingleCell | `Assets/Scripts/Run/RunDirector.cs` | `SelectThreatCells` — Chain case | — | ◻️ |
| PRESSURE-INIT-006 | PresolvedCells excluded | `Assets/Scripts/Run/RunDirector.cs` | `SelectThreatCells`, `SelectHauntedCell`, `SpawnWaveThreat` | — | ◻️ |
| PRESSURE-INIT-007 | counting_shadow curse threat | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` — curse block | — | ◻️ |
| PRESSURE-INIT-008 | hollow_eye curse haunted cell | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` — curse block | — | ◻️ |

### Core Tick Logic

| ID | Description | File | Method / Location | Evidence | Status |
| ---- | ------------- | ------ | ------------------- | ---------- | -------- |
| PRESSURE-TICK-001 | Correct placement decrements all counters | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — counter loop step 1 | — | ◻️ |
| PRESSURE-TICK-002 | Wrong placement also decrements counters | `Assets/Scripts/Run/RunDirector.cs` | `PlaceNumber` — incorrect path after `Mistakes++` | — | ◻️ |
| PRESSURE-TICK-003 | Success when last cell placed | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — success check step 2 | — | ◻️ |
| PRESSURE-TICK-004 | Chain success +1 HP | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — `if (threat.IsChain)` | — | ◻️ |
| PRESSURE-TICK-005 | Expiry 2 HP per unfilled cell | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — expiry branch | — | ◻️ |
| PRESSURE-TICK-006 | DoublePenalty doubles expiry | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — `penaltyPer` calculation | — | ◻️ |
| PRESSURE-TICK-007 | Wave counter decrement + reset + spawn | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — step 4 wave block; `SpawnWaveThreat` | — | ◻️ |
| PRESSURE-TICK-008 | Wave cap at 3 concurrent | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — `WaveSpawnCount < 3` | — | ◻️ |
| PRESSURE-TICK-009 | WaveSpawnCount decrements on removal | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — success and expiry branches | — | ◻️ |
| PRESSURE-TICK-010 | HauntedCell extra HP on mistake | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureMistake` — HauntedCell block | — | ◻️ |
| PRESSURE-TICK-011 | HauntedCell DoublePenalty stacking | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureMistake` — `extraDmg` calc | — | ◻️ |
| PRESSURE-TICK-012 | CrumblingRegion all-cell erosion | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureMistake` — CrumblingCells loop | — | ◻️ |
| PRESSURE-TICK-013 | CrumblingRegion VeryHigh first-cell only | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureMistake` — needs `CrumblingVeryHigh` branch | — | ◻️ |
| PRESSURE-TICK-014 | Fragility 0 → AutoFill trigger | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureMistake` — `if <= 0` | — | ◻️ |
| PRESSURE-TICK-015 | AutoFill places solution, costs HP | `Assets/Scripts/Run/RunDirector.cs` | `AutoFillCrumbledCell` | — | ◻️ |
| PRESSURE-TICK-016 | AutoFill silently resolves threats | `Assets/Scripts/Run/RunDirector.cs` | `AutoFillCrumbledCell` — threat loop | — | ◻️ |
| PRESSURE-TICK-017 | AutoFill does not tick counters | `Assets/Scripts/Run/RunDirector.cs` | `AutoFillCrumbledCell` — no `TickPressureThreats` call | — | ◻️ |
| PRESSURE-TICK-018 | AutoFill clears haunting | `Assets/Scripts/Run/RunDirector.cs` | `AutoFillCrumbledCell` — `HauntedCell = (-1,-1)` | — | ◻️ |
| PRESSURE-TICK-019 | All HP via `ApplyMistakePenalty` | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty(int damage)` | — | ◻️ |

### UI / Rendering

| ID | Description | File | Method / Location | Evidence | Status |
| ---- | ------------- | ------ | ------------------- | ---------- | -------- |
| PRESSURE-UI-001 | No visuals on tutorial (RunNumber==0) | `Assets/Scripts/UI/InRunController.cs` | `BuildPressureRenderData` — RunNumber guard | InRunUiPlayModeTests | ✅ |
| PRESSURE-UI-002 | Count badge per threatened cell | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — `badgeText` / `cell.BadgeLabel` block | InRunUiPlayModeTests | ✅ |
| PRESSURE-UI-003 | Badge colour by ratio (>50%/25–50%/<25%) | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — badge colour calc; needs `InitialPlacements` | InRunUiPlayModeTests | ✅ |
| PRESSURE-UI-004 | Expiry red-flash + shake animation | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — threat-removal detection + animation | InRunUiPlayModeTests | ✅ |
| PRESSURE-UI-005 | Chain success gold-flash | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — `ChainSuccessCells` colour override | InRunUiPlayModeTests | ✅ |
| PRESSURE-UI-006 | Haunted cell purple pulse | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — HauntedCell pulse block | InRunUiPlayModeTests | ✅ |
| PRESSURE-UI-007 | Crumbling brown crack tint | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — CrumblingCells tint block | InRunUiPlayModeTests | ✅ |
| PRESSURE-UI-008 | Auto-fill green flash | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — `CrumbledFlashCells` colour override | InRunUiPlayModeTests | ✅ |
| PRESSURE-UI-009 | Wave ripple animation | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — wave ripple overlay; needs `WaveRipplePending` | InRunUiPlayModeTests | ✅ |
| PRESSURE-UI-010 | Region border strip (Row/Col/Box) | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — new `DrawRegionBorderStrip` helper | InRunUiPlayModeTests | ✅ |
| PRESSURE-UI-011 | Chain animated line | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — new `DrawChainThreatLine` helper | InRunUiPlayModeTests | ✅ |

### Persistence

| ID | Description | File | Method / Location | Evidence | Status |
| ---- | ------------- | ------ | ------------------- | ---------- | -------- |
| PRESSURE-SAVE-001 | All pressure fields `[NonSerialized]` | `Assets/Scripts/Core/RuntimeModels.cs` | `LevelState` — all pressure fields | — | ◻️ |
| PRESSURE-SAVE-002 | Resume reinitialises pressure from seed | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` called in `StartLevel` | ResumeMidPuzzleTests | ✅ |
| PRESSURE-SAVE-003 | Comment documents non-persistence | `Assets/Scripts/Save/RunAutoSaveCoordinator.cs` | Line 51 — NOTE comment | — | ◻️ |

### Edge Case Guards

| ID | Description | File | Method / Location | Evidence | Status |
| ---- | ------------- | ------ | ------------------- | ---------- | -------- |
| PRESSURE-EDGE-001 | Chain <3 cells → SingleCell fallback | `Assets/Scripts/Run/RunDirector.cs` | `SelectThreatCells` — Chain case | — | ◻️ |
| PRESSURE-EDGE-002 | CrumblingRegion no-qualifying-region guard | `Assets/Scripts/Run/RunDirector.cs` | `SelectCrumblingRegion` + `InitializePressureMechanics` | — | ◻️ |
| PRESSURE-EDGE-003 | Wave spawn skips when no free cells | `Assets/Scripts/Run/RunDirector.cs` | `SpawnWaveThreat` — `available.Count == 0` | — | ◻️ |
| PRESSURE-EDGE-004 | Haunted+Crumbling same cell | `Assets/Scripts/Run/RunDirector.cs` | `AutoFillCrumbledCell` — HauntedCell clear | — | ◻️ |
| PRESSURE-EDGE-005 | counting_shadow + modifier coexist | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` — additive curse block | — | ◻️ |
| PRESSURE-EDGE-006 | hollow_eye defers to modifier | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` — `HauntedCell == (-1,-1)` guard | — | ◻️ |

---

## Boss Debuffs

**Spec:** `docs/BossDebuffEffects.md` v1.4

### Debuff Definitions

| ID | Debuff | BossModifierId | Description | Evidence | Status |
| ---- | -------- | :---: | ------------- | ---------- | -------- |
| DEBUFF-DEF-001 | RowWipe | 94 | On mistake: clears player-placed digits in same row | BossDebuffTests | ✅ |
| DEBUFF-DEF-002 | ColWipe | 95 | On mistake: clears player-placed digits in same column | BossDebuffTests | ✅ |
| DEBUFF-DEF-003 | BoxWipe | 99 | On mistake: clears player-placed digits in same region | BossDebuffTests | ✅ |
| DEBUFF-DEF-004 | CrossWipe | 100 | On mistake: clears row AND column (combined) | BossDebuffTests | ✅ |
| DEBUFF-DEF-005 | RadiusWipe | 107 | On mistake: clears within 2-cell Chebyshev radius | BossDebuffTests | ✅ |
| DEBUFF-DEF-006 | PencilPoison | — | On mistake: adds wrong pencil marks to row | — | 🗄️ Descoped (FUTURE-010) |
| DEBUFF-DEF-007 | PencilBlind | 98 | On mistake: removes all pencil marks in row+column | BossDebuffTests | ✅ |
| DEBUFF-DEF-008 | PencilScramble | 108 | On mistake: scrambles all pencil marks board-wide | BossDebuffTests | ✅ |
| DEBUFF-DEF-009 | FogPulse | — | On mistake: adds fog ring for 3 moves | — | 🗄️ Descoped (FUTURE-010) |
| DEBUFF-DEF-010 | NumberBlind | — | On mistake: hides placed digit board-wide for 2 moves | — | 🗄️ Descoped (FUTURE-010) |
| DEBUFF-DEF-011 | CellBlur | 111 | On mistake: hides digit labels in 3×3 area briefly | BossDebuffTests | ✅ |
| DEBUFF-DEF-012 | DoublePenalty | 96 | Wrong placements deal 2 HP instead of 1 | BossDebuffTests | ✅ |
| DEBUFF-DEF-013 | PencilDrain | 101 | Each wrong placement costs 1 extra pencil charge | BossDebuffTests | ✅ |
| DEBUFF-DEF-014 | GoldFine | 102 | Deducts 5 gold per mistake (clamped to 0) | BossDebuffTests | ✅ |
| DEBUFF-DEF-015 | RevealCost | 110 | Blocks next item use for 2 moves after mistake | BossDebuffTests | ✅ |
| DEBUFF-DEF-016 | DigitShift | — | After mistake: swap solution values between cells | — | 🗄️ Descoped (FUTURE-010) |
| DEBUFF-DEF-017 | GivenReveal | 109 | After mistake: convert 1–3 correct digits to given | BossDebuffTests | ✅ |
| DEBUFF-DEF-018 | CellLock | 97 | After mistake: wrong cell un-editable for 3 placements | BossDebuffTests | ✅ |

### Delivery (v1.4)

| ID | Rule | File | Method | Evidence | Status |
| ---- | ------ | ------ | -------- | ---------- | -------- |
| DEBUFF-DELIV-001 | From Floor 3 every Garden Run boss carries exactly one debuff; floors 1–2, normal puzzles and other modes carry none | `Assets/Scripts/Run/RunDirector.Choices.cs` | `RunDirector.GetBossDebuffForCurrentFloor` | BossDebuffTests | ✅ |
| DEBUFF-DELIV-002 | The debuff depends only on run seed + floor (gate, puzzle and resume agree) | `Assets/Scripts/Run/RunDirector.Choices.cs` | `RunDirector.GetBossDebuffForCurrentFloor` (seed + floor hash) | BossDebuffTests | ✅ |
| DEBUFF-DELIV-003 | Pool: 11 debuffs; CrossWipe and PencilScramble only at VeryHigh; GivenReveal never delivered | `Assets/Scripts/Boss/BossService.cs` | `BossService.GetBossDebuffPool`, `PickBossDebuff` | BossDebuffTests | ✅ |
| DEBUFF-DELIV-004 | Boss gate shows intensity and the debuff; View Modifiers and the legend list it | `Assets/Scripts/UI/BossGateViewController.cs` | `BossGateViewController`, `InRunController.RunMenu` | InRunUiPlayModeTests | ✅ |

### Integration Hooks

| ID | Hook | File | Method | Evidence | Status |
| ---- | ------ | ------ | -------- | ---------- | -------- |
| DEBUFF-HOOK-001 | `ApplyMistakePenalty(row,col)` overload dispatches debuffs | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty(int,int)` | BossDebuffTests | ✅ |
| DEBUFF-HOOK-002 | DoublePenalty sets damage=2 | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty` | BossDebuffTests | ✅ |
| DEBUFF-HOOK-003 | RowWipe → `ClearBoardRow(row)` | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty` | BossDebuffTests | ✅ |
| DEBUFF-HOOK-004 | ColWipe → `ClearBoardCol(col)` | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty` | BossDebuffTests | ✅ |
| DEBUFF-HOOK-005 | PencilBlind → `ClearPencilRowCol(row,col)` | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty` | BossDebuffTests | ✅ |
| DEBUFF-HOOK-006 | CellLock → `LockCell(row,col)` into `LockedCells` | `Assets/Scripts/Run/RunDirector.cs` | `LockCell` | BossDebuffTests | ✅ |
| DEBUFF-HOOK-007 | `TickLockedCells()` after every correct placement | `Assets/Scripts/Run/RunDirector.cs` | `TickLockedCells` | BossDebuffTests | ✅ |
| DEBUFF-HOOK-008 | `GetLockedCells()` for input blocking + board tint | `Assets/Scripts/Run/RunDirector.cs` | `GetLockedCells` | — | ◻️ |
| DEBUFF-HOOK-009 | `LevelState.LockedCells` `[NonSerialized]` dict | `Assets/Scripts/Core/RuntimeModels.cs` | `LevelState` | — | ◻️ |
| DEBUFF-HOOK-010 | `BossModifierId` enum 94–102 | `Assets/Scripts/Core/GameEnums.cs` | `BossModifierId` | — | ◻️ |
| DEBUFF-HOOK-011 | `BossService` ModifierData IDs 94–102; IDs ≥94 excluded from pool | `Assets/Scripts/Boss/BossService.cs` | `ModifierData`, `BuildEligiblePool` | E2E/diagnostic D04, D05, D18 | ✅ |
| DEBUFF-HOOK-012 | `BoardViewController` CellLocked tint (deep magenta) | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` | — | ◻️ |
| DEBUFF-HOOK-013 | `InRunController` blocks input on locked cells | `Assets/Scripts/UI/InRunController.cs` | Input handlers | — | ◻️ |
| DEBUFF-HOOK-014 | RadiusWipe → `ClearBoardRadius(row,col)` | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty(int,int)` | BossDebuffTests | ✅ |
| DEBUFF-HOOK-015 | PencilScramble → `ScramblePencilMarks()` | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty(int,int)` | BossDebuffTests | ✅ |
| DEBUFF-HOOK-016 | GivenReveal → `ConvertCellsToGiven(2)` | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty(int,int)` | BossDebuffTests | ✅ |
| DEBUFF-HOOK-017 | RevealCost → `ItemUseBlockedMoves = max(current, 2)` on mistake | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty(int,int)` | BossDebuffTests | ✅ |
| DEBUFF-HOOK-018 | CellBlur → `ApplyCellBlur(row,col)` | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty(int,int)` | BossDebuffTests | ✅ |
| DEBUFF-HOOK-019 | `TickDebuffTimers()` after every correct placement — decrements RevealCost + CellBlur counters | `Assets/Scripts/Run/RunDirector.cs` | `PlaceNumber` | BossDebuffTests | ✅ |
| DEBUFF-HOOK-020 | `BossModifierId` enum 107–111 (Batch 2 debuffs) | `Assets/Scripts/Core/GameEnums.cs` | `BossModifierId` | — | ◻️ |
| DEBUFF-HOOK-021 | `BossService` ModifierData IDs 107–111; all excluded from pool via ≥94 guard | `Assets/Scripts/Boss/BossService.cs` | `ModifierData`, `BuildEligiblePool` | E2E/diagnostic D04, D05, D18 | ✅ |
| DEBUFF-HOOK-022 | `LevelState.ItemUseBlockedMoves`, `CellBlurCells`, `CellBlurMovesLeft` `[NonSerialized]` | `Assets/Scripts/Core/RuntimeModels.cs` | `LevelState` | — | ◻️ |
---

## Curse System

**Spec:** `docs/CurseSystem.md` v1.2

### Acquisition

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| CURSE-ACQUIRE-001 | Run events offer curses as tradeoffs (Ink Peddler, Tempting Shrine) | `Assets/Scripts/Run/RunEventService.cs` | `ResolveChoice` | — | ◻️ |
| CURSE-ACQUIRE-002 | 4 curse-bearing items apply curse on use | `Assets/Scripts/Items/ItemService.cs` | `UseItem` | — | ◻️ |
| CURSE-ACQUIRE-003 | Curse items labelled with curse cost in tooltip | `Assets/Scripts/Items/ItemService.cs` | Item catalog | — | ◻️ |
| CURSE-ACQUIRE-004 | Rest node 4-way when curses active (adds Cleanse option) | `Assets/Scripts/Run/RunDirector.cs` | `AcceptRestCurseRemoval` | — | ◻️ |
| CURSE-ACQUIRE-005 | Rest node 3-way when no curses active | `Assets/Scripts/Run/RunDirector.cs` | Rest handlers | — | ◻️ |

### Curse Pool

| ID | Curse ID | Name | Severity | Evidence | Status |
| ---- | ---------- | ------ | ---------- | ---------- | -------- |
| CURSE-POOL-001 | `hollow_pencil` | Hollow Pencil | Mild | — | ◻️ |
| CURSE-POOL-002 | `fog_of_memory` | Fog of Memory | Mild | — | ◻️ |
| CURSE-POOL-003 | `misfortune` | Misfortune | Mild | — | ◻️ |
| CURSE-POOL-004 | `bad_luck` | Bad Luck | Mild | — | ◻️ |
| CURSE-POOL-005 | `price_gouge` | Price Gouge | Mild | — | ◻️ |
| CURSE-POOL-006 | `inkblot` | Inkblot | Medium | — | ◻️ |
| CURSE-POOL-007 | `crumbling_focus` | Crumbling Focus | Medium | CurseEffectTests | ✅ |
| CURSE-POOL-008 | `sealed_eyes` | Sealed Eyes | Medium | — | ◻️ |
| CURSE-POOL-009 | `blurred_sight` | Blurred Sight | Medium | — | ◻️ |
| CURSE-POOL-010 | `restless_inventory` | Restless Inventory | Medium | — | ◻️ |
| CURSE-POOL-011 | `phantom_pain` | Phantom Pain | Nasty | — | ◻️ |
| CURSE-POOL-012 | `weight_of_stone` | Weight of Stone | Nasty | CurseEffectTests | ✅ |
| CURSE-POOL-013 | `trembling_hand` | Trembling Hand | Nasty | — | ◻️ |
| CURSE-POOL-014 | `fraying_thread` | Fraying Thread | Nasty | — | ◻️ |
| CURSE-POOL-015 | `double_or_nothing` | Double or Nothing | Nasty | — | ◻️ |

### Stacking Rules

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| CURSE-STACK-001 | Unlimited curse stacking (no cap) | `Assets/Scripts/Run/CurseService.cs` | `ApplyCurse` | — | ◻️ |
| CURSE-STACK-002 | No duplicates — re-roll until different | `Assets/Scripts/Run/CurseService.cs` | `RollCurse` | CurseEffectTests | ✅ |
| CURSE-STACK-003 | Curses persist across all 5 floors | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState.ActiveCurseIds` | — | ◻️ |

### Removal

| ID | Method | File | Evidence | Status |
| ---- | -------- | ------ | ---------- | -------- |
| CURSE-REMOVE-001 | Rest node "Cleanse a Curse" removes oldest | `Assets/Scripts/Run/RunDirector.cs` | CurseEffectTests | ✅ |
| CURSE-REMOVE-002 | Error-free puzzle: 25% chance to auto-clear oldest | `Assets/Scripts/Run/CurseService.cs` | — | ◻️ |
| CURSE-REMOVE-005 | GardenMonk increases auto-clear to 50% | `Assets/Scripts/Run/CurseService.cs` | — | ◻️ |

### Integration Points

| ID | Location | File | Evidence | Status |
| ---- | ---------- | ------ | ---------- | -------- |
| CURSE-INT-001 | `RunEventService.ResolveChoice()` calls `ApplyCurse` | `Assets/Scripts/Run/RunEventService.cs` | — | ◻️ |
| CURSE-INT-002 | `RunDirector.PlaceNumber()`: phantom_pain 2×HP; trembling_hand 1HP first placement | `Assets/Scripts/Run/RunDirector.cs` | CurseEffectTests | ✅ |
| CURSE-INT-003 | `CompleteLevelAndGrantRewards()`: misfortune slot−1; restless_inventory drain | `Assets/Scripts/Run/RunDirector.cs` | CurseEffectTests | ✅ |
| CURSE-INT-004 | `AdvanceToNextFloor()`: fraying_thread −1 MaxHP | `Assets/Scripts/Run/RunDirector.cs` | CurseEffectTests | ✅ |
| CURSE-INT-005 | `ShopService.BuildOffers()`: price_gouge ×1.35 | `Assets/Scripts/Economy/RelicService.cs` | CurseEffectTests | ✅ |
| CURSE-INT-006 | Board setup: inkblot marks 3 cells as fogged | `Assets/Scripts/Run/RunDirector.cs` | — | ◻️ |
| CURSE-INT-007 | `ItemService.UseItem()`: 4 curse-bearing items call `ApplyCurse` | `Assets/Scripts/Items/ItemService.cs` | — | ◻️ |
| CURSE-INT-008 | `BuildItemRewardSlots()`: bad_luck +15% Nothing; double_or_nothing 20% re-roll | `Assets/Scripts/UI/InRunController.cs` | CurseEffectTests | ✅ |
| CURSE-INT-009 | blurred_sight: coroutine fades pencilmarks after 1.5s | `Assets/Scripts/UI/InRunController.cs` | — | ◻️ |
| CURSE-INT-010 | Rest cleanse wired via `AcceptRestCurseRemoval()` | `Assets/Scripts/Run/RunDirector.cs` | RestNodeValuesTests | ✅ |
| CURSE-INT-011 | `CursePanelController` renders active curses, expandable | `Assets/Scripts/UI/InRunController.cs` | — | ◻️ |
| CURSE-INT-012 | `ActiveCurseIds` serialized via `RunState` (no extra change) | `Assets/Scripts/Core/RuntimeModels.cs` | — | ◻️ |

---

## Meta Progression

**Spec:** `docs/MetaProgressionSystem.md` v1.4

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| META-UNLOCK-001 | 8 classes with cumulative cross-class unlock conditions | `Assets/Scripts/Core/RuntimeModels.cs` | `ClassUnlockProgress` | EconomyActionTests.CollectedGold_CountsGains_EvenAfterSpending | ✅ |
| META-UNLOCK-002 | `RecordRunAndGetNewUnlocks()` updates counters + `EvaluateUnlocks()` | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | E2E/diagnostic D09 | ✅ |
| META-UNLOCK-003 | Unlocked classes: full stat block + passive + level | `Assets/Scripts/UI/InRunController.cs` | Class select panel | ModifierDiscoveryPersistenceTests | ✅ |
| META-UNLOCK-004 | Locked classes: greyed out + unlock condition text | `Assets/Scripts/UI/InRunController.cs` | Class select panel | — | ⚠️ |
| META-XP-001 | Per-class `TotalXp` stored; level derived via `DeriveLevel()` | `Assets/Scripts/Core/SaveModels.cs` | `ClassGardenProgressEntry` | E2E/diagnostic D08, D09 | ✅ |
| META-XP-002 | Two-phase XP curve L1–15 + L16–40 | `Assets/Scripts/Economy/XpTable.cs` | `XpTable.DeriveLevel` | XpTableTests | ✅ |
| META-XP-003 | Total to L40: 16,860 XP | `Assets/Scripts/Economy/XpTable.cs` | `XpTable.CumulativeXpForLevel(40)` | XpTableTests | ✅ |
| META-XP-004 | XP committed at run end (no mid-run leveling) | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | E2E/diagnostic D09 | ✅ |
| META-PRESTIGE-001 | Max prestige tier 9 per class | `Assets/Scripts/Core/SaveModels.cs` | `ClassGardenProgressEntry.PrestigeTier` | PrestigeTests | ✅ |
| META-PRESTIGE-002 | Prestige requires Level 40 | `Assets/Scripts/Save/ProfileService.cs` | Prestige gate check | PrestigeTests | ✅ |
| META-PRESTIGE-003 | Boss gate: `3 × (PrestigeTier + 1)` boss defeats | `Assets/Scripts/Save/ProfileService.cs` | Prestige gate check | PrestigeTests | ✅ |
| META-PRESTIGE-004 | On prestige: TotalXp=0, PrestigeTier++ | `Assets/Scripts/Save/ProfileService.cs` | `TryPrestige` | PrestigeTests | ✅ |
| META-ASCEND-001 | Ascension is a global layer above per-class prestige | `Assets/Scripts/Core/SaveModels.cs` | `MetaProgressionState.AscensionLevel` | — | ❌ Deferred (FUTURE-009) |
| META-ASCEND-002 | `ApplyAscension`: increments AscensionLevel, +1 MaxStarCap — not built; Ascension deferred to FUTURE-009 (SPEC-CONF-026/036) | — | — | — | ❌ Deferred (FUTURE-009) |
| META-ASCEND-003 | `TryPrestigeReset`: resets AscensionLevel — not built; deferred to FUTURE-009 | — | — | — | ❌ Deferred (FUTURE-009) |
| META-ASCEND-004 | Monthly seed: `(year × 100) + month` | `Assets/Scripts/Run/SeasonalChallengeService.cs` | `BuildSeed` (seed only; no ascension) | SpecAlignmentTests | ✅ |
| META-COMPLETE-001 | `CompletionService.Recalculate()` — 4 checks × 25% | `Assets/Scripts/Meta/CompletionService.cs` | `Recalculate` | CompletionTrackingTests | ✅ |
| META-CODEX-001 | Item & Relic Codex tracks discovery | `Assets/Scripts/Save/ProfileService.cs` | `RecordItemDiscovery`, `RecordRelicDiscovery` | AutosaveMergeTests | ✅ |
| META-CODEX-002 | Entry marked Discovered on first acquisition only | `Assets/Scripts/Save/ProfileService.cs` | `RecordItemDiscovery` | AutosaveMergeTests | ✅ |

---

## Endless Zen Mode

**Spec:** `docs/EndlessZenMode.md` v2.1

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| ZENMODE-UNLOCK-001 | Unlocked when 10 runs are recorded (`TotalRunsStarted ≥ 10`, counted at the run result; SPEC-CONF-006) | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | SpecAlignmentTests | ✅ |
| ZENMODE-SESSION-001 | Session: 9×9 board, no timer, no XP, no items/relics, HP=0 ends session | `Assets/Scripts/Run/RunDirector.cs` | Endless mode setup | GenerationReliabilityTests | ✅ |
| ZENMODE-SESSION-002 | HP uses class base HP (class passives still apply) | `Assets/Scripts/Run/RunDirector.cs` | Endless mode setup | — | ◻️ |
| ZENMODE-DEPTH-001 | Depth starts at 0, increments after each solved puzzle | `Assets/Scripts/Run/RunDirector.cs` | `EndlessZenService.BuildLevel` | — | ◻️ |
| ZENMODE-DEPTH-002 | Stars formula: `Clamp(1 + floor(depth/4), 1, 5)` | `Assets/Scripts/Run/RunDirector.cs` | `EndlessZenService.BuildLevel` | E2E/diagnostic D22 | ✅ |
| ZENMODE-DEPTH-003 | Stars capped at 5★ in Endless Zen | `Assets/Scripts/Run/RunDirector.cs` | `EndlessZenService.BuildLevel` | E2E/diagnostic D22 | ✅ |
| ZENMODE-DEPTH-004 | Modifier cap formula: <10=1, <20=2, 20+=3 | `Assets/Scripts/Run/RunDirector.cs` | `EndlessZenService.BuildLevel` | GenerationReliabilityTests | ✅ |
| ZENMODE-RES-001 | Between levels: +1 HP, +3 Pencil restored | `Assets/Scripts/Run/RunDirector.cs` | Level completion | — | ◻️ |
| ZENMODE-SCORE-001 | `ProfileStats.HighestEndlessDepth` tracks best depth | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | — | ◻️ |

---

## Spirit Trials Mode

**Spec:** `docs/SpiritTrialsMode.md` v2.1

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| TRIAL-UNLOCK-001 | Unlocked by the first boss defeat (`TotalBossesDefeated ≥ 1`; SPEC-CONF-007) | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | SpecAlignmentTests | ✅ |
| TRIAL-SESSION-001 | Session: 9×9, timer, no XP, no gold, 1 starting item | `Assets/Scripts/Run/RunDirector.cs` | Spirit Trials setup | GenerationReliabilityTests | ✅ |
| TRIAL-SESSION-002 | Timer starts on first cell interaction | `Assets/Scripts/UI/InRunController.cs` | Input handler | — | ◻️ |
| TRIAL-TIER-001 | 4 tiers: Apprentice/Adept/Master/Grandmaster (star + modifier counts) | `Assets/Scripts/Run/RunDirector.cs` | `SpiritTrialsService` | E2E/diagnostic D23 | ✅ |
| TRIAL-TIER-002 | Modifiers assigned randomly (seeded) — player cannot choose | `Assets/Scripts/Run/RunDirector.cs` | `SpiritTrialsService` | GenerationReliabilityTests | ✅ |
| TRIAL-SCORE-001 | BasePoints = `CellsToFill × PointsPerCell` (per tier) | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | — | ◻️ |
| TRIAL-SCORE-002 | SpeedMultiplier = `max(0.5, 2.0 - (ElapsedSeconds / ParTime))` | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | — | ◻️ |
| TRIAL-SCORE-003 | Modifier constraint bonus: 0=+0, 1=+100, 2=+300 | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | — | ◻️ |
| TRIAL-SCORE-004 | Mistake penalty per tier; HP=0 ends session | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | — | ◻️ |
| TRIAL-SCORE-005 | FinalScore = `floor((Base × Speed) + Constraint + Pencil − Mistake)` | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | — | ◻️ |
| TRIAL-SCORE-006 | Minimum score 0 (never negative) | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | — | ◻️ |
| TRIAL-LEAD-001 | Per-tier personal bests in `ProfileStats` | `Assets/Scripts/Save/ProfileService.cs` | `ProfileStats` | — | ◻️ |
| TRIAL-LEAD-002 | Monthly leaderboard when `SeasonalChallengeUnlocked` | `Assets/Scripts/Save/SaveFileService.cs` | Monthly Walk seed check (`year × 100 + month`) | — | ◻️ |

---

## Sudoku Basics Tutorial

**Spec:** `docs/FirstTimePlayTutorial.md` v2.1

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| TUTO-BASICS-TRIGGER-001 | Triggers when `"sudoku_basics"` not in `CompletedKeys` | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `Start()` | StartupFlowPlayModeTests; FirstRunSetupPlayModeTests | ✅ |
| TUTO-BASICS-TRIGGER-002 | Only triggers once (check skipped on subsequent launches) | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `Start()` | — | ◻️ |
| TUTO-BASICS-TRIGGER-003 | Prompt appears after main menu loads (not before) | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `ShowSudokuBasicsPrompt` | StartupFlowPlayModeTests; FirstRunSetupPlayModeTests | ✅ |
| TUTO-BASICS-BOARD-001 | Fixed 6x6 board, 2x3 boxes, deterministic givens | `Assets/Scripts/Tutorial/TutorialModeService.cs` | `BuildSudokuBasicsLevel` / `BuildBasicsTutorialBoard` | — | ◻️ |
| TUTO-BASICS-BOARD-002 | Mistakes during tutorial not penalised | `Assets/Scripts/Run/RunDirector.cs` | Tutorial RunState | — | ◻️ |
| TUTO-BASICS-STEP-001 | Step table: 15 steps on the fixed 6×6 board (FirstTimePlayTutorial v2.1) | `Assets/Scripts/UI/InRunController.cs` | `SudokuBasicsTutorialController` | SudokuBasicsStepsTests | ✅ |
| TUTO-BASICS-STEP-002 | Each step names the target cell and digit; wrong input repeats the step | `Assets/Scripts/UI/InRunController.cs` | `SudokuBasicsTutorialController` | SudokuBasicsStepsTests | ✅ |
| TUTO-BASICS-STEP-006 | Final steps fill the board and show the completion card | `Assets/Scripts/UI/InRunController.cs` | `SudokuBasicsTutorialController` | SudokuBasicsStepsTests | ✅ |
| TUTO-BASICS-DONE-001 | Completion writes `"sudoku_basics"` to `CompletedKeys` + saves | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `MarkSudokuBasicsComplete` | — | ◻️ |
| TUTO-BASICS-DONE-002 | Replayable via Options → "Replay Sudoku Basics" | `Assets/Scripts/UI/InRunController.cs` | `MainMenuController` options | — | ⚠️ |

---

## Sudoku Generation Pipeline

**Spec:** `docs/SudokuGenerationPipeline.md` v1.7

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| GEN-REGION-001 | `BuildRegionMap(size, variant)` — 4 variants (0=Standard, 1=Alt, 2=Irregular A, 3=Jigsaw) | `Assets/Scripts/Sudoku/SudokuGenerator.cs` | `BuildRegionMap` | PuzzleGenerationServiceTests | ✅ |
| GEN-REGION-002 | All templates: each region = `size` cells, contiguous, count = board size | `Assets/Scripts/Sudoku/SudokuGenerator.cs` | Template methods | PuzzleGenerationServiceTests | ✅ |
| GEN-REGION-003 | `AllowIrregularPuzzles=false` restricts to variants 0–1 | `Assets/Scripts/Run/RunDirector.cs` | `BuildLevelConfig` | GeneratedUiPlayModeTraversalTests | ✅ |
| GEN-REGION-004 | Board shows the current puzzle's region borders; redrawn for every new board or region map | `Assets/Scripts/UI/BoardViewController.cs` | `SyncRegionBorders` | InRunUiPlayModeTests | ✅ |
| GEN-SOLVE-001 | `GenerateSolvedBoard(size, regionMap, rng)` — backtracking + MRV heuristic + seeded shuffle | `Assets/Scripts/Sudoku/SudokuGenerator.cs` | `GenerateSolvedBoard` / `FillBoard` | SudokuBacktrackingSolverTests | ✅ |
| GEN-REMOVE-001 | Cell removal to create player-facing puzzle | `Assets/Scripts/Sudoku/SudokuGenerator.cs` | `RemoveCells` | E2E/diagnostic D02 | ✅ |
| GEN-REMOVE-002 | `StarDensityService.MissingPercentForStars`: `(stars+3)×0.1`, clamped [0.01, 0.95] | `Assets/Scripts/Core/StarDensityService.cs` | `MissingPercentForStars` | E2E/diagnostic D02 | ✅ |
| GEN-REMOVE-003 | 7★ removes all cells; requires ≥1 modifier; tutorial-only | `Assets/Scripts/Core/StarDensityService.cs` | 7★ case | SpecAlignmentTests | ✅ |
| GEN-REMOVE-004 | 7★ clamp bypassed (`removeCount = totalCells`) | `Assets/Scripts/Sudoku/SudokuGenerator.cs` | `RemoveCells` | E2E/diagnostic D14 | ✅ |
| GEN-MOD-001 | `ModifierGeometryGenerator.Generate(board, modifiers, seed, intensity)` | `Assets/Scripts/Sudoku/ModifierGeometryGenerator.cs` | `Generate` | ConstraintRuleTests | ✅ |
| GEN-VALID-001 | Placement valid if satisfies all constraints (not compared to solution) | `Assets/Scripts/Sudoku/SudokuBoard.cs` | `IsCorrectAt`, `IsComplete` | PlacementRulesTests | ✅ |
| GEN-VALID-002 | Validation: `PlaceNumber` → `IsMoveValid` → `SudokuConstraintEngine.ValidateAll` | `Assets/Scripts/Run/RunDirector.cs` | `PlaceNumber` | ConstraintRuleTests, PlacementRulesTests | ✅ |

---

## Path System & Garden Overview

**Spec:** `docs/PathSystem_GardenOverview.md` v1.6

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| MAP-STRUCT-001 | 5 floors per run; each floor is a garden with distinct theme | `Assets/Scripts/Run/RunDirector.cs` | Floor management | BossClearedFlowPlayModeTests | ✅ |
| MAP-STRUCT-002 | Each floor: Shared Start → Calm/Risk Route → Shared Boss Gate | `Assets/Scripts/Run/RunGraphService.cs` | Graph generation | RunGraphTests | ✅ |
| MAP-ROUTE-001 | Calm Route: longer, lower difficulty, more safe tiles, upper half | `Assets/Scripts/Run/RunGraphService.cs` | `BuildCalmRoute` | RunGraphTests | ✅ |
| MAP-ROUTE-002 | Risk Route: shorter, higher difficulty, fewer safe tiles, lower half | `Assets/Scripts/Run/RunGraphService.cs` | `BuildRiskRoute` | RunGraphTests | ✅ |
| MAP-CROSS-001 | Exactly one cross-connection per floor (lane switch opportunity) | `Assets/Scripts/Run/RunGraphService.cs` | Cross-link logic | InRunUiPlayModeTests, RunGraphTests | ✅ |
| MAP-NODE-CURSED-001 | Cursed node shows Accept/Decline panel before puzzle | `Assets/Scripts/Run/RunDirector.cs` | Cursed node handling | — | ◻️ |
| MAP-NODE-CURSED-002 | Accept: IsCursed=true, ×1.5 gold+XP, +1 random modifier | `Assets/Scripts/Run/RunDirector.cs` | `AcceptCursedNode` | E2E/diagnostic D25 | ✅ |
| MAP-NODE-CURSED-003 | Decline: standard puzzle, no multipliers | `Assets/Scripts/Run/RunDirector.cs` | `DeclineCursedNode` | — | ◻️ |
| MAP-NODE-CURSED-004 | `CursedPuzzlesAccepted` tracked for run score breakdown | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState` | — | ◻️ |
| MAP-FX-001 | One random positive floor effect per floor | `Assets/Scripts/Run/RunDirector.cs` | `RollFloorModifiers` (positive floor effect roll) | E2E/diagnostic E05, E19 | ✅ |
| MAP-FX-002 | Floor effect seeded from `RunState.Seed + floorIndex` | `Assets/Scripts/Run/RunDirector.cs` | `RollFloorModifiers` (positive floor effect roll) | E2E/diagnostic D01 | ✅ |
| MAP-FX-003 | Stored in `RunState.HasPositiveFloorEffect + ActivePositiveFloorEffect` | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState` | E2E/diagnostic E05, E19 | ✅ |
| MAP-FX-004 | Shown on path screen as banner via `GetPositiveFloorEffectLabel()` | `Assets/Scripts/UI/RunMapController.cs` | `GetPositiveFloorEffectLabel` | E2E/diagnostic E05, E19 | ✅ |
| MAP-FX-005 | None effect has equal probability (20% each) | `Assets/Scripts/Run/RunDirector.cs` | `RollFloorModifiers` (positive floor effect roll) | — | ◻️ |

---

## Boss Mechanics

**Spec:** `docs/BossMechanicsSystem.md` v2.3

Key requirements (see spec for full list):

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| BOSS-MOD-001 | 30 player modifiers (`BossModifierId` 0–29); IDs ≥ 94 are boss debuffs (SPEC-CONF-019) | `Assets/Scripts/Core/GameEnums.cs` | `BossModifierId` | GenerationReliabilityTests | ✅ |
| BOSS-MOD-002 | `BossService.ModifierData` — tier/impact/name/description per modifier | `Assets/Scripts/Boss/BossService.cs` | `ModifierData` | BalanceBudgetContractTests, GenerationReliabilityTests | ✅ |
| BOSS-MOD-003 | `ModifierGeometryGenerator.Generate` produces overlay for each modifier | `Assets/Scripts/Sudoku/ModifierGeometryGenerator.cs` | `Generate` | BalanceBudgetContractTests.AntiBishop_IsEligibleFromSixBySix; GenerationReliabilityTests | ✅ |
| BOSS-INT-001 | `BossModifierIntensity` enum (Low/Medium/High/VeryHigh) scales geometry | `Assets/Scripts/Core/GameEnums.cs` | `BossModifierIntensity` | E2E/diagnostic D05, D18, E17 | ✅ |
| BOSS-INT-002 | Intensity set by `RunNumber` in `BuildLevelConfig` (1-2=Low, 3-5=Med, 6-8=High, 9+=VH) | `Assets/Scripts/Run/RunDirector.cs` | `BuildLevelConfig` | InRunUiPlayModeTests.BossGate_ShowsIntensity_AndTheDebuffFromFloorThree | ✅ |
| BOSS-FLOOR-001 | Floor modifier system: floors 2–5 add persistent modifiers to all puzzles | `Assets/Scripts/Run/RunDirector.cs` | `RollFloorModifiers` | HarmonyEffectsTests.BossOptionsAndFloorBonus_MatchSpecTable; BalanceBudgetContractTests | ✅ |
| BOSS-FLOOR-002 | Boss choice pool excludes active floor modifiers | `Assets/Scripts/Boss/BossService.cs` | `BuildEligiblePool` | E2E/diagnostic D18 | ✅ |
| BOSS-FLOOR-003 | `RunState.FloorModifiers` stored for persistence | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState.FloorModifiers` | E2E/diagnostic D12 | ✅ |

---

## Class XP Progression

**Spec:** `docs/ClassXpProgressionSystem.md` v1.3

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| XP-TILE-001 | `XpService.CalculateTile(boardSize, stars, modCount, isBoss, perfect)` → TileXpEntry | `Assets/Scripts/Economy/XpService.cs` | `XpService.CalculateTile` | RunFlowAcceptanceTests | ✅ |
| XP-TILE-002 | Base XP per board size: 5×5=30, 6×6=45, 7×7=60, 8×8=90, 9×9=120 | `Assets/Scripts/Economy/XpService.cs` | `XpService` | XpTableTests | ✅ |
| XP-TILE-003 | Star multiplier: 1★=×1.0, 2★=×1.2, 3-4★=×1.5, 5-6★=×2.0 | `Assets/Scripts/Economy/XpService.cs` | `XpService` | XpTableTests | ✅ |
| XP-TILE-004 | Boss ×1.5; Mod bonus +15 per active mod (boss only); Perfect +25 flat | `Assets/Scripts/Economy/XpService.cs` | `XpService` | XpTableTests | ✅ |
| XP-CURVE-001 | L1-15: `50+(n-1)×10`; L16-40: `190+(n-15)×35` | `Assets/Scripts/Economy/XpTable.cs` | `XpTable.XpToNextLevel` | XpTableTests | ✅ |
| XP-CURVE-002 | `DeriveLevel(totalXp)` derives level at runtime from cumulative XP | `Assets/Scripts/Economy/XpTable.cs` | `XpTable.DeriveLevel` (XP capped at the L40 total) | HarmonyEffectsTests, XpTableTests | ✅ |
| XP-LOG-001 | `RunDirector._tileXpLog` tracks per-tile XP for end-of-run breakdown | `Assets/Scripts/Run/RunDirector.cs` | `_tileXpLog` | CompletionTrackingTests, DeterminismAcrossResumeTests, RunFlowAcceptanceTests | ✅ |
| XP-COMMIT-001 | XP committed to class at run end in `RecordRunAndGetNewUnlocks` | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | E2E/diagnostic D09 | ✅ |

---

## Items & Relics

**Spec:** `docs/ItemsAndRelicsSystem.md` v2.1

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| ITEM-SLOT-001 | Item reward slots by star: 1★=2, 2★=3, 3★=3, 4★=4, 5★=5 | `Assets/Scripts/Items/ItemService.cs` | `RollRewardSlots` | BalanceBudgetContractTests | ✅ |
| ITEM-SLOT-002 | Nothing slot probability: 1★=25%, 2★=22%, 3★=18%, 4★=15%, 5★=12% | `Assets/Scripts/Items/ItemService.cs` | `RollRewardSlots` | EconomyActionTests.NothingReward_*; HarmonyEffectsTests.NothingChance_* | ✅ |
| ITEM-EPIC-001 | Epic items only available at class Level 15+ | `Assets/Scripts/Items/ItemService.cs` | `BuildItemPool` | E2E/diagnostic D10 | ✅ |
| RELIC-SLOT-001 | Single relic slot — superseded by the multi-relic list (ItemsAndRelicsSystem v2.0, RELIC-SLOT-002) | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState` | — | 🗄️ Deprecated |
| RELIC-ROLL-001 | Relic tier weights: floor-based 5×5 table; risk route shifts weights up | `Assets/Scripts/Economy/RelicService.cs` | `RollRelic` | — | ◻️ |
| SHOP-PRICE-001 | Shop price: `BasePrice × (1 + FloorIndex × 0.5) × priceMultiplier` | `Assets/Scripts/Economy/RelicService.cs` | `ShopService.BuildOffers` | E2E/diagnostic D11, E15 | ✅ |

---

## Save / Load

**Spec:** `docs/SaveLoadArchitecture.md` v2.4 (the SPICE `REQ-SAVE-*` stubs are archived in `docs/archive/SPICE_REQUIREMENT_TABLES.md`, SPEC-OBS-001)

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| SAVE-DUAL-001 | Dual save files — superseded: one JSON envelope per slot (SaveLoadArchitecture v2.0) | `Assets/Scripts/Save/SaveFileService.cs` | File management | — | 🗄️ Deprecated |
| SAVE-FOLDER-001 | Save folder stays `LocalLow\Kratossos\sudoku_rouge_like` (released 1.0 build, Steam Auto-Cloud root): company/product names unchanged | `ProjectSettings/ProjectSettings.asset` | `companyName`, `productName` | LegacySaveMigrationTests | ✅ |
| SAVE-MIGRATE-001 | One-time copy of profiles from `LocalLow\DefaultCompany\RunOfTheNine` (pre-release builds) into the save folder | `Assets/Scripts/Save/LegacySaveMigration.cs` | `Run`, `RunOnStartup` | LegacySaveMigrationTests | ✅ |
| SAVE-SLOT-001 | 3 profile slots: `save_profile_0/1/2.json` | `Assets/Scripts/Save/SaveFileService.cs` | Constructor | SaveDurabilityReadinessTests | ✅ |
| SAVE-SLOT-002 | `SaveProfileService` manages all 3 slots; `ActiveSlot` in `PlayerPrefs` | `Assets/Scripts/Save/ProfileService.cs` | `SaveProfileService` | — | ◻️ |
| SAVE-SLOT-003 | `GameBootstrap.ApplySlotChange()` re-wires save services after slot switch | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `ApplySlotChange` | — | ◻️ |
| SAVE-SLOT-004 | No stored active slot → most recently written profile becomes active and is stored; none → slot 0, nothing stored | `Assets/Scripts/Save/SaveProfileService.cs` | `ActiveSlot`, `EnsureActiveSlot`, `NewestExistingSlot` | ActiveSlotRecoveryTests | ✅ |
| SAVE-AUTO-001 | `RunAutoSaveCoordinator.Save()` — atomic write before serializing | `Assets/Scripts/Save/RunAutoSaveCoordinator.cs` | `Save` | AutosaveMergeTests, ResumeMidPuzzleTests, SaveDurabilityReadinessTests | ✅ |
| SAVE-RESUME-001 | `RunResumeService.TryResumeFromSave()` restores full run state including boss modifier | `Assets/Scripts/Save/RunResumeService.cs` | `TryResumeFromSave` | ResumeMidPuzzleTests | ✅ |
| SAVE-SERIAL-001 | All save data uses `JsonUtility` (`[Serializable]` on all data classes) | `Assets/Scripts/Core/SaveModels.cs` | Data models | ModifierDiscoveryPersistenceTests | ✅ |
| SAVE-SERIAL-002 | Nullable types use bool+value pattern (`HasChosenBossModifier + ChosenBossModifierId`) | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState` | ModifierDiscoveryPersistenceTests | ✅ |
| SAVE-SERIAL-003 | `HashSet<T>` not serialized — use `List<T>` + sync methods | `Assets/Scripts/Core/RuntimeModels.cs` | `SeenBossModifierList` | ModifierDiscoveryPersistenceTests | ✅ |
| SAVE-TUTO-001 | Tutorial saves excluded: `RunAutoSaveCoordinator.Save` skips `TutorialMode` runs | `Assets/Scripts/Save/RunAutoSaveCoordinator.cs` | `Save` | SaveDurabilityReadinessTests | ✅ |

---

## Input & Profiles

**Spec:** `docs/InputAndProfilesSpec.md` v2.0

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| INPUT-REMAP-001 | `InputRemapService` — keyboard defaults + gamepad button mappings | `Assets/Scripts/Core/InputRemapService.cs` | Constructor | InputRemapServiceTests | ✅ |
| INPUT-REMAP-002 | `WasActionPressed(action)` — checks keyboard then gamepad then axis crossing | `Assets/Scripts/Core/InputRemapService.cs` | `WasActionPressed` | InputRemapServiceTests | ✅ |
| INPUT-REMAP-003 | `GetAnyKeyDown()` — returns first pressed key (keyboard or joystick) | `Assets/Scripts/Core/InputRemapService.cs` | `GetAnyKeyDown` | — | ◻️ |
| INPUT-REMAP-004 | `KeybindingsPanelController` manages listen/capture/save rebind flow | `Assets/Scripts/UI/KeybindingsPanelController.cs` | `KeybindingsPanelController` | — | ◻️ |
| INPUT-PROFILE-001 | `MenuScreen.ProfileSelect` — shown on first launch if no files exist | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `Start` | StartupFlowPlayModeTests | ✅ |
| INPUT-PROFILE-002 | `SelectProfileSlot(i)`, `DeleteProfileSlot(i)`, `RefreshProfileSelectCards()` | `Assets/Scripts/UI/InRunController.cs` | `MainMenuController` | — | ◻️ |

---

## Accessibility

**Spec:** `docs/AccessibilitySpec.md` v2.1 (the SPICE `REQ-UI-*` stubs are archived, SPEC-OBS-001)

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| ACCESS-COLOR-001 | `AccessibilityService` — colorblind mode, high contrast, reduce motion, alt symbols | `Assets/Scripts/Core/AccessibilityService.cs` | Service | — | ◻️ |
| ACCESS-COLOR-002 | Accessibility toggles call `NotifyAccessibilityChanged` → rebuild overlays | `Assets/Scripts/UI/InRunController.cs` | `OptionsController` | — | ◻️ |
| ACCESS-MOTION-001 | Screen shake disabled when `ReduceMotion` on | `Assets/Scripts/UI/InRunController.cs` | Shake helper | — | ◻️ |
| ACCESS-OVERLAY-001 | High contrast: overlay spine alpha ×1.4, line width ×1.5 | `Assets/Scripts/UI/BoardViewController.cs` | Overlay render | — | ◻️ |
| ACCESS-PARTICLE-001 | All particle effects disabled when `ReduceMotion` active | `Assets/Scripts/UI/InRunController.cs` | `AmbientParticleController` | — | ◻️ |

---

## Gold Economy

**Spec:** `docs/GoldEconomySystem.md` v2.1

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| ECON-REWARD-001 | Gold awarded on puzzle completion via gold formula | `Assets/Scripts/Run/RunDirector.cs` | `CompleteLevelAndGrantRewards` | E2E/diagnostic D11, E14, E16 | ✅ |
| ECON-SHOP-001 | Shop: 2 / 3 / 4 items by floor (floors 1–2 / 3–4 / 5), no relic (SPEC-CONF-005) | `Assets/Scripts/Economy/ShopService.cs` | `ShopService.BuildOffers`, `GetShopSlotCount` | SpecAlignmentTests | ✅ |
| ECON-SHOP-002 | Price scaling: `BasePrice × (1 + FloorIndex × 0.5) × priceMultiplier` | `Assets/Scripts/Economy/RelicService.cs` | `ShopService.BuildOffers` | E2E/diagnostic D11 | ✅ |

---

## Seasonal Challenge

**Spec:** `docs/SeasonalChallengeSpec.md` v0.2

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| SEASON-UNLOCK-001 | Monthly Walk is always available (no ascension gate; SPEC-CONF-026) | `Assets/Scripts/UI/MainMenuController.cs` | Monthly Walk button and home widget | GeneratedUiPlayModeTraversalTests.HomeWidgets_*; NestedMenuButtons_* | ✅ |
| SEASON-SEED-001 | Monthly seed: `(year × 100) + month` | `Assets/Scripts/Run/SeasonalChallengeService.cs` | `BuildSeed` | SpecAlignmentTests | ✅ |
| SEASON-PUZZLE-001 | Fixed 9×9 4★ puzzle from monthly seed | `Assets/Scripts/Run/SeasonalChallengeService.cs` | `BuildChallengeConfig` | GenerationReliabilityTests | ✅ |
| SEASON-SCORE-001 | Personal best stored locally | `Assets/Scripts/Core/SaveModels.cs` | `SeasonalChallengeState` | — | ◻️ |
| SEASON-UI-001 | Prominent main menu panel with countdown | `Assets/Scripts/UI/MainMenuController.cs` | `RefreshMonthlyWalkPanel` | GeneratedUiPlayModeTraversalTests | ✅ |
| SEASON-UI-002 | `GameMode.SeasonalChallenge` enum value | `Assets/Scripts/Core/GameEnums.cs` | `GameMode` | E2E/diagnostic E13 | ✅ |

---

## Daily Goal System

**Spec:** `docs/DailyGoalSystem.md` v0.2

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| DAILY-GOAL-001 | 3 daily goals per day (date-seeded, no network) | `Assets/Scripts/Core/SaveModels.cs` | `DailyGoalState` | — | ◻️ |
| DAILY-GOAL-002 | Ink Stamps cosmetic currency from goal completion | `Assets/Scripts/Core/SaveModels.cs` | `DailyGoalState` | — | ◻️ |
| DAILY-GOAL-003 | Streak tracking (consecutive days) | `Assets/Scripts/Core/SaveModels.cs` | `DailyGoalState` | DailyStreakTests | ✅ |
| DAILY-GOAL-004 | Daily Walk widget on main menu | `Assets/Scripts/UI/MainMenuController.cs` | `ShowDailyWalkPanel` | GeneratedUiPlayModeTraversalTests | ✅ |
| DAILY-GOAL-005 | `DailyGoalState` in `ProfileSaveData` | `Assets/Scripts/Core/SaveModels.cs` | `SaveFileEnvelope` | — | ◻️ |

---

## Class Progression Unlocks

**Spec:** `docs/ClassProgressionUnlocks.md` v0.3

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| CLASS-EXCL-001 | 16 class-exclusive unlocks: 8 items (L15) + 8 relics (L30) per class | `Assets/Scripts/Items/ItemService.cs`, `RelicService.cs` | `ClassExclusiveItems`, `ClassExclusiveRelics` | — | ◻️ |
| CLASS-EXCL-002 | `ClassExclusive` flag on item/relic definitions | `Assets/Scripts/Items/ItemService.cs`, `RelicService.cs` | `ExclusiveDef`, `ExclusiveRelicDef` | — | ◻️ |
| CLASS-EXCL-003 | `ClassUnlockLevel` (15 or 30) on item/relic definitions | `Assets/Scripts/Items/ItemService.cs`, `RelicService.cs` | `UnlockLevel` = 15 (items), 30 (relics) | — | ◻️ |
| CLASS-EXCL-004 | Exclusive items/relics only rollable for their associated class | `Assets/Scripts/Items/ItemService.cs`, `RelicService.cs` | `IsAvailableForClass`, `RollRelicChoices` | GameplayEffectContractTests | ✅ |

---

## Harmony Difficulty

**Spec:** `docs/HarmonyDifficultySystem.md` v1.2 — IDs written out individually on 2026-10-06 (the spec listed ranges such as `HARMONY-BOSS-001..002`).

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| HARMONY-001 | `RunState.HarmonyLevel` (0–10) set at run start | `Assets/Scripts/Run/RunArchetypeService.cs` | `CreateRunState` | — | ◻️ |
| HARMONY-002 | `RunState.MistakeHpCost` (1; 2 from H5; 3 from H9) | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState.MistakeHpCost` | — | ◻️ |
| HARMONY-003 | `RunState.ComboDecayEnabled` (H7+) | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState.ComboDecayEnabled` | — | ◻️ |
| HARMONY-004 | `RunState.HarmonyConfig` rebuilt from the level after load | `Assets/Scripts/Save/RunResumeService.cs` | `RestoreTransientState` | — | ◻️ |
| HARMONY-BOSS-001 | Extra floor modifiers: H3–H5 +1 on floors 2–4, H6–H8 +1, H9–H10 +2 | `Assets/Scripts/Run/HarmonyDifficultyService.cs` | `GetBonusFloorModifiers` | HarmonyEffectsTests | ✅ |
| HARMONY-BOSS-002 | Boss options bonus added to shown and picked (always one exclusion) | `Assets/Scripts/Boss/BossService.cs`, `RunDirector.cs` | `RollBossChoices`, `GetBossModifierCounts` | HarmonyEffectsTests | ✅ |
| HARMONY-ECON-001 | Puzzle gold × Harmony gold multiplier (1.00–0.60) | `Assets/Scripts/Run/RunDirector.cs` | `CompleteLevelAndGrantRewards` | HarmonyEffectsTests | ✅ |
| HARMONY-ECON-002 | Shop price surcharge (×1.00–×1.50) | `Assets/Scripts/Economy/ShopService.cs` | `BuildOffers` | — | ◻️ |
| HARMONY-ECON-003 | Starting reroll token penalty | `Assets/Scripts/Run/RunArchetypeService.cs` | `CreateRunState` | — | ◻️ |
| HARMONY-SURV-001 | Starting HP penalty (min 3) | `Assets/Scripts/Run/RunArchetypeService.cs` | `CreateRunState` | — | ◻️ |
| HARMONY-SURV-002 | HP lost per mistake = mistake HP cost (DoublePenalty doubles it) | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty(row, col)` | HarmonyEffectsTests | ✅ |
| HARMONY-SURV-003 | Combo decay (H7+): the combo carried into a new puzzle is halved | `Assets/Scripts/Run/RunDirector.cs` | `PrepareRunStateForNewPuzzle` | HarmonyEffectsTests | ✅ |
| HARMONY-SURV-004 | Starting pencil penalty (min 3) | `Assets/Scripts/Run/RunArchetypeService.cs` | `CreateRunState` | — | ◻️ |
| HARMONY-SURV-005 | Item slot cap penalty (min 1) | `Assets/Scripts/Run/RunArchetypeService.cs` | `CreateRunState` | — | ◻️ |
| HARMONY-ITEM-001 | Nothing-slot chance + Harmony bonus, capped at 40 %, never removes the last item | `Assets/Scripts/Run/RunDirector.cs` | `BuildItemRewardSlots` | HarmonyEffectsTests | ✅ |
| HARMONY-RNG-001 | Positive floor effect chance × Harmony multiplier | `Assets/Scripts/Run/RunDirector.cs` | `RollFloorModifiers` (positive floor effect roll) | — | ◻️ |
| HARMONY-FLAG-001 | H5+: boss modifier labels stay "???" even when seen before | `Assets/Scripts/UI/BossGateViewController.cs` | `BuildBossGatePanel` | InRunUiPlayModeTests | ✅ |
| HARMONY-FLAG-002 | H7+: starting relics from the Tier 1 pool only | `Assets/Scripts/Economy/RelicService.cs` | `AssignStartingRelics` | — | ◻️ |
| HARMONY-FLAG-003 | H7+: each mistake also costs 1 pencil | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty(row, col)` | HarmonyEffectsTests | ✅ |
| HARMONY-FLAG-004 | H10: rest heal halved (rounded up) | `Assets/Scripts/Run/RunDirector.cs` | `GetRestHealAmount` | HarmonyEffectsTests | ✅ |
| HARMONY-FLAG-005 | H9+: cursed puzzle +50 % gold/XP suppressed | `Assets/Scripts/Run/RunDirector.LevelGeneration.cs` | `BuildCursedLevelConfig` | HarmonyEffectsTests | ✅ |
| HARMONY-XP-001 | Class XP × Harmony multiplier on every run end, no flat bonus | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | HarmonyEffectsTests | ✅ |
| HARMONY-UNLOCK-001 | Next level unlocks after a victory at the current maximum with ≥ 1 boss defeated | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | ModifierDiscoveryPersistenceTests…UnlocksNextLevel | ✅ |
| HARMONY-PERK-001 | One optional pre-run perk per run | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState.HarmonyPerk` | — | ◻️ |
| HARMONY-PERK-002 | Perk effects applied at run start | `Assets/Scripts/Run/RunArchetypeService.cs` | `CreateRunState` | — | ◻️ |
| HARMONY-UI-001 | Game Modes panel: Harmony level and perk selection | `Assets/Scripts/UI/GameModesPanelController.cs` | `SetHarmonyLevel`, `SetHarmonyPerk` | E2E/diagnostic E03, E12 | ✅ |
| HARMONY-UI-002 | Harmony node row with locked / unlocked / selected states | `Assets/Scripts/UI/HarmonyNodeRowController.cs` | `Refresh` | — | ◻️ |
| HARMONY-ACHIEVE-001 | Harmony Steam achievements (IDs 53–60) | `Assets/Scripts/Meta/SteamAchievementService.cs` | `EvaluateHarmonyAchievements` | SteamAchievementServiceTests | ✅ |
| HARMONY-SAVE-001 | Harmony fields clamped on load | `Assets/Scripts/Save/SaveFileService.cs` | `SanitizeEnvelope` | — | ◻️ |
| HARMONY-BADGE-001 | Per-level badge flags (perfect, speed) | `Assets/Scripts/Core/SaveModels.cs` | `HarmonyBadgeFlags` | — | ◻️ |
| HARMONY-DISPLAY-001 | Display name and HUD label per level | `Assets/Scripts/Run/HarmonyDifficultyService.cs` | `GetDisplayName`, `GetHudLabel` | — | ◻️ |
| HARMONY-CONFIG-001 | Single source for all Harmony parameters; grid and star weights applied within each floor's range | `Assets/Scripts/Run/HarmonyDifficultyService.cs` | `BuildConfig`, `PickBoardSize`, `PickStarsAbove` | — | ◻️ |

---

## Multi-Relic Save Compatibility

**Spec:** `docs/ItemsAndRelicsSystem.md` — section **Single Relic Slot**

> These IDs formalise code that exists in `RuntimeModels.cs` and `RunResumeService.cs` but was never spec'd.  
> `RELIC-SLOT-MULTI` (the orphaned annotation) is now resolved as `RELIC-SLOT-002`.

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| RELIC-SLOT-002 | `RunState.HeldRelics: List<RelicInstance>` — multi-relic list field (future-proof; currently max 1 entry enforced by game logic) | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState.HeldRelics` | — | ◻️ |
| RELIC-SLOT-003 | Legacy migration on resume: if `HasRelic && HeldRelic != null && HeldRelics empty` → migrate single relic into list | `Assets/Scripts/Save/RunResumeService.cs` | `RestoreTransientState` | — | ◻️ |

---

## Display Modes

**Spec:** `docs/GameDesignSpec.md` v1.8 §Display Modes

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| DISPLAY-MODE-001 | Four display modes: Fullscreen, Borderless Fullscreen, Windowed, Frameless Window | `Assets/Scripts/UI/OptionsController.cs` | `ToUnityFullScreenMode`, `DisplayModeDropdown` | DisplayModeTests, GeneratedUiPlayModeTraversalTests | ✅ |
| DISPLAY-MODE-002 | Window size: chosen resolution if it leaves 10 % free, else largest standard 16:9 size that does | `Assets/Scripts/UI/OptionsController.cs` | `GetWindowSize` | DisplayModeTests | ✅ |
| DISPLAY-MODE-003 | Window modes open centred on the monitor work area | `Assets/Scripts/Platform/WindowFrameService.cs` | `Win32.ApplyFrame` | E2E/diagnostic B16, B19 | ✅ |
| DISPLAY-MODE-004 | Frameless Window via window style (Windows player); Windowed elsewhere | `Assets/Scripts/Platform/WindowFrameService.cs` | `Apply`, `Runner` | E2E/diagnostic B16, B19 | ✅ |
| DISPLAY-MODE-005 | Saved values 0–3, unknown → Fullscreen, legacy flag → Windowed; Fullscreen flag false for window modes | `Assets/Scripts/Save/SaveFileService.cs` | `SanitizeOptions` | DisplayModeTests | ✅ |
| DISPLAY-MODE-006 | Fullscreen toggle keeps Frameless Window / Borderless Fullscreen when chosen | `Assets/Scripts/UI/OptionsController.cs` | `SetFullscreen` | DisplayModeTests | ✅ |
| DISPLAY-MODE-007 | Window title "Run of the Nine" in every mode (Windows player) | `Assets/Scripts/Platform/WindowFrameService.cs` | `WindowTitle`, `Win32.SetTitle`, `Runner.Update` | E2E/diagnostic B23 | ✅ |

---

## Options Panels

**Spec:** `docs/GameDesignSpec.md` v1.9 §Options Panels

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| UI-OPT-001 | In-run Options use the main Options' tabs, controls and look (one factory) | `Assets/Scripts/UI/OptionsUiFactory.cs` | `BuildStandardTabs` (called by `MainMenuBlueprintBuilder.BuildOptionsPanel` and `InRunUiBlueprintBuilder.BuildInGameOptionsPanel`) | GeneratedUiPlayModeTraversalTests (`InRunOptions_OfferEveryMainMenuOptionsControl`, `InRunOptionsControls_CanBeChangedWithoutLeavingOptionsPanel`), GeneratedUiWiringContractTests, screenshots | ✅ |
| UI-OPT-002 | In-run Accessibility and Run tabs; Back always visible | `Assets/Scripts/UI/InRunUiBlueprintBuilder.cs` | `BuildInGameOptionsPanel`, `OptionsUiFactory.BuildAccessibilitySection` | GeneratedUiPlayModeTraversalTests, InRunUiPlayModeTests (`RunMenu_ShowsSeedAndModifiers_AndAbandonEndsTheRun`) | ✅ |
| UI-OPT-003 | D-pad reaches every control of every tab, the tab bar and Back | `Assets/Scripts/UI/OptionsTabSet.cs` | `Relink`, `Activate` | GeneratedUiPlayModeTraversalTests (`OptionsPanels_EveryControlIsReachableWithTheDpad`) | ✅ |

---

## Shared Challenges

**Spec:** `docs/GameDesignSpec.md` v1.10 §Shared challenges

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| RUN-WEEK-001 | Seed of the week per ISO week, button in the start-run modal | `Assets/Scripts/Run/WeeklySeedService.cs`, `MainMenuController.StartRun.cs` | `SeedFor`, `CreateWeeklySeedButton` | WalkCalendarParityTests | ✅ |
| RUN-WEEK-002 | Discord posts compute daily goals, weekly seed and monthly board like the game | `tools/discord/walk_calendar.py`, `walk_posts.py` | `daily_goal_ids`, `weekly_seed`, `monthly_walk` | WalkCalendarParityTests, test_walk_calendar.py, live posts 2026-10-15 | ✅ |

---

## Credits

**Spec:** `docs/GameDesignSpec.md` v1.10 §Credits

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| CREDITS-COMMUNITY-001 | Community playtesters and translators and the Discord link in the credits | `Assets/Scripts/UI/CommunityCredits.cs`, `MainMenuBlueprintBuilder.BuildCreditsPanel` | `Load`, `PlaytesterLine`, `TranslatorLine` | CommunityCreditsTests | ✅ |

---

## In-Run Visual Style

**Spec:** `docs/design/layout-proposals.md` v1.2 §Decisions (owner decisions 2026-10-15)

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| UI-STYLE-001 | Garden path as an ink hand scroll, the same on all five floors | — | — | mockups `path_b_hand_scroll_floor1..5` | ◻️ |
| UI-STYLE-002 | Puzzle screen as a hanging scroll (ink grid, lanterns, ema plaques, wooden tiles, paper slips), 5×5 to 9×9 | — | — | mockups `puzzle_b_hanging_scroll`, `_9x9` | ◻️ |
| UI-STYLE-003 | Background paintings keep their corner mark | `Assets/Resources/background/` | — | owner decision | ✅ |
| UI-STYLE-004 | Boss gate as a trial scroll (seal, ink trial panels with ensō ticks, curse margin note, "Face the boss") | `Assets/Scripts/UI/BossGateViewController.cs`, `ScrollUiKit.cs`, `FloorThemeData.cs` | `BuildBossGatePanel`, `BuildTrialCard`, `RefreshTrialMarks` | InRunUiPlayModeTests (`BossGate_PickingTrials_TicksThem_AndConfirmRecordsThem`, intensity/debuff, masking), screenshots `after_boss_gate*` | ✅ |
| UI-STYLE-005 | Boss defeated as an offering table with the relic choice on the same screen | `Assets/Scripts/UI/EndScreenViewController.cs`, `InRunController.Completion.cs`, `RewardViewController.AcceptRelic` | `ShowBossCleared`, `BuildOffering` | BossClearedFlowPlayModeTests (take, leave, no offerings), screenshot `after_boss_defeated` | ✅ |
| UI-STYLE-006 | A killer cage is one dashed outline around all its cells | `Assets/Scripts/UI/CageOutline.cs`, `BoardViewController.DrawCageOutline`, mockups `src/common.js` | `Build`, `Dashes` | CageOutlineTests (every shape up to 4 cells is one closed loop), screenshot `after_cages` | ✅ |

---

## Localization

**Spec:** `docs/localization-catalog.md` v1.1 §French, Spanish and Brazilian Portuguese

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| LOC-PARITY-001 | fr/es/pt contain every German key | `Assets/Resources/Localization/*.json` | catalogs | LocalizationParityTests | ✅ |
| LOC-PARITY-002 | Same placeholders as German, no empty text | `Assets/Resources/Localization/*.json` | catalogs | LocalizationParityTests | ✅ |
| LOC-PARITY-003 | Gameplay content never falls back to English in fr/es/pt | `Assets/Scripts/Core/LocalizationService.cs` | `T` | LocalizationParityTests | ✅ |

---

## Audio

**Spec:** `docs/AudioVisualDirection.md` v1.6 §8-bit soundtrack; prompts `docs/audio-8bit-prompts.md`

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| AUDIO-MUSIC-001 | 8-bit direction and track list | `docs/audio-8bit-prompts.md` | — | owner listening check | ◻️ |
| AUDIO-MUSIC-002 | Delivery format, loudness, loop rules | `docs/audio-8bit-prompts.md` | — | on delivery | ◻️ |
| AUDIO-MUSIC-003 | Boss puzzle plays the stage boss track in place of the stage track | `Assets/Scripts/UI/RunAudioController.cs` | `StartBossLayer`, `StopBossLayer` | AudioClipPlayModeTests | ✅ |
| AUDIO-MUSIC-004 | Music imports as Streaming + Vorbis | `Assets/Resources/audio/music/*.meta` | import settings | on delivery | ◻️ |
| AUDIO-MUSIC-005 | Mode tracks (walks, Endless Zen, Spirit Trials, custom puzzles) with stage-track fallback | `Assets/Scripts/UI/RunAudioController.cs` | `SetMode`, `AudioAssetService.GetModeLoopId` | AudioAssetServiceTests, AudioClipPlayModeTests | ✅ |
| AUDIO-MUSIC-006 | Victory/defeat end-screen tracks after the stinger | `Assets/Scripts/UI/RunAudioController.cs` | `PlayRunEndMusic` | runtime check once the files exist | ◻️ |
| AUDIO-MUSIC-007 | Credits track with menu-track return | `Assets/Scripts/UI/MenuMusicController.cs` | `SetCreditsActive` | AudioClipPlayModeTests | ✅ |

---

## Review-Created Requirements

**Source:** `docs/review/REQUIREMENT_TRACEABILITY.md`. The 2026-10-01 review created `REQ-<SYS>-1xx` IDs for requirements that had no ID (numbered from 100 so they cannot collide with the legacy `REQ-*-0xx` IDs). IDs added after the review continue the numbering.

| ID | Description | Source / File | Code / Method | Evidence | Status |
| ---- | ------------- | --------------- | --------------- | ---------- | -------- |
| REQ-AUDIO-100 | Menu/garden/puzzle/boss music and SFX play | AudioVisualDirection §Sound Design | Authored clips load (logged); music loops are 6.0 s mono | AudioAssetServiceTests, AudioClipPlayModeTests | ⚠️ |
| REQ-AUDIO-101 | Volume sliders and mute toggles work | GDS §14 Options | OptionsController | AudioClipPlayModeTests, OptionsPersistenceTests | ◻️ |
| REQ-BOSS-100 | Boss gate shows/picks per floor table (F1 3/2 … F5 6/5) | BossMechanicsSystem §Choice Scaling | GetBossModifierCounts floor+1 options / floor picks; spec table updated | BalanceBudgetContractTests | ✅ |
| REQ-BOSS-101 | Every chosen boss modifier is active and visible in the boss puzzle | BossMechanicsSystem §Flow step 7 | Chosen boss modifiers are mandatory through the fallback chain | GenerationReliabilityTests, PuzzleGenerationServiceTests, RunFlowAcceptanceTests | ✅ |
| REQ-BOSS-102 | Unseen modifiers display '???' name and rule | BossMechanicsSystem; PathSystem §Unknown Modifiers | BossGateViewController | ModifierDiscoveryPersistenceTests | ✅ |
| REQ-BOSS-103 | Boss gate title shows intensity label | BossMechanicsSystem §Intensity | GateInfo: intensity label | InRunUiPlayModeTests | ✅ |
| REQ-BOSS-104 | Collapsible modifier legend with tap-to-isolate | BossMechanicsSystem §Modifier Legend Panel | HudViewController.Legend + BoardViewController.OverlayGroups (tap isolates for 3 s) | InRunUiPlayModeTests | ✅ |
| REQ-BOSS-105 | Boss board size stays within the floor's grid range | GDS §10 Floor Themes | Boss board follows the floor range (no Max(size, 8)) | NodeConfigPreviewTests | ✅ |
| REQ-BOSS-106 | Modifier minimum board sizes follow the spec | BossMechanicsSystem v2.2 | Modifier minimum sizes documented from BossService.ModifierData | BalanceBudgetContractTests | ✅ |
| REQ-BOSS-107 | After a non-final boss the player can claim the relic and continue | PathSystem §Floor Transition | EndScreenViewController.ShowBossCleared: offering table with the relic choice on one screen (UI-STYLE-005), Take/Leave → next floor | BossClearedFlowPlayModeTests, GeneratedUiWiringContractTests | ✅ |
| REQ-BOSS-108 | Boss debuffs have a defined delivery path in Garden Run | BossDebuffEffects (undefined) | One debuff per Garden Run boss from Floor 3 (seed + floor) | BossDebuffTests, InRunUiPlayModeTests | ✅ |
| REQ-CLASS-100 | Class select shows base + progression stats (e.g. HP 10 (+5)) | Review brief §13 Classes | Class info shows base (+level bonus) | ClassRosterTests | ✅ |
| REQ-CLASS-101 | Class base stats match the roster | GDS §6.1 vs ClassXp §Base Stats | ClassCatalog roster = ClassXpProgressionSystem 1.3 (generated) | ClassRosterTests | ✅ |
| REQ-CLASS-102 | Level unlock schedule applied per spec | ClassXpProgressionSystem | RunArchetypeService.GetLevelBonuses; Number Freak L3 typo fixed (GAP-041) | ClassRosterTests | ✅ |
| REQ-CLASS-103 | Class passives behave as specified | GDS §6.1 / ClassXp | Passives as documented | ClassRosterTests, GameplayEffectContractTests | ✅ |
| REQ-ECON-100 | Pencil can be bought during a level (20+20n) | GDS §5.2/§5.3 | Buy Pencil button: +3 for 20 + 20n gold; not for Garden Monk | EconomyActionTests, GeneratedUiWiringContractTests | ✅ |
| REQ-ECON-101 | 'Collect 50,000 gold' counts gold collected | ClassXpProgressionSystem unlocks | RunState.GoldCollected | EconomyActionTests | ✅ |
| REQ-HARM-100 | Harmony grid/star weights shape generated levels | HarmonyDifficultySystem §4.5/4.6 | Grid/star weights read by BuildLevelConfig (renormalised within the floor range) | HarmonyEffectsTests | ✅ |
| REQ-HARM-101 | Harmony mistake HP cost / gold multiplier / combo decay / flags apply | HarmonyDifficultySystem §4.1-4.3 | HP cost, gold multiplier, combo decay, flags read | HarmonyEffectsTests | ✅ |
| REQ-ITEM-100 | Item reward reroll of eligible slots (20+20n) | GDS §8 | RerollRewardSlots wired: tokens first, then 20 + 20n gold | EconomyActionTests, GeneratedUiWiringContractTests | ✅ |
| REQ-ITEM-101 | Full bag: replace existing item or choose Nothing | GDS §8 | RewardView/ShopView replace flow | DeterminismAcrossResumeTests | ✅ |
| REQ-ITEM-102 | Items & Relic Codex lists discovered entries | ItemsAndRelicsSystem §Codex | ItemsMenuController | — | ◻️ |
| REQ-ITEM-103 | Item effects apply as described | ItemsAndRelicsSystem | ApplyItemEffect; Item Block checked before the effect (GAP-040) | GameplayEffectContractTests | ⚠️ |
| REQ-ITEM-104 | Accepted relic choice adds the relic | ItemsAndRelicsSystem §Acquisition | AcceptRelicChoice adds the relic; options persisted | DeterminismAcrossResumeTests, GameplayEffectContractTests | ✅ |
| REQ-MODE-100 | Endless Zen levels generate reliably | EndlessZenMode | BossService pool + generator budgets | GenerationReliabilityTests | ✅ |
| REQ-MODE-101 | Endless/Spirit draw only functional player modifiers | EndlessZenMode; SpiritTrialsMode | GetGeneratablePlayerModifiers + DrawCombinable | GenerationReliabilityTests | ✅ |
| REQ-MODE-102 | Spirit Trials sessions generate reliably | SpiritTrialsMode | SpiritTrialsService | GenerationReliabilityTests | ✅ |
| REQ-PATH-100 | Player marker visible on the garden path | PathSystem §Player Icon | InRunController.PathTiles AddPlayerMarker | InRunUiPlayModeTests | ✅ |
| REQ-PATH-101 | Map tile preview (size/stars) matches the generated puzzle | PathSystem §Tile Types | Node configs built once per floor and stored in RunState; preview = generated | NodeConfigPreviewTests | ✅ |
| REQ-PATH-102 | Puzzle board sizes stay within floor ranges | GDS §3.1/§10 | Sizes stay within the floor range | NodeConfigPreviewTests | ✅ |
| REQ-PATH-103 | Graph connectivity: all nodes reachable and lead to boss | PathSystem §Path Structure | RunGraphService | E2E/diagnostic D06 | ✅ |
| REQ-PATH-104 | Last tile of each route before boss is Elite/PreBoss | PathSystem §Elite | RunGraphService | E2E/diagnostic D06 | ✅ |
| REQ-PERF-100 | Boss/puzzle generation wait stays within budget (≤6 s) | performance-generation-budget.md | Async budgets + fallbacks + faster generator | GenerationReliabilityTests, PerformanceReadinessTests | ✅ |
| REQ-RUN-100 | Run start allows random or entered seed | GDS §14 Start Game Flow | Class select seed field; LaunchRequest.HasSeed/Seed | RunSeedAndAbandonTests | ✅ |
| REQ-RUN-101 | In-run menu: Resume/Options/View Modifiers/View Seed/Save&Quit/Abandon | GDS §14 Pause Menu | In-run menu: seed, View Modifiers, Abandon Run (two-step) | InRunUiPlayModeTests, RunSeedAndAbandonTests | ✅ |
| REQ-RUN-102 | Same seed reproduces path, puzzles, modifiers and rewards | SudokuGenerationPipeline §Seeding | Seeded services | DeterminismAcrossResumeTests | ✅ |
| REQ-RUN-103 | Save/resume preserves seed determinism (no re-rolls) | SudokuGenerationPipeline §Seeding (save/resume) | Per-decision seeds (DecisionSeed); rolled options persisted | DeterminismAcrossResumeTests | ✅ |
| REQ-RUN-104 | Rest node: +2 HP / +3 Pencil / +1 Reroll | GDS §9b | Rest: heal MaxHP/3, +4 Pencil, +1 Reroll, Cleanse (spec aligned to code) | RestNodeValuesTests | ✅ |
| REQ-RUN-105 | Run fails at HP 0 and awards XP | GDS §2 | UI CheckGameOver | — | ◻️ |
| REQ-RUN-106 | Star cap for early floors: non-boss puzzles on floors 1–2 are at most one star above the floor default (GDS §15 guardrail, GAP-033) | `Assets/Scripts/Run/RunDirector.cs` | `RollStars`, `MaxStarsForFloor` | HarmonyEffectsTests | ✅ |
| REQ-SAVE-100 | Resuming mid-puzzle restores a playable puzzle (rules, overlay, pressure, mistakes) | SaveLoadArchitecture §Resume Flow | PuzzleSaveState level config/overlay/counters + RestoreLevelRulesAfterResume | ResumeMidPuzzleTests | ✅ |
| REQ-SAVE-101 | Mistakes/HP loss are persisted immediately | SaveLoadArchitecture §Autosave | Autosave after every mistake | PlacementRulesTests | ✅ |
| REQ-SAVE-102 | Automated tests never touch real player save slots | unity-testing conventions | SaveFileService.RootDirectoryOverride via [SetUpFixture] | EditModeTestSaveIsolation, PlayModeTestSaveIsolation, SaveDurabilityReadinessTests | ✅ |
| REQ-SUD-100 | Boards 5x5–9x9 generate with valid solution and regions | GameDesignSpec §3 | SudokuGenerator | PuzzleGenerationServiceTests | ✅ |
| REQ-SUD-101 | Every reachable puzzle configuration generates within the UI time budget (no permanent failure) | SudokuGenerationPipeline; performance-generation-budget | Generator node budgets + restarts; CreateBoardWithFallbacks; salted retry | GenerationReliabilityTests, PuzzleGenerationServiceTests | ✅ |
| REQ-SUD-102 | Puzzle uniqueness policy is explicit and fair to the player | SudokuGenerationPipeline §Solvability | CreatePuzzleWithUniquenessCheck for every board; modifier geometry derived from the solution | PuzzleGenerationServiceTests; SudokuBacktrackingSolverTests | ✅ |
| REQ-SUD-103 | A wrong placement is defined consistently (solution vs rule conflict) and costs HP | GDS §5.1; BossMechanics §Validation; REQ MAP GEN-VALID-001 | Solution-based wrong digit, rejected, penalty in RunDirector | PlacementRulesTests | ✅ |
| REQ-SUD-104 | Pencil marks consume pencil units; blocked at 0 | GDS §5.2 | TryAddPencilMark | — | ◻️ |
| REQ-SUD-105 | Undo is not available (removed in GDS v1.4) | GDS v1.4 change history | Undo binding and code path removed | PlacementRulesTests | ✅ |
| REQ-SUD-106 | Selection controls (click/drag, CTRL multi-select, arrows) | GDS §4.1 | Single-cell selection (click, arrows, stick); multi-cell → FUTURE-011 | InRunUiPlayModeTests | ✅ |
| REQ-TEST-100 | tools/run-unity-tests.ps1 executes the Unity test suites | development.md; ci-validation.md | tools/run-unity-tests.ps1 (no -quit, summary, filters) | — | ◻️ |
| REQ-TEST-101 | EditMode/PlayMode suites pass | smoke-test-readiness.md | — | RequirementTraceabilityTests | ✅ |
| REQ-TUT-100 | Custom puzzle supports every size/star/variant combination | TutorialModeSystem §Setup | Generator budgets; structural modifiers use rectangular regions | E2E/diagnostic D14 | ✅ |
| REQ-TUT-101 | Tutorial/custom puzzles grant no progression | TutorialModeSystem §Purpose | DisableProgressionRewards | RunFlowAcceptanceTests | ✅ |
| REQ-UI-100 | First launch shows profile select → first-run setup → basics prompt | startup-experience.md | Startup flow keeps Profile Select / first-run setup | FirstRunSetupPlayModeTests, StartupFlowPlayModeTests | ✅ |
| REQ-UI-101 | All shipped languages render correctly (no mojibake, no untranslated UI) | localization-catalog.md | de.json repaired + encoding guard; fr/es/pt lack 431 gameplay keys each (English fallback) | LocalizationCatalogQualityTests | ⚠️ |
| REQ-UI-102 | Language choice applies immediately to all UI | GDS v1.3 change history | Menu rebuild reuses its canvas | GeneratedUiPlayModeTraversalTests | ⚠️ |
| REQ-UI-103 | Readable UI: locked classes, toggle states, no overlap/clipping | AudioVisualDirection; AccessibilitySpec | Toggle boxes, digit styles, locked-class contrast, tooltip leak, label fitting; in-game Options restyle open | GeneratedUiPlayModeTraversalTests | ⚠️ |
| REQ-UI-104 | Failed puzzle generation is handled without trapping the player | UnityImplementationSpec (failure state) | LaunchSeasonalChallenge returns false and shows Seasonal.Status.GenerationFailed | E2E/diagnostic F02 | ⚠️ |
| REQ-UI-105 | Main menu stays responsive after returning from a run | UI flow | — | E2E/diagnostic F01, F03 | ✅ |
| REQ-UI-106 | Daily/Monthly widgets prominent on main menu | DailyGoalSystem; SeasonalChallengeSpec | Daily/Monthly widgets on the main menu | GeneratedUiPlayModeTraversalTests | ✅ |
| REQ-UI-107 | Boss-cleared screen shows stats and a labelled Continue | PathSystem §Floor Transition | EndScreenViewController.ShowBossCleared (font, localization, stats, focus) | BossClearedFlowPlayModeTests, GeneratedUiWiringContractTests | ✅ |
| REQ-XP-100 | End-of-run breakdown labels each tile with size/stars/boss | ClassXp §End-of-Run | TileXpEntry metadata set | CompletionTrackingTests, RunFlowAcceptanceTests | ✅ |
| REQ-XP-101 | XP beyond L40 is discarded | ClassXp §Excess XP | XP capped at the L40 total | HarmonyEffectsTests | ✅ |
| REQ-XP-102 | Game-over XP bar animation and level-up display | ClassXp §End-of-Run | EndScreenPresenter | — | ◻️ |

