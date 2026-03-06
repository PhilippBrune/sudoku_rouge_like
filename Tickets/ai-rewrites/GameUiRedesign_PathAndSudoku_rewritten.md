# Game UI Redesign: Path Overview -> Sudoku

## Flow

1. Start Game opens the **Class Selection** screen.
2. Player selects a class and presses **Continue**.
3. The screen opens the **Path Overview** panel (large panel).
4. Player chooses a tile from the Calm or Risk lane.
5. The screen switches to the **Sudoku Gameplay** panel (large panel).
6. After solving the current Sudoku puzzle, the screen returns to the Path Overview for the next tile.
7. Path choice is lane-locked after the first decision and remains locked until the shared boss ends.

Both screens include the **Save & Quit** buttons.

## Path Rules

- The length of the Calm lane is randomly determined between 4 and 8 tiles.
- The Risk lane is always one less tile than the Calm lane.
- The Risk lane is always harder than the Calm at each comparable step.
- Both lanes share a final boss node.
- Lane visuals are static and no longer updated every refresh tick (prevents blinking).
- Node signs display:
   - top-left: board size (for example, `7x7`)
   - center: node type
   - bottom-right: star difficulty (`*`)

## Sudoku Interaction

Implemented controls include:
- Clicking a cell to select it.
- Highlighting a selected cell and row/column highlights.
- Double-clicking a filled cell to highlight all identical numbers in the grid.
- Entering values by:
   - On-screen numpad buttons (`1..9`)
   - Keyboard (`Alpha1..Alpha9` and `Keypad1..Keypad9`)
- Given cells are read-only.
- Tutorial mode automatically selects an editable cell when possible, so number entry is immediately usable.

## Files

- `Assets/Scripts/My project/Assets/Scripts/UI/PrototypeRunScreenController.cs`
- `Assets/Scripts/My project/Assets/Scripts/UI/RunMapController.cs`
- `Assets/Scripts/My project/Assets/Scripts/Run/RunDirector.cs`
- `Assets/Scripts/My project/Assets/Scripts/Run/RunGraphService.cs`
- `Assets/Scripts/My project/Assets/Scripts/UI/MainMenuController.cs`
- `Assets/Scripts/My project/Assets/Scripts/UI/MainMenuBlueprintBuilder.cs`
- `Assets/Scripts/My project/Assets/Scripts/UI/InRunUiBlueprintBuilder.cs`

## Notes

- The first path pick no longer blocks on puzzle completion.
- Puzzle completion triggers a transition back to the Path Overview.
- Legacy debug/event panels are hidden in the redesigned in-run flow.


