# Boss Debuff Effects

**Version:** 1.4 | **Date:** 2026-10-07 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.4 | 2026-10-07 | implemented | Delivery decided and implemented (SPEC-CONF-021, SPEC-AMB-002, GAP-026): one debuff per Garden Run boss from Floor 3, drawn from run seed + floor; CrossWipe/PencilScramble at VeryHigh only; GivenReveal not delivered (DEBUFF-DELIV-001..004, REQ-BOSS-108). Statuses set from tests (RadiusWipe, PencilScramble, CellBlur, RevealCost now ✅; CellBlur rendered, Item Block blocks the whole item use). PencilPoison, FogPulse, NumberBlind, DigitShift descoped to future scope. Hook rows 014–022 added. Wrong digits are rejected since 1.1 (SPEC-CONF-003). Evidence: BossDebuffTests, InRunUiPlayModeTests. |
| 1.3 | 2026-04-14 | implemented | Added requirement IDs (DEBUFF-DEF-001..018, DEBUFF-HOOK-001..008) to all debuffs and integration hooks. |
| 1.2 | 2026-04-09 | implemented | Added BoxWipe(99), CrossWipe(100), PencilDrain(101), GoldFine(102). GameEnums extended. RunDirector.ApplyMistakePenalty wired for all new debuffs. ClearBoardRegion helper added. BossService ModifierData entries added. |
| 1.1 | 2026-04-08 | partial | First 5 debuffs implemented: RowWipe(94), ColWipe(95), DoublePenalty(96), CellLock(97), PencilBlind(98). BossModifierId enum extended. RunDirector.ApplyMistakePenalty(row,col) overload added with all hooks. TickLockedCells()/GetLockedCells() on RunDirector. BoardViewController renders CellLocked tint. InRunController: locked cells block input, correct placement calls TickLockedCells. BossService: tier/impact/name/description entries added; debuffs excluded from normal modifier pool. |
| 1.0 | 2026-04-08 | new | Initial brainstorm — no implementation yet |

---

## Overview

This document catalogues negative board-state effects that activate when the player places a **wrong digit** (i.e. a move that triggers `ApplyMistakePenalty`). These are distinct from the existing boss modifiers (fog, antiknight, etc.) which constrain what digits are valid — debuffs *react* to mistakes after they happen.

Since 1.1 a wrong digit is one that differs from the stored solution; it is rejected (never written to the board) and `RunDirector.PlaceNumber` applies the penalty and the debuff hooks (SPEC-CONF-003, GAP-011, GAP-035).

Each debuff would be a `BossModifierId` entry whose "penalty hook" fires inside `RunDirector.ApplyMistakePenalty` or a new `OnWrongPlacement` delegate on `RunDirector`.

---

## Debuff Categories

### A — Cell-erasure Effects
Immediately remove already-placed (non-given) digits from the board.

| Req ID | Id | Name | Effect | Notes | Status |
|--------|----|------|--------|-------|--------|
| DEBUFF-DEF-001 | **RowWipe** (94) | Shatter Row | On mistake: clears all player-placed digits in the same row as the wrong cell. | Strong on full rows near completion. | ✅ |
| DEBUFF-DEF-002 | **ColWipe** (95) | Shatter Column | On mistake: clears all player-placed digits in the same column as the wrong cell. | Symmetric to RowWipe; stack both for extreme runs. | ✅ |
| DEBUFF-DEF-003 | **BoxWipe** (99) | Shatter Box | On mistake: clears all player-placed digits in the same region/box as the wrong cell. | | ✅ |
| DEBUFF-DEF-004 | **CrossWipe** (100) | Shatter Cross | On mistake: clears the row AND column (full cross). | Essentially RowWipe + ColWipe combined; reserved for VeryHigh intensity. | ✅ |
| DEBUFF-DEF-005 | **RadiusWipe** (107) | Shatter Radius | On mistake: clears all player-placed digits within a 2-cell Chebyshev radius of the wrong cell. | Localised area effect. | ✅ |

