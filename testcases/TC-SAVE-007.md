# TC-SAVE-007: Verify atomic write pattern in save functionality

- **Requirement:** REQ-SAVE-008
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 08:55:34

## Precondition

Save and Quit option is selected during Garden Run mode play

## Steps

1. Start a new game or load an existing one, complete sections of the garden run, choose "Save & Quit"
2. Immediately close the application without swapping the temporary save file with primary save path
3. Restart the application and attempt to resume from last saved state

## Expected Result

The game should not be able to resume if the atomic write pattern is not followed
