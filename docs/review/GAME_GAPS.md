# Game Gaps — Actionable List

**Review date:** 2026-10-01 · **Build under test:** `feature/game-ui-refactor` @ `20d5f59` + uncommitted working tree, Unity 6000.3.10f1, fresh Windows player build
**Companion docs:** [FULL_GAME_REVIEW](FULL_GAME_REVIEW.md) · [REQUIREMENT_TRACEABILITY](REQUIREMENT_TRACEABILITY.md) · [E2E_TEST_REPORT](E2E_TEST_REPORT.md) · [SPECIFICATION_ISSUES](SPECIFICATION_ISSUES.md)

Only actionable gaps are listed. Evidence IDs: `D##` = diagnostic EditMode test against the real runtime services, `E##` = observation in the built player. Both are described in the [E2E report](E2E_TEST_REPORT.md), and screenshots and logs are in [`evidence/`](evidence/). The review itself changed nothing; the fixes made afterwards on request are listed in the [fix log](#fix-log-2026-10-05) and, for release 1.1.0, in the [1.1 fix log](#fix-log-2026-10-07-release-110). `B##` = check in the 1.1 player build ([E2E report §10](E2E_TEST_REPORT.md#10-addendum-release-110-2026-10-07)).

Priority scale: **P0** game cannot be completed · **P1** major gameplay system broken · **P2** important feature incomplete · **P3** minor functionality · **P4** polish.

## Priority overview

| Gap | Title | Status | Severity | Priority |
|---|---|---|---|---|
| [GAP-001](#gap-001) | Resuming a run mid-puzzle crashes every digit entry (NullReferenceException) | FIXED (2026-10-05) | CRITICAL | P0 |
| [GAP-002](#gap-002) | After a non-final boss the game cannot continue (blank Continue, hidden relic panel) | FIXED (2026-10-05) | CRITICAL | P0 |
| [GAP-003](#gap-003) | Puzzle generation fails or takes 20–30 s; a failed node or boss is permanently blocked | FIXED (2026-10-05) | CRITICAL | P0 |
| [GAP-004](#gap-004) | Monthly Walk fails for 5 of 24 months (incl. October 2026), shows a stale board and traps the player | FIXED (2026-10-05) | HIGH | P1 |
| [GAP-005](#gap-005) | Player-chosen boss modifiers are silently dropped or have no overlay | FIXED (2026-10-05) | HIGH | P1 |
| [GAP-006](#gap-006) | Fresh install skips profile select, first-run setup and basics prompt | FIXED (1.1) | HIGH | P1 |
| [GAP-007](#gap-007) | Endless Zen / Spirit Trials draw inert modifiers and often fail to generate | FIXED (2026-10-05) | HIGH | P1 |
| [GAP-008](#gap-008) | Harmony difficulty mostly not wired; boss pick count wrong at higher Harmony | FIXED (1.1) | HIGH | P1 |
| [GAP-010](#gap-010) | Test gate does not run; 50 failing tests; tests write real saves; no requirement→test links | FIXED (2026-10-05) | HIGH | P1 |
| [GAP-009](#gap-009) | Tile XP entries lack size/stars/boss → wrong end-of-run breakdown, completion stuck | FIXED (1.1) | MEDIUM | P2 |
| [GAP-011](#gap-011) | "Wrong placement" means rule conflict, not wrong digit (uniqueness finding corrected: in-game puzzles are unique) | FIXED (1.1) | MEDIUM | P2 |
| [GAP-012](#gap-012) | Floor-modifier counts, boss choice counts and modifier min sizes differ from spec | FIXED (1.1, spec aligned) | MEDIUM | P2 |
| [GAP-013](#gap-013) | Board sizes outside floor ranges; map preview differs from generated puzzle | FIXED (1.1) | MEDIUM | P2 |
| [GAP-014](#gap-014) | Pressure mechanics roll one floor late (F2 never, F5 55 % instead of 80 %) | FIXED (1.1) | MEDIUM | P2 |
| [GAP-015](#gap-015) | Item-reward reroll is missing | FIXED (1.1) | MEDIUM | P2 |
| [GAP-016](#gap-016) | Buying pencil during a level is missing | FIXED (1.1) | MEDIUM | P2 |
| [GAP-017](#gap-017) | Ascension does not exist; per-class prestige unreachable | PARTIALLY FIXED (1.1: prestige; Ascension deferred) | MEDIUM | P2 |
| [GAP-018](#gap-018) | Save/resume re-rolls boss choices and rewards (determinism lost) | FIXED (1.1) | MEDIUM | P2 |
| [GAP-019](#gap-019) | Localization: German mojibake, untranslated UI, language not applied live | FIXED (2026-10-14: fr/es/pt complete, native review pending) | MEDIUM | P2 |
| [GAP-020](#gap-020) | No seed entry / View Seed / Abandon Run / View Modifiers | FIXED (1.1) | MEDIUM | P2 |
| [GAP-021](#gap-021) | Accepted boss relic was not added (observed via resume path) | FIXED (1.1) | MEDIUM | P2 |
| [GAP-023](#gap-023) | Class stats/passives/levels: 3-way spec conflict; no "base (+bonus)" display | FIXED (1.1) | MEDIUM | P2 |
| [GAP-030](#gap-030) | Audio assets are not valid WAV files; audio unverifiable | PARTIALLY FIXED (clips load; listening pending) | MEDIUM | P2 |
| [GAP-037](#gap-037) | Codex discoveries (and in-run option changes) are overwritten by the next autosave | FIXED (1.1) | MEDIUM | P2 |
| [GAP-022](#gap-022) | Path map: hidden "?" tiles, many cross-links, no player marker | FIXED (1.1) | LOW | P3 |
| [GAP-024](#gap-024) | Rest node gives +MaxHP/3 HP and +4 pencil (spec +2 / +3) | FIXED (1.1, spec aligned) | LOW | P3 |
| [GAP-025](#gap-025) | Boss gate has no intensity label; modifier legend panel missing | FIXED (1.1) | LOW | P3 |
| [GAP-026](#gap-026) | Boss debuffs have no delivery path; 4 debuffs not implemented | FIXED (1.1; 4 debuffs descoped) | LOW | P3 |
| [GAP-027](#gap-027) | "Collect 50,000 gold" counts gold held at run end | FIXED (1.1) | LOW | P3 |
| [GAP-028](#gap-028) | Daily/Monthly not on main menu; daily streak ignores skipped days | FIXED (1.1) | LOW | P3 |
| [GAP-031](#gap-031) | Mistakes are not autosaved (quit after a mistake undoes it) | FIXED (1.1) | LOW | P3 |
| [GAP-032](#gap-032) | Starting a new run silently discards the active run | FIXED (1.1) | LOW | P3 |
| [GAP-033](#gap-033) | Floor-1 tiles can roll 6★ (early-clarity guardrail) | FIXED (1.1) | LOW | P3 |
| [GAP-034](#gap-034) | Reed Pledge bonus XP booked to the previous tile / lost on first tile | FIXED (1.1) | LOW | P3 |
| [GAP-035](#gap-035) | Mistake HP penalty lives in the UI layer, not in `RunDirector` | FIXED (1.1) | LOW | P3 |
| [GAP-029](#gap-029) | UI readability and polish issues | PARTIALLY FIXED (1.1; in-game Options restyle open) | COSMETIC | P4 |
| [GAP-036](#gap-036) | `TryPurchaseShopOffer` charges gold when the item cannot be stored | FIXED (1.1) | LOW | P4 |
| [GAP-038](#gap-038) | XP log and run analytics were lost on resume | FIXED (1.1, found 2026-10-07) | MEDIUM | P2 |
| [GAP-040](#gap-040) | Item Block debuff applied the item effect but kept the item | FIXED (1.1, found 2026-10-07) | MEDIUM | P2 |
| [GAP-039](#gap-039) | Pressure visuals vanished after a board refresh; refreshes ate one-shot flashes | FIXED (1.1, found 2026-10-07) | LOW | P3 |
| [GAP-041](#gap-041) | Number Freak L3 "+1 Pencil" was never applied (catalog typo) | FIXED (1.1, found 2026-10-07) | LOW | P3 |
| [GAP-042](#gap-042) | Options dropdown lists hid their last option; no usable window mode | FIXED (1.1, found 2026-10-12) | MEDIUM | P2 |
| [GAP-045](#gap-045) | Boss fights never played boss music (the boss layer was never started) | FIXED (found 2026-10-14) | LOW | P3 |
| [GAP-044](#gap-044) | Region borders showed the previous puzzle's regions on a same-size board | FIXED (found 2026-10-14 by the owner) | HIGH | P1 |
| [GAP-043](#gap-043) | Without the `ActiveProfileSlot` pref the game opens an empty slot 0, not the existing profile | FIXED (0.1.5, found 2026-10-14) | LOW | P3 |

---


## Fix log (2026-10-05)

Done on request after the review: GAP-001, GAP-002, GAP-003/004/005/007 and GAP-010, plus the defects the GAP-010 test triage and the E2E verification exposed. Verification: Unity batchmode through the fixed `tools/run-unity-tests.ps1` on a mirror of the working tree (the project was open in the Editor), and a built Windows player (see the [E2E report addendum](E2E_TEST_REPORT.md#9-addendum-fix-verification-2026-10-05)).

**Test gate after the fix:** EditMode 351 tests, 315 passed, 0 failed, 36 skipped (26 `[Explicit]` review diagnostics, 9 tests waiting for `docs/release/*`, 1 legal-notice test waiting for SPEC-PROC-007). PlayMode 9 of 9 passed. Before: EditMode 307 (262 passed, 45 failed), PlayMode 8 (3 passed, 5 failed), and the runner itself ran nothing.

| Gap | Root cause | Change | Verified by |
|---|---|---|---|
| GAP-001 | Restore rebuilt the board only; config, constraint engine, overlay and counters stayed null. | `PuzzleSaveState` stores level config, overlay, level counters and fog-pending cells (`Has*` flags keep old saves loadable). `RunDirector.TryRestorePuzzleSaveState` rebuilds config, engine and overlay and re-initialises pressure from the level seed (PRESSURE-SAVE-002). Old saves fall back to a config rebuilt from run state. One resume sequence, `RunResumeService.ApplyToRunDirector`, used by `GameBootstrap.LaunchResume`. **Second defect, found in E2E:** `GameBootstrap` reuses one `RunDirector` for every launch, and `RunMapController.BindAutoSave` skipped binding when the director was the same, right after `BindRun` had dropped the coordinator. So only the first launch of a session autosaved; every later run or resume in that session saved nothing, and Save & Quit lost it. `BindAutoSave` now binds on every launch, which also reloads the cached envelope. | `ResumeMidPuzzleTests` (6 tests: floors 1–3, boss with chosen modifiers, legacy save, second run in one session — fails without the fix: "Expected: 7102 But was: 7101"); E2E-F01, F03 (7 resumes in one session) |
| GAP-002 | Regression from commit 0826fec, which overwrote `EndScreenViewController` with an older version (no font, no localization, no stats; also lost the Monthly Walk Ink Stamp award and run-end flavour text). The relic panel lives under the hidden path panel. | Restored the 9f19bba version and re-applied 0826fec's real changes (controller focus, Steam leaderboard, "Back" label). The Continue button gets focus; Continue switches to the path view before showing the relic choice. Fade coroutines stop when their panel is destroyed (exception when a relic was accepted mid-fade). | `BossClearedFlowPlayModeTests` (real `InRunController` flow: label font and text, focus, relic panel visible, relic added, Floor 2); wiring contract tests; E2E-F03 |
| GAP-003 | Unbounded backtracking (heavy-tailed run time); identical retry seed; impossible combinations in the pools. | Generator: node-budgeted restarts and node-bounded uniqueness checks (benchmark: plain irregular 9×9 p50 2.2 s → 57 ms). Fallback chain in `RunDirector.CreateBoardWithFallbacks`: rectangular regions → drop floor board-shaping modifiers → drop chosen ones last. Failed levels retry with a salted seed (`RunState.FailedGenerationSeeds`). Global structural modifiers always use rectangular regions (SPEC-AMB-014). Anti-Bishop excluded: unsatisfiable on its sizes (SPEC-CONF-038). | `GenerationReliabilityTests` (15 bosses across 5 floors with chosen modifiers in 7.8 s total; salted retry; rectangular rule; Anti-Bishop); `PuzzleGenerationServiceTests` fallback order |
| GAP-004 | Irregular variant + structural modifier + redundant modifier-uniqueness solver on a 4 s sync path; no level reset; no failure handling. | `StartSeasonalChallenge` clears the previous level; boards from the uniqueness generator are marked unique, so the redundant re-check is skipped; launch failure keeps the player on the menu with a localized message; a saved attempt resumes without regenerating. | `MonthlyWalk_TwoYearsOfMonthsGenerate` (24/24 months in 0.7 s, including the 5 that failed) and determinism test; E2E-F02 |
| GAP-005 | Fallback dropped all board-shaping modifiers; missing overlays were not retried. | Chosen boss modifiers are mandatory: kept until the last fallback step; when one has no geometry, up to two more boards are tried. | `BossPuzzles_AllFloors_GenerateWithChosenModifiers`; `ResumeBossPuzzle_KeepsChosenBossModifiersActive` |
| GAP-007 | Modes drew from the whole `BossModifierId` enum; `IsSupportedForGeneratedRun` always returned true. | `BossService.GetGeneratablePlayerModifiers` + `DrawCombinable` for Endless Zen and Spirit Trials; `IsSupportedForGeneratedRun` rejects IDs without rules and Anti-Bishop. | `SpecialModes_DrawOnlyModifiersThatHaveRulesAndGenerate`, `EndlessZenAndSpiritTrials_StartWithABoard` |
| GAP-010 | Runner passed `-quit`; tests used the real save folder; stale and broken tests. | Runner: no `-quit`, stale XML removed, summary printed, fails on 0 tests or failures, `-TestFilter`/`-TestCategory`, refuses to run while the Editor holds the project, waits only for Unity (an adb server it spawns used to hang the script). Tests write to a temp save root (`SaveFileService.RootDirectoryOverride` via a `[SetUpFixture]`). `[Requirement]` attribute: 41 tags link tests to requirement IDs (category `REQ:<id>`); `RequirementTraceabilityTests` rejects unknown IDs. Triage of all 50 failures below. | The suite results above |
| GAP-009 (part) | `XpService.CalculateTile` never set size, stars or boss flag. | Fields set. Completion keys are now real (`9x9_s2` instead of `0x0_s0`). Remaining: Reed Pledge attribution (GAP-034), completion cap. | `RunFlowAcceptanceTests` (both pass now) |
| GAP-019 (part) | Menu rebuild created a second canvas over the old one; keys missing. | `EnsureCanvas` reuses the builder's canvas, so a language change rebuilds the visible menu. Added the missing keys in all five catalogs; localized the Custom Puzzle summary. Remaining: de.json mojibake, other hard-coded strings. | `GeneratedUiRebuildTests`, German PlayMode traversal |
| GAP-030 (part) | All 49 WAVs had each 4-byte chunk ID ("RIFF", "WAVE", "fmt ", "data") written as one `0x01` byte. | Headers rebuilt; samples unchanged. The game now loads the authored pack instead of the procedural fallback. Listening check still needed. | `AudioAssetServiceTests` (RIFF check passes) |

**GAP-010 triage of the 50 failing tests:**

| Group | Tests | Outcome |
|---|---:|---|
| Real defects, fixed in code | 16 | 0826fec regressions: boss-cleared screen, victory screen, run-end flavour text, curse localization (4). XP tile metadata (2). Resume read the save before a queued autosave was flushed (1). Generated main menu used services before `Awake` (3). Menu rebuild stacked a second canvas and leaked a preview handler (1). Custom Puzzle selection summary never built (1). In-run Options button unwired until a run started (2). German keys missing / `∞` key corrupted (1). Saved-language load not null-safe (1). |
| Assets, registers, localization | 4 | WAV headers repaired; `Localization/` resources root registered; Steam leaderboard stub registered; three hard-coded UI strings localized. |
| Stale or wrong tests, updated to current intent | 17 | Re-wrapped docs read with whitespace normalized (4); in-game credits moved to the catalog (2); wiring evidence through factory calls (1); banner deliberately hidden in 0826fec (1); conditional Save & Quit label (1); `SetContext` is public (1); splash scene name is a field (1); RequireComponent makes "missing AudioSource" impossible (1); a starting relic absorbed the Ginkgo test's mistake (1); title literal stored escaped (1); intentional error log not expected (1); options toggles are named `<id>Toggle` (1); locked modes need an unlocked profile (1). |
| Controller size budgets | 3 | Extracted `RunDirector.LevelGeneration.cs`, `InRunController.Completion.cs`, `MainMenuBlueprintBuilder.CustomPuzzlePanel.cs`; budgets lowered to 3060 / 5000 / 3050. |
| Missing documents | 9 | `docs/release/*.md` never existed in git; the tests skip with that reason until the documents are added. |
| Owner decision | 1 | Credits claim "no Steamworks binaries" while they are committed: skipped, see SPEC-PROC-007. |
| **Total** | **50** | 45 EditMode + 5 PlayMode |

New spec items raised by the fixes: SPEC-CONF-038 (Anti-Bishop), SPEC-AMB-014 (structural modifiers on irregular regions), SPEC-PROC-007 (Steamworks notice). Correction: the review's "puzzles are often non-unique" finding (GAP-011, D02) measured a generator path the game never uses.

---

## Fix log (2026-10-07, release 1.1.0)

Done on request for release 1.1.0: every remaining gap's suggested fix, plus the recommendations of [SPECIFICATION_ISSUES](SPECIFICATION_ISSUES.md). Where a spec recommendation was doc-only but the gap asked for code, the code was changed and the spec documents it. Verification: Unity batchmode through `tools/run-unity-tests.ps1` on a mirror of the working tree, and the 1.1 player build ([E2E report §10](E2E_TEST_REPORT.md#10-addendum-release-110-2026-10-07)).

**Test gate for 1.1:** EditMode 492 tests (2026-10-13, with the display-mode work): 466 passed, 0 failed, 26 skipped (the `[Explicit]` review diagnostics). PlayMode 25 of 25 passed (was 9 after the 2026-10-05 fix pass). The 9 tests that waited for `docs/release/*` and the Steamworks legal-notice test now run and pass.

| Gap | Change | Verified by |
|---|---|---|
| GAP-006 | `MainMenuController.Start()` called `ShowMainMenu()` and replaced the Profile Select screen that `GameBootstrap` had chosen when the splash scene ran first. The menu now keeps a startup screen chosen before `Start` (`_startupScreenChosen`); each startup step is logged (`[GameBootstrap] Startup step:`). | `StartupFlowPlayModeTests` (fails without the fix), `FirstRunSetupPlayModeTests` |
| GAP-008 | Every Harmony field is read: boss option/pick counts and floor bonus, HP cost per mistake, pencil drain from H7, gold multiplier, Nothing chance, grid and star weights (renormalised within the floor range), combo halving from H7, cursed-puzzle bonus off from H9, rest heal halved at H10, hint masking from H5. The XP multiplier applies to every run end; the flat `HarmonyLevel × 50` bonus is gone (SPEC-CONF-024/025). | `HarmonyEffectsTests` (14), `InRunUiPlayModeTests.BossGate_*Harmony5*` |
| GAP-009 | `CompletionService.RecordModifierCleared` is called for every modifier of a defeated boss at run end (`RunResult.ClearedBossModifiers`); Reed Pledge see GAP-034. | `CompletionTrackingTests` |
| GAP-011 | A digit is wrong when it differs from the solution (SPEC-CONF-003). Wrong digits are rejected and cost HP; a digit that only repeats a conflict is no longer "correct". Custom Puzzle 7★ boards, which have many solutions, stay rule-based. | `PlacementRulesTests` |
| GAP-012 | Specs now carry the code's tables: floor modifiers 0/1/1/1/2, Floor N shows N + 1 boss options and picks N (2/1 … 6/5, plus the Harmony bonus), modifier minimum sizes from `BossService.ModifierData`. The stale "min 3 / min 2" comment is gone. | `HarmonyEffectsTests.BossOptionsAndFloorBonus_MatchSpecTable`, `BalanceBudgetContractTests` |
| GAP-013 | Boss boards follow the floor's size range (no `Max(size, 8)`); the modifier pool is size-aware. Node configs are built once per floor and saved in `RunState`, so the map preview equals the generated puzzle. | `NodeConfigPreviewTests` |
| GAP-014 | `RollPressureMechanic` uses the 0-based floor index. | `PressureRollTests` |
| GAP-015 | Item-reward reroll wired: reroll tokens first, then 20 + 20n gold; Nothing slots stay Nothing. | `EconomyActionTests`, `GeneratedUiWiringContractTests` |
| GAP-016 | Buy Pencil during a puzzle: +3 pencil for 20 + 20n gold; not for Garden Monk. | `EconomyActionTests`, `GeneratedUiWiringContractTests` |
| GAP-017 | Prestige button in Meta Progression with a two-step confirmation and status lines (`ProfileService.TryPrestigeClass`). Ascension is deferred (FUTURE-009); the summary shows total prestige instead. The prestige reward wording conflicts between two specs: SPEC-CONF-039, owner decision. | `PrestigeTests` |
| GAP-018 | Every random decision uses its own seed (run seed, floor, depth, node, purpose: `RunDirector.DecisionSeed`). Rolled boss options, rewards and relic choices are saved, so Save & Quit cannot re-roll them. | `DeterminismAcrossResumeTests` |
| GAP-019 | de.json: 60 double-encoded lines repaired; encoding guard over all catalogs and production `.cs` files. Localized: boss gate plurals, Harmony names and descriptions 0–10 (all languages), numpad mode, path stats bar, Save & Quit label, resume class name, puzzle header and hint, async status, Endless Zen key. French, Spanish and Portuguese still fall back to English for 431 gameplay keys each (FUTURE-014). | `LocalizationCatalogQualityTests`, German PlayMode traversal |
| GAP-020 | Seed field on the class screen (`LaunchRequest.HasSeed/Seed`). In-run menu: run seed, View Modifiers, Abandon Run (two-step). | `RunSeedAndAbandonTests`, `InRunUiPlayModeTests.RunMenu_*` |
| GAP-021 | Relic choices are rolled once with a decision seed and saved; accepting adds the relic, also after a resume. | `DeterminismAcrossResumeTests`, `GameplayEffectContractTests` |
| GAP-022 | Exactly one cross-link per floor between the middle-lane tiles, ⇄ badge on both; tiles keep their type and are all visible; player marker on the current tile. Saves from 1.0 keep their old layout until the next floor (`FloorGraphVersion`). | `RunGraphTests`, `InRunUiPlayModeTests.PathMap_*` |
| GAP-023 | Class info shows base (+level bonus) for HP, pencil, slots, gold, rerolls (`RunArchetypeService.GetLevelBonuses`). ClassXpProgressionSystem's class section is generated from `ClassCatalog`. Found on the way: GAP-041. | `ClassRosterTests` |
| GAP-024 | Spec aligned to the code values: heal max(1, MaxHP / 3), +4 pencil, +1 reroll, Cleanse (SPEC-CONF-017). | `RestNodeValuesTests` |
| GAP-025 | Boss gate shows the intensity. Modifier legend: collapsible list, tap an entry to isolate its overlay for 3 s (others at 15 %), auto-collapse after 5 s. | `InRunUiPlayModeTests.Legend_*`, `BossGate_*` |
| GAP-026 | One debuff per Garden Run boss from Floor 3, from run seed + floor; CrossWipe and PencilScramble only at VeryHigh; GivenReveal never delivered. CellBlur is now rendered; Item Block blocks the whole item use (GAP-040). PencilPoison, FogPulse, NumberBlind and DigitShift are descoped (FUTURE-010). | `BossDebuffTests` (23), `InRunUiPlayModeTests.BossGate_*` |
| GAP-027 | `RunState.GoldCollected` counts every gain; the 50,000 gold unlock reads it. | `EconomyActionTests.CollectedGold_*` |
| GAP-028 | Daily Walk and Monthly Walk widgets on the main menu with an Open button; the streak resets after a missed day. | `GeneratedUiPlayModeTraversalTests.HomeWidgets_*`, `DailyStreakTests` |
| GAP-029 | Digit styles (bold ivory givens, blue player digits, dark digit on the selected cell), size × stars notation everywhere, gold label synced every frame, Monthly Walk header, toggle boxes (main menu, in-run, first-run), tooltip leak, locked-class contrast, fitted action labels, system button heights, Harmony description strip, "Allow irregular puzzles" ON by default (SPEC-CONF-031). Open: in-game Options restyle (FUTURE-015). | `GeneratedUiPlayModeTraversalTests`, `InRunUpdateHelperTests` |
| GAP-030 | `AudioAssetService` logs each authored clip (length, rate, channels) and warns when one is missing. All clips load. Finding: every music loop is 6.0 s mono, an asset question for the owner. | `AudioClipPlayModeTests`, `AudioAssetServiceTests` |
| GAP-031 | Autosave after every mistake. | `PlacementRulesTests` |
| GAP-032 | Starting a run while one is saved shows a warning with "Abandon & Begin"; the abandoned run is recorded as a defeat (`RunResumeService.AbandonSavedRun`, `RunResultFactory`). | `RunSeedAndAbandonTests` |
| GAP-033 | Non-boss puzzles on floors 1–2 are at most one star above the floor default (REQ-RUN-106). | `HarmonyEffectsTests.StarVariance_*` |
| GAP-034 | The Reed Pledge bonus is booked on the tile that fulfilled it. | `CompletionTrackingTests.ReedPledgeBonus_*` |
| GAP-035 | The mistake penalty (HP, debuff hooks, Harmony cost) is applied inside `RunDirector.PlaceNumber`. | `PlacementRulesTests` |
| GAP-036 | The shop charges only when the item is stored (or swapped in one step). | `DeterminismAcrossResumeTests` (full-bag purchase) |
| GAP-037 | `RunAutoSaveCoordinator` reloads the file before every autosave and replaces only the fields it owns, so codex discoveries and in-run option changes survive. | `AutosaveMergeTests` |
| GAP-038 | New, see below. | `DeterminismAcrossResumeTests` |
| GAP-039 | New, see below. | `InRunUiPlayModeTests` (pressure tests) |
| GAP-040 | New, see below. | `BossDebuffTests.RevealCost_*` |
| GAP-041 | New, see below. | `ClassRosterTests.EveryStatBonusInTheSchedule_IsApplied` |

**Build smoke (E2E report §10, B01–B15):** all checks pass. Two defects found there were fixed and re-tested: the profile screen showed the raw key `Profile.EmptySlot` in English (GAP-019 follow-up; a new test, `KeyOnlyLookups_HaveEnglishText`, found four more such keys), and the View Modifiers list was see-through over the Options labels (GAP-029 follow-up).

**Test-run stability (2026-10-14, GAP-010 follow-up):** PlayMode results depended on whether the Steam client was running. Abandoning a run in `InRunUiPlayModeTests` submits the leaderboard score, which started `SteamManager`, and its `SteamAPI_Init` logs an error without a client. The test-isolation fixtures now set `PlatformServiceRegistry.SteamworksCallsEnabled = false`, so achievements, stats and the leaderboard never call Steamworks in test runs; the game is unchanged (the flag defaults to true). Verified with Steam closed: EditMode 492 (466 passed, 0 failed, 26 skipped), PlayMode 25/25.

**Release blocker PRG-PLAYER-METADATA resolved (2026-10-14):** `SteamManager` restarts through App 4858470. A first fix renamed the product to "Run of the Nine" (commit 3bc7a48); the same day the Steamworks portal showed that the live 1.0 build (Steam build 24153478, 10 Jul) is `sudoku_rouge_like.exe` with `app.info` = Kratossos / sudoku_rouge_like, and that the launch option and Steam Auto-Cloud (`WinAppDataLocalLow/Kratossos/sudoku_rouge_like/`) point there. The rename would have moved every player's saves out of the cloud path, so it was withdrawn before any upload: the names stay Kratossos / sudoku_rouge_like (SAVE-FOLDER-001, guarded by `LegacySaveMigrationTests.PlayerSettings_KeepTheSaveFolderOfTheReleasedGame`), the Windows player sets the window title "Run of the Nine" (DISPLAY-MODE-007, build check B23), and `LegacySaveMigration` copies only saves of the pre-release builds (`DefaultCompany/RunOfTheNine`, SAVE-MIGRATE-001). The FULL_GAME_REVIEW note that the Steam-reviewed build used DefaultCompany / RunOfTheNine applies to those pre-release builds, not to the released one.

Repository clean-up and release preparation for 1.1.0 are recorded in `CHANGELOG.md` and `docs/release/release-readiness-status.md`.

---

## GAP-001
**Resuming a run mid-puzzle crashes every digit entry (NullReferenceException)**

- **Requirement:** REQ-SAVE-100, SAVE-RESUME-001, PRESSURE-SAVE-002, REQ-UI-105
- **Status:** FIXED (2026-10-05, see [fix log](#fix-log-2026-10-05)); was BROKEN
- **Severity:** CRITICAL · **Priority:** P0
- **Problem:** The game autosaves after every correct placement, so "Save & Quit" (or a crash or Alt-F4) usually happens mid-puzzle. After Resume the board is shown, but **every** digit entry throws `NullReferenceException` in `RunDirector.PlaceNumber`. Completing the level would also throw (`CompleteLevelAndGrantRewards` dereferences the null config). The run can only be abandoned by starting a new one. In one session the main menu then became completely unresponsive (mouse, keyboard and Quit), which required force-closing the game.
- **Expected:** Resume restores the exact puzzle: board, givens, pencil marks, active modifiers and their overlay, constraint rules, fog, locks, pressure threats, mistakes/perfect state. Play continues normally.
- **Actual:** `CurrentLevelConfig`, `ConstraintEngine` and `CurrentOverlay` are `null` after restore. Floor modifiers vanish, wrong digits lose their conflict highlight, the star rating disappears from the header, and the puzzle timer resets. The first input throws (stack: `RunDirector.PlaceNumber` ← `InRunController.EnterNumber` ← numpad button).
- **Root Cause:** `RunDirector.TryRestorePuzzleSaveState` only rebuilds the board, given mask, pencil marks, cell locks and blurred cells. `GameBootstrap.LaunchResume` calls `StartRun` (which nulls config, engine and overlay), then `RestoreState`, `RebuildFloorGraph` and `TryRestorePuzzleSaveState`. Nothing reconstructs the level config, constraint engine, overlay, `LevelState` counters or pressure state, and `PuzzleSaveState` does not persist the level config. The Seasonal resume path avoids this only because it calls `StartSeasonalChallenge` first.
- **Suggested Fix:**
  1. Persist the level definition in `PuzzleSaveState`: new fields with safe defaults (`BoardSize`, `Stars`, `RegionVariant`, `Seed`, `ActiveModifiers`, `Intensity`, `IsBoss/IsElite/IsPreBoss`, `IsCursed` plus multipliers), plus `LevelState` counters (`Mistakes`, `PerfectSoFar`, `NoPencilUsed`, `CorrectPlacements`, `HpLost`).
  2. In `TryRestorePuzzleSaveState` set `CurrentLevelConfig`. Rebuild `ConstraintEngine` from `ModifierFactory.BuildRules(config.ActiveModifiers)`. Regenerate `CurrentOverlay` deterministically from the saved solution and seed through `ModifierGeometryGenerator.Generate` (or persist the overlay lists). Re-apply `FogHiddenCells`, and re-initialise pressure with the same seed (PRESSURE-SAVE-002 already states this intent).
  3. Save compatibility: when the new fields are absent (older saves), reconstruct the config from the saved node through the deterministic `BuildLevelConfig(nodeType, nodeIndex)` path.
  4. Investigate the post-exception soft lock (E10). Input handlers should not leave panel or flow flags in a half-switched state.
- **Affected Files:** `Assets/Scripts/Run/RunDirector.cs` (`TryRestorePuzzleSaveState`, `ExportPuzzleSaveState`, `PlaceNumber`), `Assets/Scripts/Core/SaveModels.cs` (`PuzzleSaveState`), `Assets/Scripts/Bootstrap/GameBootstrap.cs` (`LaunchResume`), `Assets/Scripts/Save/RunResumeService.cs`, `Assets/Scripts/UI/InRunController.cs` (`ApplyResumeState`)
- **Required Tests:** Promote D17 to a permanent EditMode test. Save mid-puzzle on floors 1–5 including fog, a pressure mechanic, a boss with chosen modifiers and a cursed node; resume through the exact `LaunchResume` sequence; assert placements work (correct and conflicting), rule count and overlay signature equal pre-save, the level completes and grants rewards. Add a legacy-save fixture (no new fields) that still resumes. Add a PlayMode test: Save & Quit → Resume → numpad entry → no exception, then return to menu → menu is interactive.
- **Evidence:** D12, D17 ([probe2](evidence/diagnostics_batch2_probe.txt)); E09/E10 ([stack](evidence/e2e_resume_nre_stack.txt), [screen](evidence/screens/E09_resume_board_after_nre.jpg)); reproduced in-session and after a full application restart.

## GAP-002
**After a non-final boss the game cannot continue**

- **Requirement:** REQ-BOSS-107, REQ-UI-107, MAP-STRUCT-001 (floor transition)
- **Status:** FIXED (2026-10-05, see [fix log](#fix-log-2026-10-05)); was BROKEN
- **Severity:** CRITICAL · **Priority:** P0 (a workaround exists, but a normal player has no way to find it)
- **Problem:** After solving the Floor 1 boss, the "Boss Defeated!" interstitial shows an empty stats panel and a button with **no visible label**. Clicking it closes the interstitial, but the relic-choice panel never becomes visible: the solved boss board stays on screen, nothing advances, and no further log line is written. Only Save & Quit → Resume shows the relic panel ("Reliktknoten – Wähle eins") and allows Floor 2.
- **Expected:** "Boss Defeated!" with stats (tiles, relics, gold, XP) and a labelled, localized **Continue**, then the relic choice, then the Garden Cleared card and cutscene, then Floor 2 (PathSystem §Floor Transition).
- **Actual:** Blank button, empty panel, hidden relic choice, progression blocked.
- **Root Cause:** `EndScreenViewController.ShowBossCleared` creates the button label as `new GameObject("Label", typeof(Text))` without assigning a font. In a player build a legacy `Text` without a font renders nothing. The text is also hard-coded English ("Continue →", "Boss Defeated!"). The `onClaim` callback (`InRunController` ~l.4146–4152) calls `HideBossCleared()` and `_rewardView.ShowRelicChoicePanel(...)` while the sudoku panel is the active view, so the relic panel (under the reward/path root) is not visible. The baseline `GeneratedUiWiringContractTests.BossClearedRewardScreen_HidesDetailsScrimAndCentersStats` and `…VictoryScreen_ClearsBossInterstitial…` already fail on this branch.
- **Suggested Fix:** Build the Continue button through `InRunUiFactory.CreateActionButton` (font, localization, controller navigation and first-selected handled consistently), and register it in `GeneratedUiRegistry`. Populate the stats from `EndScreenViewController.BuildBossClearedStats`. In `onClaim`, switch to the path/reward view (or re-parent the relic panel to the top-level overlay canvas) before `ShowRelicChoicePanel`, then continue to `AdvanceFloorAfterBossCleared`. Localize all strings.
- **Affected Files:** `Assets/Scripts/UI/EndScreenViewController.cs` (`ShowBossCleared`), `Assets/Scripts/UI/InRunController.cs` (boss-complete branch ~l.4130–4230), `Assets/Scripts/UI/RewardViewController.cs` (`ShowRelicChoicePanel`), localization catalogs
- **Required Tests:** PlayMode test: seeded run, complete the Floor 1 boss through `RunDirector`, then assert `BossClearedContinueBtn` has a non-empty, rendered label (font not null) and is the selected element, click it, assert the relic panel is active and visible in hierarchy, accept, assert `HeldRelics` grew by 1 and `CurrentFloor == 1`. Make the two failing wiring tests pass.
- **Evidence:** E18 ([blank button](evidence/screens/E18_boss_defeated_blank_continue.jpg), [stuck](evidence/screens/E18b_stuck_after_boss.jpg), [panel after resume](evidence/screens/E18c_relic_panel_after_resume.jpg)), E19.

## GAP-003
**Puzzle generation fails or takes 20–30 s; failed nodes are permanently blocked**

- **Requirement:** REQ-SUD-101, REQ-PERF-100, REQ-TUT-100, BOSS-MOD-003, PERF-CAP-004
- **Status:** FIXED (2026-10-05, see [fix log](#fix-log-2026-10-05)); was BROKEN
- **Severity:** CRITICAL · **Priority:** P0
- **Problem:** Many reachable configurations exceed the generation budget:
  - 9×9 irregular layouts (variants 2/3), especially with board-shaping modifiers (Antiking, NonconsecDiagonal, DistanceGe2, Nonconsecutive, AntiBishop, EntropyGlobal).
  - In the real async boss path, 1 of 40 boss puzzles failed **and failed again on retry**: the retry rebuilds the identical seed, so the final boss of that run can never be played. 8 of 40 bosses took 24–30 s.
  - About 10 % of Floor 4–5 normal puzzles failed. On one seed three nodes of the same Floor 5 failed (EntropyGlobal floor modifier + irregular variant), effectively blocking that floor.
  - AntiBishop never generates on 9×9.
  - Custom Puzzle 9×9 jigsaw fails at 1★, 6★ and 7★.
  - The raw generator took 20.3 s / 15.0 s / 6.3 s for some 9×9 jigsaw boards.
- **Expected:** Every configuration reachable in normal play generates within the documented budget (≤ 6 s async), and generation never permanently blocks progress.
- **Actual:** See D05, D14, D18, D20, D22–D24 and E13. On failure the UI rolls back to the map (`RunMapController.RestoreNavigationSnapshot`), and the next attempt uses the same `config.Seed`, so the failure is deterministic.
- **Root Cause:** Backtracking (MRV) over irregular 9×9 templates plus structural constraints has heavy-tailed run time. The pools allow combinations the generator cannot satisfy in budget (AntiBishop on 9×9, Entropy with irregular regions). The retry seed is derived only from run seed, depth, floor and node index, so retries are identical. The fallback (`TryCreateBoardAfterDroppingBoardShapingModifiers`) can still fail, and drops player choices (see GAP-005).
- **Suggested Fix:**
  1. Never block: on failure, retry with a salted seed (persist an attempt counter per node in `RunState`). Then degrade in a defined order: irregular → rectangular variant → drop *floor* (not chosen) structural modifiers → base board. Show a short notice when a fallback was used.
  2. Make generation fast: randomized restarts with a node budget instead of one long backtrack; pre-validated jigsaw templates; for structural rules, generate solutions by transforming a pre-solved compliant grid (digit permutation, band/stack shuffles where the region map allows).
  3. Gate the pools: exclude AntiBishop from 9×9 until it can generate; forbid Entropy together with irregular variants (or fix the generator); add the observed failing pairs to `BossService.IsBlockedCombination` only as a stop-gap.
  4. Pre-bake bosses for each possible choice set while the boss gate is open, and show progress for anything over 2 s.
- **Affected Files:** `Assets/Scripts/Sudoku/SudokuGenerator.cs`, `Assets/Scripts/Sudoku/PuzzleGenerationService.cs`, `Assets/Scripts/Run/RunDirector.cs` (`StartLevelAsync`, `CreatePuzzleForConfig*`, `BuildLevelConfig`), `Assets/Scripts/Boss/BossService.cs`, `Assets/Scripts/UI/RunMapController.cs`, `Assets/Scripts/UI/TutorialMenuController.cs`
- **Required Tests:** Generation matrix test (nightly CI): floors 1–5 × node types × variants 0–3 × observed modifier combinations × ≥20 seeds, asserting success within budget. Per-modifier 9×9 × variant 0–3 test. A retry-after-failure test asserting a different seed and success. A custom-puzzle size × star × variant matrix. Promote D18 and D20.
- **Evidence:** D05, D14, D18, D20 ([probe2](evidence/diagnostics_batch2_probe.txt)), D02 timings ([probe1](evidence/diagnostics_batch1_probe.txt)), D22–D24 ([probe3](evidence/diagnostics_batch3_probe.txt)), E13.

## GAP-004
**Monthly Walk fails for 5 of 24 months, shows a stale board and traps the player**

- **Requirement:** SEASON-PUZZLE-001, REQ-UI-104
- **Status:** FIXED (2026-10-05, see [fix log](#fix-log-2026-10-05)); was BROKEN
- **Severity:** HIGH · **Priority:** P1
- **Problem:** The monthly puzzle for 2026-02, 2026-03, 2026-07, **2026-10 (current month)** and 2027-03 never generates. In the build, "Monatsgang beginnen" logged `BudgetExceeded` for seed 202610 but still logged "Launched Seasonal Challenge 2026-10". It then displayed the **previous Garden Run's 7×7 board** (including that run's wrong digits) with the Anti-Springer rule text. Save & Quit is refused ("puzzle still loading"), and the in-game options Save & Quit does nothing, so the only exit is force-closing the game.
- **Expected:** A fixed 9×9 ★★★★ puzzle with 1–2 modifiers every month (SeasonalChallengeSpec). If generation ever fails, return to the menu with a message and never show another mode's board.
- **Actual:** As above; personal-best integrity is also at risk if the stale board can be completed.
- **Root Cause:** `SeasonalChallengeService.BuildChallengeConfig` uses region variant `seed % 4` (irregular for half of all months) and requires modifier-aware uniqueness, on the synchronous 4 s path. `RunDirector.StartSeasonalChallenge` does not clear `CurrentBoard`/`CurrentLevelState` from the previous run before `StartLevel`. The in-run "still loading" guard treats a failed generation as pending.
- **Suggested Fix:** Clear level state in `StartSeasonalChallenge`, as `StartRun` does. If `CurrentBoard == null` after generation, abort to the menu with a localized message. Make the monthly config generate deterministically: validate the next 36 months in a unit test and fall back to variant 0 or a different modifier draw when a month fails. Allow Save & Quit/Back whenever no puzzle is active.
- **Affected Files:** `Assets/Scripts/Run/SeasonalChallengeService.cs`, `Assets/Scripts/Run/RunDirector.cs` (`StartSeasonalChallenge`), `Assets/Scripts/Bootstrap/GameBootstrap.cs` (`LaunchSeasonalChallenge`), `Assets/Scripts/UI/InRunController.cs` (save-quit guard)
- **Required Tests:** EditMode test asserting 36 consecutive months generate (promote D24). PlayMode test with forced generation failure, asserting a return to the menu, no stale board, and that the player can leave.
- **Evidence:** D24; E12/E13 ([panel](evidence/screens/E12_monthly_walk_panel.jpg), [stale board](evidence/screens/E13_monthly_walk_stale_board.jpg), [save refused](evidence/screens/E13b_monthly_walk_save_refused.jpg), [log](evidence/e2e_monthly_walk_log.txt)).

## GAP-005
**Player-chosen boss modifiers are silently dropped or have no overlay**

- **Requirement:** REQ-BOSS-101 (spec: "The boss puzzle launches with floor modifiers + boss-chosen modifiers active simultaneously")
- **Status:** FIXED (2026-10-05, see [fix log](#fix-log-2026-10-05)); was BROKEN
- **Severity:** HIGH · **Priority:** P1
- **Problem:** In 8 of 40 boss puzzles (async UI path, 8 seeds × 5 floors), chosen or floor modifiers were removed (Antiking, DistanceGe2, NonconsecDiagonal, Nonconsecutive), or a chosen geometric modifier (Thermo) had no overlay elements. Nothing tells the player. On the one seed played in the build, the chosen Difference Kropki was present.
- **Expected:** Every chosen modifier is active and visible. If that's impossible, the choice is not offered.
- **Actual:** `LastGenerationMissingModifiers` non-empty; `CurrentLevelConfig.ActiveModifiers` missing chosen IDs.
- **Root Cause:** The `StartLevelAsync` fallback `TryCreateBoardAfterDroppingBoardShapingModifiers` removes structural modifiers when the board cannot be generated in budget. The overlay retry ("5 boards × 12 seeds", `HasAllModifiersPresent`) is not enforced on this path. `BuildLevelConfig` also drops modifiers that fail `CanCombineModifiers`.
- **Suggested Fix:** Treat chosen modifiers as mandatory. Regenerate with new seeds or variants first, then drop only floor modifiers, and finally fail back to the gate with a re-roll instead of silently dropping. Validate the offered choices by pre-baking while the gate is shown, and enforce `HasAllModifiersPresent()` with board regeneration. Fixing GAP-003 reduces the frequency.
- **Affected Files:** `Assets/Scripts/Run/RunDirector.cs` (`StartLevelAsync`, `TryCreateBoardAfterDroppingBoardShapingModifiers`, `BuildLevelConfig`), `Assets/Scripts/Sudoku/PuzzleGenerationService.cs`, `Assets/Scripts/UI/BossGateViewController.cs`
- **Required Tests:** Promote D18 (≥40 seeds): `chosen ⊆ active`, `missing == ∅`, solution satisfies all rules. The existing `RunFlowAcceptanceTests.BossFlow_…` must pass.
- **Evidence:** D05, D18; E17 ([gate](evidence/screens/E17_boss_gate_unknown_mods.jpg), [puzzle](evidence/screens/E17b_boss_puzzle_kropki.jpg)).

## GAP-006
**Fresh install skips profile select, first-run setup and basics prompt**

- **Requirement:** REQ-UI-100, INPUT-PROFILE-001, INTRO-002, TUTO-BASICS-TRIGGER-001/003
- **Status:** FIXED (1.1, 2026-10-07, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was BROKEN
- **Severity:** HIGH · **Priority:** P1
- **Problem:** With no save files and no PlayerPrefs, the first launch went straight to the main menu: no profile selection, no language/accessibility/legal setup, no Sudoku-basics prompt. The player started a run without accepting the legal notice. The setup only appeared on the *second* launch, after the run had created a profile.
- **Expected:** startup-experience.md order: profile select → first-run setup → basics prompt → main menu.
- **Actual:** Main menu directly (E01); setup on the next launch (E01b).
- **Root Cause (hypothesis, not confirmed):** `StartupFlowController.GetNextStep` returns `ProfileSelect` correctly for `HasAnyProfile == false`. The observed skip points to ordering. `RunStartupFlow` runs inside `ShowMenuAfterSplash` before or while `MainMenuBlueprintBuilder` (re)builds ("Menu built" is logged afterwards), so the shown panel is replaced by the main menu. No log line records the chosen startup step.
- **Suggested Fix:** Log the chosen `StartupFlowStep`. Run the startup flow after the generated menu signals completion, and make the profile/first-run panels top-most modal overlays that block menu input. Offer all five shipped languages in first-run setup.
- **Affected Files:** `Assets/Scripts/Bootstrap/GameBootstrap.cs` (`Start`, `ShowMenuAfterSplash`, `RunStartupFlow`), `Assets/Scripts/Bootstrap/StartupFlowController.cs`, `Assets/Scripts/UI/MainMenuController.cs`, `Assets/Scripts/UI/FirstRunSetupModalController.cs`
- **Required Tests:** PlayMode startup test with an injected empty persistent-data directory: assert the ProfileSelect panel is active and blocks the menu, then FirstRunSetup, then the basics prompt, then the menu. Repeat with the Splash scene path.
- **Evidence:** E01 ([menu](evidence/screens/E01_fresh_install_main_menu.jpg)), E01b ([setup on 2nd launch](evidence/screens/E01b_first_run_setup_on_second_launch.jpg)).

## GAP-007
**Endless Zen / Spirit Trials draw inert modifiers and often fail to generate**

- **Requirement:** REQ-MODE-100/101/102, ZENMODE-SESSION-001, ZENMODE-DEPTH-004, TRIAL-TIER-002, TRIAL-SESSION-001
- **Status:** FIXED (2026-10-05, see [fix log](#fix-log-2026-10-05)); was BROKEN
- **Severity:** HIGH · **Priority:** P1 (both modes are still locked for new profiles)
- **Problem:**
  - Both modes draw modifiers uniformly from the **entire** `BossModifierId` enum (~110 values). That includes 64 "future library" IDs (XSudoku, Sandwich, NabnerLine, …) with no rule or geometry, debuffs (94–111) and pressure mechanics (103–106). Endless Zen: 27 of 48 draws inert. Spirit Trials: 16 non-functional draws, which also break the uniqueness gate.
  - Generation failed for 10 of 32 Endless Zen levels (3 of 8 sessions failed at depth 0) and 7 of 20 Spirit Trials sessions.
- **Expected:** Only implemented player modifiers (IDs 0–29, excluding disabled ones), and reliable generation.
- **Actual:** D22/D23.
- **Root Cause:** `EndlessZenService.BuildNextLevel` and `SpiritTrialsService.BuildTrialLevel` iterate `Enum.GetValues(typeof(BossModifierId))`. `BossService.IsSupportedForGeneratedRun` always returns `true`, so `SanitizeGeneratedModifierList` filters nothing. Both modes start on the synchronous 4 s path.
- **Suggested Fix:** Draw from `BossService.BuildEligiblePool(…, 9)` (or `IsImplementedModifier` plus an explicit allow-list). Implement `IsSupportedForGeneratedRun` for real (exclude extended, debuff and pressure IDs unless the mode intends them). Use the async generation path with fallback (GAP-003). Skip uniqueness when no constraining modifier is active.
- **Affected Files:** `Assets/Scripts/Run/EndlessZenService.cs`, `Assets/Scripts/Run/SpiritTrialsService.cs`, `Assets/Scripts/Boss/BossService.cs`, `Assets/Scripts/Run/RunDirector.cs` (`StartRun` for both modes)
- **Required Tests:** Promote D22/D23: all drawn modifiers have a rule; 100 % generation across seeds × depths × tiers.
- **Evidence:** D22, D23 ([probe3](evidence/diagnostics_batch3_probe.txt)).

## GAP-008
**Harmony difficulty mostly not wired; boss pick count wrong at higher Harmony**

- **Requirement:** HARMONY-BOSS-001/002, HARMONY-ECON-001, HARMONY-SURV-002/003, HARMONY-ITEM-001, HARMONY-FLAG-001..005, REQ-HARM-100/101
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was BROKEN / MISSING
- **Severity:** HIGH · **Priority:** P1
- **Problem:** `HarmonyDifficultySystem` (status "Implemented") defines 11 tiers. Code builds a `HarmonyConfig`, but most fields are never read:
  - `GridSizeWeights`, `StarWeights`: never read; H0 and H10 produce identical star histograms (D13).
  - `BossOptionsBonus`, `NothingChanceBonus`, `HintMaskingEnabled`, `MistakeDrainsPencil`, `RestHealHalved`, `CursedBonusGoldDisabled`: never read.
  - `RunState.MistakeHpCost`, `GlobalGoldMultiplier`, `ComboDecayEnabled`: copied into the run state, never read.
  - At H7/H10 the boss gate shows `floor+2+bonus` options but requires only `floor+1` picks. The intended "exactly one exclusion" is broken (D04).
  - H3–H5 never get their floor bonus (base 0 in the table), and the bonus uses floors 3–5 instead of 2–4.

  Only HP/pencil/slot/reroll penalties, shop surcharge, positive-effect probability, the T1-only starting relic and the (victory-only) XP multiplier take effect.
- **Expected:** Spec §4.1–4.7 tables apply.
- **Actual:** Static analysis plus D04/D13.
- **Root Cause:** Config fields were added without consumers. `BossGateViewController` calls `RunDirector.GetBossModifierCounts`, which omits the bonus, while `RollBossChoices` adds `GetBonusFloorModifiers` (the floor bonus) instead of `BossOptionsBonus`. `BalanceBudgetContractTests.BossBudgets_KeepOneModifierExclusion…` passes because it tests `BossService` directly, not the UI integration.
- **Suggested Fix:** Route every `HarmonyConfig` field to its consumer:
  - `ApplyMistakePenalty` base damage = `State.MistakeHpCost`.
  - Puzzle gold × `GlobalGoldMultiplier`.
  - Combo decay on mistake.
  - `NothingChanceBonus` in `BuildItemRewardSlots`.
  - Rest heal halving; pencil drain on mistake; cursed bonus off at H9+.
  - Weighted star and size selection in `BuildLevelConfig`.
  - Pass `HarmonyConfig.BossOptionsBonus` to `GetBossModifierCounts` in both `RollBossChoices` and the gate UI.

  Align the H3–H5 floor bonus and H10 grid weights with the spec, or update the spec. Decide on the XP multiplier on defeat (SPEC-CONF-024).
- **Affected Files:** `Assets/Scripts/Run/HarmonyDifficultyService.cs`, `Assets/Scripts/Run/RunDirector.cs`, `Assets/Scripts/Boss/BossService.cs`, `Assets/Scripts/UI/BossGateViewController.cs`, `Assets/Scripts/Run/RunArchetypeService.cs`, `Assets/Scripts/Save/ProfileService.cs`
- **Required Tests:** One H0-vs-H10 effect test per field (damage, gold, nothing rate, rest, star/size histogram). A gate integration test: shown − required picks == 1 for H0..H10 × floors 1..5 (promote D04/D13).
- **Evidence:** D04, D13 ([probe1](evidence/diagnostics_batch1_probe.txt)); static analysis (grep for consumers).

## GAP-009
**Tile XP entries lack size/stars/boss → wrong end-of-run breakdown, completion stuck**

- **Requirement:** XP-TILE-001, XP-LOG-001, META-COMPLETE-001, REQ-XP-100
- **Status:** FIXED (tile metadata 2026-10-05; completion and Reed Pledge 1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110))
- **Severity:** MEDIUM · **Priority:** P2
- **Problem:** `XpService.CalculateTile` computes the right XP but never sets `TileXpEntry.BoardSize`, `Stars` or `IsBoss`. The end-of-run breakdown (`EndScreenPresenter`) therefore labels every tile "0x0 0★" and never as Boss. `ProfileService` records completion key `"0x0_s0"`, so the "all size × star combos" 25 % can never progress. `CompletionService.RecordModifierCleared` has no caller, so the "all modifiers" 25 % never progresses either. Maximum reachable completion is 50 %.
- **Expected:** Per-tile breakdown with real size, stars and boss flag; completion tracking advances.
- **Actual:** D05/D08/D09; baseline `RunFlowAcceptanceTests` GardenRun (expected 6, was 0) and BossFlow (`IsBoss` false) fail.
- **Root Cause:** Missing field assignment in `XpService.CalculateTile` (only the no-progression branch in `CompleteLevelAndGrantRewards` sets size and stars), plus a missing call site for modifier completion.
- **Suggested Fix:** Pass size, stars and boss into the entry inside `CalculateTile`, or assign them in `CompleteLevelAndGrantRewards` right after the call. On boss defeat call `RecordModifierCleared` for each active modifier. Existing profiles cannot recover lost completion data, so state that in the patch notes.
- **Affected Files:** `Assets/Scripts/Economy/XpService.cs`, `Assets/Scripts/Run/RunDirector.cs`, `Assets/Scripts/Save/ProfileService.cs`, `Assets/Scripts/Meta/CompletionService.cs`
- **Required Tests:** Make both `RunFlowAcceptanceTests` pass. Add a completion test (one solved 6×6 1★ → key `6x6_s1`; boss with Thermo → Thermo cleared) and an `EndScreenPresenter` label test.
- **Evidence:** D05, D08, D09; baseline results ([summary](evidence/baseline_test_results.txt)).

## GAP-010
**Test gate does not run; 50 failing tests; tests write real saves; no requirement→test links**

- **Requirement:** REQ-TEST-100, REQ-TEST-101, REQ-SAVE-102 (CLAUDE.md: traceability requirement → implementation → test)
- **Status:** FIXED (2026-10-05, see [fix log](#fix-log-2026-10-05)); was BROKEN
- **Severity:** HIGH · **Priority:** P1 (it hides every other regression)
- **Problem:**
  - `tools/run-unity-tests.ps1` passes `-quit` together with `-runTests`, so Unity exits with code 0 **before running any test** and no XML is written. The documented authoritative gate never runs.
  - Run directly, the suite has **45 of 307 EditMode and 5 of 8 PlayMode tests failing**:
    - 8 because `docs/release/*.md` files are missing.
    - About 20 brittle source-text "contract" tests drifted from the code or docs.
    - 4 generated-menu NREs.
    - 5 PlayMode UI failures (in-run options panel won't open, `BtnZenRecords` not interactable, `MuteAll` missing, missing German keys).
    - Real gameplay failures: `RunFlowAcceptanceTests` ×2, `GinkgoLeaf`, `ResumeService_FlushesPendingAutosaveBeforeReading`, `SaveWriteQueueTests`.
    - Controller size budgets exceeded (RunDirector 3911/3350 lines).
  - EditMode and PlayMode tests write the player's real `%LOCALAPPDATA%Low/Kratossos/sudoku_rouge_like/save_profile_2.json`; one teardown failed with an IOException on it.
  - Only **1 of 436** spec requirement IDs is referenced from any test.
- **Expected:** A green, runnable gate whose tests reference requirement IDs and never touch player data.
- **Root Cause:** Runner argument bug; no CI enforcement (`RUN_UNITY_BATCHMODE` opt-in); `SaveFileService` has no injectable root directory; tests grep source text instead of behaviour.
- **Suggested Fix:**
  1. Remove `-quit` from `Invoke-UnityTests`, then parse and print the XML totals.
  2. Add a `SaveFileService(int slot, SaveWriteQueue q, string rootDirectory)` overload; tests use a temp directory.
  3. Triage the failing tests: restore or relocate `docs/release/*`, replace source-string assertions with behavioural or registry assertions, and fix the gameplay failures through GAP-001/002/009/035.
  4. Put requirement IDs in test names or `[Category("REQ:…")]` and generate the traceability matrix from them.
  5. Run the generation matrix (GAP-003) nightly.

  The review diagnostics (`ReviewDiagnostics*Tests.cs`) are marked `[Explicit]`, so they only run when selected.
- **Affected Files:** `tools/run-unity-tests.ps1`, `Assets/Scripts/Save/SaveFileService.cs`, `Assets/Scripts/Tests/**`, `.github/workflows/static-validation.yml`, `docs/smoke-test-readiness.md`
- **Required Tests:** Meta-test: run the script and assert XML exists with total > 0. All suites green on a clean machine without LocalLow writes.
- **Evidence:** [baseline_test_results.txt](evidence/baseline_test_results.txt); first runner attempt produced no XML (`TestResults/review-2026-10-01/EditMode-unity.log`).

## GAP-011
**"Wrong placement" means rule conflict, not wrong digit; puzzles are often non-unique**

- **Requirement:** REQ-SUD-103, REQ-SUD-102, GEN-VALID-001 (vs BossMechanicsSystem §Validation Path, GDS §5.1)
- **Status:** FIXED (1.1, SPEC-CONF-003 decided, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was CONTRADICTED
- **Severity:** MEDIUM · **Priority:** P2
- **Problem:** `PlaceNumber` accepts any digit that doesn't conflict with digits already placed. 30 of 30 non-solution digits that fit the current rules were accepted as "Correct": combo +1, no HP loss, autosave (D19, and confirmed in the build, E07). Mistakes only happen on conflicts. Puzzles frequently have multiple solutions (even 9×9 1★ standard; 9×9 6★ has only 8 givens and can never be unique), so a non-solution digit may still lead to a dead end later, with no feedback. The fog path, by contrast, compares against the solution.
- **Expected:** One documented rule. Either (a) solution-based correctness with guaranteed-unique puzzles, or (b) rule-based correctness (current), with specs updated and dead-end feedback.
- **Root Cause:** Spec conflict (SPEC-CONF-003). Generator uniqueness is only enforced for competitive modes.
- **Suggested Fix:** Decide (a) or (b). For (a), enforce uniqueness (cheap for ≤ 4★; at 5–6★ require modifiers or raise givens) and compare with `Solution`. For (b), update GDS §5.1 and BossMechanics §Validation, make the fog path consistent, and add an optional "no candidates left" warning. Either way, move the HP penalty into `RunDirector` (GAP-035).
- **Affected Files:** `Assets/Scripts/Run/RunDirector.cs` (`PlaceNumber`, fog evaluation), `Assets/Scripts/Sudoku/SudokuGenerator.cs`, `Assets/Scripts/Sudoku/SudokuBoard.cs` (`IsComplete`), specs
- **Required Tests:** Placement-semantics tests for the chosen rule (D19 promoted), uniqueness rate per star (D02 promoted), fog parity test.
- **Evidence:** D02, D15, D19, E07 ([screen](evidence/screens/E07_non_solution_digit_accepted.jpg)), E08.

## GAP-012
**Floor-modifier counts, boss choice counts and modifier min sizes differ from spec**

- **Requirement:** BOSS-FLOOR-001, REQ-BOSS-100, REQ-BOSS-106
- **Status:** FIXED (1.1: specs aligned to the code tables, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was CONTRADICTED
- **Severity:** MEDIUM · **Priority:** P2
- **Problem:**
  - Floor modifiers are `{0,1,1,1,2}` for floors 1–5 (spec `{0,1,2,3,4}`).
  - The boss gate offers 1-of-2 on Floor 1 (spec 2-of-3; the table's "min 3 / min 2" isn't applied although a code comment claims it).
  - About 12 modifier minimum board sizes differ from BossMechanics v2.2 (e.g. Nonconsecutive 6 vs any, AntiBishop 9 vs 6).
  - ModularRegions is "disabled by design decision", which no spec records.
  - Undocumented blocked pairs exist.
- **Expected/Actual:** Spec tables vs D04/E17.
- **Root Cause:** Undocumented rebalance in `BossService` (`BaseFloorModifierCounts`, `GetBossModifierCounts`, `ModifierData`).
- **Suggested Fix:** Decide the authoritative table (SPEC-CONF-001/002/013). Either update `BossService` or bump BossMechanics/GDS/PathSystem with the current values and the reasons (generation budget).
- **Affected Files:** `Assets/Scripts/Boss/BossService.cs`, `docs/BossMechanicsSystem.md`, `docs/GameDesignSpec.md`, `docs/PathSystem_GardenOverview.md`
- **Required Tests:** Table-driven test of counts per floor and Harmony level, and min sizes per modifier (promote D04).
- **Evidence:** D04, E17.

## GAP-013
**Board sizes outside floor ranges; map preview differs from generated puzzle**

- **Requirement:** REQ-PATH-101, REQ-PATH-102, REQ-BOSS-105
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was CONTRADICTED / BROKEN
- **Severity:** MEDIUM · **Priority:** P2
- **Problem:**
  - Floor 1 tiles are 7×7 (spec 5–6) and the Floor 1 boss is always 8×8 or larger (spec 5–6).
  - On Floor 2 the map tile showed **7×7 ★★★** but the puzzle generated **8×8 ★★★**: the board-shaping floor modifier bumps the size *after* the preview.
- **Root Cause:** `RunMapController.TryAdvanceToBossNodeAsync` forces `BoardSize = Max(BoardSize, 8)`. `BuildLevelConfig` (board-shaping → 7 becomes 6/8), `EnsureBoardSizeSupportsActiveModifiers` and the variance/adaptation services alter the size after the fixed node config used for the preview.
- **Suggested Fix:** Compute the final size once (including all bumps) in the config used for both preview and generation (`GetFixedLevelConfig`). Remove the boss ≥ 8 clamp or document it in the spec. Keep sizes within floor ranges unless the spec lists exceptions.
- **Affected Files:** `Assets/Scripts/UI/RunMapController.cs`, `Assets/Scripts/Run/RunDirector.cs`, `Assets/Scripts/Run/RunVarianceService.cs`, `Assets/Scripts/Run/MidRunAdaptationService.cs`
- **Required Tests:** For every node of seeded runs, preview config == generated board size and stars, and sizes within the floor range.
- **Evidence:** E05, E17, E20 ([screen](evidence/screens/E20_floor2_tile7x7_puzzle8x8.jpg)), D18.

## GAP-014
**Pressure mechanics roll one floor late**

- **Requirement:** PRESSURE-ROLL-003 (also PRESSURE-ROLL-005)
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was BROKEN
- **Severity:** MEDIUM · **Priority:** P2
- **Problem:** Measured appearance on elite/pre-boss/boss nodes is F1 0 %, F2 0 %, F3 12 %, F4 30 %, F5 55 %. Spec: F2 12 %, F3 30 %, F4 55 %, F5 80 %. PressureWave starts on Floor 4 instead of Floor 3.
- **Root Cause:** `RunDirector.RollPressureMechanic` (l.1805–1825) compares the **0-based** `State.CurrentFloor` against 1-based spec floors (`if (floor < 2) return; floor switch {2=>0.12, 3=>0.30, 4=>0.55, _=>0.80}`; `if (floor >= 3)` for the wave).
- **Suggested Fix:** Use `floor < 1` and `floor switch {1=>0.12, 2=>0.30, 3=>0.55, _=>0.80}`; wave on `floor >= 2`. Check the intensity mapping in `InitializePressureMechanics` for the same off-by-one.
- **Affected Files:** `Assets/Scripts/Run/RunDirector.cs`
- **Required Tests:** Promote D16 with tolerance bands per floor.
- **Evidence:** D16 ([probe1](evidence/diagnostics_batch1_probe.txt)).

## GAP-015
**Item-reward reroll is missing**

- **Requirement:** REQ-ITEM-100 (GDS §8 "Optional reroll of eligible slots"; reroll cost 20 + 20n)
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was MISSING
- **Severity:** MEDIUM · **Priority:** P2
- **Problem:** The reward screen has no reroll. `RewardViewController.RerollRewardSlots` exists but has no caller, and `GoldTable.RerollCost` is unused. Reroll tokens are only spendable in the shop. GoldEconomy v2.0 dropped the rule without a decision record.
- **Suggested Fix:** Add a reroll button (tokens first, then gold `GoldTable.RerollCost(State.RerollCount)`) that re-rolls only unpicked, non-Nothing slots through `ItemService`, with lock rules per GDS §8. Or remove the rule from GDS if intentionally dropped.
- **Affected Files:** `Assets/Scripts/UI/RewardViewController.cs`, `Assets/Scripts/Run/RunDirector.cs`, `Assets/Scripts/Economy/GoldTable.cs`
- **Required Tests:** Reroll cost progression, locked-slot preservation, determinism with seed.
- **Evidence:** E14 ([screen](evidence/screens/E14_reward_screen.jpg)); static (no callers).

## GAP-016
**Buying pencil during a level is missing**

- **Requirement:** REQ-ECON-100 (GDS §5.2 "Can be bought with Gold during level", §5.3 cost 20 + 20n)
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was MISSING
- **Severity:** MEDIUM · **Priority:** P2
- **Problem:** No buy-pencil action exists. `GoldTable.PencilBuyCost` is unused; `PencilPurchaseCount` is reused by the emergency HP heal. `ClassPassiveService.GardenMonkPencilBuyIsHPTrade` (Garden Monk "cannot buy pencil mid-level") has nothing to restrict.
- **Suggested Fix:** Implement the HUD action (with the class restriction), or update GDS and the Garden Monk passive text. Rename the shared counter.
- **Affected Files:** `Assets/Scripts/UI/HudViewController.cs`, `Assets/Scripts/Run/RunDirector.cs`, `Assets/Scripts/Economy/GoldTable.cs`, `Assets/Scripts/Classes/ClassPassiveService.cs`
- **Required Tests:** Cost progression, Garden Monk restriction, insufficient gold.
- **Evidence:** Static analysis; E06 (no control in puzzle HUD).

## GAP-017
**Ascension does not exist; per-class prestige unreachable**

- **Requirement:** META-ASCEND-001..004, META-PRESTIGE-001..005, SEASON-UNLOCK-001
- **Status:** PARTIALLY FIXED (1.1: prestige reachable from the Meta Progression panel; Ascension deferred to FUTURE-009, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was MISSING / PARTIALLY_IMPLEMENTED
- **Severity:** MEDIUM · **Priority:** P2
- **Problem:**
  - `Assets/Scripts/Meta/AscensionService.cs` does not exist, yet `REQUIREMENT_MAP.md` marks META-ASCEND-002..004 ✅. `SeasonalChallengeUnlocked` exists only in a comment. `MaxStarCap` is never read.
  - `ClassGardenProgressionService.TryPrestige` has no caller and no UI, so level-40 classes can never prestige.
- **Suggested Fix:** Implement the ascension service and UI (or mark META-ASCEND deferred and correct the map). Add a prestige button in Meta Progression when eligible, with confirmation.
- **Affected Files:** `Assets/Scripts/Meta/*`, `Assets/Scripts/UI/MetaProgressionPanelController.cs`, `docs/REQUIREMENT_MAP.md`, `docs/MetaProgressionSystem.md`
- **Required Tests:** Prestige eligibility and reset; ascension state transitions; map/status consistency check.
- **Evidence:** Static (file absent, no callers).

## GAP-018
**Save/resume re-rolls boss choices and rewards (determinism lost)**

- **Requirement:** REQ-RUN-103 (SudokuGenerationPipeline §Seeding: "Save/resume … produces the same board")
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was BROKEN
- **Severity:** MEDIUM · **Priority:** P2
- **Problem:** Boss choices offered before Save & Quit differ after Resume (D12: `NonconsecDiagonal, Antiking, RenbanLines, ParityLines` → `EvenOdd, KillerCages, DistanceGe2, ConsecutiveLine`). Item, shop, relic, event and curse rolls also restart their sequences. This allows save-scumming and makes bug reports non-reproducible. Fresh runs *are* deterministic (D01).
- **Root Cause:** `RunDirector.InitServices(seed)` creates sequential `System.Random` instances per service. Resume calls `StartRun` → `InitServices` again, so every stream restarts. Rolled options (`BossModifierChoices`, `RolledItemSlots`, `CurrentShopOffers`, `RolledRelicChoices`) are not persisted, and `BuildItemRewardSlots` re-rolls on every call.
- **Suggested Fix:** Derive one RNG per decision from `(runSeed, floor, nodeIndex, purpose)`, and persist rolled options in `RunState` until consumed.
- **Affected Files:** `Assets/Scripts/Run/RunDirector.cs`, `Assets/Scripts/Boss/BossService.cs`, `Assets/Scripts/Items/ItemService.cs`, `Assets/Scripts/Economy/ShopService.cs`, `Assets/Scripts/Economy/RelicService.cs`, `Assets/Scripts/Core/RuntimeModels.cs`
- **Required Tests:** Promote D12: options before save == after resume, for boss gate, reward, shop and relic choice.
- **Evidence:** D01, D12. Fix verification F04: with Save & Quit + Resume before each puzzle, the first item reward after every resume offered the same two items (Garden Lantern, Koi Reflection) on four different puzzles, because the item stream restarts at the run seed ([screen](evidence/screens/F04_same_reward_after_resume.jpg)).

## GAP-019
**Localization: German mojibake, untranslated UI, language not applied live**

- **Requirement:** REQ-UI-101, REQ-UI-102
- **Status:** PARTIALLY FIXED (menu rebuild 2026-10-05; German catalog, encoding guard and UI strings 1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)). French, Spanish and Portuguese completed 2026-10-14 (420 keys each, LOC-PARITY-001..003, `LocalizationParityTests`); native review pending (PRG-LOCALIZATION)
- **Severity:** MEDIUM · **Priority:** P2
- **Problem:**
  - `Assets/Resources/Localization/de.json` contains **60 double-encoded strings** visible in the UI ("ZurÃ¼ck", "LautstÃ¤rke", "Klasse wÃ¤hlen", "UnregelmÃ¤ÃŸige RÃ¤tsel erlauben", "RÃ¤tsel wird noch geladen").
  - Many strings stay English in German:
    - in-run: "Floor 1 Depth 1", help text, "Mode: SOLVE", "Save & Quit", the top HUD bar, "Boss Defeated!";
    - class select: class passives, unlock lines, "Start Run";
    - the resume label ("Number Freak").
  - The language chosen in first-run setup isn't applied to the already-built main menu until restart.
  - First-run setup only offers English/Deutsch, although French, Spanish and Portuguese ship.
  - "Wähle 1 Modifikatoren" lacks plural handling.
  - `LocalizationCatalogQualityTests` pass because they don't check encoding.
- **Suggested Fix:**
  1. Repair `de.json` (re-decode the affected values from cp1252 to UTF-8) and add a test rejecting `Ã`/`â€`/`Â` sequences in all catalogs.
  2. Route the listed strings through `LocalizationService`.
  3. Rebuild the menu after the first-run language choice and list all catalogs.
  4. Add plural-aware format keys.
- **Affected Files:** `Assets/Resources/Localization/*.json`, `Assets/Scripts/UI/*` (in-run builders, `EndScreenViewController`, `MainMenuClassInfoPresenter`), `Assets/Scripts/UI/FirstRunSetupModalController.cs`, `Assets/Scripts/Tests/EditMode/LocalizationCatalogQualityTests.cs`
- **Required Tests:** Encoding guard, PlayMode German traversal with no missing keys (currently failing), in-run screens traversal per language.
- **Evidence:** E11 ([menu](evidence/screens/E11_menu_not_relocalized.jpg), [options](evidence/screens/E11b_options_mojibake.jpg)), E13b, E17, E18.

## GAP-020
**No seed entry / View Seed / Abandon Run / View Modifiers**

- **Requirement:** REQ-RUN-100, REQ-RUN-101 (GDS §14 Start Game Flow and Pause Menu; SudokuGenerationPipeline §Seeding "random or player-entered")
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was MISSING / PARTIALLY_IMPLEMENTED
- **Severity:** MEDIUM · **Priority:** P2
- **Problem:** There is no seed field at run start and no way to view the seed, so seed sharing and bug reproduction aren't possible for players. The in-run menu has only Options and Save & Quit: no Abandon Run (players stuck by GAP-001/004 cannot give up a run cleanly) and no View Modifiers.
- **Suggested Fix:** Add an optional seed input on the class-confirm modal, show the seed in in-run options, and add Abandon Run (confirmation, records the run result with XP) plus a View Modifiers panel.
- **Affected Files:** `Assets/Scripts/UI/MainMenuBlueprintBuilder.cs`, `Assets/Scripts/UI/MainMenuController.cs`, `Assets/Scripts/UI/InRunUiBlueprintBuilder.cs`, `Assets/Scripts/UI/InRunController.cs`, `Assets/Scripts/Bootstrap/GameBootstrap.cs`
- **Required Tests:** The entered seed reproduces the D01 trace; Abandon Run commits XP and clears the active run.
- **Evidence:** E03, E06, s31.

## GAP-021
**Accepted boss relic was not added**

- **Requirement:** REQ-ITEM-104
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was BROKEN
- **Severity:** MEDIUM · **Priority:** P2
- **Problem:** After accepting "Karmesinfächer" (Crimson Fan) in the boss relic choice, Floor 2 started, but the HUD showed "Relics 1" and the relic panel showed only the starting comb.
- **Root Cause (hypothesis):** After resume the relic options were re-rolled (GAP-018). `AcceptRelicChoice(index)` resolves through `RolledRelicChoices`, which may be stale or null on this path.
- **Suggested Fix:** Pass the chosen `RelicInstance` to `AcceptRelic` directly; persist rolled choices (GAP-018); fix together with GAP-002.
- **Affected Files:** `Assets/Scripts/UI/RewardViewController.cs`, `Assets/Scripts/Run/RunDirector.cs` (`RollRelicChoices`, `AcceptRelicChoice`)
- **Required Tests:** Boss → relic choice → accept → `HeldRelics` +1, with and without an intermediate save/resume.
- **Evidence:** E19 ([screen](evidence/screens/E19_floor2_relics_1.jpg)). Fix verification F03 (after the GAP-002 fix, no resume between boss and relic choice): Copper Tortoise accepted, HUD "Relics 2", both relics in the save ([screen](evidence/screens/F03d_floor2_relics_2.jpg)). The resume path between boss and relic choice was not re-tested.

## GAP-022
**Path map: hidden "?" tiles, many cross-links, no player marker**

- **Requirement:** MAP-ROUTE-001/002, MAP-CROSS-001, REQ-PATH-100
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was CONTRADICTED / MISSING
- **Severity:** LOW · **Priority:** P3
- **Problem:**
  - Tiles beyond the first are hidden as "?" (spec: "fully visible from floor entry").
  - The code inserts `floor+1` bridges (2–8 cross-link nodes) and turns them into a dedicated `CrossLink` node type (spec: exactly one cross-connection; tiles keep their type).
  - No player marker exists (`PlayerIconController.cs` was never created).

  Graph connectivity itself is correct (D06).
- **Suggested Fix:** Decide whether fog-of-path and multiple bridges are the new design; if so, update PathSystem (SPEC-CONF-010/030). Implement the player marker.
- **Affected Files:** `Assets/Scripts/Run/RunGraphService.cs`, `Assets/Scripts/UI/InRunController.cs` (path rendering), `docs/PathSystem_GardenOverview.md`
- **Required Tests:** Graph invariants (D06 promoted), marker position equals `CurrentNodeIndex`.
- **Evidence:** D06, E05 ([screen](evidence/screens/E05_garden_floor1.jpg)).

## GAP-023
**Class stats/passives/levels: 3-way spec conflict; no "base (+bonus)" display**

- **Requirement:** REQ-CLASS-100..103, META-UNLOCK-004
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was CONTRADICTED / MISSING
- **Severity:** MEDIUM · **Priority:** P2
- **Problem:**
  - GDS v1.5, ClassXpProgressionSystem v1.2 and `ClassCatalog` disagree on base stats (Reed Duelist, Quiet Cartographer), passives (Shrine Archivist, Koi Gambler, Lantern Seer, Number Freak, Reed Duelist) and one unlock condition (Quiet Cartographer).
  - The level schedule differs: Number Freak at L10 starts with HP 15 / Pencil 12 vs spec 14 / 13.
  - Class select shows only base values ("HP:10 Pencil:10 Slots:2") although level bonuses apply at run start. The review brief asks for "HP 10 (+5)".
- **Suggested Fix:** Decide the authoritative roster (SPEC-CONF-008) and align code or specs. Compute effective stats via `RunArchetypeService` for the class level and render `base (+bonus)` in `MainMenuClassInfoPresenter`.
- **Affected Files:** `Assets/Scripts/Classes/ClassCatalog.cs`, `Assets/Scripts/Classes/ClassPassiveService.cs`, `Assets/Scripts/Run/RunArchetypeService.cs`, `Assets/Scripts/UI/MainMenuClassInfoPresenter.cs`, specs
- **Required Tests:** Roster table test, level schedule test per class (D07 promoted), presenter output test.
- **Evidence:** D07, E03 ([screen](evidence/screens/E03_class_select.jpg)).

## GAP-024
**Rest node values differ**

- **Requirement:** REQ-RUN-104 (GDS §9b: Heal +2 HP, +3 Pencil, +1 Reroll)
- **Status:** FIXED (1.1: spec aligned to the code values, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was CONTRADICTED · **Severity:** LOW · **Priority:** P3
- **Problem / Root Cause:** `RunDirector.GetRestHealAmount() = max(1, MaxHP/3)` (+3 at 10 HP), and `AcceptRestPencilBoost` gives +4.
- **Suggested Fix:** Align the code or GDS (decision).
- **Affected Files:** `Assets/Scripts/Run/RunDirector.cs`
- **Required Tests:** Rest option values.
- **Evidence:** D25.

## GAP-025
**Boss gate has no intensity label; modifier legend panel missing**

- **Requirement:** REQ-BOSS-103, REQ-BOSS-104
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was MISSING · **Severity:** LOW · **Priority:** P3
- **Problem:** The gate title is "Boss-Tor – Wähle 1 Modifikatoren (0/1)" with no intensity label. The collapsible legend with tap-to-isolate (BossMechanics §Modifier Legend Panel) isn't present; only a description box is.
- **Suggested Fix:** Append the localized intensity to the title; implement the legend (overlay objects grouped per modifier under a `CanvasGroup`).
- **Affected Files:** `Assets/Scripts/UI/BossGateViewController.cs`, `Assets/Scripts/UI/BoardViewController.cs`
- **Required Tests:** PlayMode: title contains intensity; legend lists active modifiers; isolate dims others.
- **Evidence:** E17.

## GAP-026
**Boss debuffs have no delivery path; 4 debuffs not implemented**

- **Requirement:** REQ-BOSS-108, DEBUFF-DEF-006/009/010/016
- **Status:** FIXED (1.1: delivery implemented; PencilPoison, FogPulse, NumberBlind, DigitShift descoped to FUTURE-010, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was MISSING · **Severity:** LOW · **Priority:** P3
- **Problem:** 14 debuffs (IDs 94–111) are implemented as mistake hooks, but no Garden Run path ever adds them (IDs ≥ 94 are excluded from all rolls), and the spec never defines how they are delivered. PencilPoison, FogPulse, NumberBlind and DigitShift are missing. Localized descriptions exist only for the first five.
- **Suggested Fix:** Specify delivery (e.g. one debuff per boss from Floor 3, or via curses), wire it in `BuildLevelConfig(isBoss)`, and implement or descope the four.
- **Affected Files:** `Assets/Scripts/Run/RunDirector.cs`, `Assets/Scripts/Boss/BossService.cs`, `docs/BossDebuffEffects.md`
- **Required Tests:** Per-debuff hook tests (board/HP/pencil effects after `ApplyMistakePenalty(row, col)`).
- **Evidence:** D04/D05/D18 (no debuff ever rolled); static.

## GAP-027
**"Collect 50,000 gold" counts gold held at run end**

- **Requirement:** REQ-ECON-101, META-UNLOCK-001 (Lantern Seer)
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was CONTRADICTED · **Severity:** LOW · **Priority:** P3
- **Problem / Root Cause:** `RunMapController` builds `RunResult.GoldEarned = State.CurrentGold`, so gold spent in shops never counts toward the cumulative unlock (or `TotalGoldEarned`). `GoldCollectedThisRun` exists but is only incremented when `DailyGoals != null`.
- **Suggested Fix:** Track collected gold unconditionally and use it for `GoldEarned`; clarify the spec wording.
- **Affected Files:** `Assets/Scripts/UI/RunMapController.cs`, `Assets/Scripts/Run/RunDirector.cs`
- **Required Tests:** Earn 100, spend 60 → unlock progress +100.
- **Evidence:** Static.

## GAP-028
**Daily/Monthly not on main menu; daily streak ignores skipped days**

- **Requirement:** DAILY-GOAL-003/004, SEASON-UI-001, REQ-UI-106
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was CONTRADICTED / BROKEN · **Severity:** LOW · **Priority:** P3
- **Problem:**
  - Specs require a Daily Walk widget and a prominent Monthly Walk panel with countdown on the main menu. Both are buttons inside Game Modes ("Tagesgang", "Monatsgang").
  - `DailyGoalService.RefreshIfNewDay` only checks whether the *last recorded* day was complete. A day without launching the game never resets the streak (spec: "Any goal not completed by midnight → streak resets to 0").
- **Suggested Fix:** Place the widgets per spec or update the spec. In `RefreshIfNewDay`, reset the streak when `LastGoalDate < today − 1`.
- **Affected Files:** `Assets/Scripts/Run/DailyGoalService.cs`, `Assets/Scripts/UI/MainMenuBlueprintBuilder*.cs`
- **Required Tests:** Streak with an injected date sequence (complete D1, skip D2, open D3 → 0).
- **Evidence:** E02/E12; static.

## GAP-029
**UI readability and polish issues**

- **Requirement:** REQ-UI-103, META-UNLOCK-004, GEN-REGION-003 (toggle default)
- **Status:** PARTIALLY FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110); the in-game Options restyle needs a visual design pass, FUTURE-015); was BROKEN · **Severity:** COSMETIC · **Priority:** P4
- **Problem (observed in the build):**
  - Locked class names are nearly invisible (dark on dark), and unlock conditions appear only as hover tooltips that overlap cards.
  - Toggles show no on/off state (class select, first-run accessibility, main Options).
  - "Allow Irregular Puzzles" defaults to off (spec: on).
  - A class-select tooltip leaked onto the main menu.
  - Harmony description text is clipped behind an icon.
  - Main menu buttons have inconsistent heights.
  - Modal button labels are tiny.
  - The in-game options screen uses a different visual style from the main Options.
  - Header mixes "7x7 1*" and "7×7 ★".
  - The top-right gold counter only updates after the reward screen closes.
  - Player-placed digits are indistinguishable from givens.
  - Monthly Walk HUD: the class portrait is a white square and the status line says "Select a path and start solving." (fix verification F02, [screen](evidence/screens/F02a_monthly_walk_2026-10.jpg)).
  - Re-entering the same conflicting digit returns `PlaceResult.Correct` (D15), so the UI takes the success path: success sound, "Placed" status, autosave (`InRunController.cs:3941-3949`; seen in code, not heard).
- **Suggested Fix:** Contrast pass with `GamePalette` (≥ 4.5:1), visible toggle checkmarks, tooltip lifecycle tied to panel hide, a shared button factory for all panels, given/placed digit styles.
- **Affected Files:** `Assets/Scripts/UI/MainMenuBlueprintBuilder*.cs`, `MainMenuClassInfoPresenter.cs`, `InRunUiBlueprintBuilder.cs`, `BoardViewController.cs`, `GamePalette.cs`
- **Required Tests:** PlayMode screenshot or contrast checks; tooltip hidden after panel close.
- **Evidence:** E02–E06, E14, s20, s31.

## GAP-030
**Audio assets are not valid WAV files; audio unverifiable**

- **Requirement:** REQ-AUDIO-100/101
- **Status:** PARTIALLY FIXED (headers 2026-10-05; clip loading verified 1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)). Open: listening check; the music loops are 6.0 s mono · **Severity:** MEDIUM · **Priority:** P2
- **Problem:** All 14 music files and the SFX files under `Assets/Resources/audio/` have a `.wav` extension, but none starts with a `RIFF` header (all music files are 529,232 bytes). `AudioAssetServiceTests.ReleaseRequiredClips_HaveAuthoredAssetFilesAndMetas` and `MenuMusicController_InitializeRestoresMissingAudioSource` fail. Audio could not be judged by ear in this review, and the player log contains no audio lines.
- **Suggested Fix:** Re-export the source audio as real WAV/OGG, re-import, and confirm `AudioClip` loads (log clip length at startup). Then run a manual listening checklist: menu, each floor, puzzle, boss layer, stingers, volume sliders, mute, mute-when-unfocused.
- **Affected Files:** `Assets/Resources/audio/**`, `Assets/Scripts/UI/AudioAssetService.cs`, `MenuMusicController.cs`, `RunAudioController.cs`
- **Required Tests:** Header/format validation test (exists, failing); PlayMode clip-loaded test.
- **Evidence:** Baseline test failures; byte inspection.

## GAP-031
**Mistakes are not autosaved**

- **Requirement:** REQ-SAVE-101
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was PARTIALLY_IMPLEMENTED · **Severity:** LOW · **Priority:** P3
- **Problem / Root Cause:** `InRunController` calls `_map.SaveNow()` only after a correct placement. Closing the game right after a mistake restores the HP and relic uses from before it.
- **Suggested Fix:** Save after `ApplyMistakePenalty` as well (the write queue already batches writes).
- **Affected Files:** `Assets/Scripts/UI/InRunController.cs`
- **Required Tests:** Mistake → kill process → resume → HP reduced.
- **Evidence:** E08/E09 (saved relic uses = 1 after the absorbed mistake).

## GAP-032
**Starting a new run silently discards the active run**

- **Requirement:** (none, spec gap; see SPEC-AMB-010)
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was MISSING · **Severity:** LOW · **Priority:** P3
- **Problem:** Start Game → Start Run → Begin with an active resumable run shows no warning and overwrites the saved run.
- **Suggested Fix:** Show a confirmation ("Abandon current run?") that records the abandoned run's result.
- **Affected Files:** `Assets/Scripts/UI/MainMenuController.cs`, `Assets/Scripts/Bootstrap/GameBootstrap.cs`
- **Evidence:** E04 (s34).

## GAP-033
**Floor-1 tiles can roll 6★**

- **Requirement:** GDS §15 Balancing Guardrails ("low stars … in first runs")
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was CONTRADICTED · **Severity:** LOW · **Priority:** P3
- **Problem / Root Cause:** `RunDirector.RollStars` gives every non-boss node a 20 % chance to jump to *any* higher star level up to 6 (log: "Floor 0: variant 6★ (base 1★, 20% roll)"). A first-floor 9×9-equivalent 90 % removal is possible.
- **Suggested Fix:** Cap the bump (+1★) or per-floor maximums; tie it to the Harmony star weights (GAP-008).
- **Affected Files:** `Assets/Scripts/Run/RunDirector.cs`
- **Evidence:** Player log session 4.

## GAP-034
**Reed Pledge bonus XP booked to the previous tile**

- **Requirement:** XP-LOG-001 (item ReedPledge)
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was BROKEN · **Severity:** LOW · **Priority:** P3
- **Problem / Root Cause:** In `CompleteLevelAndGrantRewards`, `TileXpLog[Count-1].TotalXp += 25` runs **before** the current tile's entry is added, so the bonus goes to the previous tile, or is lost when the pledge is fulfilled on the run's first tile.
- **Suggested Fix:** Add the bonus to `xpEntry` before `TileXpLog.Add(xpEntry)`.
- **Affected Files:** `Assets/Scripts/Run/RunDirector.cs`
- **Required Tests:** Pledge on the first tile → that tile has +25.
- **Evidence:** Static.

## GAP-035
**Mistake HP penalty lives in the UI layer**

- **Requirement:** REQ-SUD-103 (architecture; `implement-requirement` skill: logic in services)
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was PARTIALLY_IMPLEMENTED · **Severity:** LOW · **Priority:** P3
- **Problem:** `RunDirector.PlaceNumber` never applies HP loss; `InRunController` (~l.3961) calls `ApplyMistakePenalty` after an `Invalid` result. Any non-UI caller (items, automation, tests) bypasses the penalty, which is why `GameplayEffectContractTests.GinkgoLeaf…` fails (expects HP 9, gets 10). The conflicting digit stays on the board, and re-entering the same digit returns `Correct`.
- **Suggested Fix:** Apply the penalty (with debuff dispatch) inside `PlaceNumber` and return a richer result; keep the UI responsible only for feedback.
- **Affected Files:** `Assets/Scripts/Run/RunDirector.cs`, `Assets/Scripts/UI/InRunController.cs`
- **Required Tests:** Wrong placement through `RunDirector` reduces HP; GinkgoLeaf passes.
- **Evidence:** D15, baseline failure.

## GAP-036
**`TryPurchaseShopOffer` charges gold when the item cannot be stored**

- **Requirement:** REQ-ITEM-101
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was BROKEN (API) · **Severity:** LOW · **Priority:** P4
- **Problem / Root Cause:** `AddItemToInventory` silently does nothing when the bag is full, after the gold has already been deducted.
- **Suggested Fix:** Return `false` without charging, or require a replace slot.
- **Affected Files:** `Assets/Scripts/Run/RunDirector.cs`
- **Evidence:** D25.

## GAP-037
**Codex discoveries (and in-run option changes) are overwritten by the next autosave**

- **Requirement:** META-CODEX-001, META-CODEX-002, SAVE-AUTO-001
- **Status:** FIXED (1.1, see [1.1 fix log](#fix-log-2026-10-07-release-110)); was BROKEN (found during the 2026-10-05 fix verification) · **Severity:** MEDIUM · **Priority:** P2
- **Problem:** Items and relics picked up during a run are recorded in the codex, then lost on the next correct placement. In the fixed build the save held `ItemCodex: [GardenLantern]` right after the item was claimed and `[]` after the next autosave. A boss relic (Copper Tortoise) never appeared in `DiscoveredRelics` at all. Because autosave runs after every correct placement, the codex practically never fills during normal play.
- **Root Cause:** `RunAutoSaveCoordinator` loads the whole `SaveFileEnvelope` once in `Bind()` and writes that cached copy on every save. `RewardViewController` and `ShopViewController` record discoveries through a separate `ProfileService(new SaveFileService(slot))`, which loads, modifies and saves its own copy. The coordinator never sees the change and overwrites it. The coordinator merges only `DiscoveredBossModifiers` into its cache. Same mechanism, seen in code only: `OptionsController.SaveOptions` writes `PlayerProfile.Options` the same way, so an option changed from the in-run Options panel is reverted by the next autosave.
- **Suggested Fix:** Give the save file one writer for in-run state. Either (a) `RunAutoSaveCoordinator.Save` reloads the envelope (or only the fields it does not own) before writing (simple; one ~10 KB read per placement), or (b) route codex and options writes through the coordinator's cached envelope. Option (a) also covers any future out-of-band writer.
- **Affected Files:** `Assets/Scripts/Save/RunAutoSaveCoordinator.cs`, `Assets/Scripts/Save/ProfileService.cs` (`RecordItemDiscovery`, `RecordRelicDiscovery`), `Assets/Scripts/UI/RewardViewController.cs`, `Assets/Scripts/UI/ShopViewController.cs`, `Assets/Scripts/UI/OptionsController.cs`
- **Required Tests:** EditMode: bind a coordinator, record an item and a relic discovery through `ProfileService`, change an option through the options path, autosave, reload: all three persist.
- **Evidence:** Fix verification F01/F03 (save file read after claim and after the next autosave); code reading for options.

## GAP-038
**XP log and run analytics were lost on resume**

- **Requirement:** XP-LOG-001, REQ-RUN-103
- **Status:** FIXED (1.1, found 2026-10-07 while implementing GAP-018) · **Severity:** MEDIUM · **Priority:** P2
- **Problem:** After Save & Quit and Resume, the end-of-run XP breakdown and the post-run analytics (items used, mistakes per puzzle, score inputs) only covered the tiles played after the resume.
- **Root Cause:** `RunDirector._tileXpLog` and `PostRunAnalyticsService` lived on the director, not in `RunState`, so they were never saved. `StartRun` cleared them on every launch, including resumes.
- **Fix:** `RunState.TileXpLog` and `RunState.Analytics` (serializable, with safe defaults for 1.0 saves); `StartRun` no longer clears them; the tutorial and Monthly Walk starts create fresh analytics.
- **Affected Files:** `Assets/Scripts/Core/RuntimeModels.cs`, `Assets/Scripts/Run/RunDirector.cs`, `Assets/Scripts/Run/PostRunAnalyticsService.cs`
- **Tests:** `DeterminismAcrossResumeTests` (XP log survives a resume). Saves from 1.0 resume with an empty log; tiles played before the update cannot be recovered.

## GAP-039
**Pressure visuals vanished after a board refresh; refreshes ate one-shot flashes**

- **Requirement:** PRESSURE-UI-001..011
- **Status:** FIXED (1.1, found 2026-10-07 by the new pressure PlayMode tests) · **Severity:** LOW · **Priority:** P3
- **Problem:** Countdown badges, region strips, chain lines and the cell-lock tint disappeared after a cell click, a pencil toggle or an item use. A threat expiring on the same frame as such a refresh lost its flash.
- **Root Cause:** 12 call sites in `InRunController` rendered the board directly, without the pressure, lock and blur data. Every build of the pressure render data also consumed the one-shot flash queue.
- **Fix:** All refreshes go through `RenderBoard()`. `BuildPressureRenderData(consumeOneShots)` consumes flashes only on a placement.
- **Affected Files:** `Assets/Scripts/UI/InRunController.cs`
- **Tests:** `InRunUiPlayModeTests` pressure tests (badges, flashes, tints, strips and lines on the rendered board).

## GAP-040
**Item Block debuff applied the item effect but kept the item**

- **Requirement:** DEBUFF-DEF-015, DEBUFF-HOOK-017
- **Status:** FIXED (1.1, found 2026-10-07 while implementing GAP-026) · **Severity:** MEDIUM · **Priority:** P2
- **Problem:** While Item Block (RevealCost) was active, using an item still applied its effect, and then `TryUseItem` refused to consume it: a free, reusable item.
- **Root Cause:** `InRunController.ApplyItemEffect` ran the effect before asking `RunDirector.TryUseItem`, which checks the block.
- **Fix:** `ApplyItemEffect` checks `ItemUseBlockedMoves` first and shows "Items are blocked for N more correct placement(s)."
- **Affected Files:** `Assets/Scripts/UI/InRunController.cs`
- **Tests:** `BossDebuffTests.RevealCost_BlocksItemsForTwoCorrectPlacements` (service level). The UI order was checked by reading the code; no PlayMode test drives an item use under the block.

## GAP-041
**Number Freak L3 "+1 Pencil" was never applied**

- **Requirement:** REQ-CLASS-102
- **Status:** FIXED (1.1, found 2026-10-07 while implementing GAP-023) · **Severity:** LOW · **Priority:** P3
- **Problem:** Number Freak from level 3 started with one pencil less than its unlock schedule promises. This was the cause of the review's D07 pencil mismatch.
- **Root Cause:** The catalog text read "+ 1 Pencil" (with a space); the stat parser in `RunArchetypeService` expects "+1 Pencil" and skipped it.
- **Fix:** Text corrected in `ClassCatalog`. A test now checks that every stat bonus in every schedule is applied.
- **Affected Files:** `Assets/Scripts/Classes/ClassCatalog.cs`
- **Tests:** `ClassRosterTests.EveryStatBonusInTheSchedule_IsApplied`

## GAP-042
**Options dropdown lists hid their last option; no usable window mode**

- **Requirement:** DISPLAY-MODE-001..006 (GameDesignSpec 1.7 §Display Modes, added 2026-10-12 on request)
- **Status:** FIXED (1.1, found 2026-10-12 while adding the requested window modes) · **Severity:** MEDIUM · **Priority:** P2
- **Problem:** The Display Mode list showed "Fullscreen", "Borderless" and half of "Windowed"; every main-menu Options dropdown hid its last option, and the open list was see-through. "Windowed" kept the display-sized resolution, so on a 1080p screen its title bar ended up off-screen, and "Borderless" covered the whole display: there was no usable window mode.
- **Root Cause:** Unity sizes an open list as item-template height × item count, measured against the template's Content rect. The template used 28 px items while its layout group used 30 px items plus spacing and padding, and Content was 0 px tall, which made Unity subtract one item height. Window sizes were never fitted to the display.
- **Fix:** Dropdown items are 32 px with no extra spacing, Content starts one item tall, the list background is opaque (`MainMenuBlueprintBuilder.BuildDropdown`). Four display modes: Fullscreen, Borderless Fullscreen, Windowed, Frameless Window; window modes fit the display (DISPLAY-MODE-002) and are centred; Frameless Window sets the Win32 window style (`WindowFrameService`, re-applied after Alt+Enter and when Windows restores the old size).
- **Affected Files:** `Assets/Scripts/UI/MainMenuBlueprintBuilder.cs`, `Assets/Scripts/UI/OptionsController.cs`, `Assets/Scripts/Platform/WindowFrameService.cs`, `Assets/Scripts/Core/GameEnums.cs`, `Assets/Scripts/Save/SaveFileService.cs`, localization catalogs
- **Tests:** `DisplayModeTests` (10); `GeneratedUiPlayModeTraversalTests.OptionsPanelTabsAndLanguageConfirmation_AreRuntimeNavigable` checks the four labels and that the open list shows every item (fails on the old dropdown code: "Item 2: Windowed is cut off at the bottom"); build checks B16–B19 (E2E report §10.2b).

## GAP-043
**Without the `ActiveProfileSlot` pref the game opens an empty slot 0, not the existing profile**

- **Requirement:** SAVE-SLOT-004 (SaveLoadArchitecture 2.4, new); SAVE-SLOT-002 (`SaveProfileService`, ActiveSlot in PlayerPrefs); startup flow REQ-UI-100 (profile select only when no profile exists)
- **Status:** FIXED (0.1.5; found 2026-10-14 in build check B24, same behaviour in the earlier builds) · **Severity:** LOW · **Priority:** P3
- **Problem:** The active slot lives in PlayerPrefs (registry), the profiles in `save_profile_*.json`. When the pref is missing but a profile exists in slot 1 or 2, the game opens slot 0, creates `save_profile_0.json` and shows first-time setup. Realistic case: a player with a slot 1 or 2 profile installs on a new PC; Steam Auto-Cloud restores the JSON files but not the registry. Nothing is lost (Profiles → choose the slot), but the player first sees an empty profile, and the new slot 0 file is synced to the cloud.
- **Root Cause:** `SaveProfileService.ActiveSlot` returns `PlayerPrefs.GetInt("ActiveProfileSlot", 0)`; `StartupFlowController` shows profile select only when no slot exists.
- **Fix:** `SaveProfileService.ActiveSlot` resolves a missing pref through `EnsureActiveSlot`: the most recently written profile file becomes the active slot and is stored (ties: lower slot); without any profile it stays 0 and nothing is stored, so a fresh install keeps profile select. The getter does it, so every reader agrees whichever runs first.
- **Affected Files:** `Assets/Scripts/Save/SaveProfileService.cs`
- **Tests:** `ActiveSlotRecoveryTests` (3: newest profile chosen and stored; no profile → slot 0, nothing stored; a stored slot wins over a newer profile); build check B26 (E2E report §10.2e).

## GAP-044
**Region borders showed the previous puzzle's regions on a same-size board**

- **Requirement:** GEN-REGION-004 (SudokuGenerationPipeline 1.7, new); GEN-REGION-001/002
- **Status:** FIXED (found 2026-10-14 by the owner in a run: 5×5, Floor 1, irregular regions) · **Severity:** HIGH · **Priority:** P1
- **Problem:** The gold region borders on a 5×5 board contradicted the digits: two 5s and two 3s sat inside one drawn region, and a drawn region could not take a 3, so the puzzle looked unsolvable with sudoku rules. The digits, the cell shading and the rule checks used the puzzle's real regions; only the drawn borders were wrong. The board itself was valid (unique solution).
- **Root Cause:** `BoardViewController.RebuildBoard` drew the borders only when it rebuilt the grid, which happens when the board size changes. A new puzzle only marked the overlays dirty, so the next puzzle of the same size kept the previous puzzle's region borders. Irregular layouts (every 5×5 board, and 6×6–9×9 with irregular puzzles on) made it visible.
- **Fix:** `RenderBoard` syncs the borders whenever the board or its region map changes (`SyncRegionBorders`); a board without a region map clears them.
- **Affected Files:** `Assets/Scripts/UI/BoardViewController.cs`
- **Tests:** `InRunUiPlayModeTests.RegionBorders_FollowTheNextPuzzleOfTheSameSize` (6×6 rectangular, then 6×6 irregular: the borders follow the second board).

## GAP-045
**Boss fights never played boss music**

- **Requirement:** AUDIO-MUSIC-003 (AudioVisualDirection 1.6)
- **Status:** FIXED (found 2026-10-14 while preparing the 8-bit soundtrack) · **Severity:** LOW · **Priority:** P3
- **Problem:** The spec gives every floor a boss music layer, and the five boss clips ship, but no boss puzzle ever played them.
- **Root Cause:** `RunAudioController.StartBossLayer` had no caller. Its additive layer design would also not fit AI-made tracks, which cannot be composed against the floor track sample-exactly.
- **Fix:** `InRunController.ShowSudoku` starts the boss music when `CurrentLevelConfig.IsBoss`; the boss track replaces the floor track with a crossfade on the music source and ends when the puzzle is left.
- **Affected Files:** `Assets/Scripts/UI/RunAudioController.cs`, `Assets/Scripts/UI/InRunController.cs`
- **Tests:** `AudioClipPlayModeTests.BossPuzzle_ReplacesTheFloorTrackWithTheBossTrack_AndLeavingEndsIt`.
