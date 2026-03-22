# TC-RELIC-010: Test PatternScrollItem constraint conflicts highlighting

- **Requirement:** REQ-RELIC-010
- **Type:** Integration
- **Risk:** High
- **Created:** 2026-03-22 08:54:47

## Precondition

The game world has some board constraints in play.

## Steps

1. Generate a situation that triggers constraint conflict on the board.
2. Use the pattern scroll item to examine the conflicted cells.
3. Verify the conflicted cells are highlighted.

## Expected Result

After step 2, all cells involved in the conflict should be highlighted for review or action.
