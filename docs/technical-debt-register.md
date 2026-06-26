# Technical Debt Register

## Purpose
This register classifies the placeholder and stub markers found by CBH-006 and DEAD-006. These entries are allowed only because their current behavior is intentional and bounded. New production markers should be removed or added here with owner, risk, and exit criteria.

## Current Classified Markers
| ID | File | Marker | Classification | Risk | Exit criteria |
| --- | --- | --- | --- | --- | --- |
| TD-PLACEHOLDER-001 | `Assets/Scripts/UI/CursePanelController.cs` | placeholder | Intentional fallback visual for missing curse icon sprites. | Missing art is visible as muted red instead of silently failing. | Keep dynamic icon tests passing; replace with final art or documented fallback badge if curse art changes. |
| TD-PLACEHOLDER-002 | `Assets/Scripts/Run/RunDirector.cs` | placeholder | Intentional no-op for UI-layer effect state that `RunDirector` does not own. | Future maintainers may expect gameplay logic here. | Remove comment if ownership moves to a UI presenter or effect coordinator. |
| TD-PLACEHOLDER-003 | `Assets/Scripts/UI/EndScreenViewController.cs` | placeholder | Runtime background slot is intentionally assigned later when the end-screen state is known. | Low; code path is a construction note. | Replace wording if the setup method is renamed or backgrounds become serialized assets. |
| TD-STUB-001 | `Assets/Scripts/UI/HarmonyNodeRowController.cs` | stub | Local helper type used by the generated row controller. | Naming can imply unfinished behavior. | Rename the comment if the helper remains permanent. |
| TD-PLACEHOLDER-004 | `Assets/Scripts/UI/HudViewController.cs` | placeholder | Intentional "No Relic" empty-slot display. | Low; fallback communicates empty inventory state. | Keep if the HUD continues to show empty relic slots. |
| TD-PLACEHOLDER-005 | `Assets/Scripts/UI/InRunUiFactory.cs` | placeholder | Intentional passive empty-slot, unknown boss-mod, and runtime-assigned background fallbacks. | Low to medium; unknown modifier art can hide missing assets if validation regresses. | Keep runtime resource tests passing; replace unknown state if all modifiers become permanently revealed. |

## Policy
- Prefer removing placeholder/stub wording from production comments when behavior is final.
- If a marker remains, classify it here as an intentional fallback, release blocker, or tracked refactor.
- When adding a new production marker, add the file path, marker, impact, and exit criteria in the same change.
- If a marker is removed from code, remove or update the register entry.

`TechnicalDebtRegisterTests` enforces this policy by scanning production scripts for TODO/FIXME/HACK/UNUSED/DEPRECATED/placeholder/stub/WIP markers and requiring this register to mention the affected file and marker.
