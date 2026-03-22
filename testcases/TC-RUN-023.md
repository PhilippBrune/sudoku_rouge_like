# TC-RUN-023: Test Game Resets Progress and Unlocks on Run Completion/Abandonment

- **Requirement:** REQ-RUN-023
- **Type:** Integration
- **Risk:** High
- **Created:** 2026-03-22 08:55:59

## Precondition

The user is in a Spirit Trials Mode game.

## Steps

1. End the current game without completing it.
2. Verify that progress has been reset and all modes are still accessible after abandoning the game.

## Expected Result

Upon run completion or abandonment, the game should reset its state to allow for new games or mode advances after completing Garden Runs or Ascension Levels.
