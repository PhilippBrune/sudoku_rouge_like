# Pressure Mechanics System

**Version:** 0.7 | **Date:** 2026-04-15 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 0.7 | 2026-04-15 | implemented | PRESSURE-UI-004 implemented: badge shake on expiry (`BoardViewController.ShakeBadgeCells`). All requirements now ✅. |
| 0.6 | 2026-04-15 | review | Audited implementation against codebase. Corrected 8 requirement statuses from ❌/⚠️ to ✅. One remaining gap: PRESSURE-UI-004 badge shake (flash exists, shake missing). |
| 0.5 | 2026-04-14 | review | Added unique requirement IDs (PRESSURE-ROLL/INIT/TICK/UI/SAVE/EDGE). Marked implementation status per requirement. AI-readable: every ID references the exact file + method that implements or must implement it. Identified 9 open gaps. |
| 0.4 | 2026-04-12 | under review | Resolved all open questions: wrong placements decrement Countdown Fill counter; Haunted Cell stacks with DoublePenalty (no cap); Pressure Wave never pauses; CrumblingRegion VeryHigh = fragility 2, first cell only. |
| 0.3 | 2026-04-12 | under review | Mechanic 1 granularity table now shows floor number, appearance chance, and counter value in one place for each scope. |
| 0.2 | 2026-04-12 | under review | Added global appearance rules (floor gates, puzzle types, intensity tiers). Merged Chain Ignition into Countdown Fill as "Chain" scope. Fixed Crumbling Region: crumble-lock now auto-fills correct value instead of blocking. Renumbered Pressure Wave to Mechanic 4. |
| 0.1 | 2026-04-12 | new | Initial draft — 1 primary mechanic (Countdown Fill) + 4 candidate suggestions for review |

---

## How to Use This Spec (AI Instructions)

This file is the **design source of truth** for pressure mechanics. Every requirement has:
- A unique stable ID (format `PRESSURE-CATEGORY-NNN`)
- An implementation status: ✅ Implemented | ⚠️ Partial | ❌ Not Implemented
- A reference to the primary file + method that owns it

When implementing a gap requirement:
1. Find the ID in `REQUIREMENT_MAP.md` for the exact file and method location.
2. Search the codebase for the `// [REQ: ID]` comment nearest to the insertion point.
3. Follow the design detail in this file — do not invent behaviour.
4. After implementing, update status in this file and in `REQUIREMENT_MAP.md`.

Do NOT change requirement IDs. IDs are stable identifiers — renaming one breaks traceability.

---

## Overview

Pressure mechanics are **in-puzzle urgency effects** that impose a time-limit measured in *placements* rather than real time. This keeps them fair and skill-based: the player is penalised for poor prioritisation, not poor reflexes.

All pressure mechanics share these design axioms:
- The counter is **placement-based** (every digit placed on the board — correct or incorrect — decrements the counter by 1).
- Penalties are expressed in **HP loss**, consistent with the existing mistake-penalty system.
- Pressure effects are **visible at all times** — no hidden information.
- Pressure effects deliver as **boss modifiers** (`BossModifierId` 103–106), routed through existing systems.

---

## Appearance Rules (all mechanics)

### Puzzle Types

Pressure mechanics can appear in **three puzzle types** only:

| Puzzle Type | `LevelConfig` flag | Notes |
|-------------|-------------------|-------|
| **Elite** | `IsElite = true` | Regular combat puzzle on an elite node |
| **Pre-boss** | `IsPreBoss = true` | The puzzle immediately before the floor boss |
| **Boss** | `IsBoss = true` | The boss fight puzzle itself |

Floor 1 puzzles and tutorial mode are never affected.

### Floor Probability & Intensity

Each mechanic rolls its appearance chance inside `RunDirector.BuildLevelConfig` via `RollPressureMechanic`.

| Floor | Appearance chance | `BossModifierIntensity` |
|:-----:|:-----------------:|:---------:|
| 1 | Never | — |
| 2 | 12% | Low |
| 3 | 30% | Medium |
| 4 | 55% | High |
| 5+ | 80% | Very High |

The **intensity** governs per-mechanic severity (counter lengths, fragility values, wave intervals). See each mechanic's Intensity Parameters table.

---

