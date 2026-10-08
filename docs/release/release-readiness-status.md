# Release Readiness Status — 1.1.0

Updated 2026-10-07 · Milestone: **second Steam release (1.1.0)**

## Evidence

- Unity batchmode EditMode and PlayMode XML results (`tools/run-unity-tests.ps1`, Unity 6000.3.10f1): see the table below; a run that writes no results XML counts as not run.
- Requirement → code → test links: `docs/review/REQUIREMENT_TRACEABILITY.md`, `[Requirement]` attributes checked by `RequirementTraceabilityTests`.
- Player build and smoke test: `docs/review/E2E_TEST_REPORT.md` §10.

| Suite | Total | Passed | Failed | Skipped | Date |
|-------|------:|-------:|-------:|--------:|------|
| EditMode | 482 | 456 | 0 | 26 (`[Explicit]` review diagnostics) | 2026-10-07 |
| PlayMode | 25 | 25 | 0 | 0 | 2026-10-07 |

Windows x64 player build of the 1.1 tree: smoke test B01–B15 passed (fresh install, first-run setup, seed entry, path map, wrong-digit rule, Save & Quit and Resume, reward reroll, codex persistence, Abandon & Begin; no exception in the player log). Two defects found there were fixed before these test results.

## Open gates

All gates with their state are in `docs/release/platform-release-manifest.md`. Owner-side steps (App ID, portal stats/leaderboard/cloud, store art, localization decision, listening check, beta-branch test) are listed in `docs/release/platform-readiness-checklist.md`.

## Not covered by automated evidence

- Unity Profiler captures on target hardware (`docs/performance-profiler-results.md`, PERF-CAP-001..007 pending).
- Steam client behaviour (achievements, stats, leaderboard, cloud).
- Listening checklist for audio.

## Repository state

The working tree carries the full 1.1 change set uncommitted; `docs/repository-working-tree-triage.md` describes how to group and commit it.
