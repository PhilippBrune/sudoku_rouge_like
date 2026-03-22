# Sudoku Generation Pipeline

**Version:** 1.1 | **Date:** 2026-03-22 | **Status:** under review

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.1 | 2026-03-22 | under review | Integrated OPEN-A (uniqueness checker) and OPEN-B (irregular template expansion) as requirements |
| 1.0 | 2026-03-21 | implemented | Initial specification |

---

## Overview

The puzzle generation pipeline produces complete, playable Sudoku boards for all game modes. It takes a board size, star difficulty, region variant, and seed as inputs and outputs a `SudokuBoard` containing both the solved grid and the player-facing puzzle with cells removed.

All generation is **seeded** — identical inputs produce identical puzzles, enabling seed-based run sharing and deterministic save/resume.

---

## Pipeline Stages

```
Input (size, stars, seed, regionVariant)
    │
    ▼
┌─────────────────────────┐
│ 1. Build Region Map     │ ← Variant 0-3 selects rectangular or jigsaw template
└──────────┬──────────────┘
           ▼
┌─────────────────────────┐
│ 2. Generate Solved Board│ ← Backtracking solver with MRV heuristic
└──────────┬──────────────┘
           ▼
┌─────────────────────────┐
│ 3. Remove Cells         │ ← Star density formula determines removal count
└──────────┬──────────────┘
           ▼
┌─────────────────────────┐
│ 4. Apply Modifiers      │ ← ModifierGeometryGenerator produces overlay data
└──────────┬──────────────┘
           ▼
Output: SudokuBoard + ModifierOverlayData
```

---

## Stage 1 — Region Map Construction

`SudokuGenerator.BuildRegionMap(int size, int variant)` produces a 2D integer array mapping each cell `(row, col)` to its region ID.

### Variant Routing

| Variant | Name | Strategy |
|:---:|------|----------|
| 0 | Standard | Rectangular regions (e.g. 3×3 boxes for 9×9) |
| 1 | Alternate | Rotated rectangular dimensions (e.g. 3×2 → 2×3 for 6×6) |
| 2 | Irregular A | First hardcoded jigsaw template per size |
| 3 | Irregular B (Jigsaw) | Second hardcoded jigsaw template per size |

### Size-Specific Behaviour

| Size | Variant 0 | Variant 1 | Variant 2 | Variant 3 |
|:---:|-----------|-----------|-----------|-----------|
| 5×5 | Template (even) | Template (odd) | Template (even) | Template (odd) |
| 6×6 | Rect 2×3 | Rect 3×2 | Get6x6Template | Get6x6TemplateB |
| 7×7 | Template (horizontal wave) | Template (vertical wave) | Template (horizontal) | Template (vertical) |
| 8×8 | Rect 2×4 | Rect 4×2 | Get8x8Template | Get8x8TemplateB |
| 9×9 | Rect 3×3 | Rect 3×3 | Get9x9Template | Get9x9TemplateB |

- **5×5 and 7×7** have no rectangular region option — they always use templates because no integer box dimensions evenly partition these sizes.
- **Fallback** for other sizes: if size is a perfect square, uses `√size × √size` boxes. Otherwise, diagonal mapping `(row + col) % size`.

### Jigsaw Templates

Each template is a hardcoded 2D array defining contiguous region shapes. Templates come in mirrored pairs (A and B) for visual variety:

- `Get5x5Template(variant)` — 5 regions of 5 cells
- `Get6x6Template()` / `Get6x6TemplateB()` — 6 regions of 6 cells
- `Get7x7Template(variant)` — 7 regions (horizontal/vertical wave bands)
- `Get8x8Template()` / `Get8x8TemplateB()` — 8 regions of 8 cells (staircase pattern)
- `Get9x9Template()` / `Get9x9TemplateB()` — 9 regions of 9 cells (irregular, mirrored)

All templates guarantee:
- Each region contains exactly `size` cells
- All regions are contiguous (orthogonally connected)
- Region count equals board size

### Run-Level Variant Selection

