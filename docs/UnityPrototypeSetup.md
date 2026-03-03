# Unity Prototype Setup (Use Existing Scripts)

## 1. Create Unity Project

Follow [Unity install guide](UnityInstall_Windows.md), then create a new `2D (URP)` Unity project in this repository root.

## 2. Keep/Copy Script Folder

Ensure this folder exists in the Unity project:

- `Assets/Scripts/`

It already contains:

- Core enums + run models
- Sudoku generator/validator
- Economy formulas
- Class catalog
- Route, item, and boss services
- Run director orchestrator
- Bootstrap MonoBehaviour

## 3. Scene Wiring (Fast Start)

1. Create a scene: `Assets/Scenes/Prototype.unity`
2. Add an empty GameObject named `GameBootstrap`
3. Attach component `GameBootstrap` from:
   - `SudokuRoguelike.Bootstrap.GameBootstrap`
4. Press Play
5. Check Console for run and level logs

## 4. Suggested Next Implementation Order

1. Build grid rendering UI (`SudokuBoardView`) from `RunDirector.CurrentBoard`
2. Add cell click selection and numeric input mapping
3. Add HUD bars for HP/Pencil/Gold/XP from `RunState`
4. Add item roll panel UI using `BuildItemRollPhase()` and `TryRerollItemSlots()`
5. Add route selection UI using `RollRouteChoice()` and `ApplyRoute()`
6. Add boss pre-choice and phase runner using `BossService` hooks

## 5. Important Notes

- Non-square sizes (5x5, 6x6, 7x7, 8x8) use a generated region-map variant, so constraints remain row/column/region based.
- All key formulas from your design are implemented in `FormulaService`.
- This is a functional systems foundation; final production still needs full UI, art, animations, and scene transitions.
