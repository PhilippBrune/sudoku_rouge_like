# Input, Controller Support & Save Profiles

**Version:** 2.3  
**Date:** 2026-10-25  
**Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
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
- `InputRemapService` centralizes keyboard action bindings
- in-run input logic is handled directly inside `InRunController`

## Explicit Non-Features

The checked files do **not** currently implement:

- Steam Input integration
- a shared Unity Input System asset for gameplay/menu actions
- per-device controller remapping
- cloud-synced bindings