`RunDirector.BuildLevelConfig` selects the variant:
- If `RunState.AllowIrregularPuzzles = false` → restricts to variants 0–1
- If `AllowIrregularPuzzles = true` → variants 0–3 are all eligible
- Tutorial dropdown maps: index 0 → variant 0, index 1 → variant 1, index 2 → variant 3

---

## Stage 2 — Solved Board Generation

`SudokuGenerator.GenerateSolvedBoard(size, regionMap, random)` produces a fully solved grid using constraint-propagation backtracking.

### Algorithm

1. **Initialize bitmask arrays** for rows, columns, and regions. Each mask is an integer where bit `(1 << value)` indicates the value is used.
2. **Call `FillBoard()` recursively:**
   a. `FindNextCell()` selects the empty cell with the **fewest valid candidates** (Minimum Remaining Values / MRV heuristic).
   b. Build candidate list by checking which values are not in `rowMask[row] | colMask[col] | regionMask[region]`.
   c. **Shuffle** the candidate list (Fisher-Yates) using the seeded `Random` for variety.
   d. Try each candidate: set the value, update bitmasks, recurse.
   e. If recursion fails, **backtrack**: clear the cell, undo bitmask changes, try next candidate.
   f. If no candidates work, return `false` (triggers backtrack in parent call).
3. Return `true` when all cells are filled.

### MRV Heuristic Details

`FindNextCell()` scans all empty cells and selects the one with the minimum candidate count:
- If a cell has **0 candidates** → inconsistency detected, early return triggers backtrack
- If a cell has **1 candidate** → immediate selection (forced move)
- Ties broken by scan order (top-left to bottom-right)

This heuristic dramatically reduces the search space compared to naive left-to-right filling.

### Performance

- Typical 9×9 generation: < 5ms
- Worst case (irregular regions with poor constraint propagation): < 50ms
- The seeded Random ensures identical boards for identical inputs

---

## Stage 3 — Cell Removal (Star Density)

After generating the solved board, cells are removed to create the player-facing puzzle.

### Density Formula

`StarDensityService.MissingPercentForStars(int stars)`:

```
MissingPercent = (stars + 3) × 0.1
```

Clamped to `[0.01, 0.95]` for stars 1–6. 7★ is a special case (tutorial only).

| Stars | Missing % | 5×5 Removed | 5×5 Givens | 9×9 Removed | 9×9 Givens |
|:---:|:---:|:---:|:---:|:---:|:---:|
| 1★ | 40% | 10 | 15 | 32 | 49 |
| 2★ | 50% | 12 | 13 | 40 | 41 |
| 3★ | 60% | 15 | 10 | 48 | 33 |
| 4★ | 70% | 17 | 8 | 56 | 25 |
| 5★ | 80% | 20 | 5 | 64 | 17 |
| 6★ | 90% | 22 | 3 | 73 | 8 |
| 7★ | 100% | 25 | 0 | 81 | 0 |

### 7★ — Tutorial Only

7★ removes **all cells** (0 givens). This is only available in tutorial mode and **requires at least one modifier to be active**. Without modifier constraints, a completely empty grid has no solving entry point. The tutorial UI enforces this: selecting 7★ without at least one modifier toggle enabled shows a warning and blocks puzzle start.

For 7★, the removal clamp is bypassed — `removeCount = totalCells` (no minimum given requirement).

### Removal Algorithm

1. Calculate `removeCount = Math.Clamp(Round(totalCells × missingPercent), 1, totalCells - 1)` (or `totalCells` for 7★ tutorial)
2. Generate a shuffled list of all cell indices (using seeded Random)
3. Remove cells in shuffled order up to `removeCount`

### Star Difficulty Clamp Fix (JIRA-0019 R4)

The upper clamp bound was changed from `totalCells - size` to `totalCells - 1`. This allows 9×9 6★ to remove 73 cells (leaving 8 givens), which was previously impossible.

---

## Stage 4 — Modifier Geometry Generation

`ModifierGeometryGenerator.Generate(board, modifiers, seed, intensity)` creates overlay geometry from the solved board for each active boss modifier.

See [BossMechanicsSystem](BossMechanicsSystem.md) for the full modifier table, geometry strategies, and overlay rendering details.

