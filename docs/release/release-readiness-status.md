# Release Readiness Status — 0.2.1

Updated 2026-10-21 · Milestone: **Steam build v0.2.1 (7th iteration)**, built and smoke-tested, not uploaded; the previous milestones were Steam build v0.2.0 (6th iteration) and its beta

## Evidence

- Unity batchmode EditMode and PlayMode XML results (`tools/run-unity-tests.ps1`, Unity 6000.3.10f1): see the table below; a run that writes no results XML counts as not run.
- Requirement → code → test links: `docs/review/REQUIREMENT_TRACEABILITY.md`, `[Requirement]` attributes checked by `RequirementTraceabilityTests`.
- Player build and smoke test: `docs/review/E2E_TEST_REPORT.md` §10.

| Suite | Total | Passed | Failed | Skipped | Date |
|-------|------:|-------:|-------:|--------:|------|
| EditMode | 689 | 661 | 0 | 28 (`[Explicit]` review diagnostics and the translation export) | 2026-10-21 |
| PlayMode | 105 | 85 | 0 | 20 (`[Explicit]` screenshot diagnostics) | 2026-10-21 |

Windows x64 player build 0.2.1: built 2026-10-21 (B30, with the licence texts in `Licenses/`) and smoke-tested (B31: reaches the main menu with "Version 0.2.1", no exception in the player log; saves unchanged, PlayerPrefs restored), `docs/review/E2E_TEST_REPORT.md` §12. Before it: 0.2.0 beta B28/B29 (§11).

Previous build (0.1.5): smoke test B01–B15 passed (fresh install, first-run setup, seed entry, path map, wrong-digit rule, Save & Quit and Resume, reward reroll, codex persistence, Abandon & Begin; no exception in the player log); display modes B16–B19, Steam names, window title and save folder B23–B25, the uploaded 0.1.5 build B26–B27 (`docs/review/E2E_TEST_REPORT.md` §10.2b–§10.2e).

## Open gates

All gates with their state are in `docs/release/platform-release-manifest.md`. Owner-side steps (App ID, portal stats/leaderboard/cloud, store art, localization decision, listening check, beta-branch test) are listed in `docs/release/platform-readiness-checklist.md`.

## Not covered by automated evidence

- Unity Profiler captures on target hardware (`docs/performance-profiler-results.md`, PERF-CAP-001..007 pending).
- Steam client behaviour (achievements, stats, leaderboard, cloud).
- Listening checklist for audio.

## Repository state

The 0.2.1 change set is committed on `feature/game-ui-refactor`, tagged `v0.2.1` and pushed with the owner's OK (2026-10-21); `CHANGELOG.md` 0.2.1 lists it; `docs/repository-working-tree-triage.md` describes how the 0.1.5 set was grouped.
