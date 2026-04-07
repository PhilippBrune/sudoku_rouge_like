# TC-SHOP-025: Test NothingSlotBehaviourSystem in item reward phase

- **Requirement:** REQ-SHOP-024
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 12:40:38

## Precondition

Player starts a new game and goes through the shop. The player should be at an item reward phase.

## Steps

1. Start a new game.
2. Navigate to the item reward phase by clicking on "Continue" button or any other way available for it in UI.
3. Select an item from the Nothing slot (which is supposed to grant nothing).
4. Verify that no gold bonus or reward has been granted, just end to the reward phase.

## Expected Result

The player should not receive any rewards or bonuses and the reward phase should continue until all items have been given out.
