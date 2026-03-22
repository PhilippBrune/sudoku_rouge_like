# TC-BOSS-022: Validation of Inputs for Negative Modifiers

- **Requirement:** REQ-BOSS-022
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 12:39:11

## Precondition

A game instance with active negative modifiers.

## Steps

1. Start a new gameplay session.
2. Attempt to input data that adheres to the associated constraints but includes a global negative modifier.
3. Verify if the system properly validates and rejects these inputs without crashing or erroring out.

## Expected Result

The system should validate any given inputs in compliance with their associated constraints including negative modifiers. Any invalid input should be rejected, preventing gameplay from being disrupted.
