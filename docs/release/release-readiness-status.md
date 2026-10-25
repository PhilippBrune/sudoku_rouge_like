# Release Readiness Status: 0.2.2 beta

Updated 2026-10-25 · Milestone: **Steam build v0.2.2 beta (8th iteration)** for the password beta branch (playtest of the one-more-run slice, the fixed perks and the D-pad); the previous milestones were Steam build v0.2.1 (7th iteration) and v0.2.0 (6th iteration)

## Evidence

- Unity batchmode EditMode and PlayMode XML results (`tools/run-unity-tests.ps1`, Unity 6000.3.10f1): see the table below; a run that writes no results XML counts as not run.
- Requirement → code → test links: `docs/review/REQUIREMENT_TRACEABILITY.md`, `[Requirement]` attributes checked by `RequirementTraceabilityTests`.
- Player build and smoke test: `docs/review/E2E_TEST_REPORT.md` §10.

| Suite | Total | Passed | Failed | Skipped | Date |
|-------|------:|-------:|-------:|--------:|------|
| EditMode | 712 | 684 | 0 | 28 (`[Explicit]` review diagnostics and the translation export) | 2026-10-25 |
| PlayMode | 110 | 90 | 0 | 20 (`[Explicit]` screenshot diagnostics) | 2026-10-25 |

Windows x64 player build 0.2.2 beta: built 2026-10-25 (B32, with `Licenses/`) and smoke-tested (B33: "Version 0.2.2" on the main menu, no exception in the player log; one of two starts ended with a D3D12 present error after Steam started, not reproduced; saves and PlayerPrefs restored), `docs/review/E2E_TEST_REPORT.md` §13. Before it: 0.2.1 B30/B31 (§12), 0.2.0 beta B28/B29 (§11).

Previous build (0.1.5): smoke test B01–B15 passed (fresh install, first-run setup, seed entry, path map, wrong-digit rule, Save & Quit and Resume, reward reroll, codex persistence, Abandon & Begin; no exception in the player log); display modes B16–B19, Steam names, window title and save folder B23–B25, the uploaded 0.1.5 build B26–B27 (`docs/review/E2E_TEST_REPORT.md` §10.2b–§10.2e).

## Open gates

All gates with their state are in `docs/release/platform-release-manifest.md`. Owner-side steps (App ID, portal stats/leaderboard/cloud, store art, localization decision, listening check, beta-branch test) are listed in `docs/release/platform-readiness-checklist.md`.

## Not covered by automated evidence

- Unity Profiler captures on target hardware (`docs/performance-profiler-results.md`, PERF-CAP-001..007 pending).
- Steam client behaviour (achievements, stats, leaderboard, cloud).
- Listening checklist for audio.

## Repository state

The 0.2.2 beta change set is committed on `feature/game-ui-refactor` and tagged `v0.2.2-beta` locally (not pushed); `CHANGELOG.md` 0.2.2 beta lists it. 0.2.1 was tagged `v0.2.1` and pushed with the owner's OK (2026-10-21).
