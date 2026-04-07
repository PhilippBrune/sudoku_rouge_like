# TC-SHOP-021: Gold Persistence Across Runs

- **Requirement:** REQ-SHOP-022
- **Type:** Integration
- **Risk:** Low
- **Created:** 2026-03-22 12:40:29

## Precondition

A player has spent gold in previous runs and now starting a new game.

## Steps

1. Start a new run of the game after spending gold previously.
2. Verify that the player's inventory shows the correct amount of gold they started with, plus any additional gold spent during previous runs.

## Expected Result

The game retains all previous gold spent across multiple sessions and allows them to buy items/rerolls using this as well.
