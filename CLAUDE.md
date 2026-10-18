# CLAUDE.md

## Project Rules

- Treat `docs/` as the authoritative game specification.
- Preserve unique requirement IDs.
- Maintain traceability from requirement → implementation → test.
- Never mark a feature complete without runtime/E2E verification where applicable.
- Follow existing architecture, coding conventions, Unity best practices, deterministic testing, and save compatibility.
- Use the project's documented skills/workflows for specification analysis, implementation, debugging, UI, Sudoku generation, and automated testing.
- Run Unity tests with `tools/run-unity-tests.ps1` (batchmode); a run that writes no results XML counts as "not run", never as passed. The owner usually has the Editor open: don't close it, run on the mirror instead (`tools/sync-test-mirror.ps1`, then the runner with `-ProjectPath $env:TEMP\sr_testcopy`; `-WithLibrary` when Unity reports missing `UnityEngine.UI`). Never run Unity batchmode on the repo itself: it imports the owner's untracked folders and rewrites the `*.csproj` files.
- Compile check before a Unity run: `python tools/quick_compile.py [Game|GameTests.EditMode|GameTests.PlayMode]` (about 20 s).
- UI changes: show before/after screenshots from the game (`LayoutScreenshotDiagnostics`, an `[Explicit]` PlayMode test; Unity without `-nographics`) and design proposals as HTML mockups from the game's art (`docs/design/layout-proposals/render.ps1`). The owner picks the proposal before it is built.
- Every player-facing string goes through `LocalizationService` keys and is added to all five catalogs (`Assets/Resources/Localization/en, de, fr, es, pt.json`).
- Spec edits: bump the version, add a Change History row and set the status in the header.
- `docs/review/GAME_GAPS.md` is the fix backlog. Cite the gap ID (`GAP-NNN`) and requirement IDs in commit messages for fixes.
- Do not resolve specification contradictions (`docs/review/SPECIFICATION_ISSUES.md`, `SPEC-CONF-*`) without asking the user.
- `docs/ROADMAP_BACKLOG.md` is the feature backlog (`RM-NNN`); bugs stay in `GAME_GAPS.md`.

## Release, Steam, Discord and money

- Commit locally on the feature branch; never push, set a Steam build live, publish a store page or release without the owner's explicit OK. Commit messages: Conventional Commits, no attribution lines.
- Steamworks: never ask for or handle the Steam password or Steam Guard codes; only act on Run of the Nine (App 4858470), never on Corebound (5420920); stop before every Publish button and show screenshots.
- Discord: never print, log or commit the bot token (it lives only in `~/.claude/channels/discord/.env`); never use the owner's personal account. New public texts go to `#release-drafts` first and are posted after the owner says so.
- Paid services: `tools/audio/mureka_generate.py --yes` costs money; run it only when asked.
- Don't move, rename or delete the owner's files (for example `Assets/Resources/audio/music_new/`), test leftovers or Unity-generated files without asking.
- Maintained Python/PowerShell tools live in un-ignored folders (`tools/discord`, `tools/audio`, `tools/localization`, `tools/*.ps1`); add a `.gitignore` exception for a new tool folder.

## ECC (Everything Claude Code)

ECC ([affaan-m/ECC](https://github.com/affaan-m/ECC), v2.2.3) is installed user-wide in `~/.claude` (agents, skills, commands, rules; no hooks), source checkout in `~/.claude/ecc-source`. Use it in this project where it fits:

- Agents: `planner` / `architect` before larger features or refactors, `csharp-reviewer` and `code-reviewer` on a change before committing, `python-reviewer` for `tools/**/*.py`, `security-reviewer` for save, platform, Steam and Discord code, `build-error-resolver` for compile errors, `doc-updater` for larger spec updates.
- Skills: `verification-loop` before calling work done, `dotnet-patterns`, `intent-driven-development` for vague requests, `council` for real trade-offs.
- Per task: feature → `planner`, build, tests, screenshots, `csharp-reviewer` + `code-reviewer`, fix CRITICAL/HIGH, `verification-loop`, commit. Bug fix → failing test first (`tdd-guide` habit), fix, review, commit with `GAP-NNN`. Tool change → `python-reviewer`, `python -m unittest` in the tool folder.
- Precedence: this file and `docs/` win over ECC's generic rules. Unity components and run state are mutable by design (ignore "never mutate"); tests are NUnit through the Unity Test Runner via `tools/run-unity-tests.ps1` (not xUnit; no fixed coverage target); requirement IDs and traceability stay mandatory.
- Hooks stay off. Enable them only on request (`configure-ecc`): GateGuard denies the first edit of every file and config-protection blocks linter/formatter changes.
