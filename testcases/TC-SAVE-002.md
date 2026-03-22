# TC-SAVE-002: Run State Save System Test

- **Requirement:** REQ-SAVE-003
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 08:55:23

## Precondition

A run has been initiated with a game state

## Steps

1. Initiate a new run and save its state using the `SaveRunState` method.
2. Resume that same run from this saved state using the `LoadRunState` method.

## Expected Result

The resumed run should be identical to the one it was initially initiated with, except for time since start-up.
