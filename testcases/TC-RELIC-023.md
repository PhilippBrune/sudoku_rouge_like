# TC-RELIC-023: Verify Item Price Scaling and Base Prices Change Based on Floor Level and Rarity

- **Requirement:** REQ-RELIC-023
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 13:49:47

## Precondition

There exists an item with a given base price, rarity level, floor level.

## Steps

1. Increase the floor level of the player by one.
2. Verify that the item's base price has increased as expected based on the new floor level and its rarity.
3. Compare the result with the manual calculations or an automated way to calculate it.

## Expected Result

The item's base price increases as expected after increasing the floor level by one.
