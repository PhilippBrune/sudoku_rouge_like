# Game UI Redesign: Path Overview -> Sudoku

## Flow

1. Start Game opens **Class Select**.
2. Player picks class and presses **Continue**.
3. Screen opens **Path Overview** (large panel).
4. Player chooses first tile on Calm or Risk lane.
5. Screen switches to **Sudoku Gameplay** (large panel).
6. After solving the current Sudoku, screen returns to Path Overview for next tile.
7. Path choice is lane-locked after first decision and remains locked until shared boss end.

Both screens include **Save & Quit**.

## Path Rules

- Calm lane length is randomized each run: `4..8` tiles.
- Risk lane is always exactly `Calm - 1` tiles.
- Risk lane is always harder than Calm at each comparable step.
- Both lanes share one final boss node.
- Lane visuals are static and no longer rebuilt every refresh tick (prevents blinking).
- Node signs show:
  - top-left: board size (for example `7x7`)
  - center: node type
  - bottom-right: star difficulty (`*`)

## Sudoku Interaction

Implemented controls:
- Click a cell to select it.
- Selected cell highlight + row/column highlight.
- Double-click a filled cell to highlight all identical numbers in grid.
- Enter values by:
  - On-screen numpad buttons (`1..9`)
  - Keyboard (`Alpha1..Alpha9` and `Keypad1..Keypad9`)
- Given cells are read-only.
- Tutorial mode auto-selects an editable cell when possible, so number entry is immediately usable.

## Files

- `Assets/Scripts/My project/Assets/Scripts/UI/PrototypeRunScreenController.cs`
- `Assets/Scripts/My project/Assets/Scripts/UI/RunMapController.cs`
- `Assets/Scripts/My project/Assets/Scripts/Run/RunDirector.cs`
- `Assets/Scripts/My project/Assets/Scripts/Run/RunGraphService.cs`
- `Assets/Scripts/My project/Assets/Scripts/UI/MainMenuController.cs`
- `Assets/Scripts/My project/Assets/Scripts/UI/MainMenuBlueprintBuilder.cs`
- `Assets/Scripts/My project/Assets/Scripts/UI/InRunUiBlueprintBuilder.cs`

## Notes

- First path pick no longer blocks on puzzle completion.
- Puzzle completion triggers transition back to path overview.
- Legacy debug/event panels are hidden in the redesigned in-run flow.
