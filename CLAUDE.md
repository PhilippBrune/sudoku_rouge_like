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
