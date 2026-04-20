# Input, Controller Support & Save Profiles

**Version:** 1.1 | **Date:** 2026-04-16 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.1 | 2026-04-16 | implemented | Inline requirement IDs added (INPUT-REMAP-001–004, INPUT-PROFILE-001/002, SAVE-SLOT-001–003). |
| 1.0 | 2026-04-08 | implemented | Initial spec written alongside implementation. Covers 3-slot save profiles, keybinding rebind UI, and gamepad support. |

---

## 1. Save Profile System (3 slots) `[SAVE-SLOT-001]` `[SAVE-SLOT-002]` `[SAVE-SLOT-003]`

### Goal
Allow up to 3 independent save profiles per device — useful for household sharing or separate challenge runs.

### Architecture

| Class | Location | Role |
|-------|----------|------|
| `SaveFileService` | `Assets/Scripts/Save/SaveFileService.cs` | Per-slot file I/O. Constructor takes `int slotIndex` (0–2). File name: `save_profile_{slot}.json`. |
| `SaveProfileService` | `Assets/Scripts/Save/SaveProfileService.cs` | Cross-slot manager. Reads summaries from all 3 slots, persists active slot in `PlayerPrefs("ActiveProfileSlot")`. |
| `ProfileSlotSummary` | `Assets/Scripts/Core/SaveModels.cs` | Lightweight in-memory summary: class, run count, boss count, last-played timestamp, has-active-run flag. Derived at load time, never persisted separately. |

### Slot file naming

```
{Application.persistentDataPath}/save_profile_0.json   ← default slot
{Application.persistentDataPath}/save_profile_1.json
{Application.persistentDataPath}/save_profile_2.json
```

**Migration note:** The old `save_data.json` file (single-slot) is not auto-migrated. On first launch with the new system the slot 0 file simply does not exist, which triggers the Profile Select screen.

### Profile Select screen `[INPUT-PROFILE-001]`

- Shown automatically on first launch (`!profileSlots.AnySlotExists()`) before the main menu.
- Also accessible via the **Profiles** button on the main menu (10th button, between Items and Options).
- 3 side-by-side slot cards, each showing: name, last class played, run stats, last-played date, "run in progress" badge.
- Active slot is highlighted with a gold outline.
- **Load** button: calls `MainMenuController.SelectProfileSlot(i)` → `SaveProfileService.SelectSlot(i)` → re-wires all save services via `GameBootstrap.ApplySlotChange()` → navigates to main menu. `[INPUT-PROFILE-002]`
- **Delete** button: calls `SaveProfileService.DeleteSlot(i)` → deletes the file, refreshes cards.

### `MenuScreen` additions

| Value | Purpose |
|-------|---------|
| `ProfileSelect` | Profile selection / creation / deletion panel |
| `Keybindings` | Keyboard + controller rebinding panel |

---

## 2. Controller / Gamepad Support `[INPUT-REMAP-001]`

### Architecture

All input flows through `InputRemapService` (`Assets/Scripts/Core/InputRemapService.cs`).

### `WasActionPressed(InputAction action)` — query order `[INPUT-REMAP-002]`

1. Player's custom keyboard binding (from `_overrides`, falls back to `Defaults`)
2. Fixed gamepad button (`GamepadDefaults` — not rebindable, always active)
3. Left-stick / D-pad axis crossing threshold (movement actions only)

### Default keyboard bindings

| Action | Key |
|--------|-----|
| Move Up/Down/Left/Right | Arrow keys |
| Digits 1–9 | NumRow 1–9 |
| Toggle Pencil | P |
| Clear Cell | Delete |
| Undo | Z |
| Confirm | Return |
| Cancel | Escape |
| Tab Next / Previous | Tab / Left Shift |
| Skip Animation | Space |

### Default gamepad bindings (Xbox / generic layout)

| Action | Button |
|--------|--------|
| Move Up/Down/Left/Right | D-pad (JoystickButton12–15) OR left stick (±Horizontal/Vertical axis) |
| Toggle Pencil | X / Square (Button 2) |
| Clear Cell | B / Circle (Button 1) |
| Undo | LB / L1 (Button 4) |
| Confirm | A / Cross (Button 0) |
| Cancel | B / Circle (Button 1) |
| Skip Animation | Start (Button 7) |
| Tab Next / Previous | RB / LB (Buttons 5 / 4) |
| Digits 1–9 | Not mapped — use on-screen numpad (navigated by stick / d-pad) |

**Note:** Gamepad defaults are read-only and not user-rebindable in the current implementation. Only keyboard bindings can be rebound.

### Axis debounce

`InputRemapService.Tick()` must be called once per frame from a `MonoBehaviour.Update()` (e.g. `InRunInputController`). It resets axis "pressed" state so a held stick only fires once per crossing, not every frame.

---

## 3. Keybinding Rebind UI `[INPUT-REMAP-004]`

### Panel