## Rolling & Activation Requirements

**[PRESSURE-ROLL-001]** ✅ Implemented
No pressure mechanic may activate on floor 1 (`floor < 2`) or when `RunState.TutorialMode == true`.
> `RunDirector.RollPressureMechanic` — first two guard lines.

**[PRESSURE-ROLL-002]** ✅ Implemented
A pressure mechanic may only roll when the puzzle node is elite, pre-boss, or boss (`LevelConfig.IsElite || IsPreBoss || IsBoss`).
> `RunDirector.RollPressureMechanic` — node-type guard.
> `LevelConfig.IsElite` and `LevelConfig.IsPreBoss` in `RuntimeModels.cs` (both included in `Clone()`).

**[PRESSURE-ROLL-003]** ✅ Implemented
Appearance probability by floor: F2 = 12 %, F3 = 30 %, F4 = 55 %, F5+ = 80 %. Uses `rng.NextDouble()` comparison.
> `RunDirector.RollPressureMechanic` — `chance` switch expression.

**[PRESSURE-ROLL-004]** ✅ Implemented
At most one pressure mechanic is added per puzzle. `ActiveModifiers.Contains(chosen)` deduplicates before adding.
> `RunDirector.RollPressureMechanic` — dedup guard before `config.ActiveModifiers.Add`.

**[PRESSURE-ROLL-005]** ✅ Implemented
`PressureWave` (`BossModifierId.PressureWave = 106`) is only eligible from floor 3+.
> `RunDirector.RollPressureMechanic` — `if (floor >= 3) candidates.Add(BossModifierId.PressureWave)`.

---

## Mechanic 1 — Countdown Fill (`BossModifierId.CountdownFill = 103`)

### Concept

One or more cells — or an entire box, row, column, or **chain** — are marked "threatened". Each threatened target displays a countdown: the number of placements the player may still make on the *whole board* before the threat expires. If the target has not been filled when the counter hits zero, the player loses **2 HP per unfilled threatened cell**.

This is the primary "fill in X placements" mechanic. A **single-cell** CountdownFill threat corresponds directly to the design idea of "a field that needs to be filled within X first placements."

### Countdown Counter Values by Floor and Scope

| Scope | Floor 2 (Low) | Floor 3 (Medium) | Floor 4 (High) | Floor 5+ (VH) |
|-------|:------------:|:----------------:|:--------------:|:-------------:|
| **Single cell** | 10 | 8 | 6 | 4 |
| **Box** | 18 | 14 | 10 | 8 |
| **Row / Column** | 14 | 11 | 8 | 6 |
| **Chain** (3–5 cells) | 14 | 11 | 8 | 6 |

### Chain Scope — Dual Outcome

The **Chain** scope connects 3–5 logically related empty cells (sharing a row, column, or box):
- **Success** (all chain cells filled before counter = 0): **+1 HP** reward.
- **Failure** (counter = 0, any chain cells unfilled): **−2 HP** as normal.

Chain cells can be filled in any order. Length = 3 at Low/Med intensity, target 4 at High/VH.

### Counter Behaviour (Resolved Decision #1)

- Decrements on **every placement** — both correct and incorrect. Guessing costs time.
- A threat is immediately removed (no HP loss) when all target cells are filled correctly.
- Multiple independent threats can coexist, each with its own counter.
- On expiry: HP is deducted; the cell(s) are **not locked** — the player may still fill them.

### Initialization Requirements

**[PRESSURE-INIT-001]** ✅ Implemented
CountdownFill init: scope selected by `PickCountdownScope(intensity, rng)` → cells selected by `SelectThreatCells(board, scope, rng)` → counter from `CounterForScope(scope, cells.Count, intensity)` → `PressureThreat` added to `LevelState.ActiveThreats`.
> `RunDirector.InitializePressureMechanics` — `case BossModifierId.CountdownFill`.

**[PRESSURE-INIT-005]** ✅ Implemented
Chain scope targets 3–4 cells. Seed cell + connected cells sharing row/col/box. Falls back to `SingleCell` if fewer than 3 qualifying empty cells found.
> `RunDirector.SelectThreatCells` — `case ThreatScope.Chain`.

