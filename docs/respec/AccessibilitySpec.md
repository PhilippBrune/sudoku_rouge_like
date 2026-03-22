# Accessibility Specification

**Version:** 1.1 | **Date:** 2026-03-22 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.1 | 2026-03-22 | implemented | All features implemented: AccessibilityService, colorblind line patterns, high contrast, font scaling, reduce motion, alt constraint symbols, keyboard navigation, input remapping |
| 1.0 | 2026-03-21 | review | Initial specification |

---

## Overview

Run of the Nine provides accessibility options to ensure the game is playable for users with visual impairments, motor limitations, and sensory sensitivities. All accessibility settings are stored in `AccessibilitySettings` and persisted in the profile save.

---

## Settings Model

```csharp
AccessibilitySettings
{
    bool  ColorblindMode;                  // default: false
    bool  HighContrastMode;                // default: false
    float FontScale;                       // default: 1.0 (range 0.8–1.5)
    bool  ReduceMotion;                    // default: false
    bool  AlternativeConstraintSymbols;    // default: false
}
```

All settings take effect immediately on toggle — no restart required.

---

## Colorblind Mode

**Setting:** `ColorblindMode = true`

### Problem

Several game systems rely on colour to convey information:
- Modifier overlay lines (7 distinct hues)
- Even/Odd cell markers (blue square vs orange circle)
- Kropki dots (white hollow vs black filled)
- HP bar (green → yellow → red gradient)
- Item rarity borders (silver, gold, cyan/purple)
- Relic tier borders (silver → gold → prismatic)
- Combo streak indicators

### Changes When Enabled

| System | Normal Mode | Colorblind Mode |
|--------|-------------|-----------------|
| **Modifier lines** | Colour-only differentiation | Colour + **pattern overlay**: dashed, dotted, dash-dot, double-line, wavy, thick, thin per modifier type |
| **Even/Odd markers** | Blue square / Orange circle | Blue square with **"E" label** / Orange circle with **"O" label** |
| **Kropki dots** | White hollow / Black filled | White hollow with **"1" label** (diff=1) / Black filled with **"×" label** (ratio) |
| **HP bar** | Green → Red gradient | Single colour with **numeric HP text overlay** always visible |
| **Item rarity** | Border colour only | Border colour + **rarity icon badge** (star count: ★, ★★, ★★★) |
| **Relic tier** | Border glow intensity | Border + **tier number badge** (T1, T2, T3, T4, L) |
| **Combo counter** | Colour pulse | Colour pulse + **size increase** (scale 1.0 → 1.2 at 10+) |
| **Fog of War** | Dark overlay | Dark overlay + **diagonal hatch pattern** |

### Modifier Line Patterns

When colorblind mode is active, each modifier line type gets a unique stroke pattern in addition to colour:

| Modifier | Colour | Pattern |
|----------|--------|---------|
| German Whispers | Green | Solid thick (3px) |
| Dutch Whispers | Teal | Dashed (long dash) |
| Parity Lines | Blue | Dotted |
| Renban Lines | Pink | Dash-dot |
| Palindrome | Grey | Double line |
| Thermo | Warm gradient | Solid with arrow markers |
| Between Lines | White | Wavy |

---

## High Contrast Mode

**Setting:** `HighContrastMode = true`

### Changes When Enabled

| Element | Normal Mode | High Contrast Mode |
|---------|-------------|-------------------|
| **Grid background** | Soft cream/white | Pure white `#FFFFFF` |
| **Grid borders** | Soft grey | Black `#000000` (2px) |
| **Given digits** | Dark grey | Bold black |
| **Player-placed digits** | Blue-grey | Dark blue, bold |
| **Pencil marks** | Light grey, small | Dark grey, slightly larger |
| **Selected cell** | Soft blue highlight | Bright yellow highlight with 3px black border |
| **Error highlight** | Muted red | Bright red with pulsing border |
| **Modifier overlays** | Semi-transparent (0.55 alpha) | Higher opacity (0.85 alpha) + thicker lines (2× width) |
| **Fog of War cells** | Near-black | Solid black with white "?" in center |
| **Menu backgrounds** | Semi-transparent dark | Solid dark `(0.02, 0.02, 0.04, 0.95)` |
| **Button text** | Soft white | Pure white, bold |
| **Button hover** | 8% white overlay | 20% white overlay + border highlight |

