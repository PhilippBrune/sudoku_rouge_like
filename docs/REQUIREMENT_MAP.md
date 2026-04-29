# Requirement Traceability Map â€” All Systems

**Updated:** 2026-04-15

Legend: âœ… Implemented | âš ï¸ Partial | âŒ Not Implemented | ðŸ†• New (spec only, no code yet)

---

## Index

| System Prefix | Spec File | Status |
|--------------|-----------|--------|
| [PRESSURE](#pressure-mechanics) | `docs/PressureMechanicsSpec.md` | âœ… |
| [DEBUFF](#boss-debuffs) | `docs/BossDebuffEffects.md` | âœ… (9 of 18) |
| [CURSE](#curse-system) | `docs/CurseSystem.md` | âœ… |
| [META](#meta-progression) | `docs/MetaProgressionSystem.md` | âœ… |
| [ZENMODE](#endless-zen-mode) | `docs/EndlessZenMode.md` | âœ… |
| [TRIAL](#spirit-trials-mode) | `docs/SpiritTrialsMode.md` | âœ… |
| [TUTO-BASICS](#sudoku-basics-tutorial) | `docs/FirstTimePlayTutorial.md` | âœ… |
| [GEN](#sudoku-generation-pipeline) | `docs/SudokuGenerationPipeline.md` | âœ… |
| [MAP](#path-system--garden-overview) | `docs/PathSystem_GardenOverview.md` | âœ… |
| [BOSS](#boss-mechanics) | `docs/BossMechanicsSystem.md` | âœ… |
| [XP](#class-xp-progression) | `docs/ClassXpProgressionSystem.md` | âœ… |
| [ITEM](#items--relics) | `docs/ItemsAndRelicsSystem.md` | âœ… |
| [SAVE](#save--load) | `docs/SaveLoadArchitecture.md` | âœ… |
| [INPUT](#input--profiles) | `docs/InputAndProfilesSpec.md` | âœ… |
| [ACCESS](#accessibility) | `docs/AccessibilitySpec.md` | âœ… |
| [ECON](#gold-economy) | `docs/GoldEconomySystem.md` | âœ… |
| [SEASON](#seasonal-challenge) | `docs/SeasonalChallengeSpec.md` | ðŸ†• |
| [DAILY](#daily-goal-system) | `docs/DailyGoalSystem.md` | ðŸ†• |
| [CLASS](#class-progression-unlocks) | `docs/ClassProgressionUnlocks.md` | ðŸ†• |

---

## Pressure Mechanics

**Spec:** `docs/PressureMechanicsSpec.md` v0.5 | **Plan:** `docs/plan/PressureMechanicsPlan.md`

### Rolling & Activation

| ID | Description | File | Method / Location | Status |
|----|-------------|------|-------------------|--------|
| PRESSURE-ROLL-001 | No floor 1 or tutorial mode | `Assets/Scripts/Run/RunDirector.cs` | `RollPressureMechanic` â€” first two guards | âœ… |
| PRESSURE-ROLL-002 | Elite/pre-boss/boss nodes only | `Assets/Scripts/Run/RunDirector.cs` | `RollPressureMechanic` â€” node type guard | âœ… |
| PRESSURE-ROLL-002 | `LevelConfig` node flags | `Assets/Scripts/Core/RuntimeModels.cs` | `LevelConfig.IsElite`, `LevelConfig.IsPreBoss` | âœ… |
| PRESSURE-ROLL-003 | Floor probability table | `Assets/Scripts/Run/RunDirector.cs` | `RollPressureMechanic` â€” `chance` switch | âœ… |
| PRESSURE-ROLL-004 | One mechanic per puzzle max | `Assets/Scripts/Run/RunDirector.cs` | `RollPressureMechanic` â€” `Contains` dedup guard | âœ… |
| PRESSURE-ROLL-004 | Modifier data registration | `Assets/Scripts/Boss/BossService.cs` | `ModifierData` â€” IDs 103â€“106 | âœ… |
| PRESSURE-ROLL-005 | PressureWave from floor 3+ | `Assets/Scripts/Run/RunDirector.cs` | `RollPressureMechanic` â€” `if (floor >= 3)` | âœ… |

### Initialization

| ID | Description | File | Method / Location | Status |
|----|-------------|------|-------------------|--------|
| PRESSURE-INIT-001 | CountdownFill scope/cells/counter init | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` â€” CountdownFill case | âœ… |
| PRESSURE-INIT-002 | HauntedCell intensity-keyed selection | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` â€” HauntedCell case; `SelectHauntedCell` | âœ… |
| PRESSURE-INIT-003 | CrumblingRegion one-region fragility init | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` â€” CrumblingRegion case; `SelectCrumblingRegion` | âœ… |
| PRESSURE-INIT-004 | PressureWave interval/counter init | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` â€” PressureWave case | âœ… |
| PRESSURE-INIT-005 | Chain length 3â€“4; fallback to SingleCell | `Assets/Scripts/Run/RunDirector.cs` | `SelectThreatCells` â€” Chain case | âœ… |
| PRESSURE-INIT-006 | PresolvedCells excluded | `Assets/Scripts/Run/RunDirector.cs` | `SelectThreatCells`, `SelectHauntedCell`, `SpawnWaveThreat` | âœ… |
| PRESSURE-INIT-007 | counting_shadow curse threat | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` â€” curse block | âœ… |
| PRESSURE-INIT-008 | hollow_eye curse haunted cell | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` â€” curse block | âœ… |

### Core Tick Logic

| ID | Description | File | Method / Location | Status |
|----|-------------|------|-------------------|--------|
| PRESSURE-TICK-001 | Correct placement decrements all counters | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` â€” counter loop step 1 | âœ… |
| PRESSURE-TICK-002 | Wrong placement also decrements counters | `Assets/Scripts/Run/RunDirector.cs` | `PlaceNumber` â€” incorrect path after `Mistakes++` | âŒ |
| PRESSURE-TICK-003 | Success when last cell placed | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` â€” success check step 2 | âœ… |
| PRESSURE-TICK-004 | Chain success +1 HP | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` â€” `if (threat.IsChain)` | âœ… |
| PRESSURE-TICK-005 | Expiry 2 HP per unfilled cell | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` â€” expiry branch | âœ… |
| PRESSURE-TICK-006 | DoublePenalty doubles expiry | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` â€” `penaltyPer` calculation | âœ… |
| PRESSURE-TICK-007 | Wave counter decrement + reset + spawn | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` â€” step 4 wave block; `SpawnWaveThreat` | âœ… |
| PRESSURE-TICK-008 | Wave cap at 3 concurrent | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` â€” `WaveSpawnCount < 3` | âœ… |
| PRESSURE-TICK-009 | WaveSpawnCount decrements on removal | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureThreats` â€” success and expiry branches | âœ… |
| PRESSURE-TICK-010 | HauntedCell extra HP on mistake | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureMistake` â€” HauntedCell block | âœ… |
| PRESSURE-TICK-011 | HauntedCell DoublePenalty stacking | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureMistake` â€” `extraDmg` calc | âœ… |
| PRESSURE-TICK-012 | CrumblingRegion all-cell erosion | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureMistake` â€” CrumblingCells loop | âœ… |
| PRESSURE-TICK-013 | CrumblingRegion VeryHigh first-cell only | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureMistake` â€” needs `CrumblingVeryHigh` branch | âŒ |
| PRESSURE-TICK-014 | Fragility 0 â†’ AutoFill trigger | `Assets/Scripts/Run/RunDirector.cs` | `TickPressureMistake` â€” `if <= 0` | âœ… |
| PRESSURE-TICK-015 | AutoFill places solution, costs HP | `Assets/Scripts/Run/RunDirector.cs` | `AutoFillCrumbledCell` | âœ… |
| PRESSURE-TICK-016 | AutoFill silently resolves threats | `Assets/Scripts/Run/RunDirector.cs` | `AutoFillCrumbledCell` â€” threat loop | âœ… |
| PRESSURE-TICK-017 | AutoFill does not tick counters | `Assets/Scripts/Run/RunDirector.cs` | `AutoFillCrumbledCell` â€” no `TickPressureThreats` call | âœ… |
| PRESSURE-TICK-018 | AutoFill clears haunting | `Assets/Scripts/Run/RunDirector.cs` | `AutoFillCrumbledCell` â€” `HauntedCell = (-1,-1)` | âœ… |
| PRESSURE-TICK-019 | All HP via `ApplyMistakePenalty` | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty(int damage)` | âœ… |

### UI / Rendering

| ID | Description | File | Method / Location | Status |
|----|-------------|------|-------------------|--------|
| PRESSURE-UI-001 | No visuals on tutorial (RunNumber==0) | `Assets/Scripts/UI/InRunController.cs` | `BuildPressureRenderData` â€” RunNumber guard | âœ… |
| PRESSURE-UI-002 | Count badge per threatened cell | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` â€” `badgeText` / `cell.BadgeLabel` block | âœ… |
| PRESSURE-UI-003 | Badge colour by ratio (>50%/25â€“50%/<25%) | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` â€” badge colour calc; needs `InitialPlacements` | âš ï¸ |
| PRESSURE-UI-004 | Expiry red-flash + shake animation | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` â€” threat-removal detection + animation | âŒ |
| PRESSURE-UI-005 | Chain success gold-flash | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` â€” `ChainSuccessCells` colour override | âŒ |
| PRESSURE-UI-006 | Haunted cell purple pulse | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` â€” HauntedCell pulse block | âœ… |
| PRESSURE-UI-007 | Crumbling brown crack tint | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` â€” CrumblingCells tint block | âœ… |
| PRESSURE-UI-008 | Auto-fill green flash | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` â€” `CrumbledFlashCells` colour override | âŒ |
| PRESSURE-UI-009 | Wave ripple animation | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` â€” wave ripple overlay; needs `WaveRipplePending` | âŒ |
| PRESSURE-UI-010 | Region border strip (Row/Col/Box) | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` â€” new `DrawRegionBorderStrip` helper | âŒ |
| PRESSURE-UI-011 | Chain animated line | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` â€” new `DrawChainThreatLine` helper | âŒ |

### Persistence

| ID | Description | File | Method / Location | Status |
|----|-------------|------|-------------------|--------|
| PRESSURE-SAVE-001 | All pressure fields `[NonSerialized]` | `Assets/Scripts/Core/RuntimeModels.cs` | `LevelState` â€” all pressure fields | âœ… |
| PRESSURE-SAVE-002 | Resume reinitialises pressure from seed | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` called in `StartLevel` | âœ… |
| PRESSURE-SAVE-003 | Comment documents non-persistence | `Assets/Scripts/Save/RunAutoSaveCoordinator.cs` | Line 51 â€” NOTE comment | âœ… |

### Edge Case Guards

| ID | Description | File | Method / Location | Status |
|----|-------------|------|-------------------|--------|
| PRESSURE-EDGE-001 | Chain <3 cells â†’ SingleCell fallback | `Assets/Scripts/Run/RunDirector.cs` | `SelectThreatCells` â€” Chain case | âœ… |
| PRESSURE-EDGE-002 | CrumblingRegion no-qualifying-region guard | `Assets/Scripts/Run/RunDirector.cs` | `SelectCrumblingRegion` + `InitializePressureMechanics` | âœ… |
| PRESSURE-EDGE-003 | Wave spawn skips when no free cells | `Assets/Scripts/Run/RunDirector.cs` | `SpawnWaveThreat` â€” `available.Count == 0` | âœ… |
| PRESSURE-EDGE-004 | Haunted+Crumbling same cell | `Assets/Scripts/Run/RunDirector.cs` | `AutoFillCrumbledCell` â€” HauntedCell clear | âœ… |
| PRESSURE-EDGE-005 | counting_shadow + modifier coexist | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` â€” additive curse block | âœ… |
| PRESSURE-EDGE-006 | hollow_eye defers to modifier | `Assets/Scripts/Run/RunDirector.cs` | `InitializePressureMechanics` â€” `HauntedCell == (-1,-1)` guard | âœ… |

---

## Boss Debuffs

**Spec:** `docs/BossDebuffEffects.md` v1.3

### Debuff Definitions

| ID | Debuff | BossModifierId | Description | Status |
|----|--------|:---:|-------------|--------|
| DEBUFF-DEF-001 | RowWipe | 94 | On mistake: clears player-placed digits in same row | âœ… |
| DEBUFF-DEF-002 | ColWipe | 95 | On mistake: clears player-placed digits in same column | âœ… |
| DEBUFF-DEF-003 | BoxWipe | 99 | On mistake: clears player-placed digits in same region | âœ… |
| DEBUFF-DEF-004 | CrossWipe | 100 | On mistake: clears row AND column (combined) | âœ… |
| DEBUFF-DEF-005 | RadiusWipe | â€” | On mistake: clears within 2-cell Chebyshev radius | âŒ |
| DEBUFF-DEF-006 | PencilPoison | â€” | On mistake: adds wrong pencil marks to row | âŒ |
| DEBUFF-DEF-007 | PencilBlind | 98 | On mistake: removes all pencil marks in row+column | âœ… |
| DEBUFF-DEF-008 | PencilScramble | â€” | On mistake: scrambles all pencil marks board-wide | âŒ |
| DEBUFF-DEF-009 | FogPulse | â€” | On mistake: adds fog ring for 3 moves | âŒ |
| DEBUFF-DEF-010 | NumberBlind | â€” | On mistake: hides placed digit board-wide for 2 moves | âŒ |
| DEBUFF-DEF-011 | CellBlur | â€” | On mistake: hides digit labels in 3Ã—3 area briefly | âŒ |
| DEBUFF-DEF-012 | DoublePenalty | 96 | Wrong placements deal 2 HP instead of 1 | âœ… |
| DEBUFF-DEF-013 | PencilDrain | 101 | Each wrong placement costs 1 extra pencil charge | âœ… |
| DEBUFF-DEF-014 | GoldFine | 102 | Deducts 5 gold per mistake (clamped to 0) | âœ… |
| DEBUFF-DEF-015 | RevealCost | â€” | Blocks next item use for 2 moves after mistake | âŒ |
| DEBUFF-DEF-016 | DigitShift | â€” | After mistake: swap solution values between cells | âŒ |
| DEBUFF-DEF-017 | GivenReveal | â€” | After mistake: convert 1â€“3 correct digits to given | âŒ |
| DEBUFF-DEF-018 | CellLock | 97 | After mistake: wrong cell un-editable for 3 placements | âœ… |

### Integration Hooks

| ID | Hook | File | Method | Status |
|----|------|------|--------|--------|
| DEBUFF-HOOK-001 | `ApplyMistakePenalty(row,col)` overload dispatches debuffs | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty(int,int)` | âœ… |
| DEBUFF-HOOK-002 | DoublePenalty sets damage=2 | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty` | âœ… |
| DEBUFF-HOOK-003 | RowWipe â†’ `ClearBoardRow(row)` | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty` | âœ… |
| DEBUFF-HOOK-004 | ColWipe â†’ `ClearBoardCol(col)` | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty` | âœ… |
| DEBUFF-HOOK-005 | PencilBlind â†’ `ClearPencilRowCol(row,col)` | `Assets/Scripts/Run/RunDirector.cs` | `ApplyMistakePenalty` | âœ… |
| DEBUFF-HOOK-006 | CellLock â†’ `LockCell(row,col)` into `LockedCells` | `Assets/Scripts/Run/RunDirector.cs` | `LockCell` | âœ… |
| DEBUFF-HOOK-007 | `TickLockedCells()` after every correct placement | `Assets/Scripts/Run/RunDirector.cs` | `TickLockedCells` | âœ… |
| DEBUFF-HOOK-008 | `GetLockedCells()` for input blocking + board tint | `Assets/Scripts/Run/RunDirector.cs` | `GetLockedCells` | âœ… |
| DEBUFF-HOOK-009 | `LevelState.LockedCells` `[NonSerialized]` dict | `Assets/Scripts/Core/RuntimeModels.cs` | `LevelState` | âœ… |
| DEBUFF-HOOK-010 | `BossModifierId` enum 94â€“102 | `Assets/Scripts/Core/GameEnums.cs` | `BossModifierId` | âœ… |
| DEBUFF-HOOK-011 | `BossService` ModifierData IDs 94â€“102; IDs â‰¥94 excluded from pool | `Assets/Scripts/Boss/BossService.cs` | `ModifierData`, `BuildEligiblePool` | âœ… |
| DEBUFF-HOOK-012 | `BoardViewController` CellLocked tint (deep magenta) | `Assets/Scripts/UI/BoardViewController.cs` | `RenderBoard` | âœ… |
| DEBUFF-HOOK-013 | `InRunController` blocks input on locked cells | `Assets/Scripts/UI/InRunController.cs` | Input handlers | âœ… |

---

## Curse System

**Spec:** `docs/CurseSystem.md` v1.1

### Acquisition

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| CURSE-ACQUIRE-001 | Run events offer curses as tradeoffs (Ink Peddler, Tempting Shrine) | `Assets/Scripts/Run/RunEventService.cs` | `ResolveChoice` | âœ… |
| CURSE-ACQUIRE-002 | 4 curse-bearing items apply curse on use | `Assets/Scripts/Items/ItemService.cs` | `UseItem` | âœ… |
| CURSE-ACQUIRE-003 | Curse items labelled with curse cost in tooltip | `Assets/Scripts/Items/ItemService.cs` | Item catalog | âœ… |
| CURSE-ACQUIRE-004 | Rest node 4-way when curses active (adds Cleanse option) | `Assets/Scripts/Run/RunDirector.cs` | `AcceptRestCurseRemoval` | âœ… |
| CURSE-ACQUIRE-005 | Rest node 3-way when no curses active | `Assets/Scripts/Run/RunDirector.cs` | Rest handlers | âœ… |

### Curse Pool

| ID | Curse ID | Name | Severity | Status |
|----|----------|------|----------|--------|
| CURSE-POOL-001 | `hollow_pencil` | Hollow Pencil | Mild | âœ… |
| CURSE-POOL-002 | `fog_of_memory` | Fog of Memory | Mild | âœ… |
| CURSE-POOL-003 | `misfortune` | Misfortune | Mild | âœ… |
| CURSE-POOL-004 | `bad_luck` | Bad Luck | Mild | âœ… |
| CURSE-POOL-005 | `price_gouge` | Price Gouge | Mild | âœ… |
| CURSE-POOL-006 | `inkblot` | Inkblot | Medium | âœ… |
| CURSE-POOL-007 | `crumbling_focus` | Crumbling Focus | Medium | âœ… |
| CURSE-POOL-008 | `sealed_eyes` | Sealed Eyes | Medium | âœ… |
| CURSE-POOL-009 | `blurred_sight` | Blurred Sight | Medium | âœ… |
| CURSE-POOL-010 | `restless_inventory` | Restless Inventory | Medium | âœ… |
| CURSE-POOL-011 | `phantom_pain` | Phantom Pain | Nasty | âœ… |
| CURSE-POOL-012 | `weight_of_stone` | Weight of Stone | Nasty | âœ… |
| CURSE-POOL-013 | `trembling_hand` | Trembling Hand | Nasty | âœ… |
| CURSE-POOL-014 | `fraying_thread` | Fraying Thread | Nasty | âœ… |
| CURSE-POOL-015 | `double_or_nothing` | Double or Nothing | Nasty | âœ… |

### Stacking Rules

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| CURSE-STACK-001 | Unlimited curse stacking (no cap) | `Assets/Scripts/Run/CurseService.cs` | `ApplyCurse` | âœ… |
| CURSE-STACK-002 | No duplicates â€” re-roll until different | `Assets/Scripts/Run/CurseService.cs` | `RollCurse` | âœ… |
| CURSE-STACK-003 | Curses persist across all 5 floors | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState.ActiveCurseIds` | âœ… |

### Removal

| ID | Method | File | Status |
|----|--------|------|--------|
| CURSE-REMOVE-001 | Rest node "Cleanse a Curse" removes oldest | `Assets/Scripts/Run/RunDirector.cs` | âœ… |
| CURSE-REMOVE-002 | Error-free puzzle: 25% chance to auto-clear oldest | `Assets/Scripts/Run/CurseService.cs` | âœ… |
| CURSE-REMOVE-005 | GardenMonk increases auto-clear to 50% | `Assets/Scripts/Run/CurseService.cs` | âœ… |

### Integration Points

| ID | Location | File | Status |
|----|----------|------|--------|
| CURSE-INT-001 | `RunEventService.ResolveChoice()` calls `ApplyCurse` | `Assets/Scripts/Run/RunEventService.cs` | âœ… |
| CURSE-INT-002 | `RunDirector.PlaceNumber()`: phantom_pain 2Ã—HP; trembling_hand 1HP first placement | `Assets/Scripts/Run/RunDirector.cs` | âœ… |
| CURSE-INT-003 | `CompleteLevelAndGrantRewards()`: misfortune slotâˆ’1; restless_inventory drain | `Assets/Scripts/Run/RunDirector.cs` | âœ… |
| CURSE-INT-004 | `AdvanceToNextFloor()`: fraying_thread âˆ’1 MaxHP | `Assets/Scripts/Run/RunDirector.cs` | âœ… |
| CURSE-INT-005 | `ShopService.BuildOffers()`: price_gouge Ã—1.35 | `Assets/Scripts/Economy/RelicService.cs` | âœ… |
| CURSE-INT-006 | Board setup: inkblot marks 3 cells as fogged | `Assets/Scripts/Run/RunDirector.cs` | âœ… |
| CURSE-INT-007 | `ItemService.UseItem()`: 4 curse-bearing items call `ApplyCurse` | `Assets/Scripts/Items/ItemService.cs` | âœ… |
| CURSE-INT-008 | `BuildItemRewardSlots()`: bad_luck +15% Nothing; double_or_nothing 20% re-roll | `Assets/Scripts/UI/InRunController.cs` | âœ… |
| CURSE-INT-009 | blurred_sight: coroutine fades pencilmarks after 1.5s | `Assets/Scripts/UI/InRunController.cs` | âœ… |
| CURSE-INT-010 | Rest cleanse wired via `AcceptRestCurseRemoval()` | `Assets/Scripts/Run/RunDirector.cs` | âœ… |
| CURSE-INT-011 | `CursePanelController` renders active curses, expandable | `Assets/Scripts/UI/InRunController.cs` | âœ… |
| CURSE-INT-012 | `ActiveCurseIds` serialized via `RunState` (no extra change) | `Assets/Scripts/Core/RuntimeModels.cs` | âœ… |

---

## Meta Progression

**Spec:** `docs/MetaProgressionSystem.md` v1.3

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| META-UNLOCK-001 | 8 classes with cumulative cross-class unlock conditions | `Assets/Scripts/Core/RuntimeModels.cs` | `ClassUnlockProgress` | âœ… |
| META-UNLOCK-002 | `RecordRunAndGetNewUnlocks()` updates counters + `EvaluateUnlocks()` | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | âœ… |
| META-UNLOCK-003 | Unlocked classes: full stat block + passive + level | `Assets/Scripts/UI/InRunController.cs` | Class select panel | âœ… |
| META-UNLOCK-004 | Locked classes: greyed out + unlock condition text | `Assets/Scripts/UI/InRunController.cs` | Class select panel | âœ… |
| META-XP-001 | Per-class `TotalXp` stored; level derived via `DeriveLevel()` | `Assets/Scripts/Core/SaveModels.cs` | `ClassGardenProgressEntry` | âœ… |
| META-XP-002 | Two-phase XP curve L1â€“15 + L16â€“40 | `Assets/Scripts/Run/RunDirector.cs` | `XpService.DeriveLevel` | âœ… |
| META-XP-003 | Total to L40: 16,860 XP | `Assets/Scripts/Run/RunDirector.cs` | `XpService` constants | âœ… |
| META-XP-004 | XP committed at run end (no mid-run leveling) | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | âœ… |
| META-PRESTIGE-001 | Max prestige tier 9 per class | `Assets/Scripts/Core/SaveModels.cs` | `ClassGardenProgressEntry.PrestigeTier` | âœ… |
| META-PRESTIGE-002 | Prestige requires Level 40 | `Assets/Scripts/Save/ProfileService.cs` | Prestige gate check | âœ… |
| META-PRESTIGE-003 | Boss gate: `3 Ã— (PrestigeTier + 1)` boss defeats | `Assets/Scripts/Save/ProfileService.cs` | Prestige gate check | âœ… |
| META-PRESTIGE-004 | On prestige: TotalXp=0, PrestigeTier++ | `Assets/Scripts/Save/ProfileService.cs` | `TryPrestige` | âœ… |
| META-ASCEND-001 | Ascension is a global layer above per-class prestige | `Assets/Scripts/Core/SaveModels.cs` | `MetaProgressionState.AscensionLevel` | âœ… |
| META-ASCEND-002 | `ApplyAscension`: increments AscensionLevel, +1 MaxStarCap, unlocks seasonal | `Assets/Scripts/Meta/AscensionService.cs` | `ApplyAscension` | âœ… |
| META-ASCEND-003 | `TryPrestigeReset`: resets AscensionLevel=0, MaxStarCap=5 | `Assets/Scripts/Meta/AscensionService.cs` | `TryPrestigeReset` | âœ… |
| META-ASCEND-004 | Monthly seed: `(year Ã— 100) + month` | `Assets/Scripts/Meta/AscensionService.cs` | `BuildMonthlySeed` | âœ… |
| META-COMPLETE-001 | `CompletionService.Recalculate()` â€” 4 checks Ã— 25% | `Assets/Scripts/Meta/CompletionService.cs` | `Recalculate` | âœ… |
| META-CODEX-001 | Item & Relic Codex tracks discovery | `Assets/Scripts/Save/ProfileService.cs` | `RecordItemDiscovery`, `RecordRelicDiscovery` | âœ… |
| META-CODEX-002 | Entry marked Discovered on first acquisition only | `Assets/Scripts/Save/ProfileService.cs` | `RecordItemDiscovery` | âœ… |

---

## Endless Zen Mode

**Spec:** `docs/EndlessZenMode.md` v1.2

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| ZENMODE-UNLOCK-001 | Unlocked when `TotalRuns â‰¥ 10` via `RecordRunAndGetNewUnlocks` | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | âœ… |
| ZENMODE-SESSION-001 | Session: 9Ã—9 board, no timer, no XP, no items/relics, HP=0 ends session | `Assets/Scripts/Run/RunDirector.cs` | Endless mode setup | âœ… |
| ZENMODE-SESSION-002 | HP uses class base HP (class passives still apply) | `Assets/Scripts/Run/RunDirector.cs` | Endless mode setup | âœ… |
| ZENMODE-DEPTH-001 | Depth starts at 0, increments after each solved puzzle | `Assets/Scripts/Run/RunDirector.cs` | `EndlessZenService.BuildLevel` | âœ… |
| ZENMODE-DEPTH-002 | Stars formula: `Clamp(1 + floor(depth/4), 1, 5)` | `Assets/Scripts/Run/RunDirector.cs` | `EndlessZenService.BuildLevel` | âœ… |
| ZENMODE-DEPTH-003 | Stars capped at 5â˜… in Endless Zen | `Assets/Scripts/Run/RunDirector.cs` | `EndlessZenService.BuildLevel` | âœ… |
| ZENMODE-DEPTH-004 | Modifier cap formula: <10=1, <20=2, 20+=3 | `Assets/Scripts/Run/RunDirector.cs` | `EndlessZenService.BuildLevel` | âœ… |
| ZENMODE-RES-001 | Between levels: +1 HP, +3 Pencil restored | `Assets/Scripts/Run/RunDirector.cs` | Level completion | âœ… |
| ZENMODE-SCORE-001 | `ProfileStats.HighestEndlessDepth` tracks best depth | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | âœ… |

---

## Spirit Trials Mode

**Spec:** `docs/SpiritTrialsMode.md` v1.2

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| TRIAL-UNLOCK-001 | Unlocked via full 5-floor Garden Run OR Ascension Level 1 | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | âœ… |
| TRIAL-SESSION-001 | Session: 9Ã—9, timer, no XP, no gold, 1 starting item | `Assets/Scripts/Run/RunDirector.cs` | Spirit Trials setup | âœ… |
| TRIAL-SESSION-002 | Timer starts on first cell interaction | `Assets/Scripts/UI/InRunController.cs` | Input handler | âœ… |
| TRIAL-TIER-001 | 4 tiers: Apprentice/Adept/Master/Grandmaster (star + modifier counts) | `Assets/Scripts/Run/RunDirector.cs` | `SpiritTrialsService` | âœ… |
| TRIAL-TIER-002 | Modifiers assigned randomly (seeded) â€” player cannot choose | `Assets/Scripts/Run/RunDirector.cs` | `SpiritTrialsService` | âœ… |
| TRIAL-SCORE-001 | BasePoints = `CellsToFill Ã— PointsPerCell` (per tier) | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | âœ… |
| TRIAL-SCORE-002 | SpeedMultiplier = `max(0.5, 2.0 - (ElapsedSeconds / ParTime))` | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | âœ… |
| TRIAL-SCORE-003 | Modifier constraint bonus: 0=+0, 1=+100, 2=+300 | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | âœ… |
| TRIAL-SCORE-004 | Mistake penalty per tier; HP=0 ends session | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | âœ… |
| TRIAL-SCORE-005 | FinalScore = `floor((Base Ã— Speed) + Constraint + Pencil âˆ’ Mistake)` | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | âœ… |
| TRIAL-SCORE-006 | Minimum score 0 (never negative) | `Assets/Scripts/Run/RunDirector.cs` | Score calculation | âœ… |
| TRIAL-LEAD-001 | Per-tier personal bests in `ProfileStats` | `Assets/Scripts/Save/ProfileService.cs` | `ProfileStats` | âœ… |
| TRIAL-LEAD-002 | Monthly leaderboard when `SeasonalChallengeUnlocked` | `Assets/Scripts/Meta/AscensionService.cs` | `BuildMonthlySeed` | âœ… |

---

## Sudoku Basics Tutorial

**Spec:** `docs/FirstTimePlayTutorial.md` v1.1

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| TUTO-BASICS-TRIGGER-001 | Triggers when `"sudoku_basics"` not in `CompletedKeys` | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `Start()` | âœ… |
| TUTO-BASICS-TRIGGER-002 | Only triggers once (check skipped on subsequent launches) | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `Start()` | âœ… |
| TUTO-BASICS-TRIGGER-003 | Prompt appears after main menu loads (not before) | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `ShowSudokuBasicsPrompt` | âœ… |
| TUTO-BASICS-BOARD-001 | Fixed 6x6 board, 2x3 boxes, deterministic givens | `Assets/Scripts/Tutorial/TutorialModeService.cs` | `BuildSudokuBasicsLevel` / `BuildBasicsTutorialBoard` | ✅ |
| TUTO-BASICS-BOARD-002 | Mistakes during tutorial not penalised | `Assets/Scripts/Run/RunDirector.cs` | Tutorial RunState | âœ… |
| TUTO-BASICS-STEP-001 | 7 named steps with highlighted overlays + text | `Assets/Scripts/UI/InRunController.cs` | `SudokuBasicsTutorialController` | âœ… |
| TUTO-BASICS-STEP-002 | Player advances via "Next" button or by performing action | `Assets/Scripts/UI/InRunController.cs` | `SudokuBasicsTutorialController` | âœ… |
| TUTO-BASICS-STEP-006 | Step 6 requires player to place digit 2 in target cell | `Assets/Scripts/UI/InRunController.cs` | `SudokuBasicsTutorialController` | âœ… |
| TUTO-BASICS-DONE-001 | Completion writes `"sudoku_basics"` to `CompletedKeys` + saves | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `MarkSudokuBasicsComplete` | âœ… |
| TUTO-BASICS-DONE-002 | Replayable via Options â†’ "Replay Sudoku Basics" | `Assets/Scripts/UI/InRunController.cs` | `MainMenuController` options | âœ… |

---

## Sudoku Generation Pipeline

**Spec:** `docs/SudokuGenerationPipeline.md` v1.5

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| GEN-REGION-001 | `BuildRegionMap(size, variant)` â€” 4 variants (0=Standard, 1=Alt, 2=Irregular A, 3=Jigsaw) | `Assets/Scripts/Core/SudokuGenerator.cs` | `BuildRegionMap` | âœ… |
| GEN-REGION-002 | All templates: each region = `size` cells, contiguous, count = board size | `Assets/Scripts/Core/SudokuGenerator.cs` | Template methods | âœ… |
| GEN-REGION-003 | `AllowIrregularPuzzles=false` restricts to variants 0â€“1 | `Assets/Scripts/Run/RunDirector.cs` | `BuildLevelConfig` | âœ… |
| GEN-SOLVE-001 | `GenerateSolvedBoard(size, regionMap, rng)` â€” backtracking + MRV heuristic + seeded shuffle | `Assets/Scripts/Core/SudokuGenerator.cs` | `GenerateSolvedBoard` / `FillBoard` | âœ… |
| GEN-REMOVE-001 | Cell removal to create player-facing puzzle | `Assets/Scripts/Core/SudokuGenerator.cs` | `RemoveCells` | âœ… |
| GEN-REMOVE-002 | `StarDensityService.MissingPercentForStars`: `(stars+3)Ã—0.1`, clamped [0.01, 0.95] | `Assets/Scripts/Core/StarDensityService.cs` | `MissingPercentForStars` | âœ… |
| GEN-REMOVE-003 | 7â˜… removes all cells; requires â‰¥1 modifier; tutorial-only | `Assets/Scripts/Core/StarDensityService.cs` | 7â˜… case | âœ… |
| GEN-REMOVE-004 | 7â˜… clamp bypassed (`removeCount = totalCells`) | `Assets/Scripts/Core/SudokuGenerator.cs` | `RemoveCells` | âœ… |
| GEN-MOD-001 | `ModifierGeometryGenerator.Generate(board, modifiers, seed, intensity)` | `Assets/Scripts/Core/ModifierGeometryGenerator.cs` | `Generate` | âœ… |
| GEN-VALID-001 | Placement valid if satisfies all constraints (not compared to solution) | `Assets/Scripts/Core/SudokuBoard.cs` | `IsCorrectAt`, `IsComplete` | âœ… |
| GEN-VALID-002 | Validation: `PlaceNumber` â†’ `IsMoveValid` â†’ `SudokuConstraintEngine.ValidateAll` | `Assets/Scripts/Run/RunDirector.cs` | `PlaceNumber` | âœ… |

---

## Path System & Garden Overview

**Spec:** `docs/PathSystem_GardenOverview.md` v1.5

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| MAP-STRUCT-001 | 5 floors per run; each floor is a garden with distinct theme | `Assets/Scripts/Run/RunDirector.cs` | Floor management | âœ… |
| MAP-STRUCT-002 | Each floor: Shared Start â†’ Calm/Risk Route â†’ Shared Boss Gate | `Assets/Scripts/Run/RunGraphService.cs` | Graph generation | âœ… |
| MAP-ROUTE-001 | Calm Route: longer, lower difficulty, more safe tiles, upper half | `Assets/Scripts/Run/RunGraphService.cs` | `BuildCalmRoute` | âœ… |
| MAP-ROUTE-002 | Risk Route: shorter, higher difficulty, fewer safe tiles, lower half | `Assets/Scripts/Run/RunGraphService.cs` | `BuildRiskRoute` | âœ… |
| MAP-CROSS-001 | Exactly one cross-connection per floor (lane switch opportunity) | `Assets/Scripts/Run/RunGraphService.cs` | Cross-link logic | âœ… |
| MAP-NODE-CURSED-001 | Cursed node shows Accept/Decline panel before puzzle | `Assets/Scripts/Run/RunDirector.cs` | Cursed node handling | âœ… |
| MAP-NODE-CURSED-002 | Accept: IsCursed=true, Ã—1.5 gold+XP, +1 random modifier | `Assets/Scripts/Run/RunDirector.cs` | `AcceptCursedNode` | âœ… |
| MAP-NODE-CURSED-003 | Decline: standard puzzle, no multipliers | `Assets/Scripts/Run/RunDirector.cs` | `DeclineCursedNode` | âœ… |
| MAP-NODE-CURSED-004 | `CursedPuzzlesAccepted` tracked for run score breakdown | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState` | âœ… |
| MAP-FX-001 | One random positive floor effect per floor | `Assets/Scripts/Run/RunDirector.cs` | `RollPositiveFloorEffect` | âœ… |
| MAP-FX-002 | Floor effect seeded from `RunState.Seed + floorIndex` | `Assets/Scripts/Run/RunDirector.cs` | `RollPositiveFloorEffect` | âœ… |
| MAP-FX-003 | Stored in `RunState.HasPositiveFloorEffect + ActivePositiveFloorEffect` | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState` | âœ… |
| MAP-FX-004 | Shown on path screen as banner via `GetPositiveFloorEffectLabel()` | `Assets/Scripts/Run/RunMapController.cs` | `GetPositiveFloorEffectLabel` | âœ… |
| MAP-FX-005 | None effect has equal probability (20% each) | `Assets/Scripts/Run/RunDirector.cs` | `RollPositiveFloorEffect` | âœ… |

---

## Boss Mechanics

**Spec:** `docs/BossMechanicsSystem.md` â€” partial IDs (pending full spec update)

Key requirements (see spec for full list):

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| BOSS-MOD-001 | 15 boss modifiers (IDs 0â€“14) in `BossModifierId` enum | `Assets/Scripts/Core/GameEnums.cs` | `BossModifierId` | âœ… |
| BOSS-MOD-002 | `BossService.ModifierData` â€” tier/impact/name/description per modifier | `Assets/Scripts/Boss/BossService.cs` | `ModifierData` | âœ… |
| BOSS-MOD-003 | `ModifierGeometryGenerator.Generate` produces overlay for each modifier | `Assets/Scripts/Core/ModifierGeometryGenerator.cs` | `Generate` | âœ… |
| BOSS-INT-001 | `BossModifierIntensity` enum (Low/Medium/High/VeryHigh) scales geometry | `Assets/Scripts/Core/GameEnums.cs` | `BossModifierIntensity` | âœ… |
| BOSS-INT-002 | Intensity set by `RunNumber` in `BuildLevelConfig` (1-2=Low, 3-5=Med, 6-8=High, 9+=VH) | `Assets/Scripts/Run/RunDirector.cs` | `BuildLevelConfig` | âœ… |
| BOSS-FLOOR-001 | Floor modifier system: floors 2â€“5 add persistent modifiers to all puzzles | `Assets/Scripts/Run/RunDirector.cs` | `RollFloorModifiers` | âœ… |
| BOSS-FLOOR-002 | Boss choice pool excludes active floor modifiers | `Assets/Scripts/Boss/BossService.cs` | `BuildEligiblePool` | âœ… |
| BOSS-FLOOR-003 | `RunState.FloorModifiers` stored for persistence | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState.FloorModifiers` | âœ… |

---

## Class XP Progression

**Spec:** `docs/ClassXpProgressionSystem.md` â€” partial IDs (pending full spec update)

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| XP-TILE-001 | `XpService.CalculateTile(boardSize, stars, modCount, isBoss, perfect)` â†’ TileXpEntry | `Assets/Scripts/Run/RunDirector.cs` | `XpService.CalculateTile` | âœ… |
| XP-TILE-002 | Base XP per board size: 5Ã—5=30, 6Ã—6=45, 7Ã—7=60, 8Ã—8=90, 9Ã—9=120 | `Assets/Scripts/Run/RunDirector.cs` | `XpService` | âœ… |
| XP-TILE-003 | Star multiplier: 1â˜…=Ã—1.0, 2â˜…=Ã—1.2, 3-4â˜…=Ã—1.5, 5-6â˜…=Ã—2.0 | `Assets/Scripts/Run/RunDirector.cs` | `XpService` | âœ… |
| XP-TILE-004 | Boss Ã—1.5; Mod bonus +15 per active mod (boss only); Perfect +25 flat | `Assets/Scripts/Run/RunDirector.cs` | `XpService` | âœ… |
| XP-CURVE-001 | L1-15: `50+(n-1)Ã—10`; L16-40: `190+(n-15)Ã—35` | `Assets/Scripts/Run/RunDirector.cs` | `XpService.DeriveLevel` | âœ… |
| XP-CURVE-002 | `DeriveLevel(totalXp)` derives level at runtime from cumulative XP | `Assets/Scripts/Run/RunDirector.cs` | `XpService.DeriveLevel` | âœ… |
| XP-LOG-001 | `RunDirector._tileXpLog` tracks per-tile XP for end-of-run breakdown | `Assets/Scripts/Run/RunDirector.cs` | `_tileXpLog` | âœ… |
| XP-COMMIT-001 | XP committed to class at run end in `RecordRunAndGetNewUnlocks` | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` | âœ… |

---

## Items & Relics

**Spec:** `docs/ItemsAndRelicsSystem.md` â€” partial IDs (pending full spec update)

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| ITEM-SLOT-001 | Item reward slots by star: 1â˜…=2, 2â˜…=3, 3â˜…=3, 4â˜…=4, 5â˜…=5 | `Assets/Scripts/Items/ItemService.cs` | `RollRewardSlots` | âœ… |
| ITEM-SLOT-002 | Nothing slot probability: 1â˜…=25%, 2â˜…=22%, 3â˜…=18%, 4â˜…=15%, 5â˜…=12% | `Assets/Scripts/Items/ItemService.cs` | `RollRewardSlots` | âœ… |
| ITEM-EPIC-001 | Epic items only available at class Level 15+ | `Assets/Scripts/Items/ItemService.cs` | `BuildItemPool` | âœ… |
| RELIC-SLOT-001 | Single relic slot: `RunState.HasRelic + HeldRelic` | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState` | âœ… |
| RELIC-ROLL-001 | Relic tier weights: floor-based 5Ã—5 table; risk route shifts weights up | `Assets/Scripts/Economy/RelicService.cs` | `RollRelic` | âœ… |
| SHOP-PRICE-001 | Shop price: `BasePrice Ã— (1 + FloorIndex Ã— 0.5) Ã— priceMultiplier` | `Assets/Scripts/Economy/RelicService.cs` | `ShopService.BuildOffers` | âœ… |

---

## Save / Load

**Spec:** `docs/SaveLoadArchitecture.md` â€” partial IDs (existing REQ-SAVE-018..039 stubs pending update)

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| SAVE-DUAL-001 | Dual-file architecture: profile save + run save | `Assets/Scripts/Save/SaveFileService.cs` | File management | âœ… |
| SAVE-SLOT-001 | 3 profile slots: `save_profile_0/1/2.json` | `Assets/Scripts/Save/SaveFileService.cs` | Constructor | âœ… |
| SAVE-SLOT-002 | `SaveProfileService` manages all 3 slots; `ActiveSlot` in `PlayerPrefs` | `Assets/Scripts/Save/ProfileService.cs` | `SaveProfileService` | âœ… |
| SAVE-SLOT-003 | `GameBootstrap.ApplySlotChange()` re-wires save services after slot switch | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `ApplySlotChange` | âœ… |
| SAVE-AUTO-001 | `RunAutoSaveCoordinator.Save()` â€” atomic write before serializing | `Assets/Scripts/Save/RunAutoSaveCoordinator.cs` | `Save` | âœ… |
| SAVE-RESUME-001 | `RunResumeService.TryResumeFromSave()` restores full run state including boss modifier | `Assets/Scripts/Save/RunResumeService.cs` | `TryResumeFromSave` | âœ… |
| SAVE-SERIAL-001 | All save data uses `JsonUtility` (`[Serializable]` on all data classes) | `Assets/Scripts/Core/SaveModels.cs` | Data models | âœ… |
| SAVE-SERIAL-002 | Nullable types use bool+value pattern (`HasChosenBossModifier + ChosenBossModifierId`) | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState` | âœ… |
| SAVE-SERIAL-003 | `HashSet<T>` not serialized â€” use `List<T>` + sync methods | `Assets/Scripts/Core/RuntimeModels.cs` | `SeenBossModifierList` | âœ… |
| SAVE-TUTO-001 | Tutorial saves excluded: `RunAutoSaveCoordinator.Save` skips `TutorialMode` runs | `Assets/Scripts/Save/RunAutoSaveCoordinator.cs` | `Save` | âœ… |

---

## Input & Profiles

**Spec:** `docs/InputAndProfilesSpec.md` â€” partial IDs (pending full spec update)

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| INPUT-REMAP-001 | `InputRemapService` â€” keyboard defaults + gamepad button mappings | `Assets/Scripts/Core/InputRemapService.cs` | Constructor | âœ… |
| INPUT-REMAP-002 | `WasActionPressed(action)` â€” checks keyboard then gamepad then axis crossing | `Assets/Scripts/Core/InputRemapService.cs` | `WasActionPressed` | âœ… |
| INPUT-REMAP-003 | `GetAnyKeyDown()` â€” returns first pressed key (keyboard or joystick) | `Assets/Scripts/Core/InputRemapService.cs` | `GetAnyKeyDown` | âœ… |
| INPUT-REMAP-004 | `KeybindingsPanelController` manages listen/capture/save rebind flow | `Assets/Scripts/Core/InputRemapService.cs` | `KeybindingsPanelController` | âœ… |
| INPUT-PROFILE-001 | `MenuScreen.ProfileSelect` â€” shown on first launch if no files exist | `Assets/Scripts/Bootstrap/GameBootstrap.cs` | `Start` | âœ… |
| INPUT-PROFILE-002 | `SelectProfileSlot(i)`, `DeleteProfileSlot(i)`, `RefreshProfileSelectCards()` | `Assets/Scripts/UI/InRunController.cs` | `MainMenuController` | âœ… |

---

## Accessibility

**Spec:** `docs/AccessibilitySpec.md` â€” partial IDs (existing REQ-UI-018..026 stubs pending update)

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| ACCESS-COLOR-001 | `AccessibilityService` â€” colorblind mode, high contrast, reduce motion, alt symbols | `Assets/Scripts/Core/AccessibilityService.cs` | Service | âœ… |
| ACCESS-COLOR-002 | Accessibility toggles call `NotifyAccessibilityChanged` â†’ rebuild overlays | `Assets/Scripts/UI/InRunController.cs` | `OptionsController` | âœ… |
| ACCESS-MOTION-001 | Screen shake disabled when `ReduceMotion` on | `Assets/Scripts/UI/InRunController.cs` | Shake helper | âœ… |
| ACCESS-OVERLAY-001 | High contrast: overlay spine alpha Ã—1.4, line width Ã—1.5 | `Assets/Scripts/UI/BoardViewController.cs` | Overlay render | âœ… |
| ACCESS-PARTICLE-001 | All particle effects disabled when `ReduceMotion` active | `Assets/Scripts/UI/InRunController.cs` | `AmbientParticleController` | âœ… |

---

## Gold Economy

**Spec:** `docs/GoldEconomySystem.md` â€” partial IDs (pending full spec update)

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| ECON-REWARD-001 | Gold awarded on puzzle completion via gold formula | `Assets/Scripts/Run/RunDirector.cs` | `CompleteLevelAndGrantRewards` | âœ… |
| ECON-SHOP-001 | Shop: 3 items + optional relic per visit | `Assets/Scripts/Economy/RelicService.cs` | `ShopService.BuildOffers` | âœ… |
| ECON-SHOP-002 | Price scaling: `BasePrice Ã— (1 + FloorIndex Ã— 0.5) Ã— priceMultiplier` | `Assets/Scripts/Economy/RelicService.cs` | `ShopService.BuildOffers` | âœ… |

---

## Seasonal Challenge

**Spec:** `docs/SeasonalChallengeSpec.md` â€” **Status: ðŸ†• Not yet implemented**

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| SEASON-UNLOCK-001 | Unlocked after first ascension (`SeasonalChallengeUnlocked = true`) | `Assets/Scripts/Meta/AscensionService.cs` | `ApplyAscension` | âœ… seed logic |
| SEASON-SEED-001 | Monthly seed: `(year Ã— 100) + month` | `Assets/Scripts/Meta/AscensionService.cs` | `BuildMonthlySeed` | âœ… |
| SEASON-PUZZLE-001 | Fixed 9Ã—9 4â˜… puzzle from monthly seed | â€” | â€” | ðŸ†• |
| SEASON-SCORE-001 | Personal best stored locally | â€” | `SeasonalChallengeState` | ðŸ†• |
| SEASON-UI-001 | Prominent main menu panel with countdown | â€” | â€” | ðŸ†• |
| SEASON-UI-002 | `GameMode.SeasonalChallenge` enum value | `Assets/Scripts/Core/GameEnums.cs` | `GameMode` | ðŸ†• |

---

## Daily Goal System

**Spec:** `docs/DailyGoalSystem.md` â€” **Status: ðŸ†• Not yet implemented**

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| DAILY-GOAL-001 | 3 daily goals per day (date-seeded, no network) | â€” | â€” | ðŸ†• |
| DAILY-GOAL-002 | Ink Stamps cosmetic currency from goal completion | â€” | â€” | ðŸ†• |
| DAILY-GOAL-003 | Streak tracking (consecutive days) | â€” | `DailyGoalState` | ðŸ†• |
| DAILY-GOAL-004 | Daily Walk widget on main menu | â€” | â€” | ðŸ†• |
| DAILY-GOAL-005 | `DailyGoalState` in `ProfileSaveData` | `Assets/Scripts/Core/SaveModels.cs` | `ProfileSaveData` | ðŸ†• |

---

## Class Progression Unlocks

**Spec:** `docs/ClassProgressionUnlocks.md` â€” **Status: ðŸ†• Not yet implemented**

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| CLASS-EXCL-001 | 16 class-exclusive unlocks: 8 items (L15) + 8 relics (L30) per class | â€” | â€” | ðŸ†• |
| CLASS-EXCL-002 | `ClassExclusive` flag on item/relic definitions | â€” | Item/relic catalog | ðŸ†• |
| CLASS-EXCL-003 | `ClassUnlockLevel` (15 or 30) on item/relic definitions | â€” | Item/relic catalog | ðŸ†• |
| CLASS-EXCL-004 | Exclusive items/relics only rollable for their associated class | â€” | `ItemService`, `RelicService` | ðŸ†• |

---

## Multi-Relic Save Compatibility

**Spec:** `docs/ItemsAndRelicsSystem.md` â€” section **Single Relic Slot** | **Status: âœ… Implemented (introduced by linter change)**

> These IDs formalise code that exists in `RuntimeModels.cs` and `RunResumeService.cs` but was never spec'd.  
> `RELIC-SLOT-MULTI` (the orphaned annotation) is now resolved as `RELIC-SLOT-002`.

| ID | Description | File | Method | Status |
|----|-------------|------|--------|--------|
| RELIC-SLOT-002 | `RunState.HeldRelics: List<RelicInstance>` â€” multi-relic list field (future-proof; currently max 1 entry enforced by game logic) | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState.HeldRelics` | âœ… |
| RELIC-SLOT-003 | Legacy migration on resume: if `HasRelic && HeldRelic != null && HeldRelics empty` â†’ migrate single relic into list | `Assets/Scripts/Save/RunResumeService.cs` | `RestoreTransientState` | âœ… |

