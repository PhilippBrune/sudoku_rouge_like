# Accessibility Specification

**Version:** 2.0  
**Date:** 2026-04-27  
**Status:** implemented

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
| `ColorblindMode` | Recolours high-risk conflict visuals through `AccessibilityService` | Refreshed live from Options |
| `HighContrastMode` | Boosts board/overlay contrast and line readability | Refreshed live from Options |
| `ReduceMotion` | Suppresses or reduces motion-heavy feedback and particle effects | `AmbientParticleController.RefreshSettings()` respects it |
| `AlternativeConstraintSymbols` | Swaps to alternate constraint symbols where supported | Applied during board rebuild |
| `FontScale` | Scales accessibility-aware UI text and preview content | Stored in `AccessibilitySettings` |
| `ScreenReaderEnabled` | Enables an on-screen announcement bar for status updates | Implemented as caption fallback, not OS speech |

## Announcement Captions

When `ScreenReaderEnabled` is on, menu and in-run status updates are mirrored into `AccessibilityAnnouncementOverlay`.

Current behavior:

- Appears as a bottom-screen caption bar
- Shows the latest message for about 3 seconds
- Keeps a small in-memory history buffer for the active session
- Triggered from `MainMenuController.SetStatus()` and `InRunController.SetStatus()`

## Accessibility UI

The Accessibility panel is built in `MainMenuBlueprintBuilder` and currently exposes:

- Colorblind Mode
- High Contrast
- Reduce Motion
- Alternative Symbols
- Font Scale
- Screen Reader
- Reset to Defaults

`AccessibilityPreviewController` renders a read-only preview board so visual changes can be inspected immediately.

## Related Gameplay Assist Settings

These are not stored in `AccessibilitySettings`, but they affect usability and are part of the shipped options flow:

- `HighlightConflicts`
- `ConfirmBeforeWrongPlacement`
- `DoubleTapConfirmNumberEntry`
- `AutoPencilCleanup`
- `ShowCandidateCount`
- `CursorSnapOnSelection`
- `ControllerRumbleEnabled`

Implementation references:

- `Assets/Scripts/UI/OptionsController.cs`
- `Assets/Scripts/Core/SaveModels.cs`

## Explicit Non-Goals In The Current Repo

The following are **not** implemented in the checked files:

- OS-level screen reader bridges
- spoken text-to-speech output
- announcement verbosity/rate controls
- a dedicated announcement history panel
- Steam Input or platform accessibility middleware

This spec intentionally matches the current implementation rather than earlier platform-integration plans.