### Contrast Ratios

High contrast mode targets **WCAG AA** compliance (minimum 4.5:1 contrast ratio for normal text, 3:1 for large text):
- Given digits on white background: 21:1 (black on white)
- Player digits on white: 8.6:1 (dark blue on white)
- Selected cell border on background: 21:1 (black on yellow)

---

## Font Scaling

**Setting:** `FontScale` (float, range 0.8–1.5, default 1.0)

### Behaviour

All text elements in the game scale by this multiplier:

| Element | Base Size | At 0.8× | At 1.5× |
|---------|:---:|:---:|:---:|
| Grid digits | 24pt | 19pt | 36pt |
| Pencil marks | 10pt | 8pt | 15pt |
| HUD text (HP, Pencil, Gold) | 16pt | 13pt | 24pt |
| Menu text | 18pt | 14pt | 27pt |
| Button labels | 16pt | 13pt | 24pt |
| Modifier descriptions | 14pt | 11pt | 21pt |
| Cage sum labels | 12pt | 10pt | 18pt |

### Layout Adaptation

- Grid cell size adjusts to accommodate larger digits (cells grow proportionally)
- HUD elements reflow if text exceeds container bounds
- Menu panels expand vertically to fit scaled text
- Minimum font size floor: 8pt (even at 0.8× scale, nothing goes below 8pt)

---

## Reduce Motion

**Setting:** `ReduceMotion = true`

### Changes When Enabled

| Animation | Normal | Reduced Motion |
|-----------|--------|----------------|
| **Ambient particles** (petals, dust, ripples) | Active per floor theme | Completely disabled |
| **Screen shake** | On mistake, boss activation | Disabled |
| **XP bar fill animation** | 1500ms smooth fill | Instant fill (no animation) |
| **Level-up burst** | 500ms particle burst | Simple text flash (200ms) |
| **Item reward appear** | 300ms bounce scale | Instant appear |
| **Floor transition** | 800ms fade-out/in | 200ms cut (minimal fade) |
| **Combo counter pulse** | 100ms scale bounce | No pulse (static number) |
| **Relic reveal** | 400ms fade + scale | Instant appear |
| **Cell highlight** | 100ms ease-out | Instant highlight (no transition) |
| **Number placement** | 150ms ease-in-out | Instant appear |
| **Error flash** | 200ms red flash | Static red border (no flash) |
| **HP/Pencil bar** | 300ms/200ms smooth | Instant snap to new value |
| **Modifier legend isolate** | 200ms dim/restore | Instant dim/restore |
| **Menu panel open/close** | 200ms ease-out | Instant show/hide |

### Performance Benefit

Reduce Motion also improves performance on low-end hardware by eliminating particle systems and animation coroutines.

---

## Alternative Constraint Symbols

**Setting:** `AlternativeConstraintSymbols = true`

### Problem

Some modifier overlays rely on shape/colour alone to convey constraint type. Players with visual processing differences may struggle to distinguish between similar-looking overlays (e.g. different line types, dot types).

### Changes When Enabled

| Constraint | Normal Symbol | Alternative Symbol |
|-----------|---------------|-------------------|
| **Difference Kropki** | White hollow circle | White circle with **"1"** text |
| **Ratio Kropki** | Black filled circle | Black circle with **"×"** text |
| **Even marker** | Blue square | Blue square with **"E"** text |
| **Odd marker** | Orange circle | Orange circle with **"O"** text |
| **Killer cage sum** | Number in corner | Number in corner + **dashed cage outline thickened to 2px** |
| **Arrow circle** | Grey hollow circle | Grey circle with **"Σ"** text (sum indicator) |
| **Thermo bulb** | Circle at line start | Circle with **"▲"** arrow pointing along thermo direction |
| **Between Line endpoints** | Circles at ends | Circles with **"↕"** between indicator |
| **Fog cells** | Dark overlay | Dark overlay with **"?"** text in each fogged cell |

