# Performance Profiler Results

## Purpose
This ledger tracks REL-009 and the remaining performance audit risk: static
budget tests exist, but target-hardware Unity Profiler captures are still
required before beta, early access, or release-candidate approval.

## Current Status
- Static performance guardrails: present.
- Background source/import budgets: present.
- Puzzle generation budgets and uniqueness metrics: present.
- Generated UI rebuild guardrails: present.
- Authored audio catalog guardrails: present.
- Unity Profiler captures: **not recorded yet**.
- Release gate: **blocked until captures are recorded and reviewed**.
- Last automated Unity attempt: **blocked by local Unity licensing/headless
  execution failure**.

## Last Capture Attempt

- Date: 2026-05-18
- Command: `powershell -ExecutionPolicy Bypass -File .\tools\run-unity-tests.ps1 -TestPlatform PlayMode`
- Unity path: `C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe`
- Result: command timed out after 15 minutes without writing
  `TestResults/PlayMode-results.xml`.
- Log: `TestResults/PlayMode-unity.log`
- Blocker evidence: Unity licensing client reconnects repeatedly timed out, and
  the log reports `com.unity.editor.headless` was not found.
- Required next action: open Unity/Unity Hub on this machine, restore or refresh
  the editor license/headless support, then rerun Unity batchmode or capture the
  scenarios manually in the Unity Profiler.

## Last Validation Attempt

- Date: 2026-05-19
- Command: `dotnet build sudoku_rouge_like.slnx --verbosity minimal`
- Result: FAILED before reaching profiler-specific assertions.
- Evidence: generated project references to `Library/ScriptAssemblies/UnityEngine.UI.dll`,
  `Library/ScriptAssemblies/UnityEditor.UI.dll`, and
  `Library/ScriptAssemblies/Unity.InputSystem.dll` did not resolve.
- Local state note: equivalent package assemblies were found under
  `Library/Bee/artifacts/1900b0aE.dag/`, so this is treated as a local Unity
  generated-project/cache state issue until Unity can regenerate
  `Library/ScriptAssemblies`.
- Required next action: after Unity licensing/headless support is restored,
  let Unity recompile or regenerate project files, then rerun the build/test
  validation before closing this performance gate.

## Required Capture Matrix

| ID | Scenario | Target Evidence | Status |
| --- | --- | --- | --- |
| PERF-CAP-001 | Startup through splash and first main menu display | CPU frame time, GC alloc, scene load timing | PENDING |
| PERF-CAP-002 | Main menu navigation through profile, options, credits, class select, and challenge panels | UI rebuild allocations and input latency | PENDING |
| PERF-CAP-003 | First run start, path display, first puzzle entry, and return to path | frame time during board setup and UI swap | PENDING |
| PERF-CAP-004 | Boss gate display, modifier selection, boss puzzle generation, and boss solve | generation time, async completion, main-thread stalls | PENDING |
| PERF-CAP-005 | Reward panel, shop panel, rest node, save-and-quit | allocations, disk I/O, panel rebuild spikes | PENDING |
| PERF-CAP-006 | Language switch to German at 100% and 150% font scale | rebuild cost and layout stability | PENDING |
| PERF-CAP-007 | Long-run session with multiple floor transitions | memory growth, audio stability, cumulative GC | PENDING |

## Recording Template

For each capture, record:

- Date:
- Commit or branch:
- Unity version:
- Build type:
- Hardware:
- Resolution:
- Scenario ID:
- Average frame time:
- 95th percentile frame time:
- Max frame time:
- GC allocations:
- Texture memory:
- Audio memory:
- Notes:
- Follow-up tasks:

## Release Rule
Do not mark performance release-ready while any required capture is `PENDING`.
Static tests can close implementation regressions, but they do not replace
Unity Profiler evidence.
