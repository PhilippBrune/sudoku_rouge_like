# TC-GENERAL-099: Test Economy for Items - Verify Buying, Selling, Rerolling, and Replacing Within Budget Constraints

- **Requirement:** REQ<｜begin▁of▁sentence｜>-RELIC-024
- **Type:** Integration
- **Risk:** Medium
- **Created:** 2026-03-22 13:49:47

## Precondition

Player starts the game with a specific budget.

## Steps

1. Attempt to buy an item that exceeds player's budget.
2. Verify that the transaction is denied due to insufficient funds and no items are purchased.
3. Spend all available money on buying items, then sell them for profit or use rerolling functionality.
4. Repeat the process until the player’s budget reaches zero.

## Expected Result

The player can only buy items within their budget constraint. Profit from selling/rerolling is correctly calculated and accounted for in the player's remaining budget.
