# TC-RELIC-018: Test Reroll Mechanic

- **Requirement:** REQ-RELIC-018
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 13:49:22

## Precondition

The player has an item in their inventory and they have used up all of the remaining uses of that item.

## Steps

1. Click on a grid cell with no puzzle piece present to activate the reroll mechanic.
2. The system should display a list of new items for the player to pick from, based on categories like Solver, Finder, etc., but not including any items they've already picked or used. 
3. Choose a different item by clicking on it in the displayed list and verify that the grid cell now displays the newly selected item.

## Expected Result

The system should display a new set of items for reroll and allow the player to pick one from them.
