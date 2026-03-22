# TC-GENERAL-032: Verify RunDirector isolates run logic during tutorial by disabling all non-puzzle features

- **Requirement:** REQ<｜begin▁of▁sentence｜>TUTORIAL-075</s>
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:39:57

## Precondition

The game has been built and compiled successfully.

## Steps

1. Open the game in Unity.
2. Start a new game.
3. Verify that RunDirector is isolating run logic during tutorial by disabling all non-puzzle features (progression, items, path system). It should work based on TutorialSetupConfig object.

## Expected Result

The RunDirector is isolated from the rest of the game and its functionalities are disabled as per requirements when starting a new tutorial run.
