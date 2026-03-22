# TC-RELIC-011: Test KoiReflectionItem candidates highlighting without consumption of Pencil charges

- **Requirement:** REQ-RELIC-011
- **Type:** Unit
- **Risk:** Low
- **Created:** 2026-03-22 08:54:47

## Precondition

The game world has some cells with no value but with pencil charges.

## Steps

1. Use the koi reflection item to examine the cells with pencil charges.
2. Verify the candidate cells are highlighted without consuming any charges.

## Expected Result

After step 1, all cells that could be filled but do not have a value should be highlighted and no Pencil charges consumed.
