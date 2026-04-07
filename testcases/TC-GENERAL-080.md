# TC-GENERAL-080: GameStateController - Verify correct flow of the game

- **Requirement:** REQ-GENERAL-017
- **Type:** Integration
- **Risk:** High
- **Created:** 2026-03-22 13:47:16

## Precondition

The game is running and the system is ready for testing.

## Steps

1. Start the game with player on class selection screen.
2. Select a class, confirm the selection.
3. Press 'Start Game'.
4. Play through several rounds of gameplay (e.g., battle, puzzles etc.).
5. Check if the game state controller is managing flow correctly throughout the gameplay.

## Expected Result

The overall flow of the game should be managed without any issues and should transition smoothly between different game states (like BattleState, PuzzleState etc.) as per the game progression.
