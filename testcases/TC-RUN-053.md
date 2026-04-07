# TC-RUN-053: Check if ProfileService can return new unlocks after playing 10 runs

- **Requirement:** REQ-RUN-053
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 12:39:53

## Precondition

User has played at least 9 runs

## Steps

1. Call ProfileService.RecordRunAndGetNewUnlocks() method to simulate the user playing another run.
2. Verify that it returns a list of new unlocks.
3. Check if the unlocked item is as expected based on the run count.

## Expected Result

Returns a correct list of new unlocks.
