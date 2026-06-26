# Sudoku Generation Pipeline

**Version:** 1.5 | **Date:** 2026-04-14 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.5 | 2026-04-14 | implemented | Added requirement IDs (GEN-REGION, GEN-SOLVE, GEN-REMOVE, GEN-MOD, GEN-VALID). |
| 1.4 | 2026-03-28 | implemented | Non-deterministic validation: placement is valid if it satisfies all constraints (no conflict in row/col/region/modifier), NOT compared against the single generated solution. PlaceResult.WrongButValid removed; only Correct/Invalid/IsGiven remain. SudokuBoard.IsComplete() and IsCorrectAt() use constraint satisfaction. Solution property retained for item effects only. |
| 1.3 | 2026-03-23 | implemented | IsUniqueSolution property added to SudokuBoard |
| 1.2 | 2026-03-22 | implemented | OPEN-A (uniqueness checker) and OPEN-B (irregular templates) both verified present in SudokuGenerator |
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

**[GEN-REGION-001]** `SudokuGenerator.BuildRegionMap(int size, int variant)` produces a 2D integer array mapping each cell `(row, col)` to its region ID.

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

**[GEN-REGION-002]** All templates guarantee:
- Each region contains exactly `size` cells
- All regions are contiguous (orthogonally connected)
- Region count equals board size

### Run-Level Variant Selection

**[GEN-REGION-003]** `RunDirector.BuildLevelConfig` selects the variant:
- If `RunState.AllowIrregularPuzzles = false` → restricts to variants 0–1
- If `AllowIrregularPuzzles = true` → variants 0–3 are all eligible
- Tutorial dropdown maps: index 0 → variant 0, index 1 → variant 1, index 2 → variant 3

---

## Stage 2 — Solved Board Generation

**[GEN-SOLVE-001]** `SudokuGenerator.GenerateSolvedBoard(size, regionMap, random)` produces a fully solved grid using constraint-propagation backtracking.

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

**[GEN-REMOVE-001]** After generating the solved board, cells are removed to create the player-facing puzzle.

### Density Formula

**[GEN-REMOVE-002]** `StarDensityService.MissingPercentForStars(int stars)`:

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

**[GEN-REMOVE-003]** 7★ removes **all cells** (0 givens). Only available in tutorial mode; **requires at least one modifier to be active**. Without modifier constraints, a completely empty grid has no solving entry point. The tutorial UI enforces this: selecting 7★ without at least one modifier toggle enabled shows a warning and blocks puzzle start.

**[GEN-REMOVE-004]** For 7★, the removal clamp is bypassed — `removeCount = totalCells` (no minimum given requirement).

### Removal Algorithm

1. Calculate `removeCount = Math.Clamp(Round(totalCells × missingPercent), 1, totalCells - 1)` (or `totalCells` for 7★ tutorial)
2. Generate a shuffled list of all cell indices (using seeded Random)
3. Remove cells in shuffled order up to `removeCount`

### Star Difficulty Clamp Fix (JIRA-0019 R4)

The upper clamp bound was changed from `totalCells - size` to `totalCells - 1`. This allows 9×9 6★ to remove 73 cells (leaving 8 givens), which was previously impossible.

---

## Stage 4 — Modifier Geometry Generation

**[GEN-MOD-001]** `ModifierGeometryGenerator.Generate(board, modifiers, seed, intensity)` creates overlay geometry from the solved board for each active boss modifier.

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

**[GEN-VALID-001]** During gameplay, placements are validated against the generated constraints.

### Validation Path

**[GEN-VALID-002]** Validation path:
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
- Spirit Trials and Monthly Walk validate modifier-aware uniqueness after overlay generation when active modifiers are present
- Garden Run, bosses, and tutorial modifier puzzles do not yet require this gate

**Current competitive gate:** `LevelConfig.RequireUniqueModifierSolution` causes
`PuzzleGenerationService` to run the backtracking solver on the puzzle grid with
the generated overlay and all active modifier rules. If uniqueness cannot be
verified within the existing retry/budget window, generation fails instead of
silently accepting the ambiguous competitive puzzle.

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
Modifiers are intended to constrain the solution space toward a single valid solution. A post-removal uniqueness validation step is now required for competitive modifier puzzles and remains recommended for Garden Run.

---

## Post-Removal Uniqueness Checker

**Priority:** Implemented for Spirit Trials and Monthly Walk; Medium (Garden Run)
**Blocker for:** Competitive integrity (Spirit Trials), puzzle quality

The pipeline shall validate that the puzzle + active modifier constraints produce exactly one valid solution.

**Implementation requirements:**
1. Run the backtracking solver on the puzzle grid (not the solution) with all active `IOrderedConstraintRule` instances registered in the `SudokuConstraintEngine`
2. If the solver finds more than one completion, retry board/overlay generation within the configured budget
3. Competitive configurations fail generation when uniqueness cannot be verified
4. Entry point: `PuzzleGenerationService.Generate()` when `LevelConfig.RequireUniqueModifierSolution` is true
5. Internal methods: `HasUniqueSolution`, `CountSolutions`, `CountCandidates`, `IsValidPlacement`
6. All active `IOrderedConstraintRule` instances are checked via `IsValidPlacement` during the solve