This setting overlaps with Colorblind Mode — both add text labels. When both are active, labels are shown once (not duplicated).

---

## Gameplay Assists

These are in `GameplaySettings` (not `AccessibilitySettings`) but serve accessibility purposes:

| Setting | Default | Effect |
|---------|:---:|--------|
| `ConfirmBeforeWrongPlacement` | false | Shows confirmation dialog before placing a wrong number (prevents accidental HP loss) |
| `DoubleTapConfirmNumberEntry` | false | Requires two taps/clicks to confirm a number placement |
| `AutoPencilCleanup` | false | Automatically removes pencil marks that conflict with a placed number |
| `HighlightConflicts` | true | Highlights cells that violate constraints in real-time |
| `ShowCandidateCount` | true | Shows the number of valid candidates per cell in a subtle indicator |
| `CursorSnapOnSelection` | false | Cursor snaps to cell center on selection (reduces precision needed) |

---

## Keyboard Navigation

Full keyboard navigation is supported for all game screens:

| Context | Keys | Action |
|---------|------|--------|
| Grid | Arrow keys | Move selection cursor |
| Grid | 1–9, Numpad | Place number |
| Grid | P | Toggle pencil mode |
| Grid | Delete/Backspace | Clear cell |
| Grid | CTRL+Z | Undo last action |
| Grid | Space | Skip XP animation (end-of-run) |
| Menus | Tab / Shift+Tab | Navigate between buttons |
| Menus | Enter / Space | Activate focused button |
| Menus | Escape | Back / Close panel |
| Dialogs | Left/Right arrows | Switch between dialog options |
| Dialogs | Enter | Confirm selection |

All interactive elements have a visible **focus indicator** (highlighted border) when navigated via keyboard.

---

## Text-to-Value Alternatives

For players who struggle with spatial puzzles, the following display aids are always available (not behind a toggle):

| Aid | Description |
|-----|-------------|
| **Candidate count badge** | Small number in cell corner showing how many valid candidates remain |
| **Region colouring** | Alternating subtle background tints for different regions (aids region boundary perception) |
| **Row/Column highlight** | Selected cell's row and column are subtly highlighted |

---

## Source Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Core/RuntimeModels.cs` | `AccessibilitySettings`, `GameplaySettings` data models |
| `Assets/Scripts/Save/SaveFileService.cs` | Settings persistence and sanitization |
| `Assets/Scripts/UI/MainMenuController.cs` | Options panel wiring |
| `Assets/Scripts/UI/PrototypeRunScreenController.cs` | In-puzzle rendering (applies accessibility overrides) |

---

## Screen Reader Support

### Architecture

A custom `AccessibilityAnnouncer` singleton provides text-to-speech announcements via the OS accessibility API. Unity does not have built-in screen reader support, so this uses platform-specific bridges.

### Platform Bridges

| Platform | API | Notes |
|----------|-----|-------|
| Windows | `UI Automation` via `AutomationPeer` or NVDA/JAWS COM bridge | Most common screen readers on PC |
| macOS | `NSAccessibility` protocol | VoiceOver integration |
| Linux | `AT-SPI2` D-Bus interface | Orca screen reader |

Fallback: If no screen reader is detected, announcements are displayed as **on-screen text captions** in a dedicated announcement bar at the bottom of the screen (auto-dismiss after 3s).

### Announcement Triggers

| Context | Announcement Text |
|---------|-------------------|
| **Cell selected** | "Row {r}, Column {c}. {value or 'empty'}. {candidateCount} candidates." |
| **Number placed (correct)** | "Placed {value}. Correct." |
| **Number placed (wrong)** | "Placed {value}. Incorrect. {currentHP} HP remaining." |
| **Pencil mark added** | "Pencil mark {value} in row {r}, column {c}." |
| **Pencil mark removed** | "Pencil mark {value} removed." |
| **Cell cleared** | "Cell cleared. Row {r}, column {c}." |
| **Modifier active** | "Modifier active: {modifierName}. {shortDescription}." |
| **Fog revealed** | "Fog cleared. {count} cells revealed." |
| **HP change** | "{currentHP} of {maxHP} HP." |
| **Pencil change** | "{currentPencil} pencil marks remaining." |
| **Gold change** | "{currentGold} gold." |
| **Combo streak** | "Combo: {count}." |
| **Item used** | "Used {itemName}. {effect}." |
| **Puzzle complete** | "Puzzle complete. {stars} star, {boardSize} board." |
| **Level up** | "Level up! Class {className} now level {level}." |
| **Boss gate** | "Boss gate. Choose {pickCount} of {optionCount} modifiers." |
| **Menu focus** | "{buttonLabel}. {position} of {total}." |

