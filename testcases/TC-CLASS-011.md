# TC-CLASS-011: Check if PersistenceModel stores XP as a single total value per class

- **Requirement:** REQ-CLASS-012
- **Type:** Unit
- **Risk:** Low
- **Created:** 2026-03-22 08:52:59

## Precondition

The game is initialized and has played at least one run

## Steps

1. Start the game and complete a series of levels
2. Observe the total accumulated XP value in the PersistenceModel

## Expected Result

No separate current level XP should be stored, the XP is stored as a single total accumulated value per class
