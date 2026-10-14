# Release Readiness Status — 0.1.5

Updated 2026-10-14 · Milestone: **Steam build v0.1.5 (5th iteration)**, the change set the review documents call release 1.1

## Evidence

- Unity batchmode EditMode and PlayMode XML results (`tools/run-unity-tests.ps1`, Unity 6000.3.10f1): see the table below; a run that writes no results XML counts as not run.
- Requirement → code → test links: `docs/review/REQUIREMENT_TRACEABILITY.md`, `[Requirement]` attributes checked by `RequirementTraceabilityTests`.
- Player build and smoke test: `docs/review/E2E_TEST_REPORT.md` §10.

| Suite | Total | Passed | Failed | Skipped | Date |
|-------|------:|-------:|-------:|--------:|------|
| EditMode | 498 | 472 | 0 | 26 (`[Explicit]` review diagnostics) | 2026-10-14 |
| PlayMode | 25 | 25 | 0 | 0 | 2026-10-14 |

Windows x64 player build: smoke test B01–B15 passed (fresh install, first-run setup, seed entry, path map, wrong-digit rule, Save & Quit and Resume, reward reroll, codex persistence, Abandon & Begin; no exception in the player log); display modes B16–B19, Steam names, window title and save folder B23–B25, the uploaded 0.1.5 build B26–B27 (`docs/review/E2E_TEST_REPORT.md` §10.2b–§10.2e).

## Open gates

All gates with their state are in `docs/release/platform-release-manifest.md`. Owner-side steps (App ID, portal stats/leaderboard/cloud, store art, localization decision, listening check, beta-branch test) are listed in `docs/release/platform-readiness-checklist.md`.

## Not covered by automated evidence

- Unity Profiler captures on target hardware (`docs/performance-profiler-results.md`, PERF-CAP-001..007 pending).
- Steam client behaviour (achievements, stats, leaderboard, cloud).
- Listening checklist for audio.

## Repository state

The 0.1.5 change set is committed on `feature/game-ui-refactor` (not pushed); `docs/repository-working-tree-triage.md` describes how it was grouped.
