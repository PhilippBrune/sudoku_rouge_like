# TC-CLASS-015: Validate Total Earned XP per Run

- **Requirement:** REQ-CLASS-015
- **Type:** Unit
- **Risk:** Low
- **Created:** 2026-03-22 08:53:12

## Precondition

Game is running with an action earning XP

## Steps

1. Start a game session without earning any XP
2. Perform several actions that earn XP
3. End the current run and verify total earned XP displayed in the UI

## Expected Result

The total earned XP per run matches the calculated target of 80 runs to reach Level 40 (16,860 XP total).
