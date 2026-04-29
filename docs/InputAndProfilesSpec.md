# Input, Controller Support & Save Profiles

**Version:** 2.0  
**Date:** 2026-04-27  
**Status:** implemented

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

## Main Menu and In-Run Input Model

Current implementation split:

- `MainMenuController` handles menu navigation, confirmation, cancel/back, and overlays
- `InputRemapService` centralizes keyboard action bindings
- in-run input logic is handled directly inside `InRunController`

## Explicit Non-Features

The checked files do **not** currently implement:

- Steam Input integration
- a shared Unity Input System asset for gameplay/menu actions
- per-device controller remapping
- cloud-synced bindings
