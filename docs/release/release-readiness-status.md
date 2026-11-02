# Release Readiness Status: 0.3.3

Updated 2026-11-02 · Milestone: **Steam build v0.3.3**, a local test build for the owner (the story scenes, ten story achievements, the XP brush, the legend in the rule slip and the 0.3.1 controller findings; not uploaded); before it 0.3.2 (the story of the Nine, the quit screen, irregular regions on every Custom Puzzle size); before it **Steam build v0.3.1**, a local test build (the 0.3.1 playtest fixes: event tiles in the Garden Run, the Daily Walk shorter and saved for its day, the Custom Puzzle result and regions, the Options and Accessibility fixes, the brush cursor; not uploaded); before it **Steam build v0.3.0 (9th iteration)** for `default` (the Daily Walk as a run, the Incense Walk, the stamp shop, class relics and keystones, 21 achievements, the Options test; the 0.2.2 beta is folded in, it was never uploaded); the previous milestones were Steam build v0.2.2 beta (8th iteration) and v0.2.1 (7th iteration)

## Evidence

- Unity batchmode EditMode and PlayMode XML results (`tools/run-unity-tests.ps1`, Unity 6000.3.10f1): see the table below; a run that writes no results XML counts as not run.
- Requirement → code → test links: `docs/review/REQUIREMENT_TRACEABILITY.md`, `[Requirement]` attributes checked by `RequirementTraceabilityTests`.
- Player build and smoke test: `docs/review/E2E_TEST_REPORT.md` §10.

| Suite | Total | Passed | Failed | Skipped | Date |
|-------|------:|-------:|-------:|--------:|------|
| EditMode | 831 | 802 | 0 | 29 (`[Explicit]` review diagnostics, the irregular-region probe and the translation export) | 2026-11-02 |
| PlayMode | 143 | 111 | 0 | 32 (`[Explicit]` screenshot diagnostics) | 2026-11-02 |

Windows x64 player build 0.3.3 (local test build): built 2026-11-02 from a fresh `Library` into `Builds/RunOfTheNine_0.3.3` (with `Licenses/`, no missing script) and smoke-tested ("Version 0.3.3" on the main menu after 30 s, no exception in the player log; closing the window showed the goodbye; saves and PlayerPrefs restored). Before it: Windows x64 player build 0.3.2 (local test build): built 2026-11-02 from a fresh `Library` into `Builds/RunOfTheNine_0.3.2` (with `Licenses/`, no missing script) and smoke-tested ("Version 0.3.2" on the main menu, no exception in the player log; closing the window showed the goodbye and the player then quit on its own after 3 s; saves and PlayerPrefs restored). Before it: Windows x64 player build 0.3.1 (local test build): built 2026-11-02 from a fresh `Library` into `Builds/RunOfTheNine_0.3.1` (with `Licenses/`, build log without a missing script) and smoke-tested ("Version 0.3.1" on the main menu after 30 s, no exception in the player log; it ended a Daily Walk saved on an earlier day in the owner's profile as DAILY-WALK-010 says; saves and PlayerPrefs restored afterwards). Before it: Windows x64 player build 0.3.0: built 2026-10-27 from a fresh `Library` (B34, with `Licenses/`, no missing script) and smoke-tested (B35: "Version 0.3.0" on the main menu after 30 s, no exception in the player log; saves and PlayerPrefs restored), `docs/review/E2E_TEST_REPORT.md` §14. Before it: Windows x64 player build 0.2.2 beta: built 2026-10-25 (B32, with `Licenses/`) and smoke-tested (B33: "Version 0.2.2" on the main menu, no exception in the player log; one of two starts ended with a D3D12 present error after Steam started, not reproduced; saves and PlayerPrefs restored), `docs/review/E2E_TEST_REPORT.md` §13. Before it: 0.2.1 B30/B31 (§12), 0.2.0 beta B28/B29 (§11).

Previous build (0.1.5): smoke test B01–B15 passed (fresh install, first-run setup, seed entry, path map, wrong-digit rule, Save & Quit and Resume, reward reroll, codex persistence, Abandon & Begin; no exception in the player log); display modes B16–B19, Steam names, window title and save folder B23–B25, the uploaded 0.1.5 build B26–B27 (`docs/review/E2E_TEST_REPORT.md` §10.2b–§10.2e).

## Open gates

All gates with their state are in `docs/release/platform-release-manifest.md`. Owner-side steps (App ID, portal stats/leaderboard/cloud, store art, localization decision, listening check, beta-branch test) are listed in `docs/release/platform-readiness-checklist.md`.

## Not covered by automated evidence

- Unity Profiler captures on target hardware (`docs/performance-profiler-results.md`, PERF-CAP-001..007 pending).
- Steam client behaviour (achievements, stats, leaderboard, cloud).
- Listening checklist for audio.

## Repository state

The 0.3.0 change set is committed on `feature/game-ui-refactor` and tagged `v0.3.0` locally (not pushed); `CHANGELOG.md` 0.3.0 lists it. The 0.2.2 beta was tagged `v0.2.2-beta` and pushed with the owner's OK (2026-10-27). 0.2.1 was tagged `v0.2.1` and pushed with the owner's OK (2026-10-21). `docs/repository-working-tree-triage.md` describes how the 0.1.5 set was grouped.
