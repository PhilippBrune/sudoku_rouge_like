# Future Scope Decision Register

## Purpose
This register resolves the FUTURE-005 through FUTURE-008 audit risks by making
achievement, cloud-save, onboarding, and modding scope explicit for the current
milestone.

## Current Milestone
Approved scope (1.1.0): second Steam release. The FUTURE-005..008 decisions still apply; FUTURE-009..016 record the deferrals of the 1.1 review pass.

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

## 1.1 Review Decisions (2026-10-07)

Recorded while implementing `docs/review/SPECIFICATION_ISSUES.md` and `docs/review/GAME_GAPS.md` for release 1.1.0. Requirement IDs stay reserved.

| ID | Area | Decision | Source | Exit Criteria |
| --- | --- | --- | --- | --- |
| FUTURE-009 | Ascension (META-ASCEND-001..003) | Not built; deferred. Monthly Walk has no ascension gate. | SPEC-CONF-026, SPEC-CONF-036, GAP-017; MetaProgressionSystem §Ascension | Design approved, `AscensionService` implemented with tests, map rows ✅ from evidence. |
| FUTURE-010 | Boss debuffs PencilPoison, FogPulse, NumberBlind, DigitShift (DEBUFF-DEF-006/009/010/016) | Descoped. DigitShift conflicts with solution-based validation. | GAP-026, SPEC-CONF-021; BossDebuffEffects §Descoped | Each needs a design that keeps puzzles unique, plus hook and delivery tests. |
| FUTURE-011 | Multi-cell selection (GDS §4.1) | Not built; single-cell selection only. | SPEC-MAC-005 | Selection model, input mapping for keyboard/controller, acceptance tests. |
| FUTURE-012 | Accurate Map path redirect | Not built; the relic reveals tile details instead. | ClassProgressionUnlocks v0.3 | Redirect rule specified, map UI and save support, tests. |
| FUTURE-013 | Legend polish (isolate pulse, cancel by board tap) | Not built; isolation works by tapping the entry. | GAP-025; BossMechanicsSystem §Modifier Legend Panel | Visual pass with PlayMode coverage. |
| FUTURE-014 | French, Spanish, Portuguese gameplay content | 431 keys per language fall back to English (items, relics, bosses, narrative, classes). | GAP-019 follow-up; PRG-LOCALIZATION | Keys translated, `ReleaseLocalizationReadinessTests` extended to all catalogs. |
| FUTURE-015 | In-game Options visual style | The in-run Options panel keeps its own style; aligning it with the main Options needs a visual design pass. | GAP-029 (remaining item) | Shared panel/button style applied and reviewed in screenshots. |
| FUTURE-016 | Achievement list (MetaProgressionSystem, 52 proposed) | Not built; only the Harmony achievements (HARMONY-ACHIEVE-001) are granted. | SPEC-MAC-008 | Each achievement has an ID, a condition hook, a test and a Steamworks portal entry. |

## Release Copy Rules
- Do not market Steam achievements unless Steam runtime validation is complete.
- Do not market cloud save unless a real provider and conflict UX are present.
- Do not market mod support unless import/export validation and support policy
  exist.
- It is acceptable to market custom puzzle setup only as an in-game built-in
  feature, not as external modding.
