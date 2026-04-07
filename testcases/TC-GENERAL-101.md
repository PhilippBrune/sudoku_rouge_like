# TC-GENERAL-101: Backup support for save files

- **Requirement:** REQ<｜begin▁of▁sentence｜>-SAVE-020
- **Type:** Unit
- **Risk:** Low
- **Created:** 2026-03-22 13:50:37

## Precondition

A game is in progress and a save file has been created.

## Steps

1. Start a new game.
2. Save the game state.
3. Modify the save file directly to introduce corruption (e.g., rename the file).
4. Restart the game without deleting or overwriting the backup copy of the save file.

## Expected Result

The system should create a backup copy of the save file each time it is saved, and restore this backup if needed during the game. If validation fails after restoring from the backup, the system should display an error message indicating the issue with the backup copy.
