# CI Validation

The repository has a lightweight CI scaffold in:

- `.github/workflows/static-validation.yml`

## What Runs Automatically

The `repository-checks` job runs on pull requests, pushes to `main`/`master`,
and manual dispatch.

It verifies:

- Required workflow, development, project, test, localization, production font,
  and third-party font notice files exist.
- The retired root `sudoku_rouge_like.sln` file remains absent so `.slnx` stays
  the single supported direct dotnet entrypoint.
- README local markdown links resolve to files in the repository.
- English and German JSON localization catalogs parse successfully.
- Background prompt/source files stay outside runtime `Assets/Resources`.
- `dotnet build sudoku_rouge_like.slnx --verbosity minimal` only when the
  runner has the Unity editor assemblies and generated `Library` assemblies
  needed by the Unity-generated project files.

## What Does Not Run Automatically Yet

Unity batchmode tests are the authoritative gate, but hosted CI requires a
licensed Unity installation and project import state. The workflow includes an
opt-in `unity-batchmode` job that runs only when repository variable
`RUN_UNITY_BATCHMODE` is set to `true`.

Before enabling that job, configure a runner with:

- Unity `6000.3.10f1`
- A valid Unity license for batchmode
- Any required package/import cache setup
- Access to run `tools/run-unity-tests.ps1`

## Release Policy

Static CI is a regression guardrail, not a release gate. Release candidates
still require local or CI Unity batchmode EditMode and PlayMode results recorded
under `TestResults/`.
