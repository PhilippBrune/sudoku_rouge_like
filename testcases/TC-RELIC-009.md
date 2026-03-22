# TC-RELIC-009: Test WindChimeItem undoing last mistake and effects based on rarity

- **Requirement:** REQ-RELIC-009
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 08:54:47

## Precondition

The player character has a wind chime item in inventory.

## Steps

1. Pick up the wind chime item from the game world.
2. Use the wind chime (or other related actions that would undo a mistake).
3. Verify the correct effect is applied based on its rarity.

## Expected Result

After step 2, the player should have undone their last action and received an appropriate feedback or status effect based on the rarity of the item.