- Accessible via **Keybindings...** button in the Options panel.
- `MenuScreen.Keybindings` registered in `MenuFlowService`.
- 12 rebindable actions displayed in a scrollable table with alternating row backgrounds.
- Columns: **Action name** | **Current key** | **Gamepad default (read-only)** | **Rebind button**

### `KeybindingsPanelController` (`Assets/Scripts/UI/KeybindingsPanelController.cs`)

| Method | Description |
|--------|-------------|
| `Configure(remap, statusText)` | Inject `InputRemapService` and status label |
| `RegisterLabel(action, text)` | Builder registers each row's key-label for live refresh |
| `StartListening(action)` | Enters "press any key" mode for that action |
| `ResetAll()` | Calls `remap.ResetAllBindings()`, refreshes all labels |
| `Update()` | Polls `InputRemapService.GetAnyKeyDown()`; Escape cancels |

### Rebind flow

1. Player clicks **Rebind** on a row → `StartListening(action)` → status bar shows prompt.
2. Next key press captured via `InputRemapService.GetAnyKeyDown()`.
3. If Escape → cancel (binding unchanged).
4. Otherwise → `remap.SetBinding(action, key)` + refresh label + status "Binding saved."
5. Bindings persisted immediately to `PlayerPrefs("InputOverrides")`.

### `InputRemapService.GetAnyKeyDown()` (static) `[INPUT-REMAP-003]`

Iterates all `KeyCode` values and returns the first one for which `Input.GetKeyDown` is true. Works for both keyboard keys and joystick buttons — so controller face buttons can also be bound to keyboard actions.

---

## 4. Roguelike Completeness Gaps (audit summary)

From the full codebase audit, the major missing/stubbed areas in the roguelike design are:

| Area | Status | Notes |
|------|--------|-------|
| Story / Narrative | ⚠ Minimal | 3 hardcoded events; no dialogue trees, cutscenes, or lore system. Expand `RunEventService` with a larger event pool and branching choices. |
| Curse System | ⚠ Stub | `CurseService.HasActiveCurse` always returns false. Awaits `CurseSystem.md` spec completion. |
| Event pool | ⚠ Thin | Only 3 event types (Merchant, Shrine, Ink Pot). Design doc `GameDesignSpec.md` describes `Sacrifice`, `RiskAmplification`, `ResourceTrade` categories — events for these need authoring. |
| Cloud save | ⚠ Stub | `LocalCloudSaveProvider` is a no-op. Steam cloud sync class exists but not wired. |
| Achievement UI | ⚠ Backend only | `SteamAchievementService` and `MasteryService` exist; no in-game achievement display screen. |
| Extended modifier pool | ⚠ Enum only | IDs 30–93 are defined in `BossModifierId` but have no constraint rules, geometry, or `BossService` entries. |
| Boss debuffs (remaining) | ⚠ Partial | BoxWipe, CrossWipe, FogPulse, GoldFine, PencilDrain not yet implemented (see `BossDebuffEffects.md`). |
| Sprite art | ⚠ Placeholder | All icons are generated PNGs. Final art pass blocked on art assets. |
| Prefab migration (V7) | ⚠ Blocked | Requires Unity Editor Play Mode capture — not a code task. |
| Controller digit input | ⚠ Partial | On-screen numpad navigable by stick, but digits 1–9 have no direct gamepad button mapping. |

---

## Requirements Traceability

| REQ-ID | Section | Code File | Status |
|--------|---------|-----------|--------|
| `SAVE-SLOT-001` | Save Profile System — slot file naming | `Assets/Scripts/Save/SaveFileService.cs` · constructor | ✅ |
| `SAVE-SLOT-002` | Save Profile System — cross-slot manager | `Assets/Scripts/Save/ProfileService.cs` · `SaveProfileService` | ✅ |
| `SAVE-SLOT-003` | Save Profile System — slot change re-wire | `Assets/Scripts/Bootstrap/GameBootstrap.cs` · `ApplySlotChange` | ✅ |
| `INPUT-PROFILE-001` | Profile Select screen — first launch | `Assets/Scripts/Bootstrap/GameBootstrap.cs` · `Start` | ✅ |
| `INPUT-PROFILE-002` | Profile Select screen — Load/Delete buttons | `Assets/Scripts/UI/MainMenuController.cs` · `SelectProfileSlot`, `DeleteProfileSlot`, `RefreshProfileSelectCards` | ✅ |
| `INPUT-REMAP-001` | Controller / Gamepad Support — `InputRemapService` | `Assets/Scripts/Core/InputRemapService.cs` · constructor | ✅ |
| `INPUT-REMAP-002` | Controller — `WasActionPressed` query order | `Assets/Scripts/Core/InputRemapService.cs` · `WasActionPressed` | ✅ |
| `INPUT-REMAP-003` | Keybinding — `GetAnyKeyDown` | `Assets/Scripts/Core/InputRemapService.cs` · `GetAnyKeyDown` | ✅ |
| `INPUT-REMAP-004` | Keybinding Rebind UI — `KeybindingsPanelController` | `Assets/Scripts/UI/KeybindingsPanelController.cs` | ✅ |
