# TC-GENERAL-005: Validate Total Earned XP per Category during a Typical Run

- **Requirement:** REQ<｜begin▁of▁sentence｜>-CLASS-016
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 08:53:12

## Precondition

Game is running with an action earning XP in different categories

## Steps

1. Start a game session without earning any XP
2. Perform several actions that earn XP from each category
3. End the current run and verify total earned XP displayed for each category in the UI

## Expected Result

The total earned XP for each category matches the calculated target after performing typical mid-game actions.
