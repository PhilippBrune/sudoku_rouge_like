# TC-GENERAL-040: Verify depth progression after solving a puzzle

- **Requirement:** REQ<｜begin▁of▁sentence｜>055
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 12:39:53

## Precondition

User is in gameplay session and has already solved at least one puzzle

## Steps

1. Solve a puzzle to increase the depth level.
2. Check the current depth value.
3. Verify if the next puzzle's difficulty is dependent on this new depth level.

## Expected Result

Depth progresses after solving each puzzle and increases gradually with each solved puzzle.