**[PRESSURE-INIT-006]** ✅ Implemented
All cell selection (`SelectThreatCells`, `SelectHauntedCell`, `SpawnWaveThreat`) excludes `LevelState.PresolvedCells` (e.g. Thermo bulb cells).
> `RunDirector.SelectThreatCells`, `SelectHauntedCell`, `SpawnWaveThreat` — `PresolvedCells.Contains` guards.

---

## Mechanic 2 — Haunted Cell (`BossModifierId.HauntedCell = 104`)

### Concept

A specific cell is **haunted**. While it remains empty, every mistake the player makes *anywhere else* on the board triggers **double HP loss** (i.e. `ApplyMistakePenalty` fires twice). The haunting ends the moment the cell is filled correctly — no HP penalty on resolution.

### Intensity Parameters

| Intensity | Cell selection |
|-----------|---------------|
| Low | Easy cell — last quartile by candidate count (most candidates = easiest to fill early) |
| Medium | Mid-difficulty cell — upper half by candidate count |
| High | Hard cell — fewest candidates |
| VeryHigh | Hardest cell (fewest candidates) + extra damage also doubled by any active DoublePenalty debuff |

### Initialization Requirements

**[PRESSURE-INIT-002]** ✅ Implemented
HauntedCell init: `SelectHauntedCell(board, config.Intensity, rng)` sorts empty non-presolved cells by candidate count ascending; picks based on intensity quartile. Result stored in `LevelState.HauntedCell`.
> `RunDirector.InitializePressureMechanics` — `case BossModifierId.HauntedCell`.
> `RunDirector.SelectHauntedCell`.

---

## Mechanic 3 — Crumbling Region (`BossModifierId.CrumblingRegion = 105`)

### Concept

A box, row, or column is **crumbling**. Each empty cell in it starts with a **fragility counter**. Every mistake the player makes *anywhere on the board* causes empty crumbling cells to lose fragility. When a cell's fragility hits zero, the game **auto-fills it with the correct value** and the player loses **1 HP**. The puzzle remains fully solvable.

The player can prevent auto-fill by solving a crumbling cell before its fragility runs out; doing so removes it from the crumbling pool safely (no HP loss).

### Intensity Parameters

| Intensity | Starting fragility | VeryHigh behaviour |
|-----------|--------------------|---------------------|
| Low | 4 | — |
| Medium | 3 | — |
| High | 2 | — |
| VeryHigh | 2 | Only the **first** (lowest-keyed) empty crumbling cell loses fragility per mistake. All others unaffected until it is resolved. |

### Initialization Requirements

**[PRESSURE-INIT-003]** ✅ Implemented
CrumblingRegion init: `SelectCrumblingRegion(board, rng)` returns `(regionType, regionIndex)` for a region with ≥ 2 empty cells. `FragilityForIntensity(intensity)` → fragility (4/3/2/2). All empty non-presolved cells in that region are entered into `LevelState.CrumblingCells[row*100+col] = fragility`. `LevelState.CrumblingStartFragility` is set for crack-render normalisation. Only one region per puzzle.
> `RunDirector.InitializePressureMechanics` — `case BossModifierId.CrumblingRegion`.
> `RunDirector.SelectCrumblingRegion`, `RunDirector.GetRegionEmptyCells`.

---

## Mechanic 4 — Pressure Wave (`BossModifierId.PressureWave = 106`)

### Concept

The board has a **wave counter** starting at W. Every time the wave counter reaches zero, a new random empty cell is assigned a fresh short Countdown Fill threat. The counter resets to W. This continues until the puzzle is complete.

The wave counter **never pauses** — even while 3 threats are already active, it keeps counting. Spawns are skipped when at the cap, but the counter resets regardless.

### Intensity Parameters

| Intensity | Wave interval W | Per-threat counter |
|-----------|:---------------:|:------------------:|
| Low | 12 | 7 |
| Medium | 10 | 6 |
| High | 8 | 5 |
| VeryHigh | 6 | 4 |

### Initialization Requirements

**[PRESSURE-INIT-004]** ✅ Implemented
PressureWave init: `LevelState.WaveInterval = WaveIntervalForIntensity(intensity)`, `WaveThreatCounter = PerThreatCounterForIntensity(intensity)`, `WaveCounter = WaveInterval`, `WaveSpawnCount = 0`.
> `RunDirector.InitializePressureMechanics` — `case BossModifierId.PressureWave`.

