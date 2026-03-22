# TC-SAVE-008: Check data lifecycle management in save functionality

- **Requirement:** REQ-SAVE-009
- **Type:** Integration
- **Risk:** Low
- **Created:** 2026-03-22 08:55:34

## Precondition

Save and Quit option has been selected during Garden Run mode play

## Steps

1. Start a new game or load an existing one, complete sections of the garden run, choose "Save & Quit"
2. Verify that a save file was created in the expected location with correct data
3. Close and restart the application to simulate player's end of game
4. Attempt to resume from last saved state using "Resume Game" option

## Expected Result

The game should not be able to resume as there is no existing saved game
