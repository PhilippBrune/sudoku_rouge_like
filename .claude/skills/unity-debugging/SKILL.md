---
name: unity-debugging
description: Debug runtime errors, crashes, broken flows, save/resume problems, and test failures in this Unity project — locate logs, reproduce deterministically with seeds, isolate in a test, and fix the root cause. Use when something errors, behaves differently from the spec, a test fails, or a save won't load.
---

# Unity Debugging

## Where to look

| Source | Path |
|---|---|
| Editor log | `%LOCALAPPDATA%\Unity\Editor\Editor.log` (`Editor-prev.log` for the previous session) |
| Player log (built game) | `%USERPROFILE%\AppData\LocalLow\Kratossos\sudoku_rouge_like\Player.log` |
| Batchmode test logs/results | `TestResults\<Platform>-unity.log`, `TestResults\<Platform>-results.xml` |
| Save files | `%USERPROFILE%\AppData\LocalLow\Kratossos\sudoku_rouge_like\save_profile_{0,1,2}.json` (+ `.bak`, `.tmp`) |
| Built player | `build\windows\sudoku_rouge_like.exe` (may be stale, so check its timestamp against recent commits) |

Grep logs for `Exception`, `NullReferenceException`, `MissingReferenceException`, `error CS`, `Failed to`, and `[REQ:`-tagged warnings. Read the first exception, not the cascade after it.

## Workflow

1. **Reproduce deterministically.** Capture the run seed, class, floor/node, board size, stars, and active modifiers. Generation is seed-driven, so the same seed reproduces the same puzzle (`sudoku-generation` skill).
2. **Isolate in a test.** Write a failing EditMode test against the plain C# service (`RunDirector`, `PuzzleGenerationService`, `XpService`, ...) before touching code. Use a PlayMode harness only when the bug needs the UI or a frame loop (`unity-testing` skill).
3. **Find the root cause**, not the symptom. Check spec intent first (`spec-analysis` skill) because sometimes the code is right and the expectation is wrong.
4. **Fix minimally** in the owning system (`implement-requirement` skill). Don't add null-guards that hide a wiring bug. Find why the reference is null.
5. **Verify**: the new test passes in Unity batchmode, the existing suite stays green, and for flow bugs the scenario is re-run in-game.

## Known pitfalls in this repo

- **Batchmode exit 198**: Unity licensing. Sign in via Unity Hub.
- **Batchmode "another Unity instance is running"**: this project is open in the Editor. An editor on a different project is fine.
- **Scene backup recovery prompt** in the log: open the project once in the Editor and resolve the dialog.
- **`dotnet build` errors from stale `.csproj` entries**: the Unity-generated project files may reference missing sources. Use Unity batchmode as the authority (`docs/development.md`).
- **Cached level configs**: `RunMapController.GetFixedLevelConfig` caches configs. Mutating the cached object (instead of the returned clone) leaks boss-modifier choices across nodes.
- **Save fields lost on resume**: the field isn't serializable by `JsonUtility` (nullable, `HashSet`, `Dictionary`), or `RunAutoSaveCoordinator.Save` / `RunResumeService.TryResumeFromSave` doesn't copy it. See the `[NonSerialized]` + mirrored-`List<>` pattern in `RunState`.
- **UI controls dead after a language switch**: menus are rebuilt and listeners must be attached during build (`procedural-ui` skill).
- **Built-in Knob sprite / dual Graphic** errors: use the procedural sprite helpers, with Text and Image on separate GameObjects.

## Saves during debugging

Never delete or overwrite the user's real save slots. Back up `save_profile_*.json*` before experiments and restore them afterwards, or use the last slot the way PlayMode tests do.
