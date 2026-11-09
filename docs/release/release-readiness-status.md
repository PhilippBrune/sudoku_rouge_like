# Release Readiness Status: 0.5.0

Updated 2026-11-09 · Milestone: **Steam build v0.5.0** (in development, no build yet; evidence below is 0.4.3, Steam build 26534828, live on `default` since 2026-11-09). Last release: **v0.4.3** for `default` (the Garden Run on three floors, the path focus ring, the bag-full screen, fog lanterns in the market, four new class passives, GAP-119 to GAP-121; not uploaded); before it **Steam build v0.4.2** (uploaded by the owner with its notes, 2026-11-07: RM-104 content with trades, the All/Friends boards, the run sounds, Text Size on every screen and OpenDyslexic); before it 0.4.1 (built, uploaded and announced by the owner, 2026-11-04) and **Steam build v0.4.0 (10th iteration)** for `default`, the first public build since 0.3.0 (it carries the test builds 0.3.1 to 0.3.3 and the Spirit Trials starting item; not uploaded); before it **Steam build v0.3.3**, a local test build for the owner (the story scenes, ten story achievements, the XP brush, the legend in the rule slip and the 0.3.1 controller findings; not uploaded); before it 0.3.2 (the story of the Nine, the quit screen, irregular regions on every Custom Puzzle size); before it **Steam build v0.3.1**, a local test build (the 0.3.1 playtest fixes: event tiles in the Garden Run, the Daily Walk shorter and saved for its day, the Custom Puzzle result and regions, the Options and Accessibility fixes, the brush cursor; not uploaded); before it **Steam build v0.3.0 (9th iteration)** for `default` (the Daily Walk as a run, the Incense Walk, the stamp shop, class relics and keystones, 21 achievements, the Options test; the 0.2.2 beta is folded in, it was never uploaded); the previous milestones were Steam build v0.2.2 beta (8th iteration) and v0.2.1 (7th iteration)

## Evidence

- Unity batchmode EditMode and PlayMode XML results (`tools/run-unity-tests.ps1`, Unity 6000.3.10f1): see the table below; a run that writes no results XML counts as not run.
- Requirement → code → test links: `docs/review/REQUIREMENT_TRACEABILITY.md`, `[Requirement]` attributes checked by `RequirementTraceabilityTests`.
- Player build and smoke test: `docs/review/E2E_TEST_REPORT.md` §10.

| Suite | Total | Passed | Failed | Skipped | Date |
|-------|------:|-------:|-------:|--------:|------|
| EditMode | 912 | 881 | 0 | 31 (`[Explicit]` review diagnostics, the irregular-region probe and the translation export) | 2026-11-09 |
| PlayMode | 178 | 132 | 0 | 46 (`[Explicit]` screenshot diagnostics) | 2026-11-09 |

Windows x64 player build 0.4.3 (release for `default`, final): built 2026-11-09 from a fresh `Library` into `Builds/RunOfTheNine_0.4.3` and `RunOfTheNine_0.4.3_win64.zip` (54.9 MB, 158 files). Smoke test: player running after 30 s, 0 exceptions, Direct3D 11.0 in its log, saves restored byte for byte. Tests above ran on that same state.

Previous build (0.1.5): smoke test B01–B15 passed (fresh install, first-run setup, seed entry, path map, wrong-digit rule, Save & Quit and Resume, reward reroll, codex persistence, Abandon & Begin; no exception in the player log); display modes B16–B19, Steam names, window title and save folder B23–B25, the uploaded 0.1.5 build B26–B27 (`docs/review/E2E_TEST_REPORT.md` §10.2b–§10.2e).

## Open gates

All gates with their state are in `docs/release/platform-release-manifest.md`. Owner-side steps (App ID, portal stats/leaderboard/cloud, store art, localization decision, listening check, beta-branch test) are listed in `docs/release/platform-readiness-checklist.md`.

## Not covered by automated evidence

- Unity Profiler captures on target hardware (`docs/performance-profiler-results.md`, PERF-CAP-001..007 pending).
- Steam client behaviour (achievements, stats, leaderboard, cloud).
- Listening checklist for audio.

## Repository state

The 0.4.0 change set is committed on `feature/game-ui-refactor` and tagged `v0.4.0` locally (not pushed; `CHANGELOG.md` 0.3.1 to 0.4.0 lists it). The 0.3.0 change set is committed on `feature/game-ui-refactor` and tagged `v0.3.0` locally (not pushed); `CHANGELOG.md` 0.3.0 lists it. The 0.2.2 beta was tagged `v0.2.2-beta` and pushed with the owner's OK (2026-10-27). 0.2.1 was tagged `v0.2.1` and pushed with the owner's OK (2026-10-21). `docs/repository-working-tree-triage.md` describes how the 0.1.5 set was grouped.
