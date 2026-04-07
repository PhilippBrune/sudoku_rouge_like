# TC-SHOP-032: Test GoldPersistenceSystem to ensure it functions correctly

- **Requirement:** REQ-SHOP-031, REQ-SHOP-022
- **Type:** Integration
- **Risk:** Low
- **Created:** 2026-03-22 13:48:45

## Precondition

Player has started the game and acquired some gold

## Steps

1. Deactivate the game or close the application and reopen it
2. Verify if the player's inventory still contains the correct amount of gold after reopening the game

## Expected Result

The expected result is that the GoldPersistenceSystem should maintain a persisting record of player's gold across different sessions of the game
