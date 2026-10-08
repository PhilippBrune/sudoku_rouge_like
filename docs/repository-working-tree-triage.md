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

## 1.1.0 Change Set (2026-10-07)

The uncommitted tree holds release 1.1.0: the 2026-10-05 fix pass and the 1.1 pass (`docs/review/GAME_GAPS.md`, both fix logs). Many runtime files carry several gaps (for example `InRunController.cs`), so a split by file cannot follow gap boundaries exactly. A workable grouping, each message citing its GAP and requirement IDs (CLAUDE.md):

1. Runtime code and tests: `Assets/Scripts/**` (new partial classes and test fixtures included, with their `.meta` files), `Game.csproj` and the two test `.csproj` files. Message lists GAP-001..041 by area.
2. Localization catalogs and their `.meta` files (`es.json.meta` and `pt.json.meta` are new).
3. Specs and review/release docs: `docs/**`, `CHANGELOG.md`, `README.md`; the moved files (`docs/archive/`, `docs/art-source/ui-mockups/`) and the deleted `docs/*.md.meta` and `docs/ParkStoryDraft.md`.
4. Project settings (`bundleVersion` 1.1.0) only after the company/product decision (PRG-PLAYER-METADATA in `docs/release/platform-release-manifest.md`), together with `.gitignore`.

The audio WAV files show as modified because their headers were repaired on 2026-10-05 (GAP-030); commit them with group 2 or as their own commit.

## Do Not
- Do not use `git reset --hard` to clean the tree.
- Do not restore the retired root `sudoku_rouge_like.sln`.
- Do not move prompt/source text files back into runtime `Resources`.
- Do not mark this repository release-ready until the dirty tree has been
  reviewed, committed, and validated through Unity batchmode.