### Settings

| Setting | Default | Description |
|---------|:---:|-----------|
| `ScreenReaderEnabled` | false | Master toggle for all announcements |
| `AnnouncementVerbosity` | Medium | Low: actions only / Medium: + context / High: + candidates and constraints |
| `AnnouncementRate` | 1.0 | Speech rate multiplier (0.5–2.0) |

### Implementation Notes

- `AccessibilityAnnouncer.Announce(string text, Priority priority)` queues announcements
- Priority levels: `Low` (ambient info), `Medium` (user actions), `High` (errors, HP loss) — higher priority interrupts lower
- Announcement queue processes one at a time; new High priority flushes queue
- All announcements also logged to an in-game **announcement history panel** (scrollable, last 20 entries)

---

## Input Remapping

### Architecture

Uses Unity's **Input System** package (`com.unity.inputsystem`) for rebindable action maps.

### Action Map

All game inputs are defined as named actions in an `InputActionAsset`:

| Action | Default Keyboard | Category |
|--------|-----------------|----------|
| `MoveUp` | Arrow Up | Grid Navigation |
| `MoveDown` | Arrow Down | Grid Navigation |
| `MoveLeft` | Arrow Left | Grid Navigation |
| `MoveRight` | Arrow Right | Grid Navigation |
| `SelectCell` | Left Click / Enter | Grid Navigation |
| `Number1`–`Number9` | 1–9 / Numpad 1–9 | Number Input |
| `ClearCell` | Delete / Backspace | Number Input |
| `TogglePencil` | P | Mode Toggle |
| `Undo` | CTRL+Z | Edit |
| `Redo` | CTRL+Y | Edit |
| `SelectAll` | CTRL+A | Selection |
| `DeselectAll` | CTRL+SHIFT+A | Selection |
| `InvertSelection` | CTRL+I | Selection |
| `ExpandSelectionUp/Down/Left/Right` | CTRL+Arrow | Selection |
| `OpenPauseMenu` | Escape | UI |
| `SkipAnimation` | Space | UI |
| `NavigateMenuNext` | Tab | UI |
| `NavigateMenuPrev` | Shift+Tab | UI |
| `ConfirmMenu` | Enter / Space | UI |
| `BackMenu` | Escape | UI |

### Rebinding UI

The Options → Controls panel provides a rebinding interface:

```
┌──────────────────────────────────────┐
│  Controls                            │
│                                      │
│  Grid Navigation                     │
│  ├─ Move Up      [↑]     [Rebind]   │
│  ├─ Move Down    [↓]     [Rebind]   │
│  ├─ Move Left    [←]     [Rebind]   │
│  └─ Move Right   [→]     [Rebind]   │
│                                      │
│  Number Input                        │
│  ├─ Number 1     [1]     [Rebind]   │
│  ├─ ...                              │
│  └─ Clear Cell   [Del]   [Rebind]   │
│                                      │
│  Mode Toggle                         │
│  └─ Pencil Mode  [P]     [Rebind]   │
│                                      │
│  [ Reset All to Defaults ]           │
└──────────────────────────────────────┘
```

### Rebind Flow

1. Player clicks **[Rebind]** next to an action
2. Button text changes to **"Press a key..."**
3. Next key/button press is captured via `InputActionRebindingExtensions.PerformInteractiveRebinding()`
4. If the key conflicts with another binding, show warning: "Already bound to {action}. Swap?" → Yes swaps both, No cancels
5. New binding is stored in `PlayerPrefs` as JSON override (survives restarts)

### Persistence

