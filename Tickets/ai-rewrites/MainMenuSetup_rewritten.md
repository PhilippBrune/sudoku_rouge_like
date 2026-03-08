# Main Menu Setup (Run of the Nine Theme)

This document outlines the setup and wiring of the themed main menu to match the visual style of `main_menue.png` and uses icon mappings from `icons.png`.

## Scripts

- `Assets/Scripts/UI/MainMenuController.cs`
- `Assets/Scripts/UI/MainMenuBlueprintBuilder.cs`

(Use the copies inside your active Unity project path if you are working in `Assets/Scripts/My project/Assets/Scripts/...`.)

## 1) Create Scene

1. In the Unity Project window, create a new scene at: `Assets/Scenes/MainMenu.unity`.
2. Open the newly created scene.

## 2) Add Builder Object

1. Create an Empty object named `MainMenuBuilder`.
2. Attach the `MainMenuBlueprintBuilder` component to `MainMenuBuilder`.
3. In the component menu, click **Build Minimal Main Menu**.

This action will auto-create:
- Canvas and EventSystem (if not already present)
- A centered vertical menu stack with atmospheric background layers
- Buttons: Start, Resume, Tutorial, Meta Progression, Game Modes, Items, Options, Credits, Quit
- Icon wiring using `Assets/Resources/GeneratedIcons/*` mapped from `icons.png`
- `MainMenuController` on `MainMenuBuilder`
- Status message area for resume feedback
- Options overlay with a master volume slider and a Back button
- Credits overlay with a Back button

## Icon Mapping Source

- Reference sheet: `icons.png`
- Runtime icon assets: `Assets/Resources/GeneratedIcons/*.png`
- Main menu button icon paths are assigned in `MainMenuBlueprintBuilder.ApplyMenuButtonIcon(...)`

## 3) Configure Gameplay Scene Name

On `MainMenuBuilder` ÔåÆ `MainMenuController`:
- Set the `Gameplay Scene Name` to `Prototype` (or your actual gameplay scene name).

## 4) Add Scenes To Build Settings

Open `File ÔåÆ Build Settings`:
- Add the `MainMenu` scene
- Add the `Prototype` scene
- Order the scenes as follows:
  - `MainMenu`
  - `Prototype`

## 5) Test

Start the game from the `MainMenu` scene:
- `Start Game` should load the `Prototype` scene
- `Resume` should load the `Prototype` scene only if a saved game exists, otherwise it shows a warning in the status text
- `Options` opens an overlay panel with a master volume slider and a Back button
- `Credits` opens an overlay panel with a Back button
- `Quit` exits the editor play mode

## Notes

- The main menu builder follows the visual style of `main_menue.png`, using a centered stack and warm gold accents.
- The Class Select and Game Modes buttons also use icon-sheet-based icon assignments.
- If a button icon is missing, it defaults to text-only mode and maintains functionality.

Assumptions/Clarifications:
- The term "Class Select" is used in the context of a game mode that allows players to select a class (e.g., warrior, mage, rogue).
- The term "Prototype" is used to represent the gameplay scene that will be loaded when the player starts a game.


