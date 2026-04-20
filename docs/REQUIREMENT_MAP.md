# Requirement Traceability Map — All Systems

**Updated:** 2026-04-15

Legend: ✅ Implemented | ⚠️ Partial | ❌ Not Implemented | 🆕 New (spec only, no code yet)

---

## Index

| System Prefix | Spec File | Status |
|--------------|-----------|--------|
| [PRESSURE](#pressure-mechanics) | `docs/PressureMechanicsSpec.md` | ✅ |
| [DEBUFF](#boss-debuffs) | `docs/BossDebuffEffects.md` | ✅ (9 of 18) |
| [CURSE](#curse-system) | `docs/CurseSystem.md` | ✅ |
| [META](#meta-progression) | `docs/MetaProgressionSystem.md` | ✅ |
| [ZENMODE](#endless-zen-mode) | `docs/EndlessZenMode.md` | ✅ |
| [TRIAL](#spirit-trials-mode) | `docs/SpiritTrialsMode.md` | ✅ |
| [TUTO-BASICS](#sudoku-basics-tutorial) | `docs/FirstTimePlayTutorial.md` | ✅ |
| [GEN](#sudoku-generation-pipeline) | `docs/SudokuGenerationPipeline.md` | ✅ |
| [MAP](#path-system--garden-overview) | `docs/PathSystem_GardenOverview.md` | ✅ |
| [BOSS](#boss-mechanics) | `docs/BossMechanicsSystem.md` | ✅ |
| [XP](#class-xp-progression) | `docs/ClassXpProgressionSystem.md` | ✅ |
| [ITEM](#items--relics) | `docs/ItemsAndRelicsSystem.md` | ✅ |
| [SAVE](#save--load) | `docs/SaveLoadArchitecture.md` | ✅ |
| [INPUT](#input--profiles) | `docs/InputAndProfilesSpec.md` | ✅ |
| [ACCESS](#accessibility) | `docs/AccessibilitySpec.md` | ✅ |
| [ECON](#gold-economy) | `docs/GoldEconomySystem.md` | ✅ |
| [SEASON](#seasonal-challenge) | `docs/SeasonalChallengeSpec.md` | 🆕 |
| [DAILY](#daily-goal-system) | `docs/DailyGoalSystem.md` | 🆕 |
| [CLASS](#class-progression-unlocks) | `docs/ClassProgressionUnlocks.md` | 🆕 |

---

## Pressure Mechanics

**Spec:** `docs/PressureMechanicsSpec.md` v0.5 | **Plan:** `docs/plan/PressureMechanicsPlan.md`

### Rolling & Activation

| ID | Description | File | Method / Location | Status |
|----|-------------|------|-------------------|--------|
| PRESSURE-ROLL-001 | No floor 1 or tutorial mode | `Assets/Scripts/Run/RunDirector.cs` | `RollPressureMechanic` — first two guards | ✅ |
| PRESSURE-ROLL-002 | Elite/pre-boss/boss nodes only | `Assets/Scripts/Run/RunDirector.cs` | `RollPressureMechanic` — node type guard | ✅ |
| PRESSURE-ROLL-002 | `LevelConfig` node flags | `Assets/Scripts/Core/RuntimeModels.cs` | `LevelConfig.IsElite`, `LevelConfig.IsPreBoss` | ✅ |
| PRESSURE-ROLL-003 | Floor probability table | `Assets/Scripts/Run/RunDirector.cs` | `RollPressureMechanic` — `chance` switch | ✅ |
| PRESSURE-ROLL-004 | One mechanic per puzzle max | `Assets/Scripts/Run/RunDirector.cs` | `RollPressureMechanic` — `Contains` dedup guard | ✅ |
| PRESSURE-ROLL-004 | Modifier data registration | `Assets/Scripts/Boss/BossService.cs` | `ModifierData` — IDs 103–106 | ✅ |
| PRESSURE-ROLL-005 | PressureWave from floor 3+ | `Assets/Scripts/Run/RunDirector.cs` | `RollPressureMechanic` — `if (floor >= 3)` | ✅ |

### Initialization

| ID | Description | File | Method / Location | Status |
|----|-------------|------|-------------------|--------|
| PRESSURE-INIT-001 | CountdownFill scope/cells/counter init | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` — CountdownFill case | ✅ |
| PRESSURE-INIT-002 | HauntedCell intensity-keyed selection | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` — HauntedCell case; `SelectHauntedCell` | ✅ |
| PRESSURE-INIT-003 | CrumblingRegion one-region fragility init | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` — CrumblingRegion case; `SelectCrumblingRegion` | ✅ |
| PRESSURE-INIT-004 | PressureWave interval/counter init | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` — PressureWave case | ✅ |
| PRESSURE-INIT-005 | Chain length 3–4; fallback to SingleCell | `Assets/Scripts/Run/RunDirector.cs` | `SelectThreatCells` — Chain case | ✅ |
| PRESSURE-INIT-006 | PresolvedCells excluded | `Assets/Scripts/Run/RunDirector.cs` | `SelectThreatCells`, `SelectHauntedCell`, `SpawnWaveThreat` | ✅ |
| PRESSURE-INIT-007 | counting_shadow curse threat | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` — curse block | ✅ |
| PRESSURE-INIT-008 | hollow_eye curse haunted cell | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` — curse block | ✅ |

### Core Tick Logic

| ID | Description | File | Method / Location | Status |
|----|-------------|------|-------------------|--------|
| PRESSURE-TICK-001 | Correct placement decrements all counters | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — counter loop step 1 | ✅ |
| PRESSURE-TICK-002 | Wrong placement also decrements counters | `Assets/Scripts/Run/RunDirector.cs` | `PlaceNumber` — incorrect path after `Mistakes++` | ❌ |
| PRESSURE-TICK-003 | Success when last cell placed | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — success check step 2 | ✅ |
| PRESSURE-TICK-004 | Chain success +1 HP | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — `if (threat.IsChain)` | ✅ |
| PRESSURE-TICK-005 | Expiry 2 HP per unfilled cell | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — expiry branch | ✅ |
| PRESSURE-TICK-006 | DoublePenalty doubles expiry | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — `penaltyPer` calculation | ✅ |
| PRESSURE-TICK-007 | Wave counter decrement + reset + spawn | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — step 4 wave block; `SpawnWaveThreat` | ✅ |
| PRESSURE-TICK-008 | Wave cap at 3 concurrent | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — `WaveSpawnCount < 3` | ✅ |
| PRESSURE-TICK-009 | WaveSpawnCount decrements on removal | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` — success and expiry branches | ✅ |
| PRESSURE-TICK-010 | HauntedCell extra HP on mistake | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureMistake` — HauntedCell block | ✅ |
| PRESSURE-TICK-011 | HauntedCell DoublePenalty stacking | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureMistake` — `extraDmg` calc | ✅ |
| PRESSURE-TICK-012 | CrumblingRegion all-cell erosion | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureMistake` — CrumblingCells loop | ✅ |
| PRESSURE-TICK-013 | CrumblingRegion VeryHigh first-cell only | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureMistake` — needs `CrumblingVeryHigh` branch | ❌ |
| PRESSURE-TICK-014 | Fragility 0 → AutoFill trigger | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureMistake` — `if <= 0` | ✅ |
| PRESSURE-TICK-015 | AutoFill places solution, costs HP | `Assets/Scripts/Run/RunDirector.cs` | `AutoFillCrumbledCell` | ✅ |
| PRESSURE-TICK-016 | AutoFill silently resolves threats | `Assets/Scripts/Run/RunDirector.cs` | `AutoFillCrumbledCell` — threat loop | ✅ |
| PRESSURE-TICK-017 | AutoFill does not tick counters | `Assets/Scripts/Run/RunDirector.cs` | `AutoFillCrumbledCell` — no `TickPressureThreats` call | ✅ |
| PRESSURE-TICK-018 | AutoFill clears haunting | `Assets/Scripts/Run/RunDirector.cs` | `AutoFillCrumbledCell` — `HauntedCell = (-1,-1)` | ✅ |
| PRESSURE-TICK-019 | All HP via `ApplyMistakePenalty` | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty(int damage)` | ✅ |

### UI / Rendering

| ID | Description | File | Method / Location | Status |
|----|-------------|------|-------------------|--------|
| PRESSURE-UI-001 | No visuals on tutorial (RunNumber==0) | `Assets/Scripts/UI/InRunController.cs` | `BuildPressureRenderData` — RunNumber guard | ✅ |
| PRESSURE-UI-002 | Count badge per threatened cell | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — `badgeText` / `cell.BadgeLabel` block | ✅ |
| PRESSURE-UI-003 | Badge colour by ratio (>50%/25–50%/<25%) | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — badge colour calc; needs `InitialPlacements` | ⚠️ |
| PRESSURE-UI-004 | Expiry red-flash + shake animation | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — threat-removal detection + animation | ❌ |
| PRESSURE-UI-005 | Chain success gold-flash | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — `ChainSuccessCells` colour override | ❌ |
| PRESSURE-UI-006 | Haunted cell purple pulse | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — HauntedCell pulse block | ✅ |
| PRESSURE-UI-007 | Crumbling brown crack tint | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — CrumblingCells tint block | ✅ |
| PRESSURE-UI-008 | Auto-fill green flash | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — `CrumbledFlashCells` colour override | ❌ |
| PRESSURE-UI-009 | Wave ripple animation | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — wave ripple overlay; needs `WaveRipplePending` | ❌ |
| PRESSURE-UI-010 | Region border strip (Row/Col/Box) | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — new `DrawRegionBorderStrip` helper | ❌ |
| PRESSURE-UI-011 | Chain animated line | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` — new `DrawChainThreatLine` helper | ❌ |

### Persistence

| ID | Description | File | Method / Location | Status |
|----|-------------|------|-------------------|--------|
| PRESSURE-SAVE-001 | All pressure fields `[NonSerialized]` | `Assets/Scripts/Core/RuntimeModels.cs` | `LevelState` — all pressure fields | ✅ |
| PRESSURE-SAVE-002 | Resume reinitialises pressure from seed | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` called in `StartLevel` | ✅ |
| PRESSURE-SAVE-003 | Comment documents non-persistence | `Assets/Scripts/Save/RunAutoSaveCoordinator.cs` | Line 51 — NOTE comment | ✅ |

### Edge Case Guards

| ID | Description | File | Method / Location | Status |
|----|-------------|------|-------------------|--------|
| PRESSURE-EDGE-001 | Chain <3 cells → SingleCell fallback | `Assets/Scripts/Run/RunDirector.cs` | `SelectThreatCells` — Chain case | ✅ |
| PRESSURE-EDGE-002 | CrumblingRegion no-qualifying-region guard | `Assets/Scripts/Run/RunDirector.cs` | `SelectCrumblingRegion` + `InitializePressureMechanics` | ✅ |
| PRESSURE-EDGE-003 | Wave spawn skips when no free cells | `Assets/Scripts/Run/RunDirector.cs` | `SpawnWaveThreat` — `available.Count == 0` | ✅ |
| PRESSURE-EDGE-004 | Haunted+Crumbling same cell | `Assets/Scripts/Run/RunDirector.cs` | `AutoFillCrumbledCell` — HauntedCell clear | ✅ |
| PRESSURE-EDGE-005 | counting_shadow + modifier coexist | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` — additive curse block | ✅ |
| PRESSURE-EDGE-006 | hollow_eye defers to modifier | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` — `HauntedCell == (-1,-1)` guard | ✅ |

---

## Boss Debuffs

**Spec:** `docs/BossDebuffEffects.md` v1.3

### Debuff Definitions

| ID | Debuff | BossModifierId | Description | Status |
|----|--------|:---:|-------------|--------|
| DEBUFF-DEF-001 | RowWipe | 94 | On mistake: clears player-placed digits in same row | ✅ |
| DEBUFF-DEF-002 | ColWipe | 95 | On mistake: clears player-placed digits in same column | ✅ |
| DEBUFF-DEF-003 | BoxWipe | 99 | On mistake: clears player-placed digits in same region | ✅ |
| DEBUFF-DEF-004 | CrossWipe | 100 | On mistake: clears row AND column (combined) | ✅ |
| DEBUFF-DEF-005 | RadiusWipe | — | On mistake: clears within 2-cell Chebyshev radius | ❌ |
| DEBUFF-DEF-006 | PencilPoison | — | On mistake: adds wrong pencil marks to row | ❌ |
| DEBUFF-DEF-007 | PencilBlind | 98 | On mistake: removes all pencil marks in row+column | ✅ |
| DEBUFF-DEF-008 | PencilScramble | — | On mistake: scrambles all pencil marks board-wide | ❌ |
| DEBUFF-DEF-009 | FogPulse | — | On mistake: adds fog ring for 3 moves | ❌ |
| DEBUFF-DEF-010 | NumberBlind | — | On mistake: hides placed digit board-wide for 2 moves | ❌ |
| DEBUFF-DEF-011 | CellBlur | — | On mistake: hides digit labels in 3×3 area briefly | ❌ |
| DEBUFF-DEF-012 | DoublePenalty | 96 | Wrong placements deal 2 HP instead of 1 | ✅ |
| DEBUFF-DEF-013 | PencilDrain | 101 | Each wrong placement costs 1 extra pencil charge | ✅ |
| DEBUFF-DEF-014 | GoldFine | 102 | Deducts 5 gold per mistake (clamped to 0) | ✅ |
| DEBUFF-DEF-015 | RevealCost | — | Blocks next item use for 2 moves after mistake | ❌ |
| DEBUFF-DEF-016 | DigitShift | — | After mistake: swap solution values between cells | ❌ |
| DEBUFF-DEF-017 | GivenReveal | — | After mistake: convert 1–3 correct digits to given | ❌ |
| DEBUFF-DEF-018 | CellLock | 97 | After mistake: wrong cell un-editable for 3 placements | ✅ |

### Integration Hooks

| ID | Hook | File | Method | Status |
|----|------|------|--------|--------|
| DEBUFF-HOOK-001 | `ApplyMistakePenalty(row,col)` overload dispatches debuffs | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty(int,int)` | ✅ |
| DEBUFF-HOOK-002 | DoublePenalty sets damage=2 | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty` | ✅ |
| DEBUFF-HOOK-003 | RowWipe → `ClearBoardRow(row)` | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty` | ✅ |
| DEBUFF-HOOK-004 | ColWipe → `ClearBoardCol(col)` | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty` | ✅ |
| DEBUFF-HOOK-005 | PencilBlind → `ClearPencilRowCol(row,col)` | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty` | ✅ |
| DEBUFF-HOOK-006 | CellLock → `LockCell(row,col)` into `LockedCells` | `Assets/Scripts/Run/RunDirector.cs` | `LockCell` | ✅ |
| DEBUFF-HOOK-007 | `TickLockedCells()` after every correct placement | `Assets/Scripts/Run/RunDirector.cs` | `TickLockedCells` | ✅ |
| DEBUFF-HOOK-008 | `GetLockedCells()` for input blocking + board tint | `Assets/Scripts/Run/RunDirector.cs` | `GetLockedCells` | ✅ |
| DEBUFF-HOOK-009 | `LevelState.LockedCells` `[NonSerialized]` dict | `Assets/Scripts/Core/RuntimeModels.cs` | `LevelState` | ✅ |
| DEBUFF-HOOK-010 | `BossModifierId` enum 94–102 | `Assets/Scripts/Core/GameEnums.cs` | `BossModifierId` | ✅ |
| DEBUFF-HOOK-011 | `BossService` ModifierData IDs 94–102; IDs ≥94 excluded from pool | `Assets/Scripts/Boss/BossService.cs` | `ModifierData`, `BuildEligiblePool` | ✅ |
| DEBUFF-HOOK-012 | `BoardViewController` CellLocked tint (deep magenta) | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` | ✅ |
| DEBUFF-HOOK-013 | `InRunController` blocks input on locked cells | `Assets/Scripts/UI/InRunController.cs` | Input handlers | ✅ |

---

## Curse System

**Spec:** `docs/CurseSystem.md` v1.1

### Acquisition

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| CURSE-ACQUIRE-001 | Run events offer curses as tradeoffs (Ink Peddler, Tempting Shrine) | `Assets/Scripts/Run/RunEventService.cs` | `ResolveChoice` | ✅ |
| CURSE-ACQUIRE-002 | 4 curse-bearing items apply curse on use | `Assets/Scripts/Items/ItemService.cs` | `UseItem` | ✅ |
| CURSE-ACQUIRE-003 | Curse items labelled with curse cost in tooltip | `Assets/Scripts/Items/ItemService.cs` | Item catalog | ✅ |
| CURSE-ACQUIRE-004 | Rest node 4-way when curses active (adds Cleanse option) | `Assets/Scripts/Run/RunDirector.cs` | `AcceptRestCurseRemoval` | ✅ |
| CURSE-ACQUIRE-005 | Rest node 3-way when no curses active | `Assets/Scripts/Run/RunDirector.cs` | Rest handlers | ✅ |

### Curse Pool

| ID | Curse ID | Name | Severity | Status |
|----|----------|------|----------|--------|
| CURSE-POOL-001 | `hollow_pencil` | Hollow Pencil | Mild | ✅ |
| CURSE-POOL-002 | `fog_of_memory` | Fog of Memory | Mild | ✅ |
| CURSE-POOL-003 | `misfortune` | Misfortune | Mild | ✅ |
| CURSE-POOL-004 | `bad_luck` | Bad Luck | Mild | ✅ |
| CURSE-POOL-005 | `price_gouge` | Price Gouge | Mild | ✅ |
| CURSE-POOL-006 | `inkblot` | Inkblot | Medium | ✅ |
| CURSE-POOL-007 | `crumbling_focus` | Crumbling Focus | Medium | ✅ |
| CURSE-POOL-008 | `sealed_eyes` | Sealed Eyes | Medium | ✅ |
| CURSE-POOL-009 | `blurred_sight` | Blurred Sight | Medium | ✅ |
| CURSE-POOL-010 | `restless_inventory` | Restless Inventory | Medium | ✅ |
| CURSE-POOL-011 | `phantom_pain` | Phantom Pain | Nasty | ✅ |
| CURSE-POOL-012 | `weight_of_stone` | Weight of Stone | Nasty | ✅ |
| CURSE-POOL-013 | `trembling_hand` | Trembling Hand | Nasty | ✅ |
| CURSE-POOL-014 | `fraying_thread` | Fraying Thread | Nasty | ✅ |
| CURSE-POOL-015 | `double_or_nothing` | Double or Nothing | Nasty | ✅ |

### Stacking Rules

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| CURSE-STACK-001 | Unlimited curse stacking (no cap) | `Assets/Scripts/Run/CurseService.cs` | `ApplyCurse` | ✅ |
| CURSE-STACK-002 | No duplicates — re-roll until different | `Assets/Scripts/Run/CurseService.cs` | `RollCurse` | ✅ |
| CURSE-STACK-003 | Curses persist across all 5 floors | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState.ActiveCurseIds` | ✅ |

### Removal

| ID | Method | File | Status |
|----|--------|------|--------|
| CURSE-REMOVE-001 | Rest node "Cleanse a Curse" removes oldest | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| CURSE-REMOVE-002 | Error-free puzzle: 25% chance to auto-clear oldest | `Assets/Scripts/Run/CurseService.cs` | ✅ |
| CURSE-REMOVE-005 | GardenMonk increases auto-clear to 50% | `Assets/Scripts/Run/CurseService.cs` | ✅ |

### Integration Points

| ID | Location | File | Status |
|----|----------|------|--------|
| CURSE-INT-001 | `RunEventService.ResolveChoice()` calls `ApplyCurse` | `Assets/Scripts/Run/RunEventService.cs` | ✅ |
| CURSE-INT-002 | `RunDirector.PlaceNumber()`: phantom_pain 2×HP; trembling_hand 1HP first placement | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| CURSE-INT-003 | `CompleteLevelAndGrantRewards()`: misfortune slot−1; restless_inventory drain | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| CURSE-INT-004 | `AdvanceToNextFloor()`: fraying_thread −1 MaxHP | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| CURSE-INT-005 | `ShopService.BuildOffers()`: price_gouge ×1.35 | `Assets/Scripts/Economy/RelicService.cs` | ✅ |
| CURSE-INT-006 | Board setup: inkblot marks 3 cells as fogged | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| CURSE-INT-007 | `ItemService.UseItem()`: 4 curse-bearing items call `ApplyCurse` | `Assets/Scripts/Items/ItemService.cs` | ✅ |
| CURSE-INT-008 | `BuildItemRewardSlots()`: bad_luck +15% Nothing; double_or_nothing 20% re-roll | `Assets/Scripts/UI/InRunController.cs` | ✅ |
| CURSE-INT-009 | blurred_sight: coroutine fades pencilmarks after 1.5s | `Assets/Scripts/UI/InRunController.cs` | ✅ |
| CURSE-INT-010 | Rest cleanse wired via `AcceptRestCurseRemoval()` | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| CURSE-INT-011 | `CursePanelController` renders active curses, expandable | `Assets/Scripts/UI/InRunController.cs` | ✅ |
| CURSE-INT-012 | `ActiveCurseIds` serialized via `RunState` (no extra change) | `Assets/Scripts/Core/RuntimeModels.cs` | ✅ |

---

## Meta Progression

**Spec:** `docs/MetaProgressionSystem.md` v1.3

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| META-UNLOCK-001 | 8 classes with cumulative cross-class unlock conditions | `Assets/Scripts/Core/RuntimeModels.cs` | `ClassUnlockProgress` | ✅ |
| META-UNLOCK-002 | `RecordRunAndGetNewUnlocks()` updates counters + `EvaluateUnlocks()` | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | ✅ |
| META-UNLOCK-003 | Unlocked classes: full stat block + passive + level | `Assets/Scripts/UI/InRunController.cs` | Class select panel | ✅ |
| META-UNLOCK-004 | Locked classes: greyed out + unlock condition text | `Assets/Scripts/UI/InRunController.cs` | Class select panel | ✅ |
| META-XP-001 | Per-class `TotalXp` stored; level derived via `DeriveLevel()` | `Assets/Scripts/Core/SaveModels.cs` | `ClassGardenProgressEntry` | ✅ |
| META-XP-002 | Two-phase XP curve L1–15 + L16–40 | `Assets/Scripts/Run/RunDirector.cs` | `XpService.DeriveLevel` | ✅ |
| META-XP-003 | Total to L40: 16,860 XP | `Assets/Scripts/Run/RunDirector.cs` | `XpService` constants | ✅ |
| META-XP-004 | XP committed at run end (no mid-run leveling) | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | ✅ |
| META-PRESTIGE-001 | Max prestige tier 9 per class | `Assets/Scripts/Core/SaveModels.cs` | `ClassGardenProgressEntry.PrestigeTier` | ✅ |
| META-PRESTIGE-002 | Prestige requires Level 40 | `Assets/Scripts/Save/ProfileService.cs` | Prestige gate check | ✅ |
| META-PRESTIGE-003 | Boss gate: `3 × (PrestigeTier + 1)` boss defeats | `Assets/Scripts/Save/ProfileService.cs` | Prestige gate check | ✅ |
| META-PRESTIGE-004 | On prestige: TotalXp=0, PrestigeTier++ | `Assets/Scripts/Save/ProfileService.cs` | `TryPrestige` | ✅ |
| META-ASCEND-001 | Ascension is a global layer above per-class prestige | `Assets/Scripts/Core/SaveModels.cs` | `MetaProgressionState.AscensionLevel` | ✅ |
| META-ASCEND-002 | `ApplyAscension`: increments AscensionLevel, +1 MaxStarCap, unlocks seasonal | `Assets/Scripts/Meta/AscensionService.cs` | `ApplyAscension` | ✅ |
| META-ASCEND-003 | `TryPrestigeReset`: resets AscensionLevel=0, MaxStarCap=5 | `Assets/Scripts/Meta/AscensionService.cs` | `TryPrestigeReset` | ✅ |
| META-ASCEND-004 | Monthly seed: `(year × 100) + month` | `Assets/Scripts/Meta/AscensionService.cs` | `BuildMonthlySeed` | ✅ |
| META-COMPLETE-001 | `CompletionService.Recalculate()` — 4 checks × 25% | `Assets/Scripts/Meta/CompletionService.cs` | `Recalculate` | ✅ |
| META-CODEX-001 | Item & Relic Codex tracks discovery | `Assets/Scripts/Save/ProfileService.cs` | `RecordItemDiscovery`, `RecordRelicDiscovery` | ✅ |
| META-CODEX-002 | Entry marked Discovered on first acquisition only | `Assets/Scripts/Save/ProfileService.cs` | `RecordItemDiscovery` | ✅ |

---

## Endless Zen Mode

**Spec:** `docs/EndlessZenMode.md` v1.2

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| ZENMODE-UNLOCK-001 | Unlocked when `TotalRuns ≥ 10` via `RecordRunAndGetNewUnlocks` | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | ✅ |
| ZENMODE-SESSION-001 | Session: 9×9 board, no timer, no XP, no items/relics, HP=0 ends session | `Assets/Scripts/Run/RunDirector.cs` | Endless mode setup | ✅ |
| ZENMODE-SESSION-002 | HP uses class base HP (class passives still apply) | `Assets/Scripts/Run/RunDirector.cs` | Endless mode setup | ✅ |
| ZENMODE-DEPTH-001 | Depth starts at 0, increments after each solved puzzle | `Assets/Scripts/Run/RunDirector.cs` | `EndlessZenService.BuildLevel` | ✅ |
| ZENMODE-DEPTH-002 | Stars formula: `Clamp(1 + floor(depth/4), 1, 5)` | `Assets/Scripts/Run/RunDirector.cs` | `EndlessZenService.BuildLevel` | ✅ |
| ZENMODE-DEPTH-003 | Stars capped at 5★ in Endless Zen | `Assets/Scripts/Run/RunDirector.cs` | `EndlessZenService.BuildLevel` | ✅ |
| ZENMODE-DEPTH-004 | Modifier cap formula: <10=1, <20=2, 20+=3 | `Assets/Scripts/Run/RunDirector.cs` | `EndlessZenService.BuildLevel` | ✅ |
| ZENMODE-RES-001 | Between levels: +1 HP, +3 Pencil restored | `Assets/Scripts/Run/RunDirector.cs` | Level completion | ✅ |
| ZENMODE-SCORE-001 | `ProfileStats.HighestEndlessDepth` tracks best depth | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | ✅ |

---

## Spirit Trials Mode

**Spec:** `docs/SpiritTrialsMode.md` v1.2

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| TRIAL-UNLOCK-001 | Unlocked via full 5-floor Garden Run OR Ascension Level 1 | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | ✅ |
| TRIAL-SESSION-001 | Session: 9×9, timer, no XP, no gold, 1 starting item | `Assets/Scripts/Run/RunDirector.cs` | Spirit Trials setup | ✅ |
| TRIAL-SESSION-002 | Timer starts on first cell interaction | `Assets/Scripts/UI/InRunController.cs` | Input handler | ✅ |
| TRIAL-TIER-001 | 4 tiers: Apprentice/Adept/Master/Grandmaster (star + modifier counts) | `Assets/Scripts/Run/RunDirector.cs` | `SpiritTrialsService` | ✅ |
| TRIAL-TIER-002 | Modifiers assigned randomly (seeded) — player cannot choose | `Assets/Scripts/Run/RunDirector.cs` | `SpiritTrialsService` | ✅ |
| TRIAL-SCORE-001 | BasePoints = `CellsToFill × PointsPerCell` (per tier) | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | ✅ |
| TRIAL-SCORE-002 | SpeedMultiplier = `max(0.5, 2.0 - (ElapsedSeconds / ParTime))` | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | ✅ |
| TRIAL-SCORE-003 | Modifier constraint bonus: 0=+0, 1=+100, 2=+300 | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | ✅ |
| TRIAL-SCORE-004 | Mistake penalty per tier; HP=0 ends session | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | ✅ |
| TRIAL-SCORE-005 | FinalScore = `floor((Base × Speed) + Constraint + Pencil − Mistake)` | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | ✅ |
| TRIAL-SCORE-006 | Minimum score 0 (never negative) | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | ✅ |
| TRIAL-LEAD-001 | Per-tier personal bests in `ProfileStats` | `Assets/Scripts/Save/ProfileService.cs` | `ProfileStats` | ✅ |
| TRIAL-LEAD-002 | Monthly leaderboard when `SeasonalChallengeUnlocked` | `Assets/Scripts/Meta/AscensionService.cs` | `BuildMonthlySeed` | ✅ |

---

## Sudoku Basics Tutorial

**Spec:** `docs/FirstTimePlayTutorial.md` v1.1

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| TUTO-BASICS-TRIGGER-001 | Triggers when `"sudoku_basics"` not in `CompletedKeys` | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `Start()` | ✅ |
| TUTO-BASICS-TRIGGER-002 | Only triggers once (check skipped on subsequent launches) | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `Start()` | ✅ |
| TUTO-BASICS-TRIGGER-003 | Prompt appears after main menu loads (not before) | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `ShowSudokuBasicsPrompt` | ✅ |
| TUTO-BASICS-BOARD-001 | Fixed 4×4 board, 2×2 boxes, ~8 given cells | `Assets/Scripts/Run/RunDirector.cs` | `TutorialModeService.BuildSudokuBasicsLevel` | ✅ |
| TUTO-BASICS-BOARD-002 | Mistakes during tutorial not penalised | `Assets/Scripts/Run/RunDirector.cs` | Tutorial RunState | ✅ |
| TUTO-BASICS-STEP-001 | 7 named steps with highlighted overlays + text | `Assets/Scripts/UI/InRunController.cs` | `SudokuBasicsTutorialController` | ✅ |
| TUTO-BASICS-STEP-002 | Player advances via "Next" button or by performing action | `Assets/Scripts/UI/InRunController.cs` | `SudokuBasicsTutorialController` | ✅ |
| TUTO-BASICS-STEP-006 | Step 6 requires player to place digit 2 in target cell | `Assets/Scripts/UI/InRunController.cs` | `SudokuBasicsTutorialController` | ✅ |
| TUTO-BASICS-DONE-001 | Completion writes `"sudoku_basics"` to `CompletedKeys` + saves | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `MarkSudokuBasicsComplete` | ✅ |
| TUTO-BASICS-DONE-002 | Replayable via Options → "Replay Sudoku Basics" | `Assets/Scripts/UI/InRunController.cs` | `MainMenuController` options | ✅ |

---

## Sudoku Generation Pipeline

**Spec:** `docs/SudokuGenerationPipeline.md` v1.5

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| GEN-REGION-001 | `BuildRegionMap(size, variant)` — 4 variants (0=Standard, 1=Alt, 2=Irregular A, 3=Jigsaw) | `Assets/Scripts/Core/SudokuGenerator.cs` | `BuildRegionMap` | ✅ |
| GEN-REGION-002 | All templates: each region = `size` cells, contiguous, count = board size | `Assets/Scripts/Core/SudokuGenerator.cs` | Template methods | ✅ |
| GEN-REGION-003 | `AllowIrregularPuzzles=false` restricts to variants 0–1 | `Assets/Scripts/Run/RunDirector.cs` | `BuildLevelConfig` | ✅ |
| GEN-SOLVE-001 | `GenerateSolvedBoard(size, regionMap, rng)` — backtracking + MRV heuristic + seeded shuffle | `Assets/Scripts/Core/SudokuGenerator.cs` | `GenerateSolvedBoard` / `FillBoard` | ✅ |
| GEN-REMOVE-001 | Cell removal to create player-facing puzzle | `Assets/Scripts/Core/SudokuGenerator.cs` | `RemoveCells` | ✅ |
| GEN-REMOVE-002 | `StarDensityService.MissingPercentForStars`: `(stars+3)×0.1`, clamped [0.01, 0.95] | `Assets/Scripts/Core/StarDensityService.cs` | `MissingPercentForStars` | ✅ |
| GEN-REMOVE-003 | 7★ removes all cells; requires ≥1 modifier; tutorial-only | `Assets/Scripts/Core/StarDensityService.cs` | 7★ case | ✅ |
| GEN-REMOVE-004 | 7★ clamp bypassed (`removeCount = totalCells`) | `Assets/Scripts/Core/SudokuGenerator.cs` | `RemoveCells` | ✅ |
| GEN-MOD-001 | `ModifierGeometryGenerator.Generate(board, modifiers, seed, intensity)` | `Assets/Scripts/Core/ModifierGeometryGenerator.cs` | `Generate` | ✅ |
| GEN-VALID-001 | Placement valid if satisfies all constraints (not compared to solution) | `Assets/Scripts/Core/SudokuBoard.cs` | `IsCorrectAt`, `IsComplete` | ✅ |
| GEN-VALID-002 | Validation: `PlaceNumber` → `IsMoveValid` → `SudokuConstraintEngine.ValidateAll` | `Assets/Scripts/Run/RunDirector.cs` | `PlaceNumber` | ✅ |

---

## Path System & Garden Overview

**Spec:** `docs/PathSystem_GardenOverview.md` v1.5

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| MAP-STRUCT-001 | 5 floors per run; each floor is a garden with distinct theme | `Assets/Scripts/Run/RunDirector.cs` | Floor management | ✅ |
| MAP-STRUCT-002 | Each floor: Shared Start → Calm/Risk Route → Shared Boss Gate | `Assets/Scripts/Run/RunGraphService.cs` | Graph generation | ✅ |
| MAP-ROUTE-001 | Calm Route: longer, lower difficulty, more safe tiles, upper half | `Assets/Scripts/Run/RunGraphService.cs` | `BuildCalmRoute` | ✅ |
| MAP-ROUTE-002 | Risk Route: shorter, higher difficulty, fewer safe tiles, lower half | `Assets/Scripts/Run/RunGraphService.cs` | `BuildRiskRoute` | ✅ |
| MAP-CROSS-001 | Exactly one cross-connection per floor (lane switch opportunity) | `Assets/Scripts/Run/RunGraphService.cs` | Cross-link logic | ✅ |
| MAP-NODE-CURSED-001 | Cursed node shows Accept/Decline panel before puzzle | `Assets/Scripts/Run/RunDirector.cs` | Cursed node handling | ✅ |
| MAP-NODE-CURSED-002 | Accept: IsCursed=true, ×1.5 gold+XP, +1 random modifier | `Assets/Scripts/Run/RunDirector.cs` | `AcceptCursedNode` | ✅ |
| MAP-NODE-CURSED-003 | Decline: standard puzzle, no multipliers | `Assets/Scripts/Run/RunDirector.cs` | `DeclineCursedNode` | ✅ |
| MAP-NODE-CURSED-004 | `CursedPuzzlesAccepted` tracked for run score breakdown | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState` | ✅ |
| MAP-FX-001 | One random positive floor effect per floor | `Assets/Scripts/Run/RunDirector.cs` | `RollPositiveFloorEffect` | ✅ |
| MAP-FX-002 | Floor effect seeded from `RunState.Seed + floorIndex` | `Assets/Scripts/Run/RunDirector.cs` | `RollPositiveFloorEffect` | ✅ |
| MAP-FX-003 | Stored in `RunState.HasPositiveFloorEffect + ActivePositiveFloorEffect` | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState` | ✅ |
| MAP-FX-004 | Shown on path screen as banner via `GetPositiveFloorEffectLabel()` | `Assets/Scripts/Run/RunMapController.cs` | `GetPositiveFloorEffectLabel` | ✅ |
| MAP-FX-005 | None effect has equal probability (20% each) | `Assets/Scripts/Run/RunDirector.cs` | `RollPositiveFloorEffect` | ✅ |

---

## Boss Mechanics

**Spec:** `docs/BossMechanicsSystem.md` — partial IDs (pending full spec update)

Key requirements (see spec for full list):

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| BOSS-MOD-001 | 15 boss modifiers (IDs 0–14) in `BossModifierId` enum | `Assets/Scripts/Core/GameEnums.cs` | `BossModifierId` | ✅ |
| BOSS-MOD-002 | `BossService.ModifierData` — tier/impact/name/description per modifier | `Assets/Scripts/Boss/BossService.cs` | `ModifierData` | ✅ |
| BOSS-MOD-003 | `ModifierGeometryGenerator.Generate` produces overlay for each modifier | `Assets/Scripts/Core/ModifierGeometryGenerator.cs` | `Generate` | ✅ |
| BOSS-INT-001 | `BossModifierIntensity` enum (Low/Medium/High/VeryHigh) scales geometry | `Assets/Scripts/Core/GameEnums.cs` | `BossModifierIntensity` | ✅ |
| BOSS-INT-002 | Intensity set by `RunNumber` in `BuildLevelConfig` (1-2=Low, 3-5=Med, 6-8=High, 9+=VH) | `Assets/Scripts/Run/RunDirector.cs` | `BuildLevelConfig` | ✅ |
| BOSS-FLOOR-001 | Floor modifier system: floors 2–5 add persistent modifiers to all puzzles | `Assets/Scripts/Run/RunDirector.cs` | `RollFloorModifiers` | ✅ |
| BOSS-FLOOR-002 | Boss choice pool excludes active floor modifiers | `Assets/Scripts/Boss/BossService.cs` | `BuildEligiblePool` | ✅ |
| BOSS-FLOOR-003 | `RunState.FloorModifiers` stored for persistence | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState.FloorModifiers` | ✅ |

---

## Class XP Progression

**Spec:** `docs/ClassXpProgressionSystem.md` — partial IDs (pending full spec update)

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| XP-TILE-001 | `XpService.CalculateTile(boardSize, stars, modCount, isBoss, perfect)` → TileXpEntry | `Assets/Scripts/Run/RunDirector.cs` | `XpService.CalculateTile` | ✅ |
| XP-TILE-002 | Base XP per board size: 5×5=30, 6×6=45, 7×7=60, 8×8=90, 9×9=120 | `Assets/Scripts/Run/RunDirector.cs` | `XpService` | ✅ |
| XP-TILE-003 | Star multiplier: 1★=×1.0, 2★=×1.2, 3-4★=×1.5, 5-6★=×2.0 | `Assets/Scripts/Run/RunDirector.cs` | `XpService` | ✅ |
| XP-TILE-004 | Boss ×1.5; Mod bonus +15 per active mod (boss only); Perfect +25 flat | `Assets/Scripts/Run/RunDirector.cs` | `XpService` | ✅ |
| XP-CURVE-001 | L1-15: `50+(n-1)×10`; L16-40: `190+(n-15)×35` | `Assets/Scripts/Run/RunDirector.cs` | `XpService.DeriveLevel` | ✅ |
| XP-CURVE-002 | `DeriveLevel(totalXp)` derives level at runtime from cumulative XP | `Assets/Scripts/Run/RunDirector.cs` | `XpService.DeriveLevel` | ✅ |
| XP-LOG-001 | `RunDirector._tileXpLog` tracks per-tile XP for end-of-run breakdown | `Assets/Scripts/Run/RunDirector.cs` | `_tileXpLog` | ✅ |
| XP-COMMIT-001 | XP committed to class at run end in `RecordRunAndGetNewUnlocks` | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | ✅ |

---

## Items & Relics

**Spec:** `docs/ItemsAndRelicsSystem.md` — partial IDs (pending full spec update)

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| ITEM-SLOT-001 | Item reward slots by star: 1★=2, 2★=3, 3★=3, 4★=4, 5★=5 | `Assets/Scripts/Items/ItemService.cs` | `RollRewardSlots` | ✅ |
| ITEM-SLOT-002 | Nothing slot probability: 1★=25%, 2★=22%, 3★=18%, 4★=15%, 5★=12% | `Assets/Scripts/Items/ItemService.cs` | `RollRewardSlots` | ✅ |
| ITEM-EPIC-001 | Epic items only available at class Level 15+ | `Assets/Scripts/Items/ItemService.cs` | `BuildItemPool` | ✅ |
| RELIC-SLOT-001 | Single relic slot: `RunState.HasRelic + HeldRelic` | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState` | ✅ |
| RELIC-ROLL-001 | Relic tier weights: floor-based 5×5 table; risk route shifts weights up | `Assets/Scripts/Economy/RelicService.cs` | `RollRelic` | ✅ |
| SHOP-PRICE-001 | Shop price: `BasePrice × (1 + FloorIndex × 0.5) × priceMultiplier` | `Assets/Scripts/Economy/RelicService.cs` | `ShopService.BuildOffers` | ✅ |

---

## Save / Load

**Spec:** `docs/SaveLoadArchitecture.md` — partial IDs (existing REQ-SAVE-018..039 stubs pending update)

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| SAVE-DUAL-001 | Dual-file architecture: profile save + run save | `Assets/Scripts/Save/SaveFileService.cs` | File management | ✅ |
| SAVE-SLOT-001 | 3 profile slots: `save_profile_0/1/2.json` | `Assets/Scripts/Save/SaveFileService.cs` | Constructor | ✅ |
| SAVE-SLOT-002 | `SaveProfileService` manages all 3 slots; `ActiveSlot` in `PlayerPrefs` | `Assets/Scripts/Save/ProfileService.cs` | `SaveProfileService` | ✅ |
| SAVE-SLOT-003 | `GameBootstrap.ApplySlotChange()` re-wires save services after slot switch | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `ApplySlotChange` | ✅ |
| SAVE-AUTO-001 | `RunAutoSaveCoordinator.Save()` — atomic write before serializing | `Assets/Scripts/Save/RunAutoSaveCoordinator.cs` | `Save` | ✅ |
| SAVE-RESUME-001 | `RunResumeService.TryResumeFromSave()` restores full run state including boss modifier | `Assets/Scripts/Save/RunResumeService.cs` | `TryResumeFromSave` | ✅ |
| SAVE-SERIAL-001 | All save data uses `JsonUtility` (`[Serializable]` on all data classes) | `Assets/Scripts/Core/SaveModels.cs` | Data models | ✅ |
| SAVE-SERIAL-002 | Nullable types use bool+value pattern (`HasChosenBossModifier + ChosenBossModifierId`) | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState` | ✅ |
| SAVE-SERIAL-003 | `HashSet<T>` not serialized — use `List<T>` + sync methods | `Assets/Scripts/Core/RuntimeModels.cs` | `SeenBossModifierList` | ✅ |
| SAVE-TUTO-001 | Tutorial saves excluded: `RunAutoSaveCoordinator.Save` skips `TutorialMode` runs | `Assets/Scripts/Save/RunAutoSaveCoordinator.cs` | `Save` | ✅ |

---

## Input & Profiles

**Spec:** `docs/InputAndProfilesSpec.md` — partial IDs (pending full spec update)

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| INPUT-REMAP-001 | `InputRemapService` — keyboard defaults + gamepad button mappings | `Assets/Scripts/Core/InputRemapService.cs` | Constructor | ✅ |
| INPUT-REMAP-002 | `WasActionPressed(action)` — checks keyboard then gamepad then axis crossing | `Assets/Scripts/Core/InputRemapService.cs` | `WasActionPressed` | ✅ |
| INPUT-REMAP-003 | `GetAnyKeyDown()` — returns first pressed key (keyboard or joystick) | `Assets/Scripts/Core/InputRemapService.cs` | `GetAnyKeyDown` | ✅ |
| INPUT-REMAP-004 | `KeybindingsPanelController` manages listen/capture/save rebind flow | `Assets/Scripts/Core/InputRemapService.cs` | `KeybindingsPanelController` | ✅ |
| INPUT-PROFILE-001 | `MenuScreen.ProfileSelect` — shown on first launch if no files exist | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `Start` | ✅ |
| INPUT-PROFILE-002 | `SelectProfileSlot(i)`, `DeleteProfileSlot(i)`, `RefreshProfileSelectCards()` | `Assets/Scripts/UI/InRunController.cs` | `MainMenuController` | ✅ |

---

## Accessibility

**Spec:** `docs/AccessibilitySpec.md` — partial IDs (existing REQ-UI-018..026 stubs pending update)

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| ACCESS-COLOR-001 | `AccessibilityService` — colorblind mode, high contrast, reduce motion, alt symbols | `Assets/Scripts/Core/AccessibilityService.cs` | Service | ✅ |
| ACCESS-COLOR-002 | Accessibility toggles call `NotifyAccessibilityChanged` → rebuild overlays | `Assets/Scripts/UI/InRunController.cs` | `OptionsController` | ✅ |
| ACCESS-MOTION-001 | Screen shake disabled when `ReduceMotion` on | `Assets/Scripts/UI/InRunController.cs` | Shake helper | ✅ |
| ACCESS-OVERLAY-001 | High contrast: overlay spine alpha ×1.4, line width ×1.5 | `Assets/Scripts/UI/BoardViewController.cs` | Overlay render | ✅ |
| ACCESS-PARTICLE-001 | All particle effects disabled when `ReduceMotion` active | `Assets/Scripts/UI/InRunController.cs` | `AmbientParticleController` | ✅ |

---

## Gold Economy

**Spec:** `docs/GoldEconomySystem.md` — partial IDs (pending full spec update)

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| ECON-REWARD-001 | Gold awarded on puzzle completion via gold formula | `Assets/Scripts/Run/RunDirector.cs` | `CompleteLevelAndGrantRewards` | ✅ |
| ECON-SHOP-001 | Shop: 3 items + optional relic per visit | `Assets/Scripts/Economy/RelicService.cs` | `ShopService.BuildOffers` | ✅ |
| ECON-SHOP-002 | Price scaling: `BasePrice × (1 + FloorIndex × 0.5) × priceMultiplier` | `Assets/Scripts/Economy/RelicService.cs` | `ShopService.BuildOffers` | ✅ |

---

## Seasonal Challenge

**Spec:** `docs/SeasonalChallengeSpec.md` — **Status: 🆕 Not yet implemented**

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| SEASON-UNLOCK-001 | Unlocked after first ascension (`SeasonalChallengeUnlocked = true`) | `Assets/Scripts/Meta/AscensionService.cs` | `ApplyAscension` | ✅ seed logic |
| SEASON-SEED-001 | Monthly seed: `(year × 100) + month` | `Assets/Scripts/Meta/AscensionService.cs` | `BuildMonthlySeed` | ✅ |
| SEASON-PUZZLE-001 | Fixed 9×9 4★ puzzle from monthly seed | — | — | 🆕 |
| SEASON-SCORE-001 | Personal best stored locally | — | `SeasonalChallengeState` | 🆕 |
| SEASON-UI-001 | Prominent main menu panel with countdown | — | — | 🆕 |
| SEASON-UI-002 | `GameMode.SeasonalChallenge` enum value | `Assets/Scripts/Core/GameEnums.cs` | `GameMode` | 🆕 |

---

## Daily Goal System

**Spec:** `docs/DailyGoalSystem.md` — **Status: 🆕 Not yet implemented**

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| DAILY-GOAL-001 | 3 daily goals per day (date-seeded, no network) | — | — | 🆕 |
| DAILY-GOAL-002 | Ink Stamps cosmetic currency from goal completion | — | — | 🆕 |
| DAILY-GOAL-003 | Streak tracking (consecutive days) | — | `DailyGoalState` | 🆕 |
| DAILY-GOAL-004 | Daily Walk widget on main menu | — | — | 🆕 |
| DAILY-GOAL-005 | `DailyGoalState` in `ProfileSaveData` | `Assets/Scripts/Core/SaveModels.cs` | `ProfileSaveData` | 🆕 |

---

## Class Progression Unlocks

**Spec:** `docs/ClassProgressionUnlocks.md` — **Status: 🆕 Not yet implemented**

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| CLASS-EXCL-001 | 16 class-exclusive unlocks: 8 items (L15) + 8 relics (L30) per class | — | — | 🆕 |
| CLASS-EXCL-002 | `ClassExclusive` flag on item/relic definitions | — | Item/relic catalog | 🆕 |
| CLASS-EXCL-003 | `ClassUnlockLevel` (15 or 30) on item/relic definitions | — | Item/relic catalog | 🆕 |
| CLASS-EXCL-004 | Exclusive items/relics only rollable for their associated class | — | `ItemService`, `RelicService` | 🆕 |

---

## Multi-Relic Save Compatibility

**Spec:** `docs/ItemsAndRelicsSystem.md` — section **Single Relic Slot** | **Status: ✅ Implemented (introduced by linter change)**

> These IDs formalise code that exists in `RuntimeModels.cs` and `RunResumeService.cs` but was never spec'd.  
> `RELIC-SLOT-MULTI` (the orphaned annotation) is now resolved as `RELIC-SLOT-002`.

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| RELIC-SLOT-002 | `RunState.HeldRelics: List<RelicInstance>` — multi-relic list field (future-proof; currently max 1 entry enforced by game logic) | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState.HeldRelics` | ✅ |
| RELIC-SLOT-003 | Legacy migration on resume: if `HasRelic && HeldRelic != null && HeldRelics empty` → migrate single relic into list | `Assets/Scripts/Save/RunResumeService.cs` | `RestoreTransientState` | ✅ |
