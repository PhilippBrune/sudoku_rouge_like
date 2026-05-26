# Repository Working Tree Triage

## Purpose
This note resolves the REPO-004 audit risk by documenting how to handle the
current large dirty working tree without reverting user or generated work.

## Current State
- The repository contains a broad audit-hardening change set across runtime
  code, tests, docs, assets, CI scaffolding, and plan cleanup.
- `git status --short` is expected to remain large until the owner reviews and
  commits or splits the work.
- Root maintenance scripts have moved under `tools/`.
- Runtime background prompt files have moved from `Assets/Resources/background`
  to `docs/art-source/background`.
- New release-readiness docs, legal files, localization catalogs, audio/font
  assets, PlayMode tests, and static guardrails are uncommitted.

## Required Triage Before Release Branching
1. Run `git status --short` and group changes by area:
   - build and CI
   - localization
   - legal and platform readiness
   - audio/font/assets
   - gameplay/runtime code
   - tests
   - docs and plan cleanup
2. Review deleted root scripts against their replacements under `tools/`.
3. Confirm moved prompt files are absent from `Assets/Resources/background` and
   present under `docs/art-source/background`.
4. Run the static validation workflow locally where possible:

```powershell
dotnet build sudoku_rouge_like.slnx --verbosity minimal
dotnet test GameTests.EditMode.csproj --verbosity minimal
dotnet test GameTests.PlayMode.csproj --verbosity minimal
```

5. Run Unity batchmode before treating the branch as beta or release-ready:

```powershell
.\tools\run-unity-tests.ps1 -TestPlatform All
```

6. Split commits by reviewable ownership area if the change set remains too
   broad for one review.

## Do Not
- Do not use `git reset --hard` to clean the tree.
- Do not restore the retired root `sudoku_rouge_like.sln`.
- Do not move prompt/source text files back into runtime `Resources`.
- Do not mark this repository release-ready until the dirty tree has been
  reviewed, committed, and validated through Unity batchmode.
