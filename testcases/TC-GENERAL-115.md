# TC-GENERAL-115: Testing Screen Transitions Configuration with Unity's `ScreenManager`

- **Requirement:** REQ-GENERAL-037
- **Type:** Manual
- **Risk:** High
- **Created:** 2026-03-22 13:55:02

## Precondition

The game should be running without crashing, and all panels are configured correctly.

## Steps

1. Press the 'New Run' button to start a new game.
2. Check if the correct panel (usually GamePanel) is shown after pressing the New Run button.
3. Pause the game by pressing the 'Pause' button.
4. Check if the correct panel (usually PausePanel) is shown after pausing the game.
5. Resume the game from pause by clicking on the 'Resume' button.
6. Check if the GamePanel is now visible again.
7. Lose a game to check if the Victory/Defeat panel appears as expected when losing the game.
8. Press the 'Return to Menu' button.
9. Verify that the MainMenuPanel is shown after returning to menu.

## Expected Result

The correct screen should be visible based on user inputs or game events. No crash in the system during these transitions.
