# TC-GENERAL-085: Manage and use Pencil units for removing numbers

- **Requirement:** REQ<｜begin▁of▁sentence｜>021 — PencilUnitsResource: The system shall manage Pencil Units that are used to remove numbers from cells in the Sudoku puzzle.>
- **Type:** Unit
- **Risk:** Low
- **Created:** 2026-03-22 13:47:33

## Precondition

Game is running with at least one available Pencil unit

## Steps

1. Start a new game or load an existing saved game
2. Open the Sudoku puzzle
3. Attempt to fill in a number by erasing instead of filling it
4. Verify if the pencil units reduce by one when used for removing numbers from cells

## Expected Result

When a player attempts to remove a number, it should consume a Pencil unit and not be allowed if no Pencil Units are available.
