# Input, Controller Support & Save Profiles

**Version:** 2.8  
**Date:** 2026-11-02  
**Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 2.8 | 2026-11-02 | implemented | 0.3.1 playtest (owner), controller findings: A confirms and B goes back on every screen, the left stick moves in the menus, Start opens and closes the Options, A on Select Class chooses the class and goes to Begin (INPUT-PAD-005, GAP-101..104, GAP-106); the mouse pointer hides while a pad is in use (INPUT-PAD-006, GAP-105); the key hints stop flickering (INPUT-HINT-002, GAP-107). The D-pad that placed digits and the right stick that moved the pointer point to a Steam Input template (GAP-108, open). |
| 2.7 | 2026-11-02 | implemented | 0.3.1 playtest (owner): Enter on an empty cell with pencil marks clears them (INPUT-KEY-001); the Options marks read LB/RB with a pad and Q/E on the keyboard, and Q/E turn the pages (INPUT-HINT-001, GAP-099). |
| 2.6 | 2026-10-28 | implemented | 0.3.0 playtest (RM-207): the key hints follow the device in use (INPUT-HINT-001); the Custom Puzzle book also turns with PageUp/PageDown. |
| 2.5 | 2026-10-27 | implemented | 0.3.0 playtest (GAP-077..083, GAP-094, GAP-095, RM-206): a gamepad's buttons and right stick come from the Input System (INPUT-PAD-003); the pad layout in a puzzle and on the path (INPUT-PAD-004); the seed field takes no pad focus; the node tooltip follows the pad's focus. INPUT-PAD-002's chosen mode now applies to joysticks the Input System does not know as a gamepad. |
| 2.4 | 2026-10-26 | implemented | Owner request: X-input and D-input modes (INPUT-PAD-002). The buttons are read by position; Options → Controls → Controller: Automatic, X-input or D-input. |
| 2.3 | 2026-10-25 | implemented | GAP-070: in a run the D-pad and LT/RT are read through the Input System (INPUT-PAD-001); before, an Xbox pad's D-pad and both triggers did nothing there. |
| 2.2 | 2026-10-22 | implemented | Owner decision (platform plan, decision 3): controllers reach the game through Steam Input's default layout as Xbox pads; no Steam Input API code. |
| 2.1 | 2026-10-22 | implemented | GAP-062 (owner: "analyze, plan and fix"): on the main menu's panels the D-pad takes one step along the focused control's links, once per press (INPUT-NAV-001); arrow keys and the left stick stay with the UI input module. |
| 2.0 | 2026-04-27 | implemented | Spec matched to the implementation (before this history). |

## Overview

The checked-in project uses a custom input/remap layer plus a three-slot local profile system. This is **not** a Unity Input System action-map setup and it is **not** Steam Input-driven.

Primary implementation references:

- `Assets/Scripts/Core/InputRemapService.cs`
- `Assets/Scripts/UI/KeybindingsPanelController.cs`
- `Assets/Scripts/UI/MainMenuController.cs`
- `Assets/Scripts/Save/SaveFileService.cs`
- `Assets/Scripts/Save/SaveProfileService.cs`

## Profile Slots

Three local save slots are supported:

- slot 0
- slot 1
- slot 2

`SaveProfileService` tracks the active slot through `PlayerPrefs`, and `GameBootstrap.ApplySlotChange()` rewires save/profile services when the player switches slots.

The profile UI shows per-slot summary data such as:

- last class played
- run counts
- boss counts
- last played time
- whether a resumable run exists

## Keyboard Remapping

Keyboard remapping is handled by `InputRemapService`.

Current behavior:

- bindings are stored per slot
- conflict detection is supported
- actions can be reset to defaults
- only keyboard bindings are user-rebindable

The rebind flow is owned by `KeybindingsPanelController`.

## Controller Support

Controller input uses direct `Input` / `KeyCode` checks plus fixed gamepad defaults. It does not currently use:

- Unity Input System gameplay action maps
- Steam Input action sets
- per-controller-type remapping UI

Gamepad support is still present for core menu and puzzle navigation, but it is implemented through the existing custom input layer.

The button numbers are those of an Xbox (XInput) pad on Windows. Other pads, and the Steam Deck (which runs the Windows build through Proton), are made to look like one by Steam Input's default layout in Steamworks (owner decision 2026-10-22, `docs/platform-ports-macos-linux.md` decision 3). This is a Steamworks setting, not code: the game has no Steam Input action sets.

## Main Menu and In-Run Input Model

Current implementation split:

- `MainMenuController` handles menu navigation, confirmation, cancel/back, and overlays

