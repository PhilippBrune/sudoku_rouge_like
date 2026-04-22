# Boss Debuff Effects

**Version:** 1.3 | **Date:** 2026-04-14 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.3 | 2026-04-14 | implemented | Added requirement IDs (DEBUFF-DEF-001..018, DEBUFF-HOOK-001..008) to all debuffs and integration hooks. |
| 1.2 | 2026-04-09 | implemented | Added BoxWipe(99), CrossWipe(100), PencilDrain(101), GoldFine(102). GameEnums extended. RunDirector.ApplyMistakePenalty wired for all new debuffs. ClearBoardRegion helper added. BossService ModifierData entries added. |
| 1.1 | 2026-04-08 | partial | First 5 debuffs implemented: RowWipe(94), ColWipe(95), DoublePenalty(96), CellLock(97), PencilBlind(98). BossModifierId enum extended. RunDirector.ApplyMistakePenalty(row,col) overload added with all hooks. TickLockedCells()/GetLockedCells() on RunDirector. BoardViewController renders CellLocked tint. InRunController: locked cells block input, correct placement calls TickLockedCells. BossService: tier/impact/name/description entries added; debuffs excluded from normal modifier pool. |
| 1.0 | 2026-04-08 | new | Initial brainstorm — no implementation yet |

---

## Overview

This document catalogues negative board-state effects that activate when the player places a **wrong digit** (i.e. a move that triggers `ApplyMistakePenalty`). These are distinct from the existing boss modifiers (fog, antiknight, etc.) which constrain what digits are valid — debuffs *react* to mistakes after they happen.

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
| DEBUFF-DEF-005 | **RadiusWipe** | Shatter Radius | On mistake: clears all player-placed digits within a 2-cell Chebyshev radius of the wrong cell. | Localised area effect. | ❌ |

**Implementation hook:** `SudokuBoard.ClearValue(r, c)` already exists. Wipe logic iterates the board and calls `ClearValue` on non-given cells in the target area.

---

### B — Pencil-mark Pollution
Adds or removes pencil marks to sabotage the player's notes.

| Req ID | Id | Name | Effect | Notes | Status |
|--------|----|------|--------|-------|--------|
| DEBUFF-DEF-006 | **PencilPoison** | Misinformation | On mistake: adds 1–3 random *wrong* pencil marks to each empty cell in the wrong cell's row. | Requires a "fake mark" bit separate from real pencil marks, or just pollutes the existing HashSet. | ❌ |
| DEBUFF-DEF-007 | **PencilBlind** (98) | Fog of Notes | On mistake: removes all pencil marks in the wrong cell's row and column. | Punishes pure pencil-mark solvers. | ✅ |
| DEBUFF-DEF-008 | **PencilScramble** | Scramble Notes | On mistake: replaces all pencil marks board-wide with random values. | Nuclear option — very high intensity only. | ❌ |

**Note:** If pencil pollution uses the existing `_pencilMarks[r,c]` HashSet, distinguishing fake from real marks requires a second board layer. A simpler approach: erase rather than add fake marks.

---

### C — Vision Impairment
Reduces what the player can see.

| Req ID | Id | Name | Effect | Notes | Status |
|--------|----|------|--------|-------|--------|
| DEBUFF-DEF-009 | **FogPulse** | Fog Surge | On mistake: adds a 1-tile fog ring around the wrong cell for 3 moves. | Extends the existing FogOfWar rendering; uses `FogCells` list. | ❌ |
| DEBUFF-DEF-010 | **NumberBlind** | Glare | On mistake: hides all instances of the placed digit on the board for 2 moves (cells exist, digits invisible). | Requires a "blinded digit" field in render state. | ❌ |
| DEBUFF-DEF-011 | **CellBlur** | Ink Blur | On mistake: briefly hides the digit labels of a random 3×3 area (not fog — underlying values unchanged). | Visual only, no board state change. | ❌ |

---

### D — Resource Drain
Compounds existing HP/resource penalties.

| Req ID | Id | Name | Effect | Notes | Status |
|--------|----|------|--------|-------|--------|
| DEBUFF-DEF-012 | **DoublePenalty** (96) | Brittle Ink | Wrong placements deal 2 HP instead of 1. | Simplest debuff; just multiplies `ApplyMistakePenalty` damage. | ✅ |
| DEBUFF-DEF-013 | **PencilDrain** (101) | Ink Spill | Each wrong placement costs 1 extra pencil charge (in addition to HP). | Only relevant if pencil charges are finite. | ✅ |
| DEBUFF-DEF-014 | **GoldFine** (102) | Tax Collector | Deducts 5 gold per mistake (clamped to 0). | Punishes economy runners. | ✅ |
| DEBUFF-DEF-015 | **RevealCost** | Curse of Clarity | The next hint or item use is blocked for 2 moves after a mistake. | | ❌ |

