# TC-SHOP-027: Gold Acquisition from Shop System

- **Requirement:** REQ-SHOP-026
- **Type:** Integration
- **Risk:** Medium
- **Created:** 2026-03-22 13:48:17

## Precondition

A player is in the game and has bought items from the shop interface.

## Steps

1. Purchase an item from the shop using the `PurchaseItem` function of GoldAcquisitionSystem.
2. Monitor the gold balance to verify if it decreased by the cost of the purchased item.

## Expected Result

The player's gold balance should decrease by the value of the bought item after purchasing.