| ID | Requirement |
|---|---|
| INPUT-NAV-001 | On every main-menu panel the D-pad moves the focus one step along the focused control's own link (its explicit Up/Down/Left/Right, or Unity's automatic neighbour), once per press, so grids and rows move as they look (the Codex's Down goes one row down). The D-pad is read through the Input System (`Gamepad.dpad`) and the legacy D-pad buttons 12–15 (`MenuDpad`); the arrow keys and the left stick are left to the UI input module, so nothing moves twice. A movement key bound in Keybindings that the module does not read (anything but the arrow keys and W/A/S/D) moves the same way. An explicit link to a hidden control falls back to the nearest control in that direction. With no focus on the panel, the first control is chosen. (It used to cycle through the panel's controls in creation order on any direction key, GAP-062.) |
| INPUT-PAD-001 | In a run the D-pad moves the board cursor, the path cursor and the bag selection, and LT/RT scroll the path and switch bag slots, on every pad: they are read through the Input System (`Gamepad.current`), with the legacy D-pad buttons 12–15 some pads report as the fallback. The face buttons, the shoulders and the sticks stay on the legacy joystick buttons and axes, which match an Xbox pad on Windows (and every pad Steam Input turns into one). (On Windows an Xbox pad reports its D-pad as axes the Input Manager does not define, and the triggers had no axis there, GAP-070.) |
| INPUT-PAD-002 | The game reads its controller buttons by position: A is the bottom face button, B the right, X the left, Y the top, then LB, RB, Back, Start and the stick presses. An X-input pad (Xbox, and every pad Steam Input turns into one) numbers them 0–9 with the D-pad as 12–15 or axes; a D-input pad (a Legion Go with one controller detached, a PlayStation pad without Steam Input, most older PC pads) numbers them X 0, A 1, B 2, Y 3, LB 4, RB 5, LT 6, RT 7, Back 8, Start 9, stick presses 10 and 11, with the D-pad as a hat (`PadButtons.Map`). Options → Controls → Controller chooses Automatic (default: D-input for PlayStation pads and pads the game does not know, X-input for Xbox and Nintendo pads), X-input or D-input; the choice is saved with the profile. The menus' confirm and cancel follow it (`Submit (D-input)`, `Cancel (D-input)` in the Input Manager). |
| INPUT-PAD-003 | A pad the Input System knows as a gamepad is read there, by the button's place (A bottom, B right, X left, Y top, LB, RB, Back/Select, Start, L3, R3), in any mode; only other joysticks go through the legacy button numbers and INPUT-PAD-002's mode. Only one of the two is read, so a press never counts twice. The legacy read alone missed LB/RB in the Options on an Xbox One pad (GAP-077). The first press of each button per start is logged with whether the legacy read saw it. The right stick comes from the Input System too (GAP-081). |
| INPUT-PAD-004 | In a puzzle: D-pad and left stick move on the board, the right stick moves the number pad's cursor (only over the board's digits), A places that digit, B held 0.4 s clears the cell, LB opens the bag and LT the relics (again, or B, goes back), RB or X switches Solve and Pencil, L3 or R3 highlights the chosen cell's digit, Start opens the Options, a tap on Select saves and quits (it was a 1.5 s hold). In the bag the D-pad picks an item, A uses it, L3 shows it; among the relics the D-pad picks one and its text shows. On the path: Start opens the Options, B asks to leave, LT/RT scroll, Select saves and quits; A and moving between nodes are the UI's (GAP-080). Nothing in a puzzle stays chosen for the UI, so Enter or A acts once and the arrows cannot reach Options or Save & Quit (GAP-082, GAP-083). The seed field on Select Class takes no pad focus (GAP-078); the path's node tooltip shows at the focused node (GAP-079). |
| INPUT-HINT-001 | The key hints show the pad's buttons after a pad button or stick and the keys after a key, a click or a mouse move (`InputDeviceTracker`, ticked every frame); a pad connected at start (Steam Deck, Legion Go) shows the pad's buttons first. Each hint's catalog key holds the pad's text and `<key>.Keys` the keyboard's: Options, Game Modes, Select Class, Meta Progression, the Codex, the stamp shop, the Daily Walk result, the Custom Puzzle book, the rest and the puzzle screen (RM-207). The marks beside the Options bookmarks read LB/RB with a pad and Q/E on the keyboard, and Q/E turn the pages as PageUp/PageDown do; in a run Q does not save and quit while the Options are open (0.3.1 playtest, GAP-099). |
| INPUT-PAD-005 | A pad the Input System reads confirms and goes back through the Input System on every screen (0.3.1 playtest: "A works only now and then", "B goes back only sometimes"; the player log said "legacy no" for every button, so the UI module's pad axes never fired). `PadUiBridge` submits the chosen control once per A press and marks the press as used, so a screen that reads A too does not act twice; with such a pad the UI module keeps only the keyboard's axes ("Submit (keys)", "Cancel (keys)", "Horizontal (keys)", "Vertical (keys)"). The left stick steps through the menus like the D-pad (`MenuDpad.ReadStick`: a step past 0.6, again after 0.4 s and every 0.14 s while held, released under 0.3) and through the path's nodes. Start opens and closes the Options in the main menu and in a run (not while a key binding waits for its button); B closes them. The Options and the scenes over the path (shop, reward, event, rest) move with the D-pad and the left stick, and their focus starts inside them, so A never presses a control behind them; the keys move there through the UI module. On Select Class, A on a class seal chooses it and moves the focus to Begin (or Abandon & Begin). A floor's painting, the quit screen and the XP brush take a pad button as "any key". |
| INPUT-PAD-006 | While a pad is in use the mouse pointer is hidden (not locked: a locked pointer sits in the middle of the window, so the click that switches back would be lost or land there); a click, a key or a mouse move after the pad has rested (INPUT-HINT-002) brings it back (0.3.1 playtest: "the right stick must not move the mouse; the pointer is inactive while the pad is used"). The game itself never moves the pointer with a stick; a Steam Input template that maps the right stick to the mouse is Steam's setting (GAP-108). |
| INPUT-HINT-002 | The hints switch from the pad to the keys on a click or a key at once, but on a mouse move only after the pad has rested 2 s (`InputDeviceTracker.PadRestSeconds`): a stick that also moves the pointer flipped the hints every frame (0.3.1 playtest: "the controller hint flickers"). |
| INPUT-KEY-001 | In a puzzle, Enter on an empty, open cell with pencil marks clears them ("Pencil marks cleared."); on a cell with a digit it highlights that digit as H does (0.3.1 playtest). |
- `InputRemapService` centralizes keyboard action bindings
- in-run input logic is handled directly inside `InRunController`

## Explicit Non-Features

The checked files do **not** currently implement:

- Steam Input integration
- a shared Unity Input System asset for gameplay/menu actions
- per-device controller remapping
- cloud-synced bindings
