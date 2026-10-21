# Accessibility Specification

**Version:** 2.3  
**Date:** 2026-10-21  
**Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 2.3 | 2026-10-21 | implemented | Accessibility is a page of the Options scroll (UI-STYLE-017) with Text Size and the preview board on its slip; the separate panel is gone. |
| 2.2 | 2026-10-21 | implemented | Options audit (GAP-061): Colorblind Mode uses the Okabe–Ito palette for line colours and no longer simulates colour blindness on the cells (ACCESS-COLOR-003); Text Size scales the board digits and pencil marks (ACCESS-FONT-001); "Screen Reader" is named Status Captions; the gameplay assists act as named (GameDesignSpec 1.15 Options Effects). |
| 2.1 | 2026-10-07 | implemented | Measurable contrast criteria and measured values (SPEC-MAC-006); given/player digit styles and visible toggle states (GAP-029); high-contrast behaviour stated with its multipliers. |

## Overview

The checked-in project ships a focused accessibility set centered on readability, motion reduction, and lightweight interaction feedback. Settings live in `AccessibilitySettings` inside `OptionsState` and are persisted per save slot.

Primary implementation references:

- `Assets/Scripts/Core/SaveModels.cs`
- `Assets/Scripts/Core/AccessibilityService.cs`
- `Assets/Scripts/UI/OptionsController.cs`
- `Assets/Scripts/UI/MainMenuBlueprintBuilder.cs`
- `Assets/Scripts/UI/AccessibilityPreviewController.cs`
- `Assets/Scripts/UI/InRunController.cs`
- `Assets/Scripts/UI/AccessibilityAnnouncementOverlay.cs`

## Implemented Features

| Setting | Runtime Behavior | Notes |
|---|---|---|
| `ColorblindMode` | Line colours that tell rules apart switch to the Okabe–Ito palette (`BoardViewController.LineColorFor`, ACCESS-COLOR-003); cells keep their colours (the old protanopia simulation made them harder to read) | Refreshed live from Options |
| `HighContrastMode` | Replaces cell colours with the `GamePalette.Hc*` set (by cell luminance and conflict state), overlay line width ×1.5 and overlay alpha ×1.4 (`AccessibilityService`) | Refreshed live from Options |
| `ReduceMotion` | Suppresses or reduces motion-heavy feedback and particle effects | `AmbientParticleController.RefreshSettings()` respects it |
| `AlternativeConstraintSymbols` | Swaps to alternate constraint symbols where supported | Applied during board rebuild |
| `FontScale` | Scales the board's digits and pencil marks (within the cell, ACCESS-FONT-001), the preview and accessibility-aware menu text | Stored in `AccessibilitySettings` |
| `ScreenReaderEnabled` | "Status Captions": an on-screen announcement bar for status updates | A caption bar, not OS speech; renamed from "Screen Reader" (GAP-061) |

## Announcement Captions

When `ScreenReaderEnabled` is on, menu and in-run status updates are mirrored into `AccessibilityAnnouncementOverlay`.

Current behavior:

- Appears as a bottom-screen caption bar
- Shows the latest message for about 3 seconds
- Keeps a small in-memory history buffer for the active session
- Triggered from `MainMenuController.SetStatus()` and `InRunController.SetStatus()`

## Accessibility UI

The Accessibility page of the Options scroll (UI-STYLE-017, `OptionsUiFactory.BuildAccessibilitySection`) exposes:

- Colorblind Mode
- High Contrast
- Reduce Motion
- Alternative Symbols
- Text Size (80–150 %)
- Status Captions
- Reset this page

`AccessibilityPreviewController` renders a read-only preview board on the page's slip (main menu) so visual changes can be inspected immediately. The run's Options have the same page without Text Size and the preview.

## Related Gameplay Assist Settings

These are not stored in `AccessibilitySettings`, but they affect usability and are part of the shipped options flow:

- `HighlightConflicts` (Highlight Errors, OPT-GAME-001)
- `KeepPencilMarks` (Auto Pencil Cleanup off, OPT-GAME-002; `AutoPencilCleanup` is kept for old saves)
- `ShowCandidateCount` (Show Digits Left, OPT-GAME-003)
- `CursorSnapOnSelection` (Jump to Next Empty Cell, OPT-GAME-004)
- `DoubleTapConfirmNumberEntry` (Double-Tap to Confirm, OPT-GAME-005)
- `ControllerRumbleEnabled`
- `ConfirmBeforeWrongPlacement` is no longer offered (GAP-061); the field stays for old saves.

Their effects are listed in GameDesignSpec §Options Effects.

Implementation references:

- `Assets/Scripts/UI/OptionsController.cs`
- `Assets/Scripts/Core/SaveModels.cs`

## Measurable Readability Criteria (SPEC-MAC-006, 1.1)

Contrast ratios follow WCAG 2.x relative luminance. Default (non-high-contrast) palette, measured 2026-10-07:

| Element | Foreground | Background | Ratio | Criterion |
|---------|-----------|------------|:----:|:---------:|
| Given digit (bold ivory) | `CellNumberGiven` (0.96, 0.93, 0.82) | `CellGiven` (0.20, 0.29, 0.20) | 8.3:1 | ≥ 4.5:1 |
| Player digit (blue ink) | `CellNumberPlaced` (0.62, 0.87, 1.00) | `CellEmpty` (0.12, 0.18, 0.14) | ≥ 8.6:1 | ≥ 4.5:1 |
| Player digit on row/column highlight | `CellNumberPlaced` | `CellRowCol` (0.18, 0.34, 0.56) | 5.0:1 | ≥ 4.5:1 |
| Any digit on the selected cell | `CellNumberOnSelected` (0.10, 0.08, 0.05) | `CellSelected` (0.73, 0.49, 0.18) | 5.3:1 | ≥ 4.5:1 |
| Locked class name (class select) | (0.72, 0.69, 0.62) | (0.16, 0.15, 0.13) | 7.0:1 | ≥ 4.5:1 |

- Givens and player digits differ in colour **and** weight (bold/regular), so they stay distinguishable without colour vision (GAP-029; before 1.1 givens were near-black at 2:1).
- Toggles always show a framed box; "on" fills it, so the state does not depend on colour alone (GAP-029).
- Overlay geometry can be isolated per modifier with the legend (BossMechanicsSystem §Modifier Legend Panel).
- A new UI colour pair must meet 4.5:1 for text and 3:1 for non-text indicators.

## Explicit Non-Goals In The Current Repo

The following are **not** implemented in the checked files:

- OS-level screen reader bridges
- spoken text-to-speech output
- announcement verbosity/rate controls
- a dedicated announcement history panel
- Steam Input or platform accessibility middleware

This spec intentionally matches the current implementation rather than earlier platform-integration plans.

## Production Validation

Release validation for these features is tracked in
`docs/accessibility-validation.md`. That checklist defines the required
colorblind, high-contrast, reduce-motion, font-scale, alternative-symbol,
caption, and rumble validation matrix for beta and release-candidate builds.
