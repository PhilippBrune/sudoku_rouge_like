# Smoke Test Readiness

## Purpose

This manifest defines the minimum automated smoke coverage required before the
project can be treated as beta, early-access, or release-candidate ready.

The repository has EditMode and PlayMode smoke coverage, but authoritative
runtime validation still requires Unity batchmode execution. Direct `dotnet`
commands are compile smoke checks only.

## Current Status

- Static CI guardrail: present in `.github/workflows/static-validation.yml`.
- Unity batchmode CI: present but opt-in through `RUN_UNITY_BATCHMODE`.
- EditMode acceptance coverage: present.
- PlayMode generated UI traversal coverage: present.
- Release gate status: not complete until Unity batchmode EditMode and PlayMode
  XML results are produced and reviewed.

## Required Smoke Matrix

| ID | Scenario | Test Coverage | Required Before |
| --- | --- | --- | --- |
| SMOKE-EDIT-RUNFLOW | Garden run can start, solve a puzzle, export save state, apply boss modifier flow, and complete tutorial puzzle. | `Assets/Scripts/Tests/EditMode/RunFlowAcceptanceTests.cs` | Alpha stabilization |
| SMOKE-UI-WIRING | Generated menu buttons, option controls, and conditional buttons have static wiring contracts. | `Assets/Scripts/Tests/EditMode/GeneratedUiWiringContractTests.cs`, `GeneratedUiRegistryTests.cs`, `ConditionalButtonStateTests.cs` | Alpha stabilization |
| SMOKE-FIRST-RUN | First-run language/accessibility/legal setup can open, apply, and persist choices. | `Assets/Scripts/Tests/PlayMode/FirstRunSetupPlayModeTests.cs` | Alpha stabilization |
| SMOKE-PLAYMODE-UI | Main menu, nested menus, in-run options, language confirmation, German 150% font traversal, and generated UI traversal work in PlayMode harnesses. | `Assets/Scripts/Tests/PlayMode/GeneratedUiPlayModeTraversalTests.cs` | Alpha stabilization |
| SMOKE-PLAYMODE-ASSEMBLY | PlayMode test assembly is discoverable by Unity Test Runner. | `Assets/Scripts/Tests/PlayMode/PlayModeSmokeTests.cs` | Development validation |
| SMOKE-BATCHMODE | EditMode and PlayMode suites run through Unity batchmode and produce XML result files. | `tools/run-unity-tests.ps1 -TestPlatform All` | Beta/release candidate |

## CI Policy

`.github/workflows/static-validation.yml` must continue to:

- require this manifest;
- require `tools/run-unity-tests.ps1`;
- require `GameTests.EditMode.csproj` and `GameTests.PlayMode.csproj`;
- expose the `unity-batchmode` job;
- clearly state that `RUN_UNITY_BATCHMODE` controls whether authoritative Unity
  execution runs in CI.

If `RUN_UNITY_BATCHMODE` is not enabled, CI may pass as a static guardrail only.
That is acceptable for development branches but not for beta, early-access, or
release-candidate approval.

## Local Validation Commands

Authoritative local validation:

```powershell
.\tools\run-unity-tests.ps1 -TestPlatform All
```

Fast compile smoke:

```powershell
dotnet build sudoku_rouge_like.slnx --verbosity minimal
dotnet test GameTests.EditMode.csproj --verbosity minimal
dotnet test GameTests.PlayMode.csproj --verbosity minimal
```

The fast compile smoke does not replace Unity Test Runner execution.
