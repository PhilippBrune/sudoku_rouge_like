# TC-RELIC-002: Test RerollRules System

- **Requirement:** REQ-RELIC-002
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 08:54:19

## Precondition

A puzzle instance is running, some item slots have been selected

## Steps

1. With a few item slots filled, attempt to reroll unselected items
2. Verify that the system properly allows for this action and new items are selected from available pools
3. Attempt to reroll without selecting any first

## Expected Result

The system prompts the user if they have not made a selection before performing a reroll, or selects new items automatically after successful reroll
