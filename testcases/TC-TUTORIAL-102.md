# TC-TUTORIAL-102: Test Mistake Behaviour in Class-based Mode

- **Requirement:** REQ-TUTORIAL-102
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 12:43:07

## Precondition

The player is in class-based mode.

## Steps

1. Start the game and navigate to a puzzle with a specific class requirement.
2. Make an incorrect move in the puzzle that does not fulfill the class requirements.
3. Verify if making any mistake causes a loss of HP equivalent to the class stats per configuration used by the player.

## Expected Result

Making a mistake should cause a loss of HP based on the class's stats when playing in class-based mode.
