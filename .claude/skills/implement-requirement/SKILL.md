---
name: implement-requirement
description: Implement or change a gameplay feature from a spec requirement in this Unity project, keeping requirement → code → test traceability, architecture, determinism, and save compatibility intact. Use whenever writing or modifying C# gameplay code under Assets/Scripts for a spec'd feature, gap, ticket, or bug fix.
---

# Implement a Requirement

## 1. Pin down the requirement

- Load the `spec-analysis` skill and identify the exact requirement IDs (e.g. `BOSS-MOD-002`).
- Read the spec text and the `docs/REQUIREMENT_MAP.md` row for each ID: it names the owning file and method.
- If the spec is ambiguous or contradicts another spec, stop and ask. Do not invent behavior.

## 2. Respect the architecture

Primary entry points (see `docs/UnityImplementationSpec.md`):

| Concern | Owner |
|---|---|
| Startup, profile wiring, mode launch, resume | `Assets/Scripts/Bootstrap/GameBootstrap.cs` |
| Run state, level generation, rewards, economy, boss flow, special modes | `Assets/Scripts/Run/RunDirector.cs` |
| In-run UI flow and puzzle interaction | `Assets/Scripts/UI/InRunController.cs` (+ `InRunUiBlueprintBuilder`, `BoardViewController`, `HudViewController`) |
| Menu UI (built in code) | `MainMenuBlueprintBuilder*.cs`, `MainMenuController.cs` |
| Sudoku generation and rules | `Assets/Scripts/Sudoku/` (see `sudoku-generation` skill) |
| Boss modifier pool and phases | `Assets/Scripts/Boss/BossService.cs` |
| Saves | `Assets/Scripts/Save/`, models in `Assets/Scripts/Core/SaveModels.cs` |

Conventions:
- Namespaces follow the folder: `SudokuRoguelike.Core`, `.Run`, `.Sudoku`, `.UI`, `.Save`, ...
- Game logic lives in plain C# services that tests can construct without a scene. Keep MonoBehaviours thin.
- All game UI is built procedurally in C#, with no prefabs for game UI (see `procedural-ui` skill).
- No Newtonsoft JSON. Use Unity `JsonUtility` / `System.Text.Json` or the existing manual serialization.
- Match the surrounding file's style, comment density, and naming. `RunDirector` and `InRunController` are very large, so add focused helpers rather than growing giant methods.

## 3. Determinism

- All randomness in run, puzzle, and reward logic must come from a seeded `System.Random` derived from the run seed (+ level index or node), never `UnityEngine.Random` or an unseeded `new Random()`.
- The same seed must yield the same path, puzzle, modifiers, and rewards. If you add a new random roll, derive its RNG from the existing seed chain and don't consume extra draws from a shared RNG mid-sequence. Doing so shifts every later roll and breaks seed reproducibility and resume.

## 4. Save compatibility

Save format: one JSON envelope per slot (`save_profile_{0..2}.json` with `.tmp`/`.bak`) in `Application.persistentDataPath`; see `docs/SaveLoadArchitecture.md`.
- Adding a field: give it a safe default so older saves load unchanged. Never rename or remove a serialized field without a migration path.
- Nullable/complex types that `JsonUtility` can't serialize need the existing pattern: a `bool Has...` + value fields, or a `[NonSerialized]` runtime collection mirrored into a serialized `List<>` (see `RunState.SyncSeenModifiersToList`).
- If run state is new, make sure `RunAutoSaveCoordinator.Save` writes it and `RunResumeService.TryResumeFromSave` restores it.
- Add or extend a round-trip test for every new persisted field.

## 5. Traceability

- Put a `// [REQ: <ID>] <short description>` comment at the primary implementation site, matching the existing style (see `BossService.cs`).
- Add or extend a test that exercises the requirement (see `unity-testing` skill). Mention the ID in the test name or a comment.
- Update `REQUIREMENT_MAP.md` (file, method, status) and the spec's per-requirement status. Bump the spec version and add a Change History row.

## 6. Definition of done

A requirement may be marked ✅ only when all of these hold:
1. Code is in place and tagged.
2. A Unity EditMode/PlayMode test covering it passes through `tools/run-unity-tests.ps1` (not just `dotnet build`), **or** you observed the behavior at runtime (PlayMode harness / built player) where an automated test isn't practical. Record how it was verified.
3. Existing tests still pass.
4. Saves from before the change still load.

Otherwise mark it ⚠️ and state what verification is missing. Don't change unrelated gameplay behavior along the way. Report it instead.
