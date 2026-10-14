# CLAUDE.md

## Project Rules

- Treat `docs/` as the authoritative game specification.
- Preserve unique requirement IDs.
- Maintain traceability from requirement → implementation → test.
- Never mark a feature complete without runtime/E2E verification where applicable.
- Follow existing architecture, coding conventions, Unity best practices, deterministic testing, and save compatibility.
- Use the project's documented skills/workflows for specification analysis, implementation, debugging, UI, Sudoku generation, and automated testing.
- Run Unity tests with `tools/run-unity-tests.ps1` (batchmode). It fails if this project is open in the Unity Editor, so close the Editor first; a run that writes no results XML counts as "not run", never as passed.
- `docs/review/GAME_GAPS.md` is the fix backlog. Cite the gap ID (`GAP-NNN`) and requirement IDs in commit messages for fixes.
- Do not resolve specification contradictions (`docs/review/SPECIFICATION_ISSUES.md`, `SPEC-CONF-*`) without asking the user.

## ECC (Everything Claude Code)

ECC ([affaan-m/ECC](https://github.com/affaan-m/ECC), v2.2.3) is installed user-wide in `~/.claude` (agents, skills, commands, rules; no hooks), source checkout in `~/.claude/ecc-source`. Use it in this project where it fits:

- Agents: `planner` / `architect` before larger features or refactors, `csharp-reviewer` and `code-reviewer` on a change before committing, `security-reviewer` for save, platform and Steam code, `build-error-resolver` for compile errors.
- Skills: `verification-loop` before calling work done, `dotnet-patterns`, `intent-driven-development` for vague requests, `council` for real trade-offs.
- Precedence: this file and `docs/` win over ECC's generic rules. Unity components and run state are mutable by design (ignore "never mutate"); tests are NUnit through the Unity Test Runner via `tools/run-unity-tests.ps1` (not xUnit; no fixed coverage target); requirement IDs and traceability stay mandatory.
- Hooks stay off. Enable them only on request (`configure-ecc`): GateGuard denies the first edit of every file and config-protection blocks linter/formatter changes.