**Implementation hook:** `SudokuBoard.ClearValue(r, c)` already exists. Wipe logic iterates the board and calls `ClearValue` on non-given cells in the target area.

---

### B — Pencil-mark Pollution
Adds or removes pencil marks to sabotage the player's notes.

| Req ID | Id | Name | Effect | Notes | Status |
|--------|----|------|--------|-------|--------|
| DEBUFF-DEF-006 | **PencilPoison** | Misinformation | On mistake: adds 1–3 random *wrong* pencil marks to each empty cell in the wrong cell's row. | Requires a "fake mark" bit separate from real pencil marks, or just pollutes the existing HashSet. | Descoped (future scope, 2026-10-07) |
| DEBUFF-DEF-007 | **PencilBlind** (98) | Fog of Notes | On mistake: removes all pencil marks in the wrong cell's row and column. | Punishes pure pencil-mark solvers. | ✅ |
| DEBUFF-DEF-008 | **PencilScramble** (108) | Scramble Notes | On mistake: replaces the pencil marks of every empty cell with 2–3 random values that are never the cell's solution. | Nuclear option — delivered only at VeryHigh intensity (DEBUFF-DELIV-003). | ✅ |

**Note:** If pencil pollution uses the existing `_pencilMarks[r,c]` HashSet, distinguishing fake from real marks requires a second board layer. A simpler approach: erase rather than add fake marks.

---

### C — Vision Impairment
Reduces what the player can see.

| Req ID | Id | Name | Effect | Notes | Status |
|--------|----|------|--------|-------|--------|
| DEBUFF-DEF-009 | **FogPulse** | Fog Surge | On mistake: adds a 1-tile fog ring around the wrong cell for 3 moves. | Extends the existing FogOfWar rendering; uses `FogCells` list. | Descoped (future scope, 2026-10-07) |
| DEBUFF-DEF-010 | **NumberBlind** | Glare | On mistake: hides all instances of the placed digit on the board for 2 moves (cells exist, digits invisible). | Requires a "blinded digit" field in render state. | Descoped (future scope, 2026-10-07) |
| DEBUFF-DEF-011 | **CellBlur** (111) | Ink Blur | On mistake: hides the digit and pencil labels of the 3×3 area around the wrong cell for 3 correct placements (not fog — values unchanged; the area is tinted). | Visual only, no board state change. Rendered by `BoardViewController`. | ✅ |

---

### D — Resource Drain
Compounds existing HP/resource penalties.

| Req ID | Id | Name | Effect | Notes | Status |
|--------|----|------|--------|-------|--------|
| DEBUFF-DEF-012 | **DoublePenalty** (96) | Brittle Ink | Wrong placements deal double the mistake HP cost (2 instead of 1 at Harmony H0–H4). | Multiplies the damage in `ApplyMistakePenalty(row, col)`. | ✅ |
| DEBUFF-DEF-013 | **PencilDrain** (101) | Ink Spill | Each wrong placement costs 1 extra pencil charge (in addition to HP). | Only relevant if pencil charges are finite. | ✅ |
| DEBUFF-DEF-014 | **GoldFine** (102) | Tax Collector | Deducts 5 gold per mistake (clamped to 0). | Punishes economy runners. | ✅ |
| DEBUFF-DEF-015 | **RevealCost** (110) | Item Block | After a mistake, items cannot be used until 2 more correct placements. | The whole use is blocked (effect and consumption). | ✅ |

---

### E — Board Mutation
Changes the puzzle state in ways that alter correct solutions.

| Req ID | Id | Name | Effect | Notes | Status |
|--------|----|------|--------|-------|--------|
| DEBUFF-DEF-016 | **DigitShift** | Rolling Numbers | After a mistake: the target cell and 1–2 random empty cells swap their *correct* solution values, requiring different digits than before. | Conflicts with solution-based validation (SPEC-CONF-003): a changed solution makes the puzzle non-unique. | Descoped (future scope, 2026-10-07) |
| DEBUFF-DEF-017 | **GivenReveal** (109) | Stone to Ink | After a mistake: permanently converts up to 2 of the player's correct placed digits to "given" (immovable) status. | Hook implemented but **not delivered**: since wrong digits are rejected, it only fixes correct digits, which also protects them from the wipe debuffs. | ✅ (hook only) |
| DEBUFF-DEF-018 | **CellLock** (97) | Frozen Cell | After a mistake: the wrong cell becomes temporarily un-editable for 3 correct placements. | Simpler than DigitShift; just sets a "locked until move N" state. | ✅ |

