# Garden of Numbers — Core Systems Hardening

This document formalizes the implemented hardening layer for deterministic puzzle quality, fair difficulty, emotional game feel, and robust save compatibility.

## 1) Core Sudoku Engine Depth

## Deterministic Generation + Validation

Implemented pipeline in `SudokuGenerationService`:
1. Build solved board (deterministic seed)
2. Remove cells incrementally (constructive removal)
3. After each removal:
   - Validate uniqueness via backtracking solution count
   - Validate logical solvability via logical analyzer
   - Validate target difficulty tier gate
4. Stop at target star density once quality gates still pass

Files:
- `Assets/Scripts/Sudoku/SudokuGenerationService.cs`
- `Assets/Scripts/Sudoku/SudokuBacktrackingSolver.cs`
- `Assets/Scripts/Sudoku/SudokuLogicalAnalyzer.cs`
- `Assets/Scripts/Sudoku/SudokuDifficultyGrader.cs`

## Solvability + Uniqueness Requirements

- Exactly one solution required (`CountSolutions == 1`)
- Logical solver validation required by default
- Brute-force dependency only allowed when explicitly enabled (`AllowBruteForceOnly`, intended for chaos boss variants)

## Logical-Only Tiering

Technique model defined:
- Tier 1: Naked Single, Hidden Single
- Tier 2: Naked Pair, Pointing Pair
- Tier 3: Box-Line Reduction, Naked Triples
- Tier 4: X-Wing, Swordfish

Current analyzer executes deterministic Tier 1 human-style passes and records step metadata; tier model and scoring are fully extensible for adding higher-tier detectors.

## Difficulty Grading Algorithm

`DifficultyScore = Σ(TechniqueWeight × Count) + DependencyDepth + ModifierComplexityWeight`

Weights include:
- Naked Single = 1
- Hidden Single = 1.5
- Naked Pair = 3
- X-Wing = 8
- Swordfish = 12

Puzzle metadata stored as:
- Step count
- Highest technique used
- Dependency depth estimate
- Modifier complexity weight
- Difficulty score

Model type:
- `PuzzleAnalysis` in `Assets/Scripts/Core/HardeningModels.cs`

## 2) Emotional Game Feel Layer

Implemented via `RunFeelService`:
- Correct streak tracking (`5/10/20` combo thresholds)
- Combo gold bonus hooks
- Near-death state (`HP <= 2`) and music layer escalation
- Perfect solve eligibility (`Clear Mind`) tracking
- Solver-item usage tracking for perfect-clear disqualification

Integrated into run loop:
- On correct placement: streak and focus escalation
- On mistake: streak reset + tension escalation
- On level complete: optional `+10% XP` Clear Mind bonus

Files:
- `Assets/Scripts/Run/RunFeelService.cs`
- `Assets/Scripts/Run/RunDirector.cs`

## 3) Long-Term Meta Goals

Implemented profile-side mastery/completion systems:
- Boss clears per modifier
- Perfect boss clears
- 9x9 5★ style milestone counters
- Dual-modifier clears
- No-item style run counter
- Global completion % recalculation

Files:
- `Assets/Scripts/Meta/MasteryService.cs`
- `Assets/Scripts/Meta/CompletionService.cs`
- `Assets/Scripts/Save/ProfileService.cs`

Modifier mastery badge tier enum available:
- None / Bronze / Silver / Gold / Spirit

## 4) UX Edge Case Decisions

Implemented decisions:
- Restart level disallowed in normal runs; tutorial-only restart policy
- Optional accidental-input protection setting includes:
  - Confirm before wrong placement
  - Double-tap confirm number entry
- Undo beyond pencil erase remains unsupported (no undo stack introduced)

Files:
- `Assets/Scripts/UI/PauseMenuService.cs`
- `Assets/Scripts/Core/RuntimeModels.cs`

## 5) Save During Puzzle + Versioned Save Architecture

Implemented save envelope model:

```
SaveFileEnvelope {
  SaveVersion,
  PlayerProfile,
  MetaProgress,
  ActiveRunState,
  TutorialProgress,
  Statistics,
  Mastery,
  Completion
}
```

Separate save paths:
- Profile save file
- Run save file

Version strategy:
- Save version string on each file
- Minor mismatch => migration + backup
- Major mismatch => safe fail + backup
- Migration hook service for future version steps

Files:
- `Assets/Scripts/Save/SaveFileService.cs`
- `Assets/Scripts/Save/SaveMigrationService.cs`
- `Assets/Scripts/Core/HardeningModels.cs`

## Auto-Save Triggers

`RunSaveTrigger` enum includes:
- Pause
- Quit
- Boss phase transition
- Manual checkpoint

`RunDirector` exposes `AutoSaveRequested` event + `RequestAutoSave(trigger)`.
`RunAutoSaveCoordinator` persists run state deterministically.

Files:
- `Assets/Scripts/Run/RunDirector.cs`
- `Assets/Scripts/Save/RunAutoSaveCoordinator.cs`

## 6) Deterministic Modifier Injection Order (Design Contract)

The rule order contract is formalized for deterministic behavior:
1. Base Sudoku rules
2. Region constraints
3. Line-based modifiers
4. Dot-based modifiers
5. Arithmetic constraints
6. Fog overlay post-process

`SudokuConstraintEngine` remains the central injection point and should register rules in this fixed order.

## 7) Notes on Extension Work

Already prepared for extension without refactor:
- Add Tier 2–4 logical detectors to `SudokuLogicalAnalyzer`
- Expand boss chaos mode by toggling `AllowBruteForceOnly`
- Bind audio/visual responses to `RunFeelState` for final polish
- Add explicit migration steps per version in `SaveMigrationService`