---

## Curse Delivery

**[PRESSURE-INIT-007]** ✅ Implemented
Curse `counting_shadow`: at puzzle start, adds one `SingleCell` `PressureThreat` with `RemainingPlacements = State.CurrentFloor + 4`. Stacks with any modifier-driven CountdownFill threat.
> `RunDirector.InitializePressureMechanics` — curse block after modifier switch.

**[PRESSURE-INIT-008]** ✅ Implemented
Curse `hollow_eye`: assigns `LevelState.HauntedCell` at `Medium` intensity only if `HauntedCell == (-1, -1)` (modifier-driven haunting takes priority).
> `RunDirector.InitializePressureMechanics` — curse block after modifier switch.

---

## Core Tick Logic Requirements

**[PRESSURE-TICK-001]** ✅ Implemented
Every correct placement decrements `RemainingPlacements` on **all** `LevelState.ActiveThreats` by 1.
> `RunDirector.TickPressureThreats` — counter-decrement loop (step 1).

**[PRESSURE-TICK-002]** ✅ Implemented
Every incorrect placement **also** decrements `RemainingPlacements` on all `ActiveThreats` by 1, and advances the wave counter. Implemented via `TickPressureCountersOnly()` called in `PlaceNumber` after `Mistakes++`.
> `RunDirector.PlaceNumber` — incorrect path.

**[PRESSURE-TICK-003]** ✅ Implemented
After decrementing, if `placedRow, placedCol` is in `threat.Cells`, remove it. If `Cells` becomes empty → success path.
> `RunDirector.TickPressureThreats` — step 2, success check.

**[PRESSURE-TICK-004]** ✅ Implemented
Chain success (`threat.IsChain == true` on empty-cells resolution) awards `+1 HP`, clamped to `State.MaxHP`.
> `RunDirector.TickPressureThreats` — `if (threat.IsChain) State.CurrentHP = Math.Min(...)`.

**[PRESSURE-TICK-005]** ✅ Implemented
Threat expiry (`RemainingPlacements <= 0` with `Cells.Count > 0`) deducts 2 HP per unfilled cell via `ApplyMistakePenalty(penaltyPer)`.
> `RunDirector.TickPressureThreats` — expiry branch.

**[PRESSURE-TICK-006]** ✅ Implemented
When `DoublePenalty` debuff is active, expiry penalty is 4 HP per unfilled cell instead of 2. Computed as `IsDebuffActive(BossModifierId.DoublePenalty) ? 4 : 2`.
> `RunDirector.TickPressureThreats` — `penaltyPer` calculation.

**[PRESSURE-TICK-007]** ✅ Implemented
Wave counter decrements on every **correct** placement. When `WaveCounter <= 0`, spawns one threat (if under cap) then resets `WaveCounter = WaveInterval` regardless.
> `RunDirector.TickPressureThreats` — step 4, `WaveInterval > 0` block.
> Note: per PRESSURE-TICK-002, this should also decrement on incorrect placements once that gap is closed.

**[PRESSURE-TICK-008]** ✅ Implemented
Wave spawn capped at 3 concurrent wave-spawned threats (`ls.WaveSpawnCount < 3`). Spawn is skipped but counter still resets.
> `RunDirector.TickPressureThreats` — `if (ls.WaveSpawnCount < 3) SpawnWaveThreat()`.

**[PRESSURE-TICK-009]** ✅ Implemented
`WaveSpawnCount` decrements when a wave-spawned threat (`IsWaveSpawned == true`) is removed (success or expiry).
> `RunDirector.TickPressureThreats` — both success and expiry branches check `threat.IsWaveSpawned`.

**[PRESSURE-TICK-010]** ✅ Implemented
HauntedCell: when a mistake occurs at `(row, col)` that is **not** the haunted cell, fire `ApplyMistakePenalty(extraDmg)` for 1 extra HP (2 with DoublePenalty). No penalty when filling the haunted cell itself.
> `RunDirector.TickPressureMistake` — HauntedCell block.

