---
name: sudoku-generation
description: Work on Sudoku puzzle generation, region maps/jigsaw templates, star density/clue removal, boss modifier geometry, constraint rules, solver/uniqueness, and seeded determinism. Use when touching Assets/Scripts/Sudoku/, StarDensityService, modifier/constraint code, or debugging unsolvable/ambiguous/wrong puzzles.
---

# Sudoku Generation

Spec: `docs/SudokuGenerationPipeline.md` (GEN-* IDs). Boss modifiers: `docs/BossMechanicsSystem.md` (BOSS-*). Debuffs: `docs/BossDebuffEffects.md` (DEBUFF-*). Pressure: `docs/PressureMechanicsSpec.md` (PRESSURE-*).

## Pipeline

1. **Region map**: `SudokuGenerator.BuildRegionMap(size, variant)`. Variants: 0 = standard rectangular, 1 = alt rectangular, 2 = first irregular template, 3 = jigsaw / Irregular B. The tutorial dropdown maps index 2 → variant 3. `RunState.AllowIrregularPuzzles == false` restricts runs to variants 0–1 (`RunDirector.BuildLevelConfig`).
2. **Solved board**: `SudokuGenerator.GenerateSolvedBoard` / `FillBoard`, backtracking with the MRV heuristic (`FindNextCell`).
3. **Clue removal**: star density from `Core/StarDensityService.cs`, formula `(stars + 3) × 0.1` clamped to 1–6★ (6★ = 90%). 7★ is tutorial-only and requires modifiers. The removal clamp upper bound is `totalCells − 1`.
4. **Modifier geometry**: `ModifierGeometryGenerator.Generate(board, modifiers, seed, intensity)` → `ModifierOverlayData` (lines, arrows, cages, Kropki dots, `CellMarkers`). `BossModifierIntensity` Low ×0.5 / Medium ×1.0 / High ×1.5 / VeryHigh ×2.0. Nonconsecutive and Antiknight have no geometry.
5. **Orchestration and retries**: `PuzzleGenerationService.Generate(config, board, seed, boardFactory, settings, overlayFactory)` returns success, overlay completeness, `MissingModifiers`, and retry metrics.

Validation: `SudokuConstraintEngine.ValidateAll()` runs `IOrderedConstraintRule`s from `ModifierFactory` in category order (`ConstraintRuleCategory`: BaseSudoku 0, Region 1, GlobalNegative 2, Line 3, Dot 4, Arithmetic 5, CellLevel 6, FogPostProcess 7).

## Invariants to keep

- **Geometry must agree with the stored solution.** Every overlay element (sum, dot, parity marker, line rule) must be satisfied by the solved board. A modifier the solution violates makes the puzzle unwinnable.
- **Uniqueness policy**: base-rules-only puzzles do *not* guarantee uniqueness, and the player is checked against the stored solution. `LevelConfig.RequireUniqueModifierSolution` (Spirit Trials, Monthly Walk) makes the backtracking solver verify uniqueness with all modifier rules, failing generation rather than accepting ambiguity. Don't silently change which modes require uniqueness. That is a spec decision.
- **Board-size gating**: German Whispers and Killer Cages require 7×7+ (enforced in `BossService` `ModifierData.minBoardSize`). Debuff IDs ≥ 94 and pressure IDs 103–106 are excluded from the normal modifier pool.
- **Determinism**: each puzzle derives its `System.Random` from run seed + level index. Region map, solution, removal order, and geometry all use that RNG. Adding or reordering RNG draws changes every downstream puzzle for existing seeds and breaks save/resume regeneration, so don't do it without a spec change.
- **Chosen boss modifier**: `RunMapController.GetFixedLevelConfig` caches node configs. The player's `ChosenBossModifier` is applied to the returned clone at call time and must never be written into the cached config.

## Testing generation changes

Use the `unity-testing` skill. For any change here, cover:
- every board size 5×5–9×9 × every region variant generates a valid solved board (`SudokuValidator`);
- clue count per star level matches the density formula (incl. 6★ on 9×9 → 8 givens);
- each affected modifier's overlay is satisfied by the solution and rejects a known-bad placement;
- modifier combinations that can co-occur in a boss phase still generate within the retry budget;
- same seed → identical board + overlay; different seed → different;
- uniqueness-gated modes fail cleanly instead of returning an ambiguous puzzle.

Existing references: `PuzzleGenerationServiceTests`, `SudokuBacktrackingSolverTests`, `ConstraintRuleTests`. Performance budgets: `docs/performance-generation-budget.md`.