### Key Parameters

- **board:** The solved `int[,]` grid (geometry is generated against the solution, not the puzzle)
- **modifiers:** List of active `BossModifierId` values
- **seed:** Seeded Random for deterministic geometry
- **intensity:** `BossModifierIntensity` enum that scales target element counts

### Intensity Scaling

| Intensity | Multiplier | Run Numbers |
|-----------|:---:|:---:|
| Low | ×0.5 | 1–2 |
| Medium | ×1.0 | 3–5 |
| High | ×1.5 | 6–8 |
| VeryHigh | ×2.0 | 9+ |

The Crimson Fan relic reduces intensity by one step.

### Output: ModifierOverlayData

```
Lines          → List<ModifierLine>       (whispers, parity, renban, palindrome, thermo, between)
Arrows         → List<ArrowConstraint>    (circle + path cells)
Cages          → List<KillerCage>         (cell list + sum)
Dots           → List<KropkiDot>          (cell pair + dot type)
CellMarkers    → List<CellMarker>         (even/odd markers)
FogCells       → HashSet<long>            (packed row/col coordinates)
```

---

## Constraint Validation

During gameplay, placements are validated against the generated constraints.

### Validation Path

1. `RunDirector.PlaceNumber()` checks `Solution[row,col] == value` (correctness against the solved board)
2. `SudokuValidator.IsMoveValid()` optionally accepts a `SudokuConstraintEngine` for modifier-aware pencil mark validation
3. `SudokuConstraintEngine.ValidateAll()` iterates all registered `IOrderedConstraintRule` implementations in category order

### Constraint Rule Categories (Execution Order)

| Order | Category | Modifiers |
|:---:|----------|-----------|
| 0 | BaseSudoku | Row / Column / Region uniqueness (always active) |
| 1 | Region | (reserved for future region-based modifiers) |
| 2 | GlobalNegative | Antiknight, Nonconsecutive |
| 3 | Line | German Whispers, Dutch Whispers, Parity, Renban, Palindrome, Thermo, Between Lines |
| 4 | Dot | Difference Kropki, Ratio Kropki |
| 5 | Arithmetic | Killer Cages, Arrow Sums |
| 6 | CellLevel | Even Odd |
| 7 | FogPostProcess | Fog of War (visibility only; does not restrict placement) |

### Solvability & Uniqueness Policy

The pipeline targets **unique-solution puzzles** where the combination of base Sudoku rules and active modifiers constrains the board to exactly one valid solution.

**Without modifiers (base Sudoku only):**
- Cell removal is random and does not guarantee a unique solution
- The player solves against the stored solution — if multiple solutions exist, any placement matching the stored solution is accepted
- Items and relics compensate for ambiguous states

**With modifiers (boss puzzles, tutorial with modifiers):**
- Modifiers add constraints that restrict the solution space
- The combined constraint set (base rules + modifier rules) should ideally produce a unique solution
- The generator does not currently validate uniqueness post-removal, but the modifier geometry is generated from the solved board, ensuring all constraints are consistent with at least one valid solution

**Future enhancement:** A post-removal uniqueness checker that verifies exactly one solution exists given all active constraints. This would run the backtracking solver on the puzzle (not the solution) with all modifier rules active and confirm it finds exactly one completion. Priority: high for Spirit Trials (competitive integrity), medium for Garden Run.

---

## Board Size Restrictions

Certain modifiers have minimum board size requirements enforced during modifier pool rolling:

| Modifier | Minimum Size | Reason |
|----------|:---:|--------|
| German Whispers | 7×7 | Requires digit difference ≥ 5; impossible on small digit ranges |
| Killer Cages | 7×7 | Cage sum constraints need sufficient digit variety |

All other modifiers work on all board sizes (5×5 through 9×9).

---

## Seeding & Determinism

The entire pipeline is deterministic given the same seed:

1. Seed is set at run start (random or player-entered)
2. Each puzzle derives its own `Random` instance from the run seed + level index
3. Region map, solved board, cell removal order, and modifier geometry all use this seeded Random
4. This enables:
   - **Seed sharing:** Players can share run seeds to replay identical puzzles
   - **Save/resume:** Regenerating a puzzle from seed produces the same board
   - **Debugging:** Reproducible puzzle states for bug reports