**[PRESSURE-TICK-011]** ✅ Implemented
HauntedCell extra damage respects `DoublePenalty`: `extraDmg = IsDebuffActive(BossModifierId.DoublePenalty) ? 2 : 1`. Both the base penalty and the HauntedCell extra both double independently (stack freely, no cap).
> `RunDirector.TickPressureMistake` — `extraDmg` computation.

**[PRESSURE-TICK-012]** ✅ Implemented
CrumblingRegion (Low/Med/High intensity): on every mistake, **all** cells in `LevelState.CrumblingCells` lose 1 fragility. Cells reaching 0 are auto-filled immediately.
> `RunDirector.TickPressureMistake` — CrumblingCells foreach loop.

**[PRESSURE-TICK-013]** ✅ Implemented
CrumblingRegion **VeryHigh** intensity: on each mistake, only the **first** (minimum `row*100+col` key) crumbling cell erodes. `LevelState.CrumblingVeryHigh` flag set in `InitializePressureMechanics`; branch in `TickPressureMistake` handles VH separately.
> `RuntimeModels.LevelState` — `CrumblingVeryHigh` field.
> `RunDirector.InitializePressureMechanics` — `levelState.CrumblingVeryHigh = (config.Intensity == BossModifierIntensity.VeryHigh)`.
> `RunDirector.TickPressureMistake` — `if (ls.CrumblingVeryHigh)` branch.

**[PRESSURE-TICK-014]** ✅ Implemented
A crumbling cell reaching fragility 0 triggers `AutoFillCrumbledCell(row, col)`.
> `RunDirector.TickPressureMistake` — `if (ls.CrumblingCells[k] <= 0)` branch.

**[PRESSURE-TICK-015]** ✅ Implemented
`AutoFillCrumbledCell`: places `CurrentBoard.Solution[row, col]` via `CurrentBoard.PlaceValue`; removes `row*100+col` from `CrumblingCells`; costs 1 HP (2 with DoublePenalty) via `ApplyMistakePenalty`.
> `RunDirector.AutoFillCrumbledCell`.

**[PRESSURE-TICK-016]** ✅ Implemented
`AutoFillCrumbledCell` silently resolves any `ActiveThreats` whose `Cells` list contained the auto-filled cell (removes that cell, removes threat if empty — no HP reward for chain success via auto-fill).
> `RunDirector.AutoFillCrumbledCell` — threat resolution loop.

**[PRESSURE-TICK-017]** ✅ Implemented
`AutoFillCrumbledCell` does **not** decrement threat counters. Auto-fill is not a player placement.
> `RunDirector.AutoFillCrumbledCell` — does not call `TickPressureThreats`.

**[PRESSURE-TICK-018]** ✅ Implemented
`AutoFillCrumbledCell` clears `LevelState.HauntedCell` to `(-1, -1)` if the auto-filled cell matches the haunted cell.
> `RunDirector.AutoFillCrumbledCell` — `if (ls.HauntedCell == (row, col)) ls.HauntedCell = (-1, -1)`.

**[PRESSURE-TICK-019]** ✅ Implemented
All HP penalties from pressure mechanics route through `RunDirector.ApplyMistakePenalty(int damage)`, which respects `MistakeShieldCharges`, class passives, relic absorb, warding flame, and death-prevention.
> `RunDirector.ApplyMistakePenalty(int damage)`.

---

## UI / Rendering Requirements

**[PRESSURE-UI-001]** ✅ Implemented
No pressure visuals when `RunState.RunNumber == 0` (Sudoku basics tutorial). `InRunController.BuildPressureRenderData` returns `null` when `run.State.RunNumber == 0`.
> `InRunController.BuildPressureRenderData` — `RunNumber == 0` early return.

**[PRESSURE-UI-002]** ✅ Implemented
Every cell that is a target of an active `PressureThreat` displays a count badge (top-right corner) showing `RemainingPlacements` while the threat is active.
> `BoardViewController.RenderBoard` — `badgeText` and `cell.BadgeLabel` block.

