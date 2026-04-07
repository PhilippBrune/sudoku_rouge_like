# TC-GENERAL-071: Control flow of selecting different game modes like Spirit Trials and Endless Zen

- **Requirement:** REQ-GENERAL-007
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 12:46:04

## Precondition

User is in the main menu, no game in progress

## Steps

1. Attempt to select "Spirit Trials", "Endless Zen" etc., from the dropdown or other navigation menus
2. Observe if the game mode selection process initiates and the new game state is transitioned to

## Expected Result

Selection of each option results in starting a new game in that corresponding mode, without crashing the application
