# Puzzle Generation Readiness Matrix

## Purpose

This document defines the release-facing guardrails for Sudoku board,
modifier-overlay, and boss puzzle generation. It is a contract for tests and
profiling, not proof that target hardware performance has already been
validated.

## Current Release Stance

- Generation status: guarded for development, not release-profiled.
- Runtime profiling status: needs Unity Profiler validation on target hardware.
- Garden Run modifier uniqueness: recommended, not currently required.
- Competitive modifier uniqueness: required for Spirit Trials with modifiers and
  Monthly Walk/Seasonal Challenge with modifiers.

## Generation Budget Matrix

| Flow | Modifier Count | Uniqueness Required | Settings | Expected Behavior |
| --- | ---: | --- | --- | --- |
| No-modifier puzzle | 0 | No | direct success path | Reuse initial board and empty overlay without overlay seed attempts. |
| Standard async boss | 1-6 | No unless config requests it | `AsyncBossDefault` | Use 3 board retries, 8 overlay seed attempts, 2500 ms budget, parallel overlay seeds. |
| High-complexity async boss | 7+ | No unless config requests it | `HighComplexityBossDefault` | Use 3 board retries, 4 overlay seed attempts, 4000 ms budget, parallel overlay seeds. |
| Structural modifier puzzle | Any with global board-shaping modifier | No unless config requests it | `StructuralModifierDefault` | Use 4 fresh board seed attempts, 6 overlay seed attempts, 6000 ms per board attempt, parallel overlay seeds. |
| Synchronous fallback/start path | Any | Config controlled | `SynchronousDefault` | Use 5 board retries, 12 overlay seed attempts, 4000 ms budget, no parallel overlay seeds. |
| Spirit Trials with modifiers | 1+ | Yes | service settings | Fail generation when modifier-aware uniqueness cannot be verified. |
| Monthly Walk / Seasonal Challenge with modifiers | 1+ | Yes | service settings | Fail generation when modifier-aware uniqueness cannot be verified. |
| Garden Run boss with modifiers | 1+ | No | async boss settings | May accept non-unique modifier puzzles; must preserve metrics and missing-modifier reporting. |

## Release Guardrails

1. `PuzzleGenerationService` must keep explicit metrics for retries, overlay
   attempts, unique-solution attempts, elapsed time, budget exceeded state, and
   last error.
2. High-complexity boss generation starts at seven active modifiers. This keeps
   late-floor modifier stacks from spending extra overlay attempts before the
   first valid overlay is accepted.
3. Global board-shaping modifiers such as Anti-Knight use the structural
   modifier budget because solved-board and uniqueness checks can be slower
   even when only one modifier is active.
4. Competitive configurations must set `LevelConfig.RequireUniqueModifierSolution`
   before calling `PuzzleGenerationService.Generate`.
5. Garden Run does not yet enforce modifier-aware uniqueness. Do not present
   Garden Run boss puzzles as release-proven unique until that rule changes.
6. Missing overlay geometry must remain visible through
   `PuzzleGenerationResult.MissingModifiers` and `RunDirector.LastGenerationMissingModifiers`.
7. Unity Profiler captures are still required for boss gate, high-complexity
   boss generation, Seasonal Challenge generation, and Spirit Trials generation.

## Automated Checks

`Assets/Scripts/Tests/EditMode/PuzzleGenerationServiceTests.cs` verifies:

- documented generation budgets match `PuzzleGenerationSettings`;
- high-complexity async boss selection starts at seven active modifiers;
- structural modifier configs use the structural budget;
- non-boss without structural modifiers and lower-complexity boss configs use the standard async budget;
- competitive modifier modes request the uniqueness gate;
- uniqueness failures preserve explicit failure reasons and metrics.

These tests do not replace runtime profiling. They prevent accidental changes to
the release contract while generation behavior is still being stabilized.
