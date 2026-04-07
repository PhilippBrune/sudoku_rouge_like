# TC-SHOP-019: Gold spending on Pencil Refills or Item Rerolls

- **Requirement:** REQ-SHOP-019
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 12:40:29

## Precondition

A player has sufficient gold to purchase a pencil refill/reroll and item.

## Steps

1. Start the game with a known quantity of gold.
2. Attempt to buy a Pencil Refill or Item Reroll when they have enough gold available.
3. Verify that the correct amount of gold was subtracted from the player's inventory after the purchase.

## Expected Result

The system properly calculates and deducts the cost for each purchase, with no remaining excess gold in the player's inventory.
