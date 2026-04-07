# First Time Play — Sudoku Basics Tutorial

**Version:** 0.1 | **Date:** 2026-04-07 | **Status:** new — awaiting review

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 0.1 | 2026-04-07 | new | Initial specification |

---

## Overview

When a player launches the game for the very first time, they are asked whether they want the basic rules of Sudoku explained. If they accept, a short, fixed, step-by-step tutorial plays before they reach the main menu. This is a **one-time experience** — it never plays automatically again, but can be replayed on demand from the main menu.

---

## Trigger Condition

Triggered automatically on first launch if:
```
TutorialProgressState.CompletedKeys does NOT contain "sudoku_basics"
```

On any subsequent launch, the check is skipped. No dialog appears.

The check and prompt happen immediately after the main menu loads (not before), so the game's title screen appears first.

---

## The Prompt Dialog

A simple centered modal dialog over the main menu background:

```
┌─────────────────────────────────────────────────────┐
│                                                     │
│   Welcome to Run of the Nine.                       │
│                                                     │
│   Do you want a quick introduction to               │
│   how Sudoku puzzles work?                          │
│   (This takes about 2 minutes.)                     │
│                                                     │
│        [ Yes, show me ]     [ Skip ]                │
│                                                     │
└─────────────────────────────────────────────────────┘
```

- **"Yes, show me"** → launch the Sudoku Basics Tutorial level (see below).
- **"Skip"** → mark as complete (`CompletedKeys.Add("sudoku_basics")`), proceed to main menu.

Both choices record `"sudoku_basics"` in `TutorialProgressState.CompletedKeys` so the prompt never appears again.

---

## Tutorial Level Setup

The tutorial uses a **fixed 4×4 board** with a pre-determined solution and a pre-determined set of given cells. Using a 4×4 keeps it non-threatening for players unfamiliar with Sudoku.

### Board layout (fixed, seeded)
- **Size:** 4×4
- **Region variant:** Standard (2×2 boxes)
- **Given cells:** ~8 of 16 cells pre-filled (50% density = easy)
- **Solution:** deterministic — the same every time (hard-coded or seed `999`)
- **No modifiers**, no HP cost on mistakes, no pencil cost, no gold

### RunState for basics tutorial
```
Mode       = GameMode.Tutorial
TutorialMode = true
CurrentHP  = 999
MaxHP      = 999
CurrentPencil = 999
MaxPencil  = 999
DisableProgressionRewards = true
CurrentGold = 0
```

Mistakes made during this tutorial are **not penalised** — a wrong placement shows the conflict highlight but does not cost HP. The player can freely experiment.

---

## Step-by-Step Flow

The tutorial progresses through 7 **named steps**. Each step shows a highlighted overlay and a text box anchored below or above the board.

The player advances to the next step by pressing **"Next"** or (once input is required) by performing the action.

### Step 1 — "The Grid"
> *"This is a Sudoku grid. It's divided into rows, columns, and regions."*
- Highlight: all row lines flash briefly, then column lines, then the 2×2 box borders (3 distinct highlights, 1 second each)
- No input required. "Next →" button.

### Step 2 — "The Rule"
> *"Each row, each column, and each region must contain every number exactly once. In a 4×4 grid, each uses the numbers 1, 2, 3, and 4."*
- Highlight: one complete row pulses gold to show it must contain 1-2-3-4.
- No input required. "Next →" button.

### Step 3 — "Given Digits"
> *"Some cells are already filled in. These are the 'givens' — fixed clues you cannot change."*
- Highlight: all given cells pulse (slightly brighter shade, same technique as `GivenCellColor`)
- No input required. "Next →" button.

### Step 4 — "Finding a Safe Cell"
> *"Let's find a cell we can fill in. Look at this row — it already has 1 and 3. The missing numbers are 2 and 4."*
- Highlight: a specific row with 2 given digits. The two empty cells in that row pulse.
- Arrow or bracket points to the row.
- No input required. "Next →" button.

