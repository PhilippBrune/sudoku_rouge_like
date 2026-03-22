# TC-TUTORIAL-024: ProfileService stores and loads persistent data for Tutorial Progress per profile

- **Requirement:** REQ-TUTORIAL-024
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 11:38:11

## Precondition

A user has completed a tutorial puzzle.

## Steps

1. Complete a puzzle in the tutorial mode.
2. Save progress to a test profile.
3. Load that same profile and verify if the tutorial progress is persisted.

## Expected Result

The ProfileService should store tutorial completion data per profile, which can be loaded at any time.
