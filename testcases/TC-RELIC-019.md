# TC-RELIC-019: Test Item Acquisition Mechanism

- **Requirement:** REQ-RELIC-019
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 13:49:22

## Precondition

The player has completed a puzzle and obtained an item.

## Steps

1. Complete a puzzle to obtain a new item.
2. Check if the inventory is full by trying to acquire another item.
3. Verify that the system does not allow further acquisition of items when the inventory is already full.

## Expected Result

The system should prevent the player from acquiring more items once their inventory is full.
