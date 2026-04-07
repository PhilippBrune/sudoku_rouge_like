# TC-SHOP-028: Additional Gold Sources from System

- **Requirement:** REQ-SHOP-027
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 13:48:17

## Precondition

A player is in the game and has started earning additional gold sources.

## Steps

1. Call the `ActivateSource` function of AdditionalGoldSourcesSystem to start an active source.
2. Monitor the output of GoldEconomySystem to verify if it's increasing over time as expected.

## Expected Result

The system should be producing a non-zero amount of gold over time from the activated source.
