# TC-GENERAL-077: Test EndScreenFlowControl for Success and Failure Situations

- **Requirement:** REQ-GENERAL-012
- **Type:** Integration
- **Risk:** Medium
- **Created:** 2026-03-22 12:46:14

## Precondition

A game is played successfully or unsuccessfully.

## Steps

1. Play a game, either win or lose.
2. Verify the displayed screen after completing/ending the game for success or failure case.

## Expected Result

For a successful completion (win), an appropriate victory message, XP breakdown, and stats should be shown; for a failed play (loss), a defeat message along with player's stats are expected to be shown.