**[PRESSURE-UI-003]** ✅ Implemented
Badge colour ramps based on `RemainingPlacements / InitialPlacements` ratio: above 50% = grey `(0.70, 0.70, 0.70, 0.90)`, 25–50% = amber `(1.00, 0.65, 0.10, 0.90)`, below 25% = red `(0.95, 0.20, 0.20, 0.90)`. `InitialPlacements` stored on `PressureThreat`, set at creation and in `SpawnWaveThreat`. Ratio-based colour ramp applied in both `RenderBoard` and `RenderPressureThreatOverlays`.
> `RuntimeModels.PressureThreat` — `InitialPlacements` field.
> `BoardViewController.RenderBoard` + `RenderPressureThreatOverlays` — badge colour block.

**[PRESSURE-UI-004]** ✅ Implemented
When a threat expires, the affected cells receive a red cell flash (via `FlashCells`) and each cell's badge `RectTransform` is shaken horizontally (3 cycles, 3 px amplitude, 0.3 s, amplitude decays to zero). Both effects are gated behind `!AccessibilityService.IsReduceMotion`.
> `BoardViewController.ShakeBadgeCells` + `ShakeBadgeCoroutine` — badge shake.
> `InRunController` line ~1289 — calls `ShakeBadgeCells` inside existing `!IsReduceMotion` block alongside `ScreenShake`.

**[PRESSURE-UI-005]** ✅ Implemented
When a chain threat succeeds, all chain cells briefly flash gold `(1.00, 0.85, 0.10, 0.80)` for 0.4 s. Implemented via `LevelState.ChainSuccessCells` populated in `TickPressureThreats`, forwarded through `PressureRenderData`, rendered via `FlashCells` coroutine in `BoardViewController`.
> `RuntimeModels.LevelState` — `ChainSuccessCells`.
> `RunDirector.TickPressureThreats` — chain success branch.
> `BoardViewController` — `FlashCells` coroutine.

**[PRESSURE-UI-006]** ✅ Implemented
Haunted cell: pulsing purple vignette `(0.45, 0.10, 0.60, 1.0)` lerped onto the cell background. Alpha oscillates 0.25–0.65 at `sin(Time.time * 2f)`. Stops immediately when the cell is filled (checked via `val == 0`).
> `BoardViewController.RenderBoard` — HauntedCell pulse block.

**[PRESSURE-UI-007]** ✅ Implemented
Crumbling cells: brown crack tint `(0.60, 0.35, 0.15, 1.0)` lerped at `crackIntensity * 0.65f` where `crackIntensity = 1 - fragility/maxFragility`. Invisible at full health; fully tinted at fragility 0.
> `BoardViewController.RenderBoard` — CrumblingCells block.

**[PRESSURE-UI-008]** ✅ Implemented
When `AutoFillCrumbledCell` fills a cell, that cell briefly flashes green `(0.35, 0.90, 0.45, 0.80)` for 0.3 s, then settles to given-cell background colour. Implemented via `LevelState.CrumbledFlashCells`, forwarded through `PressureRenderData`, rendered via `FlashCells` coroutine.
> `RuntimeModels.LevelState` — `CrumbledFlashCells`.
> `RunDirector.AutoFillCrumbledCell` — adds cell to `CrumbledFlashCells`.
> `BoardViewController` — `FlashCells` coroutine.

**[PRESSURE-UI-009]** ✅ Implemented
When `SpawnWaveThreat` spawns a new threat, a one-shot translucent ring ripple expands from the board edge inward (0.5 s animation). `WaveRipplePending` in `PressureRenderData` is set when wave spawn count increases; `TriggerWaveRipple()` in `BoardViewController` runs the expanding ring coroutine.
> `RuntimeModels.PressureRenderData` — `WaveRipplePending`.
> `InRunController.BuildPressureRenderData` — sets `WaveRipplePending`.
> `BoardViewController.TriggerWaveRipple` — expanding ring coroutine.

**[PRESSURE-UI-010]** ✅ Implemented
For Row, Column, and Box scope threats, a coloured border strip is rendered along the full region (same grey/amber/red colour ramp as per-cell badge). Implemented via `DrawPressureStrip` / `DrawPressureStripAbs` helper methods called from `RenderPressureThreatOverlays`.
> `BoardViewController.RenderPressureThreatOverlays` — Row/Col/Box scope branch.
> `BoardViewController.DrawPressureStrip`, `DrawPressureStripAbs`.

