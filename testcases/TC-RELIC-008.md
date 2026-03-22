# TC-RELIC-008: Test MeditationStoneItem restoration HP upon use

- **Requirement:** REQ-RELIC-008
- **Type:** Unit
- **Risk:** Low
- **Created:** 2026-03-22 08:54:47

## Precondition

The player character has an empty HP bar and meditation stone item in inventory.

## Steps

1. Pick up the meditation stone item from the game world.
2. Equip the meditation stone to a suitable slot (like weapon or shield).
3. Use the meditation stone on self (or another player if possible).
4. Verify HP bar is restored.

## Expected Result

After step 3, the player's HP should be fully restored.
