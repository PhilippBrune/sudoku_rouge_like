# TC-RELIC-024: Test Epic Item Generation Rules - Verify Availability of Epic Items

- **Requirement:** REQ-RELIC-025
- **Type:** Unit
- **Risk:** Low
- **Created:** 2026-03-22 13:49:47

## Precondition

The game is at the start with a pool of non-epic items.

## Steps

1. Increase the floor level to trigger epic item generation event.
2. Check if an epic item has appeared in the inventory after increasing the floor level.
3. Repeat the process multiple times to check randomness of generated epic items.

## Expected Result

After every successful increase in floor level, there should be a chance for generating an epic item based on pre-defined rules.
