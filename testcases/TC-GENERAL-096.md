# TC-GENERAL-096: Level up progression system

- **Requirement:** REQ-GENERAL-028
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 13:48:17

## Precondition

A player is in the game and has earned experience points.

## Steps

1. Call the `LevelUp` function of ClassXpProgressionSystem with an input that represents enough xp to level up.
2. Verify if the player's character levels up by inspecting their stats.

## Expected Result

The character should have increased in level and relevant stats.