---

## Delivery (REQ-BOSS-108)

Decided 2026-10-07 (SPEC-CONF-021, SPEC-AMB-002, GAP-026). Implemented in `RunDirector.GetBossDebuffForCurrentFloor`, `RunDirector.BuildLevelConfig` and `BossService.GetBossDebuffPool`.

| Req ID | Rule |
|--------|------|
| DEBUFF-DELIV-001 | From Floor 3 (0-based floor ≥ 2) every **Garden Run** boss carries exactly one debuff. Floors 1–2, normal puzzles and the other modes (Tutorial, Endless Zen, Spirit Trials, Monthly Walk) carry none. |
| DEBUFF-DELIV-002 | The debuff depends only on the run seed and the floor, so the boss gate, the boss puzzle and a resumed run agree. |
| DEBUFF-DELIV-003 | Pool: RowWipe, ColWipe, BoxWipe, RadiusWipe, DoublePenalty, CellLock, PencilBlind, PencilDrain, GoldFine, RevealCost, CellBlur. CrossWipe and PencilScramble join the pool only at VeryHigh intensity (run 9+). GivenReveal is not delivered. |
| DEBUFF-DELIV-004 | The boss gate shows the intensity and, from Floor 3, "Boss debuff: name – rule" under its title. The in-run View Modifiers list and the modifier legend list the debuff too. |

Evidence: `BossDebuffTests` (delivery per floor/mode/intensity, determinism, one hook test per debuff), `InRunUiPlayModeTests.BossGate_ShowsIntensity_AndTheDebuffFromFloorThree`.

---

## Recommended First Candidates

Ordered by implementation effort vs. tension payoff:

1. **RowWipe** / **ColWipe** — high tension, low complexity (already have `ClearValue`)
2. **DoublePenalty** — trivial, already supported by `ApplyMistakePenalty(damage)`
3. **CellLock** — low complexity, clear visual feedback
4. **PencilBlind** — good counterplay to over-reliance on pencil marks
5. **FogPulse** — extends existing FogOfWar, natural fit for boss encounters

---

## Integration Points (implemented)

