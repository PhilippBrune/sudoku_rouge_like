# TC-RUN-059: Test HP Attrition Implementation

- **Requirement:** REQ-RUN-060
- **Type:** Unit
- **Risk:** Low
- **Created:** 2026-03-22 12:40:06

## Precondition

Player's HP is at a certain level after completing a level

## Steps

1. Complete a level that requires resource consumption and check the player's HP before and after the level completion
2. Verify if HP decreases by a set amount on level completion (which can be found in game settings)

## Expected Result

The HP should decrease by a certain value after every level completion, indicating attrition
