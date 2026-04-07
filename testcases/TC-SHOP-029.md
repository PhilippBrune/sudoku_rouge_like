# TC-SHOP-029: Test GoldSpendingSystem to ensure it functions correctly

- **Requirement:** REQ-SHOP-028, REQ-SHOP-019
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 13:48:45

## Precondition

Player has sufficient gold in their inventory

## Steps

1. Activate the game and navigate to a town where you have enough gold
2. Use an action that costs gold, for example buying a new sword or upgrading your armor
3. Verify if the correct amount of gold is deducted from the player's inventory after the transaction

## Expected Result

The expected result is that the GoldSpendingSystem should correctly manage the gold deduction post-transaction
