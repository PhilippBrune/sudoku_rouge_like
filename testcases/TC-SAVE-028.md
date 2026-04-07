# TC-SAVE-028: Check if the new save file is created on first launch of the game

- **Requirement:** REQ-SAVE-028
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 13:51:05

## Precondition

The game has just been launched.

## Steps

1. Launch the game
2. Save and close the game (without starting a new game)
3. Open the save file in a text editor to check if it's created

## Expected Result

A new json file named "save_game.json" is created with default data