| Req ID | Hook | Implementation | File | Status |
|--------|------|----------------|------|--------|
| DEBUFF-HOOK-001 | `ApplyMistakePenalty(int row, int col)` new overload dispatches all debuff effects | `RunDirector.ApplyMistakePenalty(row,col)` | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-002 | DoublePenalty doubles the mistake HP cost before HP deduction | `ApplyMistakePenalty` — `damage *= 2` | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-003 | RowWipe calls `ClearBoardRow(row)` on mistake | `ApplyMistakePenalty` — RowWipe branch | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-004 | ColWipe calls `ClearBoardCol(col)` on mistake | `ApplyMistakePenalty` — ColWipe branch | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-005 | PencilBlind calls `ClearPencilRowCol(row,col)` | `ApplyMistakePenalty` — PencilBlind branch | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-006 | CellLock writes `row*100+col → 3` into `LevelState.LockedCells` | `LockCell(row,col)` → `LockedCells` dict | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-007 | `TickLockedCells()` decrements locked counts; removes entries ≤ 0; called after every correct placement | `RunDirector.TickLockedCells()` | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-008 | `GetLockedCells()` returns `HashSet<(int,int)>` for input blocking and board tinting | `RunDirector.GetLockedCells()` | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-009 | `LevelState.LockedCells: Dictionary<int,int>` — `[NonSerialized]`, transient per puzzle | `LevelState.LockedCells` | `Assets/Scripts/Core/RuntimeModels.cs` | ✅ |
| DEBUFF-HOOK-010 | `BossModifierId` enum entries 94–102 registered | `BossModifierId` enum | `Assets/Scripts/Core/GameEnums.cs` | ✅ |
| DEBUFF-HOOK-011 | `BossService.ModifierData` has tier/impact/name/description for IDs 94–102; IDs ≥ 94 excluded from `BuildEligiblePool` (debuffs are delivered by DEBUFF-DELIV-001 only) | `ModifierData` dictionary | `Assets/Scripts/Boss/BossService.cs` | ✅ |
| DEBUFF-HOOK-012 | `BoardViewController.RenderBoard` applies `GamePalette.CellLocked` (deep magenta) tint via `Color.Lerp(..., 0.75f)` for locked cells | `RenderBoard` — `lockedCells` param | `Assets/Scripts/UI/BoardViewController.cs` | ✅ |
| DEBUFF-HOOK-013 | `InRunController` blocks input on locked cells; `RunDirector.PlaceNumber` routes mistakes through `ApplyMistakePenalty(row,col)` and correct placements through `TickLockedCells()` | `InRunController` input handlers, `RunDirector.PlaceNumber` | `Assets/Scripts/UI/InRunController.cs`, `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-014 | RadiusWipe calls `ClearBoardRadius(row,col)` | `ApplyMistakePenalty` — RadiusWipe branch | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-015 | PencilScramble calls `ScramblePencilMarks()` | `ApplyMistakePenalty` — PencilScramble branch | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-016 | GivenReveal calls `ConvertCellsToGiven(2)` | `ApplyMistakePenalty` — GivenReveal branch | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-017 | RevealCost sets `LevelState.ItemUseBlockedMoves = 2`; `TryUseItem` and `InRunController.ApplyItemEffect` refuse items while it is above 0 | `ApplyMistakePenalty`, `TryUseItem`, `ApplyItemEffect` | `Assets/Scripts/Run/RunDirector.cs`, `Assets/Scripts/UI/InRunController.cs` | ✅ |
| DEBUFF-HOOK-018 | CellBlur fills `LevelState.CellBlurCells` (3×3 around the wrong cell) for 3 correct placements; `BoardViewController.RenderBoard` hides those labels | `ApplyCellBlur`, `RenderBoard` | `Assets/Scripts/Run/RunDirector.cs`, `Assets/Scripts/UI/BoardViewController.cs` | ✅ |
| DEBUFF-HOOK-019 | `TickDebuffTimers()` counts RevealCost and CellBlur down on every correct placement | `RunDirector.TickDebuffTimers` | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-020 | `BossModifierId` entries 107–111 (batch 2) | `BossModifierId` enum | `Assets/Scripts/Core/GameEnums.cs` | ✅ |
| DEBUFF-HOOK-021 | Batch 2 registered in `BossService.ModifierData`, excluded from the player pool | `ModifierData` dictionary | `Assets/Scripts/Boss/BossService.cs` | ✅ |
| DEBUFF-HOOK-022 | `LevelState.ItemUseBlockedMoves`, `CellBlurCells`, `CellBlurMovesLeft` | `LevelState` | `Assets/Scripts/Core/RuntimeModels.cs` | ✅ |

---

## Descoped (future scope)

Decided 2026-10-07 and recorded in `future-scope-decision-register.md`. The IDs stay reserved.

| Req ID | Id | Reason |
|--------|----|--------|
| DEBUFF-DEF-006 | PencilPoison | Needs a second pencil layer to tell fake marks from the player's own. |
| DEBUFF-DEF-009 | FogPulse | Needs fog cells that expire; Fog of War cells are permanent until revealed. |
| DEBUFF-DEF-010 | NumberBlind | Overlaps CellBlur; needs a per-digit render state. |
| DEBUFF-DEF-016 | DigitShift | Changing the solution breaks solution-based validation and uniqueness (SPEC-CONF-003). |

---

## Scope Note

These are **boss-encounter modifiers**, not general run modifiers. They activate only when included in `LevelConfig.ActiveModifiers` (which for boss puzzles includes the player's chosen modifiers).
