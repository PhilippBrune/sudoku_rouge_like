# Main Menu Setup (Run of the Nine Theme)

This builds and wires the themed main menu to match `main_menue.png` and uses icon mappings from `icons.png`.

## Scripts

- `Assets/Scripts/UI/MainMenuController.cs`
- `Assets/Scripts/UI/MainMenuBlueprintBuilder.cs`

(Use the copies inside your active Unity project path if you are working in `Assets/Scripts/My project/Assets/Scripts/...`.)

## 1) Create Scene

1. In Unity Project window, create scene: `Assets/Scenes/MainMenu.unity`.
2. Open the scene.

## 2) Add Builder Object

1. Create Empty object named `MainMenuBuilder`.
2. Add component `MainMenuBlueprintBuilder`.
3. In component menu (`⋮`), click **Build Minimal Main Menu**.

This auto-creates:
- Canvas + EventSystem (if missing)
- Centered vertical menu stack with atmospheric background layers
- Buttons: Start, Resume, Tutorial, Meta Progression, Game Modes, Items, Options, Credits, Quit
- Icon wiring using `Assets/Resources/GeneratedIcons/*` mapped from `icons.png`
- `MainMenuController` on `MainMenuBuilder`
- Status message area for resume feedback
- Options overlay (master volume slider + back)
- Credits overlay (text + back)

## Icon Mapping Source

- Reference sheet: `icons.png`
- Runtime icon assets: `Assets/Resources/GeneratedIcons/*.png`
- Main menu button icon paths are assigned in `MainMenuBlueprintBuilder.ApplyMenuButtonIcon(...)`

## 3) Configure Gameplay Scene Name

On `MainMenuBuilder` -> `MainMenuController`:
- Set `Gameplay Scene Name` = `Prototype` (or your real run scene name).

## 4) Add Scenes To Build Settings

Open `File -> Build Settings`:
- Add `MainMenu` scene
- Add `Prototype` scene
- Put order:
  1. `MainMenu`
  2. `Prototype`

## 5) Test

Press Play from `MainMenu` scene:
- `Start Game` should load `Prototype`
- `Resume` loads `Prototype` only if run save exists, otherwise status text shows a warning
- `Options` opens a panel with master volume slider and Back
- `Credits` opens a panel with Back
- `Quit` exits play mode in editor

## Notes

- The builder now follows the painterly garden look from `main_menue.png` with a centered stack and warm gold accents.
- Class Select and Game Modes buttons also use icon-sheet-based icon assignments.
- If a button icon is missing, it falls back to text-only and keeps functionality intact.
