# TC-GENERAL-072: Validate class select button pressability based on current game state and player progress

- **Requirement:** REQ-GENERAL-008
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 12:46:04

## Precondition

User is in a game that allows class selection, with at least one unselected class

## Steps

1. Attempt to press the buttons for each available class in the game's character select screen
2. Observe if any button becomes unpressable based on current game state and player progress

## Expected Result

All pressable class select buttons remain clickable and functional even when the game is paused or not, and only unlocked classes are pressable as per player progression
