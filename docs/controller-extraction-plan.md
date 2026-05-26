# Controller Extraction Plan

## Purpose
This plan tracks CBH-002: several central controllers still combine too many responsibilities. The current repository has working extraction helpers and smoke coverage, but a broad refactor without PlayMode safety would be high risk. The immediate guardrail is a size ratchet plus a staged extraction plan.

## Current Ratchet Budgets
| File | Current responsibility concentration | Current line budget | Next extraction target |
| --- | --- | ---: | --- |
| `Assets/Scripts/UI/InRunController.cs` | Puzzle interaction, item targeting, save/quit UI, tutorial flow, controller polling, status text, run panel transitions | 5100 | Move item targeting and tutorial interaction state into focused presenters/controllers, then lower this budget below the measured baseline. |
| `Assets/Scripts/UI/MainMenuBlueprintBuilder.cs` | Main menu construction, options, profiles, class select, game modes, accessibility, keybindings | 3340 | Continue partial builder extraction by panel, following `MainMenuBlueprintBuilder.ChallengePanels.cs`, then lower this budget below the measured baseline. |
| `Assets/Scripts/Run/RunDirector.cs` | Run state, level generation, rewards, boss/curses/debuffs, mode-specific flow, pressure mechanics | 3350 | Extract reward resolution, debuff application, and mode progression services after acceptance coverage. |

The budgets are intentionally set just above the measured file sizes after the
current localization, first-run, audio, and platform-readiness hardening work.
They are not quality targets; they prevent further growth while incremental
extraction proceeds. The next extraction touching either UI controller should
lower the corresponding budget rather than leave it at this temporary ceiling.

## Existing Extraction Evidence
- `MainMenuBlueprintBuilder.ChallengePanels.cs` owns Daily Walk and Monthly Walk panel construction.
- `ControllerIndicatorPoller`, `AsyncLevelCompletionPresenter`, and `TimedVisualStateController` remove focused logic from per-frame UI code.
- `GeneratedUiWiringContractTests`, PlayMode smoke tests, and run-flow tests provide a growing safety net.

## Execution Order
1. Add or strengthen PlayMode coverage for the flow being extracted.
2. Extract a focused presenter/service with a narrow public API.
3. Leave the old controller as orchestration only.
4. Update `ControllerSizeBudgetTests` to lower the budget after the extraction lands.
5. Keep localization and save compatibility unchanged during each slice.

## Risks
- Moving UI construction without traversal tests can break generated button wiring.
- Moving run logic without save/resume tests can change persisted state.
- Extracting too much at once can make regression attribution difficult.

## Validation
`ControllerSizeBudgetTests` enforces the current ratchet and requires this plan to remain visible. Future extraction work should lower the budgets instead of raising them unless a reviewer accepts the tradeoff.