---

## Source Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Sudoku/SudokuGenerator.cs` | `CreatePuzzle()`, `BuildRegionMap()`, `GenerateSolvedBoard()`, `FillBoard()`, `FindNextCell()`, all jigsaw templates |
| `Assets/Scripts/Core/StarDensityService.cs` | `MissingPercentForStars()`, star → density formula |
| `Assets/Scripts/Sudoku/ModifierGeometryGenerator.cs` | `Generate()`, geometry creation for all 15 modifiers |
| `Assets/Scripts/Sudoku/ModifierFactory.cs` | Maps `BossModifierId` → `IOrderedConstraintRule` instances |
| `Assets/Scripts/Sudoku/ConstraintRules.cs` | 15 `IOrderedConstraintRule` implementations |
| `Assets/Scripts/Sudoku/ModifierModels.cs` | Data structures: `ModifierLine`, `ArrowConstraint`, `KillerCage`, `KropkiDot`, `CellMarker`, `ModifierOverlayData` |
| `Assets/Scripts/Sudoku/SudokuConstraintEngine.cs` | Central rule engine with `ValidateAll()` |
| `Assets/Scripts/Run/RunDirector.cs` | `BuildLevelConfig()` — variant selection, intensity assignment, `StartLevel()` wiring |
| `Assets/Scripts/Core/GameEnums.cs` | `BossModifierId`, `BossModifierIntensity`, `ConstraintRuleCategory` enums |

---

## Resolved Design Decisions

### 7★ Requires Modifiers
7★ (100% missing) is tutorial-only and **requires at least one modifier** to be active. The UI blocks puzzle start without a modifier toggle enabled. This ensures the player has constraint information to work with on an empty grid.

### Uniqueness via Modifiers
Modifiers are intended to constrain the solution space toward a single valid solution. A post-removal uniqueness validation step is required (see REQ-UNIQUE below).

---

## Post-Removal Uniqueness Checker

**Priority:** High (Spirit Trials), Medium (Garden Run)
**Blocker for:** Competitive integrity (Spirit Trials), puzzle quality

The pipeline shall validate that the puzzle + active modifier constraints produce exactly one valid solution.

**Implementation requirements:**
1. Run the backtracking solver on the puzzle grid (not the solution) with all active `IOrderedConstraintRule` instances registered in the `SudokuConstraintEngine`
2. If the solver finds more than one completion, remove additional cells or regenerate with a shifted seed
3. Retry up to `maxAttempts` before falling back to the unvalidated puzzle
4. Entry point: `SudokuGenerator.CreatePuzzleWithUniquenessCheck(size, missingPercent, seed, regionVariant, constraintEngine, maxAttempts)`
5. Internal methods: `HasUniqueSolution`, `CountSolutions`, `CountCandidates`, `IsValidPlacement`
6. All active `IOrderedConstraintRule` instances are checked via `IsValidPlacement` during the solve

**Spirit Trials requirement:** All Spirit Trials puzzles **must** use the uniqueness-checked path. A puzzle that passes uniqueness validation gets a `IsUniqueSolution = true` flag on the board, which the anti-cheat system can verify.

---

## Irregular Template Expansion

**Priority:** Low
**Blocker for:** Puzzle variety in long play sessions (80+ runs)

Currently each board size has 2 jigsaw templates (A and B, variants 2–3). For long-term variety, the system shall support 4+ templates per size (variants 4, 5, ...).

**Requirements:**
1. The architecture already supports this — `BuildRegionMap` routes by variant index
2. New templates shall maintain the constraints: each region has exactly `size` cells, all regions contiguous, region count equals board size
3. Templates are hardcoded `int[,]` arrays in `SudokuGenerator.cs`, one method per size per variant (e.g., `Get6x6TemplateC()`)
4. `RunDirector.BuildLevelConfig` variant selection range must expand to include new variant indices when templates are added
5. Consider procedural jigsaw generation as an alternative to hardcoded templates for future scalability

