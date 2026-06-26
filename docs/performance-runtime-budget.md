# Runtime Performance Budget

## Purpose

This budget tracks frame-sensitive runtime work that must remain bounded before
beta and release-candidate milestones. It is not a profiler result; it is the
engineering contract that keeps known hotspots visible until Unity Profiler
captures can verify actual frame time and allocations.

## Current Release Stance

- Runtime performance status: needs Unity Profiler verification.
- Current automated gate: static update-loop and documentation checks.
- Release requirement: profile the scenarios below on target hardware before
  beta, early access, or release-candidate submission.

## Frame Budget Targets

| Area | Budget | Notes |
| --- | --- | --- |
| Main-thread gameplay frame | 16.6 ms target, 33.3 ms fallback | 60 FPS target, 30 FPS minimum on target hardware. |
| Per-frame GC allocations during stable puzzle play | 0 B target | Spikes during explicit panel rebuilds are allowed only when measured and documented. |
| Save/profile disk I/O in `Update()`/`LateUpdate()` | 0 calls | Disk-backed save/profile loading belongs on explicit events, startup, or throttled non-frame paths. |
| Generated UI rebuilds | user-action only | Language changes, reward panels, shop panels, and boss gates may rebuild on command, not continuously. |
| Ambient UI particles | bounded count | `PathAmbientParticles` is intentionally tiny and must stay bounded unless replaced by pooled VFX. |

## Tracked Hotspots

| Component | Current Risk | Guardrail |
| --- | --- | --- |
| `Assets/Scripts/UI/InRunController.cs` | Large `Update()` and `LateUpdate()` surface with input, visual timers, async polling, and UI state work. | Keep new work out of the frame loop unless it is time-sensitive and allocation-free. Prefer extracted helpers with tests. |
| `Assets/Scripts/UI/RunAudioController.cs` | Audio volumes update every frame for live slider feedback. | Frame loop may read `OptionsController.LiveAudio`, but must not load save/profile data from disk. |
| `Assets/Scripts/UI/PathAmbientParticles.cs` | Small particle loop runs every frame. | Keep particle count fixed and avoid allocation inside `Update()`. |
| `Assets/Scripts/UI/RewardViewController.cs` and `ShopViewController.cs` | Dynamic panels allocate during user-triggered rebuilds. | Rebuild only on open/refresh actions. Profile before adding pooling. |
| `Assets/Scripts/Run/RunDirector.cs` and `Assets/Scripts/Sudoku/PuzzleGenerationService.cs` | Puzzle generation and modifier overlays can be expensive. | Keep generation off the frame loop where possible and profile boss/modifier combinations. |

## Generated UI Rebuild Guardrail

`GeneratedUiRebuildTests` verifies that `MainMenuBlueprintBuilder.ForceRebuild()`
reuses the generated root, removes stale generated children, and does not stack
duplicate slot-change handlers. This does not prove allocation-free rebuilds,
but it prevents the highest-risk correctness regression while profiler captures
are still pending.

## Required Profiler Scenarios

Before beta or release candidate, capture Unity Profiler data for:

1. Startup through splash and first main menu display.
2. Main menu navigation through profile, options, credits, class select, and challenge panels.
3. First run start, path display, first puzzle entry, and return to path.
4. Boss gate display, modifier selection, boss puzzle generation, and boss solve.
5. Reward panel, shop panel, rest node, and save-and-quit.
6. Language switch to German at normal and 150% font scale.
7. Long-run session with multiple floor transitions.

Record profiler captures, target hardware, Unity version, build type, and commit
in a follow-up performance notes file before marking performance release-ready.

## Current Automated Checks

`Assets/Scripts/Tests/EditMode/PerformanceReadinessTests.cs` enforces the
current static guardrails:

- This budget document must exist and mention the tracked hotspots.
- `RunAudioController.Update()` must not perform save/profile disk loading.
- The known `Update()` and `LateUpdate()` hotspot inventory must stay explicit.

These tests do not replace Unity Profiler validation. They prevent accidental
regression while the project is still moving toward runtime performance
verification.
