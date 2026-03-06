# In-Game UI Flow Setup (Event / Curse / Heat Curve)

This document provides an implementation-ready English version of the given specification, adhering to the output rules and project context provided.

## Assumptions/Clarifications
The original source provided was not ambiguous, so no clarifications were necessary.

## Scripts Added
- `Assets/Scripts/UI/InRunUiFlowController.cs`
- `Assets/Scripts/UI/EventChoiceScreenController.cs`
- `Assets/Scripts/UI/CursePanelController.cs`
- `Assets/Scripts/UI/HeatCurveGraphController.cs`
- `Assets/Scripts/UI/InRunUiBlueprintBuilder.cs`

## Quick Start (One-Click Blueprint)

1. Create an empty GameObject named `InRunUiBuilder` in your scene.
2. Attach `InRunUiBlueprintBuilder` to this object.
3. Drag your existing `RunMapController` into the builder's `Run Map Controller` field.
4. Open the context menu of the builder object and run the **Build In-Run UI Blueprint** command.

The outcome will be:
- Creation/update of `Canvas/InRunUI`
- Addition and wiring of `InRunUiFlowController`
- Creation and wiring of Event/Curse/Heat panels with default anchors, sizes, fonts, and colors

## Default Blueprint Layout

- `EventPanel`: anchors `(0.12,0.12)  -> (0.88,0.78)`
- `CursePanel`: anchors `(0.02,0.58) -> (0.30,0.96)`
- `HeatGraphPanel`: anchors `(0.32,0.76) -> (0.98,0.96)`

All panels use semi-transparent dark background and readable default typography (Arial).

## 1) Scene Hierarchy (Recommended)

Create a UI root:

- `Canvas`
  - `InRunUI` (attach `InRunUiFlowController`)
  - `EventPanel` (attach `EventChoiceScreenController`)
  - `CursePanel` (attach `CursePanelController`)
  - `HeatGraphPanel` (attach `HeatCurveGraphController`)

Assign the existing `RunMapController` reference to `InRunUiFlowController`.

## 2) Event Choice Screen Wiring

`EventChoiceScreenController` fields:
- `panelRoot`: Event panel root object
- `promptText`: prompt text UI
- `resultText`: status/result line
- `optionsRoot`: container with layout group
- `optionButtonPrefab`: button prefab with child `Text`

Runtime flow:
- Call `InRunUiFlowController.OnNodeEntered(NodeType.Event)`
- The event panel opens, choices are instantiated
- Selecting a choice resolves through `RunMapController.ChooseEventOption(...)`
- Call `OnEventClosed()` when the player closes the panel

## 3) Curse Panel Wiring

`CursePanelController` fields:
- `titleText`
- `curseListText`
- `tensionText`

Runtime flow:
- Call `RefreshRuntimePanels()` after node transitions, event resolution, shop purchases, or puzzle completion.

## 4) Heat Curve Graph Wiring

`HeatCurveGraphController` fields:
- `graphRoot` (`RectTransform` area)
- `pointPrefab` (`Image`, small circle/square)
- `segmentPrefab` (`Image`, thin horizontal rectangle)
- `yAxisLabel` (`Text`)

Runtime flow:
- `RenderCurrentRunCurve()` pulls `RunState.HeatHistory`
- Graph redraws points + connecting segments each refresh

## 5) Minimal Integration Calls

After selecting a path in your map UI:
- Get the returned node type
- Call `OnNodeEntered(node.Type)`

After completing event choice or closing event panel:
- Call `OnEventClosed()`

For generic updates:
- Call `RefreshRuntimePanels()`

## Notes

- This document details the production-ready flow logic at script level.
- Art/polish assets (final icons, themed panels, animation/VFX, custom graph styling) are still pending.


