# In-Game UI Flow Setup (Event / Curse / Heat Curve)

This setup connects the newly added runtime UI controllers in Unity scenes.

## Scripts Added

- `Assets/Scripts/UI/InRunUiFlowController.cs`
- `Assets/Scripts/UI/EventChoiceScreenController.cs`
- `Assets/Scripts/UI/CursePanelController.cs`
- `Assets/Scripts/UI/HeatCurveGraphController.cs`
- `Assets/Scripts/UI/InRunUiBlueprintBuilder.cs` (one-click hierarchy + default styling builder)

## Quick Start (One-Click Blueprint)

1. In your scene, create an empty GameObject named `InRunUiBuilder`.
2. Attach `InRunUiBlueprintBuilder`.
3. Drag your existing `RunMapController` into the builder's `Run Map Controller` field.
4. Open component context menu and run **Build In-Run UI Blueprint**.

Outcome:
- Creates/updates `Canvas/InRunUI`
- Adds and wires `InRunUiFlowController`
- Creates and wires Event/Curse/Heat panels with default anchors, sizes, fonts, and colors

## Default Blueprint Layout

- `EventPanel`: anchors `(0.12,0.12) -> (0.88,0.78)`
- `CursePanel`: anchors `(0.02,0.58) -> (0.30,0.96)`
- `HeatGraphPanel`: anchors `(0.32,0.76) -> (0.98,0.96)`

All panels use semi-transparent dark background and readable default typography (Arial).

## 1) Scene Hierarchy (recommended)

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
- call `InRunUiFlowController.OnNodeEntered(NodeType.Event)`
- event panel opens, choices are instantiated
- selecting a choice resolves through `RunMapController.ChooseEventOption(...)`
- call `OnEventClosed()` when player closes panel

## 3) Curse Panel Wiring

`CursePanelController` fields:
- `titleText`
- `curseListText`
- `tensionText`

Runtime flow:
- call `RefreshRuntimePanels()` after node transitions, event resolution, shop purchases, or puzzle completion.

## 4) Heat Curve Graph Wiring

`HeatCurveGraphController` fields:
- `graphRoot` (`RectTransform` area)
- `pointPrefab` (`Image`, small circle/square)
- `segmentPrefab` (`Image`, thin horizontal rectangle)
- `yAxisLabel` (`Text`)

Runtime flow:
- `RenderCurrentRunCurve()` pulls `RunState.HeatHistory`
- graph redraws points + connecting segments each refresh

## 5) Minimal Integration Calls

After selecting a path in your map UI:
- get returned node type
- call `OnNodeEntered(node.Type)`

After completing event choice or closing event panel:
- call `OnEventClosed()`

For generic updates:
- call `RefreshRuntimePanels()`

## Notes

- This is production-ready flow logic at script level.
- Art/polish assets (final icons, themed panels, animation/VFX, custom graph styling) are still pending.