**Spirit Trials requirement:** Spirit Trials modifier puzzles **must** use the uniqueness-checked path. `PuzzleGenerationResult.UniqueModifierSolutionVerified` and `PuzzleGenerationMetrics.UniqueSolutionVerified` record the successful gate.

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



---

## Requirements Traceability

<!-- AUTO-GENERATED by SPICE pipeline. Do not edit manually. -->

| REQ-ID | Title | Linked Systems |
|--------|-------|----------------|
| REQ-SUDOKU-076 | TITLE | LINKED_SYSTEMS |
| REQ-SUDOKU-077 | SudokuGenerationPipeline | RunDirector, SudokuGenerator |
| REQ-SUDOKU-078 | BuildRegionMap | SudokuGenerator |
| REQ-SUDOKU-079 | GenerateSolvedBoard | SudokuGenerator |
| REQ-SUDOKU-080 | RemoveCells | StarDensityService, SudokuGenerator |
| REQ-SUDOKU-081 | ApplyModifiers | BossMechanicsSystem, ModifierGeometryGenerator |
| REQ-SUDOKU-082 | SeedFunctionality | SudokuGenerator |
| REQ-SUDOKU-083 | VariantSelectionLogic | RunDirector, SudokuGenerator |
| REQ-SUDOKU-084 | RegionMapConstructionDetails | SudokuGenerator |
| REQ-SUDOKU-085 | CellRemovalDetails | StarDensityService, SudokuGenerator |
| REQ-SUDOKU-086 | ModifierGeometryGenerationDetails | BossMechanicsSystem, ModifierGeometryGenerator |
| REQ-SUDOKU-087 | IrregularTemplateExpansion | SudokuGenerator |
| REQ-SUDOKU-088 | UniquenessChecker | SudokuGenerator |
| REQ-SUDOKU-089 | JigsawTemplatesDetails | SudokuGenerator |
| REQ-SUDOKU-090 | SeedFunctionalityExplanation | SudokuGenerator |
| REQ-SUDOKU-091 | IntensityScalingDetails | BossMechanicsSystem, StarDensityService |
| REQ-SUDOKU-092 | VariantSelectionExplanation | RunDirector, SudokuGenerator |
| REQ-SUDOKU-093 | CellRemovalExplanation | SudokuGenerator |
| REQ-SUDOKU-094 | ModifierGeometryGenerationExplanation | BossMechanicsSystem, ModifierGeometryGenerator |
| REQ-SUDOKU-095 | IrregularTemplateExpansionExplanation | SudokuGenerator |
| REQ-SUDOKU-096 | UniquenessCheckerExplanation | SudokuGenerator |
| REQ-SUDOKU-097 | JigsawTemplatesUsageExplanation | SudokuGenerator |
| REQ-SUDOKU-098 | PipelineStagesPrioritization | SudokuGenerator |
| REQ-SUDOKU-099 | VariantRoutingExplanation | RunDirector, SudokuGenerator |
| REQ-SUDOKU-100 | CellRemovalStarDifficultyClampFixExplanation | SudokuGenerator |
| REQ-SUDOKU-101 | ModifierGeometryGenerationPerformance | BossMechanicsSystem, ModifierGeometryGenerator |
| REQ-SUDOKU-102 | SeedFunctionalityComparison | SudokuGenerator |
| REQ-SUDOKU-103 | UniquenessCheckerPerformance | SudokuGenerator |
| REQ-SUDOKU-104 | JigsawTemplatesUsageComparison | SudokuGenerator |
| REQ-SUDOKU-105 | IntensityScalingPerformance | BossMechanicsSystem, StarDensityService |
| REQ-SUDOKU-106 | VariantSelectionComparison | RunDirector, SudokuGenerator |
| REQ-SUDOKU-107 | CellRemovalComparison | SudokuGenerator |
| REQ-SUDOKU-108 | ModifierGeometryGenerationComparison | BossMechanicsSystem, ModifierGeometryGenerator |
| REQ-SUDOKU-109 | IrregularTemplateExpansionPerformance | SudokuGenerator |
| REQ-SUDOKU-110 | UniquenessCheckerComparison | SudokuGenerator |
| REQ-SUDOKU-111 | JigsawTemplatesUsagePerformance | SudokuGenerator |
| REQ-SUDOKU-112 | PipelineStagesPrioritizationExplanation | SudokuGenerator |
| REQ-SUDOKU-113 | CellRemovalStarDifficultyClampFixComparison | SudokuGenerator |
| REQ-SUDOKU-114 | VariantSelectionImpactExplanation | RunDirector, SudokuGenerator |
| REQ-SUDOKU-115 | ModifierGeometryGenerationImpactExplanation | BossMechanicsSystem, ModifierGeometryGenerator |
| REQ-SUDOKU-116 | IrregularTemplateExpansionImpactExplanation | SudokuGenerator |
| REQ-SUDOKU-117 | UniquenessCheckerImpactExplanation | SudokuGenerator |
