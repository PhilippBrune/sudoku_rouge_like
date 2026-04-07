# TC-SAVE-038: Test save file rotation policy with atomic write pattern

- **Requirement:** REQ-SAVE-038
- **Type:** Integration
- **Risk:** High
- **Created:** 2026-03-22 13:51:56

## Precondition

A game instance is running with at least one save file.

## Steps

1. Launch the game and load a saved state.
2. Save another game state. 
3. Verify that only the last five saves are preserved, and the older ones have been replaced by the new save.
4. Verify that each save was written atomically i.e., no part of it is lost or incomplete during saving.

## Expected Result

Only the last five saves are retained, all others are overwritten, and all writes were atomic.
