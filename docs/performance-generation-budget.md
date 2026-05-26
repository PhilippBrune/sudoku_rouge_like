# Puzzle Generation Budget

## Runtime Path

Boss and modifier-heavy level generation now routes through:

```text
Assets/Scripts/Sudoku/PuzzleGenerationService.cs
```

`RunDirector` still owns run state, board application, constraint engine setup,
relic/curse hooks, and pressure mechanics. The generation service owns board
retry count, overlay seed attempts, elapsed-time metrics, missing modifier
reporting, cancellation support, and structured failure reasons.

## Default Budgets

Async boss generation:

- Board retries: 3
- Overlay seed attempts per board: 8
- Budget: 2500 ms
- Overlay seed attempts: parallel

Structural modifier generation:

- Board retries: 4 fresh board seeds
- Overlay seed attempts per board: 6
- Budget: 6000 ms per board seed attempt
- Overlay seed attempts: parallel
- Used for global board-shaping modifiers such as Anti-Knight, where a single
  bad seed can exhaust the standard async budget.

Synchronous level generation:

- Board retries: 5
- Overlay seed attempts per board: 12
- Budget: 4000 ms
- Overlay seed attempts: sequential

These are guardrails, not final performance targets. Final values must be tuned
with Unity Profiler captures on target hardware.

## Failure Behavior

Async boss generation no longer falls back to synchronous generation on the main
thread. If board generation fails or the background task faults, `RunDirector`
leaves the puzzle unloaded and the UI returns to the path with a localized
failure message.

If overlay generation succeeds only partially, the level can still start; any
active modifier without generated overlay geometry is dropped before constraint
rules are registered.

## Metrics To Inspect

`RunDirector.LastGenerationMetrics` exposes:

- `BoardRetriesAttempted`
- `OverlaySeedAttempts`
- `BoardGenerationFailures`
- `OverlayGenerationFailures`
- `ElapsedMilliseconds`
- `BudgetExceeded`
- `LastError`

`RunDirector.LastGenerationMissingModifiers` exposes modifiers that could not
produce overlay geometry for the selected board/seed attempts.

## Profiler Scenarios

Capture these before changing budgets:

1. Enter first boss with one simple modifier.
2. Enter late boss with three or more modifiers.
3. Enter cursed node with an extra modifier.
4. Enter custom/tutorial puzzle with many selected modifiers.
5. Repeat with German language and 150% font scale to include UI rebuild cost.

Record profiler results in a dated note before increasing retry counts.
