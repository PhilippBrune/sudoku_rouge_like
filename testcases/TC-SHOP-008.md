# TC-SHOP-008: Testing the selling items and relics feature

- **Requirement:** REQ-SHOP-008
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 08:54:09

## Precondition

Game is running with items/relics for sale.

## Steps

1. Open the shop menu.
2. Attempt to sell an item or a relic using gold.
3. Verify that no new items are added and gold spent does not result in increased gold reserves.

## Expected Result

Selling items or relics should be disabled, and attempting to do so should have no effect on the game state.
