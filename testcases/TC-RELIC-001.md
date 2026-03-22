# TC-RELIC-001: Test ItemSlotSystem with Different Star Difficulty

- **Requirement:** REQ-RELIC-001
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 08:54:19

## Precondition

A puzzle instance is running, star difficulty levels are set up in the game

## Steps

1. Select a puzzle and start it
2. With different star difficulties selected (e.g., 1-star, 3-star etc.), attempt to select an item from the reward slots
3. Verify that only items with matching or lower difficulty can be selected

## Expected Result

Only items of equal or lower star difficulty are available for selection in each instance