- Rebindings stored via `InputActionAsset.SaveBindingOverridesAsJson()` → saved to profile
- On load: `InputActionAsset.LoadBindingOverridesFromJson()` restores custom bindings
- "Reset All" calls `InputActionAsset.RemoveAllBindingOverrides()` and saves

---

## Controller Support

### Architecture

Uses **Steam Input API** for flexible controller mapping, supporting Xbox, PlayStation, Switch Pro, and Steam Controller natively.

### Steam Input Action Sets

Two action sets for different game contexts:

**Action Set: Gameplay**

| Action | Type | Default Xbox | Default PS | Description |
|--------|------|-------------|-----------|-------------|
| `GridMove` | D-Pad / Left Stick | D-Pad | D-Pad | Move cell cursor |
| `GridSelect` | Button | A | Cross | Confirm cell selection |
| `NumberCycle` | Bumpers | LB/RB cycle 1-9 | L1/R1 cycle 1-9 | Cycle through digits |
| `NumberConfirm` | Button | A | Cross | Place currently displayed number |
| `ClearCell` | Button | X | Square | Clear selected cell |
| `TogglePencil` | Button | Y | Triangle | Toggle pencil mode |
| `Undo` | Button | B | Circle | Undo last action |
| `OpenMenu` | Button | Start | Options | Open pause menu |
| `QuickSelect1-9` | Right Stick flick (8 directions + click) | Flick directions map to 1-8, click = 9 | Same | Fast number entry without cycling |

**Action Set: Menus**

| Action | Type | Default Xbox | Default PS | Description |
|--------|------|-------------|-----------|-------------|
| `Navigate` | D-Pad / Left Stick | D-Pad | D-Pad | Move between UI elements |
| `Confirm` | Button | A | Cross | Activate focused element |
| `Back` | Button | B | Circle | Go back / close panel |
| `TabLeft` | Trigger | LT | L2 | Previous tab/page |
| `TabRight` | Trigger | RT | R2 | Next tab/page |

### Number Entry System (Controller)

Since controllers lack a numpad, two input methods are provided:

**Method 1 — Cycle Mode (Default):**
- LB/RB cycles the "pending digit" displayed on the selected cell (1→2→...→9→1)
- A/Cross confirms the pending digit
- Visual: large digit preview appears above the cell while cycling

**Method 2 — Radial Menu:**
- Hold RT/R2 to open a radial menu showing digits 1–9 in a circle
- Left stick points to desired digit, release RT/R2 to confirm
- Visual: circular overlay centered on selected cell

Player chooses preferred method in Options → Controls → Controller Number Entry.

### Haptic Feedback

| Event | Vibration | Duration |
|-------|-----------|:---:|
| Correct placement | Soft pulse (left motor 20%) | 50ms |
| Wrong placement | Sharp double pulse (both motors 60%) | 100ms |
| Combo milestone (5, 10, 20) | Gentle rumble (right motor 30%) | 150ms |
| Level complete | Satisfying pulse (both 40%) | 200ms |
| HP critical (≤ 2) | Sustained low rumble (left 15%) | 500ms |

Haptic feedback respects `ReduceMotion` — disabled when active.

### Steam Input Configuration

```
Steam Input Config (VDF):
    "actions": {
        "Gameplay": {
            "GridMove": { "type": "stick_padgyro" },
            "NumberCycle": { "type": "button" },
            ...
        },
        "Menus": {
            "Navigate": { "type": "stick_padgyro" },
            ...
        }
    }
```

- Steam Input handles all controller types transparently
- Players can customize via Steam's Big Picture controller configurator
- In-game glyphs update dynamically based on connected controller type (Xbox icons, PS icons, etc.)

### Controller Glyph Display

When a controller is detected, all UI button prompts switch from keyboard labels to controller glyphs:
- Xbox: A/B/X/Y coloured buttons
- PlayStation: Cross/Circle/Square/Triangle symbols
- Switch: A/B/X/Y (different layout)
- Generic: numbered button labels

Glyph sprites loaded from `Resources/ControllerGlyphs/{platform}/` at runtime.

---

## Open Issues

No open issues remaining.
