# TC-TUTORIAL-184: Mistake Behaviour in Free Play

- **Requirement:** REQ-TUTORIAL-183
- **Type:** Unit
- **Risk:** Low
- **Created:** 2026-03-22 13:52:49

## Precondition

Player is in tutorial mode and starts a new game from the main menu.

## Steps

1. Start a new game from main menu and enter into tutorial mode.
2. Attempt to complete a puzzle or level during tutorial play with deliberate mistakes.
3. Check if player loses HP or the game ends after making mistakes in tutorial mode.

## Expected Result

Mistakes should not cost any resources, including health points (HP), and no game end should occur due to incorrect input. The correctness of actions is determined by the rules provided by the tutorial.
