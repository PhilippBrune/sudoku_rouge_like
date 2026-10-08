---
name: procedural-ui
description: Build or modify game UI in this project, where all UI is generated in C# (no prefabs) — blueprint builders, controllers, the generated-UI wiring registry, localization keys, controller/keyboard navigation, accessibility, and palette. Use when adding/changing screens, panels, buttons, HUD, board rendering, or fixing UI layout/navigation bugs.
---

# Procedural UI

All game UI is constructed at runtime in C#. Don't add prefabs or scene-authored UI for game screens.

## Where things live (`Assets/Scripts/UI/`)

| Area | Builder / controller |
|---|---|
| Main menu, class select, options, profiles, game modes | `MainMenuBlueprintBuilder.cs` (+ `.ChallengePanels.cs`), `MainMenuController.cs`, `MainMenuTextPresenter.cs`, `MainMenuClassInfoPresenter.cs` |
| In-run hierarchy | `InRunUiBlueprintBuilder.cs`, `InRunUiFactory.cs`, `InRunController.cs`, `InRunUiFlowController.cs` |
| Board and cells, overlays (lines, cages, dots, markers) | `BoardViewController.cs` |
| HUD (HP / pencil bars, gold) | `HudViewController.cs` |
| Garden map / path | `RunMapController.cs`, `NodeHoverHandler.cs` |
| Boss gate choice | `BossGateViewController.cs` |
| Rewards, shop, end screen | `RewardViewController.cs`, `ShopViewController.cs`, `EndScreenPresenter.cs`/`EndScreenViewController.cs` |
| Colors | `GamePalette.cs`, the single source for UI colors including cell states (`CellLocked`, tutorial highlights) |
| Screen switching | `ScreenManager.cs`, `MenuFlowService.cs` |

## Rules

- **Rebuild-safe**: menus are rebuilt at runtime (e.g. after a language change). Attach listeners during build, never rely on serialized inspector wiring, and don't cache references that a rebuild invalidates.
- **Wiring registry**: register every release-critical control in `GeneratedUiRegistry` *after* its listener is attached (`docs/ui-wiring-registry.md`). New menu actions extend the required-control list. `GeneratedUiWiringContractTests` / `GeneratedUiRegistryTests` enforce this.
- **Localization**: no hard-coded player-facing strings. Add keys to all catalogs in `Assets/Resources/Localization/` (`en`, `de`, `fr`, `es`, `pt`) and follow `docs/localization-catalog.md`. Layout must survive German text at 150% font scale (covered by PlayMode traversal tests).
- **Controller/keyboard**: every interactive control must be reachable via `EventSystem` navigation and `InputRemapService` actions (`WasActionPressed`), including toggles and dropdowns, and must have a sensible first-selected element when a panel opens. Steam Full Controller Support depends on this (`docs/InputAndProfilesSpec.md`).
- **Accessibility**: respect colorblind, high-contrast, reduce-motion, and font-scale options (`docs/AccessibilitySpec.md`). Don't encode meaning by color alone.
- **Sprites**: use the existing procedural sprite helpers (`BoardViewController.BuildCircleSprite/BuildRingSprite`, `InRunUiBlueprintBuilder.BuildWhiteSprite`, white fill sprite in `InRunUiFactory`) or `InRunUiFactory.LoadResourceSprite` with `Resources` paths that exist. Never use `Resources.GetBuiltinResource(... "UI/Skin/Knob.psd")`. A `Text` and an `Image` can't share one GameObject, so use a parent Image with a child Text.
- **Performance**: don't rebuild whole hierarchies per frame or per placement. Update the affected cells and labels (`docs/performance-ui-update-components.md`). `ControllerSizeBudgetTests` guards controller growth, so extract helpers instead of bloating `InRunController`.

## Verifying UI work

UI isn't done until it has been observed:
1. Add or extend a PlayMode harness test that builds the screen, clicks through it via the generated hierarchy, and asserts panel state (pattern: `GeneratedUiPlayModeTraversalTests`). Use the `unity-testing` skill.
2. For visual changes, run the game (built player or Editor play mode) and capture a screenshot at 1920×1080 and a small resolution such as 1280×720. Check overlap, clipping, and readability in English and German.
3. Check the log for `NullReferenceException` / missing-reference errors during the traversal.
