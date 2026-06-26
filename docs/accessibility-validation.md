# Accessibility Validation

## Purpose

This checklist defines the validation required before accessibility support can
be treated as production-ready. The project implements accessibility settings,
but release readiness still requires test coverage, visual review, and runtime
Unity validation.

## Current Status

- Settings persistence: covered by `OptionsPersistenceTests`.
- First-run accessibility choices: covered by `FirstRunSetupPlayModeTests`.
- Generated options navigation: covered by `GeneratedUiPlayModeTraversalTests`.
- German 150% generated UI traversal: covered by
  `GeneratedUiPlayModeTraversalTests.GermanLargeFont_GeneratedMenusBuildAndNavigateWithoutMissingLocalization`.
- Service behavior: covered by `AccessibilityReadinessTests`.
- Full rendered UI review: still requires Unity PlayMode screenshots and manual
  assistive workflow validation.

## Required Accessibility Matrix

| ID | Area | Required Validation | Current Evidence |
| --- | --- | --- | --- |
| ACCESS-COLOR | Colorblind mode | Conflict, given, entered, and overlay colors remain distinguishable. | `AccessibilityService.GetCellColor`, `AccessibilityPreviewController` |
| ACCESS-CONTRAST | High contrast | Board cells, conflict states, overlay lines, and text remain readable. | `AccessibilityService.GetOverlayAlpha`, `GetLineWidthMultiplier` |
| ACCESS-MOTION | Reduce motion | Screen shake, ambient particles, and motion-heavy feedback are reduced or suppressed. | `AccessibilityService.GetAnimationDuration`, `InRunController`, `AmbientParticleController` |
| ACCESS-FONT | Font scale | Text remains usable from 0.8x through 1.5x. | `OptionsController.SetFontScale`, `AccessibilityPreviewController` |
| ACCESS-SYMBOLS | Alternative symbols | Constraint overlays show non-color-only cues where supported. | `BoardViewController`, `BossGateViewController` |
| ACCESS-CAPTIONS | Screen reader captions | Important status updates are mirrored to caption overlay history. | `AccessibilityAnnouncementOverlay`, `MainMenuController`, `InRunController` |
| ACCESS-RUMBLE | Controller rumble | Rumble can be disabled and stops immediately. | `OptionsController.SetControllerRumbleEnabled`, `RumbleService` |

## Release Requirements

Before beta or release candidate:

1. Run EditMode and PlayMode tests through Unity batchmode.
2. Capture screenshots for English and German at 100% and 150% font scale.
3. Verify colorblind and high-contrast previews in the options screen.
4. Verify reduce-motion suppresses screen shake and ambient particle effects.
5. Verify caption overlay appears only when screen reader captions are enabled.
6. Verify controller rumble can be disabled before and during a run.
7. Record any unsupported platform-level accessibility integrations as explicit
   non-goals in `docs/AccessibilitySpec.md`.

## Non-Goals For Current Release Scope

The current project does not provide OS-level screen reader integration,
text-to-speech, configurable announcement verbosity, or Steam Input integration.
Those features require a separate platform accessibility milestone.
