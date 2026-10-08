---
name: unity-testing
description: Write and run automated tests for this Unity project — EditMode/PlayMode NUnit tests, Unity batchmode runs, deterministic seeded scenarios, smoke-test manifest upkeep, and E2E verification. Use when adding tests, verifying a feature works, running the test suite, or deciding whether something can be marked complete.
---

# Unity Automated Testing

Unity batchmode is the authoritative test gate. `dotnet build` / `dotnet test` are compile smoke checks only and never count as a pass.

## Running tests

From the repo root (PowerShell):

```powershell
.\tools\run-unity-tests.ps1 -TestPlatform EditMode   # fast, logic
.\tools\run-unity-tests.ps1 -TestPlatform PlayMode   # scene/UI harnesses
.\tools\run-unity-tests.ps1 -TestPlatform All
```

- The runner deletes a stale results XML first, prints total/passed/failed/skipped and every failure, and exits non-zero if a test failed, no XML was written, or 0 tests ran. A missing XML always means "not run", never "passed". (Before 2026-10-05 it passed `-quit` with `-runTests`, so no test ever ran, GAP-010.)
- Select tests with `-TestFilter "<FullName regex or names>"` or `-TestCategory "<category>"`. `[Explicit]` fixtures (the review diagnostics) only run when selected by name.
- Run the tests for one requirement: `-TestCategory "REQ:SAVE-RESUME-001"` (see `[Requirement]` below).
- If this project is open in the Unity Editor, the script refuses to run. To test anyway, mirror the repo (with `Library/` the first time) into a short path such as `%TEMP%\sr_testcopy` with robocopy and pass `-ProjectPath` to that copy.
- Editor: Unity `6000.3.10f1` (`ProjectSettings/ProjectVersion.txt`). Override with `-UnityExe` or `$env:UNITY_EXE`.
- Output: `TestResults/<Platform>-results.xml` (NUnit XML) and `TestResults/<Platform>-unity.log`.
- Batchmode fails if this project is open in an Editor. Check with `Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Select CommandLine` and look for `-projectpath ...sudoku_rouge_like`. An editor open on a *different* project is fine.
- Exit code `198` means a licensing problem: Unity Hub must be signed in.
- Runs take minutes. Use `run_in_background` and inspect the XML when done.

Summarize results from the XML:

```powershell
[xml]$r = Get-Content TestResults\EditMode-results.xml
$r.'test-run' | Select total, passed, failed, skipped
$r.SelectNodes("//test-case[@result='Failed']") | % { $_.fullname; $_.failure.message.'#cdata-section' }
```

Always also scan the log for `Exception`, `NullReferenceException`, `error CS`, and `Missing` lines. A green XML with errors in the log needs investigation.

## Layout and conventions

- EditMode: `Assets/Scripts/Tests/EditMode/` (asmdef `GameTests.EditMode`).
- PlayMode: `Assets/Scripts/Tests/PlayMode/` (asmdef `GameTests.PlayMode`).
- Namespace `SudokuRoguelike.Tests`, `[TestFixture] public sealed class <Subject>Tests`, method names `Subject_Condition_Expected`.
- Prefer constructing plain C# services directly (see `PuzzleGenerationServiceTests`, `RunFlowAcceptanceTests`). Inject fakes via existing optional parameters (`boardFactory`, `overlayFactory`, ...) rather than adding test-only branches to production code.
- PlayMode UI tests build harnesses in code (see `GeneratedUiPlayModeTraversalTests.BuildMainMenuHarness`) and destroy created roots in `[UnityTearDown]`.
- **Never touch the player's real saves.** Both test assemblies have a namespace-less `[SetUpFixture]` (`TestSaveIsolation.cs`) that points `SaveFileService.RootDirectoryOverride` at a temp folder for the whole run. Build save paths with `SaveFileService.SaveRoot` / `GetSlotPath(slot)`, never `Application.persistentDataPath`. Tests still use slot `SaveFileService.MaxSlots - 1`, flush with `SaveFileService.FlushSharedPendingWrites()`, and restore `SaveProfileService.ActiveSlot`.
- Source-contract tests read classes with `SourceText.ReadClass(path)` (main file plus `X.*.cs` partials) and documents with `DocText.Read(path)` (whitespace-normalized), so splitting a controller or re-wrapping a paragraph doesn't break or silently skip a check.
- Reset global state you change (`Time.timeScale`, `LocalizationService` language, statics).

## Determinism

- Every gameplay test uses a fixed seed. No `UnityEngine.Random`, wall-clock time, or frame-timing-dependent asserts.
- Prefer a reproducibility check for seeded features: generate twice with the same seed and assert identical results (board, region map, overlay, rewards, path), then assert a different seed differs.
- Date-seeded features (Daily goals, Monthly Walk) must take an injected date in tests.

## What a "verified" feature needs

- Logic requirement: an EditMode test asserting the spec'd values (numbers, thresholds, ordering) plus at least one boundary case.
- Flow and UI requirement: a PlayMode test that drives the real controllers (click buttons through the generated hierarchy, assert panel state), or a recorded E2E run of the built player. Static wiring tests (`GeneratedUiWiringContractTests`) alone don't prove behavior.
- Persisted state: a save → load round-trip test.
- Tag the test with `[Requirement("<ID>", ...)]` (method or fixture). It adds the category `REQ:<ID>` and a `Requirement` property in the results XML. `RequirementTraceabilityTests` fails if an ID is not found in `docs/REQUIREMENT_MAP.md` or `docs/review/REQUIREMENT_TRACEABILITY.md`.

## Keep manifests in sync

When adding or retiring smoke-level coverage, update `docs/smoke-test-readiness.md` (`SMOKE-*` rows). `SmokeTestReadinessTests` and CI (`.github/workflows/static-validation.yml`) check that manifest.

## Temporary diagnostic tests

Diagnostic automation created only for verification goes in a clearly named file (e.g. `Diagnostics_<Topic>Tests.cs` with a `// TEST INFRASTRUCTURE — temporary diagnostic` header). Either promote it to a real test or delete it before finishing, and say which.
