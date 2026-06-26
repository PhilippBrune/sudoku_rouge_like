# Future Scope Decision Register

## Purpose
This register resolves the FUTURE-005 through FUTURE-008 audit risks by making
achievement, cloud-save, onboarding, and modding scope explicit for the current
milestone.

## Current Milestone
Approved scope: internal development validation and alpha-readiness hardening.

The current build may include local implementations and readiness scaffolding,
but it must not claim platform release support, cloud save, online leaderboards,
or modding support until the exit criteria below are met.

## Decisions

| ID | Area | Current Decision | Repository Evidence | Exit Criteria |
| --- | --- | --- | --- | --- |
| FUTURE-005 | Achievements and leaderboards | Local achievement persistence is in scope. Platform forwarding is bridge-only and release-blocked until Steam runtime validation exists. Online leaderboards are out of current scope. | `SteamAchievementService`, `PlatformAchievementBridge`, `docs/steam-platform-integration.md`, `docs/release/platform-release-manifest.md` | Steam SDK imported, achievement IDs verified against backend, runtime unlock validation recorded, leaderboard design approved if marketed. |
| FUTURE-006 | Cloud save/profile sync | Out of current scope. Saves remain local profile slots. Cloud save is explicitly reported as `NotConfigured`. | `LocalOnlyCloudSaveProvider`, `docs/platform-services-foundation.md`, `docs/SaveLoadArchitecture.md` | Provider selected, conflict-resolution UX designed, migration tests added, platform runtime validation recorded. |
| FUTURE-007 | Onboarding/tutorial progression | First-run setup and Sudoku basics are in scope. A deeper roguelike systems tutorial is deferred until core UI/runtime validation is stable. | `FirstRunSetupModalController`, `SudokuBasicsTutorialController`, `docs/startup-experience.md`, `docs/FirstTimePlayTutorial.md` | Tutorial milestones cover map choices, items, relics, curses, boss gates, and pressure mechanics with localized copy and smoke coverage. |
| FUTURE-008 | Modding/custom puzzle support | Custom puzzle setup is in scope. External import/export and modding APIs are out of current scope. | `TutorialMenuController`, `TutorialModeService`, `docs/TutorialModeSystem.md` | File format, validation rules, localization boundaries, save compatibility, and sandboxing policy are designed and tested. |

## Release Copy Rules
- Do not market Steam achievements unless Steam runtime validation is complete.
- Do not market cloud save unless a real provider and conflict UX are present.
- Do not market mod support unless import/export validation and support policy
  exist.
- It is acceptable to market custom puzzle setup only as an in-game built-in
  feature, not as external modding.
