# TC-SHOP-030: Test GoldEconomyPerFloorSystem to ensure it functions correctly

- **Requirement:** REQ-SHOP-029, REQ-SHOP-020
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 13:48:45

## Precondition

Player has visited a new floor in the game

## Steps

1. Activate the game and navigate to a new floor (town or dungeon)
2. Complete all available actions on that floor, for instance by defeating monsters, gathering resources, etc.
3. Verify if the correct amount of gold has been added to the player's inventory after visiting each new floor

## Expected Result

The expected result is that the GoldEconomyPerFloorSystem should correctly manage the gold addition post-visit to a new floor
