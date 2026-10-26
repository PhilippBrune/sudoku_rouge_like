# Requirement Traceability Map — All Systems

**Updated:** 2026-10-26 (ACHIEVE-RUN-001, ACHIEVE-CLASS-001, ACHIEVE-MODE-001, ACHIEVE-COLLECT-001; before: INPUT-PAD-001 (GAP-070); before: HARMONY-PERK-002..005 (GAP-064, GAP-067, GAP-069), LEGAL-LICENSE-001 (GAP-068), INPUT-NAV-001 (GAP-062), the one-more-run slice RUN-AGAIN-001, RUN-END-NEAR-001, RUN-END-MOVED-001, STAKES-001, RUN-LENGTH-001, META-CODEX-010; before: UI-STYLE-024..026, LOC-NUM-001, ACCESS-MOTION-002)

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
| [UI-STYLE](#in-run-visual-style) | `docs/design/layout-proposals.md` | ✅ 9 |
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

**Spec:** `docs/MetaProgressionSystem.md` v1.12

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| META-UNLOCK-001 | 8 classes with cumulative cross-class unlock conditions | `Assets/Scripts/Core/RuntimeModels.cs` | `ClassUnlockProgress` | EconomyActionTests.CollectedGold_CountsGains_EvenAfterSpending | ✅ |
| META-UNLOCK-002 | `RecordRunAndGetNewUnlocks()` updates counters + `EvaluateUnlocks()` | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | E2E/diagnostic D09 | ✅ |
| META-UNLOCK-003 | Unlocked classes: full stat block + passive + level | `Assets/Scripts/UI/InRunController.cs` | Class select panel | ModifierDiscoveryPersistenceTests | ✅ |
| META-UNLOCK-004 | Locked classes: greyed out + unlock condition text | `Assets/Scripts/UI/InRunController.cs` | Class select panel | — | ⚠️ |
| META-UNLOCK-006 | Each locked class shows its unlock goal with its progress on its plaque and scroll; never more than the goal; the same thresholds as `CheckNewUnlocks` | `Assets/Scripts/Classes/ClassUnlockService.cs`, `UI/MetaProgressionPanelController.cs` | `GetProgress`, `GoalText` | ClassGardenDataTests (5 progress tests), ClassGardenPlayModeTests.Plaques_ShowLevelsForUnlockedClassesAndGoalsForLockedOnes | ✅ |
| META-STATS-001 | Garden Runs and best floor per class (Garden Runs only; from 2026-10-21, old saves start at 0) | `Assets/Scripts/Meta/ClassGardenProgressionService.cs`, `Save/ProfileService.cs`, `Run/RunResultFactory.cs`, `Core/SaveModels.cs` | `RecordGardenRun`, `RecordRunAndGetNewUnlocks`, `RunResult.FloorReached`, `ClassGardenProgressEntry.GardenRuns/BestFloor` | ClassGardenDataTests (runs and floor), ClassGardenPlayModeTests.Scroll_ShowsTheChosenClassWithRunsAndItsLevelPath | ✅ |
| META-XP-006 | 12 milestone levels per class (`ClassCatalog.GetAllUnlocks`), stat bonuses applied by `GetLevelBonuses`, shown in the player's language | `Assets/Scripts/Classes/ClassCatalog.cs`, `UI/ClassUnlockText.cs`, `UI/MainMenuClassInfoPresenter.cs`, `UI/MetaProgressionPanelController.Scroll.cs` | `GetAllUnlocks`, `Localize`, `PathRows` | ClassRosterTests, ClassGardenDataTests (schedule texts in every language, passive values), ClassGardenPlayModeTests | ✅ |
| META-XP-007 | From Level 30 every run with the class starts with its item (GAP-063; Golden Koi, Rice Paper Umbrella, Temple Incense replace three items that did not exist) | `Assets/Scripts/Classes/ClassCatalog.cs`, `Run/RunArchetypeService.cs` | `GetStartingItem`, `StartingItemLevel`, `AddStartingItem` | ClassLevelRewardTests (4) | ✅ |
| META-XP-008 | At Level 40 the class's frame: a band in its colour round its plaque in the garden of classes and a ring round its badge in a run; kept after prestige (GAP-063) | `Assets/Scripts/UI/ClassFrames.cs`, `UI/MetaProgressionPanelController.cs`, `UI/HudViewController.cs`, `UI/ScrollUiKit.Scenes.cs` | `IsUnlocked`, `Colour`, `EmaFrame`, `Ring`, `ApplyClassFrame` | ClassLevelRewardTests (2), screenshot `after_meta_progression` (Number Freak after a prestige) | ✅ |
| META-XP-001 | Per-class `TotalXp` stored; level derived via `DeriveLevel()` | `Assets/Scripts/Core/SaveModels.cs` | `ClassGardenProgressEntry` | E2E/diagnostic D08, D09 | ✅ |
| META-XP-002 | Two-phase XP curve L1–15 + L16–40 | `Assets/Scripts/Economy/XpTable.cs` | `XpTable.DeriveLevel` | XpTableTests | ✅ |
| META-XP-003 | Total to L40: 16,860 XP | `Assets/Scripts/Economy/XpTable.cs` | `XpTable.CumulativeXpForLevel(40)` | XpTableTests | ✅ |
| META-XP-004 | XP committed at run end (no mid-run leveling) | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | E2E/diagnostic D09 | ✅ |
| META-PRESTIGE-001 | Max prestige tier 9 per class | `Assets/Scripts/Core/SaveModels.cs` | `ClassGardenProgressEntry.PrestigeTier` | PrestigeTests | ✅ |
| META-PRESTIGE-002 | Prestige requires Level 40 | `Assets/Scripts/Save/ProfileService.cs` | Prestige gate check | PrestigeTests | ✅ |
| META-PRESTIGE-003 | Boss gate: `3 × (PrestigeTier + 1)` boss defeats | `Assets/Scripts/Save/ProfileService.cs` | Prestige gate check | PrestigeTests | ✅ |
| META-PRESTIGE-004 | On prestige: TotalXp=0, PrestigeTier++ | `Assets/Scripts/Save/ProfileService.cs` | `TryPrestige` | PrestigeTests | ✅ |
| META-PRESTIGE-005 | Prestige reward: the badge plus +1 starting gold per tier for runs with that class, at most +9 (SPEC-CONF-039, owner 2026-10-20) | `Assets/Scripts/Meta/ClassGardenProgressionService.cs`, `Run/RunArchetypeService.cs`, `Bootstrap/GameBootstrap.cs` | `PrestigeStartingGold`, `CreateRunState`, `LaunchRequest.ClassPrestigeTier` | PrestigeTests.PrestigeTier_AddsOneStartingGoldPerTier | ✅ |
| META-ASCEND-001 | Ascension is a global layer above per-class prestige — retired (owner 2026-10-20, GAP-017): Harmony and prestige cover it; the save fields stay unused | `Assets/Scripts/Core/SaveModels.cs` | `MetaProgressionState.AscensionLevel` (unused) | — | 🗄️ Descoped |
| META-ASCEND-002 | `ApplyAscension`: +1 AscensionLevel, +1 MaxStarCap — retired with Ascension (GAP-017) | — | — | — | 🗄️ Descoped |
| META-ASCEND-003 | `TryPrestigeReset`: global prestige reset — retired with Ascension (GAP-017) | — | — | — | 🗄️ Descoped |
| META-ASCEND-004 | Monthly seed: `(year × 100) + month` | `Assets/Scripts/Run/SeasonalChallengeService.cs` | `BuildSeed` (seed only; no ascension) | SpecAlignmentTests | ✅ |
| META-COMPLETE-001 | `CompletionService.Recalculate()` — 4 checks × 25% | `Assets/Scripts/Meta/CompletionService.cs` | `Recalculate` | CompletionTrackingTests | ✅ |
| META-CODEX-001 | Item & Relic Codex tracks discovery | `Assets/Scripts/Save/ProfileService.cs` | `RecordItemDiscovery`, `RecordRelicDiscovery` | AutosaveMergeTests | ✅ |
| META-CODEX-002 | Entry marked Discovered on first acquisition only | `Assets/Scripts/Save/ProfileService.cs` | `RecordItemDiscovery` | AutosaveMergeTests | ✅ |
| META-CODEX-006 | Full discovery of the items (without the counter items) unlocks the Reed Duelist; the Codex's goal and the class garden count the same way | `Assets/Scripts/Meta/CodexProgress.cs`, `Save/ProfileService.cs` | `CodexProgress.RequiredItems`, `ItemCodexComplete` | ClassGardenDataTests.ReedDuelistProgress_CountsTheFoundItemsWithoutTheCounterItems, CounterItemTests | ✅ |
| META-CODEX-005 | Undiscovered entries show "???" with a silhouette icon (the Codex book's tiles and right page, UI-STYLE-018) | `Assets/Scripts/UI/ItemsMenuController.cs` | `BuildTiles`, `ShowDetail` (`Silhouette`, `Codex.Unknown`; an unknown entry hides its kind too) | CodexBookPlayModeTests.UnknownEntries_ShowQuestionMarksAndSilhouettesAndHideTheirKind, CursesPage_CountsFiltersAndShowsTheChosenCurse | ✅ |
| META-CODEX-008 | Curses met in any run (taken, also when cleansed later) are recorded for the Codex's Curses page; merged at the run's end and on every autosave; old saves start with none, ids this build does not know are kept | `Assets/Scripts/Run/CurseService.cs`, `Save/ModifierDiscoveryService.cs`, `Save/ProfileService.cs`, `Save/RunAutoSaveCoordinator.cs`, `Core/SaveModels.cs`, `Core/RuntimeModels.cs` | `ApplyCurse` (`RunState.CursesSeenThisRun`), `MergeCursesIntoMeta`, `SanitizeMetaProgress`, `MetaProgressionState.DiscoveredCurses` | ModifierDiscoveryPersistenceTests (6 curse tests), CodexBookPlayModeTests | ✅ |
| META-CODEX-009 | Each item's Codex entry counts how often it was taken (every pickup from a reward or the shop) and used (counted once, at the run's end; a use never reveals an item the Codex has not found) | `Assets/Scripts/Save/ProfileService.cs`, `Run/RunDirector.cs`, `Core/SaveModels.cs`, `Core/RuntimeModels.cs`, `UI/ItemsMenuController.cs` | `RecordItemDiscovery` (`TimesFound`), `TryUseItem` (`RunState.ItemTypesUsedThisRun`), `CountItemUses` (`TimesUsed`), `AddItems` | ModifierDiscoveryPersistenceTests (4 count tests), GameplayEffectContractTests.TryUseItem_RecordsTheItemTypeForTheCodex, CodexBookPlayModeTests.ItemsPage_ShowsHowOftenTheItemWasFoundAndUsed | ✅ |
| META-CODEX-010 | Codex Items page: the rarity filter (Every rarity · Normal · Rare · Epic) and "Best paired with" (the class whose Level 30 runs start with the item, or that it is exclusive to) | `Assets/Scripts/UI/ItemsMenuController.cs`, `MainMenuBlueprintBuilder.cs` | `CycleRarity`, `Shown`, `BestPairedWith` | CodexBookPlayModeTests (`ItemsPage_FiltersByRarity_AndNamesTheClassAnItemBelongsTo`), screenshot `after_codex_rarity` | ✅ |

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
| GEN-MOD-002 | Catalog batch B1/B2/B3/B6 geometry is read off the solution (lines never cross themselves, GAP-050): lines grown with a per-type check, zipper and palindrome grown from the middle, complete dot sets last, pairs, markers and mirror pairs | `Assets/Scripts/Sudoku/ModifierGeometryGenerator.Catalog.cs` | `GrowLineSet`, `TryGrowZipper`, `GenerateFullKropkiSet`, `GenerateDifferenceKPairs`, `GenerateMultipleCellMarkers`, `GenerateMirrorCellPairs` | CatalogModifierTests | ✅ |
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
| MAP-NODE-CURSED-004 | `CursedPuzzlesAccepted` tracked for run score breakdown (accepted and completed, GAP-054) | `Assets/Scripts/Run/RunDirector.cs`, `RunResultFactory.cs`, `RunScoreBreakdown.cs` | `CompleteLevelAndGrantRewards`, `RecordCursedPuzzleCompleted`, `Build` | RunScoreBreakdownTests (`ClearedCursedPuzzle_CountsForTheRunScore`) | ✅ |
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
| BOSS-MOD-001 | 43 player modifiers (`BossModifierId` 0–29 and the catalog batch 38, 40–44, 51, 52, 58–60, 74, 76); IDs ≥ 94 are boss debuffs (SPEC-CONF-019, BossMechanicsSystem 2.8) | `Assets/Scripts/Core/GameEnums.cs` | `BossModifierId` | GenerationReliabilityTests | ✅ |
| BOSS-MOD-002 | `BossService.ModifierData` — tier/impact/name/description per modifier | `Assets/Scripts/Boss/BossService.cs` | `ModifierData` | BalanceBudgetContractTests, GenerationReliabilityTests | ✅ |
| BOSS-MOD-003 | `ModifierGeometryGenerator.Generate` produces overlay for each modifier | `Assets/Scripts/Sudoku/ModifierGeometryGenerator.cs` | `Generate` | BalanceBudgetContractTests.AntiBishop_IsEligibleFromSixBySix; GenerationReliabilityTests | ✅ |
| BOSS-MOD-004 | A chosen modifier counts as present only with its own geometry (per line, dot, pair, cage, arrow, marker and cell-pair type); the plain Killer and Arrow rules ignore other subtypes (GAP-047) | `Assets/Scripts/Sudoku/ModifierPresence.cs`, `PuzzleGenerationService.cs`, `RunDirector.cs`, `ConstraintRules.cs` | `ModifierPresence.IsPresent`, `FindMissingModifiers`, `KillerCagesRule`, `ArrowSumsRule` | ModifierPresenceTests | ✅ |
| BOSS-MOD-005 | Every playable modifier is complete in the catalog: name, rule text (the new batch in all five catalogs), unique abbreviation, icon, legend layer and colour | `Assets/Scripts/Boss/BossService.cs`, `UI/BoardViewController.OverlayGroups.cs` | `GetModifierName`, `GetModifierDescription`, `GetConstraintAbbreviation`, `GetOverlayGroupKeys`, `TryGetLegendColor` | CatalogModifierTests.EveryPlayableModifier_HasNameRuleAbbreviationIconAndLegend | ✅ |
| BOSS-MOD-006 | Rule soundness: a modifier's rules never reject the solution while it is filled in, and catch a wrong digit | `Assets/Scripts/Sudoku/ConstraintRules*.cs` | `IsValid` | CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-038 | Nonconsecutive Line: neighbours on the line are never consecutive (lime, ink crosses) | `Assets/Scripts/Sudoku/ConstraintRules.LineVariants.cs`, `ModifierGeometryGenerator.Catalog.cs` | `NonconsecLineRule` | CatalogModifierTests (`NonconsecutiveLine_RejectsConsecutiveNeighboursOnly`, `EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule`) | ✅ |
| BOSS-LIB-040 | High-Low Line: digits alternate between the low and the high half (≤ midpoint, e.g. 1–5 on 9×9) | `Assets/Scripts/Sudoku/ConstraintRules.LineVariants.cs`, `ModifierGeometryGenerator.Catalog.cs` | `HighLowLineRule` | CatalogModifierTests (`HighLowLine_AlternatesHalves_FiveCountsAsLowOn9x9`, `EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule`) | ✅ |
| BOSS-LIB-041 | Nabner Line: no two digits on the line repeat or are consecutive (red) | `Assets/Scripts/Sudoku/ConstraintRules.LineVariants.cs`, `ModifierGeometryGenerator.Catalog.cs` | `NabnerLineRule` | CatalogModifierTests (`NabnerLine_RejectsRepeatsAndConsecutiveAnywhereOnTheLine`, `EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule`) | ✅ |
| BOSS-LIB-042 | Entropic Line: any three neighbours hold one of 1–3, 4–6, 7–9 (peach, 7×7 and up) | `Assets/Scripts/Sudoku/ConstraintRules.LineVariants.cs`, `ModifierGeometryGenerator.Catalog.cs` | `TriadLineRule(EntropicLine)` | CatalogModifierTests (`TriadLines_GroupRepeatsEveryThirdCell`, `EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule`) | ✅ |
| BOSS-LIB-043 | Modular Line: any three neighbours hold one of each remainder mod 3 (teal) | `Assets/Scripts/Sudoku/ConstraintRules.LineVariants.cs`, `ModifierGeometryGenerator.Catalog.cs` | `TriadLineRule(ModularLine)` | CatalogModifierTests (`TriadLines_GroupRepeatsEveryThirdCell`, `EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule`) | ✅ |
| BOSS-LIB-044 | Zipper Line: cells the same distance from the middle sum to the same total, the middle cell holds it (lavender) | `Assets/Scripts/Sudoku/ConstraintRules.LineVariants.cs`, `ModifierGeometryGenerator.Catalog.cs` | `ZipperLineRule` | CatalogModifierTests (`ZipperLine_PairsAddUpToTheMiddle`, `EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule`) | ✅ |
| BOSS-LIB-051 | Fast Thermo: each digit at least 2 above the one before (double bulb) | `Assets/Scripts/Sudoku/ConstraintRules.LineVariants.cs`, `ModifierGeometryGenerator.Catalog.cs` | `FastThermoRule` | CatalogModifierTests (`FastThermo_ClimbsByAtLeastTwo_AndLeavesRoomAtTheEnds`, `EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule`) | ✅ |
| BOSS-LIB-052 | Ambiguous Thermo: strictly increasing in one unknown direction (bars at both ends) | `Assets/Scripts/Sudoku/ConstraintRules.LineVariants.cs`, `ModifierGeometryGenerator.Catalog.cs` | `AmbiguousThermoRule` | CatalogModifierTests (`AmbiguousThermo_AcceptsEitherDirectionUntilTheDigitsDecide`, `EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule`) | ✅ |
| BOSS-LIB-058 | Full White Kropki: every difference-1 neighbour pair has a white dot; no dot forbids it | `Assets/Scripts/Sudoku/ConstraintRules.DotsPairsCells.cs`, `ModifierGeometryGenerator.Catalog.cs` | `FullKropkiSetRule(false)` | CatalogModifierTests (`FullKropkiSets_ForbidTheRelationWhereTheDotIsMissing`, `EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule`) | ✅ |
| BOSS-LIB-059 | Full Black Kropki: every 1:2 neighbour pair has a black dot; no dot forbids it | `Assets/Scripts/Sudoku/ConstraintRules.DotsPairsCells.cs`, `ModifierGeometryGenerator.Catalog.cs` | `FullKropkiSetRule(true)` | CatalogModifierTests (`FullKropkiSets_ForbidTheRelationWhereTheDotIsMissing`, `EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule`) | ✅ |
| BOSS-LIB-060 | Difference ≥ 3: cells joined by a plum bar differ by at least 3 | `Assets/Scripts/Sudoku/ConstraintRules.DotsPairsCells.cs`, `ModifierGeometryGenerator.Catalog.cs` | `DifferenceKPairRule` | CatalogModifierTests (`DifferenceMultiplesAndMirror_CheckTheirCells`, `EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule`) | ✅ |
| BOSS-LIB-074 | Multiples of 3: marked cells hold 3, 6 or 9 | `Assets/Scripts/Sudoku/ConstraintRules.DotsPairsCells.cs`, `ModifierGeometryGenerator.Catalog.cs` | `MultipleCellsRule` | CatalogModifierTests (`DifferenceMultiplesAndMirror_CheckTheirCells`, `EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule`) | ✅ |
| BOSS-LIB-076 | Mirror Cells: same-letter cells mirrored through the centre hold the same digit | `Assets/Scripts/Sudoku/ConstraintRules.DotsPairsCells.cs`, `ModifierGeometryGenerator.Catalog.cs` | `MirrorCellsRule` | CatalogModifierTests (`DifferenceMultiplesAndMirror_CheckTheirCells`, `EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule`) | ✅ |
| BOSS-LIB-045 | Region Sum Line: the line breaks at every region border (ink ticks) and every piece adds up to the same total (navy, 7×7 and up) | `Assets/Scripts/Sudoku/ConstraintRules.ArrowsSums.cs`, `ModifierGeometryGenerator.CagesArrows.cs` | `RegionSumLineRule`, `TryGrowRegionSumLine` | CatalogCageArrowRuleTests (`RegionSumLine_EveryPieceMatchesTheFinishedOne`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-064 | Hidden Killer: cages marked "?" hold no repeated digit; the sum stays hidden and is never checked or shown (7×7 and up; never with Killer Cages) | `Assets/Scripts/Sudoku/ConstraintRules.Cages.cs`, `UI/BoardViewController.CagesArrows.cs`, `UI/InRunController.cs` | `TypedCageRule(HiddenSum)`, `CageLabel`, `BuildPatternScrollMechanics` | CatalogCageArrowRuleTests (`HiddenKiller_ChecksRepeatsOnly`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-065 | Product Cage: the digits multiply to the label "×N", no repeats (teal, 6×6 and up) | `Assets/Scripts/Sudoku/ConstraintRules.Cages.cs`, `ModifierGeometryGenerator.CagesArrows.cs` | `TypedCageRule(Product)`, `TryGrowTypedCage` | CatalogCageArrowRuleTests (`ProductCage_ProductSoFarMustDivideTheLabel`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-066 | Difference Cage: two cells that differ by the label "−N" (blue) | `Assets/Scripts/Sudoku/ConstraintRules.Cages.cs`, `ModifierGeometryGenerator.CagesArrows.cs` | `TypedCageRule(Difference)` | CatalogCageArrowRuleTests (`DifferenceCage_TwoCellsDifferByTheLabel`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-069 | Renban Cage: the digits form a run of consecutive numbers in any order (pink, label "R") | `Assets/Scripts/Sudoku/ConstraintRules.Cages.cs`, `ModifierGeometryGenerator.CagesArrows.cs` | `TypedCageRule(RenbanSet)`, `IsRun` | CatalogCageArrowRuleTests (`RenbanCage_DigitsSpanNoMoreThanTheCage`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-070 | Arrow Average: the circle marked "Ø" holds the average of its arrow (7×7 and up) | `Assets/Scripts/Sudoku/ConstraintRules.ArrowsSums.cs`, `ModifierGeometryGenerator.CagesArrows.cs` | `TypedArrowRule(Average)`, `TryBuildAverageArrow` | CatalogCageArrowRuleTests (`ArrowAverage_CircleIsTheMeanOfThePath`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-073 | Double Arrow: the arrow between two circles adds up to both circle digits | `Assets/Scripts/Sudoku/ConstraintRules.ArrowsSums.cs`, `ModifierGeometryGenerator.CagesArrows.cs` | `TypedArrowRule(DoubleEndpoint)`, `TryBuildDoubleArrow` | CatalogCageArrowRuleTests (`DoubleArrow_PathEqualsBothCircles`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-037 | K-Whispers: neighbours on a magenta line differ by at least the number on its tag (2 or 3 up to 7×7, 3 or 6 above) | `Assets/Scripts/Sudoku/ConstraintRules.CatalogB6.cs`, `ModifierGeometryGenerator.CatalogB6.cs` | `WhisperKLineRule`, `GenerateWhisperKLines` | CatalogB6RuleTests (`WhisperK_NeighboursDifferByTheLinesNumber`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-046 | 10-Line: the line breaks at every region border and every piece adds up to 10 (sienna, number tag, 7×7 and up) | `Assets/Scripts/Sudoku/ConstraintRules.ArrowsSums.cs`, `ModifierGeometryGenerator.CagesArrows.cs` | `RegionSumLineRule(NLine)`, `TryGrowRegionSumLine` | CatalogCageArrowRuleTests (`NLine_EveryPieceMakesTen`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-048 | Sum Line: all digits on the line add up to the number at its end (olive) | `Assets/Scripts/Sudoku/ConstraintRules.ArrowsSums.cs`, `ModifierGeometryGenerator.CagesArrows.cs` | `SumLineRule`, `GenerateSumLines` | CatalogCageArrowRuleTests (`SumLine_AddsUpToTheNumberAtItsEnd`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-061 | Ratio 1:3: across a black dot marked 3 one digit is three times the other | `Assets/Scripts/Sudoku/ConstraintRules.CatalogB6.cs`, `ModifierGeometryGenerator.CatalogB6.cs` | `RatioNPairRule`, `GenerateRatioNPairs` | CatalogB6RuleTests (`RatioN_OneDigitIsThreeTimesTheOther`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-063 | Distance Pairs: two cells a diagonal step or a knight move apart, joined by a brown link, differ by the steps between them | `Assets/Scripts/Sudoku/ConstraintRules.CatalogB6.cs`, `ModifierGeometryGenerator.CatalogB6.cs` | `CellPairValueRule(Distance)`, `GenerateDistancePairs` | CatalogB6RuleTests (`DistancePairsAndOppositeSums_CheckThePartner`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-067 | Ratio Cage: in a two-cell ink cage marked 1:N the larger digit is N times the smaller | `Assets/Scripts/Sudoku/ConstraintRules.Cages.cs`, `ModifierGeometryGenerator.CagesArrows.cs` | `TypedCageRule(Ratio)` | CatalogCageArrowRuleTests (`RatioCage_LargerIsTheLabelTimesTheSmaller`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-072 | Pill Arrow: the two pill cells read as a two-digit number (top or left first) and the arrow adds up to it (8×8 and up) | `Assets/Scripts/Sudoku/ConstraintRules.ArrowsSums.cs`, `ModifierGeometryGenerator.CagesArrows.cs`, `UI/BoardViewController.CagesArrows.cs` | `TypedArrowRule(Pill)`, `TryBuildPillArrow`, `DrawPill` | CatalogCageArrowRuleTests (`PillArrow_PathAddsUpToTheTwoDigitNumber`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-077 | Opposite Sums: a cell with a crimson number and its mirror cell through the centre add up to it | `Assets/Scripts/Sudoku/ConstraintRules.CatalogB6.cs`, `ModifierGeometryGenerator.CatalogB6.cs` | `CellPairValueRule(OppositeSum)`, `GenerateOppositeSums` | CatalogB6RuleTests (`DistancePairsAndOppositeSums_CheckThePartner`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-078 | Counting Circles: each circled digit d appears in exactly d circles (orange rings) | `Assets/Scripts/Sudoku/ConstraintRules.CatalogB6.cs`, `ModifierGeometryGenerator.CatalogB6.cs` | `CountingCirclesRule`, `GenerateCountingCircles` | CatalogB6RuleTests (`CountingCircles_EachCircledDigitCountsItself`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-080 | Distance Cells: a cell with a navy dot holds its distance in orthogonal steps to the cell in the navy ring (7×7 and up) | `Assets/Scripts/Sudoku/ConstraintRules.CatalogB6.cs`, `ModifierGeometryGenerator.CatalogB6.cs` | `DistanceCellRule`, `GenerateDistanceCells` | CatalogB6RuleTests (`DistanceCells_HoldTheirStepsToTheRing`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-093 | Inverted Zone (owner decision 2026-10-19): green whisper lines (≥ 5) that flip to "less than 5" for a step between two cells of the shaded 3×3 zone; 9×9 only, never with German Whispers | `Assets/Scripts/Sudoku/ConstraintRules.CatalogB6.cs`, `ModifierGeometryGenerator.CatalogB6.cs`, `UI/BoardViewController.CatalogMarksB6.cs` | `ZoneWhisperRule`, `GenerateNegationZone`, `DrawNegationZone` | CatalogB6RuleTests (`InvertedZone_FlipsTheWhisperForStepsInsideTheZone`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-079 | Quadruples: every digit in a white corner circle appears in one of the four cells round it (9×9) | `Assets/Scripts/Sudoku/ConstraintRules.OutsideClues.cs`, `ModifierGeometryGenerator.OutsideClues.cs`, `UI/BoardViewController.OutsideClues.cs` | `QuadrupleRule`, `GenerateQuadruples`, `DrawQuadruples` | CatalogOutsideClueRuleTests (`Quadruples_ListedDigitsNeedRoomAroundTheCorner`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-081 | Skyscrapers: a clue in the ring counts the digits seen from its side, higher ones hiding lower ones | `Assets/Scripts/Sudoku/ConstraintRules.OutsideClues.cs`, `ModifierGeometryGenerator.OutsideClues.cs`, `UI/BoardViewController.OutsideClues.cs` | `OutsideClueRule(Skyscrapers)`, `SkyscrapersFit`, `GenerateRingClues` | CatalogOutsideClueRuleTests (`Skyscrapers_CountVisibleBuildingsFromTheClue`, `Skyscrapers_BoundsFromTheFilledFront`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-082 | Sandwich: a clue in the ring is the sum between the 1 and the highest digit of its row or column (6×6 and up) | `Assets/Scripts/Sudoku/ConstraintRules.OutsideClues.cs`, `ModifierGeometryGenerator.OutsideClues.cs`, `UI/BoardViewController.OutsideClues.cs` | `OutsideClueRule(Sandwich)`, `SandwichFits` | CatalogOutsideClueRuleTests (`Sandwich_SumsTheDigitsBetweenOneAndNine`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-083 | X-Sums: the first digit from the clue says how many digits to add; the clue is their sum | `Assets/Scripts/Sudoku/ConstraintRules.OutsideClues.cs`, `ModifierGeometryGenerator.OutsideClues.cs`, `UI/BoardViewController.OutsideClues.cs` | `OutsideClueRule(XSums)`, `XSumsFit` | CatalogOutsideClueRuleTests (`XSums_FirstDigitSaysHowManyToAdd`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-084 | Little Killer: a clue with an arrow outside a corner of a diagonal is the sum along it (6×6 and up) | `Assets/Scripts/Sudoku/ConstraintRules.OutsideClues.cs`, `ModifierGeometryGenerator.OutsideClues.cs`, `UI/BoardViewController.OutsideClues.cs` | `OutsideClueRule(LittleKiller)`, `GenerateLittleKillers`, `DrawLittleKiller` | CatalogOutsideClueRuleTests (`LittleKiller_AddsTheDiagonal`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| UI-STYLE-010 | Clue ring: on a board with outside clues the grid is drawn about 12 % smaller (owner decision 2026-10-19) and the clues stand beside each row and column, Little Killer clues outside the corner of their diagonal with an ink arrow | `Assets/Scripts/UI/BoardViewController.OutsideClues.cs`, `BoardViewController.cs` | `HasClueRing`, `ClueRingBoardScale`, `BuildGrid`, `DrawOutsideClues` | screenshots `after_catalog_skyscrapers_quadruples`, `after_catalog_sandwich`, `after_catalog_xsums`, `after_catalog_little_killer` (`LayoutScreenshotDiagnostics.RenderCatalogModifiers`) | ✅ |
| UI-STYLE-011 | Corner clues: a quadruple is a white circle with an ink rim on the shared corner, its digits in one or two rows | `Assets/Scripts/UI/BoardViewController.OutsideClues.cs` | `DrawQuadruples` | screenshot `after_catalog_skyscrapers_quadruples` | ✅ |
| BOSS-LIB-030 | X-Sudoku: no digit repeats on either long diagonal (indigo diagonals); the generator builds the solution with both diagonals as houses | `Assets/Scripts/Sudoku/ConstraintRules.ExtraHouses.cs`, `ModifierGeometryGenerator.ExtraHouses.cs`, `UI/BoardViewController.ExtraHouses.cs`, `SudokuGenerator.SolutionShape.cs` | `ExtraHouseRule(XSudoku)`, `GenerateXSudoku`, `SolutionShape.Diagonal` | CatalogExtraHouseTests (`ExtraHouses_RejectARepeatInTheirHouseOnly`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-031 | Hyper Sudoku: four teal windows (rows and columns 2–4 and 6–8) hold no repeated digit (9×9) | `Assets/Scripts/Sudoku/ConstraintRules.ExtraHouses.cs`, `ModifierGeometryGenerator.ExtraHouses.cs`, `UI/BoardViewController.ExtraHouses.cs`, `SudokuGenerator.SolutionShape.cs` | `ExtraHouseRule(HyperSudoku)`, `SolutionShape.Window` | CatalogExtraHouseTests (`ExtraHouses_RejectARepeatInTheirHouseOnly`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-032 | Disjoint Groups: cells in the same position of their box hold different digits; a slate pip in every cell shows its position (9×9) | `Assets/Scripts/Sudoku/ConstraintRules.ExtraHouses.cs`, `ModifierGeometryGenerator.ExtraHouses.cs`, `UI/BoardViewController.ExtraHouses.cs`, `SudokuGenerator.SolutionShape.cs` | `ExtraHouseRule(DisjointGroups)`, `SolutionShape.DisjointGroup` | CatalogExtraHouseTests (`ExtraHouses_RejectARepeatInTheirHouseOnly`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-053 | Extra Region: a violet region of board-size cells (no row, column or box) holds no repeated digit; read off the solution | `Assets/Scripts/Sudoku/ConstraintRules.ExtraHouses.cs`, `ModifierGeometryGenerator.ExtraHouses.cs`, `UI/BoardViewController.ExtraHouses.cs` | `ExtraHouseRule(ExtraRegion)`, `GenerateExtraRegions` | CatalogExtraHouseTests (`ExtraRegion_RejectsARepeatInsideIt`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| BOSS-LIB-068 | Magic Square: in the gold box every row, column and diagonal adds up to 15; the generator fills a random box with a turned or mirrored Lo Shu square first (9×9) | `Assets/Scripts/Sudoku/ConstraintRules.ExtraHouses.cs`, `ModifierGeometryGenerator.ExtraHouses.cs`, `UI/BoardViewController.ExtraHouses.cs`, `SudokuGenerator.SolutionShape.cs` | `MagicSquareRule`, `GenerateMagicSquare`, `PrefillMagicSquare` | CatalogExtraHouseTests (`MagicSquare_EveryLineAddsUpTo15`), CatalogModifierTests.EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule | ✅ |
| GEN-SOLVE-002 | Extra houses shape the solution: X-Sudoku, Hyper Sudoku and Disjoint Groups are houses of the fill, and a pre-baked board is reused only when every solution-shaping rule matches (Entropy included, GAP-051); givens stay chosen under the base rules | `Assets/Scripts/Sudoku/SudokuGenerator.SolutionShape.cs`, `SudokuGenerator.cs`, `Run/RunDirector.LevelGeneration.cs`, `Boss/BossService.cs` | `SolutionShape.For`, `FillBoard`, `StructuralModsMatch`, `ShapesSolution` | CatalogExtraHouseTests (`Generator_BuildsSolutionsThatKeepTheShape`, `PreBakedBoard_MatchesOnEverySolutionShapingRule`), PreBakedBossBoardTests (real bake-then-pick flow) | ✅ |
| GEN-SOLVE-003 | Prefilled region: the magic-square box is filled before the rest of the grid | `Assets/Scripts/Sudoku/SudokuGenerator.SolutionShape.cs` | `PrefillMagicSquare`, `RandomMagicSquare` | CatalogExtraHouseTests (`Generator_BuildsSolutionsThatKeepTheShape`) | ✅ |
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
| SHOP-PRICE-001 | Shop price: `BasePrice × (1 + FloorIndex × 0.5) × priceMultiplier` (base prices: ECON-BAL-001) | `Assets/Scripts/Economy/RelicService.cs` | `ShopService.BuildOffers` | E2E/diagnostic D11, E15 | ✅ |

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
| INPUT-NAV-001 | Main menu panels: the D-pad moves one step along the focused control's link, once per press; arrow keys and the stick stay with the UI input module (GAP-062) | `Assets/Scripts/UI/MenuDpad.cs`, `MainMenuController.cs` | `MenuDpad.ReadPressed`, `MenuDpad.Step`, `HandleMenuGamepadNavigation` | MenuDpadPlayModeTests | ✅ |
| INPUT-PAD-001 | In a run the D-pad (board, path, bag) and LT/RT are read through the Input System, with the legacy D-pad buttons 12–15 as the fallback, so an Xbox pad and every pad Steam Input turns into one work (GAP-070) | `Assets/Scripts/Core/InputRemapService.cs`, `InRunController.cs`, `MenuDpad.cs` | `WasDpadPressed`, `ReadTrigger`, `WasGamepadActionPressed`, `WasActionPressed` | GamepadDpadPlayModeTests | ✅ |
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
| ACCESS-MOTION-002 | Reduce Motion stops every scale pulse and pop (path nodes, the boss node, digits, the board-fill wave, combo, reward plaques) and the badge shake; the element keeps its size (GAP-066) | `Assets/Scripts/UI/AnimationHelper.cs`, `InRunController.cs`, `BoardViewController.cs` | `AnimationHelper.MotionReduced`, `PulseScale`, `StaggerRevealNodes`, `ShakeBadgeCoroutine` | ReduceMotionTests | ✅ |
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
| HARMONY-PERK-002 | Perk effects applied at run start, before the starting bag: the perk's items first, the Level 30 class item if a slot is left; a perk's pencil raises the maximum too | `Assets/Scripts/Run/RunArchetypeService.cs` | `CreateRunState`, `ApplyHarmonyPerk`, `AddPerkItems`, `AddPencil` | HarmonyPerkStartTests | ✅ |
| HARMONY-PERK-003 | Moonshade Offering: +20 gold, +2 pencil (maximum too, GAP-067); the first item reward's slot 1 is Nothing | `Assets/Scripts/Run/RunArchetypeService.cs`, `RunDirector.cs` | `ApplyHarmonyPerk`, `BuildItemRewardSlots` | HarmonyPerkStartTests (`MoonshadeOffering_*`) | ✅ |
| HARMONY-PERK-004 | Scholar's Burden: −1 item slot, never below one; +5 pencil (maximum too); a Rare Ink Well (two from H8) first in the bag, never past its slots, fixed ids (GAP-064) | `Assets/Scripts/Run/RunArchetypeService.cs` | `ApplyHarmonyPerk`, `AddPerkItems` | HarmonyPerkStartTests (`ScholarsBurden_*`) | ✅ |
| HARMONY-PERK-005 | Empty Canvas: no starting items; every item reward Rare or better, also after a reroll (GAP-069) | `Assets/Scripts/Run/RunDirector.cs`, `RunDirector.Economy.cs`, `Assets/Scripts/Items/ItemService.cs` | `ApplyEmptyCanvas`, `RerollRewardSlots`, `ItemService.AtLeastRare` | HarmonyPerkStartTests (`EmptyCanvas_*`) | ✅ |
| HARMONY-UI-001 | Game Modes panel: Harmony level and perk selection | `Assets/Scripts/UI/GameModesPanelController.cs` | `SetHarmonyLevel`, `SetHarmonyPerk` | E2E/diagnostic E03, E12 | ✅ |
| HARMONY-UI-002 | Harmony node row with locked / unlocked / selected states | `Assets/Scripts/UI/HarmonyNodeRowController.cs` | `Refresh` | — | ◻️ |
| HARMONY-ACHIEVE-001 | Harmony Steam achievements (IDs 53–60) | `Assets/Scripts/Meta/SteamAchievementService.cs` | `EvaluateHarmonyAchievements` | SteamAchievementServiceTests | ✅ |
| ACHIEVE-RUN-001 | Run achievements at every Garden Run end (first puzzle, first boss, Floor 5, first win, no mistakes, no items, 1 HP, three cursed puzzles, 9×9, five stars); none from tutorials, custom puzzles or other modes (`docs/Achievements.md`) | `Assets/Scripts/Meta/SteamAchievementService.cs`, `Save/ProfileService.cs` | `EvaluateProgressAchievements`, `RecordRunAndGetNewUnlocks` | ProgressAchievementTests | ✅ |
| ACHIEVE-CLASS-001 | Class achievements: a second class and all eight unlocked, a class at Level 10 and 40, a win with every class (`ClassesWithAWin`) | `Assets/Scripts/Meta/SteamAchievementService.cs`, `Core/SaveModels.cs` | `EvaluateProgressAchievements` | ProgressAchievementTests | ✅ |
| ACHIEVE-MODE-001 | Mode achievements at the results of Endless Zen (depth 25), Spirit Trials (Grandmaster passed) and the Monthly Walk (finished), saved even without a new best | `Assets/Scripts/Meta/SteamAchievementService.cs`, `UI/EndScreenViewController.cs` | `EvaluateModeAchievements` | ProgressAchievementTests | ✅ |
| ACHIEVE-COLLECT-001 | Collection achievements: a 7-day Daily Walk streak, half and all of the Codex | `Assets/Scripts/Meta/SteamAchievementService.cs` | `EvaluateProgressAchievements`, `CodexHalf` | ProgressAchievementTests | ✅ |
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
| OPT-GAME-001 | Highlight Errors: a wrong digit flashes its cell, conflicts are coloured; off: neither (GAP-061) | `Assets/Scripts/UI/InRunController.GameplayOptions.cs`, `BoardViewController.cs` | `HighlightErrorsOn`, `RenderBoard` (`highlightErrors`) | GameplayOptionsPlayModeTests (`HighlightErrors_Off_AWrongDigitFlashesNoCell`) | ✅ |
| OPT-GAME-002 | Auto Pencil Cleanup: a placed digit leaves the marks of its row, column and box; off keeps them (GAP-061) | `Assets/Scripts/Run/RunDirector.cs`, `UI/OptionsController.cs`, `Core/SaveModels.cs` | `PlaceNumber` (`AutoPencilCleanup`), `SetAutoPencilCleanup`, `GameplaySettings.KeepPencilMarks` | GameplayOptionsTests (`AutoPencilCleanup_OnClearsRowColumnAndBox_OffKeepsTheMarks`), OptionsPersistenceTests | ✅ |
| OPT-GAME-003 | Show Digits Left: each number pad tile shows how many of its digit are missing (GAP-061) | `Assets/Scripts/UI/NumpadViewController.cs` | `UpdateButtonStates` ("Left" label) | GameplayOptionsPlayModeTests (`ShowDigitsLeft_NumberPadCountsTheMissingDigits`) | ✅ |
| OPT-GAME-004 | Jump to Next Empty Cell: after a correct digit the selection moves on (GAP-061) | `Assets/Scripts/UI/InRunController.GameplayOptions.cs` | `JumpToNextEmptyCellIfOn` | GameplayOptionsPlayModeTests (`JumpToNextEmptyCell_AfterACorrectDigit`) | ✅ |
| OPT-GAME-005 | Double-Tap to Confirm: the second press of the same digit on the same cell places it (GAP-061) | `Assets/Scripts/UI/InRunController.GameplayOptions.cs` | `ConfirmDigitEntry` | GameplayOptionsPlayModeTests (`DoubleTapToConfirm_TheSecondPressPlacesTheDigit`) | ✅ |
| OPT-GAME-006 | Input Lock: after a cell is chosen, digits wait for the set time (on by default, 0.25 s; 0.10–1.00 s in 0.05 s steps; off: at once); old saves keep 0.25 s | `Assets/Scripts/Core/SaveModels.cs`, `UI/OptionsController.cs`, `UI/OptionsUiFactory.Pages.cs`, `UI/OptionsUiFactory.Paper.cs`, `UI/InRunController.cs` | `GameplaySettings.InputLockEnabled/InputLockSeconds/InputLockDelay/ClampInputLockSeconds`, `SetInputLockEnabled`, `SetInputLockSeconds`, `BuildGameplayPage`, `Seconds`, `EnterNumber` | GameplayOptionsTests (3 input lock tests), GameplayOptionsPlayModeTests.InputLock_IgnoresADigitForTheSetTime_AndOffTakesItAtOnce, Options_InputLockTime_StepsByAFifthOfATenthBetweenTheLimits, screenshot `docs/design/layout-proposals/after_options_gameplay.jpg` | ✅ |
| OPT-GFX-001 | Screen Shake off stops the shake (also without Reduce Motion) (GAP-061) | `Assets/Scripts/UI/InRunController.GameplayOptions.cs`, `UI/OptionsController.cs` | `ScreenShakeAllowed`, `SetScreenShake` | GameplayOptionsPlayModeTests (`ScreenShakeOff_StopsTheShake`) | ✅ |
| OPT-AUDIO-001 | Master Volume counts once (listener at full volume) (GAP-061) | `Assets/Scripts/UI/OptionsController.cs` | `LoadOptions`, `SetMasterVolume`, `ResetAudioToDefaults` | GameplayOptionsTests (`MasterVolume_CountsOnce`) | ✅ |
| OPT-LABEL-001 | Options named for what they do: no Confirm Wrong Placement; Board Brightness/Contrast (redraw the board); Status Captions; Show Digits Left; Jump to Next Empty Cell (GAP-061) | `Assets/Scripts/UI/OptionsUiFactory.cs`, `MainMenuBlueprintBuilder.cs`, `OptionsController.cs` | `BuildStandardTabs`, `SetBrightness`, `SetContrast` | GameplayOptionsPlayModeTests (`Options_NameWhatTheyDo`), GeneratedUiWiringContractTests | ✅ |
| ACCESS-COLOR-003 | Colorblind Mode: Okabe–Ito line colours for the rules told apart by colour; no simulation on the cells (GAP-061) | `Assets/Scripts/UI/BoardViewController.cs`, `Core/AccessibilityService.cs` | `LineColorFor`, `Refresh` | GameplayOptionsTests (`ColorblindMode_UsesTheOkabeItoLineColours_AndDoesNotSimulateOnCells`) | ✅ |
| ACCESS-FONT-001 | Text Size scales the board's digits and pencil marks (within the cell) (GAP-061) | `Assets/Scripts/UI/BoardViewController.cs` | `RebuildBoard`, `BuildGrid` (`_gridFontScale`) | GameplayOptionsPlayModeTests (`TextSize_ScalesTheBoardDigits`) | ✅ |

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
| LEGAL-LICENSE-001 | The licence texts travel with the game: the Credits' Licence Texts section (Roboto notice, Apache 2.0, Steamworks.NET MIT, bundled in `Resources/Legal`, equal to `docs/legal/third-party`) and a `Licenses` folder in every standalone build (GAP-068) | `Assets/Scripts/Core/ThirdPartyLicenses.cs`, `Assets/Scripts/Editor/LicenseFilesBuildStep.cs`, `MainMenuBlueprintBuilder.cs` | `ThirdPartyLicenses.Load`, `CopyToBuild`, `LicenseFilesBuildStep.OnPostprocessBuild` | ThirdPartyLicensesTests, screenshot `after_credits_licenses` | ✅ |

---

## In-Run Visual Style

**Spec:** `docs/design/layout-proposals.md` v1.2 §Decisions (owner decisions 2026-10-15)

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| UI-STYLE-001 | Garden path as an ink hand scroll, the same on all five floors | `Assets/Scripts/UI/InRunController.PathScroll.cs`, `InRunController.PathTiles.cs`, `ScrollUiKit.Parts.cs` | `EnsurePathOverlayRoot`, `RebuildPathChrome`, `CreateNodeButton` | InRunUiPlayModeTests (`PathScroll_TwoRefreshesInOneFrame…`, `PathScroll_EmptyRouteSpace_TakesTheDrag`, path map), GeneratedUiWiringContractTests, screenshots `after_path_floor1..5` | ✅ |
| UI-STYLE-002 | Puzzle screen as a hanging scroll (ink grid, lanterns, ema plaques, wooden tiles, paper slips), 5×5 to 9×9 | `Assets/Scripts/UI/InRunUiBlueprintBuilder.cs`, `InRunController.PuzzleScroll.cs`, `HudViewController.cs`, `NumpadViewController.cs`, `BoardViewController.cs`, `GamePalette.cs`, `ScrollUiKit.Parts.cs` | `BuildSudokuGameplayPanel`, `BuildLantern`, `ApplyPuzzleScrollFloor`, `BagSlotColor`, `LanternText`, `BuildNumpad`, `SnapToPixels` | InRunUiPlayModeTests (`PuzzleScroll_LanternsBagSlotsAndCageCells_ReadAsDesigned`, legend), PuzzleScrollPaletteTests (digit contrast ≥ 4.5:1), GeneratedUiWiringContractTests (`SudokuPuzzleScreen_SitsOnAHangingScroll_WithoutDarkBoxes`, header lanes), screenshots `after_puzzle_6x6`, `_7x7_cages`, `_7x7_irregular`, `_9x9` | ✅ |
| UI-STYLE-003 | Background paintings keep their corner mark | `Assets/Resources/background/` | — | owner decision | ✅ |
| UI-STYLE-004 | Boss gate as a trial scroll (seal, ink trial panels with ensō ticks, curse margin note, "Face the boss") | `Assets/Scripts/UI/BossGateViewController.cs`, `ScrollUiKit.cs`, `FloorThemeData.cs` | `BuildBossGatePanel`, `BuildTrialCard`, `RefreshTrialMarks` | InRunUiPlayModeTests (`BossGate_PickingTrials_TicksThem_AndConfirmRecordsThem`, intensity/debuff, masking), screenshots `after_boss_gate*` | ✅ |
| UI-STYLE-005 | Boss defeated as an offering table with the relic choice on the same screen | `Assets/Scripts/UI/EndScreenViewController.cs`, `InRunController.Completion.cs`, `RewardViewController.AcceptRelic` | `ShowBossCleared`, `BuildOffering` | BossClearedFlowPlayModeTests (take, leave, no offerings), screenshot `after_boss_defeated` | ✅ |
| UI-STYLE-007 | Reward after a puzzle: tally scroll and ema plaques on a red cord; choose, then take | `Assets/Scripts/UI/RewardViewController.Tally.cs`, `ScrollUiKit.Scenes.cs` | `ShowRewardScreen`, `BuildTally`, `BuildPlaque`, `OnPlaqueClicked` | RewardScreenPlayModeTests (real solve flow: time, keys 1 then 1 again), RewardPlaqueLayoutTests (up to nine plaques fit; GAP-055, GAP-056), GeneratedUiWiringContractTests (`RewardScreen_IsATallyScrollWithEmaPlaques`), screenshot `after_reward` (diagnostic `RenderRewardShrineAndShop`: choose, take, item lands in the bag) | ✅ |
| UI-STYLE-008 | Relic node as the offering table at the shrine, with run and held-relic tags | `Assets/Scripts/UI/RewardViewController.Shrine.cs`, `ScrollUiKit.Offering` | `ShowRelicChoicePanel`, `ChooseShrineRelic` | GeneratedUiWiringContractTests (`RelicShrine_UsesTheOfferingTable_AndLegendaryArtLoadsCompletely`), screenshot `after_relic_shrine` | ✅ |
| UI-STYLE-009 | Shop as the market stall: goods with price tags, purse tag, paper slip with Buy and the gold after buying | `Assets/Scripts/UI/ShopViewController.cs` | `BuildStall`, `BuildGood`, `RefreshSlip`, `RefreshPurse` | GeneratedUiWiringContractTests (`ShopScreen_IsTheMarketStall_WithPriceTagsPurseAndSlip`), screenshot `after_shop` | ✅ |
| UI-STYLE-006 | A killer cage is one dashed outline around all its cells | `Assets/Scripts/UI/CageOutline.cs`, `BoardViewController.DrawCageOutline`, mockups `src/common.js` | `Build`, `Dashes` | CageOutlineTests (every shape up to 4 cells is one closed loop), screenshot `after_cages` | ✅ |
| UI-MENU-VERSION-001 | The main menu shows the game's version in the bottom-left corner (GameDesignSpec 1.13, Main Menu) | `Assets/Scripts/UI/MainMenuBlueprintBuilder.cs` | `VersionText` (`Application.version`) | MainMenuVersionPlayModeTests | ✅ |
| UI-STYLE-012 | Custom Puzzle as a rule book: settings slip, one tab per family (LB/RB), the 71 rules as tiles (catalog additions marked NEW), chosen rules as chips in the tray, at most five; a rule that does not fit the board or the other rules says why | `Assets/Scripts/UI/CustomPuzzleScreen.cs`, `CustomPuzzleScreen.Book.cs`, `TutorialMenuController.cs`, `MainMenuBlueprintBuilder.CustomPuzzlePanel.cs`, `Assets/Scripts/Boss/ModifierFamilies.cs` | `Build`, `ShowFamily`, `StepFamily`, `ToggleModifier`, `CanSelect`, `DropRulesThatNoLongerFit` | CustomPuzzleRuleBookTests (incl. `Random_FillsTheTrayWithFittingRules_AndRerolls`), CustomPuzzleScreenPlayModeTests (incl. the Random button), GeneratedUiWiringContractTests (`CustomPuzzleModes_DisableUnavailableChoicesAndValidateBeforeLaunch`), screenshots `before_custom_puzzle`, `after_custom_puzzle` | ✅ |
| UI-STYLE-013 | Rest as tea bowls on a low table: one slip per choice with the value after it, Cleanse only while a curse is active; choose, then drink once | `Assets/Scripts/UI/RewardViewController.Rest.cs`, `ScrollUiKit.TeaBowl` | `ShowRestChoicePanel`, `BuildBowl`, `OnBowlClicked`, `DrinkRest` | RunNodeScenesPlayModeTests (`Rest_ChooseThenDrink_AppliesOnce_CleanseOnlyWithACurse`), GeneratedUiWiringContractTests (`RestScreen_IsATeaTable_WithOneSlipPerBowl`), screenshot `after_rest` | ✅ |
| UI-STYLE-014 | Event as a hand scroll over the path: story left, choices as strips with cost and gain, a choice the player cannot pay is greyed with the reason, result line and Continue (GAP-052) | `Assets/Scripts/UI/EventChoiceScreenController.cs` | `OpenEvent`, `BuildStrip`, `Affordability`, `Take` | RunNodeScenesPlayModeTests (`Event_OpensAboveThePath_UnaffordableChoicesAreGreyed_ContinueCloses`), screenshots `before_event`, `after_event` | ✅ |
| UI-STYLE-015 | Cursed tile as a sealed talisman: the extra rule with icon and text, the puzzle's size and stars, reward, risk and current curses on tags; accept, or decline and play it normally | `Assets/Scripts/UI/BossGateViewController.Cursed.cs` | `ShowCursedNodePanel`, `BuildTalisman`, `BuildCursedGainTag` | RunNodeScenesPlayModeTests (`CursedTile_AcceptPassesTheCursedPuzzle_DeclineTheNormalOne`), AdvancedFeedbackReadinessTests, screenshot `after_cursed` | ✅ |
| UI-STYLE-016 | Garden Run end, one layout for victory and defeat: banner, floor seals (lost floor crossed out), tally with the class's closing line, class XP bar with level-up stamp, run-score ledger, unlocks or the next goal | `Assets/Scripts/UI/EndScreenViewController.RunEnd.cs`, `EndScreenViewController.cs`, `Assets/Scripts/Run/RunScoreBreakdown.cs` (GameDesignSpec §9c, GAP-053) | `ShowRunEndScroll`, `BuildFloorSeals`, `BuildScoreLedger`, `BuildUnlocks`, `UseRunEndScroll`, `RunScoreBreakdown.Calculate` | RunNodeScenesPlayModeTests (`RunEnd_GardenRunDefeat_SealsAndCrossesTheFloors_OtherModesKeepTheTextLayout`), RunScoreBreakdownTests, screenshots `after_run_end_victory`, `after_run_end_defeat` | ✅ |
| UI-STYLE-017 | Options as a hand scroll with bookmarks in the main menu and the run: banner, bookmarks (LB/RB, PageUp/PageDown), one setting per line, brush-stroke values, hanko switches with notes, a slip per page (preview board on the main menu's Accessibility page), Reset this page and Back under the scroll; the separate Accessibility panel is gone | `Assets/Scripts/UI/OptionsUiFactory.Paper.cs`, `OptionsUiFactory.cs`, `OptionsUiFactory.Pages.cs`, `OptionsUiStyle.cs`, `OptionsTabSet.cs`, `MainMenuBlueprintBuilder.cs`, `InRunUiBlueprintBuilder.cs` | `BuildOptionsScroll`, `BuildPaperValueRow`, `PaintPaperToggle`, `PageSlip`, `BuildStandardTabs`, `BuildLanguagePage`, `TurnPage`, `BuildOptionsPanel`, `BuildControlsPage`, `BuildInGameOptionsPanel` | GeneratedUiPlayModeTraversalTests (every control reachable, in-run twins, German 150 %), GameplayOptionsPlayModeTests (`Options_NameWhatTheyDo`), screenshots `after_options`, `after_options_a11y`, `after_options_inrun` | ✅ |
| UI-STYLE-018 | The Codex as a collector's book: the "Your collection · Codex" banner, a wooden book with two paper pages, bookmarks Items, Relics, Rules and Curses with their counts (LB/RB, PageUp/PageDown); the left page has the All / Found / Not yet found filter (Y, F) and the tiles (found in colour, the rest as ink silhouettes); the right page shows the chosen entry (icon, rarities or tier or family or severity, rule, how often found and used, where it comes from, when found), the collection's goal (items: the Reed Duelist unlock without the counter items) and a "found" stamp with the share; Back and the key hint under the book; explicit D-pad paths from the bookmarks to Back | `Assets/Scripts/UI/ItemsMenuController.cs`, `MainMenuBlueprintBuilder.cs`, `ScrollUiKit.cs` | `BuildItemsPanel`, `ShowTab`, `Refresh`, `BuildTiles`, `WireNavigation`, `ShowDetail`, `BuildGoal`, `DestroyChildrenNow` | CodexBookPlayModeTests (curses page, items goal, D-pad reach and exact links, focus kept on Y and on an empty page, German and French bookmarks fit), GeneratedUiWiringContractTests.ItemsCodex_ProvidesRelicRuleAndCurseTabsWithIcons, GeneratedUiPlayModeTraversalTests (Codex opens and closes), screenshots `after_codex`, `after_codex_curses` | ✅ |
| UI-STYLE-019 | Meta Progression as the garden of classes: the "Your garden · Meta Progression" banner over the class-select painting, the eight classes as ema plaques on two red cords (level and prestige stars, or dark with the unlock goal and progress), the chosen class on a hanging scroll (name; level, runs and best floor; XP stroke or the way to its unlock; passive; every milestone with its exclusive item, relic and prestige, ticked off up to its level; prestige stars, their gold and, at Level 40, the prestige button or what it still needs), the totals (classes, relics, codex, highest Harmony, completion), Back and the key hint; explicit D-pad links, X prestiges | `Assets/Scripts/UI/MetaProgressionPanelController.cs`, `MetaProgressionPanelController.Scroll.cs`, `MainMenuBlueprintBuilder.cs`, `MainMenuController.cs`, `Meta/CodexProgress.cs` | `BuildMetaProgressionPanel`, `BuildPlaques`, `ShowClass`, `BuildPath`, `BuildPrestige`, `BuildTotals`, `WireNavigation`, `OpenMetaProgression`, `PrestigeClass` | ClassGardenPlayModeTests (plaques, scroll, prestige with two presses, D-pad links), GeneratedUiPlayModeTraversalTests (opens and closes), screenshots `after_meta_progression`, `after_meta_progression_locked` | ✅ |
| UI-STYLE-020 | Pencil mode in indigo ink (owner pick A, 0.2.0 playtest): the number pad tiles and the switch's Pencil half turn indigo with paper-white digits, the pencil marks on the board are indigo too; Solve mode keeps the wooden tiles | `Assets/Scripts/UI/NumpadViewController.cs`, `ScrollUiKit.Parts.cs`, `GamePalette.cs` | `SetPencilMode`, `ScrollUiKit.SetTileInk`, `IndigoInk`, `GamePalette.PencilMarkColor` | InRunUiPlayModeTests (`PencilMode_TurnsTheNumberPadIndigo_AndSolveBringsTheWoodBack`), screenshots `before_pencil_mode`, `after_pencil_mode` | ✅ |
| UI-STYLE-021 | Garden flow D (owner pick, 0.2.0 playtest): entering a floor of a Garden Run shows the floor's painting for two seconds with its name on a hanging banner and the path rolled up below, then the path scroll unrolls; a click or any key skips it; once per floor, not on a resumed floor; Reduce Motion skips the unroll | `Assets/Scripts/UI/InRunController.FloorEntry.cs`, `InRunController.cs` | `ShowFloorEntryIfNew`, `BuildFloorEntry`, `DismissFloorEntry`, `UnrollPath`, `FloorEntrySeconds` | InRunUiPlayModeTests (`FloorEntry_ShowsThePaintingAndName_ThenThePathUnrolls_OncePerFloor`), screenshots `after_floor_entry_floor1`, `after_floor_entry_floor2` | ✅ |
| UI-STYLE-022 | Garden flow A · Washi window (owner pick, with D): the floor's painting sharp and lightly dimmed round the path scroll and showing through its thin paper; title, first-pick note, route names, floor marks, legend and node labels on paper slips; High Contrast keeps the opaque paper and the dark blurred room | `Assets/Scripts/UI/InRunController.PathWashi.cs`, `InRunController.PathScroll.cs` | `ApplyPathWashi`, `FitSlip`, `SlipBehind`, `SlipFirstPickNote`, `PillUnderLabel`, `PathWashiOn`, `ApplyPathFloorArt` | InRunUiPlayModeTests.PathScroll_WashiWindow_ShowsThePainting_AndHighContrastKeepsTheOpaquePaper, screenshots `after_path_washi_floor1/2/4` | ✅ |
| UI-STYLE-024 | Select Class · Who walks the garden (owner pick 2026-10-21): the class painting dimmed, eight class seals (level and prestige stars, or dark with the unlock goal), the chosen class on a hanging scroll (level line, HP, Pencil, Bag, Relics and Gold with their level bonus, passive, the next level rewards), the run's settings on a strip (Harmony stones and perk for a Garden Run, a line on the mode otherwise, irregular puzzles, seed and week seed); Begin starts the run without a confirmation window, "Abandon & Begin" with a warning when a saved run would end | `Assets/Scripts/UI/ClassSelectScreen.cs`, `MainMenuBlueprintBuilder.ClassSelect.cs`, `MainMenuController.StartRun.cs` | `ClassSelectScreen.RefreshSeals`, `ShowClass`, `ShowRunSettings`, `BuildClassSelectPanel`, `BeginRunFromSelect` | ModeScreensPlayModeTests (`SelectClass_SealsShowLevelOrGoal_AndTheScrollShowsTheChosenClass`, `SelectClass_TheStripHoldsTheRunSettings_AndThereIsNoConfirmationWindow`, `Harmony_SetOnGameModes_IsTheHarmonyOfSelectClass`), ConditionalButtonStateTests, screenshots `after_class_select` | ✅ |
| UI-STYLE-025 | Game Modes · Five paths (owner pick 2026-10-21): five noren curtains from a wooden beam (Garden Run, Endless Zen, Spirit Trials, Daily Walk, Monthly Walk), each with icon, name, a line on the mode and its state on a strip (Harmony, best depth, best score, goals and streak, theme and days left; a locked mode dark with what opens it); the focused mode's slip below with its settings and the way in; Harmony set here and on Select Class, a change on either reaches both | `Assets/Scripts/UI/GameModesScreen.cs`, `MainMenuBlueprintBuilder.GameModes.cs`, `MainMenuController.GameModes.cs` | `GameModesScreen.Refresh`, `Show`, `ShowHarmony`, `BuildGameModesPanel`, `BuildCurtain`, `RefreshGameModesPanel`, `RefreshGameModesHarmony` | ModeScreensPlayModeTests (`GameModes_FiveCurtains_FocusShowsTheirSlip`, `Harmony_SetOnGameModes_IsTheHarmonyOfSelectClass`, `GermanAndFrench_CurtainAndSlipTextsFit`), ConditionalButtonStateTests, GeneratedUiPlayModeTraversalTests, screenshots `after_game_modes` | ✅ |
| UI-STYLE-026 | The results of Endless Zen, Spirit Trials and the Monthly Walk in the run-end family (owner 2026-10-21): the mode's painting, a banner with the mode and the outcome, the numbers on a tally paper with a closing line, the record (deepest descent, the tier's score, the month's grade) large on a dark panel with the personal best and a "New best" stamp, notes on a slip (ink stamps, the month's countdown), the way back; the bests are saved as before | `Assets/Scripts/UI/EndScreenViewController.ModeEnd.cs`, `EndScreenViewController.cs` | `ShowModeEndScroll`, `BuildModeTally`, `BuildModeRecord`, `ShowEndlessZenResult`, `ShowSpiritTrialsResult`, `ShowSeasonalResult` | RunNodeScenesPlayModeTests (`ModeResults_ShowTheRecordOnAScroll_AndKeepSavingTheBests`), screenshots `after_result_endless_zen`, `after_result_spirit_trials`, `after_result_monthly_walk` | ✅ |
| RUN-AGAIN-001 | Walk again on every run end, first and focused: same class, mode, Harmony, perk, tier and irregular setting, a new seed; the Monthly Walk starts again; tutorials have Back only (RM-110) | `Assets/Scripts/Run/RunAgain.cs`, `Assets/Scripts/UI/InRunController.WalkAgain.cs`, `EndScreenViewController.RunEnd.cs` | `RunAgain.Request`, `KindOf`, `WalkAgain`, `BuildEndButtons` | OneMoreRunTests (`WalkAgain_*`), RunNodeScenesPlayModeTests (run end and mode results) | ✅ |
| RUN-END-NEAR-001 | A lost run says how close it came (the boss or a puzzle of the floor, cells left) and the best floor with the class (RM-111) | `Assets/Scripts/UI/RunEndInsights.cs`, `EndScreenViewController.cs` | `RunEndInsights.SoClose`, `CellsLeft`, `AddNearMiss` | OneMoreRunTests (`SoClose_*`) | ✅ |
| RUN-END-MOVED-001 | "This run": at most three things the run moved: a newly opened Harmony first, the unlocks brought closer (closest first), the Codex, the next Harmony's condition (RM-112) | `Assets/Scripts/UI/RunEndInsights.cs`, `EndScreenViewController.RunEnd.cs` | `RunEndInsights.Moved`, `BuildThisRun` | OneMoreRunTests (`Moved_*`), RunNodeScenesPlayModeTests | ✅ |
| STAKES-001 | Stakes in numbers: the cursed tile's gold and XP played normally and cursed, the boss gate's XP for the fight and per trial, the XP a struck trial costs (RM-115) | `Assets/Scripts/Economy/RewardPreview.cs`, `BossGateViewController.Cursed.cs`, `BossGateViewController.Gold.cs` | `RewardPreview.PuzzleGold`, `PuzzleXp`, `BuildCursedGainTag`, `BuildGateStakes` | OneMoreRunTests (`RewardPreview_IsWhatThePuzzlePays`), InRunUiPlayModeTests, RunNodeScenesPlayModeTests | ✅ |
| RUN-LENGTH-001 | Run-length measures on the PC only: time a run is open, time to its first choice, gap since the previous run of the session; the last 200 runs in the save; the run's length in the run-end eyebrow; `tools/playtest/run_length_report.py` (concept §6, RM-113 decision; five floors stay) | `Assets/Scripts/Run/RunLengthLog.cs`, `Core/SaveModels.cs`, `Core/RuntimeModels.cs`, `InRunController.cs`, `GameBootstrap.cs` | `RunLengthLog.OnRunStarted`, `Tick`, `OnPathChoice`, `Record`, `Append` | OneMoreRunTests (`RunClock_*`, `RunHistory_*`, `RunLength_*`), RunNodeScenesPlayModeTests, `tools/playtest/test_run_length_report.py` | ✅ |
| SCORE-GRADE-001 | Every solved puzzle gets a grade from its own mistakes: Perfect 0 (+5 %), Clean 1 (0), Rough 2–3 (−2 %), Messy 4+ (−5 %); replaces the flat mistake penalty and the perfect bonus (GameDesignSpec 1.14 §9c) | `Assets/Scripts/Run/RunScoreBreakdown.cs`, `PostRunAnalyticsService.cs`, `Core/RuntimeModels.cs` | `GradeFor`, `Calculate`, `RecordPuzzleSolved`, `PuzzleGradeEntry` | RunScoreBreakdownTests (`GradeFor_FollowsTheMistakesOfThatPuzzle`, `SpecExample_Base400_Gives444`, `SpecBreakdown_Base1200`) | ✅ |
| SCORE-GRADE-002 | The puzzle that ends the run unsolved counts as Messy and keeps its mistakes and floor | `Assets/Scripts/Run/RunResultFactory.cs`, `PostRunAnalyticsService.cs` | `Build` (lost puzzle), `BuildPuzzleGrades` | RunMistakeCountTests (`EveryPuzzlesMistakes_CountAtTheEnd_AlsoThePuzzleThatEndedTheRun`) | ✅ |
| SCORE-GRADE-003 | The run-end scroll shows one stamp per puzzle in the order played, floors apart, with a legend; the ledger names Rough and Messy rows; the text breakdown lists the letters | `Assets/Scripts/UI/EndScreenViewController.RunEnd.cs`, `RunGradeStamps.cs`, `EndScreenPresenter.cs` | `BuildGradeStamps`, `LedgerRows`, `RunGradeStamps.Line` | RunNodeScenesPlayModeTests (`RunEnd_GardenRunDefeat_SealsAndCrossesTheFloors_OtherModesKeepTheTextLayout`), RunScoreBreakdownTests (`FinishedRun_EndScreenBreakdown_MatchesTheStoredScore`), screenshot `after_run_end_grades` | ✅ |
| SCORE-GRADE-004 | Rough and Messy together cost at most 50 % of the base | `Assets/Scripts/Run/RunScoreBreakdown.cs` | `Calculate` | RunScoreBreakdownTests (`Rows_AlwaysAddUpToTheScore_AndThePenaltyStopsAtHalf`) | ✅ |
| SCORE-GOLD-001 | Gold held at the end adds a point per 5 gold, at most 15 % of the base, as its own ledger row (gold-and-items concept L5) | `Assets/Scripts/Run/RunScoreBreakdown.cs`, `RunResultFactory.cs`, `UI/EndScreenViewController.RunEnd.cs` | `Calculate` (Gold row), `PostRunAnalytics.GoldLeft` | RunScoreBreakdownTests (`KeptGold_AddsAPointPerFiveGold_UpToFifteenPercent`, `FinishedRun_EndScreenBreakdown_MatchesTheStoredScore`) | ✅ |
| ECON-BAL-001 | Item base prices Normal 40, Rare 75, Epic 150 (×2.5, gold-and-items concept L1), × (1 + 0.5 × floor index); one price table | `Assets/Scripts/Economy/GoldTable.cs`, `Items/ItemService.cs`, `Economy/ShopService.cs` | `BaseItemPrice`, `FloorPriceFactor`, `ShopItemPrice`, `BuildOffers` | GoldAndPressureBalanceTests (`ItemPrices_NormalRareEpic_Are40_75_150_TimesTheFloor`) | ✅ |
| ECON-BAL-002 | Puzzle gold is four fifths of the size-and-stars table (L1) | `Assets/Scripts/Economy/GoldTable.cs` | `CalculatePuzzleGold`, `PuzzleGoldFactor` | GoldAndPressureBalanceTests (`PuzzleGold_IsFourFifthsOfTheTable`), ReviewDiagnosticsTests D11 | ✅ |
| ECON-BAL-003 | From Floor 4 of a Garden Run a mistake costs at least 2 HP at every Harmony level (L6); the floor's entry says so | `Assets/Scripts/Run/RunDirector.cs`, `UI/InRunController.FloorEntry.cs` | `LateFloorMistakeHpCost`, `ApplyMistakePenalty(row, col)`, `BuildFloorEntry` | GoldAndPressureBalanceTests (`Mistake_CostsTwoHpFromFloorFour`), screenshot `after_floor_entry_floor4` | ✅ |
| ECON-BAL-004 | The rest heals a quarter of max HP, at least 1 (L6; it was a third) | `Assets/Scripts/Run/RunDirector.Economy.cs` | `GetRestHealAmount` | GoldAndPressureBalanceTests (`Rest_HealsAQuarterOfMaxHp`), RestNodeValuesTests, HarmonyEffectsTests | ✅ |
| ECON-GATE-001 | Boss gate: strike one offered trial for 60 gold × floor number, once per gate; the boss takes one trial fewer; not when it takes one; kept in the save | `Assets/Scripts/Run/RunDirector.BossGate.cs`, `UI/BossGateViewController.Gold.cs`, `BossGateViewController.cs`, `Core/RuntimeModels.cs` | `TryStrikeBossTrial`, `GetBossPicksRequired`, `BuildGateGoldRow`, `TryStrikeTrial` | BossGateGoldTests, InRunUiPlayModeTests (`BossGate_GoldStrikesATrial_AndLowersTheIntensity`), screenshots `after_boss_gate`, `after_boss_gate_struck` | ✅ |
| ECON-GATE-002 | Boss gate: lower the boss puzzle's intensity one step for 40 gold × floor number, once per gate; not below Low; the boss debuff stays | `Assets/Scripts/Run/RunDirector.BossGate.cs`, `RunDirector.LevelGeneration.cs`, `UI/BossGateViewController.Gold.cs` | `TryLowerBossIntensity`, `GetBossIntensity`, `BuildLevelConfig` | BossGateGoldTests, InRunUiPlayModeTests (`BossGate_GoldStrikesATrial_AndLowersTheIntensity`) | ✅ |
| ITEM-COUNTER-001 | Water Basin (lines): up to 3 empty cells of one line, the selected cell's line first, else the one closest to done | `Assets/Scripts/Run/RunDirector.CounterItems.cs`, `Items/CounterItems.cs`, `Items/ItemService.cs`, `UI/InRunController.CounterItems.cs` | `GetCounterItemTargets`, `ApplyCounterItem`, `FromClosestGroup` | CounterItemTests (`WaterBasin_FillsOneLine_TheSelectedCellsLineFirst`) | ✅ |
| ITEM-COUNTER-002 | Twin Pebbles (dots and pairs): both cells of one dot or pair mark | `Assets/Scripts/Run/RunDirector.CounterItems.cs` | `GetCounterItemTargets` | CounterItemTests (`TwinPebbles_FillsBothCellsOfOneMark`) | ✅ |
| ITEM-COUNTER-003 | Rice Bowl (cages and arrows): up to 3 empty cells of one cage or arrow | `Assets/Scripts/Run/RunDirector.CounterItems.cs` | `GetCounterItemTargets` | CounterItemTests (`RiceBowl_FillsOneCage`) | ✅ |
| ITEM-COUNTER-004 | Moss Stone (cells): 2 marked, paired or fogged cells | `Assets/Scripts/Run/RunDirector.CounterItems.cs` | `GetCounterItemTargets`, `FromPool` | CounterItemTests (`MossStone_FillsMarkedCells`) | ✅ |
| ITEM-COUNTER-005 | Compass of Order (whole board): the 2 most constrained empty cells of the extra houses | `Assets/Scripts/Run/RunDirector.CounterItems.cs` | `WholeBoardHouseCells`, `BasicCandidateCount` | CounterItemTests (`CompassOfOrder_FillsDiagonalCells_OnAnXSudoku`) | ✅ |
| ITEM-COUNTER-006 | Sun Medallion (outside clues): the first 2 empty cells one clue reads | `Assets/Scripts/Run/RunDirector.CounterItems.cs` | `GetCounterItemTargets` (`OutsideClueGeometry.Cells`) | CounterItemTests (`SunMedallion_FillsTheFirstCellsAClueReads`, `ACounterWithoutItsFamily_FillsNothing_AndIsNotUsedUp`) | ✅ |
| ITEM-COUNTER-007 | Shops lean toward the counters of the floor's and the boss's rules: 50 % per slot, each once, fixed rarity | `Assets/Scripts/Economy/ShopService.cs`, `Run/RunDirector.Economy.cs`, `Items/CounterItems.cs` | `BuildOffers` (`counterItems`), `BuildShopOffers`, `CounterItems.ForRules` | CounterItemTests (`Shops_LeanTowardTheBoardsCounters_EachOnce_AtTheirFixedRarity`, `CountersForRules_FollowTheFamilies_AndSkipDebuffs`) | ✅ |

---

## Localization

**Spec:** `docs/localization-catalog.md` v1.1 §French, Spanish and Brazilian Portuguese

| ID | Description | File | Method | Evidence | Status |
| ---- | ------------- | ------ | -------- | ---------- | -------- |
| LOC-PARITY-001 | fr/es/pt contain every German key | `Assets/Resources/Localization/*.json` | catalogs | LocalizationParityTests | ✅ |
| LOC-PARITY-002 | Same placeholders as German, no empty text | `Assets/Resources/Localization/*.json` | catalogs | LocalizationParityTests | ✅ |
| LOC-PARITY-003 | Gameplay content never falls back to English in fr/es/pt | `Assets/Scripts/Core/LocalizationService.cs` | `T` | LocalizationParityTests | ✅ |
| LOC-NUM-001 | Numbers follow the game's language, not the PC's region: decimal and group separators of `Format`/`FormatPlural` and of the XP multiplier come from the chosen language's culture (en-US, de-DE, fr-FR, es-ES, pt-BR; a space group separator becomes a no-break space, which the font has) (GAP-065) | `Assets/Scripts/Core/LocalizationService.cs`, `HarmonyNodeRowController.cs`, `OptionsUiFactory.Paper.cs` | `LocalizationService.Culture`, `CultureFor`, `Format`, `FormatPlural` | LocalizedNumberTests | ✅ |

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
| REQ-BOSS-107 | After a non-final boss the player can claim the relic and continue | PathSystem §Floor Transition | EndScreenViewController.ShowBossCleared: offering table with the relic choice on one screen (UI-STYLE-005), Take/Leave → next floor | BossClearedFlowPlayModeTests, RunMistakeCountTests (`FloorTally_AddsTheFloorsPuzzles_AndResetsOnTheNextFloor`: boss and floor columns), GeneratedUiWiringContractTests | ✅ |
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
| REQ-RUN-104 | Rest node: +2 HP / +3 Pencil / +1 Reroll | GDS §9b | Rest: heal MaxHP/4 (ECON-BAL-004; MaxHP/3 before 2026-10-21), +4 Pencil, +1 Reroll, Cleanse (spec aligned to code) | RestNodeValuesTests | ✅ |
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