### Step 5 — "Column Elimination"
> *"Now look at the column above the left empty cell. It already has a 4. So this cell must be 2."*
- Highlight: the column containing the target empty cell pulses briefly.
- The number "2" appears as a suggestion highlight on the cell.
- No input required. "Next →" button.

### Step 6 — "Your Turn: Place a Number"
> *"Tap the cell, then press 2 on the numpad. Give it a try!"*
- The target cell pulses with a golden border.
- Input is now enabled for that specific cell only. All other cells are locked.
- Numpad is shown. Only the "2" button is highlighted; others are dimmed.
- **Required action:** player taps cell and presses 2. On success, a brief positive animation plays (green flash).
- If the player presses a wrong number, the numpad shakes slightly and a small label says "Try 2 — check the row and column!"

### Step 7 — "That's Sudoku!"
> *"You placed the right number. That's how Sudoku works — use logic to eliminate possibilities until only one number fits each cell. Now try filling the rest on your own!"*
- All locked cells are unlocked. Player can freely complete the 4×4 puzzle.
- A "Finish Tutorial" button appears in the corner — the player does not need to complete the puzzle to proceed.
- If the player completes the puzzle correctly, a small "Puzzle Complete!" celebration plays (matching the normal completion sound).

---

## Completion

When the player either presses "Finish Tutorial" or completes the puzzle:
1. `TutorialProgressState.CompletedKeys.Add("sudoku_basics")` (if not already added)
2. Save file is written.
3. Transition to Main Menu (the same transition as finishing the full Tutorial mode).

---

## Replay from Main Menu

The tutorial should be replayable. Placement in the menu:

**Options → [new] Replay Sudoku Basics Tutorial**

Or alternatively as a dedicated button in the Tutorial submenu alongside the normal Tutorial mode.

Pressing the button triggers the exact same flow (prompt skipped; tutorial launches directly).

---

## Implementation Notes

### Files to create / modify

| File | Change |
|------|--------|
| `Save/SaveModels.cs` → `TutorialProgressState` | Already has `CompletedKeys` list — no model change needed |
| `Tutorial/TutorialModeService.cs` | Add `BuildSudokuBasicsLevel()` returning a fixed 4×4 `LevelConfig` with `Seed = 999` |
| `UI/SudokuBasicsTutorialController.cs` | **New file.** Step machine: `_step` int, `NextStep()`, `OnCellPlaced()`, step highlight helpers |
| `UI/PrototypeRunScreenController.cs` | Add `ShowSudokuBasicsTutorialStep(int step, string text)` that draws the step text box and highlights |
| `UI/MainMenuBlueprintBuilder.cs` | Add first-time prompt modal after menu build; add "Replay Basics Tutorial" button in Options panel |
| `UI/MainMenuController.cs` | Add `ShowSudokuBasicsPrompt()`, `LaunchSudokuBasicsTutorial()`, `HasCompletedSudokuBasics()` |
| `Bootstrap/GameBootstrap.cs` | On first load: call `MainMenuController.ShowSudokuBasicsPrompt()` if not completed |

### Step text box
- A simple dark panel (same `PanelBg` color as in-run UI) anchored to the bottom of the screen
- Text: ~18pt, readable against all floor backgrounds
- "Next →" button in the bottom-right corner of the panel
- Panel width: 80% screen width, height: ~120px
- Shows only during the basics tutorial, hidden at all other times

### Locking cells for Step 6
During Step 6, the `PrototypeRunScreenController` cell input checks a `_lockedToCell(row, col)` guard before accepting a tap. This is a lightweight per-step flag — no new infrastructure required.

### No penalty for mistakes during Step 6
`RunDirector.PlaceNumber()` checks `State.TutorialMode` — since `TutorialMode = true`, HP cost is already suppressed by existing code. No change needed.

---

## Scope Boundaries

| In scope | Out of scope |
|----------|-------------|
| 4×4 fixed board, 7 steps | Advanced techniques (naked pairs, X-wings, etc.) |
| Row / column / region highlight | Pencilmark tutorial |
| One guided placement | All-cells walkthrough |
| Replay from Options | Separate "Hint" system during normal runs |
| Completion flag in save | Achievement for completing the basics tutorial |
