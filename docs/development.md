# Development Workflow

## Authoritative Build and Test Entry Point

Unity is the authoritative build and test runner for this repository.

Target editor version:

```text
Unity 6000.3.10f1
```

The version is recorded in `ProjectSettings/ProjectVersion.txt`.

Do not treat `sudoku_rouge_like.slnx` or generated `.csproj` files as the
release authority. They are useful for IDE indexing and quick compile checks,
but Unity owns assembly generation, package resolution, asset imports, scene
loading, and Test Runner execution.

For direct dotnet validation, use `sudoku_rouge_like.slnx`. The stale
`sudoku_rouge_like.sln` root solution has been retired; do not recreate it or
use it as a release gate.

The canonical `.slnx` currently contains:

- `Game.csproj`
- `GameTests.EditMode.csproj`
- `GameTests.PlayMode.csproj`

## Running Tests

From the repository root:

```powershell
.\tools\run-unity-tests.ps1 -TestPlatform EditMode
```

To run PlayMode tests:

```powershell
.\tools\run-unity-tests.ps1 -TestPlatform PlayMode
```

To run both:

```powershell
.\tools\run-unity-tests.ps1 -TestPlatform All
```

If Unity is installed outside the default Unity Hub path, pass the editor path:

```powershell
.\tools\run-unity-tests.ps1 -UnityExe "C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe"
```

The script writes XML results and Unity logs to `TestResults/`.

## Startup Scene Order

Build settings are expected to contain:

1. `Assets/Scenes/Splash.unity`
2. `Assets/Scenes/Prototype.unity`

`Splash.unity` owns the branded splash sequence through
`SplashSceneBootstrap`, then loads `Prototype` by name. `GameBootstrap` still
keeps the inline splash fallback for editor workflows that open
`Prototype.unity` directly.

The first-run language, accessibility, legal acknowledgement, and Sudoku basics
prompt order is documented in `docs/startup-experience.md`.

## Localization Catalogs

Runtime localization catalogs live in:

- `Assets/Resources/Localization/en.json`
- `Assets/Resources/Localization/de.json`

`LocalizationService` loads these JSON files first and then falls back to the
legacy in-code dictionaries. See `docs/localization-catalog.md` for the current
catalog workflow and migration rules.

## Direct Dotnet Checks

Static compile check:

```powershell
dotnet build sudoku_rouge_like.slnx --verbosity minimal
```

Do not recreate or substitute `dotnet build sudoku_rouge_like.sln`; release
validation and developer documentation are standardized on the `.slnx`
entrypoint.

EditMode compile/test adapter check:

```powershell
dotnet test GameTests.EditMode.csproj
```

These commands can catch C# compile errors quickly, but they are not the
production test gate unless a proper .NET test adapter and Unity assembly setup
are added. Prefer the Unity batchmode command above for audit, release, and CI
validation.

`GameTests.PlayMode.csproj` is included so the solution can compile the
PlayMode assembly. PlayMode test sources live under
`Assets/Scripts/Tests/PlayMode/`, including generated UI traversal and first-run
setup smoke coverage. A successful direct `dotnet test GameTests.PlayMode.csproj`
is still only a compile/test-adapter check, not an authoritative Unity runtime
pass.

## CI Scaffold

The repository tracks a lightweight GitHub Actions workflow:

- `.github/workflows/static-validation.yml`

The automatic job validates required files and parses the English/German
localization catalogs. It runs the direct dotnet compile smoke only when the
runner has the Unity editor assemblies and generated `Library` assemblies
available.

The workflow also contains an opt-in Unity batchmode job guarded by the
repository variable `RUN_UNITY_BATCHMODE`. Enable it only on a runner with Unity
`6000.3.10f1`, a valid batchmode license, and the expected project import state.

See `docs/ci-validation.md` for details.

## Smoke Test Readiness

The current smoke-test matrix is tracked in
`docs/smoke-test-readiness.md`. Keep that manifest in sync with the EditMode,
PlayMode, and CI test files whenever adding or retiring smoke coverage.

## Expected Release Gate

Before a release candidate:

1. Run EditMode tests through Unity batchmode.
2. Run PlayMode tests through Unity batchmode.
3. Confirm generated XML result files exist in `TestResults/`.
4. Review Unity logs for asset import, scene load, and missing reference errors.
5. Record the Unity editor version used for the build.

## Troubleshooting

Unity exit code `198` usually means batchmode could not obtain a valid Editor
license. Activate Unity for the local machine through Unity Hub, then rerun the
same command.

If the Unity log contains a scene backup recovery prompt, open the project once
in the Editor and resolve the recovery dialog before relying on batchmode tests.

## Current Known Limitations

- CI is configured only as a lightweight static guardrail, not as an
  authoritative release gate.
- The retired root `sudoku_rouge_like.sln` must remain absent; Unity batchmode
  remains the supported validation entrypoint.
- Some generated project files can be overwritten by Unity regeneration.
- Runtime UI, localization layout, and scene traversal still need PlayMode
  coverage beyond the current edit-mode checks.
