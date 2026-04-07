# TC-SHOP-020: Gold spending exceeds Pencil Inventory

- **Requirement:** REQ-SHOP-019
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 12:40:29

## Precondition

A player has a known quantity of gold and pencils, trying to exceed the available inventory.

## Steps

1. Start the game with a known amount of gold and pencils in the inventory.
2. Attempt to buy an item/reroll when they have more gold than pencils in inventory.
3. Verify that the system prevents the purchase and only deducts what is available in the inventory.

## Expected Result

The system does not allow buying items or rerolling if there's no enough pencils, returning a message indicating this condition.