**[PRESSURE-UI-011]** ✅ Implemented
For Chain scope threats, a pulsing animated line connects all chain cells. The line dims as `RemainingPlacements` drops. Reuses the existing line-drawing infrastructure. Implemented via `DrawPressureLine` called from `RenderPressureThreatOverlays`.
> `BoardViewController.RenderPressureThreatOverlays` — Chain scope branch.
> `BoardViewController.DrawPressureLine`.

---

## Persistence Requirements

**[PRESSURE-SAVE-001]** ✅ Implemented
All pressure state fields on `LevelState` are marked `[System.NonSerialized]` — they are ephemeral per-puzzle and not included in the run save envelope.
> `RuntimeModels.LevelState` — all pressure fields tagged `[System.NonSerialized]`.

**[PRESSURE-SAVE-002]** ✅ Implemented
On mid-puzzle resume (`RunResumeService.TryResumeFromSave`), pressure state re-initialises from seed via `InitializePressureMechanics`. Fragility resets to full. Wave counter resets to `WaveInterval`. Any mid-puzzle progression in crumbling is lost — accepted limitation.
> `RunDirector.InitializePressureMechanics` — called during `StartLevel` after board generation.

**[PRESSURE-SAVE-003]** ✅ Implemented
`RunAutoSaveCoordinator` has a comment documenting that pressure mechanics are not persisted mid-puzzle.
> `RunAutoSaveCoordinator.cs` — NOTE comment at line 51.

---

## Edge Case Requirements

**[PRESSURE-EDGE-001]** ✅ Implemented
If Chain scope selection finds fewer than 3 qualifying empty cells, fall back to `SingleCell` scope (return a single cell instead of a chain).
> `RunDirector.SelectThreatCells` — `case ThreatScope.Chain` early-exit fallback.

**[PRESSURE-EDGE-002]** ✅ Implemented
`SelectCrumblingRegion` requires a region with ≥ 2 empty cells. If no qualifying region is found, returns `regionType = -1`; `InitializePressureMechanics` skips the mechanic silently that puzzle.
> `RunDirector.SelectCrumblingRegion` — candidates require `cnt >= 2`.
> `RunDirector.InitializePressureMechanics` — `if (regionType < 0) break`.

**[PRESSURE-EDGE-003]** ✅ Implemented
`SpawnWaveThreat` finds all empty cells not already in any active threat's `Cells` list. If the resulting `available` list is empty (all empty cells threatened), the spawn is skipped and the wave counter still resets.
> `RunDirector.SpawnWaveThreat` — `if (available.Count == 0) return`.

**[PRESSURE-EDGE-004]** ✅ Implemented
HauntedCell and CrumblingRegion can overlap on the same cell. Both apply independently. `AutoFillCrumbledCell` clears `HauntedCell` if the auto-filled cell happens to be the haunted cell.
> `RunDirector.AutoFillCrumbledCell` — `if (ls.HauntedCell == (row, col)) ls.HauntedCell = (-1,-1)`.

**[PRESSURE-EDGE-005]** ✅ Implemented
`counting_shadow` curse and `CountdownFill` modifier can both be active — they produce two independent entries in `LevelState.ActiveThreats`.
> `RunDirector.InitializePressureMechanics` — modifier switch does not guard against duplicate threat types; curse block is additive.

**[PRESSURE-EDGE-006]** ✅ Implemented
`hollow_eye` curse only sets `HauntedCell` when it is not already set by a modifier (`levelState.HauntedCell == (-1, -1)` guard). Modifier-driven haunting takes priority.
> `RunDirector.InitializePressureMechanics` — `if (...CurseService.IsActive(...) && levelState.HauntedCell == (-1, -1))`.

---

## Design Summary

| Mechanic | `BossModifierId` | Driver | Duration | HP risk | Reward? |
|----------|:---------------:|--------|----------|---------|---------|
| Countdown Fill | 103 | All placements (correct + wrong) | One-shot per threat | 2 per unfilled cell | +1 HP (Chain only) |
| Haunted Cell | 104 | Mistakes | Until cell filled | ×2 per mistake while active | No |
| Crumbling Region | 105 | Mistakes | Until region solved | 1 per auto-filled cell | No |
| Pressure Wave | 106 | Placements (periodic) | Whole puzzle | 2 per expired wave threat | No |

## Open Gaps Summary

All requirements implemented. ✅