---

### E — Board Mutation
Changes the puzzle state in ways that alter correct solutions.

| Req ID | Id | Name | Effect | Notes | Status |
|--------|----|------|--------|-------|--------|
| DEBUFF-DEF-016 | **DigitShift** | Rolling Numbers | After a mistake: the target cell and 1–2 random empty cells swap their *correct* solution values, requiring different digits than before. | High implementation complexity — requires re-validating the full board solution. Only feasible if solution is stored separately from the live board. | ❌ |
| DEBUFF-DEF-017 | **GivenReveal** | Stone to Ink | After a mistake: permanently converts 1–3 of the player's correct placed digits back to "given" (immovable) status, locking them. | Interesting for end-game pressure. | ❌ |
| DEBUFF-DEF-018 | **CellLock** (97) | Frozen Cell | After a mistake: the wrong cell becomes temporarily un-editable for 3 correct placements. | Simpler than DigitShift; just sets a "locked until move N" state. | ✅ |

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
| DEBUFF-HOOK-002 | DoublePenalty sets damage=2 before HP deduction | `ApplyMistakePenalty` — `damage = 2` branch | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-003 | RowWipe calls `ClearBoardRow(row)` on mistake | `ApplyMistakePenalty` — RowWipe branch | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-004 | ColWipe calls `ClearBoardCol(col)` on mistake | `ApplyMistakePenalty` — ColWipe branch | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-005 | PencilBlind calls `ClearPencilRowCol(row,col)` | `ApplyMistakePenalty` — PencilBlind branch | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-006 | CellLock writes `row*100+col → 3` into `LevelState.LockedCells` | `LockCell(row,col)` → `LockedCells` dict | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-007 | `TickLockedCells()` decrements locked counts; removes entries ≤ 0; called after every correct placement | `RunDirector.TickLockedCells()` | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-008 | `GetLockedCells()` returns `HashSet<(int,int)>` for input blocking and board tinting | `RunDirector.GetLockedCells()` | `Assets/Scripts/Run/RunDirector.cs` | ✅ |
| DEBUFF-HOOK-009 | `LevelState.LockedCells: Dictionary<int,int>` — `[NonSerialized]`, transient per puzzle | `LevelState.LockedCells` | `Assets/Scripts/Core/RuntimeModels.cs` | ✅ |
| DEBUFF-HOOK-010 | `BossModifierId` enum entries 94–102 registered | `BossModifierId` enum | `Assets/Scripts/Core/GameEnums.cs` | ✅ |
| DEBUFF-HOOK-011 | `BossService.ModifierData` has tier/impact/name/description for IDs 94–102; IDs ≥ 94 excluded from `BuildEligiblePool` | `ModifierData` dictionary | `Assets/Scripts/Boss/BossService.cs` | ✅ |
| DEBUFF-HOOK-012 | `BoardViewController.RenderBoard` applies `GamePalette.CellLocked` (deep magenta) tint via `Color.Lerp(..., 0.75f)` for locked cells | `RenderBoard` — `lockedCells` param | `Assets/Scripts/UI/BoardViewController.cs` | ✅ |
| DEBUFF-HOOK-013 | `InRunController` blocks input on locked cells; routes mistake/correct through `ApplyMistakePenalty(row,col)` / `TickLockedCells()` | `InRunController` input handlers | `Assets/Scripts/UI/InRunController.cs` | ✅ |

---

## Remaining Candidates (not yet implemented)

| Req ID | Id | Notes |
|--------|----|-------|
| DEBUFF-DEF-005 | RadiusWipe | Clears all digits within 2-cell Chebyshev radius |
| DEBUFF-DEF-006 | PencilPoison | Adds fake pencil marks (requires second board layer) |
| DEBUFF-DEF-008 | PencilScramble | Scrambles all pencil marks board-wide |
| DEBUFF-DEF-009 | FogPulse | Adds fog ring for 3 moves around wrong cell |
| DEBUFF-DEF-010 | NumberBlind | Hides placed digit board-wide for 2 moves |
| DEBUFF-DEF-011 | CellBlur | Hides digit labels in random 3×3 area briefly |
| DEBUFF-DEF-015 | RevealCost | Blocks next item use for 2 moves |
| DEBUFF-DEF-016 | DigitShift | Swaps solution values between cells (very high complexity) |
| DEBUFF-DEF-017 | GivenReveal | Converts player digits back to givens |

---

## Scope Note

These are **boss-encounter modifiers**, not general run modifiers. They activate only when included in `LevelConfig.ActiveModifiers` (which for boss puzzles includes the player's chosen modifiers).
